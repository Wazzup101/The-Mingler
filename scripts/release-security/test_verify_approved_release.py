import hashlib
import importlib.util
import tempfile
import unittest
from pathlib import Path

spec = importlib.util.spec_from_file_location("release_verifier", Path(__file__).with_name("verify-approved-release.py"))
verifier = importlib.util.module_from_spec(spec)
spec.loader.exec_module(verifier)


class ReleaseSecurityTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        content = b"synthetic-not-an-executable"
        (self.root / "fixture.tar.gz").write_bytes(content)
        self.manifest = {"repository": "fixture/repo", "releaseTag": "fixture", "assets": {
            "fixture.tar.gz": {"bytes": len(content), "sha256": hashlib.sha256(content).hexdigest()}}}

    def test_valid_bytes_do_not_claim_build_provenance(self):
        claim = verifier.verify(self.root, self.manifest)
        self.assertFalse(claim["builtInThisWorkflow"])
        self.assertFalse(claim["nativeDeviceTestingClaimed"])

    def test_same_size_tampering_is_rejected(self):
        target = self.root / "fixture.tar.gz"
        target.write_bytes(b"x" * target.stat().st_size)
        with self.assertRaises(ValueError): verifier.verify(self.root, self.manifest)

    def test_missing_and_additional_assets_are_rejected(self):
        (self.root / "extra").write_bytes(b"x")
        with self.assertRaises(ValueError): verifier.verify(self.root, self.manifest)
        (self.root / "extra").unlink()
        (self.root / "fixture.tar.gz").unlink()
        with self.assertRaises(ValueError): verifier.verify(self.root, self.manifest)


if __name__ == "__main__":
    unittest.main()
