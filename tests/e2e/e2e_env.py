"""Environment & PATH Doctor end-to-end checks (imported by e2e.py).

Read-only by design: it scans, browses every section, opens the repair preview and answers **No**, then proves
the User and System environment registry keys are byte-for-byte unchanged. It never applies a repair to the
machine running the sweep (repairs are covered by unit tests against an in-memory store).
"""
import re
import subprocess
import time
import winreg

KEYS = [
    (winreg.HKEY_CURRENT_USER, r'Environment'),
    (winreg.HKEY_LOCAL_MACHINE, r'SYSTEM\CurrentControlSet\Control\Session Manager\Environment'),
]


def env_fingerprint():
    data = {}
    for hive, path in KEYS:
        with winreg.OpenKey(hive, path) as k:
            i = 0
            while True:
                try:
                    name, value, kind = winreg.EnumValue(k, i)
                except OSError:
                    break
                data[(path, name)] = (value, kind)
                i += 1
    return data


def install(e2e):
    ui, ps, check, wait, shot, W = e2e.ui, e2e.ps, e2e.check, e2e.wait, e2e.shot, e2e.W

    def environment():
        before = env_fingerprint()
        out = ui('tab', Value='Environment'); time.sleep(1.5)
        check('Environment tab selectable by UI Automation (keyboard/screen-reader path)', 'selected' in out, out)
        check('Environment page shows its empty state', 'Scan' in ui('texts', Value="Click 'Scan'"), '')
        ui('btnname', Value='Scan')
        ok = wait(lambda: re.search(r'\d+ error\(s\)', ui('texts', Value=r'error\(s\)')) is not None, timeout=90, step=2)
        summary = ui('texts', Value=r'error\(s\)')
        check('scan completes and shows a summary', ok, summary)
        status = ui('texts', Value='Found|No problems')
        check('status explains what was found and what is pre-selected', 'pre-selected' in status or 'No problems' in status, status[:160])
        problems = ui('rows', Index=0).splitlines()
        check('problems list is populated', len([p for p in problems if p.strip()]) > 0, f'{len(problems)} rows')
        shot('env-problems')

        for section, needle in [('PATH', 'PATH'), ('Command conflicts', 'Command'), ('All variables', 'Variables'), ('Backups', 'Backups')]:
            r = ui('tab', Value=section); time.sleep(0.8)
            check(f'section opens: {section}', 'selected' in r, r)
            shot('env-' + section.replace(' ', '-').lower())
            if section == 'PATH':
                check('PATH section lists entries', len(ui('rows', Index=0).splitlines()) > 0)
            if section == 'All variables':
                rows = ui('rows', Index=0)
                check('variables list shows values with secrets masked', 'Path' in rows and not re.search(r'(?i)(password|token|secret)[^|]*\|[^|]*\|[^|]*\|\s*[^•\s]', rows), '')

        ui('tab', Value='Problems'); time.sleep(0.8)
        subprocess.Popen(['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', f'{W}/uia2.ps1', '-Action', 'btnname', '-Value', 'Fix Selected...'])
        time.sleep(3)
        msg = ps('msgbox.ps1', '-Title', 'Repair environment')
        if 'NO MESSAGEBOX' in msg:
            status = ui('texts', Value='Select at least one|Nothing to change')
            check('Fix Selected with nothing safe selected explains why', bool(status.strip()), msg)
        else:
            check('repair shows a preview naming the variables and the backup', 'will change' in msg and 'backup' in msg.lower(), msg[:160])
            ps('msgbox.ps1', '-Title', 'Repair environment', '-Button', 'No'); time.sleep(1)
            check('answering No cancels', 'nothing changed' in ui('texts', Value='cancelled').lower())

        check('environment registry keys unchanged by scan/preview/cancel', env_fingerprint() == before)

    return [environment]
