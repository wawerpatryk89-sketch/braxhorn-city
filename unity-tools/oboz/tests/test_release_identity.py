import importlib.util
from pathlib import Path
import unittest

spec=importlib.util.spec_from_file_location('release_identity',Path(__file__).parents[1]/'release_identity.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)

class UpdateTests(unittest.TestCase):
    def previous(self):
        return {'applicationId':m.PACKAGE,'versionCode':100,'signingCertificateSha256':'a'*64}
    def test_rerun_and_clock_rollback_still_increase(self):
        self.assertEqual(m.version_code(m.EPOCH+50,self.previous()),101)
    def test_first_install(self):
        self.assertEqual(m.version_code(m.EPOCH+200),200)
    def test_wrong_application_is_rejected(self):
        p=self.previous();p['applicationId']='com.braxhorn.city'
        with self.assertRaises(ValueError):m.version_code(m.EPOCH+200,p)
    def test_missing_certificate_is_rejected(self):
        p=self.previous();p.pop('signingCertificateSha256')
        with self.assertRaises(ValueError):m.version_code(m.EPOCH+200,p)
    def test_overflow_is_rejected(self):
        with self.assertRaises(ValueError):m.version_code(m.EPOCH+m.LIMIT+1)
    def test_valid_signed_update(self):
        r=m.verify_metadata("package: name='com.braxhorn.oboz' versionCode='101'",'Signer #1 certificate SHA-256 digest: '+'a'*64,101,self.previous())
        self.assertEqual(r['versionCode'],101)
    def test_changed_signer_is_rejected(self):
        with self.assertRaises(ValueError):m.verify_metadata("package: name='com.braxhorn.oboz' versionCode='101'",'Signer #1 certificate SHA-256 digest: '+'b'*64,101,self.previous())
    def test_downgrade_is_rejected(self):
        with self.assertRaises(ValueError):m.verify_metadata("package: name='com.braxhorn.oboz' versionCode='100'",'Signer #1 certificate SHA-256 digest: '+'a'*64,100,self.previous())
    def test_verification_rejects_other_previous_app(self):
        p=self.previous();p['applicationId']='com.other.app'
        with self.assertRaises(ValueError):m.verify_metadata("package: name='com.braxhorn.oboz' versionCode='101'",'Signer #1 certificate SHA-256 digest: '+'a'*64,101,p)
    def test_verification_rejects_invalid_version(self):
        with self.assertRaises(ValueError):m.verify_metadata("package: name='com.braxhorn.oboz' versionCode='0'",'Signer #1 certificate SHA-256 digest: '+'a'*64,0)
    def test_malformed_identity_is_rejected(self):
        for previous in ([], 'invalid', {'applicationId':m.PACKAGE,'versionCode':100,'signingCertificateSha256':None}):
            with self.assertRaises(ValueError):m.version_code(m.EPOCH+200,previous)
    def test_multiple_signers_are_rejected(self):
        signing='Signer #1 certificate SHA-256 digest: '+'a'*64+'\nSigner #2 certificate SHA-256 digest: '+'b'*64
        with self.assertRaises(ValueError):m.verify_metadata("package: name='com.braxhorn.oboz' versionCode='101'",signing,101,self.previous())

    def test_build_tools_37_scheme_labels(self):
        for label in ('V1', 'V2', 'V3.0', 'V3.1', 'V4'):
            signing=label+' Signer: certificate SHA-256 digest: '+'a'*64+'\n'
            r=m.verify_metadata("package: name='com.braxhorn.oboz' versionCode='101'",signing,101,self.previous())
            self.assertEqual(r['signingCertificateSha256'],'a'*64)
    def test_build_tools_37_rejects_invalid_certificates(self):
        prefix='V3.0 Signer: certificate SHA-256 digest: '
        for signing in ('', prefix+'b'*64, prefix+'a'*63, prefix+'g'*64,
                        prefix+'a'*64+'\n'+prefix+'a'*64,
                        prefix+'a'*64+'\nV2 Signer: certificate SHA-256 digest: '+'b'*64,
                        'Unknown Signer: certificate SHA-256 digest: '+'a'*64):
            with self.subTest(signing=signing):
                with self.assertRaises(ValueError):
                    m.verify_metadata("package: name='com.braxhorn.oboz' versionCode='101'",signing,101,self.previous())
    def test_source_stamp_is_not_application_signer(self):
        signing='V3.0 Signer: certificate SHA-256 digest: '+'a'*64+'\nSource Stamp Signer certificate SHA-256 digest: '+'b'*64
        self.assertEqual(m.verify_metadata("package: name='com.braxhorn.oboz' versionCode='101'",signing,101,self.previous())['signingCertificateSha256'],'a'*64)

if __name__=='__main__':unittest.main()

