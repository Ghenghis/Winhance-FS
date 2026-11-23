"""Winhance end-to-end sweep harness.

Drives the real app through UI Automation (PowerShell helpers next to this file) against a throwaway sandbox in
C:/Temp/fmtest and verifies every result on disk. Never touches system settings: the non-File-Manager pages are
only navigated to and screenshotted; nothing on them is clicked.

usage: python e2e.py <sweep-name>
"""
import os, sys, time, shutil, subprocess, glob, re, ctypes

SWEEP = sys.argv[1] if len(sys.argv) > 1 else 'sweep'
W = os.path.dirname(os.path.abspath(__file__)).replace('\\', '/')
SB = r'C:\Temp\fmtest'
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
EXE = os.path.join(REPO, 'src', 'Winhance.WPF', 'bin', 'Debug', 'net9.0-windows', 'Winhance.exe')
SHOTS = os.path.join(os.environ.get('TEMP', W), 'winhance-e2e', SWEEP)
LOGDIR = r'C:\ProgramData\Winhance\Logs'
os.makedirs(SHOTS, exist_ok=True)
results = []


def ps(script, *args, timeout=90):
    r = subprocess.run(['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command',
                        "[Console]::OutputEncoding=[Text.Encoding]::UTF8; & '" + f'{W}/{script}' + "' " + ' '.join(str(a) if re.fullmatch(r'-[A-Za-z]+', str(a)) else "'" + str(a).replace("'", "''") + "'" for a in args)],
                       capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=timeout)
    return (r.stdout + r.stderr).strip()


def ui(action, **kw):
    args = ['-Action', action]
    for k, v in kw.items():
        if v is True:
            args.append(f'-{k}')
        else:
            args += [f'-{k}', str(v)]
    return ps('uia2.ps1', *args)


def shot(name):
    ps('shot.ps1', '-Out', os.path.join(SHOTS, f'{len(os.listdir(SHOTS)):02d}-{name}.png'))


def check(name, ok, detail=''):
    results.append((name, bool(ok), detail))
    print(f"{'PASS' if ok else 'FAIL'}  {name}" + (f'  [{detail}]' if detail and not ok else ''), flush=True)


def ls(p):
    return sorted(os.listdir(p)) if os.path.isdir(p) else []


def wait(pred, timeout=10, step=0.3):
    end = time.time() + timeout
    while time.time() < end:
        if pred():
            return True
        time.sleep(step)
    return pred()


def write(p, text='x'):
    os.makedirs(os.path.dirname(p), exist_ok=True)
    with open(p, 'w', encoding='utf-8') as fh:
        fh.write(text)


# ---------------------------------------------------------------- fixture
def build_fixture():
    if os.path.isdir(SB):
        shutil.rmtree(SB)
    for f in ['alpha.txt', 'beta.txt', 'IMG_0001.jpg', 'IMG_0002.jpg', 'IMG_0003.jpg', 'Résumé café.txt',
              'a file with a very long name that keeps going and going to test column clipping.txt']:
        write(os.path.join(SB, 'left', f), f)
    write(os.path.join(SB, 'left', 'sub', 'gamma.txt'), 'gamma')
    write(os.path.join(SB, 'left', 'folderA', 'inner.txt'), 'inner')
    os.makedirs(os.path.join(SB, 'right'))
    for i in range(1, 5):
        write(os.path.join(SB, 'rename', f'IMG_000{i}.jpg'), f'p{i}')
    write(os.path.join(SB, 'rename', 'readme.txt'), 'doc')
    for f in ['report.pdf', 'notes.txt', 'todo.txt', 'photo1.jpg', 'photo2.png', 'song.mp3', 'data.csv', 'archive.zip', 'script.py']:
        write(os.path.join(SB, 'organize', f), f)
    os.makedirs(os.path.join(SB, 'organized-elsewhere'))


# ---------------------------------------------------------------- app lifecycle
def restart_app():
    subprocess.run(['powershell', '-NoProfile', '-Command',
                    "$p=Get-Process Winhance -ErrorAction SilentlyContinue; if($p){[void]$p.CloseMainWindow(); [void]$p.WaitForExit(15000)}"],
                   capture_output=True)
    startup = os.path.join(LOGDIR, 'WinhanceStartupLog.txt')
    before = sum(1 for _ in open(startup, encoding='utf-8', errors='ignore'))
    subprocess.Popen(['powershell', '-NoProfile', '-Command', f"Start-Process -FilePath '{EXE}' -WorkingDirectory '{os.path.dirname(EXE)}'"])

    def started():
        lines = open(startup, encoding='utf-8', errors='ignore').read().splitlines()[before:]
        return any('OnStartup method completed' in l or 'Error during startup' in l for l in lines)
    ok = wait(started, timeout=120, step=1)
    lines = open(startup, encoding='utf-8', errors='ignore').read().splitlines()[before:]
    check('app starts without startup errors', ok and not any('Error' in l for l in lines))
    time.sleep(2)


def current_log():
    logs = sorted(glob.glob(os.path.join(LOGDIR, 'Winhance_Log_*.log')), key=os.path.getmtime)
    return logs[-1] if logs else None


# ---------------------------------------------------------------- tests
def pages():
    for page in ['Software & Apps', 'Optimize', 'Customize', 'Advanced  Tools', 'Settings', 'File Manager']:
        out = ps('uia.ps1', '-Action', 'click', '-Name', page)
        time.sleep(2.5)
        shot('page-' + page.replace(' ', '').replace('&', 'and'))
        check(f'page opens: {page.strip()}', 'invoked' in out, out)


def set_panes(left, right):
    ui('setedit', Index=1, Value=left)
    ui('btnhelp', Value='Refresh', Index=0)
    ui('setedit', Index=2, Value=right)
    ui('btnhelp', Value='Refresh', Index=1)
    time.sleep(1)


def rows(i):
    return [r.split(' | ')[0] for r in ui('rows', Index=i).splitlines() if r.strip()]


def browser():
    L, R = os.path.join(SB, 'left'), os.path.join(SB, 'right')
    ui('clicktext', Value='File Browser'); time.sleep(1)
    set_panes(L, R)
    lr = rows(0)
    check('browser lists sandbox (incl. unicode + long names)', 'Résumé café.txt' in lr and any(n.startswith('a file with a very long') for n in lr) and 'sub' in lr, ','.join(lr))
    shot('browser-loaded')

    # multi-select copy F5
    ui('select', Index=0, Value='alpha.txt'); ui('select', Index=0, Value='beta.txt', Add=True); ui('keys', Value='{F5}')
    check('F5 copies multi-selection to other pane', wait(lambda: {'alpha.txt', 'beta.txt'} <= set(ls(R))) and {'alpha.txt', 'beta.txt'} <= set(ls(L)))
    check('right pane refreshes after copy', wait(lambda: 'alpha.txt' in rows(1), 5))

    # copy again -> name conflict produces a unique name, never overwrites
    ui('select', Index=0, Value='alpha.txt'); ui('keys', Value='{F5}')
    check('copy onto existing name keeps both (no overwrite)', wait(lambda: any(n.startswith('alpha') and n != 'alpha.txt' for n in ls(R))), ','.join(ls(R)))

    # move folder F6
    ui('select', Index=0, Value='folderA'); ui('keys', Value='{F6}')
    check('F6 moves a folder with contents', wait(lambda: os.path.isfile(os.path.join(R, 'folderA', 'inner.txt')) and not os.path.exists(os.path.join(L, 'folderA'))))

    # rename F2 (invalid then valid)
    ui('select', Index=1, Value='beta.txt'); ui('keys', Value='{F2}'); time.sleep(1)
    out = ps('dlg.ps1', '-Title', 'Rename', '-SetText', 'bad:name.txt', '-Click', 'Rename'); time.sleep(0.6)
    out2 = ps('dlg.ps1', '-Title', 'Rename', '-Read')
    check('rename rejects invalid characters', 'not allowed' in out2 and 'beta.txt' in ls(R), out2)
    shot('rename-validation')
    ps('dlg.ps1', '-Title', 'Rename', '-SetText', 'renamed.txt', '-Click', 'Rename')
    check('F2 rename applies', wait(lambda: 'renamed.txt' in ls(R) and 'beta.txt' not in ls(R)))

    # rename cancel keeps name
    ui('select', Index=1, Value='renamed.txt'); ui('keys', Value='{F2}'); time.sleep(1)
    ps('dlg.ps1', '-Title', 'Rename', '-SetText', 'should-not-apply.txt', '-Click', 'Cancel'); time.sleep(0.8)
    check('rename Cancel changes nothing', 'renamed.txt' in ls(R) and 'should-not-apply.txt' not in ls(R))

    # context menu items + delete via menu with confirm -> recycle bin
    items = ''
    for _ in range(3):  # Shift+F10 can race window activation; retry before judging
        ui('select', Index=1, Value='alpha.txt'); ui('keys', Value='+{F10}'); time.sleep(1.2)
        items = ui('menuitems')
        if 'Delete' in items:
            break
        ui('keys', Value='{ESC}')
    needed = ['Open', 'Cut', 'Copy', 'Paste', 'Rename', 'Delete', 'Copy to left pane', 'Move to left pane', 'New folder', 'Refresh', 'Open in Explorer', 'Properties']
    check('context menu has all items', all(n in items for n in needed), items.replace('\n', ','))
    shot('context-menu')
    ui('menu', Value='Delete'); time.sleep(1)
    msg = ps('msgbox.ps1', '-Title', 'Confirm Delete')
    check('delete asks for confirmation (Recycle Bin wording)', 'Recycle Bin' in msg, msg)
    shot('dialog-delete-confirm')
    ps('msgbox.ps1', '-Title', 'Confirm Delete', '-Button', 'No'); time.sleep(0.8)
    check('delete No keeps the file', 'alpha.txt' in ls(R))
    ui('select', Index=1, Value='alpha.txt'); ui('keys', Value='{DELETE}'); time.sleep(1)
    ps('msgbox.ps1', '-Title', 'Confirm Delete', '-Button', 'Yes')
    check('delete Yes removes the file', wait(lambda: 'alpha.txt' not in ls(R)))
    rb = subprocess.run(['powershell', '-NoProfile', '-Command',
                         "(New-Object -ComObject Shell.Application).NameSpace(10).Items() | ? { $_.Name -eq 'alpha.txt' } | % { $_.ExtendedProperty('System.Recycle.DeletedFrom') }"],
                        capture_output=True, text=True).stdout
    check('deleted file is in the Recycle Bin', 'fmtest\\right' in rb.lower() or 'fmtest\\right' in rb, rb.strip())

    # Explorer clipboard holding a COPY of left\IMG_0002; then cut IMG_0003 in-app and paste
    subprocess.run(['powershell', '-NoProfile', '-STA', '-Command',
                    "Add-Type -AssemblyName System.Windows.Forms; $c=New-Object System.Collections.Specialized.StringCollection; [void]$c.Add('" + os.path.join(L, 'IMG_0002.jpg') + "'); [System.Windows.Forms.Clipboard]::SetFileDropList($c)"])
    ui('select', Index=0, Value='IMG_0003.jpg'); ui('keys', Value='^x')
    ui('select', Index=1, Value='renamed.txt'); ui('keys', Value='^v')
    check('cut+paste moves only the cut file (Explorer clipboard not misused)',
          wait(lambda: 'IMG_0003.jpg' in ls(R)) and 'IMG_0003.jpg' not in ls(L) and 'IMG_0002.jpg' in ls(L) and 'IMG_0002.jpg' not in ls(R))
    # Explorer copy -> paste copies
    subprocess.run(['powershell', '-NoProfile', '-STA', '-Command',
                    "Add-Type -AssemblyName System.Windows.Forms; $c=New-Object System.Collections.Specialized.StringCollection; [void]$c.Add('" + os.path.join(L, 'IMG_0002.jpg') + "'); [System.Windows.Forms.Clipboard]::SetFileDropList($c)"])
    ui('select', Index=1, Value='renamed.txt'); ui('keys', Value='^v')
    check('Explorer copy pasted in app is a copy', wait(lambda: 'IMG_0002.jpg' in ls(R)) and 'IMG_0002.jpg' in ls(L))
    # app copy -> clipboard readable by Explorer with copy effect
    ui('select', Index=1, Value='IMG_0002.jpg'); ui('keys', Value='^c'); time.sleep(0.5)
    cb = subprocess.run(['powershell', '-NoProfile', '-STA', '-Command',
                         "Add-Type -AssemblyName System.Windows.Forms; ([System.Windows.Forms.Clipboard]::GetFileDropList() -join ','); $d=[System.Windows.Forms.Clipboard]::GetData('Preferred DropEffect'); if($d){$b=New-Object byte[] 4; [void]$d.Read($b,0,4); [BitConverter]::ToInt32($b,0)}"],
                        capture_output=True, text=True).stdout.split()
    check('Ctrl+C puts Explorer-compatible copy on clipboard', any('IMG_0002.jpg' in x for x in cb) and '5' in cb, ' '.join(cb))
    # paste folder into itself is refused
    ui('select', Index=1, Value='folderA'); ui('keys', Value='^c'); time.sleep(0.3)
    set_panes(L, os.path.join(R, 'folderA'))
    ui('select', Index=1, Value='inner.txt'); ui('keys', Value='^v'); time.sleep(1.5)
    check('pasting a folder into itself is refused', not os.path.exists(os.path.join(R, 'folderA', 'folderA')))
    set_panes(L, R)

    # new folder
    before = set(ls(R))
    ui('select', Index=1, Value='renamed.txt'); ui('keys', Value='^+n')
    check('Ctrl+Shift+N creates a folder', wait(lambda: any(n.startswith('New Folder') for n in set(ls(R)) - before)))

    # navigation: Enter on folder opens it, Backspace goes up, Back button returns
    ui('select', Index=0, Value='sub'); ui('keys', Value='{ENTER}'); time.sleep(1.2)
    check('Enter on a folder navigates into it', 'gamma.txt' in rows(0))
    ui('keys', Value='{BACKSPACE}'); time.sleep(1.2)
    check('Backspace navigates up', 'sub' in rows(0))
    ui('btnhelp', Value='Go Back', Index=0); time.sleep(1.2)
    check('Back button returns to previous folder', 'gamma.txt' in rows(0))
    ui('btnhelp', Value='Go Up', Index=0); time.sleep(1.2)
    check('Up button navigates to parent', 'sub' in rows(0))
    shot('browser-after-ops')


def batch_rename():
    D = os.path.join(SB, 'rename')
    ui('clicktext', Value='Batch Rename'); time.sleep(1.2)
    ui('setedit', Index=1, Value=D); ui('setedit', Index=2, Value='*.jpg')
    ui('btnname', Value='Clear All'); ui('btnname', Value='+ Find/Replace'); time.sleep(0.6)
    ui('setedit', Index=3, Value='IMG_'); ui('setedit', Index=4, Value='Vacation_')
    ps('uia.ps1', '-Action', 'focus', '-Name', 'Add Files...'); time.sleep(1.5)
    texts = ui('texts', Value='files selected')
    check('typed source path loads files (filter honoured)', '4 files selected' in texts, texts)
    prev = ui('rows', Index=0).splitlines()
    check('live preview updates without clicking Refresh', len(prev) == 4 and all('Vacation_' in p for p in prev), ' / '.join(prev))
    check('preview numbers rows', prev and prev[0].startswith('1 |'), prev[0] if prev else '')
    shot('batch-preview')
    ui('btnname', Value='Apply Rename'); time.sleep(2)
    check('apply renames only matching files', wait(lambda: sorted(ls(D)) == ['Vacation_0001.jpg', 'Vacation_0002.jpg', 'Vacation_0003.jpg', 'Vacation_0004.jpg', 'readme.txt']), ','.join(ls(D)))
    ui('btnname', Value='Undo Last Batch'); time.sleep(2)
    check('undo restores original names', wait(lambda: sorted(ls(D)) == ['IMG_0001.jpg', 'IMG_0002.jpg', 'IMG_0003.jpg', 'IMG_0004.jpg', 'readme.txt']), ','.join(ls(D)))
    ui('setedit', Index=1, Value=os.path.join(SB, 'does-not-exist')); ps('uia.ps1', '-Action', 'focus', '-Name', 'Add Files...'); time.sleep(1)
    check('missing folder reported clearly', 'not found' in ui('texts', Value='not found').lower() or '0 files' in ui('texts', Value='files selected'))


def organizer():
    D = os.path.join(SB, 'organize')
    E = os.path.join(SB, 'organized-elsewhere')
    ui('clicktext', Value='Smart Organizer'); time.sleep(1.2)
    ui('setedit', Index=1, Value=D); ui('btnname', Value='Analyze'); time.sleep(2)
    t = ui('texts', Value='Found')
    check('analyze categorizes files', 'Found 9 files in 5 categories' in t, t)
    shot('organizer-analyzed')
    ui('btnname', Value='Apply Organization'); time.sleep(2.5)
    moved = {r: ls(os.path.join(D, r)) for r in ['Documents', 'Images', 'Archives', 'Code', 'Audio']}
    check('apply moves files into category folders', sum(len(v) for v in moved.values()) == 9, str(moved))
    ui('btnname', Value='Undo'); time.sleep(2.5)
    check('undo restores files and removes empty folders', wait(lambda: len([f for f in ls(D) if os.path.isfile(os.path.join(D, f))]) == 9 and not any(os.path.isdir(os.path.join(D, f)) for f in ls(D))), ','.join(ls(D)))
    # second apply without re-analyze must be refused (plan cleared after apply) -> analyze again, move elsewhere
    ui('btnname', Value='Analyze'); time.sleep(2)
    ps('uia.ps1', '-Action', 'click', '-Name', 'Move to:'); time.sleep(0.5)
    ui('setedit', Index=2, Value=E)
    ui('btnname', Value='Apply Organization'); time.sleep(2.5)
    ok = sum(len(ls(os.path.join(E, d))) for d in ls(E) if os.path.isdir(os.path.join(E, d))) == 9
    check('"Move to" organizes into another folder', ok, ','.join(ls(E)))
    ui('btnname', Value='Undo'); time.sleep(2.5)
    check('undo of "Move to" restores source', wait(lambda: len(ls(D)) == 9), ','.join(ls(D)))
    ps('uia.ps1', '-Action', 'click', '-Name', 'Same folder with subfolders')


def space_recovery():
    ui('clicktext', Value='Space Recovery'); time.sleep(1.2)
    ui('btnname', Value='Analyze')
    ok = wait(lambda: 'Found' in ui('texts', Value='Found|No recovery'), timeout=150, step=3)
    t = ui('texts', Value='Recoverable|Found')
    check('analysis completes with a non-zero total', ok and re.search(r'[1-9][\d.]* (KB|MB|GB|TB) Recoverable', t), t.replace('\n', ' | '))
    r = ui('rows', Index=0).splitlines()
    names = [x.split(' | ')[0] for x in r]
    check('no duplicate locations', len(names) == len(set(names)), ','.join(names))
    shot('space-recovery')
    subprocess.Popen(['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', f'{W}/uia2.ps1', '-Action', 'btnname', '-Value', 'Clean Selected'])
    time.sleep(3)
    msg = ps('msgbox.ps1', '-Title', 'Confirm Clean')
    check('clean asks for confirmation naming the folder', 'Temp' in msg and 'cannot be undone' in msg, msg[:120])
    shot('dialog-clean-confirm')
    ps('msgbox.ps1', '-Title', 'Confirm Clean', '-Button', 'No'); time.sleep(1)
    check('clean No cancels', 'cancel' in ui('texts', Value='cancel').lower())


def log_check(log_before_lines):
    log = current_log()
    lines = open(log, encoding='utf-8', errors='ignore').read().splitlines()
    new = lines[log_before_lines:] if log_before_lines <= len(lines) else lines
    exc = [l for l in new if re.search(r'exception|\[ERROR\]', l, re.I)]
    check('no exceptions/errors logged during sweep', not exc, ' || '.join(exc[:3])[:300])


if __name__ == '__main__':
    t0 = time.time()
    build_fixture()
    restart_app()
    log = current_log()
    base = sum(1 for _ in open(log, encoding='utf-8', errors='ignore')) if log else 0
    sections = [pages, browser, batch_rename, organizer, space_recovery]
    if SWEEP != 'sweep1':
        sys.path.insert(0, W)
        import e2e_extended
        sections[2:2] = e2e_extended.install(sys.modules[__name__])
        import e2e_env
        sections += e2e_env.install(sys.modules[__name__])
        import e2e_v14
        sections += e2e_v14.install(sys.modules[__name__])
        import e2e_v15
        sections += e2e_v15.install(sys.modules[__name__])
    only = {'v14': ('duplicates', 'forensics'), 'v15': ('search_tab',)}.get(SWEEP)
    if only:  # just the sections new in that release
        ps('uia.ps1', '-Action', 'click', '-Name', 'File Manager'); time.sleep(2.5)
        sections = [s for s in sections if s.__name__ in only]
    for section in sections:
        try:
            section()
        except Exception as ex:  # keep sweeping; record the crash as a failure
            check(f'{section.__name__} section ran to completion', False, repr(ex))
    log_check(base)
    passed = sum(1 for _, ok, _ in results if ok)
    print(f'\n== {SWEEP}: {passed}/{len(results)} passed in {time.time() - t0:.0f}s; screenshots in {SHOTS}')
    with open(os.path.join(SHOTS, 'results.txt'), 'w', encoding='utf-8') as fh:
        for n, ok, d in results:
            fh.write(f"{'PASS' if ok else 'FAIL'}\t{n}\t{d}\n")
