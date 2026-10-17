"""Parse all checked-in zsh scripts without executing installers."""
import shutil
import subprocess
from project import ROOT

def main():
    zsh = shutil.which('zsh')
    if not zsh:
        raise SystemExit('zsh is required for shell syntax validation (run on macOS CI)')
    paths = list((ROOT / 'scripts').glob('*.sh'))
    for key in ['claude', 'codex']:
        paths.extend((ROOT / key).glob('*.sh'))
    for path in paths:
        subprocess.run([zsh, '-n', str(path)], check=True)
    print('Shell syntax OK: ' + str(len(paths)))
if __name__ == '__main__':
    main()
