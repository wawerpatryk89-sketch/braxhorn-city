"""Offline regression tests for transient source download failures."""
import importlib.util
import io
import hashlib
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import urllib.error
import http.client

spec = importlib.util.spec_from_file_location("kickboxing_fetch", Path(__file__).parents[1] / "fetch_assets.py")
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)

class DownloadRetries(unittest.TestCase):
    def test_502_then_success_keeps_exact_bytes(self):
        error = urllib.error.HTTPError("https://example.test", 502, "Bad Gateway", {}, None)
        with patch.object(m.urllib.request, "urlopen", side_effect=[error, io.BytesIO(b"original")]) as call, patch.object(m.time, "sleep") as sleep:
            self.assertEqual(m.fetch("https://example.test"), b"original")
            self.assertEqual(call.call_count, 2)
            sleep.assert_called_once_with(2)

    def test_404_is_not_retried(self):
        error = urllib.error.HTTPError("https://example.test", 404, "Missing", {}, None)
        with patch.object(m.urllib.request, "urlopen", side_effect=error) as call, patch.object(m.time, "sleep") as sleep:
            with self.assertRaises(urllib.error.HTTPError):
                m.fetch("https://example.test")
            self.assertEqual(call.call_count, 1)
            sleep.assert_not_called()

    def test_persistent_503_is_bounded_and_fails(self):
        error = urllib.error.HTTPError("https://example.test", 503, "Unavailable", {}, None)
        with patch.object(m.urllib.request, "urlopen", side_effect=error) as call, patch.object(m.time, "sleep") as sleep:
            with self.assertRaises(urllib.error.HTTPError):
                m.fetch("https://example.test")
            self.assertEqual(call.call_count, 5)
            self.assertEqual([c.args[0] for c in sleep.call_args_list], [2, 4, 8, 16])

    def test_network_failure_then_success(self):
        with patch.object(m.urllib.request, "urlopen", side_effect=[urllib.error.URLError("timeout"), io.BytesIO(b"original")]), patch.object(m.time, "sleep"):
            self.assertEqual(m.fetch("https://example.test"), b"original")

    def test_partial_response_is_retried_from_start(self):
        with patch.object(m.urllib.request, "urlopen", side_effect=[http.client.IncompleteRead(b"part"), io.BytesIO(b"whole")]), patch.object(m.time, "sleep"):
            self.assertEqual(m.fetch("https://example.test"), b"whole")

    def test_digest_failure_preserves_existing_file(self):
        with tempfile.TemporaryDirectory() as folder:
            target = Path(folder) / "Sink.glb"
            target.write_bytes(b"keep")
            with patch.object(m, "fetch", return_value=b"changed"):
                with self.assertRaises(RuntimeError):
                    m.save_source({"url": "https://example.test", "sha256": hashlib.sha256(b"original").hexdigest()},
                                  target, "source", "author", "license")
            self.assertEqual(target.read_bytes(), b"keep")

if __name__ == "__main__":
    unittest.main()
