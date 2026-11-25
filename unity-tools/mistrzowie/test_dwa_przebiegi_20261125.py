"""25.11.2026 (owner 17:19 "Czego nie ruszyłeś sposobu budowania? … Wszystko jest w kompresji w grze?", 17:22 "Go"):
the Champions build the way Miasto R1 builds.

  - .glb images: written beside their model before Unity (tekstury_glb_osobno.py) and imported compressed
    (BraxChampionsGlbOsobnoImport, ASTC 6x6) - glTFast no longer makes them RGBA32 (#52: 250 images, ~1.7 GB), and the Unity
    step that never loaded one (#52, #57) and was killed for memory (#54, #57) has nothing left to do;
  - two runs: the Unity step has its own deadline (285 min), the world is saved when Unity completed it and no APK came
    out (stage 1 ended, or the APK failed / ran out of time), and the next run of the same commit restores it and builds
    only the APK;
  - the container may use the host's swap (dockerMemoryLimit 22g; #57 exit 137 with 14 GB of swap free);
  - BuildOptions.None (ZASADA-1 C)."""
import json
import os
import shutil
import struct
import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
WF = (ROOT / ".github/workflows/mistrzowie-apk.yml").read_text(encoding="utf-8")
BUILD = (ED / "BraxChampionsBuild.cs").read_text(encoding="utf-8")
ETAPY = (ED / "BraxChampionsEtapy.cs").read_text(encoding="utf-8")
OSOBNO = (ED / "BraxChampionsGlbOsobno.cs").read_text(encoding="utf-8")
STORE = (ROOT / "unity-tools/mistrzowie/world_store.sh").read_text(encoding="utf-8")
sys.path.insert(0, str(Path(__file__).resolve().parent))
import tekstury_glb_osobno as T  # noqa: E402

PNG = b"\x89PNG\r\n\x1a\n" + b"\x00" * 30
JPG = b"\xff\xd8\xff\xe0" + b"\x01" * 25
WEBP = b"RIFF\x00\x00\x00\x00WEBPVP8 " + b"\x02" * 10


def glb(path, images, materials, textures, samplers=()):
    blob, views, imgs = b"", [], []
    for name, data in images:
        views.append({"buffer": 0, "byteOffset": len(blob), "byteLength": len(data)})
        imgs.append({"name": name, "bufferView": len(views) - 1, "mimeType": "image/png"})
        blob += data + b"\x00" * ((4 - len(data) % 4) % 4)
    js = {"asset": {"version": "2.0"}, "images": imgs, "bufferViews": views, "buffers": [{"byteLength": len(blob)}],
          "materials": materials, "textures": textures, "samplers": list(samplers)}
    raw = json.dumps(js).encode()
    raw += b" " * ((4 - len(raw) % 4) % 4)
    body = struct.pack("<II", len(raw), T.CH_JSON) + raw + struct.pack("<II", len(blob), T.CH_BIN) + blob
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(struct.pack("<III", T.GLB_MAGIC, 2, 12 + len(body)) + body)


class GlbOsobno(unittest.TestCase):
    def setUp(self):
        self.tmp = Path(tempfile.mkdtemp())
        self.assets = self.tmp / "Assets"

    def tearDown(self):
        shutil.rmtree(self.tmp)

    def model(self, rel):
        glb(self.assets / rel, [("kolor", PNG), ("normal", JPG), ("webp", WEBP), ("oba", PNG)],
            materials=[{"pbrMetallicRoughness": {"baseColorTexture": {"index": 0}, "metallicRoughnessTexture": {"index": 3}},
                        "normalTexture": {"index": 1}},
                       {"pbrMetallicRoughness": {"baseColorTexture": {"index": 3}}}],
            textures=[{"source": 0, "sampler": 0}, {"source": 1}, {"source": 2}, {"source": 3}],
            samplers=[{"wrapS": 33071, "wrapT": 33071}])

    def test_images_beside_the_model_byte_for_byte(self):
        self.model("Source/HarborBeach/Models/łódź 1.glb")
        rap = self.tmp / "rap.json"
        self.assertEqual(T.main(["x", str(self.assets), str(rap)]), 0)
        r = json.loads(rap.read_text(encoding="utf-8"))
        self.assertEqual(r["glb"], [{"model": "Source/HarborBeach/Models/łódź 1.glb", "obrazy": 3}])
        js, chunks = T.czytaj(self.assets / "Source/HarborBeach/Models/łódź 1.glb")
        d = self.assets / "Source/HarborBeach/Models"
        uris = [i.get("uri") for i in js["images"]]
        # colour with its clamp wrap; a normal map is data (linear); WebP stays inside; used as colour AND data = colour
        self.assertEqual(uris[0], "_tekstury_glb_m/d_1/d_1_00__clamp.png")
        self.assertEqual(uris[1], "_tekstury_glb_m/d_1/d_1_01__lin.jpg")
        self.assertIsNone(uris[2])
        self.assertIn("bufferView", js["images"][2])
        self.assertEqual(uris[3], "_tekstury_glb_m/d_1/d_1_03.png")
        self.assertEqual((d / uris[0]).read_bytes(), PNG)
        self.assertEqual((d / uris[1]).read_bytes(), JPG)
        self.assertEqual(js["images"][1]["mimeType"], "image/jpeg")
        for i in (0, 1, 3):
            self.assertNotIn("bufferView", js["images"][i])
        # the rest of the file as it was (meshes, materials, the BIN block)
        self.assertEqual(len(js["bufferViews"]), 4)
        # run again: nothing left to take out, nothing changes
        before = (self.assets / "Source/HarborBeach/Models/łódź 1.glb").read_bytes()
        T.main(["x", str(self.assets), str(rap)])
        self.assertEqual((self.assets / "Source/HarborBeach/Models/łódź 1.glb").read_bytes(), before)
        self.assertEqual(json.loads(rap.read_text(encoding="utf-8"))["glb"], [])

    def test_only_the_scene_s_folders_and_never_the_tripo_originals(self):
        self.model("Source/Prywatne/Miasto/pojazdy_klasyki_01/oryginal/syrena_tripo.glb")
        self.model("Source/Claude/Osiedle/inny.glb")
        self.model("Source/Prywatne/Miasto/basen_01/prysznic.glb")
        (self.assets / "Source/Mistrzowie/zly.glb").parent.mkdir(parents=True, exist_ok=True)
        (self.assets / "Source/Mistrzowie/zly.glb").write_bytes(b"nie glb")
        rap = self.tmp / "rap.json"
        T.main(["x", str(self.assets), str(rap)])
        r = json.loads(rap.read_text(encoding="utf-8"))
        self.assertEqual([g["model"] for g in r["glb"]], ["Source/Prywatne/Miasto/basen_01/prysznic.glb"])
        self.assertEqual(len(r["bledy"]), 1)                                   # a bad file is reported, never fatal
        self.assertFalse((self.assets / "Source/Prywatne/Miasto/pojazdy_klasyki_01/oryginal/_tekstury_glb_m").exists())

    def test_the_scene_s_models_are_covered(self):
        # #57's 45 models with uncompressed .glb images, by folder
        for d in ("Source/HarborBeach/Models", "Source/Claude/Metro", "Source/Claude/Library", "Source/CityRebuild/landscape",
                  "Source/Mistrzowie", "Source/Prywatne/Miasto"):
            self.assertIn(d, T.KATALOGI)


class Unity(unittest.TestCase):
    def test_import_settings(self):
        imp = OSOBNO[OSOBNO.index("class BraxChampionsGlbOsobnoImport"):]
        for s in ('ps.format = TextureImporterFormat.ASTC_6x6;', 'ps.maxTextureSize = 2048;', 't.isReadable = false;',
                  't.mipmapEnabled = true; t.streamingMipmaps = true;', 't.sRGBTexture = !n.Contains("__lin");',
                  'n.Contains("__clamp") ? TextureWrapMode.Clamp', 'public override int GetPostprocessOrder() { return 1000; }'):
            self.assertIn(s, imp)
        self.assertEqual(T.FOLDER, "_tekstury_glb_m")
        self.assertIn('public const string Folder = "/_tekstury_glb_m/";', OSOBNO)
        # Miasto R1's own rule ("/_tekstury_glb/", 1K) never matches this folder
        self.assertNotIn("/_tekstury_glb/", "Assets/x/_tekstury_glb_m/a/b.png")

    def test_the_casino_rule_does_not_throw_on_the_vegetation_images(self):
        # the casino importer throws on any non-provider map under Vegetation2K/ - the two embedded vegetation .glb
        c = (ED / "BraxChampionsCasinoT11TextureImport.cs").read_text(encoding="utf-8")
        self.assertLess(c.index("if(assetPath.Contains(BraxChampionsGlbOsobno.Folder))return;"), c.index("throw new InvalidOperationException"))

    def test_check_after_import_never_fails_the_build(self):
        self.assertIn('public const string Raport = "mistrzowie-tekstury-glb.json";', OSOBNO)
        self.assertEqual(T.RAPORT.name, "mistrzowie-tekstury-glb.json")
        self.assertIn("ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport", OSOBNO)
        self.assertIn("catch (Exception e)", OSOBNO)
        self.assertLess(BUILD.index("BraxChampionsGlbOsobno.Sprawdz();"), BUILD.index("BraxChampionsSourceReadiness.VerifyBeforeScene();"))

    def test_stage_two_builds_only_the_apk(self):
        i = BUILD.index("if(apk&&!validationOnly&&BraxChampionsEtapy.SwiatPrzywrocony(report.sourceCommit))")
        blok = BUILD[i:BUILD.index("return;", i)]
        self.assertIn("BuildExplorationApk();", blok)
        self.assertNotIn("Generate(", blok)
        self.assertLess(i, BUILD.index("BraxChampionsFoundation.Generate();"))
        self.assertIn("Czytaj(Przywrocony) == commit && Czytaj(Gotowy) == commit", ETAPY)

    def test_stage_one_ends_cleanly_after_the_world(self):
        apk = BUILD[BUILD.index("            if(apk)\n            {"):]
        split, gotowy = apk.index("SplitIntoAreas(report);"), apk.index("BraxChampionsEtapy.SwiatGotowy(report.sourceCommit);")
        koniec, player = apk.index("if(BraxChampionsEtapy.KoniecEtapu1(report.sourceCommit))"), apk.index("BuildExplorationApk();")
        self.assertLess(split, gotowy)
        self.assertLess(gotowy, koniec)
        self.assertLess(koniec, player)
        self.assertNotIn("throw", apk[koniec:player])
        self.assertIn("AssetDatabase.SaveAssets();\n        File.WriteAllText(Gotowy, commit", ETAPY)
        self.assertIn('public const string Etap1 = "../build/Android/etap-1.txt";', ETAPY)
        self.assertIn("if (zostalo >= reszta) return false;", ETAPY)

    def test_no_lz4(self):
        self.assertNotIn("CompressWithLz4", BUILD)
        self.assertIn("options=BuildOptions.None});", BUILD)

    def test_new_scripts_have_their_own_guid(self):
        for f in ("BraxChampionsEtapy.cs.meta", "BraxChampionsGlbOsobno.cs.meta"):
            self.assertRegex((ED / f).read_text(encoding="utf-8"), r"guid: [0-9a-f]{32}")


class Workflow(unittest.TestCase):
    def step(self, name):
        i = WF.index("      - name: " + name)
        j = WF.find("\n      - name:", i + 1)
        return WF[i:j if j > 0 else len(WF)]

    def test_order_before_unity(self):
        order = ["Advisory - isolate only unused UV-incompatible GLBs", "Tekstury z .glb jako osobne pliki",
                 "Klucz zapisanego świata", "Przywróć zapisany świat tego commitu", "Termin kroku Unity i reguła etapów",
                 "Unity 6 - Miasto Mistrzów, Brax i APK do zwiedzania"]
        pos = [WF.index("      - name: " + n) for n in order]
        self.assertEqual(pos, sorted(pos))
        self.assertLess(WF.index("BRAX_LIBRARY_DROPPED"), pos[3])               # the world after the Library's disk rule
        self.assertIn('echo "START_ZADANIA=$(date +%s)" >> "$GITHUB_ENV"', self.step("Runner disk check"))

    def test_unity_step_limits(self):
        u = self.step("Unity 6 - Miasto Mistrzów, Brax i APK do zwiedzania")
        self.assertIn("timeout-minutes: 285", u)
        self.assertIn("dockerMemoryLimit: 22g", u)
        self.assertIn("timeout-minutes: 355", WF)
        t = self.step("Termin kroku Unity i reguła etapów")
        for s in ("LIMIT_KROKU_MIN: 285", "LIMIT_ZADANIA_MIN: 355", "> unity-city/mistrzowie-ci.txt",
                  'touch "$RUNNER_TEMP/mistrzowie-start.marker"', "LC_ALL=C sort"):
            self.assertIn(s, t)
        self.assertLess(t.index("mistrzowie-przed.txt"), t.index("mistrzowie-start.marker"))

    def test_saving_after_unity(self):
        stop = self.step("Zatrzymaj kontener Unity po przerwanym kroku")
        self.assertIn("steps.unity_build.outcome != 'success'", stop)
        self.assertIn("docker stop -t 45", stop)
        save = self.step("Zapisz zbudowany świat")
        for s in ("always() && !cancelled()", "hashFiles('unity-city/mistrzowie-swiat-gotowy.txt') != ''",
                  "hashFiles('build/Android/braxhorn-mistrzowie.gotowe') == ''", "steps.world_restore.outputs.restored != 'true'",
                  "world_store.sh save unity-city", "continue-on-error: true"):
            self.assertIn(s, save)
        unity = WF.index("      - name: Unity 6 - Miasto Mistrzów")
        self.assertLess(unity, WF.index("      - name: Zatrzymaj kontener Unity"))
        self.assertLess(WF.index("      - name: Zatrzymaj kontener Unity"), WF.index("      - name: Zapisz zbudowany świat"))
        self.assertLess(WF.index("      - name: Zapisz zbudowany świat"), WF.index("      - name: Save Champions import memory"))
        restore = self.step("Przywróć zapisany świat tego commitu")
        self.assertIn("continue-on-error: true", restore)
        self.assertIn("rm -f unity-city/mistrzowie-swiat-gotowy.txt unity-city/mistrzowie-swiat-przywrocony.txt", restore)

    def test_store_never_restores_a_half_world(self):
        up_parts = STORE.index('--clobber "$work/$name-$run".part*')
        up_manifest = STORE.index('--clobber "$work/$name.sha256"')
        self.assertLess(up_parts, up_manifest)
        self.assertLess(up_manifest, STORE.index("delete-asset"))
        self.assertLess(STORE.index("sha256sum -c --quiet"), STORE.index('tar -xf - -C "$work/x"'))
        self.assertLess(STORE.index('tar -xf - -C "$work/x"'), STORE.index('cp -a "$work/x/." "$project/"'))
        self.assertIn("! -path 'Assets/StreamingAssets/*'", STORE)


class StoreRoundTrip(unittest.TestCase):
    """world_store.sh save then restore through a fake `gh` keeping release assets in a folder"""

    def test_round_trip(self):
        tmp = Path(tempfile.mkdtemp())
        try:
            rel, bin_ = tmp / "release", tmp / "bin"
            rel.mkdir(); bin_.mkdir()
            gh = bin_ / "gh"
            gh.write_text(textwrap.dedent(f"""\
                #!/usr/bin/env bash
                R={rel}
                case "$1 $2" in
                  "release view") [ -d "$R/tag" ] || exit 1; if [ "$5" = "--json" ] || [ "$4" = "--json" ]; then ls "$R/tag"; fi ;;
                  "release create") mkdir -p "$R/tag" ;;
                  "release upload") shift 2; for a in "$@"; do [ -f "$a" ] && cp "$a" "$R/tag/"; done ;;
                  "release download") p=""; d=""; while [ $# -gt 0 ]; do case "$1" in --pattern) p=$2; shift;; --dir) d=$2; shift;; esac; shift; done
                                      [ -f "$R/tag/$p" ] || exit 1; cp "$R/tag/$p" "$d/" ;;
                  "release delete-asset") rm -f "$R/tag/$4" ;;
                esac
                """))
            gh.chmod(0o755)
            proj = tmp / "p"
            for f in ("Assets/a.txt", "Assets/stary.txt", "Assets/zostaje.txt", "ProjectSettings/x.asset"):
                (proj / f).parent.mkdir(parents=True, exist_ok=True)
                (proj / f).write_text("przed " + f)
            env = dict(os.environ, PATH=f"{bin_}:{os.environ['PATH']}", GH_TOKEN="t", GITHUB_REPOSITORY="o/r", KLUCZ="tajne",
                       RUNNER_TEMP=str(tmp / "rt"), GITHUB_RUN_ID="7", GITHUB_RUN_ATTEMPT="1", LC_ALL="C")
            (tmp / "rt").mkdir()
            before = tmp / "przed.txt"
            before.write_text(subprocess.run("cd %s && find Assets ProjectSettings -type f | LC_ALL=C sort" % proj, shell=True,
                                             capture_output=True, text=True, check=True).stdout)
            start = tmp / "start"; start.touch()
            os.utime(start, (1_000_000_000, 1_000_000_000))
            for f in ("Assets/a.txt", "Assets/stary.txt", "Assets/zostaje.txt", "ProjectSettings/x.asset"):
                os.utime(proj / f, (900_000_000, 900_000_000))
            # "Unity": a new scene and its meta, a changed setting, a deleted file, built bundles (never part of the world)
            (proj / "Assets/PlayableGenerated/M").mkdir(parents=True)
            (proj / "Assets/PlayableGenerated/M/s.unity").write_text("scena")
            (proj / "Assets/PlayableGenerated/M/s.unity.meta").write_text("guid: 1")
            (proj / "ProjectSettings/x.asset").write_text("po")
            (proj / "Assets/stary.txt").unlink()
            (proj / "Assets/StreamingAssets/BraxContent").mkdir(parents=True)
            (proj / "Assets/StreamingAssets/BraxContent/b.bundle").write_text("bundle")
            store = str(ROOT / "unity-tools/mistrzowie/world_store.sh")
            key = "0123456789abcdef0123456789abcdef"
            # no world marker: nothing saved
            r = subprocess.run(["bash", store, "save", str(proj), key], env=dict(env, WORLD_START=str(start), WORLD_BEFORE=str(before)),
                               capture_output=True, text=True)
            self.assertEqual(r.returncode, 0, r.stderr)
            self.assertIn("nothing to save", r.stdout)
            (proj / "mistrzowie-swiat-gotowy.txt").write_text("abc123\n")
            r = subprocess.run(["bash", store, "save", str(proj), key], env=dict(env, WORLD_START=str(start), WORLD_BEFORE=str(before)),
                               capture_output=True, text=True)
            self.assertEqual(r.returncode, 0, r.stderr)
            self.assertIn("BRAX_WORLD_STORE saved key=" + key, r.stdout)
            self.assertTrue((rel / "tag" / f"mistrzowie-world-{key}.sha256").exists())
            # a fresh checkout of the same commit
            proj2 = tmp / "p2"
            for f in ("Assets/a.txt", "Assets/stary.txt", "Assets/zostaje.txt", "ProjectSettings/x.asset"):
                (proj2 / f).parent.mkdir(parents=True, exist_ok=True)
                (proj2 / f).write_text("przed " + f)
            r = subprocess.run(["bash", store, "restore", str(proj2), key], env=env, capture_output=True, text=True)
            self.assertEqual(r.returncode, 0, r.stderr)
            self.assertIn("BRAX_WORLD_STORE restored", r.stdout)
            self.assertEqual((proj2 / "Assets/PlayableGenerated/M/s.unity").read_text(), "scena")
            self.assertEqual((proj2 / "Assets/PlayableGenerated/M/s.unity.meta").read_text(), "guid: 1")
            self.assertEqual((proj2 / "ProjectSettings/x.asset").read_text(), "po")
            self.assertFalse((proj2 / "Assets/stary.txt").exists())
            self.assertTrue((proj2 / "Assets/zostaje.txt").exists())
            self.assertFalse((proj2 / "Assets/StreamingAssets").exists())
            self.assertEqual((proj2 / "mistrzowie-swiat-przywrocony.txt").read_text(), "abc123\n")
            # another commit's key: a miss, a normal full run
            proj3 = tmp / "p3"; proj3.mkdir()
            r = subprocess.run(["bash", store, "restore", str(proj3), "f" * 32], env=env, capture_output=True, text=True)
            self.assertEqual(r.returncode, 0, r.stderr)
            self.assertIn("miss", r.stdout)
            self.assertFalse((proj3 / "mistrzowie-swiat-przywrocony.txt").exists())
            # a damaged part: a miss, the project untouched
            part = next((rel / "tag").glob("mistrzowie-world-*.part*"))
            part.write_bytes(part.read_bytes()[:-5] + b"xxxxx")
            proj4 = tmp / "p4"; proj4.mkdir()
            r = subprocess.run(["bash", store, "restore", str(proj4), key], env=env, capture_output=True, text=True)
            self.assertEqual(r.returncode, 0, r.stderr)
            self.assertIn("SHA-256", r.stdout)
            self.assertEqual(list(proj4.iterdir()), [])
        finally:
            shutil.rmtree(tmp)


class KlasykiWBunkrze(unittest.TestCase):
    """#60 (25.11, run 45658651245) stopped at 3 h 08 min of Unity: "Second A1 player car forbidden" - the owner's classics in
    the bunker are BraxChampionsCars of the foundation scene, and the owner car's guard counted every car"""

    def test_only_a_second_owner_car_is_forbidden(self):
        car = (ED / "BraxChampionsCarBuilder.cs").read_text(encoding="utf-8")
        guard = car[car.index("public static void Generate(Transform parent)"):car.index('throw new InvalidOperationException("Second A1 player car forbidden");')]
        self.assertIn("c => !c.helikopter && c.name == RootName", guard)
        self.assertIn("var root = new GameObject(RootName);", car)
        self.assertIn('KLASYK | ', (ED / "BraxChampionsKlasyki.cs").read_text(encoding="utf-8"))
        self.assertIn('cars=cars.Where(c=>!c.name.StartsWith("KLASYK | ")).ToArray();', BUILD)


if __name__ == "__main__":
    unittest.main()
