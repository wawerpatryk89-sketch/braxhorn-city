"""Statyczny preflight przebudowy obozu, bez Unity i bez zmian w postaciach.

Uruchom: python3 -m unittest unity-tools/oboz/tests/test_compact_owner_remodel.py
To nie jest test funkcjonalny APK, geometrii mesh ani wybranej konfiguracji Android.
"""
import json
import math
from pathlib import Path
import re
import unittest

ROOT=Path(__file__).resolve().parents[3]
EDITOR=ROOT/"unity-city/Assets/Editor/Oboz"
OBOS=ROOT/"unity-tools/oboz"


def get_float(cs: str, key: str) -> float:
    m=re.search(r"\b"+re.escape(key)+r"\s*=\s*(-?\d+(?:\.\d+)?)f;",cs)
    if not m:
        raise AssertionError(f"Nie znaleziono stałej {key}")
    return float(m.group(1))


class CompactOwnerRemodelTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.layout=(EDITOR/"BraxObozCampusLayout.cs").read_text("utf8")
        cls.env=(EDITOR/"BraxObozEnvironment.cs").read_text("utf8")
        cls.campus=(EDITOR/"BraxObozCampus.cs").read_text("utf8")
        cls.services=(EDITOR/"BraxObozServices.cs").read_text("utf8")
        cls.pbr=(EDITOR/"BraxObozMaterials.cs").read_text("utf8")
        cls.importer=(EDITOR/"BraxObozImport.cs").read_text("utf8")
        cls.zones=json.loads((EDITOR/"CampusLayout.json").read_text("utf8"))
        cls.handoff=json.loads((OBOS/"OWNER_ASSETS/HANDOFF_MANIFEST_20261105.json").read_text("utf8"))

    def test_world_scale_and_gate(self):
        self.assertEqual(get_float(self.env,"K"),1.294)
        self.assertEqual(get_float(self.layout,"GateZ"),-125)
        self.assertIn('P(0,0,-125)',self.campus)
        self.assertIn('float sourceLength=actualLengthMetres/K;',self.campus)
        self.assertIn('const float actualLengthMetres=100f;',self.campus)

    def test_plot_is_at_least_half_smaller(self):
        rx=get_float(self.layout,"PlotRadiusX")
        rz=get_float(self.layout,"PlotRadiusZ")
        center=get_float(self.layout,"PlotCenterZ")
        ratio=math.pi*rx*rz/(252*240)
        self.assertGreater(ratio,.35)
        self.assertLessEqual(ratio,.50)
        self.assertAlmostEqual(center-rz,-125)

    def test_required_owner_sources_in_manifest(self):
        names={Path(p["path"]).name for p in self.handoff["contents"]}
        required={
            "leather_punching_bag.glb",
            "kitchen_-_assets.glb",
            "glass-food-display-showcase-with-trays.glb",
            "picnic_set_1.glb",
            "close-coupled-toilet.glb",
            "wall-urinal.glb",
            "trough-washbasin.glb",
            "shower-tray-square.glb",
            "rubberized_track_2k.blend.zip",
            "Meshy_AI_The_object_is_a_recta_1104160815_texture_fbx.zip",
            "montorfano_4k.hdr",
            "44B8F5D6-7F27-4EF3-A804-9BC8C4F462D0.jpeg",
        }
        self.assertFalse(required-names)
        self.assertEqual(len(self.handoff["contents"]),31)

    def test_owner_import_fail_closed_without_assets(self):
        self.assertIn("OwnerBagPrototype",self.env)
        self.assertNotIn('Cylinder(pivot,"Korpus worka"',self.env)
        self.assertIn('OwnerTrackPbr()',self.campus)
        self.assertNotIn('TartanGrain()',self.campus)
        self.assertIn('OwnerShelter(shelter)',self.campus)
        self.assertIn('OwnerPanorama()',self.env)
        self.assertIn('OwnerGateSign(gate)',self.campus)
        self.assertIn('OwnerFixture(cubicle,"close-coupled-toilet.glb"',self.services)
        self.assertIn('OwnerFixture(shower,"shower-tray-square.glb"',self.services)
        self.assertIn('OwnerFixture(sink,"trough-washbasin.glb"',self.services)
        self.assertIn('OwnerFixture(building,"wall-urinal.glb"',self.services)
        self.assertIn('if(id=="canteen")FurnishCanteen(group)',self.services)

    def test_pbr_and_stream_policy(self):
        self.assertIn('android.maxTextureSize=androidLimit',self.pbr)
        self.assertIn('const int androidLimit = 2048;',self.pbr)
        self.assertIn('t.streamingMipmaps = true;',self.importer)
        self.assertIn('androidHdr.overridden=false',self.importer)
        self.assertIn('t.streamingMipmaps=false;',self.importer)
        self.assertIn('TextureImporterFormat.ASTC_4x4',self.importer)
        self.assertIn('TextureImporterFormat.ASTC_6x6',self.importer)

    def test_no_fake_glb_sources_in_public_git(self):
        ignored=(ROOT/".gitignore").read_text("utf8")
        self.assertIn('unity-city/Assets/Source/Prywatne/',ignored)
        self.assertIn('OwnerAssets/',self.env)
        self.assertIn("*.glb",ignored)
        self.assertIn("*.fbx",ignored)
        self.assertIn("*.zip",ignored)

    def test_no_bunker_anywhere_in_camp(self):
        """Właściciel odrzucił całkowicie bunkier; scenie nie wolno go odtwarzać."""
        for old_token in ("SourceBunker(", "underground_bunker__unity.glb",
                          "Bunkier bramy", "Bunkier dowodzenia"):
            self.assertNotIn(old_token, self.env)
        for region in ("clearances", "lawnClearances"):
            self.assertFalse(
                [zone["id"] for zone in self.zones[region]
                 if "bunker" in zone["id"].lower() or "bunkier" in zone["id"].lower()],
                "Bunkier nadal rezerwuje teren i blokuje nową roślinność"
            )

    def test_clearance_data_inside_plot(self):
        rx=get_float(self.layout,"PlotRadiusX")-8.8
        rz=get_float(self.layout,"PlotRadiusZ")-8.8
        cz=get_float(self.layout,"PlotCenterZ")
        allowed={"bus-turn","bus-approach","bus-stop","bus-connect",
                 "footpath-external","bus-return-walkway","gate-pocket","footpath"}
        for kind in ("clearances","lawnClearances"):
            ids=set()
            for zone in self.zones[kind]:
                self.assertNotIn(zone["id"],ids,zone["id"])
                ids.add(zone["id"])
                if zone["id"] in allowed or "bieznia-100m"==zone["id"]:
                    continue
                for xx in (-.5,.5):
                    for zz in (-.5,.5):
                        x=zone["x"]+xx*zone["width"]
                        z=zone["z"]+zz*zone["depth"]
                        ratio=(x/rx)**2+((z-cz)/rz)**2
                        self.assertLessEqual(ratio,1.05,(kind,zone["id"],round(ratio,3)))
        self.assertIn('new Rect(-51.5f,-41f,2.5f,28f)',self.env)
        self.assertIn('Cabin("Domek 1", -60, -18, -90)',self.env)
        self.assertIn('Cabin("Domek 3", -42, -18, 90)',self.env)


if __name__=="__main__":
    unittest.main()
