// PODGLĄD NOWEJ CZĘŚCI MIASTA R1 (16.11.2026, właściciel 19:02: „bez zdjęcia nic nie akceptuje”): prawdziwy kod gry poza Unity.
// Wieżowce C5_U07 / C5_U04 (promenada) i C5_U12 (Aleja Centralna) z lokalami w parterach — BraxRegionFawela.WiezaMurowana z tymi
// samymi numerami stylów co w grze (kolejność tablicy budynków), wnętrza z generatora ulicy; plac ustawek i komis — kroki generatora
// nowych dzielnic (BraxRegionMiastoBNoweMiejsca). Ulica i chodniki dookoła uproszczone (tylko tło zdjęć). Zapis jak kontrola geometrii.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class Harness
{
    public static Func<string, bool> MaModel = id => true;
    public static Func<string, bool> MaTeksture = slug => true;
}

public static class Podglad
{
    sealed class Pow { public Renderer r; public int sub; public Material m; public string nazwa; public bool ruchomy; }
    struct Tr { public int pow, os, znak; public float d; public Vector2 a, b, c; public float u0, u1, v0, v1; public Vector3 n; public float pd; }
    static string Sciezka(Transform t) { var l = new List<string>(); for (var x = t; x != null; x = x.parentT) l.Add(x.name); l.Reverse(); return string.Join(" / ", l.Skip(1)); }
    static bool Ruchomy(Transform t)
    {
        for (var x = t; x != null; x = x.parentT)
            if (x.gameObject.GetComponent<BraxCityDoor>() != null || x.gameObject.GetComponent<BraxDrzwiPrzesuwne>() != null) return true;
        return false;
    }

    // numer wieżowca w kolejności tablicy budynków gry (styl i szkło zależą od kolejności) — C5: U12 0, U08 1, U00 2, U03 3, U07 4, U04 5…
    static readonly string[] KolejnoscC5 = { "C5_U12", "C5_U08", "C5_U00", "C5_U03", "C5_U07", "C5_U04", "C5_U09", "C5_U02", "C5_U05", "C5_U06", "C5_U01", "C5_U10", "C5_U11", "C5_U13" };
    static readonly HashSet<int> Szklane = new HashSet<int> { 1, 3, 5, 8, 10, 12 };
    static void Wieza(Transform g, string id)
    {
        int nr = Array.IndexOf(KolejnoscC5, id);
        BraxRegionFawela.wiez = nr; BraxRegionFawela.wiezSzklanych = Enumerable.Range(0, nr).Count(Szklane.Contains); BraxRegionFawela.wiezMurowanych = nr - BraxRegionFawela.wiezSzklanych;
        var (x, z, w, d) = BraxRegionMiastoB.Budynek(id);
        BraxRegionFawela.WiezaMurowana(g, id, x, .15f, z, w, d);
    }

    static void Portowa(Transform root)
    {
        var gp = new GameObject("PORTOWA (podgląd: ulica główna z banerem)").transform; gp.SetParent(root, false);
        var kolizje = new GameObject("kolizje").transform; kolizje.SetParent(gp, false);
        BraxRegionMiastoB.Plaski("Portowa: jezdnia (podgląd)", BraxRegionMiastoB.Asfalt, Rect.MinMaxRect(-106f, 23.755f, 54f, 33.245f), .003f);
        BraxRegionMiastoB.ChodnikPub("Portowa: chodnik płd. (podgląd)", -106f, 54f, 20.6f, 23.755f);
        BraxRegionMiastoB.ChodnikPub("Portowa: chodnik płn. (podgląd)", -106f, 54f, 33.245f, 36.1f);
        var T = typeof(BraxUlicePierzeje); var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
        T.GetField("style", F).SetValue(null, T.GetMethod("Style", F).Invoke(null, null));
        var lokale = (System.Collections.IDictionary)T.GetField("Lokale", F).GetValue(null);
        // wszystkie 17 kamienic ulicy głównej — dane jak BraxCityArchitecture.StreetBuildings (bez BRAXHORN TOWER)
        var budynki = new[] {
            new BraxCityArchitecture.Building("apothecary","APTEKA",38,52,-4,20.6f,8),
            new BraxCityArchitecture.Building("corner_store","DELIKATESY",26,38,-4,20.6f,6),
            new BraxCityArchitecture.Building("bar", "BAR 24", -12, -1, 11.6f, 20.6f, 7),
            new BraxCityArchitecture.Building("restaurant", "BISTRO", -24, -12, 11.6f, 20.6f, 10),
            new BraxCityArchitecture.Building("workshop_shop", "PRACOWNIA", -36, -24, 11.6f, 20.6f, 7, false, true),
            new BraxCityArchitecture.Building("hotel","HOTEL",-48,-36,11.6f,20.6f,11),
            new BraxCityArchitecture.Building("viaduct_corner","BRAXHORN",-61.4f,-48,11.6f,20.6f,8),
            new BraxCityArchitecture.Building("workshop","WARSZTAT",-87,-72.5f,11.6f,20.6f,6,false,true),
            new BraxCityArchitecture.Building("estate_corner","KAWIARNIA",-104,-87,11.6f,20.6f,9),
            new BraxCityArchitecture.Building("bakery","PIEKARNIA",18,32,36.1f,48,6,true),
            new BraxCityArchitecture.Building("gallery", "GALERIA", -10, 2, 36.1f, 48, 8, true, true),
            new BraxCityArchitecture.Building("office_entrance", "BRAXHORN", -22, -10, 36.1f, 48, 7, true, true),
            new BraxCityArchitecture.Building("apartments_1", "KSIĘGARNIA", -34, -22, 36.1f, 48, 9, true),
            new BraxCityArchitecture.Building("apartments_2","KAWIARNIA",-46,-34,36.1f,48,7,true,true),
            new BraxCityArchitecture.Building("apartments_3","PRACOWNIA",-61.4f,-46,36.1f,48,8,true),
            new BraxCityArchitecture.Building("estate_apartments_1","BRAXHORN",-88,-71,36.1f,48,6,true,true),
            new BraxCityArchitecture.Building("estate_apartments_2","BRAXHORN",-104,-88,36.1f,48,9,true) };
        foreach (var b in budynki)
        {
            var lok = lokale[b.id];
            var f = T.GetMethod("Front", F).Invoke(null, new object[] { b, lok, false });
            BraxUliceKit.UstawGrupe("Elewacja " + b.id);
            T.GetMethod("Elewacja", F).Invoke(null, new object[] { f, kolizje });
            BraxUliceKit.UstawGrupe("Szyldy " + b.id, 90f);
            T.GetMethod("Szyldy", F).Invoke(null, new object[] { f });
            // 18.11 14:21: drzwi lokalu jak w grze (BraxUlicePierzeje.Drzwi → BraxDrzwiLokalu.Podwojne) — szyld nad drzwiami
            try { T.GetMethod("Drzwi", F).Invoke(null, new object[] { gp, b, lok, f }); } catch (Exception e) { Console.WriteLine("BŁĄD drzwi " + b.id + ": " + (e.InnerException ?? e).Message); }
        }
        BraxUliceKit.Zapisz(gp, "portowa_podglad");
        var opis = BraxMiastoBanery.NadUlica(gp, -17f, 20.6f, 36.1f, -18f, -16f, .003f, 8f);
        Console.WriteLine("LOG PORTOWA: " + (opis ?? "brak paczki banerów"));
    }

    public static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        string outDir = args.Length > 0 ? args[0] : "out"; Directory.CreateDirectory(outDir);
        float limitCm = 2f;
        var miasto = new GameObject("BraxhornCity");
        new GameObject("swiat").AddComponent<BraxCityWorld>();
        var root = new GameObject("CLAUDE miasto B (podgląd nowej części)").transform; root.SetParent(miasto.transform, false);
        BraxRegionMiastoB.root = root; BraxRegionMiastoB.Materialy();
        // ---- teren: kwartały A1, C1, C2 z płyt bez posadzek lokali (prawdziwy krok gry), aleja i ul. Tramwajowa uproszczone
        // tło: jezdnie ulic C0–C3 i Alei (asfalt 2 cm pod płytami nie sięga pod nie — tylko w szczelinach między kwartałami)
        foreach (var (x0, x1) in new[] { (-459.75f, -440.25f), (-609.75f, -590.25f), (-309.75f, -290f) })
            foreach (var (z0, z1) in new[] { (-60f, 23.7f), (33.3f, 100f) })   // bez skrzyżowania z jezdnią alei (ona leży osobno)
                BraxRegionMiastoB.Plaski("ulica C (podgląd)", BraxRegionMiastoB.Asfalt, Rect.MinMaxRect(x0, z0, x1, z1), .003f);
        var bez = BraxRegionMiastoB.PodgladPosadzki().Select(q => q.Item2).ToList();
        foreach (var (xa, xb) in new[] { (-440.25f, -309.75f), (-590.25f, -459.75f) })
        {
            BraxRegionMiastoB.PodgladChodnikBez("kwartał A (płyty)", Rect.MinMaxRect(xa, 40.5f, xb, 91f), bez);
            BraxRegionMiastoB.PodgladChodnikBez("kwartał C z promenadą (płyty)", Rect.MinMaxRect(xa, -52.5f, xb, 16.5f), bez);
        }
        var trawa = BraxRegionMeble.Tex("trawnik (podgląd)", 3f, new Color(.62f, .72f, .48f), .95f, "leafy_grass", "aerial_grass_rock");
        BraxRegionMiastoB.ChodnikPub("aleja: chodnik płd. (podgląd)", -600f, -300f, 16.5f, 21.5f); BraxRegionMiastoB.ChodnikPub("aleja: chodnik płn. (podgląd)", -600f, -300f, 35.5f, 40.5f);
        BraxRegionMiastoB.Plaski("aleja: pas zieleni (podgląd)", trawa, Rect.MinMaxRect(-600f, 21.5f, -300f, 23.75f), .12f); BraxRegionMiastoB.Plaski("aleja: pas zieleni (podgląd)", trawa, Rect.MinMaxRect(-600f, 33.25f, -300f, 35.5f), .12f);
        BraxRegionMiastoB.Plaski("aleja: jezdnia (podgląd)", BraxRegionMiastoB.Asfalt, Rect.MinMaxRect(-600f, 23.75f, -300f, 33.25f), .003f);
        // ---- wieżowce z lokalami
        var gb = new GameObject("CLAUDE nowe dzielnice: budynki").transform; gb.SetParent(root, false);
        foreach (var id in new[] { "C5_U12", "C5_U07", "C5_U04", "C5_U09", "C5_U02", "C5_U08", "C5_U00", "C5_U03" }) { try { Wieza(gb, id); } catch (Exception e) { Console.WriteLine("BŁĄD wieżowiec " + id + ": " + e); } }
        try { BraxRegionMiastoB.PodgladLokaleKontrola(); } catch (Exception e) { Console.WriteLine("kontrola lokali: " + e.Message); }
        // ---- plac ATLANTA z podwórzem ustawek, pas zieleni z komisem przy ul. Tramwajowej
        BraxRegionMiastoB.PodgladPlacAtlanty();
        // tło podglądu: trawa 3 cm pod wszystkim (bez pustki pod płytami poza wyciętym kawałkiem miasta) — w grze dookoła stoi miasto
        BraxRegionMiastoB.Plaski("tło podglądu (trawa)", trawa, Rect.MinMaxRect(-1000f, -200f, -200f, 300f), -.03f);
        BraxRegionMiastoB.Plaski("pas zieleni na południu (podgląd)", trawa, Rect.MinMaxRect(-759.75f, -85.45f, -738f, -72f), .12f);
        BraxRegionMiastoB.Plaski("pas zieleni na południu (podgląd)", trawa, Rect.MinMaxRect(-688f, -85.45f, -660f, -72f), .12f);
        BraxRegionMiastoB.ChodnikPub("ul. Tramwajowa: chodnik płd. (podgląd)", -759.75f, -660f, -72f, -67f);
        BraxRegionMiastoB.Plaski("ul. Tramwajowa: jezdnia (podgląd)", BraxRegionMiastoB.Asfalt, Rect.MinMaxRect(-759.75f, -66.75f, -660f, -57.75f), .003f);
        BraxRegionMiastoB.ChodnikPub("ul. Tramwajowa: chodnik płn. (podgląd)", -759.75f, -660f, -57.5f, -52.5f);
        BraxRegionMiastoB.PodgladKomisNawierzchnia();
        foreach (var (n, a) in new (string, Action)[] { ("plac ustawek", BraxRegionMiastoB.PodgladPlacUstawek), ("komis", BraxRegionMiastoB.PodgladKomis) })
            try { a(); } catch (Exception e) { Console.WriteLine("BŁĄD " + n + ": " + e); }
        // ---- PORTOWA (ulica główna R1), fragment x −40…6: elewacje z prawdziwego generatora pierzei (BraxUlicePierzeje.Front/Elewacja/
        // Szyldy przez refleksję, te same dane budynków co w grze), jezdnia i chodniki uproszczone, bryły za elewacjami pominięte; baner
        // właściciela nad jezdnią (BraxMiastoBanery.NadUlica — ten sam wywołanie co w BraxMiastoUlica.Apply)
        try { Portowa(root); } catch (Exception e) { Console.WriteLine("BŁĄD Portowa: " + e); }
        File.WriteAllText(Path.Combine(outDir, "opis.txt"), string.Join("\n", Rejestr.Notatki) + "\n\nAKCJE:\n" + string.Join("\n", BraxMiastoAAkcje.Lista.Select(q => q.id + " — " + q.etykieta + " @ " + q.p.ToString("F1"))));
        // ---------------- zbiór powierzchni
        var pows = new List<Pow>(); var mats = new List<Material>(); var matIdx = new Dictionary<Material, int>();
        var rend = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.gameObject.activeSelf).ToList();
        var pos = new List<float>(); var nor = new List<float>(); var uvs = new List<float>(); var idx = new List<int>(); var kol = new List<float>();
        var meshe = new List<string>();
        var tris = new List<Tr>();
        foreach (var r in rend)
        {
            var mf = r.gameObject.GetComponent<MeshFilter>(); var mesh = mf?.sharedMesh; if (mesh == null || mesh.vertices.Length == 0) continue;
            var t = r.transform; var rot = t.rotation; bool ruchT = Ruchomy(t);
            var wv = mesh.vertices.Select(v => t.TransformPoint(v)).ToArray();
            var wn = (mesh.normals.Length == wv.Length ? mesh.normals : new Vector3[wv.Length]).Select(n => (rot * n).normalized).ToArray();
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var m = s < r.sharedMaterials.Length ? r.sharedMaterials[s] : r.sharedMaterial; if (m == null) continue;
                if (!matIdx.TryGetValue(m, out int mi)) { mi = mats.Count; mats.Add(m); matIdx[m] = mi; }
                int pw = pows.Count; pows.Add(new Pow { r = r, sub = s, m = m, nazwa = Sciezka(t), ruchomy = ruchT });
                var tri = mesh.GetTriangles(s); if (tri.Length == 0) continue;
                // eksport (wierzchołki tylko użyte przez tę partię)
                var map = new Dictionary<int, int>(); int v0 = pos.Count / 3, i0 = idx.Count;
                foreach (int k in tri)
                {
                    if (!map.TryGetValue(k, out int nk))
                    {
                        nk = map.Count; map[k] = nk; var p = wv[k]; var n = wn[k]; var uv = k < mesh.uv.Length ? mesh.uv[k] : Vector2.zero;
                        pos.Add(p.x); pos.Add(p.y); pos.Add(p.z); nor.Add(n.x); nor.Add(n.y); nor.Add(n.z); uvs.Add(uv.x); uvs.Add(uv.y);
                        var cc = mesh.colors32 != null && k < mesh.colors32.Length ? mesh.colors32[k] : new Color32(255, 255, 255, 255);   // kolor wierzchołka (shader miasta mnoży albedo)
                        kol.Add(cc.r / 255f); kol.Add(cc.g / 255f); kol.Add(cc.b / 255f);
                    }
                    idx.Add(nk);
                }
                meshe.Add("{\"n\":" + Js(pows[pw].nazwa) + ",\"m\":" + mi + ",\"v0\":" + v0 + ",\"vn\":" + map.Count + ",\"i0\":" + i0 + ",\"in\":" + tri.Length + ",\"ruch\":" + (ruchT ? "true" : "false") + "}");
                // trójkąty osiowe do kontroli
                for (int i = 0; i + 2 < tri.Length; i += 3)
                {
                    Vector3 a = wv[tri[i]], b = wv[tri[i + 1]], c = wv[tri[i + 2]];
                    var n = Vector3.Cross(b - a, c - a); float p2 = n.magnitude; if (p2 < 1e-5f) continue; n /= p2;
                    int os = Math.Abs(n.x) > Math.Abs(n.y) ? (Math.Abs(n.x) > Math.Abs(n.z) ? 0 : 2) : (Math.Abs(n.y) > Math.Abs(n.z) ? 1 : 2);
                    float no = os == 0 ? n.x : os == 1 ? n.y : n.z; if (Math.Abs(no) < .995f) continue;
                    Vector2 P(Vector3 q) => os == 0 ? new Vector2(q.z, q.y) : os == 1 ? new Vector2(q.x, q.z) : new Vector2(q.x, q.y);
                    float D(Vector3 q) => os == 0 ? q.x : os == 1 ? q.y : q.z;
                    var A = P(a); var B2 = P(b); var C = P(c);
                    tris.Add(new Tr { pow = pw, os = os, znak = no > 0 ? 1 : 0, d = (D(a) + D(b) + D(c)) / 3f, a = A, b = B2, c = C, n = n, pd = Vector3.Dot(n, a),
                        u0 = Math.Min(A.x, Math.Min(B2.x, C.x)), u1 = Math.Max(A.x, Math.Max(B2.x, C.x)), v0 = Math.Min(A.y, Math.Min(B2.y, C.y)), v1 = Math.Max(A.y, Math.Max(B2.y, C.y)) });
                }
            }
        }
        // ---------------- zapis geometrii
        using (var f = new BinaryWriter(File.Create(Path.Combine(outDir, "geom.bin"))))
        {
            foreach (var x in pos) f.Write(x); foreach (var x in nor) f.Write(x); foreach (var x in uvs) f.Write(x); foreach (var x in idx) f.Write(x); foreach (var x in kol) f.Write(x);
        }
        var js = new StringBuilder("{\"nv\":" + pos.Count / 3 + ",\"ni\":" + idx.Count + ",\"kolory\":true,\"meshe\":[" + string.Join(",\n", meshe) + "],\n\"materialy\":[");
        js.Append(string.Join(",\n", mats.Select(m =>
        {
            var c = m.color; var e = m.c.TryGetValue("_EmissionColor", out var ee) ? ee : Color.black;
            return "{\"n\":" + Js(m.name) + ",\"rodzaj\":\"" + m.rodzaj + "\",\"slug\":" + Js(m.slugi.FirstOrDefault() ?? "") + ",\"metry\":" + F(m.metry) + ",\"kolor\":[" + F(c.r) + "," + F(c.g) + "," + F(c.b) + "," + F(c.a) + "],\"rough\":" + F(m.GetFloat("_Roughness")) + ",\"metal\":" + F(m.GetFloat("_Metallic")) + ",\"emisja\":[" + F(e.r) + "," + F(e.g) + "," + F(e.b) + "],\"plik\":" + Js(m.mainTexture != null ? m.mainTexture.name ?? "" : "") + ",\"wyciecie\":" + (m.renderQueue == 2450 ? "true" : "false") + ",\"skala\":[" + F(m.mainTextureScale.x) + "," + F(m.mainTextureScale.y) + "]}";
        })));
        js.Append("],\n\"modele\":[" + string.Join(",\n", Rejestr.Modele.Select(m => { var fl = m.go != null ? m.go.transform.position : m.floor; float yw = m.go != null ? m.go.transform.rotation.eulerAngles.y : m.yaw; return (m, fl, yw); }).Select(q => { var m = q.m; return "{\"id\":" + Js(m.id) + ",\"n\":" + Js(m.nazwa) + ",\"p\":[" + F(q.fl.x) + "," + F(q.fl.y) + "," + F(q.fl.z) + "],\"yaw\":" + F(q.yw) + ",\"s\":" + F(m.scale) + ",\"sink\":" + F(m.sink) + ",\"maxH\":" + F(m.maxH) + "}"; })));
        js.Append("],\n\"swiatla\":[" + string.Join(",", Rejestr.Swiatla.Select(l => "{\"p\":[" + F(l.p.x) + "," + F(l.p.y) + "," + F(l.p.z) + "],\"i\":" + F(l.i) + ",\"r\":" + F(l.r) + "}")) + "]}");
        File.WriteAllText(Path.Combine(outDir, "geom.json"), js.ToString());
        Console.WriteLine("geometria: renderery " + rend.Count + ", partie " + pows.Count + ", wierzchołki " + pos.Count / 3 + ", trójkąty " + idx.Count / 3 + ", modele " + Rejestr.Modele.Count + ", materiały " + mats.Count);
        { int nan = 0; for (int q = 0; q < pos.Count; q++) if (float.IsNaN(pos[q]) || float.IsInfinity(pos[q])) nan++; Console.WriteLine("wierzchołki NaN/∞ (ma być 0): " + nan / 3); }   // 12.11: kadłub łodzi z Pow(cos<0) dawał NaN

        // ---------------- kontrola migotania
        var kub = new Dictionary<(int, int, int, int, int), List<int>>();
        const float cel = 2f;
        for (int i = 0; i < tris.Count; i++)
        {
            var t = tris[i]; int db = (int)Math.Floor(t.d / .02f);
            for (int cu = (int)Math.Floor(t.u0 / cel); cu <= (int)Math.Floor(t.u1 / cel); cu++)
                for (int cv = (int)Math.Floor(t.v0 / cel); cv <= (int)Math.Floor(t.v1 / cel); cv++)
                {
                    var k = (t.os * 2 + t.znak, db, cu, cv, 0);
                    if (!kub.TryGetValue(k, out var l)) kub[k] = l = new List<int>(); l.Add(i);
                }
        }
        var pary = new Dictionary<(int, int), (double pole, float dd, Vector3 gdzie, double maxPole)>();
        var obw = new Dictionary<(int, int), (int os, int znak, float d, Vector2 mn, Vector2 mx)>();
        var widziane = new HashSet<(int, int)>();
        foreach (var kv in kub)
        {
            var (oz, db, cu, cv, _) = kv.Key; var l = kv.Value;
            var kand = new List<int>(l); if (kub.TryGetValue((oz, db + 1, cu, cv, 0), out var w)) kand.AddRange(w);
            for (int i = 0; i < l.Count; i++)
                for (int j = 0; j < kand.Count; j++)
                {
                    int ia = l[i], ib = kand[j]; if (ia == ib) continue;
                    if (j < l.Count && ib < ia) continue;
                    var A = tris[ia]; var B = tris[ib]; if (A.pow == B.pow) continue;
                    float dd = Math.Abs(A.d - B.d); if (dd >= limitCm / 100f) continue;
                    if (A.u1 <= B.u0 || B.u1 <= A.u0 || A.v1 <= B.v0 || B.v1 <= A.v0) continue;
                    var key2 = ia < ib ? (ia, ib) : (ib, ia); if (!widziane.Add(key2)) continue;
                    var wsp = PrzeciecieP(A, B); double pole = wsp.Count < 3 ? 0 : Math.Abs(Pole(wsp)); if (pole < 1e-5) continue;
                    dd = float.MaxValue; foreach (var q in wsp) dd = Math.Min(dd, Math.Abs(Wys(A, q) - Wys(B, q)));
                    if (dd >= limitCm / 100f) continue;
                    var key = A.pow < B.pow ? (A.pow, B.pow) : (B.pow, A.pow);
                    var srodek = (A.a + A.b + A.c) / 3f; var g = A.os == 0 ? new Vector3(A.d, srodek.y, srodek.x) : A.os == 1 ? new Vector3(srodek.x, A.d, srodek.y) : new Vector3(srodek.x, srodek.y, A.d);
                    if (!pary.TryGetValue(key, out var p)) p = (0, dd, g, 0);
                    pary[key] = (p.pole + pole, Math.Min(p.dd, dd), pole > p.maxPole ? g : p.gdzie, Math.Max(p.maxPole, pole));
                    var imn = new Vector2(Math.Max(A.u0, B.u0), Math.Max(A.v0, B.v0)); var imx = new Vector2(Math.Min(A.u1, B.u1), Math.Min(A.v1, B.v1));
                    if (!obw.TryGetValue(key, out var o)) o = (A.os, A.znak, A.d, imn, imx); else o = (o.os, o.znak, o.d, Vector2.Min(o.mn, imn), Vector2.Max(o.mx, imx));
                    obw[key] = o;
                }
        }
        var raport = new StringBuilder();
        // styk krawędzi (16.11): dwie płaszczyzny w jednym licu, które tylko stykają się krawędzią (płyta chodnika i posadzka lokalu
        // w progu) — średnia szerokość wspólnego pola < 1 mm (szum float32 przy |x| ≈ 400 m) — nie migocze (brak wspólnych pikseli)
        var styki = pary.Where(p => { var ob = obw[p.Key]; float dl = Mathf.Max(ob.mx.x - ob.mn.x, ob.mx.y - ob.mn.y); return p.Value.dd < .001f && dl > .05f && p.Value.pole / dl < .001; }).Select(p => p.Key).ToList();
        foreach (var k in styki) pary.Remove(k);
        var lista = pary.OrderByDescending(p => p.Value.pole).ToList();
        int zle = 0, szkloN = 0, tenSam = 0, ruch = 0, portowa = 0;
        // Portowa (16.11): elewacje z generatora pierzei w podglądzie BEZ kroku gry „A: kontrola migotania pierzei i lokali”
        // (BraxMiastoA.KontrolaUlicy wycina starszą powierzchnię każdej pary < 2 cm) — liczone osobno; baner liczony normalnie
        bool Elew(string n) => n.Contains("PORTOWA (podgląd") && !n.Contains("Ulica: baner");
        raport.AppendLine("KONTROLA MIGOTANIA nowej części miasta R1 — pary powierzchni równoległych, odstęp < " + limitCm + " cm, wspólne pole dokładne");
        foreach (var kv in lista)
        {
            var A = pows[kv.Key.Item1]; var B = pows[kv.Key.Item2];
            bool same = A.m == B.m; bool szk = A.m.renderQueue >= 2900 || B.m.renderQueue >= 2900; bool ru = A.ruchomy || B.ruchomy;
            bool elew = Elew(A.nazwa) && Elew(B.nazwa);
            string typ = elew ? "PORTOWA — ELEWACJE (w grze KontrolaUlicy)" : same ? "TEN SAM MATERIAŁ (te same UV — niewidoczne)" : szk ? "SZKŁO" : "MIGOTANIE";
            if (elew) portowa++; else if (same) tenSam++; else if (szk) szkloN++; else zle++;
            if (ru) ruch++;
            var ob = obw[kv.Key]; string osN = (ob.znak == 1 ? "+" : "-") + "XYZ"[ob.os]; string uN = ob.os == 0 ? "z" : "x", vN = ob.os == 1 ? "z" : "y";
            raport.AppendLine($"{typ}{(ru ? " [ruchome]" : "")}: {Krot(A.nazwa)} <{Mat(A.m)}> ↔ {Krot(B.nazwa)} <{Mat(B.m)}> | odstęp {kv.Value.dd * 100f:F1} cm, pole {kv.Value.pole:F4} m² | lico {osN}={ob.d:F3}, {uN} {ob.mn.x:F2}…{ob.mx.x:F2}, {vN} {ob.mn.y:F2}…{ob.mx.y:F2}");
        }
        raport.Insert(0, $"PODSUMOWANIE: pary {lista.Count} — migotanie (różne materiały) {zle}, szkło {szkloN}, ten sam materiał {tenSam}, w tym ruchome {ruch} (styki krawędzi bez wspólnego pola, pominięte: {styki.Count}); Portowa — elewacje pierzei (w grze KontrolaUlicy): {portowa}\n");
        File.WriteAllText(Path.Combine(outDir, "migotanie.txt"), raport.ToString());
        Console.WriteLine(raport.ToString().Split('\n')[0]);
        foreach (var l in UnityEngine.Debug.Log_.Where(x => x.StartsWith("WARN") || x.StartsWith("ERR") || x.Contains("BRAXHORN_WIEZA ") || x.Contains("LADOWISKO") || x.Contains("komis aut"))) Console.WriteLine(l);
        return 0;
    }

    // położenie płaszczyzny trójkąta wzdłuż osi w punkcie (u, v) części wspólnej
    static float Wys(Tr t, Vector2 q)
    {
        float nu = t.os == 0 ? t.n.z : t.n.x, nv = t.os == 1 ? t.n.z : t.n.y, na = t.os == 0 ? t.n.x : t.os == 1 ? t.n.y : t.n.z;
        return (t.pd - nu * q.x - nv * q.y) / na;
    }
    static List<Vector2> PrzeciecieP(Tr A, Tr B)
    {
        var p = new List<Vector2> { A.a, A.b, A.c }; if (Pole(p) < 0) p.Reverse();
        var q = new List<Vector2> { B.a, B.b, B.c }; if (Pole(q) < 0) q.Reverse();
        for (int i = 0; i < 3 && p.Count > 0; i++)
        {
            Vector2 e0 = q[i], e1 = q[(i + 1) % 3]; var wy = new List<Vector2>();
            Func<Vector2, double> S = v => (double)(e1.x - e0.x) * (v.y - e0.y) - (double)(e1.y - e0.y) * (v.x - e0.x);
            for (int k = 0; k < p.Count; k++)
            {
                var cur = p[k]; var prv = p[(k + p.Count - 1) % p.Count]; double sc = S(cur), sp = S(prv);
                if (sc >= 0) { if (sp < 0) wy.Add(Cut(prv, cur, sp, sc)); wy.Add(cur); }
                else if (sp >= 0) wy.Add(Cut(prv, cur, sp, sc));
            }
            p = wy;
        }
        return p;
    }
    static Vector2 Cut(Vector2 a, Vector2 b, double sa, double sb) { float t = (float)(sa / (sa - sb)); return a + (b - a) * t; }
    static double Pole(List<Vector2> p) { double s = 0; for (int i = 0; i < p.Count; i++) { var a = p[i]; var b = p[(i + 1) % p.Count]; s += (double)a.x * b.y - (double)b.x * a.y; } return s * .5; }
    static string Krot(string n) { n = n.Replace("Dworzec: ", ""); return n.Length > 70 ? "…" + n.Substring(n.Length - 69) : n; }
    static string Mat(Material m) { var n = m.name; int i = n.IndexOf(" ["); if (i > 0) n = n.Substring(0, i); i = n.IndexOf(" ("); if (i > 0) n = n.Substring(0, i); return n.Replace("A świeci: ", "").Replace("Dworzec: ", ""); }
    static string F(float v) => float.IsNaN(v) || float.IsInfinity(v) ? "0" : v.ToString("0.#####", CultureInfo.InvariantCulture);
    static string Js(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
