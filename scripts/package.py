"""Build and validate a component archive; never publish or install."""
import argparse
import json
import platform
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
    if target != 'windows' or platform.system() != 'Windows':
        raise ValueError('This packager currently requires Windows')
    catalog = components()
    versions = json.loads((ROOT / 'versions.json').read_text(encoding='utf-8'))
    validate(versions, catalog)
    name = catalog[key]['name']
    if not skip_build:
        build(key)
    source = ROOT / key / 'windows/build'
    subprocess.run(['powershell','-NoProfile','-ExecutionPolicy','Bypass','-File',str(ROOT/'scripts/verify_windows.ps1'),'-Component',key,'-Directory',str(source)],check=True)
    for kind in ['intro','loop']:
        inspect(source/'assets'/(kind+'.mp4'),windows=True,intro=kind=='intro')
    output = ROOT / 'dist'
    output.mkdir(exist_ok=True)
    archive = output / (name + '-' + versions[key] + '-windows.zip')
    with tempfile.TemporaryDirectory(prefix='package-') as tmp:
        folder = Path(tmp) / (name + '-Windows')
        folder.mkdir()
        shutil.copy2(source/(name+'.exe'), folder)
        shutil.copytree(source/'assets',folder/'assets')
        shutil.copy2(ROOT/key/'windows/uninstall.ps1',folder)
        template = (ROOT/'scripts/install-windows-package.ps1').read_text(encoding='utf-8-sig')
        (folder/'install.ps1').write_text(template.replace('__APP_NAME__',name),encoding='utf-8-sig')
        shutil.copy2(ROOT/'LICENSE',folder/'LICENSE')
        shutil.copy2(ROOT/key/'assets/LICENSE.md',folder/'ASSETS-LICENSE.md')
        shutil.copy2(ROOT/key/'CREDITS.md',folder/'CREDITS.md')
        temporary = Path(tmp)/'package.zip'
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
    p.add_argument('--platform',choices=['windows'],default='windows')
    p.add_argument('--skip-build',action='store_true',help='Validate and package an existing local build')
    a=p.parse_args()
    package(a.component,a.platform,a.skip_build)
