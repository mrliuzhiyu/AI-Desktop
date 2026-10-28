"""Portable check entry; platform-specific checks are reported explicitly."""
import platform
import shutil
import subprocess
import sys
from project import ROOT

def run(*args):
    subprocess.run(args,cwd=ROOT,check=True)

def main():
    run(sys.executable,'scripts/check_version.py')
    run(sys.executable,'scripts/check_docs.py')
    run(sys.executable,'-m','unittest','discover','-s','tests','-v')
    for key in ['claude','codex']:
        run(sys.executable,'scripts/check_media.py',key)
    if platform.system() == 'Windows':
        run('powershell','-NoProfile','-ExecutionPolicy','Bypass','-File','scripts/check_powershell.ps1')
    else:
        print('SKIP: Windows PowerShell parser; covered by Windows CI')
    if shutil.which('zsh'):
        run(sys.executable,'scripts/check_shell.py')
    else:
        print('SKIP: zsh syntax; covered by macOS CI')
    run('git','diff','--check')
    print('Available local checks passed; SKIP items are not validated.')

if __name__ == '__main__':
    main()
