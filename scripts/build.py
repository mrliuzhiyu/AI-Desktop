"""Build a component on the current OS without installing it."""
import argparse
import platform
import subprocess
from project import ROOT, components

def build(key):
    system = platform.system()
    if system == 'Windows':
        subprocess.run(['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', str(ROOT / 'scripts/doctor.ps1')], check=True)
        command = ['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', str(ROOT / key / 'windows/build.ps1')]
    elif system == 'Darwin':
        subprocess.run(['zsh', str(ROOT / 'scripts/doctor.sh')], check=True)
        command = ['zsh', str(ROOT / key / 'build.sh')]
    else:
        raise SystemExit('Only Windows and macOS builds are supported')
    subprocess.run(command, cwd=ROOT, check=True)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('component', choices=[*components(), 'all'])
    args = parser.parse_args()
    for key in components() if args.component == 'all' else [args.component]:
        build(key)
