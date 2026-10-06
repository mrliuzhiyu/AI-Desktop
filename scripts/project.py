"""Shared repository metadata. Standard library only (Python 3.11+)."""
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]

def components():
    return json.loads((ROOT / 'components.json').read_text(encoding='utf-8'))

def component(key):
    return components()[key]
