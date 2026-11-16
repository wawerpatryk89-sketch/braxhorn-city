// Zamienniki części miasta B i biblioteki potrzebnych podglądowi nowej części miasta (stałe układu — kopie z BraxRegionMiastoB.cs).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class BraxRegionMiastoB
{
    static float K => BraxClaudeOsiedle.K;
    public const float RX = -126f, RZ = 28.5f, AX = -126f, AH = 4.5f, Kr = .25f, Ziel = 2.25f, Chod = 5f, RPlac = 22f, PetlaZX = -240f;
    const float YA = .003f, YC = .15f;
    public static Transform root;
    static Material asfalt, plyty, granitPlac;
    public static void Materialy()
    {
        asfalt = BraxRegionMeble.Tex("Miasto B: asfalt (jak ulica główna)", 2.1f, new Color(.55f, .55f, .55f), .92f, "clean_asphalt", "asphalt_04");
        plyty = BraxRegionMeble.Tex("Miasto B: płyty chodnika", 1.4f, new Color(.80f, .77f, .72f), .88f, "concrete_pavers_02", "concrete_pavement_02");
        granitPlac = BraxRegionMeble.Tex("Miasto B: płyty granitowe placu CENTRUM", 1.2f, new Color(.66f, .65f, .63f), .85f, "granite_tile_02", "concrete_pavers_03", "concrete_pavement_02");
    }
    static readonly List<(Rect r, float top)> nawierzchnie = new List<(Rect, float)>();
    // płyta (prostopadłościan od top − grub do top) — jak BraxRegionMiastoB.Plyta (bez łuków narożników)
    static void Plyta(string n, Material m, float x0, float x1, float z0, float z1, float top, float grub = .3f, bool kolizja = true)
    {
        if (x1 - x0 < .01f || z1 - z0 < .01f) return;
        var b = new BraxRegionMeble.Bud(); b.MinMax(new Vector3(x0, top - grub, z0), new Vector3(x1, top, z1));
        b.Obiekt(n, root, m, false, false); nawierzchnie.Add((Rect.MinMaxRect(x0, z0, x1, z1), top));
    }
    public static void ChodnikPub(string n, float x0, float x1, float z0, float z1) => Chodnik(n, x0, x1, z0, z1);
    static void Chodnik(string n, float x0, float x1, float z0, float z1) => Plyta("Chodnik: " + n, plyty, x0, x1, z0, z1, YC, .3f - YA);
    static float Podloze(float x, float z, float dom)
    {
        float best = float.NegativeInfinity; foreach (var (r, top) in nawierzchnie) if (r.Contains(new Vector2(x, z)) && top > best) best = top;
        return float.IsNegativeInfinity(best) ? dom : best;
    }
    static void world_point(string id, string label, Vector3 p) => BraxUliceKit.Punkt(id, label, p);
    static void NdLog(string s) => Console.WriteLine("BRAXHORN_MIASTO_ASSETY " + s);
    static IEnumerable<Rect> Odejmij(Rect a, Rect b)
    {
        var p = Przetnij(a, b);
        if (!Niepuste(p)) { yield return a; yield break; }
        if (p.yMin > a.yMin) yield return Rect.MinMaxRect(a.xMin, a.yMin, a.xMax, p.yMin);
        if (p.yMax < a.yMax) yield return Rect.MinMaxRect(a.xMin, p.yMax, a.xMax, a.yMax);
        if (p.xMin > a.xMin) yield return Rect.MinMaxRect(a.xMin, p.yMin, p.xMin, p.yMax);
        if (p.xMax < a.xMax) yield return Rect.MinMaxRect(p.xMax, p.yMin, a.xMax, p.yMax);
    }
    static Rect Przetnij(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));
    static bool Niepuste(Rect r) => r.width > .005f && r.height > .005f;
    // wywołania dla podglądu (prywatne kroki generatora)
    public static List<(string, Rect)> PodgladPosadzki() { ndPosadzki = NdPosadzkiLokali(); return ndPosadzki; }
    public static void PodgladChodnikBez(string n, Rect a, IEnumerable<Rect> bez) => ChodnikBez(n, a, bez);
    public static void PodgladPlacAtlanty() => NdPlacAtlanty();
    public static void PodgladPlacUstawek() => NdPlacUstawek();
    public static void PodgladKomisNawierzchnia() => NdKomisNawierzchnia();
    public static void PodgladKomis() => NdKomis();
    public static void PodgladLokaleKontrola() => NdLokaleKontrola();
    public static (float x, float z, float w, float d) Budynek(string id) { var b = NoweBudynki.First(q => q.id == id); return (b.x, b.z, b.w, b.d); }
    public static void Plaski(string n, Material m, Rect r, float top) => Plyta(n, m, r.xMin, r.xMax, r.yMin, r.yMax, top, .3f - YA);
    public static Material Asfalt => asfalt; public static Material Plyty => plyty;
}

public sealed class BraxMiejsceZadania : MonoBehaviour { public string id = "", napis = ""; public float promien = 1.6f; }
public sealed class BraxPunktAkcji : MonoBehaviour { public string id = "", etykieta = "AKCJA", odpowiedz = ""; public float promien = 1.7f; public Transform mowca; public float wysDymka = 2.4f; }
public sealed class BraxObrazRejonu : MonoBehaviour { public float ekspozycja = 1f; public Light slonce; public float slonceZewnatrz = 1.24f, slonceWewnatrz = .75f; }

public static class BraxSzkloFasady
{
    public const string ShaderName = "Braxhorn/SzkloFasady";
    // podgląd: szyba elewacji = ciemne szkło odbijające (w grze shader z pokojem za szybą i odbiciem nieba)
    public static Material Mat(string nazwa, float rozstaw, float kondygnacja, float floor0, Color kolor, float a, float b, Material zapas)
    { var m = BraxUliceKit.Mat("szkło fasady: " + nazwa, new Color(kolor.r * .9f, kolor.g * .95f, kolor.b, 1f), .06f, .85f); return m; }
}
public static class BraxMiastoBillboardy { public static bool Mural(Transform g, string plik, Vector3 c, Vector3 n, float w, float h) => false; }   // grafiki firm właściciela (prywatne) — w podglądzie litery
public static class BraxTelewizory { public static void NaScianie(Transform r, string s, Vector3 p, Vector3 n, float k) { } }
public static class BraxMiastoFitness { public static void Buduj(Transform t, Vector3 p) { } }
public static class BraxMiastoMotory { public static bool Rower(Transform t, int i, Vector3 p, Vector3 n) => false; }
public static class BraxCityArchitecture
{
    public sealed class Building
    {
        public string id, label; public float x0, x1, z0, z1; public int floors; public bool far, side;
        public Building(string id, string label, float x0, float x1, float z0, float z1, int floors, bool far = false, bool side = false)
        { this.id = id; this.label = label; this.x0 = x0; this.x1 = x1; this.z0 = z0; this.z1 = z1; this.floors = floors; this.far = far; this.side = side; }
        public float Height => floors * 3.2f + 1f;
    }
    public static readonly Building[] StreetBuildings = new Building[0];
    public static GameObject Box(string n, Transform parent, Vector3 c, Vector3 size, Material m, bool kol) => null;
    public static Material SurfaceTexture(string n, string slug, string plik, float metry) => BraxUliceKit.Tex(n, metry, null, new Color(.7f, .45f, .35f), .9f, slug);
}
public static class BraxCityStreetSkyline { public sealed class TowerPlan { public string id; } public static readonly TowerPlan[] HighRises = new TowerPlan[0]; }

public sealed class BraxObrot : MonoBehaviour { public float stopniNaSekunde = 30f, zasieg = 40f; public Vector3 os = Vector3.up; public bool osSwiata = true; }
public static class BraxCityMeshCut { public static int Subtract(Transform t, Bounds b, string a, string c) => 0; }
public static partial class BraxRegionFawela { internal static int trojkatow; }

public static partial class BraxRegionMeble
{
    // jak w grze (BraxRegionMeble.Model): model Poly Haven / biblioteki o zadanej wysokości rzeczywistej × K
    public static GameObject Model(Transform parent, string etykieta, Vector3 p, float yaw, float wysRealna, bool kolizja, params string[] ids)
    {
        var src = BraxRegion.Model(ids); if (!src) return null;
        var o = BraxRegion.Put(parent, src, etykieta, p, yaw, 1f, false, wysRealna);
        if (o && kolizja) { var b = BraxRegion.Bounds(o); Kolizja(parent, etykieta + " kolizja", b.center, b.size); }
        return o;
    }

    public static Material Tex(string nazwa, float realneMetry, Color tint, float szorstkosc, params string[] ids) => BraxUliceKit.Tex(nazwa, realneMetry, tint, tint, szorstkosc, ids);
    public static Material Swiecacy(string nazwa, Color c, float moc) { var m = BraxUliceKit.Mat("świeci: " + nazwa, c, .4f); m.SetColor("_EmissionColor", c * moc); m.rodzaj = "swieci"; return m; }
}

internal static partial class BraxUliceKit
{
    public static void Usun(GameObject g, string opis) { }
    public static void WyczyscOkna() { }
    public static Material TexPlik(string nazwa, string katalog, string slug, float metry, Color? odcien, Color zamiennik, float szorstkosc) => Tex(nazwa, metry, odcien, zamiennik, szorstkosc, slug == "nova_skora_pikowana" ? "brown_leather" : slug);
    public static Material OknoPokoj(float rozstaw, float kondygnacja, float floor0, int wariant0, int wariantow, float glebokosc, float offsetU) => Mat("okno: pokój za szybą (podgląd)", new Color(.16f, .19f, .22f), .08f, .6f);
    public static readonly List<BraxCityPoint> Punkty = new List<BraxCityPoint>();
    public static void Punkt(string id, string opis, Vector3 gdzie)
    {
        Punkty.RemoveAll(p => p.id == id); var q = new BraxCityPoint { id = id, label = opis, position = gdzie }; Punkty.Add(q);
        var w = UnityEngine.Object.FindAnyObjectByType<BraxCityWorld>(); if (w != null) w.points = Punkty.ToArray();
    }
}

public static class BraxMiastoAAkcje
{
    public static readonly List<(string id, string etykieta, string odpowiedz, Vector3 p)> Lista = new List<(string, string, string, Vector3)>();
}

public static partial class BraxMiastoAssety
{
    static float K => BraxClaudeOsiedle.K;
    // obrysy modeli właściciela (glb, metry) — dla obliczeń położenia w kodzie gry (BraxRegion.Bounds)
    static readonly Dictionary<string, Vector3> Obrysy = new Dictionary<string, Vector3>
    {
        { "Braxhorn_Coupe_BX10013", new Vector3(2.30f, 1.30f, 4.73f) }, { "Braxhorn_Graphite_Coupe_BX10014", new Vector3(2.18f, 1.31f, 4.71f) },
        { "Braxhorn_GT6_Coupe_BX10007", new Vector3(2.15f, 1.34f, 4.77f) }, { "Braxhorn_Roadster_BX10009", new Vector3(2.21f, 1.26f, 4.46f) },
        { "Braxhorn_S2_Spider_BX10008", new Vector3(2.21f, 1.19f, 4.49f) },
    };
    public static GameObject Postaw(Transform parent, GameObject zrodlo, string nazwa, Vector3 p, float yaw, float skalaRealna = 1f, bool statyczny = true, bool cienie = true)
    {
        if (!zrodlo) return null;
        var o = new GameObject(nazwa); o.transform.SetParent(parent, false); o.transform.position = p; o.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        float s = skalaRealna * K; string plik = zrodlo.name.Substring(zrodlo.name.LastIndexOf('/') + 1);
        Rejestr.Modele.Add(new Rejestr.Model { id = zrodlo.name, nazwa = nazwa, floor = p, yaw = yaw, scale = s, go = o });
        if (Obrysy.TryGetValue(plik, out var r)) BraxRegion.Rozmiary[o] = r * s;
        return o;
    }
    public static BoxCollider Kolizja(Transform parent, GameObject o, string nazwa, float wezej = 0f)
    {
        var b = BraxRegion.Bounds(o); var c = new GameObject(nazwa).AddComponent<BoxCollider>(); c.transform.SetParent(parent, false);
        c.transform.position = b.center; c.size = new Vector3(Mathf.Max(.05f, b.size.x - wezej * 2f), b.size.y, Mathf.Max(.05f, b.size.z - wezej * 2f)); return c;
    }
    // kontener 20′: w grze model właściciela (paczka miasto_statki2, tylko w CI) — w podglądzie zamiennik z kodu portu (BraxMiastoPort.Kontener)
    public static GameObject Kontener20(Transform parent, Vector3 podstawa, float yaw, int odcien)
    {
        float L = 6.06f * K, B = 2.44f * K, H = 2.59f * K; bool wzdluzZ = Mathf.Abs(Mathf.DeltaAngle(yaw, 90f)) < 45f;
        var g = new GameObject("Kontener 20′ (podgląd: zamiennik z kodu portu)"); g.transform.SetParent(parent, false); g.transform.position = podstawa;
        var blacha = BraxUliceKit.Tex("kontener: blacha rdzawa (podgląd)", 1.4f, new Color(.62f, .38f, .26f), new Color(.45f, .25f, .16f), .7f, "corrugated_iron", "metal_plate");
        var ciemna = BraxUliceKit.Mat("kontener: naroża (podgląd)", new Color(.12f, .12f, .13f), .55f, .5f);
        var s = new UlSiatka(); var d = new UlSiatka();
        float hx = (wzdluzZ ? B : L) * .5f, hz = (wzdluzZ ? L : B) * .5f; var c = Vector3.zero;   // siatka w układzie kontenera (obiekt g stoi w podstawie)
        s.Box(new Vector3(c.x - hx + .04f, c.y + .03f, c.z - hz + .04f), new Vector3(c.x + hx - .04f, c.y + H - .03f, c.z + hz - .04f));          // korpus
        int zebra = Mathf.FloorToInt(L / .55f);
        for (int i = 1; i < zebra; i++)
        {
            float t = -L * .5f + i * L / zebra;
            if (!wzdluzZ) foreach (float sg in new[] { -1f, 1f }) s.Box(new Vector3(c.x + t - .07f, c.y + .12f, c.z + sg * hz - (sg > 0 ? .04f : -.04f) - .0f), new Vector3(c.x + t + .07f, c.y + H - .12f, c.z + sg * (hz + .0f)));
            else foreach (float sg in new[] { -1f, 1f }) s.Box(new Vector3(c.x + sg * hx - (sg > 0 ? .04f : -.04f), c.y + .12f, c.z + t - .07f), new Vector3(c.x + sg * hx, c.y + H - .12f, c.z + t + .07f));
        }
        foreach (float a in new[] { -1f, 1f }) foreach (float b in new[] { -1f, 1f }) foreach (float yy in new[] { 0f, H - .16f })
                    d.Box(new Vector3(c.x + a * hx - (a > 0 ? .18f : 0f), c.y + yy, c.z + b * hz - (b > 0 ? .18f : 0f)), new Vector3(c.x + a * hx + (a > 0 ? 0f : .18f), c.y + yy + .16f, c.z + b * hz + (b > 0 ? 0f : .18f)));
        BraxMiastoA.Obiekt(g.transform, "kontener: blacha", s, blacha); BraxMiastoA.Obiekt(g.transform, "kontener: naroża", d, ciemna);
        BraxRegion.Rozmiary[g] = new Vector3(wzdluzZ ? B : L, H, wzdluzZ ? L : B);
        return g;
    }
}


// winda z biurowców Tyłów B (BraxRegionTylyB.Winda — w grze BraxWinda z kabiną i panelem): w podglądzie szyb z murem, drzwi przystanków
// zamknięte (stal szczotkowana) i opaski — wymiary jak w grze (kabina 1.6 × 1.4 m × K, drzwi 1.1 × 2.1 m × K)
public static class BraxRegionTylyB
{
    static float K => BraxUliceKit.K; static float KabW => 1.6f * K; static float KabD => 1.4f * K; static float DrzW => 1.1f * K; static float DrzH => 2.1f * K;
    internal static Vector2 SzybPolowy => new Vector2(KabW * .5f + .1f + .25f, KabD * .5f + .1f + .25f);
    internal static Vector2 DrzwiWindy => new Vector2(DrzW, DrzH);
    internal static void WindaZewn(Transform T, string nazwa, Vector2 szyb, float[] pY, bool[] uz, string[] nazwy, float top, float f)
    {
        var W = new GameObject("Winda " + nazwa).transform; W.SetParent(T, false);
        float sx = szyb.x, sz = szyb.y, hw = KabW * .5f + .1f, hd = KabD * .5f + .1f, t = .25f;
        var sc = new BraxRegionMeble.Bud(); var st = new BraxRegionMeble.Bud();
        void BoxZ(BraxRegionMeble.Bud bb, float x0, float x1, float y0, float y1, float dz0, float dz1) => bb.MinMax(new Vector3(x0, y0, Mathf.Min(sz + f * dz0, sz + f * dz1)), new Vector3(x1, y1, Mathf.Max(sz + f * dz0, sz + f * dz1)));
        BoxZ(sc, sx - hw - t, sx + hw + t, pY[0], top, -hd - t, -hd); BoxZ(sc, sx - hw - t, sx - hw, pY[0], top, -hd, hd); BoxZ(sc, sx + hw, sx + hw + t, pY[0], top, -hd, hd);
        for (int i = 0; i < pY.Length; i++)
        {
            float y0 = pY[i], y1 = i == pY.Length - 1 ? top : pY[i + 1];
            BoxZ(sc, sx - hw - t, sx - DrzW * .5f - .05f, y0, y1, hd, hd + t); BoxZ(sc, sx + DrzW * .5f + .05f, sx + hw + t, y0, y1, hd, hd + t);
            BoxZ(sc, sx - DrzW * .5f - .05f, sx + DrzW * .5f + .05f, y0 + DrzH + .1f, y1, hd, hd + t);
            BoxZ(st, sx - DrzW * .5f - .05f, sx + DrzW * .5f + .05f, y0 + .004f, y0 + DrzH + .1f, hd + .05f, hd + .09f);                                   // skrzydła zamknięte
            BoxZ(st, sx - DrzW * .5f - .16f, sx - DrzW * .5f - .05f, y0, y0 + DrzH + .2f, hd + t, hd + t + .06f); BoxZ(st, sx + DrzW * .5f + .05f, sx + DrzW * .5f + .16f, y0, y0 + DrzH + .2f, hd + t, hd + t + .06f);
            BoxZ(st, sx - DrzW * .5f - .16f, sx + DrzW * .5f + .16f, y0 + DrzH + .1f, y0 + DrzH + .25f, hd + t, hd + t + .06f);
            BoxZ(st, sx + DrzW * .5f + .35f, sx + DrzW * .5f + .55f, y0 + 1.0f * K, y0 + 1.35f * K, hd + t, hd + t + .03f);                           // kaseta przywołania
        }
        sc.Obiekt("Szyb windy " + nazwa, W, BraxCityMaterials.Surface("Wnętrza B: tynk szybu", new Color(.86f, .85f, .82f), .9f), false, true);
        st.Obiekt("Winda " + nazwa + ": skrzydła i opaski (stal)", W, BraxCityMaterials.Surface("Wnętrza B: stal nierdzewna szczotkowana", new Color(.70f, .71f, .72f), .35f, .85f), false, false);
    }
}
