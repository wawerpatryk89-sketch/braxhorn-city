using UnityEngine;
// the catalogue (BraxKatalog: the furniture and the working BRAXHORN TVs of the flats) needs the harbour's plain surface and
// the TV component: preview stand-ins (the material's colour / gloss / emission only; the TV keeps its fields)
public static class BraxChampionsHarbour
{
    public static Material Surface(string id, Color c, float rough, float metal, Color? emission = null, bool transparent = false)
        => new Material { name = "Champions harbour " + id, color = c, roughness = rough, metallic = metal, emission = emission ?? Color.black };
    public static Material Nakladka(Material m) => m;     // the depth offset of paint: nothing to draw differently in the preview
    // the villa's Riva: the marina's own GLB (tracked) where the preview runs beside it, placed as PlaceBoat: its long axis
    // along +z, 8.5 m, 17 % of its height under the water (-2.0), turned by yaw
    public static int PrywatnaLodz(Transform parent, string id, string kind, float x, float z, float yaw, float width, float accessX, float accessZ, float pierY)
    {
        const string path = "Assets/Source/HarborBeach/Models/bateau_riva_boat.glb";
        if (kind != "motorboat" || !System.IO.File.Exists(path)) return 0;
        float k = BraxClaudeOsiedle.K;
        var o = new GameObject(id + " | " + kind); o.transform.SetParent(parent, false);
        var pm = o.AddComponent<PreviewModel>(); pm.path = path; (pm.min, pm.max) = GltfBounds.Read(path);
        o.AddComponent<MeshRenderer>().sharedMaterial = new Material { name = "gltf", textured = true };
        Vector3 size = pm.max - pm.min; bool turn = size.x > size.z;
        float len = turn ? size.x : size.z, s = 8.5f * k / len;
        o.transform.rotation = Quaternion.Euler(0f, yaw + (turn ? -90f : 0f), 0f);
        o.transform.localScale = Vector3.one * s;
        var c = (pm.min + pm.max) * .5f;
        var off = o.transform.rotation * new Vector3(c.x * s, pm.min.y * s + size.y * s * .17f, c.z * s);
        o.transform.position = new Vector3(x * k, -2.0f * k, z * k) - off;
        return 1;
    }
}
public sealed class BraxTelewizor : MonoBehaviour { public bool wlaczony = true, pion, maMiejsce; public float zasieg = 14f, zasiegPrzycisku = 3.2f; public Material obraz, wylaczony; public int start; public Vector3 srodekLokalny, przodLokalny = Vector3.forward; }
