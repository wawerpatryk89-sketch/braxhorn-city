using UnityEngine;
// the catalogue (BraxKatalog: the furniture and the working BRAXHORN TVs of the flats) needs the harbour's plain surface and
// the TV component: preview stand-ins (the material's colour / gloss / emission only; the TV keeps its fields)
public static class BraxChampionsHarbour
{
    public static Material Surface(string id, Color c, float rough, float metal, Color? emission = null, bool transparent = false)
        => new Material { name = "Champions harbour " + id, color = c, roughness = rough, metallic = metal, emission = emission ?? Color.black };
}
public sealed class BraxTelewizor : MonoBehaviour { public bool wlaczony = true, pion; public float zasieg = 26f, zasiegPrzycisku = 2.8f; public Material obraz, wylaczony; public int start; }
