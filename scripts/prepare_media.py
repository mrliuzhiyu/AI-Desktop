"""Prepare Windows media in a build directory, never in source assets."""
import argparse
import os
import subprocess
import tempfile
from pathlib import Path
from check_media import inspect
from project import ROOT, components

def prepare(key, destination):
    destination = Path(destination).resolve()
    source = ROOT / key / 'assets'
    if destination == source.resolve():
        raise ValueError('Refusing to overwrite original media')
    destination.mkdir(parents=True, exist_ok=True)
    for name in ['intro', 'loop']:
        src = source / (name + '.mp4')
        inspect(src, intro=name == 'intro')
        fd, temporary = tempfile.mkstemp(suffix='.mp4', dir=destination)
        os.close(fd)
        try:
            command = ['ffmpeg', '-nostdin', '-v', 'error', '-y', '-i', str(src), '-map', '0:v:0', '-c:v', 'libx264', '-preset', 'fast', '-crf', '20', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', '-threads', '4']
            command += ['-map', '0:a:0', '-c:a', 'aac', '-b:a', '160k'] if name == 'intro' else ['-an']
            subprocess.run([*command, temporary], check=True)
            inspect(temporary, windows=True, intro=name == 'intro')
            os.replace(temporary, destination / (name + '.mp4'))
        finally:
            Path(temporary).unlink(missing_ok=True)

if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('component', choices=list(components()))
    p.add_argument('destination', type=Path)
    a = p.parse_args()
    prepare(a.component, a.destination)
