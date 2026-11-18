using System.Collections.Generic;
using UnityEngine;

// MIASTO R1 — PŁYWANIE I NURKOWANIE W MORZU MIASTA (18.11.2026). Właściciel 12:36: „mam nadzieję, że ta woda to jest woda, a nie
// pasek… Nie zanurkujesz… i widać nic”; 13:04: „tak, zrób tak” — przeniesienie pływania i nurkowania z gałęzi Mistrzów
// (BraxChampionsPodWoda + BraxChampionsPoolSwimVolume; wspólne BraxSwimmingZone i pływanie w BraxGymPlayer).
//  • BraxMiastoKapiel (osobny plik) — strefa wody z progiem głębokości; w czasie nurkowania podaje lustro obniżone.
//  • BraxMiastoNurek — jeden na scenę: przycisk NURKUJ nad SKOK w kolumnie CIOS (te same miary co dotykowe sterowanie; C / Ctrl
//    na klawiaturze) widoczny tylko w czasie pływania; gdy kamera jest pod lustrem wody strefy — zasłona ekranu w kolorze wody
//    morskiej (Braxhorn/MiastoPodWodaEkran), od spodu lustro wody rysuje shader wody (okno Snella); w tym czasie globalne
//    _BraxPodWoda* (MiastoPodWoda.cginc): shadery miasta i dna liczą światło w wodzie od kamery (nad wodą wyłączone — bez mgły).
// Po kamerach (kolejność 10000): stan „pod wodą” liczony z pozycji kamery w tej samej klatce.
[DefaultExecutionOrder(10000)]
public sealed class BraxMiastoNurek : MonoBehaviour
{
    /// <summary>Pochłanianie światła w wodzie morskiej na metr (RGB) — jak _Pochlanianie shadera wody HarbourWater.</summary>
    public static readonly Vector4 Pochlanianie = new Vector4(.45f, .09f, .075f, 0f);
    /// <summary>Światło rozproszone w wodzie (liniowe) — w miejsce światła straconego po drodze.</summary>
    public static readonly Vector4 Rozproszenie = new Vector4(.016f, .083f, .112f, 0f);
    static readonly int IdJest = Shader.PropertyToID("_BraxPodWodaJest"), IdLustro = Shader.PropertyToID("_BraxPodWodaLustro"),
                        IdPochl = Shader.PropertyToID("_BraxPodWodaPochlanianie"), IdRozpr = Shader.PropertyToID("_BraxPodWodaRozproszenie");
    static float ustawioneLustro = float.NaN;

    public Material ekran;
    public static float Glebokosc { get; private set; }
    public static bool Pod { get; private set; }
    internal static readonly List<BraxMiastoKapiel> Strefy = new List<BraxMiastoKapiel>();
    internal static float PlywaDo;
    const float W_Dol = 40f;   // o ile obniżone lustro w czasie nurkowania (dno zatrzymuje ciało dużo wcześniej)

    static BraxMiastoNurek jedyny;
    GameObject zaslona; Camera dlaKamery; bool trzyma; GUIStyle st; float stRozmiar;

    void OnEnable() { if (!jedyny) jedyny = this; }
    void OnDisable() { if (jedyny == this) { jedyny = null; Glebokosc = 0f; Pod = false; Globalne(float.NaN); } if (zaslona) zaslona.SetActive(false); }

    // globalne dla shaderów (MiastoPodWoda.cginc): lustro strefy, pod którym jest kamera; NaN = kamera nad wodą (wyłączone)
    static void Globalne(float lustro)
    {
        if (float.IsNaN(lustro) ? float.IsNaN(ustawioneLustro) : lustro == ustawioneLustro) return;
        ustawioneLustro = lustro;
        bool jest = !float.IsNaN(lustro);
        Shader.SetGlobalFloat(IdJest, jest ? 1f : 0f);
        Shader.SetGlobalFloat(IdLustro, jest ? lustro : -10000f);
        Shader.SetGlobalVector(IdPochl, Pochlanianie);
        Shader.SetGlobalVector(IdRozpr, Rozproszenie);
    }

    bool Plywa => Time.time < PlywaDo;

    // przycisk: nad SKOK w kolumnie CIOS (miary jak BraxTouchControls.Uklad), piksele ekranu, y w górę
    void Przycisk(out Vector2 srodek, out float r)
    {
        var g = BraxGui.BezpiecznyProstokat();
        var sa = new Rect(g.x, Screen.height - g.yMax, g.width, g.height);
        float s = Mathf.Min(Screen.width, Screen.height), k = BraxUstawienia.SkalaPrzyciskow;
        float m = s * .045f, gap = s * .024f * k, rC = s * .074f * k, rS = s * .056f * k;
        var cCios = new Vector2(sa.xMax - m - rC, sa.yMin + m + rC);
        var cSkok = cCios + new Vector2(Mathf.Cos(97f * Mathf.Deg2Rad), Mathf.Sin(97f * Mathf.Deg2Rad)) * (rC + gap + rS);
        r = rS * 1.05f;
        srodek = new Vector2(cCios.x, cSkok.y + rS + gap * 1.8f + r);
    }

    void Update()
    {
        if (jedyny != this) return;
        trzyma = false;
        if (Plywa)
        {
            Przycisk(out var c, out float r);
            float r2 = r * 1.25f * r * 1.25f;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled && (t.position - c).sqrMagnitude <= r2) trzyma = true;
            }
            if (Input.touchCount == 0 && Input.GetMouseButton(0) && ((Vector2)Input.mousePosition - c).sqrMagnitude <= r2) trzyma = true;
            if (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) trzyma = true;
        }
        Glebokosc = trzyma ? W_Dol : 0f;
    }

    void LateUpdate()
    {
        if (jedyny != this) return;
        var cam = Camera.main;
        bool pod = false; float lustro = float.NaN;
        if (cam)
        {
            var p = cam.transform.position;
            foreach (var z in Strefy)
            {
                if (!z || !z.waterSurface) continue;
                var b = z.Obszar;
                if (p.y >= z.waterSurface.position.y - .02f || p.y < b.min.y - 1f) continue;
                if (p.x < b.min.x - .5f || p.x > b.max.x + .5f || p.z < b.min.z - .5f || p.z > b.max.z + .5f) continue;
                pod = true; lustro = z.waterSurface.position.y; break;
            }
        }
        Pod = pod;
        Globalne(lustro);
        if (pod && ekran)
        {
            if (!zaslona || dlaKamery != cam) Zaslona(cam);
            if (!zaslona.activeSelf) zaslona.SetActive(true);
        }
        else if (zaslona && zaslona.activeSelf) zaslona.SetActive(false);
    }

    // kwadrat rysowany wprost w przestrzeni ekranu przez swój shader: cały ekran niezależnie od pola widzenia kamery
    void Zaslona(Camera cam)
    {
        if (zaslona) Destroy(zaslona);
        dlaKamery = cam;
        zaslona = new GameObject("pod wodą: zasłona ekranu");
        zaslona.transform.SetParent(cam.transform, false);
        var mesh = new Mesh { name = "pod wodą" };
        mesh.vertices = new[] { new Vector3(-1, -1, 0), new Vector3(-1, 1, 0), new Vector3(1, 1, 0), new Vector3(1, -1, 0) };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
        zaslona.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = zaslona.AddComponent<MeshRenderer>();
        r.sharedMaterial = ekran;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        zaslona.SetActive(false);
    }

    void OnGUI()
    {
        if (jedyny != this || !Plywa || Event.current.type != EventType.Repaint) return;
        Przycisk(out var c, out float r);
        var g = new Vector2(c.x, Screen.height - c.y);
        BraxGui.PrzyciskKolowy(g, r, null, trzyma, new Color(.20f, .62f, 1f, 1f));
        if (st == null || stRozmiar != r) { stRozmiar = r; st = BraxGui.Styl(Mathf.RoundToInt(r * .36f), true, TextAnchor.MiddleCenter); }
        BraxGui.Napis(new Rect(g.x - r, g.y - r, r * 2f, r * 2f), "NURKUJ", st, BraxGui.Biel);
    }
}
