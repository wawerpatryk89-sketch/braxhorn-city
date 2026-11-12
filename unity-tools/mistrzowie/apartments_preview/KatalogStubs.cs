using UnityEngine;
// the catalogue (BraxKatalog: the furniture and the working BRAXHORN TVs of the flats) needs the harbour's plain surface and
// the TV component: preview stand-ins (the material's colour / gloss / emission only; the TV keeps its fields)
public static class BraxChampionsHarbour
{
    public static Material Surface(string id, Color c, float rough, float metal, Color? emission = null, bool transparent = false)
        => new Material { name = "Champions harbour " + id, color = c, roughness = rough, metallic = metal, emission = emission ?? Color.black };
    public static Material Nakladka(Material m) => m;     // the depth offset of paint: nothing to draw differently in the preview
}
public sealed class BraxTelewizor : MonoBehaviour { public bool wlaczony = true, pion, maMiejsce; public float zasieg = 14f, zasiegPrzycisku = 3.2f; public Material obraz, wylaczony; public int start; public Vector3 srodekLokalny, przodLokalny = Vector3.forward; }
