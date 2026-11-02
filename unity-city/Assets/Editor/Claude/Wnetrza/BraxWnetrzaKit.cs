using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// WNĘTRZA MIASTA (2.11, wykonawca „wnętrza”) — wspólne narzędzia: metro, lobby i taras Tower, NOVA, Nutrition, mieszkanie
// Braxa, sklep BRAXHORN SPORT. Zasady: skala Braxa k = 1.294 (BraxClaudeOsiedle.K) dla mebli, drzwi, poręczy, stopni;
// posadzki matowe (gładkość ≤ 0.5 → szorstkość ≥ 0.5); siatki ZAWSZE jako pliki (Assets/PlayableGenerated/ClaudeRegion/
// Wnetrza); łączenie po materiale w komórkach 14 m (każda komórka dostaje swoje 4 najbliższe lampy ForceVertex —
// CityStaticPBR liczy do 4 lamp na obiekt); wnętrza bez bezpośredniego słońca (znacznik pieczenia w UV3: słońce 0).
public static class BraxWnetrzaKit
{
    public const string Out = "Assets/PlayableGenerated/ClaudeRegion/Wnetrza";
    public const string Src = "Assets/Source/Claude/Wnetrza/";
    public const string PH = "Assets/Source/Claude/PolyHaven/textures/";
    public static float K => BraxClaudeOsiedle.K;

    // ---------------- partie siatek ----------------
    sealed class Partia
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int> t = new List<int>();
    }
    struct Klucz : IEquatable<Klucz>
    {
        public Transform g; public Material m; public int cx, cy, cz; public float slonce;
        public bool Equals(Klucz o) => g == o.g && m == o.m && cx == o.cx && cy == o.cy && cz == o.cz && slonce == o.slonce;
        public override bool Equals(object o) => o is Klucz k && Equals(k);
        public override int GetHashCode() => (g ? g.GetInstanceID() : 0) ^ (m ? m.GetInstanceID() * 397 : 0) ^ (cx * 7919) ^ (cy * 104729) ^ (cz * 1299709) ^ slonce.GetHashCode();
    }
    static readonly Dictionary<Klucz, Partia> partie = new Dictionary<Klucz, Partia>();
    static int seria;

    /// <summary>Grupa (rodzic) dla następnych brył. Bryły trafiają do partii (grupa, materiał, komórka).</summary>
    public static Transform Grupa;
    /// <summary>Wielkość komórki łączenia (m). Mniejsza = lepsze rozłożenie lamp, więcej wywołań rysowania.</summary>
    public static float Komorka = 14f;
    /// <summary>true = wnętrze (bez bezpośredniego słońca), false = na zewnątrz (słońce jak reszta miasta).</summary>
    public static bool Wnetrze = true;
    /// <summary>Otoczenie (0..1) zapisywane w UV3 (CityStaticPBR: ambientAO = lerp(.4,1,y)).</summary>
    public static float Otoczenie = 1f;
    public static int Bryly, Trojkaty, Kolizje;

    public static void Start(Transform grupa) { Grupa = grupa; Wnetrze = true; Otoczenie = 1f; Komorka = 14f; }

    static Partia P(Material m, Vector3 srodek)
    {
        if (!m) m = Mat.Plain("wnętrza: brak materiału", Color.magenta, .9f);
        var k = new Klucz { g = Grupa, m = m, cx = Mathf.FloorToInt(srodek.x / Komorka), cy = Mathf.FloorToInt(srodek.y / 8f), cz = Mathf.FloorToInt(srodek.z / Komorka), slonce = Wnetrze ? -1f - Otoczenie : Otoczenie };
        if (!partie.TryGetValue(k, out var p)) partie[k] = p = new Partia();
        return p;
    }

    static Vector2 Planar(Vector3 p, Vector3 n)
    {
        float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
        if (ay >= ax && ay >= az) return new Vector2(p.x, p.z);
        // u rośnie w prawo patrząc na ścianę (prawo = n × góra), v w górę — napisy i fugi nie są lustrzane
        if (ax >= az) return new Vector2(p.z * Mathf.Sign(n.x), p.y);
        return new Vector2(-p.x * Mathf.Sign(n.z), p.y);
    }

    /// <summary>Czworokąt (a,b,c,d przeciwnie do wskazówek patrząc od frontu). UV planarne w metrach świata.</summary>
    public static void Quad(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        var nrm = Vector3.Cross(b - a, c - a).normalized;
        var p = P(m, (a + b + c + d) * .25f); int s = p.v.Count;
        p.v.Add(a); p.v.Add(b); p.v.Add(c); p.v.Add(d);
        for (int i = 0; i < 4; i++) p.n.Add(nrm);
        p.uv.Add(Planar(a, nrm)); p.uv.Add(Planar(b, nrm)); p.uv.Add(Planar(c, nrm)); p.uv.Add(Planar(d, nrm));
        p.t.Add(s); p.t.Add(s + 1); p.t.Add(s + 2); p.t.Add(s); p.t.Add(s + 2); p.t.Add(s + 3);
        Trojkaty += 2;
    }

    /// <summary>Czworokąt z jawnymi UV (atlas, plakat, ekran).</summary>
    public static void QuadUV(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, bool dwustronny = false)
    {
        var nrm = Vector3.Cross(b - a, c - a).normalized;
        var p = P(m, (a + b + c + d) * .25f); int s = p.v.Count;
        p.v.Add(a); p.v.Add(b); p.v.Add(c); p.v.Add(d);
        for (int i = 0; i < 4; i++) p.n.Add(nrm);
        p.uv.Add(ua); p.uv.Add(ub); p.uv.Add(uc); p.uv.Add(ud);
        p.t.Add(s); p.t.Add(s + 1); p.t.Add(s + 2); p.t.Add(s); p.t.Add(s + 2); p.t.Add(s + 3);
        if (dwustronny) { p.t.Add(s); p.t.Add(s + 2); p.t.Add(s + 1); p.t.Add(s); p.t.Add(s + 3); p.t.Add(s + 2); }
        Trojkaty += 2;
    }

    /// <summary>Prostopadłościan osiowy (środek, rozmiar). kolizja = BoxCollider.</summary>
    public static void Box(Material m, Vector3 c, Vector3 s, bool kolizja = false) => OBox(m, c, s, Quaternion.identity, kolizja);

    public static void MM(Material m, float x0, float x1, float y0, float y1, float z0, float z1, bool kolizja = false)
        => Box(m, new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f), new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), Mathf.Abs(z1 - z0)), kolizja);

    /// <summary>Prostopadłościan obrócony. bezDna = bez ściany dolnej (oszczędność na meblach stojących).</summary>
    public static void OBox(Material m, Vector3 c, Vector3 s, Quaternion q, bool kolizja = false, bool bezDna = false)
    {
        Vector3 h = s * .5f;
        Vector3 X = q * new Vector3(h.x, 0, 0), Y = q * new Vector3(0, h.y, 0), Z = q * new Vector3(0, 0, h.z);
        Vector3 P0(int sx, int sy, int sz) => c + X * sx + Y * sy + Z * sz;
        Quad(m, P0(-1, -1, 1), P0(1, -1, 1), P0(1, 1, 1), P0(-1, 1, 1));      // +Z
        Quad(m, P0(1, -1, -1), P0(-1, -1, -1), P0(-1, 1, -1), P0(1, 1, -1));  // -Z
        Quad(m, P0(-1, -1, -1), P0(-1, -1, 1), P0(-1, 1, 1), P0(-1, 1, -1));  // -X
        Quad(m, P0(1, -1, 1), P0(1, -1, -1), P0(1, 1, -1), P0(1, 1, 1));      // +X
        Quad(m, P0(-1, 1, 1), P0(1, 1, 1), P0(1, 1, -1), P0(-1, 1, -1));      // +Y
        if (!bezDna) Quad(m, P0(-1, -1, -1), P0(1, -1, -1), P0(1, -1, 1), P0(-1, -1, 1));
        Bryly++;
        if (kolizja) Kolizja(c, s, q);
    }

    /// <summary>Rura / pręt między dwoma punktami (poręcze, nogi, słupki).</summary>
    public static void Rura(Material m, Vector3 a, Vector3 b, float r, int boki = 10, bool zaslepki = true)
    {
        var os = (b - a); if (os.sqrMagnitude < 1e-8f) return; os.Normalize();
        var u = Vector3.Cross(os, Mathf.Abs(os.y) > .95f ? Vector3.forward : Vector3.up).normalized;
        var w = Vector3.Cross(os, u);
        var p = P(m, (a + b) * .5f);
        for (int i = 0; i < boki; i++)
        {
            float t0 = 2 * Mathf.PI * i / boki, t1 = 2 * Mathf.PI * (i + 1) / boki;
            Vector3 n0 = u * Mathf.Cos(t0) + w * Mathf.Sin(t0), n1 = u * Mathf.Cos(t1) + w * Mathf.Sin(t1);
            int s = p.v.Count; float L = Vector3.Distance(a, b);
            p.v.Add(a + n0 * r); p.v.Add(a + n1 * r); p.v.Add(b + n1 * r); p.v.Add(b + n0 * r);
            p.n.Add(n0); p.n.Add(n1); p.n.Add(n1); p.n.Add(n0);
            float uu0 = (float)i / boki * 2 * Mathf.PI * r, uu1 = (float)(i + 1) / boki * 2 * Mathf.PI * r;
            p.uv.Add(new Vector2(uu0, 0)); p.uv.Add(new Vector2(uu1, 0)); p.uv.Add(new Vector2(uu1, L)); p.uv.Add(new Vector2(uu0, L));
            p.t.Add(s); p.t.Add(s + 1); p.t.Add(s + 2); p.t.Add(s); p.t.Add(s + 2); p.t.Add(s + 3);
            if (zaslepki)
            {
                int c0 = p.v.Count;
                p.v.Add(a); p.v.Add(a + n1 * r); p.v.Add(a + n0 * r); for (int k = 0; k < 3; k++) { p.n.Add(-os); p.uv.Add(Vector2.zero); }
                p.t.Add(c0); p.t.Add(c0 + 1); p.t.Add(c0 + 2);
                int c1 = p.v.Count;
                p.v.Add(b); p.v.Add(b + n0 * r); p.v.Add(b + n1 * r); for (int k = 0; k < 3; k++) { p.n.Add(os); p.uv.Add(Vector2.zero); }
                p.t.Add(c1); p.t.Add(c1 + 1); p.t.Add(c1 + 2);
            }
            Trojkaty += zaslepki ? 4 : 2;
        }
    }

    /// <summary>Walec pionowy (podstawa w punkcie, promień, wysokość) z dekielkami.</summary>
    public static void Walec(Material m, Vector3 podstawa, float r, float h, int boki = 16, bool kolizja = false)
    {
        Rura(m, podstawa, podstawa + Vector3.up * h, r, boki, true);
        if (kolizja) Kolizja(podstawa + Vector3.up * h * .5f, new Vector3(r * 1.6f, h, r * 1.6f), Quaternion.identity);
    }

    /// <summary>Kula (lampa, kula lustrzana, gałka) — siatka UV.</summary>
    public static void Kula(Material m, Vector3 c, float r, int poludniki = 16, int rownolezniki = 10)
    {
        var p = P(m, c);
        int s = p.v.Count;
        for (int j = 0; j <= rownolezniki; j++)
        {
            float th = Mathf.PI * j / rownolezniki;
            for (int i = 0; i <= poludniki; i++)
            {
                float ph = 2 * Mathf.PI * i / poludniki;
                var n = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                p.v.Add(c + n * r); p.n.Add(n); p.uv.Add(new Vector2((float)i / poludniki * 4f, (float)j / rownolezniki * 2f));
            }
        }
        for (int j = 0; j < rownolezniki; j++)
            for (int i = 0; i < poludniki; i++)
            {
                int a = s + j * (poludniki + 1) + i, b = a + poludniki + 1;
                p.t.Add(a); p.t.Add(a + 1); p.t.Add(b); p.t.Add(a + 1); p.t.Add(b + 1); p.t.Add(b);
                Trojkaty += 2;
            }
    }

    /// <summary>Tablica z atlasu: środek, kierunek „w prawo” i normalna (frontem do patrzącego), szer. × wys., prostokąt w pikselach atlasu (od lewej-góry).</summary>
    public static void Tablica(Material m, Vector3 srodek, Vector3 normalna, float szer, float wys, RectInt px, int atlasW = 1024, int atlasH = 1024, bool dwustronny = false)
    {
        normalna = normalna.normalized;
        var prawo = Vector3.Cross(normalna, Vector3.up).normalized;      // prawo patrzącego na front
        if (prawo.sqrMagnitude < .5f) prawo = Vector3.right;
        var gora = Vector3.Cross(prawo, normalna).normalized;
        Vector3 hx = prawo * szer * .5f, hy = gora * wys * .5f;
        float u0 = px.x / (float)atlasW, u1 = (px.x + px.width) / (float)atlasW;
        float v1 = 1f - px.y / (float)atlasH, v0 = 1f - (px.y + px.height) / (float)atlasH;
        // kolejność: lewy dół, lewa góra, prawa góra, prawy dół (cross(b−a, c−a) = normalna → przód do patrzącego)
        QuadUV(m, srodek - hx - hy, srodek - hx + hy, srodek + hx + hy, srodek + hx - hy,
               new Vector2(u0, v0), new Vector2(u0, v1), new Vector2(u1, v1), new Vector2(u1, v0), dwustronny);
    }

    // ---------------- kolizje, rampy, lampy, punkty ----------------
    static Transform kolizje;
    public static void Kolizja(Vector3 c, Vector3 s, Quaternion q)
    {
        if (!Grupa) return;
        if (!kolizje || kolizje.parent != Grupa)
        {
            kolizje = Grupa.Find("kolizje");
            if (!kolizje) { kolizje = new GameObject("kolizje").transform; kolizje.SetParent(Grupa, false); }
        }
        var g = new GameObject("kolizja"); g.transform.SetParent(kolizje, false);
        g.transform.position = c; g.transform.rotation = q;
        g.AddComponent<BoxCollider>().size = new Vector3(Mathf.Max(.02f, s.x), Mathf.Max(.02f, s.y), Mathf.Max(.02f, s.z));
        Kolizje++;
    }
    public static void KolizjaMM(float x0, float x1, float y0, float y1, float z0, float z1)
        => Kolizja(new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f), new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), Mathf.Abs(z1 - z0)), Quaternion.identity);

    /// <summary>Pochylona płyta kolizji schodów (postać schodzi płynnie). gora/dol = środki krawędzi biegu na poziomie stopni.</summary>
    public static void Rampa(Vector3 gora, Vector3 dol, float szer)
    {
        var dir = dol - gora;
        var bok = Vector3.Cross(Vector3.up, dir).normalized;
        var nrm = Vector3.Cross(dir, bok).normalized; if (nrm.y < 0) nrm = -nrm;
        var q = Quaternion.LookRotation(dir.normalized, nrm);
        var srodek = (gora + dol) * .5f - nrm * .06f;
        Kolizja(srodek, new Vector3(szer, .12f, dir.magnitude + .3f), q);
    }

    public static Light Lampa(string nazwa, Vector3 at, float moc, float zasieg, Color? kolor = null)
    {
        var l = new GameObject(nazwa).AddComponent<Light>();
        l.transform.SetParent(Grupa, false); l.transform.position = at;
        l.type = LightType.Point; l.color = kolor ?? new Color(1f, .95f, .86f);
        l.intensity = moc; l.range = zasieg; l.shadows = LightShadows.None;
        l.renderMode = LightRenderMode.ForceVertex;
        return l;
    }

    /// <summary>Punkt AKCJI pod misje (BraxPunktAkcji) + wpis do punktów świata (BraxCityWorld.points).</summary>
    public static BraxPunktAkcji Akcja(string id, string etykieta, string odpowiedz, Vector3 at, Transform mowca = null, float promien = 1.7f)
    {
        var g = new GameObject("AKCJA " + id + " (" + etykieta + ")"); g.transform.SetParent(Grupa, false); g.transform.position = at;
        var a = g.AddComponent<BraxPunktAkcji>(); a.id = id; a.etykieta = etykieta; a.odpowiedz = odpowiedz; a.mowca = mowca; a.promien = promien;
        Punkt(id, etykieta, at);
        return a;
    }

    /// <summary>Miejsce obsady (pracownik, ochrona, DJ…) — pusty obiekt z pozycją i kierunkiem twarzy; etap ludzi stawia tu postać.</summary>
    public static Transform Obsada(string rola, Vector3 at, float yaw)
    {
        var g = new GameObject("BRAXHORN obsada: " + rola); g.transform.SetParent(Grupa, false);
        g.transform.position = at; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
        Obsady.Add(g.transform);
        return g.transform;
    }
    public static readonly List<Transform> Obsady = new List<Transform>();

    /// <summary>Siedzisko (AKCJA = usiądź / połóż się): środek siedziska na jego wysokości i kierunek twarzy siedzącego.</summary>
    public static BraxSeat Siedzisko(Vector3 srodek, Vector3 twarz, string etykieta = "ŁAWKA", bool lezy = false)
    {
        var g = new GameObject("Siedzisko: " + etykieta); g.transform.SetParent(Grupa, false);
        g.transform.position = srodek; g.transform.rotation = Quaternion.LookRotation(new Vector3(twarz.x, 0, twarz.z).normalized, Vector3.up);
        var s = g.AddComponent<BraxSeat>(); s.label = etykieta; s.lie = lezy; s.surfaceY = srodek.y;
        return s;
    }

    public static void Punkt(string id, string etykieta, Vector3 at)
    {
        var w = UnityEngine.Object.FindAnyObjectByType<BraxCityWorld>(); if (!w) return;
        var lista = (w.points ?? new BraxCityPoint[0]).ToList();
        var p = lista.FirstOrDefault(x => x.id == id);
        if (p != null) { p.position = at; p.label = etykieta; }
        else lista.Add(new BraxCityPoint { id = id, label = etykieta, position = at });
        w.points = lista.ToArray(); EditorUtility.SetDirty(w);
    }

    /// <summary>Napis 3D (TextMesh, shader z testem głębi) — do drobnych opisów; większe tablice z atlasu.</summary>
    public static void Napis(string tekst, Vector3 at, Vector3 normalna, float wys, Color kolor)
    {
        var obj = new GameObject("napis " + tekst); obj.transform.SetParent(Grupa, false); obj.transform.position = at;
        obj.transform.rotation = Quaternion.LookRotation(-normalna.normalized, Vector3.up);
        var tm = obj.AddComponent<TextMesh>(); tm.text = tekst; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
        tm.fontSize = 64; tm.characterSize = wys * 10f / 64f; tm.color = kolor; tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var sh = Shader.Find("Braxhorn/CitySignText");
        if (sh)
        {
            if (!napisMat) { Directory.CreateDirectory(Out); napisMat = new Material(sh) { name = "wnętrza: napisy" }; napisMat.mainTexture = tm.font.material.mainTexture; BraxZapisAssetu.Utworz(napisMat, Out + "/napisy.mat"); }
            obj.GetComponent<MeshRenderer>().sharedMaterial = napisMat;
        }
        obj.AddComponent<BraxCitySignText>();
    }
    static Material napisMat;

    // ---------------- zapis ----------------
    /// <summary>Zamienia partie na obiekty z siatkami zapisanymi jako pliki. Wołać na końcu każdego wnętrza.</summary>
    public static int Zapisz(string etykieta)
    {
        Directory.CreateDirectory(Out); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        int n = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var kv in partie)
            {
                var k = kv.Key; var p = kv.Value; if (p.v.Count == 0 || !k.g) continue;
                var mesh = new Mesh { name = etykieta + " / " + k.m.name, indexFormat = p.v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(p.v); mesh.SetNormals(p.n); mesh.SetUVs(0, p.uv); mesh.SetTriangles(p.t, 0);
                bool wnetrze = k.slonce < 0f; float ot = wnetrze ? -1f - k.slonce : 1f;
                if (wnetrze)
                {
                    // znacznik pieczenia CityStaticPBR (UV3): x = widoczność słońca, y = otoczenie, z ≥ .99, w = 2718
                    var b3 = new List<Vector4>(p.v.Count); for (int i = 0; i < p.v.Count; i++) b3.Add(new Vector4(0f, ot, 1.5f, 2718f));
                    mesh.SetUVs(3, b3);
                }
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                var go = new GameObject(k.m.name); go.transform.SetParent(k.g, false); go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = k.m;
                r.shadowCastingMode = wnetrze ? ShadowCastingMode.Off : ShadowCastingMode.On; r.receiveShadows = !wnetrze;
                r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
                BraxZapisAssetu.Utworz(mesh, Out + "/" + Bezpieczna(etykieta) + "_" + (seria++).ToString("D5") + ".asset");
                n++;
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        partie.Clear();
        AssetDatabase.SaveAssets();
        return n;
    }

    public static void Wyczysc() { partie.Clear(); Bryly = Trojkaty = Kolizje = 0; Obsady.Clear(); Braki.Clear(); }
    public static readonly List<string> Braki = new List<string>();

    public static string Bezpieczna(string s) => new string(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

    // ---------------- usuwanie starego i wycinanie otworów ----------------
    /// <summary>Usuwa (DestroyImmediate) obiekty o podanych nazwach (dokładnie albo „początek*”) w całej scenie. Zwraca listę usuniętych.</summary>
    public static List<string> Usun(params string[] nazwy)
    {
        var usuniete = new List<string>();
        var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var doUsuniecia = new List<GameObject>();
        foreach (var root in sc.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                foreach (var n in nazwy)
                    if (t.name == n || (n.EndsWith("*") && t.name.StartsWith(n.TrimEnd('*'), StringComparison.Ordinal)))
                    { if (!doUsuniecia.Any(g => t.IsChildOf(g.transform))) doUsuniecia.Add(t.gameObject); break; }
        foreach (var g in doUsuniecia) if (g) { usuniete.Add(Sciezka(g.transform)); UnityEngine.Object.DestroyImmediate(g); }
        return usuniete;
    }

    public static string Sciezka(Transform t) { var s = t.name; for (var p = t.parent; p; p = p.parent) s = p.name + "/" + s; return s; }

    /// <summary>Otwór w terenie (schody w dół): wycina trójkąty widocznych siatek (miasto źródłowe i renowacja) i kolizje
    /// w bryle. Obiekty pod `oprocz` nietknięte. Zwraca liczbę zmienionych siatek + kolizji.</summary>
    public static int Otwor(Transform cityRoot, Bounds bryla, string etykieta, Transform oprocz)
    {
        int n = 0;
        foreach (var scope in new[] { cityRoot, BraxClaudeGround.SourceCity })
        {
            if (!scope) continue;
            foreach (var mf in scope.GetComponentsInChildren<MeshFilter>(true))
            {
                if (oprocz && mf.transform.IsChildOf(oprocz)) continue;
                var r = mf.GetComponent<MeshRenderer>(); if (!r || !r.enabled || !mf.sharedMesh || !r.bounds.Intersects(bryla)) continue;
                try { n += BraxCityMeshCut.Subtract(mf.transform, bryla, Out + "/Otwory", etykieta); }
                catch (Exception e) { Debug.LogWarning("BRAXHORN_WNETRZA otwór " + etykieta + ": " + mf.name + ": " + e.Message); }
            }
        }
        foreach (var scope in new[] { cityRoot, BraxClaudeGround.SourcePhysics, BraxClaudeGround.SourceCity })
        {
            if (!scope) continue;
            foreach (var c in scope.GetComponentsInChildren<BoxCollider>(true))
            {
                if (!c || !c.enabled || c.isTrigger || (oprocz && c.transform.IsChildOf(oprocz)) || !c.bounds.Intersects(bryla)) continue;
                if (Quaternion.Angle(c.transform.rotation, Quaternion.identity) > .01f) { if (bryla.Contains(c.bounds.center)) { c.enabled = false; n++; } continue; }
                Bounds b = c.bounds; Vector3 lo = Vector3.Max(b.min, bryla.min), hi = Vector3.Min(b.max, bryla.max);
                if (hi.x - lo.x < 1e-4f || hi.y - lo.y < 1e-4f || hi.z - lo.z < 1e-4f) continue;
                void Dodaj(Vector3 a, Vector3 z)
                {
                    var s = z - a; if (s.x < 1e-4f || s.y < 1e-4f || s.z < 1e-4f) return;
                    var g = new GameObject(c.name + " (reszta po otworze)"); g.transform.SetParent(c.transform.parent, true);
                    g.transform.position = (a + z) * .5f; g.transform.rotation = Quaternion.identity; g.transform.localScale = Vector3.one;
                    var bc = g.AddComponent<BoxCollider>(); var ls = g.transform.lossyScale;
                    bc.size = new Vector3(s.x / Mathf.Max(1e-4f, ls.x), s.y / Mathf.Max(1e-4f, ls.y), s.z / Mathf.Max(1e-4f, ls.z));
                }
                Dodaj(b.min, new Vector3(lo.x, b.max.y, b.max.z));
                Dodaj(new Vector3(hi.x, b.min.y, b.min.z), b.max);
                Dodaj(new Vector3(lo.x, b.min.y, b.min.z), new Vector3(hi.x, lo.y, b.max.z));
                Dodaj(new Vector3(lo.x, hi.y, b.min.z), new Vector3(hi.x, b.max.y, b.max.z));
                Dodaj(new Vector3(lo.x, lo.y, b.min.z), new Vector3(hi.x, hi.y, lo.z));
                Dodaj(new Vector3(lo.x, lo.y, hi.z), new Vector3(hi.x, hi.y, b.max.z));
                c.enabled = false; n++;
            }
        }
        // osobne siatki kolizji bez renderera (płaski teren): trójkąty w prostokącie
        BraxClaudeGround.Cut(cityRoot, new Rect(bryla.min.x, bryla.min.z, bryla.size.x, bryla.size.z), bryla.min.y, bryla.max.y, oprocz);
        return n;
    }

    /// <summary>Usuwa drobne obiekty (lampy, ławki, kosze, drzewa) stojące w miejscu nowej budowli: całe bryły w obszarze.</summary>
    public static List<string> Uprzatnij(Transform cityRoot, Bounds obszar, Transform oprocz, float maxRozmiar = 7f)
    {
        var usuniete = new List<string>();
        foreach (var scope in new[] { cityRoot, BraxClaudeGround.SourceCity })
        {
            if (!scope) continue;
            foreach (var r in scope.GetComponentsInChildren<Renderer>(true))
            {
                if (!r || !r.enabled || (oprocz && r.transform.IsChildOf(oprocz))) continue;
                var b = r.bounds;
                if (!obszar.Contains(b.center) || Mathf.Max(b.size.x, b.size.z) > maxRozmiar || b.max.y < obszar.min.y) continue;
                if (b.min.y < obszar.min.y - 1f) continue;   // coś głęboko (stropy, ściany) — nie ruszamy
                r.enabled = false;
                foreach (var c in r.GetComponents<Collider>()) c.enabled = false;
                usuniete.Add(Sciezka(r.transform));
            }
            foreach (var c in scope.GetComponentsInChildren<Collider>(true))
            {
                if (!c || !c.enabled || c.isTrigger || (oprocz && c.transform.IsChildOf(oprocz)) || c.GetComponent<Renderer>()) continue;
                var b = c.bounds;
                if (!obszar.Contains(b.center) || Mathf.Max(b.size.x, b.size.z) > maxRozmiar || b.size.y < .25f) continue;
                c.enabled = false; usuniete.Add("kolizja " + Sciezka(c.transform));
            }
        }
        return usuniete;
    }

    // ---------------- materiały ----------------
    public static class Mat
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static Material Plain(string nazwa, Color kolor, float szorstkosc = .7f, float metal = 0f)
            => BraxCityMaterials.Surface("Wnętrza: " + nazwa, kolor, szorstkosc, metal);

        public static Texture2D TexAt(string path, bool normal = false)
        {
            if (!File.Exists(path)) return null;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Materiał z tekstury Poly Haven (pierwszy dostępny slug). Szorstkość STAŁA (bez mapy ARM) — posadzki matowe.</summary>
        public static Material Tex(string nazwa, float metry, Color tint, float szorstkosc, float metal, params string[] slugi)
        {
            string key = nazwa + "|" + metry + "|" + tint + "|" + szorstkosc + "|" + metal;
            if (cache.TryGetValue(key, out var c) && c) return c;
            foreach (var s in slugi)
            {
                Texture2D d = TexAt(PH + s + "/diffuse.jpg"), n = TexAt(PH + s + "/normal.jpg");
                if (!d) { var cr = "Assets/Source/CityRebuild/textures/" + s + "/2k/"; d = TexAt(cr + s + "_diff_2k.jpg"); n = TexAt(cr + s + "_nor_gl_2k.png"); }
                if (!d) continue;
                var m = BraxCityMaterials.Textured("Wnętrza: " + nazwa + " [" + s + "]", d, n, null, 1f / Mathf.Max(.05f, metry));
                m.SetColor("_Color", tint); m.SetFloat("_Roughness", Mathf.Clamp01(szorstkosc)); m.SetFloat("_Metallic", metal);
                EditorUtility.SetDirty(m);
                return cache[key] = m;
            }
            Braki.Add("brak tekstury: " + string.Join("/", slugi));
            return cache[key] = Plain(nazwa + " (bez tekstury)", tint * .8f, szorstkosc, metal);
        }

        /// <summary>Ekran / podświetlana tablica / klosz (shader bez oświetlenia, bez emisji).</summary>
        public static Material Ekran(string nazwa, Texture tex, Color tint)
        {
            string key = "ekran|" + nazwa;
            if (cache.TryGetValue(key, out var c) && c) return c;
            var sh = Shader.Find("Braxhorn/WnetrzaEkran");
            if (!sh) return cache[key] = Plain(nazwa + " (bez shadera ekranu)", tint, .4f);
            Directory.CreateDirectory(Out);
            var m = new Material(sh) { name = "Wnętrza: " + nazwa }; m.enableInstancing = true;
            if (tex) m.mainTexture = tex; m.SetColor("_Tint", tint);
            BraxZapisAssetu.Utworz(m, Out + "/ekran_" + Bezpieczna(nazwa) + ".mat");
            return cache[key] = m;
        }

        /// <summary>Promień (laser, snop) — addytywny, bez głębi.</summary>
        public static Material Promien(string nazwa, Color kolor, float moc)
        {
            string key = "promien|" + nazwa;
            if (cache.TryGetValue(key, out var c) && c) return c;
            var sh = Shader.Find("Braxhorn/WnetrzaPromien");
            if (!sh) return cache[key] = Plain(nazwa + " (bez shadera)", kolor, .4f);
            Directory.CreateDirectory(Out);
            var m = new Material(sh) { name = "Wnętrza: " + nazwa }; m.SetColor("_Tint", kolor); m.SetFloat("_Moc", moc);
            BraxZapisAssetu.Utworz(m, Out + "/promien_" + Bezpieczna(nazwa) + ".mat");
            return cache[key] = m;
        }

        /// <summary>Tekstura z repo (atlas, plakat) z ustawieniami pod napisy: maks. 1024/2048, trilinear, anizo.</summary>
        public static Texture2D Obraz(string path, int max = 1024, bool srgb = true)
        {
            if (!File.Exists(path)) { Braki.Add("brak obrazu: " + path); return null; }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp && (imp.maxTextureSize != max || imp.anisoLevel < 4 || imp.sRGBTexture != srgb || imp.wrapMode != TextureWrapMode.Clamp))
            {
                imp.maxTextureSize = max; imp.anisoLevel = 4; imp.filterMode = FilterMode.Trilinear; imp.sRGBTexture = srgb;
                imp.wrapMode = TextureWrapMode.Clamp; imp.mipmapEnabled = true; imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
