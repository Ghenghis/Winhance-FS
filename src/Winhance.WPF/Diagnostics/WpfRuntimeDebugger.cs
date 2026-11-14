#nullable enable
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace Winhance.WPF.Diagnostics
{
    /// <summary>
    /// WPF Runtime Debugger - Captures what static analysis CANNOT:
    /// - XamlParseException details (line/pos/baseUri)
    /// - Missing resource keys / merged dictionary order issues
    /// - Assembly + pack URI resolution hints
    /// - Dispatcher + AppDomain + TaskScheduler exception traps
    /// - Full inner-exception chains with HResult
    ///
    /// Writes logs to: %LOCALAPPDATA%\Winhance\diagnostics\.
    /// </summary>
    public static class WpfRuntimeDebugger
    {
        private static int _installed;
        private static string? _appNameOverride;
        private static bool _showMessageBox;

        /// <summary>
        /// Install the debugger - call FIRST in App.OnStartup() or App() constructor.
        /// </summary>
        public static void Install(string? appNameOverride = null, bool showMessageBoxOnCrash = true)
        {
            if (System.Threading.Interlocked.Exchange(ref _installed, 1) == 1)
            {
                return;
            }

            _appNameOverride = appNameOverride;
            _showMessageBox = showMessageBoxOnCrash;

            // AppDomain unhandled
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Dump("AppDomain.UnhandledException", ex);
                }
                else
                {
                    DumpText("AppDomain.UnhandledException", $"Non-Exception: {e.ExceptionObject}");
                }
            };

            // Task scheduler unobserved
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Dump("TaskScheduler.UnobservedTaskException", e.Exception);
                e.SetObserved();
            };

            // Dispatcher unhandled
            if (Application.Current != null)
            {
                Application.Current.DispatcherUnhandledException += (_, e) =>
                {
                    var path = Dump("DispatcherUnhandledException", e.Exception);
                    e.Handled = true;
                    if (_showMessageBox)
                    {
                        ShowCrashMessage(e.Exception, path);
                    }
                };

                Application.Current.Exit += (_, __) =>
                    DumpText("AppExit", "Application exit event fired.");

                // Snapshot resource dictionaries early
                TrySnapshotResourceDictionaries();
            }

            DumpText("Install", $"WpfRuntimeDebugger installed at {DateTimeOffset.Now:O}");
        }

        /// <summary>
        /// Dump exception with full chain + environment + resources.
        /// </summary>
        /// <returns></returns>
        public static string Dump(string tag, Exception ex)
        {
            var dir = GetDiagnosticsDir();
            Directory.CreateDirectory(dir);

            var file = Path.Combine(dir, $"{Timestamp()}_{Sanitize(tag)}.txt");
            var sb = new StringBuilder();

            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine("  WPF RUNTIME DEBUG DUMP");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine($"Tag: {tag}");
            sb.AppendLine($"Time: {DateTimeOffset.Now:O}");
            sb.AppendLine($"Process: {Environment.ProcessId}");
            sb.AppendLine($"Thread: {Environment.CurrentManagedThreadId}");
            sb.AppendLine($"CLR: {Environment.Version}");
            sb.AppendLine($"OS: {Environment.OSVersion}");
            sb.AppendLine($"Exe: {GetExePath()}");
            sb.AppendLine($"BaseDir: {AppContext.BaseDirectory}");
            sb.AppendLine();

            AppendExceptionChain(sb, ex);

            sb.AppendLine();
            sb.AppendLine("═══ MERGED RESOURCE DICTIONARIES ═══");
            sb.AppendLine(DescribeMergedDictionaries());

            sb.AppendLine();
            sb.AppendLine("═══ TOP-LEVEL RESOURCE KEYS (first 200) ═══");
            sb.AppendLine(DescribeResourceKeys(Application.Current?.Resources, 200));

            sb.AppendLine();
            sb.AppendLine("═══ LOADED ASSEMBLIES (first 80) ═══");
            sb.AppendLine(DescribeAssemblies(80));

            File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
            return file;
        }

        public static string DumpText(string tag, string text)
        {
            var dir = GetDiagnosticsDir();
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"{Timestamp()}_{Sanitize(tag)}.txt");
            File.WriteAllText(file, text, Encoding.UTF8);
            return file;
        }

        private static void AppendExceptionChain(StringBuilder sb, Exception ex)
        {
            sb.AppendLine("═══ EXCEPTION CHAIN (outer → inner) ═══");
            int depth = 0;
            Exception? cur = ex;

            while (cur != null && depth < 30)
            {
                sb.AppendLine();
                sb.AppendLine($"─── Depth {depth} ───");
                sb.AppendLine($"Type: {cur.GetType().FullName}");
                sb.AppendLine($"Message: {cur.Message}");
                sb.AppendLine($"HResult: 0x{cur.HResult:X8}");
                sb.AppendLine($"Source: {cur.Source}");
                sb.AppendLine($"TargetSite: {cur.TargetSite}");

                // XamlParseException special handling - THIS IS THE KEY
                if (cur is XamlParseException xpe)
                {
                    sb.AppendLine();
                    sb.AppendLine("### XamlParseException Details ###");
                    sb.AppendLine($"LineNumber: {xpe.LineNumber}");
                    sb.AppendLine($"LinePosition: {xpe.LinePosition}");
                    sb.AppendLine($"BaseUri: {xpe.BaseUri}");
                    sb.AppendLine($"NameContext: {xpe.NameContext}");

                    // Extract the actual missing resource key from the message
                    if (cur.Message.Contains("StaticResourceExtension", StringComparison.Ordinal))
                    {
                        sb.AppendLine();
                        sb.AppendLine(">>> STATICRESOURCE FAILURE <<<");
                        sb.AppendLine("A {StaticResource X} reference could not find key 'X'");
                        sb.AppendLine("CHECK: ResourceDictionary merge order in App.xaml");
                        sb.AppendLine("CHECK: Key spelling matches exactly (case-sensitive)");
                        sb.AppendLine("CHECK: pack:// URI paths are correct");
                    }
                }

                sb.AppendLine();
                sb.AppendLine("StackTrace:");
                sb.AppendLine(cur.StackTrace ?? "(no stack trace)");

                cur = cur.InnerException;
                depth++;
            }
        }

        private static string DescribeMergedDictionaries()
        {
            try
            {
                var appRes = Application.Current?.Resources;
                if (appRes == null)
                {
                    return "(Application.Current.Resources is null)";
                }

                var sb = new StringBuilder();
                int i = 0;
                foreach (var d in appRes.MergedDictionaries)
                {
                    string src = d.Source?.ToString() ?? "(no Source)";
                    int keys = 0;
                    try
                    {
                        keys = d.Keys.Count;
                    }
                    catch
                    {
                    }

                    sb.AppendLine($"[{i++}] Source={src} | Keys={keys}");
                }

                return sb.Length == 0 ? "(No merged dictionaries)" : sb.ToString();
            }
            catch (Exception e)
            {
                return $"(Failed: {e.Message})";
            }
        }

        private static string DescribeResourceKeys(ResourceDictionary? dict, int max)
        {
            if (dict == null)
            {
                return "(null)";
            }

            try
            {
                var sb = new StringBuilder();
                int count = 0;
                foreach (var k in dict.Keys)
                {
                    if (count++ >= max)
                    {
                        break;
                    }

                    sb.AppendLine($"- {k}");
                }

                return sb.Length == 0 ? "(No keys)" : sb.ToString();
            }
            catch (Exception e)
            {
                return $"(Failed: {e.Message})";
            }
        }

        private static string DescribeAssemblies(int max)
        {
            try
            {
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                Array.Sort(asms, (a, b) => string.Compare(a.FullName, b.FullName, StringComparison.OrdinalIgnoreCase));
                var sb = new StringBuilder();
                int count = 0;
                foreach (var a in asms)
                {
                    if (count++ >= max)
                    {
                        break;
                    }

                    string loc;
                    try
                    {
                        loc = a.Location;
                    }
                    catch
                    {
                        loc = "(dynamic)";
                    }

                    sb.AppendLine($"{a.GetName().Name} | {a.GetName().Version} | {loc}");
                }

                return sb.ToString();
            }
            catch (Exception e)
            {
                return $"(Failed: {e.Message})";
            }
        }

        private static void TrySnapshotResourceDictionaries()
        {
            try
            {
                if (Application.Current == null)
                {
                    return;
                }

                foreach (var d in Application.Current.Resources.MergedDictionaries)
                {
                    _ = d.Source; // Force evaluation,
                }

                DumpText("ResourceDictionarySnapshot", DescribeMergedDictionaries());
            }
            catch (Exception e)
            {
                Dump("ResourceDictionarySnapshotFailed", e);
            }
        }

        private static void ShowCrashMessage(Exception ex, string logPath)
        {
            try
            {
                MessageBox.Show(
                    $"A runtime exception occurred.\n\nLog written to:\n{logPath}\n\nMessage:\n{ex.Message}",
                    "WPF Runtime Crash",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
            }
        }

        private static string GetDiagnosticsDir()
        {
            var appName = _appNameOverride ?? Assembly.GetEntryAssembly()?.GetName().Name ?? "Winhance";
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName, "diagnostics");
        }

        private static string Timestamp() =>
            DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);

        private static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                s = s.Replace(c, '_');
            }

            return s.Replace(' ', '_');
        }

        private static string GetExePath()
        {
            try
            {
                return Process.GetCurrentProcess().MainModule?.FileName ?? "(unknown)";
            }
            catch
            {
                return "(unknown)";
            }
        }

        public static string GetDiagnosticsPath() => GetDiagnosticsDir();
    }
}
