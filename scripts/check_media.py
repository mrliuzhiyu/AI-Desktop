"""Validate media streams before packaging."""
import argparse
import json
import subprocess
from pathlib import Path
from project import ROOT, components

def inspect(path, windows=False, intro=False):
    data = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-show_streams', '-show_format', '-of', 'json', str(path)]))
    video = [s for s in data['streams'] if s['codec_type'] == 'video']
    audio = [s for s in data['streams'] if s['codec_type'] == 'audio']
    if len(video) != 1 or float(data['format'].get('duration', 0)) <= 0:
        raise ValueError('Expected one playable video: ' + str(path))
    if video[0]['width'] <= 0 or video[0]['height'] <= 0:
        raise ValueError('Invalid frame dimensions')
    if intro and not audio:
        raise ValueError('Intro audio missing')
    if windows and (video[0]['codec_name'] != 'h264' or video[0]['pix_fmt'] != 'yuv420p'):
        raise ValueError('Windows requires H.264 yuv420p')
    if windows and any(s['codec_name'] != 'aac' for s in audio):
        raise ValueError('Windows audio must be AAC')
    if windows and not intro and audio:
        raise ValueError('Loop must be silent')
    return data

if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('component', choices=list(components()))
    p.add_argument('--directory', type=Path)
    p.add_argument('--windows', action='store_true')
    a = p.parse_args()
    for name in ['intro', 'loop']:
        inspect((a.directory or ROOT / a.component / 'assets') / (name + '.mp4'), a.windows, name == 'intro')
    print('Media OK: ' + a.component)
