"""Build and validate an archive without publishing or installing."""
import argparse
import json
import platform
import plistlib
import shutil
import subprocess
import tempfile
import zipfile
from pathlib import Path
from project import ROOT, components
from build import build
from check_media import inspect
from check_package import check
from check_version import validate

def package(key, target, skip_build=False):
    expected_os = {'windows':'Windows','macos':'Darwin'}
    if platform.system() != expected_os[target]:
        raise ValueError('Build this package on ' + expected_os[target])
    catalog = components()
    versions = json.loads((ROOT / 'versions.json').read_text(encoding='utf-8'))
    validate(versions,catalog)
    name = catalog[key]['name']
    if not skip_build:
        build(key)
    source = ROOT / key / ('windows/build' if target == 'windows' else 'build')
    output = ROOT / 'dist'
    output.mkdir(exist_ok=True)
    archive = output / (name + '-' + versions[key] + '-' + target + '.zip')
    with tempfile.TemporaryDirectory(prefix='package-') as tmp:
        folder = Path(tmp) / (name + ('-Windows' if target == 'windows' else ''))
        folder.mkdir()
        if target == 'windows':
            subprocess.run(['powershell','-NoProfile','-ExecutionPolicy','Bypass','-File',str(ROOT/'scripts/verify_windows.ps1'),'-Component',key,'-Directory',str(source)],check=True)
            for kind in ['intro','loop']:
                inspect(source/'assets'/(kind+'.mp4'),windows=True,intro=kind=='intro')
            shutil.copy2(source/(name+'.exe'),folder)
            shutil.copytree(source/'assets',folder/'assets')
            shutil.copy2(ROOT/key/'windows/uninstall.ps1',folder)
            template = (ROOT/'scripts/install-windows-package.ps1').read_text(encoding='utf-8-sig')
            (folder/'install.ps1').write_text(template.replace('__APP_NAME__',name),encoding='utf-8-sig')
        else:
            app = source/(name+'.app')
            info = plistlib.loads((app/'Contents/Info.plist').read_bytes())
            if info.get('CFBundleShortVersionString') != versions[key] or info.get('CFBundleIdentifier') != catalog[key]['id']:
                raise ValueError('App metadata differs from component manifest')
            subprocess.run(['lipo',str(app/'Contents/MacOS'/name),'-verify_arch','arm64','x86_64'],check=True)
            subprocess.run(['codesign','--verify','--deep','--strict',str(app)],check=True)
            shutil.copytree(app,folder/app.name,symlinks=True)
            for file in ['install.sh','uninstall.sh']:
                shutil.copy2(ROOT/key/file,folder/file)
        shutil.copy2(ROOT/'LICENSE',folder/'LICENSE')
        shutil.copy2(ROOT/key/'assets/LICENSE.md',folder/'ASSETS-LICENSE.md')
        shutil.copy2(ROOT/key/'CREDITS.md',folder/'CREDITS.md')
        temporary = Path(tmp)/'package.zip'
        if target == 'macos':
            subprocess.run(['ditto','-c','-k','--norsrc','--noextattr','--keepParent',str(folder),str(temporary)],check=True)
        else:
            with zipfile.ZipFile(temporary,'w',zipfile.ZIP_DEFLATED) as z:
                for file in sorted(folder.rglob('*')):
                    if file.is_file(): z.write(file,file.relative_to(Path(tmp)).as_posix())
        check(temporary,key,target)
        shutil.copy2(temporary,archive)
    print('Package verified: ' + archive.name)
    return archive

if __name__ == '__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('component',choices=list(components()))
    p.add_argument('--platform',choices=['windows','macos'],required=True)
    p.add_argument('--skip-build',action='store_true',help='Validate and package an existing local build')
    a=p.parse_args()
    package(a.component,a.platform,a.skip_build)
