"""Search tab end-to-end checks (imported by e2e.py).

Indexes the sandbox folder C:/Temp/fmtest/search for every screenshot. One check also indexes drive C: through
the NTFS file table to prove the fast path works in the real app, but nothing from C: is screenshotted. The
saved index is deleted at the end so the app starts with no index next time.
"""
import os
import re
import time

INDEX_FILE = os.path.join(os.environ['LOCALAPPDATA'], 'Winhance-FS', 'search-index.bin')


def build(sb):
    root = os.path.join(sb, 'search')
    files = [
        r'Projects\winhance\README.md',
        r'Projects\winhance\src\main.rs',
        r'Projects\website\readme.txt',
        r'Documents\Taxes\Tax Return 2024.pdf',
        r'Documents\Taxes\Tax Return 2023.pdf',
        r'Documents\Quarterly Report Q3.xlsx',
        r'Documents\Report draft (old).docx',
        r'Photos\2024 Trip\IMG_0001.jpg',
        r'Photos\2024 Trip\IMG_0002.jpg',
        r'Photos\2024 Trip\IMG_0003.JPG',
        r'Downloads\ubuntu-24.04-desktop.iso',
        r'Downloads\setup.exe',
    ]
    for rel in files:
        path = os.path.join(root, rel)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, 'wb') as fh:
            fh.write(os.urandom(2048 if rel.endswith('.iso') else 300))
    return root


def install(e2e):
    ui, check, wait, shot, SB = e2e.ui, e2e.check, e2e.wait, e2e.shot, e2e.SB

    def drive_boxes():
        return [l.split(' = ')[0] for l in ui('checks').splitlines() if l.startswith('Index drive ')]

    def names():
        return [r.split(' | ')[0] for r in ui('rows', Index=0).splitlines() if r.strip()]

    def search(q, expect=None, timeout=10):
        ui('setnamed', Name='Search files', Value=q)
        if expect is None:
            time.sleep(1)
        else:
            wait(lambda: expect(names()), timeout=timeout, step=0.4)
        return names()

    def built(pattern):
        return wait(lambda: re.search(pattern, ui('texts', Value='Indexed|failed')) is not None, timeout=180, step=1)

    def search_tab():
        root = build(SB)
        if os.path.exists(INDEX_FILE):
            os.remove(INDEX_FILE)
        out = ui('tab', Value='Search'); time.sleep(1.5)
        check('Search tab selectable by UI Automation', 'selected' in out, out)
        check('Search tab explains how to start', bool(ui('texts', Value="click 'Build Index'")))
        check('engine is the Rust engine 1.5.0', bool(ui('texts', Value=r'Rust engine 1\.5\.0')))

        # Untick every drive and add only the sandbox; never build until that is confirmed, so no real drive
        # content can appear in screenshots.
        wait(lambda: bool(drive_boxes()), timeout=30, step=1)
        def all_off():
            states = [l for l in ui('checks').splitlines() if l.startswith('Index drive ')]
            for l in states:
                if not l.endswith('= Off'):
                    ui('check', Name=l.split(' = ')[0], Value='off')
            return bool(states) and all(l.endswith('= Off') for l in ui('checks').splitlines() if l.startswith('Index drive '))
        unticked = wait(all_off, timeout=30, step=1)
        check('all drives unticked', unticked, ui('checks'))
        def folder_added():
            ui('setnamed', Name='Folder to index', Value=root)
            ui('btnname', Value='Add Folder')
            time.sleep(1)
            return bool(ui('texts', Value='^Added .*fmtest'))
        added = wait(folder_added, timeout=30, step=2)
        check('sandbox folder added to the index list', added, ui('texts', Value='Added|not found'))
        if not (unticked and added):
            raise RuntimeError('index setup not confirmed; not building (would index a real drive)')
        ui('btnname', Value='Build Index')
        ok = built(r'Indexed 21 items')
        status = ui('texts', Value='Indexed|failed')
        check('builds an index of the folder (12 files, 9 folders)', ok and 'Indexed 21 items' in status, status)
        check('index summary names the folder', bool(ui('texts', Value=re.escape(root) + '.*built')))
        check('index saved to disk', os.path.exists(INDEX_FILE), INDEX_FILE)

        rows = search('report', lambda n: len(n) == 2)
        check("'report' finds the 2 reports, best match first",
              rows == ['Report draft (old).docx', 'Quarterly Report Q3.xlsx'], ' / '.join(rows))
        summary = ui('texts', Value=r'match')
        check('result summary shows count and time', any(re.search(r'2 matches · \d+ ms', s) for s in summary.splitlines()), summary)
        shot('search')

        rows = search('readme', lambda n: len(n) == 2)
        check("'readme' finds README.md and readme.txt (case-insensitive)", sorted(rows) == ['README.md', 'readme.txt'], ' / '.join(rows))
        rows = search('tax 2024', lambda n: len(n) == 1)
        check("'tax 2024' needs both words", rows == ['Tax Return 2024.pdf'], ' / '.join(rows))
        rows = search('*.jpg', lambda n: len(n) == 3)
        check("'*.jpg' wildcard (any case)", len(rows) == 3 and all(r.upper().endswith('.JPG') for r in rows), ' / '.join(rows))
        rows = search(r'winhance\ readme', lambda n: len(n) == 1)
        check(r"'winhance\ readme' matches the path", rows == ['README.md'], ' / '.join(rows))
        rows = search('report !draft', lambda n: len(n) == 1)
        check("'!draft' excludes", rows == ['Quarterly Report Q3.xlsx'], ' / '.join(rows))
        rows = search('2024 folder:', lambda n: len(n) == 1)
        check("'folder:' keeps folders", rows == ['2024 Trip'], ' / '.join(rows))
        rows = search('ext:iso;exe', lambda n: len(n) == 2)
        check("'ext:iso;exe' filters by type", sorted(rows) == ['setup.exe', 'ubuntu-24.04-desktop.iso'], ' / '.join(rows))
        rows = search('zzz-nothing', lambda n: len(n) == 0)
        check('no matches shows nothing', rows == [] and bool(ui('texts', Value='No matches')))

        os.remove(os.path.join(root, r'Downloads\setup.exe'))
        search('setup', lambda n: len(n) == 1)
        row = ui('rows', Index=0)
        check('a file deleted after indexing is marked', 'deleted or moved' in row, row)
        shot('search-syntax')

        # The real fast path: index drive C: from its file table (not screenshotted).
        if any(b == 'Index drive C:\\' for b in drive_boxes()):
            ui('check', Name='Index drive C:\\', Value='on')
            t = time.time()
            ui('btnname', Value='Build Index')
            ok = built(r'Indexed [\d,]{9,} items')
            secs = time.time() - t
            status = ui('texts', Value='Indexed|failed')
            m = re.search(r'Indexed ([\d,]+) items', status)
            count = int(m.group(1).replace(',', '')) if m else 0
            check('drive C: indexed from the file table, over 100,000 items', ok and count > 100_000 and bool(ui('texts', Value=r'C:\\ \(drive table\)')), status[:200])
            check('drive C: index built in under 60 seconds', secs < 60, f'{secs:.0f}s')
            rows = search('notepad.exe', lambda n: len(n) > 0)
            check('finds Windows files on C: (hard links listed)', any(r.lower() == 'notepad.exe' for r in rows), ' / '.join(rows[:5]))
            ui('check', Name='Index drive C:\\', Value='off')
            ui('btnname', Value='Build Index')
            built(r'Indexed 20 items')
            search('report', lambda n: len(n) == 2)

        if os.path.exists(INDEX_FILE):
            os.remove(INDEX_FILE)

    return [search_tab]
