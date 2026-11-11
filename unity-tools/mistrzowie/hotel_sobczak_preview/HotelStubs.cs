// Hotel-only stand-ins: runtime components with the SAME public fields as the real scripts
// (Assets/Claude/Runtime/BraxWinda.cs, BraxDrzwiAutomatyczne.cs, Assets/Mistrzowie/BraxHotelSobczakFloorStreamer.cs).
using UnityEngine;

public static class BraxCityMaterials
{
    public const string ShaderName = "Braxhorn/CityStaticPBR";
    public static readonly Vector3 SunDirection = new Vector3(-.45f, .78f, -.42f).normalized;
}
public sealed class BraxWinda : MonoBehaviour
{
    public string nazwa = "WINDA";
    public Transform kabina;
    public Transform kabinaL, kabinaP;
    public float[] pietraY = new float[0];
    public string[] pietraNazwy = new string[0];
    public bool[] uzytkowe = new bool[0];
    public Transform[] przystanekL = new Transform[0], przystanekP = new Transform[0];
    public Vector3[] wezwanie = new Vector3[0];
    public Vector2 szyb;
    public Vector2 wnetrze = new Vector2(.9f, .9f);
    public float przesuw = .75f;
    public int start;
}
public sealed class BraxDrzwiAutomatyczne : MonoBehaviour
{
    public Transform skrzydloL, skrzydloP;
    public float przesuw = 1.0f;
    public float zasieg = 2.6f;
    public string nazwa = "WEJŚCIE";
}
public sealed class BraxHotelSobczakFloorStreamer : MonoBehaviour
{
    public GameObject[] guestInteriors = new GameObject[0];
    public GameObject[] basementInteriors = new GameObject[0];
    public float metreScale = 1f;
    public float guestBaseY = 9.6f;
    public float floorHeight = 3.6f;
    public float halfWidth = 32f;
    public float halfDepth = 21f;
    public int neighbourFloors = 1;
}

// the catalogue (BraxKatalog: the working BRAXHORN TVs in the rooms) needs the harbour's plain surface and the TV component:
// preview stand-ins (the material's colour / gloss / emission only; the TV keeps its fields)
public static class BraxChampionsHarbour
{
    public static Material Surface(string id, Color c, float rough, float metal, Color? emission = null, bool transparent = false)
        => new Material { name = "Champions harbour " + id, color = c, roughness = rough, metallic = metal, emission = emission ?? Color.black };
}
public sealed class BraxTelewizor : MonoBehaviour { public bool wlaczony = true, pion; public float zasieg = 26f, zasiegPrzycisku = 2.8f; public Material obraz, wylaczony; public int start; }
