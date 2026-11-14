using System;
using System.IO;
using System.Text;
using System.Windows.Markup;

namespace Winhance.WPF.Diagnostics;

internal static class ExceptionDump
{
    public static string Build(Exception ex)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== EXCEPTION DUMP ===");
        sb.AppendLine(DateTime.Now.ToString("O"));
        sb.AppendLine();

        int depth = 0;
        Exception? cur = ex;

        while (cur != null)
        {
            sb.AppendLine($"--- Level {depth} ---");
            sb.AppendLine(cur.GetType().FullName);
            sb.AppendLine(cur.Message);
            sb.AppendLine(cur.StackTrace);
            sb.AppendLine();

            // XamlParseException sometimes has extra useful fields
            if (cur is XamlParseException xpe)
            {
                sb.AppendLine($"XAML Line: {xpe.LineNumber}, Position: {xpe.LinePosition}");
                if (xpe.BaseUri != null)
                {
                    sb.AppendLine($"BaseUri: {xpe.BaseUri}");
                }

                sb.AppendLine();
            }

            cur = cur.InnerException;
            depth++;
        }

        return sb.ToString();
    }

    public static string WriteToKnownPath(Exception ex, string filePrefix)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Winhance",
            "CrashLogs");

        Directory.CreateDirectory(dir);

        var file = Path.Combine(dir, $"{filePrefix}-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        File.WriteAllText(file, Build(ex));
        return file;
    }
}
