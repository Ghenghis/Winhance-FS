"""Talks to Winhance-FS-MCP.exe over stdio exactly like an MCP client and checks every tool.

usage: python mcp_smoke.py [path-to-Winhance-FS-MCP.exe]
Works only in C:/Temp/fmtest/mcp and a temporary index file (WINHANCE_FS_INDEX); the user's real index is untouched.
"""
import json
import os
import random
import shutil
import subprocess
import sys
import tempfile
import time

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
EXE = sys.argv[1] if len(sys.argv) > 1 else os.path.join(REPO, 'src', 'Winhance.Mcp', 'bin', 'Debug', 'net9.0-windows', 'Winhance-FS-MCP.exe')
SB = r'C:\Temp\fmtest\mcp'
results = []


def check(name, ok, detail=''):
    results.append(bool(ok))
    print(f"{'PASS' if ok else 'FAIL'}  {name}" + (f'  [{str(detail)[:300]}]' if detail and not ok else ''), flush=True)


def fixture():
    shutil.rmtree(SB, ignore_errors=True)
    photo = random.Random(1).randbytes(3_000_000)
    files = {
        r'Pictures\beach.jpg': photo,
        r'Backup\Pictures\beach (copy).jpg': photo,
        r'Videos\holiday.mp4': random.Random(2).randbytes(5_000_000),
        r'Documents\Tax Return 2024.pdf': b'%PDF-1.4\n' + b'x' * 5000,
        r'Documents\thesis.docx': random.Random(3).randbytes(200_000),
        r'Documents\notes.txt': b'plain text ' * 100,
    }
    for rel, data in files.items():
        p = os.path.join(SB, rel)
        os.makedirs(os.path.dirname(p), exist_ok=True)
        with open(p, 'wb') as fh:
            fh.write(data)
    with open(os.path.join(SB, r'Documents\Tax Return 2024.pdf:Zone.Identifier'), 'wb') as fh:
        fh.write(b'[ZoneTransfer]\r\nZoneId=3\r\nHostUrl=https://example.com/tax.pdf\r\n')


class Client:
    def __init__(self, env):
        self.p = subprocess.Popen([EXE], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=env)
        self.next_id = 1

    def send(self, msg):
        self.p.stdin.write((json.dumps(msg) + '\n').encode())
        self.p.stdin.flush()

    def request(self, method, params=None):
        rid = self.next_id
        self.next_id += 1
        self.send({'jsonrpc': '2.0', 'id': rid, 'method': method, 'params': params or {}})
        while True:
            line = self.p.stdout.readline()
            if not line:
                raise RuntimeError('server closed: ' + self.p.stderr.read().decode(errors='replace')[:500])
            msg = json.loads(line)
            if msg.get('id') == rid:
                return msg

    def tool(self, name, **args):
        r = self.request('tools/call', {'name': name, 'arguments': args})
        if 'error' in r:
            return {'rpcError': r['error']}
        text = r['result']['content'][0]['text']
        try:
            return json.loads(text)
        except json.JSONDecodeError:
            return {'text': text, 'isError': r['result'].get('isError')}


def main():
    fixture()
    tmp = tempfile.mkdtemp(prefix='wfs-mcp-')
    env = dict(os.environ, WINHANCE_FS_INDEX=os.path.join(tmp, 'index.bin'))

    out = subprocess.run([EXE, '--version'], capture_output=True, text=True, timeout=30).stdout
    check('--version prints the version', 'Winhance-FS MCP Server 1.5.0' in out, out)
    out = subprocess.run([EXE, '--help'], capture_output=True, text=True, timeout=30).stdout
    check('--help shows a ready-to-paste client config', '"mcpServers"' in out and 'winhance-fs' in out and 'claude mcp add' in out, out[:200])

    c = Client(env)
    init = c.request('initialize', {'protocolVersion': '2025-06-18', 'capabilities': {}, 'clientInfo': {'name': 'smoke', 'version': '1'}})
    info = init.get('result', {}).get('serverInfo', {})
    check('initialize: server identifies as winhance-fs 1.5.0', info.get('name') == 'winhance-fs' and info.get('version') == '1.5.0', init)
    check('initialize: server sends usage instructions', 'read-only' in init['result'].get('instructions', ''))
    c.send({'jsonrpc': '2.0', 'method': 'notifications/initialized'})

    tools = {t['name']: t for t in c.request('tools/list')['result']['tools']}
    expected = {'search_files', 'largest_files', 'search_index_status', 'build_search_index', 'find_duplicates',
                'hidden_streams', 'check_file_contents', 'previous_versions', 'recycle_bin'}
    check('tools/list has all 9 tools', set(tools) == expected, sorted(tools))
    ro = [n for n, t in tools.items() if n != 'build_search_index' and not t.get('annotations', {}).get('readOnlyHint')]
    check('every tool except build_search_index is marked read-only', not ro, ro)
    check('no tool is marked destructive', not any(t.get('annotations', {}).get('destructiveHint') for t in tools.values()))
    check('search_files documents the query rules', 'ext:pdf' in tools['search_files']['description'])

    r = c.tool('search_files', query='beach')
    check('search before any index explains how to build one', 'No search index' in r.get('error', '') and 'Build Index' in r.get('hint', ''), r)

    r = c.tool('build_search_index', roots=[SB])
    check('build_search_index indexes the folder', r.get('files') == 6 and r.get('folders') == 5, r)
    check('index written to the WINHANCE_FS_INDEX path only', os.path.exists(env['WINHANCE_FS_INDEX']))

    r = c.tool('search_files', query='beach')
    names = [os.path.basename(x['path']) for x in r.get('results', [])]
    check("search_files 'beach' finds both pictures, best match first", names == ['beach.jpg', 'beach (copy).jpg'], r)
    check('results carry size and date', r['results'][0].get('size') == '2.86 MB' and r['results'][0].get('modified'), r['results'][0])
    r = c.tool('search_files', query='tax 2024 ext:pdf')
    check('search_files with words and ext:', r.get('totalMatches') == 1, r)
    r = c.tool('search_files', query='', sort='modified', limit=3)
    check("empty query sorted by 'modified' lists the newest items", r.get('shown') == 3 and r.get('totalMatches') == 11, r)
    r = c.tool('search_files', query='')
    check('empty query by relevance is refused with a hint', 'error' in r, r)

    r = c.tool('largest_files', count=2)
    names = [os.path.basename(x['path']) for x in r.get('files', [])]
    check('largest_files: the 5 MB video first', names[:1] == ['holiday.mp4'] and len(names) == 2, r)
    r = c.tool('largest_files', folder=os.path.join(SB, 'Documents'), count=5)
    names = [os.path.basename(x['path']) for x in r.get('files', [])]
    check('largest_files in one folder stays in that folder', names == ['thesis.docx', 'Tax Return 2024.pdf', 'notes.txt'], r)
    r = c.tool('largest_files', extensions='jpg', count=5)
    check('largest_files by type', r.get('filesMatched') == 2, r)
    r = c.tool('largest_files', folder=r'C:\Windows')
    check('largest_files outside the index says so', 'not in the search index' in r.get('error', ''), r)

    r = c.tool('search_index_status')
    check('search_index_status reports the root and counts', r.get('files') == 6 and r['roots'][0]['path'] == SB and r.get('engine') == '1.5.0', r)

    r = c.tool('find_duplicates', folders=[SB])
    check('find_duplicates finds the copied picture', r.get('duplicateSets') == 1 and r.get('wastedSpace') == '2.86 MB', r)
    check('find_duplicates lists both copies', len(r['sets'][0]['files']) == 2 if r.get('sets') else False, r)

    r = c.tool('hidden_streams', path=SB)
    check('hidden_streams finds the download marker with its site', r.get('downloadMarkers') == 1 and 'example.com' in json.dumps(r), r)

    r = c.tool('check_file_contents', path=os.path.join(SB, 'Documents'))
    check('check_file_contents flags the random .docx as likely encrypted',
          r.get('suspicious') == 1 and r['files'][0]['verdict'] == 'likely encrypted' and r['files'][0]['path'].endswith('thesis.docx'), r)

    r = c.tool('previous_versions', path=os.path.join(SB, r'Documents\notes.txt'))
    check('previous_versions answers (snapshots or none)', 'versions' in r or 'error' in r, r)
    r = c.tool('recycle_bin', limit=3)
    check('recycle_bin answers with totals', 'total' in r and 'items' in r, r)
    r = c.tool('hidden_streams', path=r'C:\no\such\folder')
    check('bad paths give a clear error', r.get('error', '').startswith('Not found'), r)

    c.p.stdin.close()
    c.p.wait(timeout=20)
    check('server exits cleanly when the client disconnects', c.p.returncode == 0, c.p.returncode)
    check('index file is not the shared app index', not env['WINHANCE_FS_INDEX'].lower().startswith(os.path.join(os.environ['LOCALAPPDATA'], 'winhance-fs').lower()))
    shutil.rmtree(tmp, ignore_errors=True)
    shutil.rmtree(SB, ignore_errors=True)
    print(f'\n== mcp: {sum(results)}/{len(results)} passed')
    return 0 if all(results) else 1


if __name__ == '__main__':
    t = time.time()
    code = main()
    print(f'{time.time() - t:.1f}s')
    sys.exit(code)
