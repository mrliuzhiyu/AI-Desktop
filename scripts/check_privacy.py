"""Scan tracked files or reachable Git blobs; print locations, never matched values."""
import argparse
import re
import subprocess
from project import ROOT

PATTERNS = {
    'credential': re.compile(rb'(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}|sk-[A-Za-z0-9_-]{30,}|-----BEGIN (?:RSA |OPENSSH |EC )?PRIVATE KEY-----)'),
    'personal-path': re.compile(rb'(?:[A-Za-z]:[\\/]+Users[\\/]+[^\s"<>]+|/Us' + rb'ers/[^/\s"<>]+|/ho' + rb'me/[^/\s"<>]+)'),
    'private-email': re.compile(rb'[A-Za-z0-9._%+-]+@(?:qq\.com|gmail\.com|163\.com|outlook\.com|hotmail\.com)', re.I),
    'legacy-account': re.compile(rb'(?:wynne' + rb'celes|celeste' + rb'wynne0506-pixel|io\.github\.celeste\.)', re.I),
}

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def scan(history=False):
    failures = []
    count = 0
    if history:
        records = []
        for row in git('rev-list', '--objects', '--all').decode().splitlines():
            oid, _, path = row.partition(' ')
            if git('cat-file', '-t', oid).strip() == b'blob':
                records.append((path, oid))
    else:
        records = [(p, None) for p in git('ls-files', '-z').decode().split('\0') if p]
    for path, oid in records:
        data = git('cat-file', 'blob', oid) if oid else (ROOT / path).read_bytes()
        count += 1
        for name, pattern in PATTERNS.items():
            if pattern.search(data):
                failures.append((path, name))
    if failures:
        for path, name in sorted(set(failures)):
            print(name + ': ' + path)
        raise SystemExit('Privacy scan failed; inspect locally before publishing')
    print('Privacy pattern scan OK: ' + str(count) + (' historical blobs' if history else ' tracked files'))

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--history', action='store_true')
    scan(parser.parse_args().history)
