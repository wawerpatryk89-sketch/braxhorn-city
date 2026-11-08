"""Issue #30 BRAXHORN car/audio source-only regression tests. No Unity/APK."""
from pathlib import Path
import hashlib
import json
import unittest

ROOT = Path(__file__).resolve().parents[2]

def text(path):
    return (ROOT/path).read_text("utf-8")

def data(path):
    return (ROOT/path).read_bytes()

class Issue30CarAudio(unittest.TestCase):
    def test_final_audio_binaries_are_real_small_ogg_with_attribution(self):
        manifest=json.loads(text("unity-tools/mistrzowie/champion_car_audio_sources.json"))
        self.assertEqual(manifest["status"],"BINARIES_COMMITTED_AND_HASHED")
        self.assertEqual(manifest["source"]["source_sha256"],
            "15a9fcf82f53c710f489b7d888557b4937ae3f50d968c22f2be342d29df398b6")
        expected={
          "idle":("unity-city/Assets/Source/Mistrzowie/Audio/BraxCar/brax_engine_idle.ogg",
                  32307,"e44d646c067ce703596160810e2305aa6033b02aa878a16e3b93f5971af1dcfc"),
          "rev":("unity-city/Assets/Source/Mistrzowie/Audio/BraxCar/brax_engine_rev.ogg",
                 20200,"7e332fb8eb96e9773a9d76e60a80c4e905976ed3d6888f1f9191a399ea823834"),
        }
        for role,(path,size,digest) in expected.items():
            raw=data(path)
            self.assertEqual(len(raw),size,role)
            self.assertEqual(hashlib.sha256(raw).hexdigest(),digest,role)
            self.assertTrue(raw.startswith(b"OggS"),role)
            self.assertIn(b"vorbis",raw[:8192],role)
            self.assertIn(b"CC BY 3.0 Unported",raw[:8192],role)
            self.assertIn(b"Antti Makkonen",raw[:8192],role)

    def test_builder_uses_committed_ogg_and_android_mobile_import(self):
        builder=text("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCarBuilder.cs")
        self.assertIn("brax_engine_idle.ogg",builder)
        self.assertIn("brax_engine_rev.ogg",builder)
        self.assertNotIn("brax_engine_idle.mp3",builder)
        self.assertIn("AudioCompressionFormat.Vorbis",builder)
        self.assertIn("forceToMono = true",builder)
        self.assertIn("AudioSampleRateSetting.OptimizeSampleRate",builder)

    def test_runtime_has_3d_idle_rev_and_owner_speed_contract(self):
        car=text("unity-city/Assets/Mistrzowie/BraxChampionsCar.cs")
        builder=text("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCarBuilder.cs")
        self.assertIn("AudioRolloffMode.Linear",car)
        self.assertIn("spatialBlend = 1f",car)
        self.assertIn("A1_CAR_ENGINE_REV",car)
        self.assertIn("revArmed",car)
        self.assertIn("maxSpeed = 120f / 3.6f",car)
        self.assertIn("car.maxSpeed = 120f / 3.6f",builder)
        self.assertIn("LengthMetres = 4.95f",builder)
        self.assertNotIn("AudioClip.Create",car)

    def test_attribution_file_and_offline_verifier_exist(self):
        notice=text("unity-city/Assets/Source/Mistrzowie/Audio/BraxCar/ATTRIBUTION.txt")
        self.assertIn("CC BY 3.0",notice)
        self.assertIn("Antti Makkonen",notice)
        verifier=text("unity-tools/mistrzowie/prepare_champion_car_audio.py")
        self.assertNotIn("requests",verifier)
        self.assertNotIn("urllib",verifier)
        self.assertIn("A1_CAR_AUDIO_FINAL_PASS",verifier)

if __name__=="__main__":
    unittest.main()
