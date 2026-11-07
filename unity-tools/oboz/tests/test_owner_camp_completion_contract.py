"""Cheap source acceptance gates for the owner's complete camp order.

Static-only. Does NOT claim Android visuals, physics or Unity compilation pass.
Discovered by oboz-unity-build.yml *before* any Unity invocation.
"""
from pathlib import Path
import re
import unittest

ROOT=Path(__file__).resolve().parents[3]
EDITOR=ROOT/"unity-city/Assets/Editor/Oboz"
TOOLS=ROOT/"unity-tools/oboz"

def load(path):
    return path.read_text(encoding="utf-8")

class OwnerCampCompletionContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.environment=load(EDITOR/"BraxObozEnvironment.cs")
        cls.campus=load(EDITOR/"BraxObozCampus.cs")
        cls.services=load(EDITOR/"BraxObozServices.cs")
        cls.vegetation=load(EDITOR/"BraxObozVegetation.cs")
        cls.materials=load(EDITOR/"BraxObozMaterials.cs")
        cls.delivery=load(TOOLS/"owner_extra_assets.py")
        cls.workflow=load(ROOT/".github/workflows/oboz-unity-build.yml")

    def test_source_kitchen_and_lockers_are_not_baked_into_cabin_shell(self):
        # CombineDecoration recursively removes MeshFilter and MeshRenderer from
        # all descendants. It MUST run before adding the owner's imported GLBs.
        cabin = self.environment.split("static void Cabin(string name,", 1)[1]
        cabin = cabin.split("static void ColliderBox(", 1)[0]
        self.assertEqual(cabin.count("CombineDecoration(t);"), 1)
        batch = cabin.index("CombineDecoration(t);")
        self.assertLess(batch, cabin.index("OwnerFixtureSourceMetres(t"))
        self.assertLess(batch, cabin.index("OwnerCabinKitchenette(t)"))
        self.assertLess(batch, cabin.index("OwnerSingleBed(t,"))

    def test_scene_quality_report_counts_original_owner_furniture(self):
        validation = load(EDITOR/"BraxObozValidation.cs")
        self.assertIn("IDANAS=\" + beds", validation)
        self.assertIn("szafki=\" + lockers", validation)
        self.assertIn("aneks=\" + kitchens", validation)
        self.assertIn("zlew=\" + sinks", validation)
        self.assertIn("kosz=\" + bins", validation)
        self.assertIn("gym_environment FBX", validation)
        self.assertIn("tractor_tire GLB", validation)
        checks = validation.split("var sourceIssues = new List<string>();", 1)[1].split("var manifest =", 1)[0]
        self.assertIn("sourceIssues.Add(", checks)
        self.assertNotIn("structuralErrors =", checks)
        self.assertNotIn("throw new", checks)

    def test_owner_bed_and_kitchen_in_every_cabin(self):
        for i in range(1,5):
            self.assertIn(f'Cabin("Domek {i}"',self.environment)
        self.assertIn('OwnerSingleBed(t,',self.environment)
        self.assertIn('OwnerCabinKitchenette(t)',self.environment)
        self.assertIn('foreach(float side in new[]{-1f,1f})',self.environment)
        for old in ('Box(t,"Łóżko — rama"','Box(t,"Materac z tkaniną"',
                    'Box(t,"Stół",new Vector3(1.9f'):
            self.assertNotIn(old,self.environment)
        self.assertIn('OwnerExtras/Bedroom/ikea_idanas_single_bed.glb',self.services)
        for part in ("CookingTable.001_","PortableStove_Base.001_",
                     "GasCylinder.001_","PotRack.001_","Sink.001_",
                     "Plate_Regular.001_"):
            self.assertIn(part,self.services)
        self.assertIn('CabinWasteBin(region)',self.services)

    def test_canteen_source_tables_and_original_crockery(self):
        self.assertNotIn('OwnerFieldDiningTable(room,',self.services)
        self.assertIn('Assets/PlayableGenerated/Oboz/ServiceModels/',self.services)
        self.assertIn('data.furniture',self.services)
        self.assertIn('DiningPlaceSetting(room,table,at,sourceSet)',self.services)
        self.assertIn('UseOriginalContactSurface(tray)',self.services)
        self.assertIn('PlaceOrRemove(utensil,tray.transform',self.services)
        self.assertIn('"24-piece-cutlery-set.glb"',self.services)
        self.assertIn('"food_tray.glb"',self.services)
        self.assertIn('"stockpot.glb"',self.services)
        self.assertIn('"Plate_Regular.001_"',self.services)

    def test_real_grass_01_02_and_green_roundabout(self):
        self.assertNotIn('Model(Source+"grass_bermuda_01_seedling_d__unity.glb"',self.vegetation)
        self.assertIn('id="grass_medium_0"+variant+"_2k"',self.vegetation)
        self.assertIn('OwnerGrassMediumPbr(variant)',self.vegetation)
        self.assertIn('AddRondoGrass(grass);',self.vegetation)
        self.assertIn('OwnerGrassMediumPbr(int variant)',self.materials)
        self.assertIn('BraxObozMaterials.PublicPbr("Grass001",1.4f)',self.campus)
        self.assertIn('Disc(t,"Zielona wyspa centralna',self.campus)

    def test_training_24_original_bags_and_2_tracks(self):
        self.assertIn('Bags(-23, -40, true,10)',self.environment)
        self.assertIn('Bags(23, -40, false,12)',self.environment)
        self.assertIn('Bags(12,51,false,2)',self.campus)
        self.assertIn('BraxObozBagAssembly.Prepare(OwnerBagPrototype(0))',self.environment)
        self.assertIn('BraxObozBagAssembly.Create(',self.environment)
        self.assertIn('const float actualLengthMetres=100f;', (ROOT/'unity-city/Assets/Editor/Oboz/BraxObozTrackGeometry.cs').read_text())
        self.assertIn('OwnerTrackPbr()', (ROOT/'unity-city/Assets/Editor/Oboz/BraxObozTrackGeometry.cs').read_text())
        self.assertIn('IntersectsRunningTrack(',self.campus)
        self.assertNotIn('OwnerTrackPedestrianCrossing(t);',self.campus)
        self.assertNotIn('vehicleCrosswalkZ',self.campus)

    def test_original_murals_and_bathroom(self):
        self.assertIn('OriginalMural(pavilion,"WOJOWNIKOW"',self.environment)
        self.assertIn('OriginalMural(pavilion,"FIGHT_ZONE"',self.environment)
        self.assertIn('OriginalMural(cabin3,"DROGA_MISTRZA"',self.environment)
        self.assertIn('OriginalMural(gym,"TRAINING_CAMP"',self.environment)
        for model in ('close-coupled-toilet.glb','wall-urinal.glb',
                      'trough-washbasin.glb','shower-tray-square.glb'):
            self.assertIn(model,self.services)
        self.assertIn('PublicPbr("red_brick_03",1f)',self.services)
        self.assertIn('PublicPbr("long_white_tiles"',self.services)
        self.assertIn('OwnerSlabTilesPbr()',self.services)
        self.assertIn('OwnerMudForestPbr()',self.materials)

    def test_uploads_verified_before_full_unity(self):
        self.assertIn('NAME = "oboz_silownia_opony.zip"',self.delivery)
        self.assertIn('BED_NAME = "ikea_idanas_single_bed.glb"',self.delivery)
        self.assertIn('BED_SHA256 =',self.delivery)
        self.assertIn('owner_extra_assets.py preflight',self.workflow)
        self.assertIn('owner_extra_assets.py restore',self.workflow)
        self.assertIn('owner_blender_export.py',self.workflow)

if __name__=="__main__":
    unittest.main()
