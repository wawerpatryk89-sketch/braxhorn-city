import sys
from pathlib import Path
import unittest

sys.path.insert(0, str(Path(__file__).parents[1]))
from build_request import validate


class BuildRequestTests(unittest.TestCase):
    def setUp(self):
        self.request = dict(request='owner-request-new', ownerRequested=True,
                            sourceBaseCommit='a' * 40, firstInstall=False,
                            privateInputsSha256='b' * 64)
        self.previous = dict(request='owner-request-old')
        self.context = dict(event='push', ref='refs/heads/gpt/oboz', before_sha='a' * 40,
                            parent_sha='a' * 40, private_sha='b' * 64)

    def run_request(self, **context):
        return validate(self.request, self.previous, **dict(self.context, **context))

    def test_one_new_owner_request_on_reviewed_base(self):
        self.assertFalse(self.run_request())

    def test_repeated_request_cannot_build_again(self):
        self.previous = dict(self.request)
        with self.assertRaises(ValueError): self.run_request()

    def test_normal_edit_without_request_cannot_build(self):
        self.request.pop('ownerRequested')
        with self.assertRaises(ValueError): self.run_request()

    def test_other_branch_and_automatic_events_are_blocked(self):
        for context in [dict(ref='refs/heads/main'), dict(ref='refs/heads/other'),
                        dict(event='pull_request'), dict(event='schedule'), dict(event='workflow_run')]:
            with self.subTest(context=context), self.assertRaises(ValueError):
                self.run_request(**context)

    def test_stale_base_multi_commit_push_and_branch_creation_are_blocked(self):
        for context in [dict(before_sha='c' * 40), dict(parent_sha='c' * 40), dict(before_sha='0' * 40)]:
            with self.subTest(context=context), self.assertRaises(ValueError):
                self.run_request(**context)

    def test_wrong_input_manifest_is_blocked(self):
        with self.assertRaises(ValueError): self.run_request(private_sha='c' * 64)

    def test_missing_previous_request_is_blocked(self):
        self.previous = None
        with self.assertRaises(ValueError): self.run_request()

    def test_malformed_request_is_blocked(self):
        for key, value in [('sourceBaseCommit', None), ('firstInstall', 'false'),
                           ('request', ''), ('ownerRequested', 'true')]:
            original = self.request[key]
            with self.subTest(key=key):
                self.request[key] = value
                with self.assertRaises(ValueError): self.run_request()
                self.request[key] = original

    def test_manual_dispatch_keeps_explicit_first_install_input(self):
        self.previous = None
        self.assertFalse(self.run_request(event='workflow_dispatch'))
        self.assertTrue(self.run_request(event='workflow_dispatch', manual_first_install=True))


if __name__ == '__main__': unittest.main()
