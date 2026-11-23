"""Duplicate Finder and Forensics end-to-end checks (imported by e2e.py).

Works only inside the sandbox (C:/Temp/fmtest/dupes and C:/Temp/fmtest/forensics). Deleted duplicates go to the
Recycle Bin, and the Forensics Recycle Bin section then restores one of them, so the round trip is verified on disk.
"""
import os
import random
import re
import subprocess
import time

DAY = 86400


def _program(size):
    """Bytes that look like a Windows program (MZ header pointing at a PE signature)."""
    data = bytearray(random.Random(3).randbytes(size))
    data[0:2] = b'MZ'
    data[0x3C:0x40] = (0x80).to_bytes(4, 'little')
    data[0x80:0x84] = b'PE\0\0'
    return bytes(data)


def build(sb):
    d = os.path.join(sb, 'dupes')
    photo = random.Random(1).randbytes(200_000)
    report = random.Random(2).randbytes(150_000)
    now = time.time()
    files = {
        r'photos\IMG_1001.jpg': (photo, now - 1 * DAY),          # newest copy: kept
        r'backup\IMG_1001.jpg': (photo, now - 20 * DAY),
        r'backup\old\IMG_1001 (copy).jpg': (photo, now - 40 * DAY),
        r'docs\report.pdf': (report, now - 2 * DAY),             # newest copy: kept
        r'archive\report-final.pdf': (report, now - 30 * DAY),
        r'docs\unique.bin': (random.Random(9).randbytes(130_000), now),
    }
    for rel, (data, mtime) in files.items():
        path = os.path.join(d, rel)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, 'wb') as fh:
            fh.write(data)
        os.utime(path, (mtime, mtime))

    f = os.path.join(sb, 'forensics')
    os.makedirs(f, exist_ok=True)

    def put(name, data):
        with open(os.path.join(f, name), 'wb') as fh:
            fh.write(data)

    put('invoice.pdf', b'%PDF-1.4\n' + b'1 0 obj << /Type /Catalog >> endobj\n' * 300)
    put('invoice.pdf:Zone.Identifier', b'[ZoneTransfer]\r\nZoneId=3\r\nHostUrl=https://example.com/invoice.pdf\r\n')
    put('notes.txt', b'Shopping list: milk, eggs, bread.\n' * 40)
    put('notes.txt:secret', b'hidden text that Explorer never shows')
    put('report.txt', b'Quarterly report draft.\n' * 50)
    put('report.txt:payload', _program(8192))
    put('holiday-photo.jpg', _program(90_000))
    put('thesis.docx', random.Random(4).randbytes(120_000))
    put('budget.xlsx', random.Random(5).randbytes(80_000))
    put('readme.txt', b'Plain readable text. ' * 200)
    put('image.png', b'\xff\xd8\xff\xe0\x00\x10JFIF\x00' + random.Random(6).randbytes(20_000))
    return d, f


def install(e2e):
    ui, ps, check, wait, shot, W, SB = e2e.ui, e2e.ps, e2e.check, e2e.wait, e2e.shot, e2e.W, e2e.SB
    state = {}

    def background(action, value):
        subprocess.Popen(['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', f'{W}/uia2.ps1', '-Action', action, '-Value', value])
        time.sleep(3)

    def duplicates():
        d, f = build(SB)
        state['dupes'], state['forensics'] = d, f
        out = ui('tab', Value='Duplicates'); time.sleep(1.5)
        check('Duplicates tab selectable by UI Automation', 'selected' in out, out)
        check('Duplicates shows its empty state', bool(ui('texts', Value='Add folders and click')))
        ui('setnamed', Name='Folder to add', Value=d)
        ui('btnname', Value='Add')
        check('folder is added to the list', '1 folder(s)' in ui('texts', Value='folder'))
        ui('btnname', Value='Find Duplicates')
        ok = wait(lambda: re.search(r'\d+ sets', ui('texts', Value=r'sets|No duplicates')) is not None, timeout=60, step=1)
        summary = ui('texts', Value=r'sets|No duplicates')
        check('scan finds 2 sets with 3 extra copies', ok and '2 sets' in summary and '3 extra copies' in summary, summary)
        rows = [r for r in ui('rows', Index=0).splitlines() if r.strip()]
        check('results list all 5 files of the sets (not the unique one)', len(rows) == 5 and not any('unique.bin' in r for r in rows), ' / '.join(rows)[:300])
        shot('duplicates')

        ui('btnname', Value='Mark the Others'); time.sleep(0.8)
        marked = ui('texts', Value='marked')
        check('keep rule marks 3 copies', '3 marked' in marked, marked)
        shot('duplicates-marked')

        background('btnname', 'Delete Marked...')
        msg = ps('msgbox.ps1', '-Title', 'Delete duplicates')
        check('delete asks first, says Recycle Bin and re-check', 'Recycle Bin' in msg and 're-checked' in msg, msg[:200])
        shot('dialog-duplicates-delete')
        ps('msgbox.ps1', '-Title', 'Delete duplicates', '-Button', 'Yes')
        wait(lambda: 'Moved' in ui('texts', Value='Moved'), timeout=30, step=1)
        status = ui('texts', Value='Moved')
        check('3 copies moved to the Recycle Bin', 'Moved 3 file(s)' in status, status)
        kept = [os.path.exists(os.path.join(d, p)) for p in [r'photos\IMG_1001.jpg', r'docs\report.pdf', r'docs\unique.bin']]
        gone = [not os.path.exists(os.path.join(d, p)) for p in [r'backup\IMG_1001.jpg', r'backup\old\IMG_1001 (copy).jpg', r'archive\report-final.pdf']]
        check('newest copy of each set kept, older copies removed', all(kept) and all(gone), f'kept={kept} gone={gone}')
        check('list is empty once no duplicates remain', not [r for r in ui('rows', Index=0).splitlines() if r.strip()])

    def forensics():
        f = state.get('forensics') or build(SB)[1]
        out = ui('tab', Value='Forensics'); time.sleep(1.5)
        check('Forensics tab selectable by UI Automation', 'selected' in out, out)
        ui('setnamed', Name='File or folder to check', Value=f)

        ui('btnname', Value='Find Hidden Streams')
        ok = wait(lambda: 'streams' in ui('texts', Value=r'\d+ streams'), timeout=60, step=1)
        summary = ui('texts', Value=r'\d+ streams')
        check('finds 3 streams: 1 hidden program, 1 hidden data, 1 download', ok and '3 streams' in summary and '1 hidden program ' in summary and '1 download' in summary, summary)
        rows = ui('rows', Index=0).splitlines()
        check('hidden program is listed first', bool(rows) and rows[0].startswith('Hidden program'), rows[0] if rows else '')
        check('download marker names where it came from', any('example.com' in r for r in rows), ' / '.join(rows)[:300])
        shot('forensics-streams')

        ui('btnname', Value='Select Download Markers'); time.sleep(0.5)
        background('btnname', 'Remove Selected...')
        msg = ps('msgbox.ps1', '-Title', 'Remove hidden streams')
        check('removing streams asks first and explains download markers', 'download markers' in msg and 'not changed' in msg, msg[:200])
        ps('msgbox.ps1', '-Title', 'Remove hidden streams', '-Button', 'Yes')
        wait(lambda: 'Removed' in ui('texts', Value='Removed'), timeout=20, step=1)
        try:
            open(os.path.join(f, 'invoice.pdf:Zone.Identifier'), 'rb').close()
            motw_gone = False
        except OSError:
            motw_gone = True
        with open(os.path.join(f, 'invoice.pdf'), 'rb') as fh:
            content_ok = fh.read(8) == b'%PDF-1.4'
        check('download marker removed on disk, file content untouched', motw_gone and content_ok, f'gone={motw_gone} content={content_ok}')
        check('other streams remain', len([r for r in ui('rows', Index=0).splitlines() if r.strip()]) == 2)

        ui('btnname', Value='Check Contents')
        ok = wait(lambda: 'disguised' in ui('texts', Value='disguised program'), timeout=60, step=1)
        summary = ui('texts', Value='disguised program')
        check('content check: 1 disguised program, 2 likely encrypted, 1 wrong extension',
              ok and '1 disguised program ' in summary and '2 likely encrypted' in summary and '1 wrong extension' in summary, summary)
        rows = ui('rows', Index=0).splitlines()
        check('disguised program listed first', bool(rows) and rows[0].startswith('Disguised program') and 'holiday-photo.jpg' in rows[0], rows[0] if rows else '')
        check('readable files are not reported', not any(n in ' '.join(rows) for n in ['readme.txt', 'notes.txt', 'invoice.pdf']))
        shot('forensics-content')

        ui('tab', Value='Previous versions'); time.sleep(1)
        ui('setnamed', Name='File or folder to check', Value=os.path.join(f, 'readme.txt'))
        ui('btnname', Value='Find Versions')
        wait(lambda: bool(ui('texts', Value='snapshot|earlier version')), timeout=60, step=1)
        note = ui('texts', Value='No snapshots exist|earlier version|None of the')
        check('previous versions explains the result (snapshots or none)', bool(note.strip()), note[:200])
        shot('forensics-versions')

        ui('btnname', Value='Recycle Bin')
        wait(lambda: bool(ui('texts', Value=r'item\(s\)|Recycle Bin is empty')), timeout=60, step=1)
        ui('setnamed', Name='Filter deleted items', Value='IMG_1001'); time.sleep(1)
        rows = [r for r in ui('rows', Index=0).splitlines() if r.strip()]
        check('Recycle Bin lists the duplicates deleted earlier', any(r.startswith('IMG_1001 (copy).jpg') for r in rows), ' / '.join(rows)[:300])
        shot('forensics-recycle-bin')
        d = state.get('dupes')
        if d and any(r.startswith('IMG_1001 (copy).jpg') for r in rows):
            ui('select', Index=0, Value='IMG_1001 (copy).jpg')
            ui('btnname', Value='Restore')
            target = os.path.join(d, 'backup', 'old', 'IMG_1001 (copy).jpg')
            check('restore puts the file back at its original path', wait(lambda: os.path.exists(target), timeout=20), target)
            check('restored file is byte-identical to the kept copy', os.path.exists(target) and open(target, 'rb').read() == open(os.path.join(d, 'photos', 'IMG_1001.jpg'), 'rb').read())

    return [duplicates, forensics]
