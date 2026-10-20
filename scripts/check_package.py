"""Check ZIP layout, required licenses and path safety."""
import argparse
import zipfile
from pathlib import PurePosixPath
from project import components

def check(path, key, platform):
    name = components()[key]['name']
    folder = name + ('-Windows' if platform == 'windows' else '')
    common = ['LICENSE', 'ASSETS-LICENSE.md', 'CREDITS.md']
    if platform == 'windows':
        required = common + [name + '.exe', 'install.ps1', 'uninstall.ps1', 'assets/intro.mp4', 'assets/loop.mp4', 'assets/LibreBodoni-Italic.ttf', 'assets/LibreBodoni-OFL.txt']
    else:
        required = common + ['install.sh', 'uninstall.sh', name + '.app/Contents/Info.plist', name + '.app/Contents/MacOS/' + name, name + '.app/Contents/Resources/intro.mp4', name + '.app/Contents/Resources/loop.mp4']
    with zipfile.ZipFile(path) as archive:
        names = archive.namelist()
        if len(names) != len(set(names)):
            raise ValueError('Duplicate ZIP paths')
        for member in names:
            parts = PurePosixPath(member).parts
            if not parts or parts[0] != folder or '..' in parts or ':' in member or '\\' in member or member.startswith('/'):
                raise ValueError('Unsafe ZIP path')
        for rel in required:
            member = folder + '/' + rel
            if member not in names or archive.getinfo(member).file_size <= 0:
                raise ValueError('Missing or empty package member: ' + rel)
        bad = archive.testzip()
        if bad:
            raise ValueError('ZIP checksum failure')
    return folder

if __name__ == '__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('component', choices=list(components()))
    p.add_argument('platform', choices=['windows','macos'])
    p.add_argument('archive')
    a=p.parse_args()
    print('Package OK: ' + check(a.archive,a.component,a.platform))
