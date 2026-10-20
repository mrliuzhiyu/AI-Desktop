import sys
import unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'scripts'))
from check_version import validate

class VersionTests(unittest.TestCase):
    catalog = {'a': {'tag_prefix': 'a-v'}, 'b': {'tag_prefix': 'b-v'}}
    def test_valid_tag_selects_component(self):
        self.assertEqual(validate({'a':'1.2.3', 'b':'0.1.0'}, self.catalog, 'b-v0.1.0'), 'b')
    def test_rejects_missing_or_extra_components(self):
        for versions in [{'a':'1.0.0'}, {'a':'1.0.0','b':'1.0.0','c':'1.0.0'}]:
            with self.assertRaises(ValueError): validate(versions, self.catalog)
    def test_rejects_unsafe_or_malformed_versions(self):
        for v in ['', '01.2.3', '1.2', '1.2.3;echo bad', '../1.2.3', 123, None]:
            with self.subTest(version=v), self.assertRaises(ValueError):
                validate({'a':v,'b':'1.0.0'}, self.catalog)
    def test_rejects_wrong_tag_or_version(self):
        for tag in ['v1.2.3', 'a-v1.2.4', 'other-v1.2.3', 'a-v1.2.3' + chr(10)]:
            with self.subTest(tag=tag), self.assertRaises(ValueError):
                validate({'a':'1.2.3','b':'1.0.0'}, self.catalog, tag)
