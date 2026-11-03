using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// DYSKOTEKA PREMIUM (build 45 runda 2, E28/E30 — właściciel 21:10–21:24: „bogatsza, lepsza”, „dwa razy tyle ludzi”,
// „DJ premium”, bar przeniesiony pod ścianę, gdzie stały 2 sofy, strona zachodnia = duża strefa lóż).
// Sala x 66.18–105.82, z −0.82–28.82 (wejście z ulicy x 84–88 w ścianie z 28.82, wyjście x 84–88 w ścianie z −0.82).
//  • BAR PREMIUM pod ścianą południową, część zachodnia (x 68.6–81.4 — 2.6 m wolne przed wyjściem): zaplecze z cokołem
//    i marmurem, regały z butelkami (model bar_polki_butelki, zapas: półki z kodu), lustro, pas LED pod półkami, przejście
//    barmanów 1.0 m × K, lada 1.1 m × K z podświetlanym kamieniem (onyks), podnóżek mosiężny, hokery (model, zapas: kod),
//    lampy wiszące z kloszem (model, zapas: kod), szkło na zapleczu (model), napis BAR (litery przestrzenne, neon).
//    3 stanowiska: po 5 OSOBNYCH butelek na szynie, szklanka, ciecz, strumień — BraxBarDyskoteki (nalewanie, ZAMÓW).
//  • LOŻE: 15 (5 pod ścianą zachodnią na tapicerowanym panelu, 2 pod północną, 6 na wyspie ze ścianką x 76.5, 2 pod
//    południową za wyjściem) — sofa chesterfield + 2 fotele w U, stolik marmurowy PRZED siedziskiem (odstęp 0.4 m × K
//    od krawędzi siedziska); model loze_chesterfield_moet / loze_stolik_marmur_3dtech, zapas: kod.
//  • DJ PREMIUM: konsola na scenie z mikserem dj_mixer.glb (NOVA), ściana LED 8.8 × 4.4 m za DJ-em (wzór z kodu —
//    BraxKlubEfekty), 2 lasery laser_light.glb na kolumnach z wachlarzami wiązek (addytywne, z kodu, omiatają), kula
//    disco_ball.glb pod sufitem nad parkietem (obrót), 4 stożki świateł ruchomych głów z kratownicy.
//  • DETALE (właściciel 21:52): listwy przypodłogowe, obrzeże sufitu, ościeżnice i opaski obu drzwi, znaki WYJŚCIE
//    nad drzwiami, gaśnice przy wyjściach, gniazdka (0.3 m × K) i włączniki (1.1 m × K), kratki wentylacyjne, oprawy
//    z kloszem (downlighty z pierścieniem, lampy wiszące) — log BRAXHORN_DETALE.
// Miejsca dla ludzi (etap ludzi): klub.siedziska, klub.barStoja, klub.barmani, klub.dj.
public static partial class BraxClaudeOsiedle
{
    sealed class KlubMiejsca
    {
        public readonly List<(Vector3 p, Vector3 przod)> siedziska = new List<(Vector3, Vector3)>(), barStoja = new List<(Vector3, Vector3)>();
        public readonly List<Vector3> barmani = new List<Vector3>();
        public Vector3 dj, ochrona; public float scena;
        public int loze;
    }
    static KlubMiejsca klub;
    static BraxBarDyskoteki klubBar;
    static readonly List<(string co, Bounds b)> klubMeble = new List<(string, Bounds)>();
    static readonly Rola RolaDj = new Rola { nazwa = "DJ", postaci = new[] { "Male_Adult" }, sluchawki = true };

    static Material Swiecacy(string key, Color c, Color em)
    {
        var m = Plain(key, c, .5f);
        if (!m.IsKeywordEnabled("_EMISSION"))
        {
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", em); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
        }
        return m;
    }
    static readonly Dictionary<string, Material> addytywne = new Dictionary<string, Material>();
    static Material Addytywny(string key, Color c)
    {
        if (addytywne.TryGetValue(key, out var have) && have) return have;
        var sh = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Mobile/Particles/Additive");
        if (!sh) return null;
        var m = new Material(sh) { name = "Osiedle: " + key };
        if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", c);
        AssetDatabase.CreateAsset(m, Out + "/" + Safe(key) + ".mat");
        addytywne[key] = m; return m;
    }

    // ---------------- siatki osobnych obiektów (butelki, szklanki, ciecz, wiązki) ----------------
    static readonly Dictionary<string, Mesh> klubSiatki = new Dictionary<string, Mesh>();
    static Mesh ZapiszSiatke(string nazwa, Mesh m)
    {
        m.name = nazwa; m.RecalculateBounds();
        AssetDatabase.CreateAsset(m, Out + "/klub_" + Safe(nazwa) + ".asset");
        klubSiatki[nazwa] = m; return m;
    }
    // bryła obrotowa z profilu (r, y), bez dna (nic nie leży płasko na blacie — brak par współpłaszczyznowych); pokrywa = zamknięcie u góry
    static Mesh Toczona(string nazwa, Vector2[] profil, int seg, bool pokrywa)
    {
        if (klubSiatki.TryGetValue(nazwa, out var have) && have) return have;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>();
        int P = profil.Length;
        for (int i = 0; i < P; i++)
        {
            var a = profil[Mathf.Max(0, i - 1)]; var b = profil[Mathf.Min(P - 1, i + 1)];
            var tg = (b - a); var nn = new Vector2(tg.y, -tg.x).normalized;
            for (int k = 0; k <= seg; k++)
            {
                float f = k * Mathf.PI * 2f / seg; var d = new Vector3(Mathf.Cos(f), 0, Mathf.Sin(f));
                v.Add(d * profil[i].x + Vector3.up * profil[i].y); n.Add((d * nn.x + Vector3.up * nn.y).normalized); uv.Add(new Vector2(k / (float)seg, i / (float)(P - 1)));
            }
        }
        for (int i = 0; i < P - 1; i++)
            for (int k = 0; k < seg; k++)
            {
                int a = i * (seg + 1) + k, b = a + 1, c = a + seg + 1, e = c + 1;
                tr.AddRange(new[] { a, c, b, b, c, e });
            }
        if (pokrywa && profil[P - 1].x > 1e-4f)
        {
            int c0 = v.Count; v.Add(Vector3.up * profil[P - 1].y); n.Add(Vector3.up); uv.Add(new Vector2(.5f, 1f));
            for (int k = 0; k <= seg; k++) { float f = k * Mathf.PI * 2f / seg; v.Add(new Vector3(Mathf.Cos(f), 0, Mathf.Sin(f)) * profil[P - 1].x + Vector3.up * profil[P - 1].y); n.Add(Vector3.up); uv.Add(new Vector2(k / (float)seg, 1f)); }
            for (int k = 0; k < seg; k++) tr.AddRange(new[] { c0, c0 + 2 + k, c0 + 1 + k });
        }
        var m = new Mesh(); m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tr, 0);
        return ZapiszSiatke(nazwa, m);
    }
    // wiązka: dwa skrzyżowane paski od (0,0,0) wzdłuż +z na długość L, jasna u źródła, wygasa ku końcowi (kolor wierzchołków)
    static Mesh Wachlarz(string nazwa, float L, float szer, float[] katy)
    {
        if (klubSiatki.TryGetValue(nazwa, out var have) && have) return have;
        var v = new List<Vector3>(); var c = new List<Color>(); var tr = new List<int>();
        foreach (float a in katy)
        {
            var q = Quaternion.Euler(0, a, 0);
            foreach (var w in new[] { Vector3.right, Vector3.up })
            {
                int k = v.Count;
                v.Add(q * (-w * szer)); v.Add(q * (w * szer)); v.Add(q * (w * szer * 3f + Vector3.forward * L)); v.Add(q * (-w * szer * 3f + Vector3.forward * L));
                c.Add(Color.white); c.Add(Color.white); c.Add(Color.black); c.Add(Color.black);
                tr.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3, k, k + 2, k + 1, k, k + 3, k + 2 });
            }
        }
        var m = new Mesh(); m.SetVertices(v); m.SetColors(c); m.SetTriangles(tr, 0); m.RecalculateNormals();
        return ZapiszSiatke(nazwa, m);
    }
    static Mesh Stozek(string nazwa, int seg)
    {
        if (klubSiatki.TryGetValue(nazwa, out var have) && have) return have;
        var v = new List<Vector3> { Vector3.zero }; var c = new List<Color> { Color.white }; var tr = new List<int>();
        for (int k = 0; k <= seg; k++) { float f = k * Mathf.PI * 2f / seg; v.Add(new Vector3(Mathf.Cos(f) * .24f, -1f, Mathf.Sin(f) * .24f)); c.Add(Color.black); }
        for (int k = 0; k < seg; k++) tr.AddRange(new[] { 0, k + 1, k + 2, 0, k + 2, k + 1 });
        var m = new Mesh(); m.SetVertices(v); m.SetColors(c); m.SetTriangles(tr, 0); m.RecalculateNormals();
        return ZapiszSiatke(nazwa, m);
    }
    static GameObject Obiekt(Transform parent, string nazwa, Mesh mesh, Material m, Vector3 p, Quaternion q, Vector3 s, bool cien = false)
    {
        var g = new GameObject(nazwa); g.transform.SetParent(parent, false); g.transform.SetPositionAndRotation(p, q); g.transform.localScale = s;
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = cien ? ShadowCastingMode.On : ShadowCastingMode.Off;
        r.receiveShadows = cien; r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return g;
    }
    // tarcza skierowana w dół (soczewka oprawy)
    static void DyskDol(Material m, Vector3 c, float r, int seg = 12)
    {
        for (int k = 0; k < seg; k++)
        {
            float a0 = k * Mathf.PI * 2 / seg, a1 = (k + 1) * Mathf.PI * 2 / seg;
            Tri(m, c, c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r, c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r, Vector3.down);
        }
    }

    // ---------------- modele z paczki: część zestawu, kierunek przodu ----------------
    // Przód mebla z siatki: wierzchołki w górnych 20 % wysokości (oparcie, podłokietniki) przesunięte względem środka
    // w stronę tyłu — przód = przeciwnie. Siatka nieczytelna → +z (konwencja glTF).
    static Vector3 PrzodMebla(Transform czesc)
    {
        var pts = new List<Vector3>();
        foreach (var mf in czesc.GetComponentsInChildren<MeshFilter>(true))
        {
            var m = mf.sharedMesh; if (!m || !m.isReadable) continue;
            var M = mf.transform.localToWorldMatrix; foreach (var p in m.vertices) pts.Add(M.MultiplyPoint3x4(p));
        }
        if (pts.Count < 8) return Vector3.forward;
        var b = new Bounds(pts[0], Vector3.zero); foreach (var p in pts) b.Encapsulate(p);
        float yHi = b.min.y + .8f * b.size.y; Vector3 s = Vector3.zero; int n = 0;
        foreach (var p in pts) if (p.y > yHi) { s += p - b.center; n++; }
        if (n == 0) return Vector3.forward;
        s /= n;
        return Mathf.Abs(s.x) / Mathf.Max(.01f, b.size.x) > Mathf.Abs(s.z) / Mathf.Max(.01f, b.size.z) ? new Vector3(-Mathf.Sign(s.x), 0, 0) : new Vector3(0, 0, -Mathf.Sign(s.z));
    }
    // kopia JEDNEJ części zestawu (pozostałe usunięte), skala, przód w kierunku „przod”, podstawa w p.y, środek w (p.x, p.z)
    static GameObject Czesc(GameObject zrodlo, int idx, string nazwa, Vector3 p, Vector3 przod, float skala)
    {
        var o = (GameObject)UnityEngine.Object.Instantiate(zrodlo, Group(group), false);
        o.name = nazwa; o.transform.position = Vector3.zero; o.transform.rotation = zrodlo.transform.localRotation; o.transform.localScale = zrodlo.transform.localScale * skala;   // korzeń Sketchfab: obrót Z→Y zostaje
        var cz = BraxOsiedleAssety.Czesci(o);
        if (idx >= cz.Count) { UnityEngine.Object.DestroyImmediate(o); return null; }
        var keep = cz[idx];
        for (int i = 0; i < cz.Count; i++) if (i != idx && cz[i]) UnityEngine.Object.DestroyImmediate(cz[i].gameObject);
        var f0 = PrzodMebla(keep);
        o.transform.rotation = Quaternion.Euler(0, Vector3.SignedAngle(f0, przod, Vector3.up), 0) * zrodlo.transform.localRotation;
        var b = BraxOsiedleAssety.Granice(o);
        o.transform.position += new Vector3(p.x - b.center.x, p.y - b.min.y, p.z - b.center.z);
        BraxOsiedleAssety.Przygotuj(o, true);
        return o;
    }
    static void Mebel(string co, Bounds b) => klubMeble.Add((co, b));
    static Bounds Obrys(Vector3 c, Vector3 przod, float szer, float gl, float y0, float y1)
    {
        bool wzdluzX = Mathf.Abs(przod.z) > Mathf.Abs(przod.x);   // przód wzdłuż z → szerokość wzdłuż x
        var s = wzdluzX ? new Vector3(szer, y1 - y0, gl) : new Vector3(gl, y1 - y0, szer);
        return new Bounds(new Vector3(c.x, (y0 + y1) * .5f, c.z), s);
    }

    // ---------------- budowa ----------------
    static void KlubPremium(Material wallM, Material lacquer, Material steel, Material chromeK, Material marbleK, Material leatherK, Material mirrorK,
                            float X0, float X1, float Z0, float Z1, float yF, float yC, float dA, float dB, float dH, float sH)
    {
        klub = new KlubMiejsca { scena = sH }; klubMeble.Clear(); klubBar = null;
        var root = Group(group);
        var brass = Plain("klub mosiądz szczotkowany", new Color(.72f, .55f, .26f), .7f, .9f);
        var walnut = Plain("klub orzech ciemny", new Color(.20f, .12f, .07f), .45f);
        var onyx = Swiecacy("klub onyks podświetlany (blat baru)", new Color(.95f, .78f, .52f), new Color(.85f, .55f, .22f));
        var ledAmber = Swiecacy("klub pas LED bursztyn", new Color(1f, .7f, .35f), new Color(1f, .62f, .25f) * 1.6f);
        var neon = Swiecacy("klub neon BAR różowy", new Color(1f, .4f, .75f), new Color(1f, .25f, .7f) * 2.2f);
        var lens = Swiecacy("klub soczewka oprawy ciepła", new Color(1f, .93f, .82f), new Color(1f, .86f, .66f) * 1.3f);
        var exitG = Swiecacy("znak ewakuacyjny zielony (świeci)", new Color(.05f, .55f, .2f), new Color(.05f, .7f, .25f) * 1.3f);
        var red = Plain("gaśnica czerwona", new Color(.70f, .05f, .04f), .6f);
        var black = Plain("klub czerń mat (osprzęt)", new Color(.03f, .03f, .035f), .3f);
        var white = Plain("osprzęt elektryczny biały", new Color(.92f, .92f, .9f), .45f);
        var glassM = GlassClear;
        var bottleCols = new[] { Plain("butelka zielona", new Color(.08f, .32f, .14f), .85f), Plain("butelka bursztyn", new Color(.45f, .22f, .05f), .85f), Plain("butelka bezbarwna", new Color(.75f, .80f, .80f), .9f) };
        var liquid = Plain("klub drink bursztynowy", new Color(.75f, .38f, .08f), .85f);
        int modeleUzyte = 0, modeleBrak = 0;

        // ================= BAR PREMIUM =================
        const float bx0 = 68.6f, bx1 = 81.4f;
        float zB1 = Z0 + .5f * K, zL1 = zB1 + 1.0f * K, zC1 = zL1 + .65f * K, hB = .95f * K, hC = 1.1f * K, kick = .1f * K, xs = bx0 + 1.0f;
        // zaplecze: cokół cofnięty 6 cm, szafki orzech, blat marmur
        BoxMinMax(black, bx0, bx1, yF, yF + kick, Z0 + .02f, zB1 - .06f, false);
        BoxMinMax(walnut, bx0, bx1, yF + kick, yF + hB, Z0 + .02f, zB1, false);
        Collider(new Vector3((bx0 + bx1) * .5f, (yF + yF + hB) * .5f, (Z0 + .02f + zB1) * .5f), new Vector3(bx1 - bx0, hB, zB1 - Z0 - .02f));
        BoxMinMax(marbleK, bx0 - .03f, bx1 + .03f, yF + hB, yF + hB + .04f, Z0 + .02f, zB1 + .03f, false, true);
        Mebel("bar: zaplecze", new Bounds(new Vector3((bx0 + bx1) * .5f, yF + hB * .5f, (Z0 + zB1) * .5f), new Vector3(bx1 - bx0, hB, zB1 - Z0 - .02f)));
        // lustro i półki nad zapleczem
        float yS0 = yF + hB + .04f;
        BoxMinMax(mirrorK, bx0 + .1f, bx1 - .1f, yS0 + .02f, yS0 + 1.75f * K, Z0 + .02f, Z0 + .035f, false);
        var polki = BraxOsiedleAssety.ModelWnetrz("bar_polki_butelki_windupbird");
        bool polkiModel = false;
        if (polki)
        {
            try
            {
                var tmp = (GameObject)UnityEngine.Object.Instantiate(polki); tmp.transform.position = Vector3.zero; tmp.transform.rotation = polki.transform.localRotation;
                var hs = tmp.GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith("Object") || r.name.StartsWith("Box") || r.transform.parent && (r.transform.parent.name.StartsWith("Object") || r.transform.parent.name.StartsWith("Box")))
                            .Select(r => r.bounds.size.y).Where(h => h > 1e-4f).OrderBy(h => h).ToList();
                var bt0 = BraxOsiedleAssety.Granice(tmp); UnityEngine.Object.DestroyImmediate(tmp);
                float s = hs.Count >= 3 ? .30f * K / hs[hs.Count / 2] : 2.4f * K / Mathf.Max(1e-4f, Mathf.Max(bt0.size.x, bt0.size.z));
                float wr = Mathf.Max(bt0.size.x, bt0.size.z) * s;
                if (wr < 1.0f || wr > 6.5f) { s = 2.4f * K / Mathf.Max(1e-4f, Mathf.Max(bt0.size.x, bt0.size.z)); wr = 2.4f * K; }
                float yaw = bt0.size.x >= bt0.size.z ? 0f : 90f;
                int n = Mathf.Clamp(Mathf.FloorToInt((bx1 - bx0 - .4f) / (wr + .15f)), 1, 4);
                for (int i = 0; i < n; i++)
                {
                    float cx = (bx0 + bx1) * .5f + (i - (n - 1) * .5f) * (wr + .15f);
                    var o = BraxOsiedleAssety.Postaw(root, polki, "bar: regał z butelkami " + (i + 1), new Vector3(cx, yS0, Z0 + .3f), yaw, null, null);
                    if (!o) continue;
                    o.transform.localScale = polki.transform.localScale * s;
                    var b = BraxOsiedleAssety.Granice(o); o.transform.position += new Vector3(cx - b.center.x, yS0 - b.min.y, Z0 + .04f - b.min.z);
                    modeleUzyte++; polkiModel = true;
                }
            }
            catch (Exception e) { BraxOsiedleAssety.Blad("bar: regał z butelkami", e); }
        }
        else modeleBrak++;
        if (!polkiModel)
            for (int sh = 0; sh < 3; sh++)
            {
                float y = yS0 + .3f * K + sh * .42f * K;
                BoxMinMax(glassM, bx0 + .2f, bx1 - .2f, y - .02f, y, Z0 + .04f, Z0 + .34f, false, true);
                BoxMinMax(ledAmber, bx0 + .2f, bx1 - .2f, y - .035f, y - .02f, Z0 + .3f, Z0 + .33f, false, true);
                int k = 0;
                for (float x = bx0 + .35f; x < bx1 - .3f; x += .17f * K, k++) Cylinder(bottleCols[(k + sh) % 3], new Vector3(x, y, Z0 + .19f), .035f * K, (.27f + (k % 3) * .03f) * K, 8, true, false);
            }
        BoxMinMax(ledAmber, bx0 + .2f, bx1 - .2f, yF + hB - .03f, yF + hB - .012f, zB1 + .004f, zB1 + .018f, false, true);   // pas LED pod nawisem blatu zaplecza
        // szkło (model bar_szklo_saandy) na końcach lady (blat zaplecza zajmują regały)
        var szklo = BraxOsiedleAssety.ModelWnetrz("bar_szklo_saandy");
        if (szklo) foreach (float cx in new[] { xs + .75f, bx1 - .8f }) { if (BraxOsiedleAssety.Postaw(root, szklo, "bar: szkło", new Vector3(cx, yF + hC + .05f, (zL1 + zC1) * .5f), 0f, .9f, null, false)) modeleUzyte++; }
        else modeleBrak++;
        // napis BAR (litery przestrzenne, lico neon) nad lustrem
        try
        {
            var g0 = group;
            SzyldLitery("dyskoteka", "bar", "BAR", new Vector3((bx0 + bx1) * .5f, yS0 + 1.75f * K + .25f, Z0 + .02f), Vector3.forward, .45f * K, neon, MatBokiCzarne, .08f, .04f);
            group = g0;
        }
        catch (Exception e) { Debug.LogError("BRAXHORN_DYSKOTEKA napis BAR DO POPRAWKI: " + e.Message); }
        // lada: cokół cofnięty, korpus lakier + mosiężny pas, blat z podświetlanego onyksu, szyna robocza od strony barmanów
        BoxMinMax(black, xs, bx1, yF, yF + kick, zL1 + .06f, zC1 - .06f, false);
        BoxMinMax(lacquer, xs, bx1, yF + kick, yF + hC, zL1, zC1, false);
        Collider(new Vector3((xs + bx1) * .5f, yF + hC * .5f, (zL1 + zC1) * .5f), new Vector3(bx1 - xs, hC, zC1 - zL1));
        BoxMinMax(brass, xs, bx1, yF + kick + .02f, yF + kick + .05f, zC1, zC1 + .008f, false);
        BoxMinMax(onyx, xs - .05f, bx1 + .05f, yF + hC, yF + hC + .05f, zL1 - .05f, zC1 + .14f, false, true);
        BoxMinMax(steel, xs, bx1 - .7f, yF + .88f * K, yF + .9f * K, zL1 - .2f, zL1, false, true);
        BoxMinMax(lacquer, bx1 - .5f, bx1, yF, yF + hC, zB1, zL1);                                                             // zamknięcie przejścia od wschodu
        Mebel("bar: lada", new Bounds(new Vector3((xs + bx1) * .5f, yF + hC * .5f, (zL1 + zC1) * .5f), new Vector3(bx1 - xs, hC, zC1 - zL1)));
        Tube(brass, new Vector3(xs + .1f, yF + .25f * K, zC1 + .16f), new Vector3(bx1 - .1f, yF + .25f * K, zC1 + .16f), .022f * K, 10);
        for (float x = xs + .3f; x < bx1; x += 1.6f) Tube(brass, new Vector3(x, yF + .25f * K, zC1 + .16f), new Vector3(x, yF + .25f * K, zC1 + .004f), .012f * K, 6);
        // hokery: sloty co 0.8 m, w 5 slotach goście stoją (z drinkiem)
        var hoker = BraxOsiedleAssety.ModelWnetrz("bar_hoker_skora_nosteam");
        float hYaw = 0f; bool hFront = false;
        if (hoker)
        {
            var tmp = (GameObject)UnityEngine.Object.Instantiate(hoker); tmp.transform.position = Vector3.zero; tmp.transform.rotation = hoker.transform.localRotation;
            var f0 = PrzodMebla(tmp.transform); UnityEngine.Object.DestroyImmediate(tmp);
            hYaw = Vector3.SignedAngle(f0, Vector3.back, Vector3.up); hFront = true;
        }
        else modeleBrak++;
        float zH = zC1 + .14f + .3f * K;
        for (int k = 0; k < 15; k++)
        {
            float x = xs + .4f + k * .8f; if (x > bx1 - .3f) break;
            if (k % 3 == 1) { klub.barStoja.Add((new Vector3(x, yF, zH + .1f), Vector3.back)); continue; }
            var sc = new Vector3(x, yF, zH);
            GameObject o = hFront ? BraxOsiedleAssety.Postaw(root, hoker, "bar: hoker", sc, hYaw, null, .99f) : null;
            if (o) { modeleUzyte++; Collider(sc + Vector3.up * .4f * K, new Vector3(.4f * K, .8f * K, .4f * K)); Mebel("bar: hoker", BraxOsiedleAssety.Granice(o)); continue; }
            Cylinder(chromeK, sc, .2f * K, .03f, 16, true, false);
            Cylinder(chromeK, sc + Vector3.up * .03f, .03f * K, .72f * K, 8, false, false);
            Cylinder(leatherK, sc + Vector3.up * (.03f + .72f * K), .19f * K, .07f * K, 16, true, false);
            Ring(chromeK, sc + Vector3.up * .3f * K, .15f * K, .17f * K, 16);
            Collider(sc + Vector3.up * .4f * K, new Vector3(.4f * K, .8f * K, .4f * K));
            Mebel("bar: hoker", new Bounds(sc + Vector3.up * .41f * K, new Vector3(.4f * K, .82f * K, .4f * K)));
        }
        foreach (float x in new[] { 71.0f, 77.6f }) klub.barStoja.Add((new Vector3(x, yF, zH + 1.3f), Vector3.back));
        // lampy wiszące z kloszem nad ladą (model bar_lampa_markpeters, zapas: klosz z kodu), przewód do sufitu
        var lampa = BraxOsiedleAssety.ModelWnetrz("bar_lampa_markpeters"); if (!lampa) modeleBrak++;
        int oprawy = 0;
        float yLampa = yF + hC + .85f * K, zLampa = (zL1 + zC1) * .5f;
        for (float x = xs + .8f; x < bx1 - .3f; x += 1.9f)
        {
            var o = lampa ? BraxOsiedleAssety.Postaw(root, lampa, "bar: lampa wisząca", new Vector3(x, yLampa, zLampa), 0f, .35f, null, false) : null;
            float top = yLampa + .25f * K;
            if (o) { modeleUzyte++; top = BraxOsiedleAssety.Granice(o).max.y; }
            else
            {
                Cylinder(brass, new Vector3(x, yLampa, zLampa), .17f * K, .2f * K, 16, false, false);
                Cylinder(brass, new Vector3(x, yLampa + .2f * K, zLampa), .05f * K, .05f * K, 8, true, false);
                DyskDol(lens, new Vector3(x, yLampa + .03f * K, zLampa), .13f * K);
                top = yLampa + .25f * K;
            }
            if (top < yC - .02f) Tube(black, new Vector3(x, top, zLampa), new Vector3(x, yC, zLampa), .006f, 4);
            oprawy++;
        }
        // stanowiska barmanów: 5 osobnych butelek na szynie, szklanka na blacie, ciecz, strumień
        var barGo = new GameObject("dyskoteka: bar — nalewanie (BraxBarDyskoteki)"); barGo.transform.SetParent(root, false);
        klubBar = barGo.AddComponent<BraxBarDyskoteki>();
        var butelkiM = BraxOsiedleAssety.ModelWnetrz("bar_butelki_helyeouka"); if (!butelkiM) modeleBrak++;
        var mButelka = Toczona("butelka", new[] { new Vector2(.037f, 0f), new Vector2(.038f, .01f), new Vector2(.038f, .19f), new Vector2(.03f, .22f), new Vector2(.014f, .25f), new Vector2(.013f, .295f), new Vector2(.016f, .30f), new Vector2(.014f, .31f) }, 12, true);
        var mSzklanka = Toczona("szklanka", new[] { new Vector2(.040f, 0f), new Vector2(.042f, .095f), new Vector2(.039f, .095f), new Vector2(.035f, .012f), new Vector2(0f, .012f) }, 14, false);
        var mWalec = Toczona("walec jednostkowy", new[] { new Vector2(1f, 0f), new Vector2(1f, 1f) }, 10, true);
        var st = new List<BraxBarDyskoteki.Stanowisko>(); int butelkiOsobne = 0;
        float yRail = yF + .9f * K;
        foreach (float sx in new[] { 72.2f, 75.6f, 79.0f })
        {
            var s = new BraxBarDyskoteki.Stanowisko { butelkaWys = .31f * K, szklankaWys = .095f * K, cieczSkalaY = .075f * K, strumienR = .005f * K };
            var bl = new List<Transform>();
            for (int i = 0; i < 5; i++)
            {
                var p = new Vector3(sx - .36f + i * .18f, yRail, zL1 - .12f);
                var bt = new GameObject("bar: butelka (osobna) " + (i + 1)).transform; bt.SetParent(barGo.transform, false); bt.position = p;
                bool zModelu = false;
                if (butelkiM)
                {
                    try
                    {
                        var o = Czesc(butelkiM, i % 3, "butelka — model", p, Vector3.back, 1f);
                        if (o)
                        {
                            var b = BraxOsiedleAssety.Granice(o); float sk = .31f * K / Mathf.Max(1e-4f, b.size.y);
                            o.transform.localScale *= sk; b = BraxOsiedleAssety.Granice(o);
                            o.transform.position += new Vector3(p.x - b.center.x, p.y - b.min.y, p.z - b.center.z);
                            o.transform.SetParent(bt, true); zModelu = true; modeleUzyte++;
                        }
                    }
                    catch (Exception e) { BraxOsiedleAssety.Blad("bar: butelka z modelu", e); }
                }
                if (!zModelu) Obiekt(bt, "butelka — kod", mButelka, bottleCols[(i + (int)sx) % 3], p, Quaternion.identity, Vector3.one * K);
                bl.Add(bt); butelkiOsobne++;
            }
            s.butelki = bl.ToArray();
            var gp = new Vector3(sx + .35f, yF + hC + .05f, zL1 + .3f);
            s.szklanka = Obiekt(barGo.transform, "bar: szklanka", mSzklanka, glassM, gp, Quaternion.identity, Vector3.one * K).transform;
            s.ciecz = Obiekt(barGo.transform, "bar: drink w szklance", mWalec, liquid, gp + Vector3.up * .008f * K, Quaternion.identity, new Vector3(.034f * K, .075f * K, .034f * K)).transform;
            s.strumien = Obiekt(barGo.transform, "bar: strumień", mWalec, liquid, gp, Quaternion.identity, new Vector3(.005f * K, .1f, .005f * K)).transform;
            st.Add(s);
            klub.barmani.Add(new Vector3(sx, yF, (zB1 + zL1) * .5f));
        }
        klubBar.stanowiska = st.ToArray();
        klubBar.strefaZamow = new Bounds(new Vector3((xs + bx1) * .5f, yF + 1.2f, zC1 + .9f), new Vector3(bx1 - xs, 2.6f, 1.6f * K));
        PointLamp(new Vector3(72.5f, 3.3f, 1.6f), 7f, .9f, new Color(1f, .8f, .55f));
        PointLamp(new Vector3(79.0f, 3.3f, 1.6f), 7f, .9f, new Color(1f, .8f, .55f));

        // ================= LOŻE =================
        var zestaw = BraxOsiedleAssety.ModelWnetrz("loze_chesterfield_moet");
        var stolik = BraxOsiedleAssety.ModelWnetrz("loze_stolik_marmur_3dtech");
        if (!zestaw) modeleBrak++; if (!stolik) modeleBrak++;
        float skalaZ = 0f; int idxSofa = 0, idxFotel = -1;
        if (zestaw)
        {
            try
            {
                var tmp = (GameObject)UnityEngine.Object.Instantiate(zestaw); tmp.transform.position = Vector3.zero; tmp.transform.rotation = zestaw.transform.localRotation;
                var cz = BraxOsiedleAssety.Czesci(tmp);
                if (cz.Count >= 2)
                {
                    var bs = BraxOsiedleAssety.Granice(cz[0]); float ws = Mathf.Max(bs.size.x, bs.size.z);
                    skalaZ = 2.0f * K / Mathf.Max(1e-4f, ws);
                    for (int i = 1; i < cz.Count && idxFotel < 0; i++)
                    {
                        var bf = BraxOsiedleAssety.Granice(cz[i]); float wf = Mathf.Max(bf.size.x, bf.size.z);
                        if (wf > .3f * ws && wf < .7f * ws && bf.size.y > .6f * bs.size.y) idxFotel = i;
                    }
                }
                BraxOsiedleAssety.Log("loże: zestaw chesterfield części=" + cz.Count + " sofa=0 fotel=" + idxFotel + " skala=" + skalaZ.ToString("F3"));
                UnityEngine.Object.DestroyImmediate(tmp);
            }
            catch (Exception e) { BraxOsiedleAssety.Blad("loże: podział zestawu", e); skalaZ = 0f; }
        }
        float sofaW = 2.0f * K, sofaD = .9f * K, armW = .9f * K, armD = .85f * K, tR = .25f * K, gap = .4f * K, seatH = .45f * K, tH = .5f * K;
        var panel = Plain("klub panel tapicerowany bordo", new Color(.25f, .03f, .07f), .35f);
        void SofaKod(Vector3 c, Vector3 f, float w, float d)
        {
            var r = Vector3.Cross(Vector3.up, f); float yaw = Quaternion.LookRotation(f).eulerAngles.y;
            OBox(leatherK, c + Vector3.up * (yF + .2f * K), new Vector3(w, .3f * K, d), yaw);                                    // baza
            OBox(leatherK, c + f * .06f * K + Vector3.up * (yF + .385f * K), new Vector3(w - .36f * K, .13f * K, d - .3f * K), yaw);  // poduchy
            OBox(leatherK, c - f * (d * .5f - .11f * K) + Vector3.up * (yF + .5f * K), new Vector3(w, .66f * K, .22f * K), yaw);    // oparcie
            foreach (int sgn in new[] { -1, 1 }) OBox(leatherK, c + r * sgn * (w * .5f - .09f * K) + Vector3.up * (yF + .36f * K), new Vector3(.18f * K, .48f * K, d), yaw); // boki
            OBox(walnut, c + Vector3.up * (yF + .025f * K), new Vector3(w - .1f, .05f * K, d - .1f), yaw);                       // cokół
        }
        int lozeN = 0;
        void Loza(Vector3 sciana, Vector3 f, bool panelSciana)
        {
            lozeN++;
            var r = Vector3.Cross(Vector3.up, f); sciana.y = 0f;
            var cs = sciana + f * (sofaD * .5f);
            if (panelSciana) OBox(panel, sciana - f * .03f + Vector3.up * (yF + .6f * K + .55f), new Vector3(sofaW + .4f, 1.1f + .2f * K, .06f), Quaternion.LookRotation(f).eulerAngles.y);
            // sofa
            GameObject so = skalaZ > 0f ? Czesc(zestaw, idxSofa, "loża " + lozeN + ": sofa chesterfield", cs + Vector3.up * yF, f, skalaZ) : null;
            Bounds bS;
            float glS = sofaD;
            if (so)
            {
                modeleUzyte++; bS = BraxOsiedleAssety.Granice(so);
                glS = Mathf.Abs(Vector3.Dot(bS.size, new Vector3(Mathf.Abs(f.x), 0, Mathf.Abs(f.z))));
                float tyl = Vector3.Dot(bS.center, f) - glS * .5f, cel = Vector3.Dot(sciana, f);   // oparcie przy ścianie / panelu
                so.transform.position += f * (cel - tyl); bS = BraxOsiedleAssety.Granice(so); cs = sciana + f * (glS * .5f);
            }
            else { SofaKod(cs, f, sofaW, sofaD); bS = Obrys(cs, f, sofaW, sofaD, yF, yF + .83f * K); }
            var ct = sciana + f * (glS + gap + tR);                                                   // stolik 0.4 m × K przed krawędzią siedziska
            Collider(bS.center, new Vector3(bS.size.x * .96f, bS.size.y, bS.size.z * .96f)); Mebel("loża " + lozeN + " sofa", bS);
            float yS = so ? bS.min.y + bS.size.y * .58f : yF + seatH;
            foreach (int sgn in new[] { -1, 1 }) klub.siedziska.Add((cs + r * sgn * sofaW * .25f + f * .08f * K + Vector3.up * yS, f));
            // fotele w U po bokach stolika, przodem do stolika
            foreach (int sgn in new[] { -1, 1 })
            {
                var ca = ct + r * sgn * (tR + gap + armD * .5f); var fa = -r * sgn;
                GameObject fo = skalaZ > 0f && idxFotel > 0 ? Czesc(zestaw, idxFotel, "loża " + lozeN + ": fotel", ca + Vector3.up * yF, fa, skalaZ) : null;
                Bounds bA;
                if (fo) { modeleUzyte++; bA = BraxOsiedleAssety.Granice(fo); }
                else { SofaKod(ca, fa, armW, armD); bA = Obrys(ca, fa, armW, armD, yF, yF + .83f * K); }
                Collider(bA.center, new Vector3(bA.size.x * .94f, bA.size.y, bA.size.z * .94f)); Mebel("loża " + lozeN + " fotel", bA);
                float yA = fo ? bA.min.y + bA.size.y * .58f : yF + seatH;
                klub.siedziska.Add((ca + fa * .06f * K + Vector3.up * yA, fa));
            }
            // stolik marmurowy przed siedziskiem
            GameObject to = stolik ? BraxOsiedleAssety.Postaw(root, stolik, "loża " + lozeN + ": stolik marmurowy", ct + Vector3.up * yF, 0f, .5f, null) : null;
            Bounds bT;
            if (to) { modeleUzyte++; bT = BraxOsiedleAssety.Granice(to); }
            else
            {
                Cylinder(marbleK, ct + Vector3.up * (yF + tH - .03f * K), tR, .03f * K, 24, true, false);
                Cylinder(chromeK, ct + Vector3.up * yF, .04f * K, tH - .03f * K, 10, false, false);
                Cylinder(chromeK, ct + Vector3.up * yF, .2f * K, .02f * K, 20, true, false);
                bT = new Bounds(ct + Vector3.up * (yF + tH * .5f), new Vector3(tR * 2f, tH, tR * 2f));
            }
            Collider(bT.center, bT.size * .9f); Mebel("loża " + lozeN + " stolik", bT);
            // na stoliku: kubełek z szampanem i dwie szklanki (z kodu, scalone)
            float yt = bT.max.y;
            Cylinder(chromeK, ct + Vector3.up * yt - r * .08f * K, .07f * K, .17f * K, 12, true, false);
            Cylinder(bottleCols[0], ct + Vector3.up * (yt + .06f * K) - r * .08f * K, .03f * K, .26f * K, 8, true, false);
            foreach (int sgn in new[] { -1, 1 }) Cylinder(glassM, ct + Vector3.up * yt + r * .12f * K + f * sgn * .08f * K, .03f * K, .1f * K, 10, false, false);
        }
        float xW = X0 + .09f;
        foreach (float z in new[] { 6.3f, 10.6f, 14.9f, 19.2f, 23.5f }) Loza(new Vector3(xW, 0, z), Vector3.right, true);
        foreach (float x in new[] { 71.6f, 75.9f }) Loza(new Vector3(x, 0, Z1 - .09f), Vector3.back, true);
        // 3.11 15:53 (właściciel): „żadnej ściany na dyskotece — loże w U, jedno U, drugie U, stoliki między”: ścianka wyspy usunięta,
        // loże wyspy stoją oparciami do siebie (sofa + 2 fotele = U wokół stolika), 1.2 m × K przejścia między kolejnymi U
        foreach (float z in new[] { 8.2f, 12.5f, 16.8f }) { Loza(new Vector3(76.3f, 0, z), Vector3.left, false); Loza(new Vector3(76.7f, 0, z), Vector3.right, false); }
        foreach (float x in new[] { 92.5f, 96.9f }) Loza(new Vector3(x, 0, Z0 + .09f), Vector3.forward, false);
        klub.loze = lozeN;

        // ================= DJ PREMIUM =================
        const float cx0 = 101.55f, cx1 = 102.45f, cz0 = 9.9f, cz1 = 14.1f;
        float hK = 1.0f * K;
        // runda 5 (klocki): konsola z cokołem cofniętym, fazą górnej krawędzi i frontem z 3 płycin (fugi 8 mm), blat stalowy z okapem
        var ciemnyK = Plain("cokół konsoli DJ", new Color(.04f, .04f, .045f), .3f);
        BoxMinMax(ciemnyK, cx0 + .05f, cx1 - .05f, sH, sH + .1f, cz0 + .05f, cz1 - .05f);
        BoxMinMax(lacquer, cx0, cx1, sH + .1f, sH + hK - .03f, cz0, cz1);
        BoxMinMax(lacquer, cx0 + .015f, cx1 - .015f, sH + hK - .03f, sH + hK, cz0 + .015f, cz1 - .015f, false);
        for (int q = 1; q < 3; q++) { float fz = cz0 + q * (cz1 - cz0) / 3f; BoxMinMax(ciemnyK, cx0 - .003f, cx0, sH + .22f, sH + hK - .14f, fz - .004f, fz + .004f, false); }
        foreach (float pz2 in new[] { cz0 + .5f, cz1 - .5f }) Cylinder(Plain("przelotka kabli", new Color(.06f, .06f, .07f), .4f), new Vector3((cx0 + cx1) * .5f + .25f, sH + hK + .03f, pz2), .03f, .004f, 12, true, false);
        BoxMinMax(steel, cx0 - .03f, cx1 + .03f, sH + hK, sH + hK + .03f, cz0 - .03f, cz1 + .03f, false, true);
        BoxMinMax(ledAmber, cx0 - .012f, cx0, sH + .15f, sH + .19f, cz0 + .1f, cz1 - .1f, false);
        BoxMinMax(ledAmber, cx0 - .012f, cx0, sH + hK - .12f, sH + hK - .08f, cz0 + .1f, cz1 - .1f, false);
        Mebel("DJ: konsola", new Bounds(new Vector3((cx0 + cx1) * .5f, sH + hK * .5f, (cz0 + cz1) * .5f), new Vector3(cx1 - cx0, hK, cz1 - cz0)));
        GameObject Nova(string plik)
        {
            string p = "Assets/Source/Claude/Nova/" + plik;
            if (!System.IO.File.Exists(p)) return null;
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(p) ?? AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
        }
        var mikser = Nova("dj_mixer.glb");
        bool mikserOk = mikser && BraxOsiedleAssety.Postaw(root, mikser, "DJ: mikser (dj_mixer.glb, NOVA)", new Vector3((cx0 + cx1) * .5f, sH + hK + .03f, 12f), 0f, 1.1f, null, false);
        if (!mikserOk)
        {
            foreach (float tz in new[] { 11.0f, 13.0f })
            {
                var tc = new Vector3((cx0 + cx1) * .5f, sH + hK + .03f, tz);
                Cylinder(chromeK, tc, .17f * K, .02f, 24, true, false);
                Cylinder(black, tc + Vector3.up * .02f, .15f * K, .005f, 24, true, false);
            }
            BoxMinMax(steel, cx0 + .2f, cx1 - .2f, sH + hK + .03f, sH + hK + .09f, 11.7f, 12.3f, false);
            Debug.LogError("BRAXHORN_DYSKOTEKA DO POPRAWKI: brak dj_mixer.glb — konsola z kodu");
        }
        klub.dj = new Vector3(103.2f, sH, 12f);
        // ściana LED za DJ-em: osobny obiekt (tekstura z kodu w BraxKlubEfekty), rama ze stali
        float lz0 = 7.6f, lz1 = 16.4f, ly0 = sH + .3f, ly1 = sH + .3f + 4.4f, lx = X1 - .06f;
        var mQuad = new Mesh(); mQuad.SetVertices(new List<Vector3> { new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0) });
        mQuad.SetNormals(new List<Vector3> { Vector3.left, Vector3.left, Vector3.left, Vector3.left });
        mQuad.SetUVs(0, new List<Vector2> { new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1) });
        mQuad.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
        if (!(klubSiatki.TryGetValue("ekran LED", out var mLed) && mLed)) ZapiszSiatke("ekran LED", mQuad);
        var ledM = Plain("klub ściana LED (wzór z kodu)", Color.black, .2f);
        if (!ledM.IsKeywordEnabled("_EMISSION")) { ledM.EnableKeyword("_EMISSION"); ledM.SetColor("_EmissionColor", Color.white * 1.5f); ledM.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; EditorUtility.SetDirty(ledM); }
        var led = Obiekt(root, "DJ: ściana LED", klubSiatki["ekran LED"], ledM, new Vector3(lx, ly0, lz0), Quaternion.identity, new Vector3(1, ly1 - ly0, lz1 - lz0));
        foreach (var (a0, a1, b0, b1) in new[] { (ly1, ly1 + .08f, lz0 - .08f, lz1 + .08f), (ly0 - .08f, ly0, lz0 - .08f, lz1 + .08f), (ly0, ly1, lz0 - .08f, lz0), (ly0, ly1, lz1, lz1 + .08f) })
            BoxMinMax(steel, lx - .02f, X1 - .015f, a0, a1, b0, b1, false);
        // lasery na kolumnach sceny (model laser_light.glb bez jego wiązek — wiązki z kodu, addytywne, wygasające)
        var laser = Nova("laser_light.glb");
        var mWiazki = Wachlarz("wachlarz lasera", 16f, .012f, new[] { -18f, -12f, -6f, 0f, 6f, 12f, 18f });
        var lasery = new List<Transform>(); var tylkoOtwarte = new List<GameObject>();
        foreach (var (kz, kol) in new[] { (7.8f, new Color(.1f, 1f, .2f, .5f)), (16.2f, new Color(1f, .1f, .15f, .5f)) })
        {
            var baza = new Vector3(101.55f, sH + 1.8f, kz);
            if (laser)
            {
                try
                {
                    var o = (GameObject)UnityEngine.Object.Instantiate(laser, root, false); o.name = "DJ: laser (laser_light.glb, NOVA)";
                    o.transform.position = Vector3.zero; o.transform.rotation = Quaternion.Euler(0, 270f, 0);
                    var ex = BraxOsiedleAssety.Znajdz(o.transform, "EXAMPLE_Lasers"); if (ex) UnityEngine.Object.DestroyImmediate(ex.gameObject);
                    var b = BraxOsiedleAssety.Granice(o); o.transform.localScale *= .32f * K / Mathf.Max(1e-4f, b.size.y);
                    b = BraxOsiedleAssety.Granice(o); o.transform.position += new Vector3(baza.x - b.center.x, baza.y - b.min.y, baza.z - b.center.z);
                    BraxOsiedleAssety.Przygotuj(o, false);
                }
                catch (Exception e) { Debug.LogError("BRAXHORN_DYSKOTEKA laser DO POPRAWKI: " + e.Message); }
            }
            else { BoxMinMax(black, baza.x - .12f, baza.x + .12f, baza.y, baza.y + .25f, kz - .12f, kz + .12f, false); }
            var mat = Addytywny("klub wiązka lasera " + (kol.g > .5f ? "zielona" : "czerwona"), kol);
            if (mat)
            {
                var piv = new GameObject("DJ: wachlarz lasera").transform; piv.SetParent(root, false);
                piv.position = baza + Vector3.up * .22f * K; piv.rotation = Quaternion.LookRotation(new Vector3(-1f, .06f, kz < 12f ? .18f : -.18f).normalized, Vector3.up);
                Obiekt(piv, "wiązki", mWiazki, mat, piv.position, piv.rotation, Vector3.one);
                lasery.Add(piv); tylkoOtwarte.Add(piv.gameObject);
            }
        }
        // kula dyskotekowa pod sufitem nad środkiem parkietu, na łańcuchu
        var kulaM = Nova("disco_ball.glb"); Transform kula = null;
        if (kulaM)
        {
            var o = BraxOsiedleAssety.Postaw(root, kulaM, "DJ: kula dyskotekowa (disco_ball.glb, NOVA)", new Vector3(93f, 4.6f, 12f), 0f, .55f, null, false);
            if (o)
            {
                var b = BraxOsiedleAssety.Granice(o);
                var piv = new GameObject("kula — oś obrotu").transform; piv.SetParent(root, false); piv.position = new Vector3(b.center.x, b.max.y, b.center.z);
                o.transform.SetParent(piv, true); kula = piv;
                Tube(chromeK, new Vector3(93f, b.max.y, 12f), new Vector3(93f, yC, 12f), .008f, 6);
            }
        }
        // stożki świateł ruchomych głów z kratownicy (addytywne, wygasają ku podłodze)
        var glowice = new List<Transform>(); var mSt = Stozek("stożek światła", 14);
        foreach (var (p, kol) in new[] { (new Vector3(88.6f, 5.15f, 8.6f), new Color(.9f, .2f, 1f, .22f)), (new Vector3(97.4f, 5.15f, 8.6f), new Color(.2f, .6f, 1f, .22f)),
                                         (new Vector3(88.6f, 5.15f, 15.4f), new Color(1f, .5f, .1f, .22f)), (new Vector3(97.4f, 5.15f, 15.4f), new Color(.2f, 1f, .6f, .22f)) })
        {
            var mat = Addytywny("klub stożek światła " + glowice.Count, kol); if (!mat) break;
            var piv = new GameObject("DJ: ruchoma głowa — stożek").transform; piv.SetParent(root, false); piv.position = p;
            Obiekt(piv, "stożek", mSt, mat, p, Quaternion.identity, Vector3.one * (p.y - yF - .3f));
            glowice.Add(piv); tylkoOtwarte.Add(piv.gameObject);
        }
        var ef = new GameObject("dyskoteka: efekty klubu (BraxKlubEfekty)").AddComponent<BraxKlubEfekty>(); ef.transform.SetParent(root, false);
        ef.ekranLed = led.GetComponent<Renderer>(); ef.kula = kula; ef.lasery = lasery.ToArray(); ef.glowice = glowice.ToArray(); ef.tylkoGdyOtwarte = tylkoOtwarte.ToArray();
        ef.srodek = new Vector3((X0 + X1) * .5f, 2f, (Z0 + Z1) * .5f); ef.promien = 34f;
        // ochrona w sali przy wejściu (poza strefą 2.5 m za drzwiami)
        klub.ochrona = new Vector3(dB + 2.8f, yF, Z1 - 2.6f);

        // ================= DETALE =================
        int listwy = 0, gniazdka = 0, wlaczniki = 0, kratki = 0, gasnice = 0, znaki = 0, oscieznice = 0;
        float lH = .08f * K, lT = .015f;
        void ListwaX(float x0, float x1, float zf, float s) { if (x1 - x0 < .05f) return; BoxMinMax(black, x0, x1, yF, yF + lH, Mathf.Min(zf + s * .012f, zf + s * (.012f + lT)), Mathf.Max(zf + s * .012f, zf + s * (.012f + lT)), false); listwy++; }
        void ListwaZ(float z0, float z1, float xf, float s) { if (z1 - z0 < .05f) return; BoxMinMax(black, Mathf.Min(xf + s * .012f, xf + s * (.012f + lT)), Mathf.Max(xf + s * .012f, xf + s * (.012f + lT)), yF, yF + lH, z0, z1, false); listwy++; }
        ListwaZ(Z0, Z1, X0, 1f);
        ListwaZ(Z0, 7f, X1, -1f); ListwaZ(17f, 23.35f, X1, -1f);
        ListwaX(X0, bx0, Z0, 1f); ListwaX(bx1, dA - .1f, Z0, 1f); ListwaX(dB + .1f, X1, Z0, 1f);
        ListwaX(X0, dA - .1f, Z1, -1f); ListwaX(dB + .1f, 95.85f, Z1, -1f);
        // obrzeże sufitu (gzyms wewnętrzny 10 × 10 cm) — poza szatnią
        float gz = .1f;
        BoxMinMax(black, X0 + .012f, X0 + .012f + gz, yC - gz, yC - .001f, Z0, Z1, false, true);
        BoxMinMax(black, X1 - .012f - gz, X1 - .012f, yC - gz, yC - .001f, Z0, 23.35f, false, true);
        BoxMinMax(black, X0, X1, yC - gz, yC - .001f, Z0 + .012f, Z0 + .012f + gz, false, true);
        BoxMinMax(black, X0, 95.85f, yC - gz, yC - .001f, Z1 - .012f - gz, Z1 - .012f, false, true);
        // ościeżnice i opaski obu drzwi, znak WYJŚCIE nad drzwiami, gaśnica obok, włącznik
        foreach (var (zf, s) in new[] { (Z0, 1f), (Z1, -1f) })
        {
            float f0 = zf + s * .012f, f1 = zf + s * .06f, zmin = Mathf.Min(f0, f1), zmax = Mathf.Max(f0, f1);
            if (s > 0f)   // wejście z ulicy (club-front, z 28.82) — ościeżnicę dostaje przy zwężeniu bramy (E29b, BraxClaudeOsiedleBramy)
            {
                BoxMinMax(steel, dA - .1f, dA, yF, dH + .1f, zmin, zmax, false); BoxMinMax(steel, dB, dB + .1f, yF, dH + .1f, zmin, zmax, false);
                BoxMinMax(steel, dA - .1f, dB + .1f, dH, dH + .1f, zmin, zmax, false, true); oscieznice++;
            }
            float zz = zf + s * .09f;
            BoxMinMax(exitG, 86f - .3f * K, 86f + .3f * K, dH + .18f, dH + .18f + .17f * K, Mathf.Min(zf + s * .03f, zz), Mathf.Max(zf + s * .03f, zz), false, true);
            try { var g0 = group; SzyldLitery("dyskoteka", "wyjście " + (s > 0 ? "S" : "N"), "WYJŚCIE", new Vector3(86f, dH + .18f + .045f * K, zz), new Vector3(0, 0, s), .075f * K, MatLicoBiale, MatBokiCzarne, .006f, .004f); group = g0; }
            catch (Exception e) { Debug.LogError("BRAXHORN_DETALE dyskoteka znak WYJŚCIE DO POPRAWKI: " + e.Message); }
            znaki++;
            // gaśnica na wsporniku (uchwyt ~1.4 m × K), tabliczka nad nią
            var gs = new Vector3(dA - .6f, yF + .6f * K, zf + s * (.012f + .1f * K));
            BoxMinMax(steel, gs.x - .06f, gs.x + .06f, gs.y + .1f, gs.y + .4f * K, Mathf.Min(zf + s * .012f, gs.z), Mathf.Max(zf + s * .012f, gs.z), false);
            Cylinder(red, gs, .075f * K, .45f * K, 14, true, false);
            Cylinder(black, gs + Vector3.up * .45f * K, .025f * K, .07f * K, 8, true, false);
            Tube(black, gs + Vector3.up * .5f * K + Vector3.right * .02f, gs + Vector3.up * .15f * K + Vector3.right * .085f * K, .01f * K, 5);
            BoxMinMax(red, gs.x - .1f * K, gs.x + .1f * K, gs.y + .62f * K, gs.y + .82f * K, Mathf.Min(zf + s * .012f, zf + s * .02f), Mathf.Max(zf + s * .012f, zf + s * .02f), false);
            gasnice++;
            // włącznik przy drzwiach (1.1 m × K)
            var wp = new Vector3(dB + .45f, yF + 1.1f * K, zf);
            BoxMinMax(white, wp.x - .045f * K, wp.x + .045f * K, wp.y - .045f * K, wp.y + .045f * K, Mathf.Min(zf + s * .012f, zf + s * .024f), Mathf.Max(zf + s * .012f, zf + s * .024f), false);
            BoxMinMax(white, wp.x - .02f * K, wp.x + .02f * K, wp.y - .03f * K, wp.y + .03f * K, Mathf.Min(zf + s * .024f, zf + s * .03f), Mathf.Max(zf + s * .024f, zf + s * .03f), false);
            wlaczniki++;
        }
        // gniazdka podwójne (0.3 m × K): ściany wolne od mebli
        void Gniazdko(Vector3 p, Vector3 n)
        {
            var r = Vector3.Cross(Vector3.up, n); float y = yF + .3f * K;
            var c = new Vector3(p.x, y, p.z) + n * .018f;
            OBox(white, c, new Vector3(.15f * K, .08f * K, .012f), Quaternion.LookRotation(n).eulerAngles.y);
            foreach (int sgn in new[] { -1, 1 }) OBox(black, c + n * .007f + r * sgn * .035f * K, new Vector3(.03f * K, .03f * K, .003f), Quaternion.LookRotation(n).eulerAngles.y);
            gniazdka++;
        }
        foreach (float x in new[] { 90.3f, 99.5f }) Gniazdko(new Vector3(x, 0, Z0), Vector3.forward);
        foreach (float x in new[] { 80.6f, 92.0f }) Gniazdko(new Vector3(x, 0, Z1), Vector3.back);
        foreach (float z in new[] { 3.0f, 20.5f }) Gniazdko(new Vector3(X1, 0, z), Vector3.left);
        foreach (float x in new[] { 70.5f, 79.5f }) { var c = new Vector3(x, yF + hB + .12f, Z0 + .041f); OBox(white, c, new Vector3(.15f * K, .08f * K, .012f), 0f); gniazdka++; }   // nad blatem zaplecza
        // kratki wentylacyjne w suficie (2 cm pod sufitem, lamele)
        foreach (var (x, z) in new[] { (72f, 12f), (72f, 21f), (82f, 6f), (82f, 18f), (93f, 3f), (93f, 21f), (102f, 3.5f) })
        {
            float y0 = yC - .04f;
            BoxMinMax(steel, x - .3f * K, x + .3f * K, y0, y0 + .015f, z - .15f * K, z + .15f * K, false, true);
            for (int i = 0; i < 6; i++) { float zz = z - .12f * K + i * .048f * K; BoxMinMax(black, x - .28f * K, x + .28f * K, y0 - .02f, y0, zz - .006f, zz + .006f, false, true); }
            kratki++;
        }
        // oprawy sufitowe z pierścieniem i kloszem-soczewką (strefa lóż i przejścia)
        foreach (float x in new[] { 70.5f, 74f, 79f, 82.5f })
            foreach (float z in new[] { 6.5f, 12.5f, 18.5f, 24.5f })
            {
                var c = new Vector3(x, yC - .1f * K, z);
                Cylinder(black, c, .1f * K, .1f * K - .002f, 14, false, false);
                DyskDol(brass, c, .1f * K);                                   // pierścień oprawy (od spodu)
                DyskDol(lens, c - Vector3.up * .004f, .075f * K);             // klosz-soczewka 4 mm niżej (bez pary współpłaszczyznowej)
                oprawy++;
            }
        // kolizje mebli parami (cel 0)
        var pary = new List<string>();
        for (int i = 0; i < klubMeble.Count; i++)
            for (int j = i + 1; j < klubMeble.Count; j++)
            {
                var a = klubMeble[i].b; var b = klubMeble[j].b; a.Expand(-.02f); b.Expand(-.02f);
                if (a.Intersects(b)) pary.Add(klubMeble[i].co + " × " + klubMeble[j].co);
            }
        Debug.Log("BRAXHORN_MEBLE_KOLIZJE lokal=dyskoteka meble=" + klubMeble.Count + " pary=" + pary.Count + (pary.Count > 0 ? " [" + string.Join("; ", pary.Take(12)) + "]" : ""));
        Debug.Log("BRAXHORN_DETALE lokal=dyskoteka listwy=" + listwy + " gniazdka=" + gniazdka + " wlaczniki=" + wlaczniki + " kratki=" + kratki + " oprawy=" + oprawy + " gasnice=" + gasnice + " znaki_ewak=" + znaki
                  + " oscieznice=" + oscieznice + " sufit_obrzeze=tak cokoly=tak(bar, zaplecze, sofy) fugi=tak(parkiet: szczeliny 6 cm w geometrii; posadzka: żywica bez fug) parapety=— (brak okien) kratki_odplywowe=— (brak WC)");
        BraxOsiedleAssety.Log("dyskoteka: modele użyte=" + modeleUzyte + " brak=" + modeleBrak + " (brak = wersja z kodu)");
        rep.notes.Add("dyskoteka premium: bar 12.8 m pod ścianą S, " + lozeN + " lóż, DJ (LED, lasery, kula), detale; modele " + modeleUzyte + ", zapas z kodu " + modeleBrak);
    }

    // ---------------- ludzie w klubie (etap ludzi) ----------------
    static void DyskotekaLudzie(System.Random rnd, string[] lada, string[] stoi)
    {
        var g = Group("Ludzie / dyskoteka");
        int obsluga = 0, bar = 0, siedzi = 0, tanczy = 0, pary = 0, dj = 0;
        void Wnetrze(BraxMieszkaniec m) { if (m) m.wnetrze = true; }
        // barmani — podpięci pod stanowiska nalewania
        int podpieci = 0;
        if (klub != null)
            for (int i = 0; i < klub.barmani.Count; i++)
            {
                var m = Obsada("dyskoteka — barman " + (i + 1), RolaObsluga, "barman", klub.barmani[i], 0f, rnd, lada, klub.barmani[i].y);
                if (!m) continue; Wnetrze(m); obsluga++;
                if (klubBar && klubBar.stanowiska != null && i < klubBar.stanowiska.Length)
                {
                    var t = m.GetComponentsInChildren<Transform>(true);
                    var s = klubBar.stanowiska[i]; s.barman = m.transform;
                    s.ramie = t.FirstOrDefault(x => x.name == "Bip01 R UpperArm"); s.dlon = t.FirstOrDefault(x => x.name == "Bip01 R Hand");
                    if (s.ramie && s.dlon) podpieci++;
                }
            }
        Wnetrze(Obsada("dyskoteka — szatnia", RolaObsluga, "szatnia", new Vector3(97.9f, 0, 26.3f), 270f, rnd, stoi, .06f)); obsluga++;
        Obsada("dyskoteka — ochrona przy wejściu", RolaOchrona, "ochrona", new Vector3(89.5f, 0, 30.1f), 0f, rnd, new[] { "idle_neutral_01", "idle_waiting_01", "idle_look_around_01" });
        if (klub != null)
        {
            Wnetrze(Obsada("dyskoteka — ochrona w sali", RolaOchrona, "ochrona", klub.ochrona, 225f, rnd, new[] { "idle_neutral_01", "idle_look_around_01" }, klub.ochrona.y)); obsluga++;
            var mdj = Obsada("dyskoteka — DJ", RolaDj, "dj", klub.dj, 270f, rnd, new[] { "dancing_neutral", "work_mid", "dancing_cool" }, klub.scena);
            if (mdj) { Wnetrze(mdj); mdj.zmianaMin = 6f; mdj.zmianaMax = 12f; dj = 1; }
            // przy barze: stoją z drinkiem (przodem do lady)
            foreach (var (p, f) in klub.barStoja)
            {
                if (!Wolne(p)) continue;
                var m = NowyMieszkaniec(RolaImpreza, g, "Przy barze", p, Quaternion.LookRotation(f).eulerAngles.y, rnd, new[] { "drink_drinking", "idle_neutral_01" });
                if (m) { Wnetrze(m); bar++; rbInni++; }
            }
            // loże: ok. 2/3 miejsc zajęte
            foreach (var (p, f) in klub.siedziska.OrderBy(_ => rnd.Next()).Take(Mathf.Min(24, klub.siedziska.Count)))
            {
                var m = Siedzacy(RolaImpreza, g, "Loża", p, f, rnd, new[] { "sit_chair_breathe_01", "sit_chair_idle_neutral_02" });
                if (m) { Wnetrze(m); siedzi++; }
            }
        }
        // parkiet 14 × 12 m: siatka 6 × 4 z rozrzutem, twarzą do DJ-a (wschód)
        var taniec = new[] { "dancing_cool", "dancing_neutral", "dancing_silly", "cheer_01" };
        for (int i = 0; i < 6; i++)
            for (int j = 0; j < 4; j++)
            {
                var p = new Vector3(87.3f + i * 2.3f + (float)(rnd.NextDouble() - .5) * .8f, .11f, 7.4f + j * 3.0f + (float)(rnd.NextDouble() - .5) * .8f);
                if (!Wolne(p)) continue;
                var m = NowyMieszkaniec(RolaImpreza, g, "Tańczy", p, 90f + (float)(rnd.NextDouble() * 100 - 50), rnd, taniec);
                if (m) { Wnetrze(m); m.zmianaMin = 8f; m.zmianaMax = 20f; tanczy++; rbInni++; }
            }
        // rozmowy w przejściach między lożami
        foreach (var p0 in new[] { new Vector3(71.2f, .06f, 8.4f), new Vector3(71.2f, .06f, 17.2f), new Vector3(82.4f, .06f, 9.6f), new Vector3(82.4f, .06f, 20.6f) })
        {
            var a = p0 - Vector3.forward * .5f; var b = p0 + Vector3.forward * .5f;
            if (!Wolne(a, .28f) || !Wolne(b, .28f)) continue;
            var m1 = NowyMieszkaniec(RolaImpreza, g, "Rozmawia", a, 0f, rnd, KlipySlucha);
            var m2 = NowyMieszkaniec(RolaImpreza, g, "Rozmawia", b, 180f, rnd, KlipySlucha);
            if (m1 && m2) { m1.rozmowca = m2; m2.rozmowca = m1; m1.lider = true; Wnetrze(m1); Wnetrze(m2); pary++; rbInni += 2; }
            else { if (m1) UnityEngine.Object.DestroyImmediate(m1.gameObject); if (m2) UnityEngine.Object.DestroyImmediate(m2.gameObject); }
        }
        int ludzie = obsluga + dj + bar + siedzi + tanczy + pary * 2;
        Debug.Log("BRAXHORN_DYSKOTEKA loze=" + (klub != null ? klub.loze : 0) + " ludzie=" + ludzie + " dj=" + dj + " tanczy=" + tanczy + " siedzi=" + siedzi + " bar=" + bar + " rozmowy=" + pary + " obsluga=" + obsluga
                  + " (w sali; bez cienia — wnętrze, animacja wg BraxLudzie.Limit/promienia)");
        int butelki = klubBar && klubBar.stanowiska != null ? klubBar.stanowiska.Sum(s => s.butelki != null ? s.butelki.Length : 0) : 0;
        Debug.Log("BRAXHORN_BAR nalewanie=" + (podpieci > 0 ? "ok" : "brak_barmana DO POPRAWKI") + " butelki_osobne=" + butelki + " stanowiska=" + (klubBar && klubBar.stanowiska != null ? klubBar.stanowiska.Length : 0)
                  + " barmani_podpieci=" + podpieci + " zamow=tak");
    }
}
