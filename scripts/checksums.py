"""Write SHA-256 sidecars for ZIP artifacts."""
import argparse
import hashlib
from pathlib import Path

def checksum(path):
    path=Path(path)
    digest=hashlib.file_digest(path.open('rb'),'sha256').hexdigest()
    output=path.with_suffix(path.suffix+'.sha256')
    output.write_text(digest+'  '+path.name+'\n',encoding='utf-8')
    return output
if __name__ == '__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('directory',type=Path)
    a=p.parse_args()
    files=sorted(a.directory.glob('*.zip'))
    if not files: raise SystemExit('No archives found')
    for path in files: print(checksum(path).name)
