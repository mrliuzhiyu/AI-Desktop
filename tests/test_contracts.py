import json
import sys
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'scripts'))
from project import components

class ComponentContractTests(unittest.TestCase):
    def test_unique_names_and_identifiers(self):
        catalog = components()
        for field in ['name', 'id', 'tag_prefix']:
            self.assertEqual(len({x[field] for x in catalog.values()}), len(catalog))

    def test_identifier_consistency(self):
        for key, info in components().items():
            for file in ['build.sh', 'install.sh', 'uninstall.sh', 'Sources/main.swift', 'windows/src/' + info['name'] + '.cs']:
                with self.subTest(component=key, file=file):
                    text = (ROOT / key / file).read_text(encoding='utf-8-sig')
                    self.assertIn(info['id'], text)
                    for other, value in components().items():
                        if other != key:
                            self.assertNotIn(value['id'], text)

    def test_required_source_assets(self):
        for key in components():
            for file in ['assets/intro.mp4', 'assets/loop.mp4', 'assets/LICENSE.md', 'CREDITS.md', 'windows/assets/LibreBodoni-Italic.ttf', 'windows/assets/LibreBodoni-OFL.txt']:
                self.assertGreater((ROOT / key / file).stat().st_size, 0)

    def test_version_injection_wiring(self):
        for key in components():
            shell = (ROOT / key / 'build.sh').read_text(encoding='utf-8')
            windows = (ROOT / key / 'windows/build.ps1').read_text(encoding='utf-8-sig')
            self.assertIn('check_version.py --component ' + key, shell)
            self.assertIn('<key>CFBundleShortVersionString</key><string>$VERSION</string>', shell)
            self.assertIn('$versions.' + key, windows)
            self.assertIn('AssemblyFileVersion', windows)

if __name__ == '__main__':
    unittest.main()
