"""Extended checks used from sweep 2 onward (imported by e2e.py)."""
import os, time, re


def install(e2e):
    ui, ps, check, ls, wait, rows, shot, set_panes, write, SB = (
        e2e.ui, e2e.ps, e2e.check, e2e.ls, e2e.wait, e2e.rows, e2e.shot, e2e.set_panes, e2e.write, e2e.SB)

    def browser_extras():
        L, R = os.path.join(SB, 'left'), os.path.join(SB, 'right')
        ui('clicktext', Value='File Browser'); time.sleep(1)
        set_panes(L, R)
        # file created outside the app appears after Ctrl+R
        write(os.path.join(L, 'external-new.txt'), 'made outside')
        ui('select', Index=0, Value='sub'); ui('keys', Value='^r')
        check('Ctrl+R refreshes the active pane', wait(lambda: 'external-new.txt' in rows(0), 5))
        # copy onto an existing name with F5 -> unique name, original untouched
        before = open(os.path.join(R, 'IMG_0002.jpg'), encoding='utf-8').read() if os.path.exists(os.path.join(R, 'IMG_0002.jpg')) else None
        ui('select', Index=0, Value='IMG_0002.jpg'); ui('keys', Value='{F5}')
        check('F5 onto an existing name creates "name (1)"', wait(lambda: 'IMG_0002 (1).jpg' in ls(R)), ','.join(ls(R)))
        # drive button navigates the pane to the drive root, path box updates
        ui('btnname', Value='C:\\', Index=0); time.sleep(1.5)
        edits = ui('edits')
        check('drive button navigates to drive root', 'C:\\' in edits.splitlines()[1] if len(edits.splitlines()) > 1 else False, edits)
        set_panes(L, R)
        # typing a path and pressing Enter navigates
        ui('setedit', Index=1, Value=os.path.join(L, 'sub')); ui('keys', Value='{ENTER}'); time.sleep(1.2)
        check('Enter in the path box navigates', 'gamma.txt' in rows(0), ','.join(rows(0)))
        set_panes(L, R)
        # centre toolbar buttons are visible and use the same commands
        ui('select', Index=0, Value='beta.txt')
        out = ui('btnhelp', Value='Copy to other pane (F5)')
        check('toolbar Copy button works', 'invoked' in out and wait(lambda: any(n.startswith('beta') for n in ls(R))), out)
        status = ui('texts', Value='Copied')
        check('status bar reports the copy', 'Copied' in status, status)
        # move with both panes on the same folder is refused with a message, nothing changes
        set_panes(L, L)
        ui('select', Index=0, Value='alpha.txt'); ui('keys', Value='{F6}'); time.sleep(1.2)
        status = ui('texts', Value='same folder')
        check('F6 with both panes on one folder is refused', 'same folder' in status and 'alpha.txt' in ls(L), status)
        set_panes(L, R)
        ui('select', Index=0, Value='Résumé café.txt'); ui('keys', Value='{F6}')
        check('F6 moves a unicode-named file and reports it', wait(lambda: 'Résumé café.txt' in ls(R)) and 'Moved' in ui('texts', Value='Moved'))

    def batch_rename_rules():
        D = os.path.join(SB, 'rename')
        ui('clicktext', Value='Batch Rename'); time.sleep(1.2)
        ui('setedit', Index=1, Value=D); ui('setedit', Index=2, Value='*.jpg')

        def preview():
            e2e.ps('uia.ps1', '-Action', 'focus', '-Name', 'Add Files...'); time.sleep(1.2)
            return [r.split(' | ')[-1] for r in ui('rows', Index=0).splitlines() if r.strip()]

        def fresh(button):
            ui('btnname', Value='Clear All'); time.sleep(0.3)
            ui('btnname', Value=button); time.sleep(0.6)

        fresh('+ Add Text'); ui('setedit', Index=3, Value='Trip_')
        p = preview(); check('Add Text (prefix) previews', p and p[0] == 'Trip_IMG_0001.jpg', ','.join(p))
        ui('combo', Index=0, Value='Suffix'); p = preview()
        check('Add Text position Suffix previews', p and p[0] == 'IMG_0001Trip_.jpg', ','.join(p))

        fresh('+ Remove Text'); ui('setedit', Index=3, Value=r'\d'); p = preview()
        check('Remove Text (regex) previews', p and p[0] == 'IMG_.jpg', ','.join(p))
        check('duplicate results are flagged as conflicts', 'conflict' in ui('texts', Value='onflict').lower(), ui('texts', Value='onflict'))

        fresh('+ Counter'); ui('setedit', Index=3, Value='10'); ui('setedit', Index=4, Value='5'); ui('setedit', Index=5, Value='2'); ui('setedit', Index=6, Value='-')
        p = preview(); check('Counter start/step/digits/separator preview', p[:2] == ['IMG_0001-10.jpg', 'IMG_0002-15.jpg'], ','.join(p))

        fresh('+ Case'); ui('combo', Index=0, Value='LowerCase'); p = preview()
        check('Change Case lower previews', p and p[0] == 'img_0001.jpg', ','.join(p))

        fresh('+ Extension'); ui('setedit', Index=3, Value='png'); p = preview()
        check('Change Extension previews', p and p[0] == 'IMG_0001.png', ','.join(p))

        fresh('+ Date'); ui('setedit', Index=3, Value='yyyy'); p = preview()
        year = time.strftime('%Y')
        check('Date (file modified, prefix) previews', p and p[0] == f'{year}_IMG_0001.jpg', ','.join(p))

        fresh('+ Regex'); ui('setedit', Index=3, Value=r'IMG_(\d+)'); ui('setedit', Index=4, Value=r'Photo-$1'); p = preview()
        check('Regex rule previews', p and p[0] == 'Photo-0001.jpg', ','.join(p))
        shot('batch-regex')

        # apply a two-rule chain and undo
        fresh('+ Find/Replace'); ui('setedit', Index=3, Value='IMG_'); ui('setedit', Index=4, Value='Pic_')
        ui('btnname', Value='+ Counter'); time.sleep(0.6); preview()
        ui('btnname', Value='Apply Rename'); time.sleep(2)
        names = sorted(n for n in ls(D) if n.endswith('.jpg'))
        check('two-rule chain applies', names == ['Pic_0001_001.jpg', 'Pic_0002_002.jpg', 'Pic_0003_003.jpg', 'Pic_0004_004.jpg'], ','.join(names))
        ui('btnname', Value='Undo Last Batch'); time.sleep(2)
        check('chain undo restores', sorted(n for n in ls(D) if n.endswith('.jpg')) == ['IMG_0001.jpg', 'IMG_0002.jpg', 'IMG_0003.jpg', 'IMG_0004.jpg'], ','.join(ls(D)))
        ui('btnname', Value='Clear All')

    def organizer_strategies():
        D = os.path.join(SB, 'organize')
        ui('clicktext', Value='Smart Organizer'); time.sleep(1.2)
        ui('setedit', Index=1, Value=D)
        for label in ['By Date', 'By Size', 'By Project', 'AI Category', 'By File Type']:
            ps('uia.ps1', '-Action', 'click', '-Name', label); time.sleep(0.4)
            ui('btnname', Value='Analyze'); time.sleep(2)
            t = ui('texts', Value='Found|Error|No ')
            check(f'organizer strategy "{label}" analyzes', re.search(r'Found 9 files in [1-9]\d* categor', t) is not None, t.replace('\n', ' | '))
        shot('organizer-strategies')

    return [browser_extras, batch_rename_rules, organizer_strategies]
