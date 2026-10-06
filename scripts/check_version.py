"""Validate component versions and optional release tag."""
import argparse
import json
import re
from project import ROOT, components
SEMVER = re.compile(r'(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)')

def validate(versions, catalog, expected=None):
    if set(versions) != set(catalog):
        raise ValueError('Version keys must exactly match components')
    for key, value in versions.items():
        if not isinstance(value, str) or not SEMVER.fullmatch(value):
            raise ValueError('Invalid stable semver: ' + key)
    if expected is not None:
        matches = [k for k, c in catalog.items() if expected == c['tag_prefix'] + versions[k]]
        if len(matches) != 1:
            raise ValueError('Release tag does not match a component version')
        return matches[0]
    return None

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--expected')
    parser.add_argument('--component', choices=list(components()))
    args = parser.parse_args()
    versions = json.loads((ROOT / 'versions.json').read_text(encoding='utf-8'))
    key = validate(versions, components(), args.expected)
    if args.component:
        if key is not None and key != args.component:
            raise ValueError('Tag belongs to a different component')
        print(versions[args.component])
    else:
        print('Versions OK' + (': ' + key if key else ''))
if __name__ == '__main__':
    main()
