import sys
import tempfile
import unittest
import zipfile
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'scripts'))
from check_package import check

class PackageTests(unittest.TestCase):
    def test_rejects_missing_files(self):
        with tempfile.TemporaryDirectory() as tmp:
            path=Path(tmp)/'bad.zip'
            with zipfile.ZipFile(path,'w') as z: z.writestr('ClaudeBootAnimation-Windows/LICENSE','license')
            with self.assertRaisesRegex(ValueError,'Missing'): check(path,'claude','windows')
    def test_rejects_traversal(self):
        with tempfile.TemporaryDirectory() as tmp:
            path=Path(tmp)/'bad.zip'
            with zipfile.ZipFile(path,'w') as z: z.writestr('ClaudeBootAnimation-Windows/../escape','bad')
            with self.assertRaisesRegex(ValueError,'Unsafe'): check(path,'claude','windows')
