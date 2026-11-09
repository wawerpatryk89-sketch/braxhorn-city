from pathlib import Path
import hashlib,json,subprocess,unittest
ROOT=Path(__file__).resolve().parents[2]
def data(path):
    p=ROOT/path
    if p.is_file(): return p.read_bytes()
    return subprocess.check_output(["git","show","HEAD:"+path],cwd=ROOT)
def text(path): return data(path).decode("utf-8")
class Issue30CarAudio(unittest.TestCase):
    def test_audio_files_match_manifest_and_are_long_enough(self):
        m=json.loads(text("unity-tools/mistrzowie/champion_car_audio_sources.json"))
        self.assertEqual(m["status"],"BINARIES_COMMITTED_AND_HASHED")
        self.assertEqual(m["source"]["license"],"CC BY 3.0 Unported")
        for c in m["clips"]:
            raw=data(c["unity_path"])
            self.assertEqual(len(raw),c["bytes"])
            self.assertEqual(hashlib.sha256(raw).hexdigest(),c["sha256"])
            self.assertTrue(raw.startswith(b"OggS"))
            self.assertIn(b"Antti Makkonen",raw[:8192])
        idle=next(c for c in m["clips"] if c["role"]=="idle")
        rev=next(c for c in m["clips"] if c["role"]=="rev")
        self.assertGreaterEqual(idle["duration_seconds"],4.5)
        self.assertGreaterEqual(rev["duration_seconds"],2.0)
    def test_runtime_and_mobile_contract(self):
        b=text("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCarBuilder.cs")
        r=text("unity-city/Assets/Mistrzowie/BraxChampionsCar.cs")
        for s in ("v8_idle_850.ogg","v8_low_2200.ogg","v8_high_4800.ogg","AudioCompressionFormat.Vorbis","forceToMono = true"):
            self.assertIn(s,b)
        for s in ("AudioRolloffMode.Linear","spatialBlend = 1f","V8GearTopKph","maxSpeed = 200f / 3.6f"):
            self.assertIn(s,r)
        self.assertIn("LengthMetres = 4.95f",b)
        self.assertNotIn("AudioClip.Create",r)
if __name__=="__main__": unittest.main()
