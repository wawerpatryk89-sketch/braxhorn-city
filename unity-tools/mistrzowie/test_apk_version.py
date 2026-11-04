import unittest
from apk_version import version_code, EPOCH, OFFSET, ANDROID_MAX


class VersionTests(unittest.TestCase):
    def test_new_build_and_retry_use_time_not_workflow_counter(self):
        first = version_code(EPOCH + 200000000)
        self.assertGreater(version_code(EPOCH + 200000001), first)
        self.assertGreater(first, 8501)

    def test_android_bounds(self):
        self.assertEqual(version_code(EPOCH), OFFSET)
        self.assertEqual(version_code(EPOCH + ANDROID_MAX - OFFSET), ANDROID_MAX)
        for timestamp in [EPOCH - 1, EPOCH + ANDROID_MAX - OFFSET + 1]:
            with self.assertRaises(ValueError):
                version_code(timestamp)


if __name__ == "__main__":
    unittest.main()
