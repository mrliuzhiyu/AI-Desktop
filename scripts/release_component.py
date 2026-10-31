"""Select a component from a validated release tag, with optional Actions output."""
import json
import os
from check_version import validate
from project import ROOT, components
if __name__ == '__main__':
    tag=os.environ.get('RELEASE_TAG','')
    versions=json.loads((ROOT/'versions.json').read_text(encoding='utf-8'))
    key=validate(versions,components(),tag)
    if not key: raise SystemExit('Release tag is required')
    if os.environ.get('GITHUB_OUTPUT'):
        with open(os.environ['GITHUB_OUTPUT'],'a',encoding='utf-8') as f:
            f.write('component='+key+'\n')
    print('Validated release component: '+key)
