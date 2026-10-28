"""Validate governed Markdown headers and index coverage."""
import datetime
import re
from project import ROOT

def check():
    docs = ROOT / 'docs'
    index = (docs / 'README.md').read_text(encoding='utf-8')
    for path in docs.glob('*.md'):
        if path.name == 'README.md':
            continue
        text = path.read_text(encoding='utf-8')
        match = re.match(r'^---\n(.*?)\n---\n', text, re.S)
        if not match:
            raise ValueError('Missing document metadata: ' + path.name)
        fields = dict(re.findall(r'^(\w+):\s*(.+)$', match[1], re.M))
        if fields.get('status') not in {'active', 'authoritative', 'draft', 'historical', 'superseded'}:
            raise ValueError('Invalid document status: ' + path.name)
        if fields.get('type') not in {'spec', 'guide', 'progress', 'research', 'roadmap', 'reference'}:
            raise ValueError('Invalid document type: ' + path.name)
        verified = datetime.date.fromisoformat(fields.get('verified', ''))
        if verified > datetime.date.today():
            raise ValueError('Future verification date')
        if fields['status'] == 'superseded' and not (docs / fields.get('superseded_by', '__missing__')).is_file():
            raise ValueError('Missing superseding document')
        if '](' + path.name + ')' not in index:
            raise ValueError('Document missing from index: ' + path.name)
    for link in re.findall(r'\]\(([^)]+)\)', index):
        if '://' not in link and not (docs / link.split('#')[0]).is_file():
            raise ValueError('Broken index link: ' + link)
    print('Documentation metadata and index OK')

if __name__ == '__main__':
    check()
