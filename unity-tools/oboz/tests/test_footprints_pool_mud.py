"""Source contracts for a reloadable, bounded footprint effect; not a Play Mode test."""
from pathlib import Path
import re
import unittest

ROOT=Path(__file__).resolve().parents[3]
A=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozActors.cs").read_text("utf-8")
T=(ROOT/"unity-city/Assets/Oboz/BraxObozFootprintTrail.cs").read_text("utf-8")
SHADER_PATH=ROOT/"unity-city/Assets/Oboz/Resources/BraxObozFootprint.shader"
S=SHADER_PATH.read_text("utf-8")


def method(source,name):
    match=re.search(r"\b"+re.escape(name)+r"\s*\([^)]*\)\s*\{",source)
    if not match:raise AssertionError("Missing method "+name)
    start=match.end();depth=1
    for i in range(start,len(source)):
        depth+=(source[i]=="{")-(source[i]=="}")
        if depth==0:return source[start:i]
    raise AssertionError("Unclosed method "+name)


class FootprintsContract(unittest.TestCase):
    def test_owner_pool_and_expiry_limits(self):
        pool=re.search(r"const\s+int\s+PoolSize\s*=\s*(\d+)",T)
        lifetime=re.search(r"const\s+float\s+LifetimeSeconds\s*=\s*([\d.]+)f",T)
        self.assertEqual(int(pool[1]),40)
        self.assertEqual(float(lifetime[1]),20)
        self.assertRegex(T,r"new\s+Mark\s*\[\s*PoolSize\s*\]")
        self.assertRegex(T,r"cursor\s*=\s*\(cursor\+1\)\s*%\s*PoolSize")
        self.assertRegex(T,r"age\s*>=\s*LifetimeSeconds")

    def test_saved_scene_references_and_fresh_runtime_tracking(self):
        self.assertRegex(T,r"\[SerializeField\]\s*(?:private\s+)?Transform\[\]\s+actors")
        start=method(T,"Start")
        self.assertIn("ResetTracking()",start)
        self.assertIn("GetComponent<BraxObozSceneInfo>()",start)
        reset=method(T,"ResetTracking")
        self.assertIn("actors[i].position",reset)
        self.assertIn("GetComponent<CharacterController>()",reset)
        self.assertIn("GetComponent<BraxObozIdleFootContact>()",reset)
        self.assertIn("AddComponent<BraxObozFootprintTrail>().Configure(player,npcs)",A)

    def test_soil_mask_and_nearest_nonalloc_contact(self):
        surface=method(T,"IsSoftSurface")
        self.assertIn("hit.triangleIndex",surface)
        self.assertIn("hit.barycentricCoordinate",surface)
        self.assertIn(".b",surface)
        self.assertIn("piasek",method(T,"IsSoftName"))
        self.assertIn("sand_",method(T,"IsSoftName"))
        hit=method(T,"FootHit")
        self.assertIn("Physics.RaycastNonAlloc",hit)
        self.assertIn("nearest",hit)
        self.assertIn("MaxContactGap",hit)
        self.assertIn("IsSoftSurface(ground)",hit)
        tick=method(T,"LateUpdate")
        for allocation in ("RaycastAll",".Where(",".ToArray(","new Material(","FindObjects"):
            self.assertNotIn(allocation,tick)
        self.assertIn("isGrounded",tick)
        self.assertIn("TryGetSolePosition",tick)

    def test_shared_upward_mesh_and_shader_fade_survive_build(self):
        quad=method(T,"BuildQuad")
        points=[tuple(float(v.removesuffix('f')) for v in m.split(','))
                for m in re.findall(r"new Vector3\(([-.\df,]+)\)",quad)]
        self.assertEqual(len(points),4)
        a,b,c=points[:3]
        ab=[b[i]-a[i] for i in range(3)];ac=[c[i]-a[i] for i in range(3)]
        self.assertGreater(ab[2]*ac[0]-ab[0]*ac[2],0,"Top-facing winding")
        self.assertIn('Resources.Load<Shader>("BraxObozFootprint")',T)
        self.assertIn("UNITY_ACCESS_INSTANCED_PROP(Stamp,_Color)",S)
        self.assertRegex(S,r"tex2D\(_MainTex,input\.uv\)\s*\*\s*tint")
        self.assertIn("Blend SrcAlpha OneMinusSrcAlpha",S)
        self.assertTrue(Path(str(SHADER_PATH)+".meta").is_file())
        self.assertIn("mark.properties.SetColor(TintId,StampTint)",method(T,"Spawn"))
        self.assertIn("mark.renderer.SetPropertyBlock",method(T,"LateUpdate"))
        self.assertIn("Destroy(stampMesh)",method(T,"OnDestroy"))
