"""Byk właściciela: oryginalny 4X priorytet, 1X tylko podniesiony x4."""
from pathlib import Path
import unittest
R=Path(__file__).resolve().parents[3]
S=(R/"unity-city/Assets/Editor/Oboz/BraxObozCampus.cs").read_text("utf8")
class Bull4xPriority(unittest.TestCase):
    def test_owner_4x_first_and_no_double_four(self):
        start=S.index('static void RondoBull(')
        end=S.index('static void Disc(',start)
        f=S[start:end]
        self.assertLess(f.index('LoadAssetAtPath<GameObject>(scaledPath)'),
                        f.index('LoadAssetAtPath<GameObject>(originalPath)'))
        self.assertIn('if(!scaledOwnerAsset)model.transform.localScale*=4f;',f)
        self.assertIn('OBOZ_BULL_4X_SOURCE_PENDING',f)
        self.assertIn('BRAXHORN_OBOZ_BULL_CLEARANCE_REVIEW',f)
        self.assertIn('BraxObozPlacement.OnGround',f)
        self.assertNotIn('(4.8f*K)/longest',f)
