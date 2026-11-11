// HARNESS R1: uruchamia prawdziwy generator dworca (BraxMiastoDworzec.Apply) poza Unity, zapisuje geometrię świata do podglądu
// three.js (geom.bin + geom.json) i robi kontrolę migotania: współpłaszczyznowe trójkąty (oś świata, ten sam zwrot, odstęp < 2 cm)
// z RÓŻNYCH powierzchni (renderer + materiał), wspólne pole liczone dokładnie (przecięcie trójkątów), nie z prostokątów.
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

    sealed class Pow { public Renderer r; public int sub; public Material m; public string nazwa; public bool ruchomy; }
    struct Tr { public int pow, os, znak; public float d; public Vector2 a, b, c; public float u0, u1, v0, v1; public Vector3 n; public float pd; }

    static string Sciezka(Transform t) { var l = new List<string>(); for (var x = t; x != null; x = x.parentT) l.Add(x.name); l.Reverse(); return string.Join(" / ", l.Skip(1)); }
    static bool Ruchomy(Transform t)
    {
        for (var x = t; x != null; x = x.parentT)
            if (x.gameObject.GetComponent<BraxCityDoor>() != null || x.gameObject.GetComponent<BraxWinda>() != null || x.gameObject.GetComponent<BraxDrzwiPrzesuwne>() != null || x.gameObject.GetComponent<BraxZegarDworca>() != null) return true;
        return false;
    }

    public static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        string outDir = args.Length > 0 ? args[0] : "out"; Directory.CreateDirectory(outDir);
        float limitCm = 2f;
        var miasto = new GameObject("BraxhornCity");
        Transform root;
        if (args.Length > 1 && args[1] == "drzwi")
        {
            // podgląd drzwi lokalu premium (BraxDrzwiLokalu) w otworze 2.34 × 3.05 m w ścianie 0.3 m: zamknięte i uchylone 60°
            root = new GameObject("PODGLĄD drzwi lokalu").transform; root.SetParent(miasto.transform, false);
            var sc = BraxUliceKit.Tex("ściana podglądu: kamień", 2.4f, null, new Color(.8f, .75f, .7f), .9f, "sandstone_blocks_08");
            var po = BraxUliceKit.Tex("posadzka podglądu", 2.0f, null, new Color(.6f, .6f, .6f), .9f, "concrete_pavement_02");
            var sw = new UlSiatka(); var pp = new UlSiatka();
            sw.Box(new Vector3(-4f, 0f, -.3f), new Vector3(-1.17f, 4f, 0f)); sw.Box(new Vector3(1.17f, 0f, -.3f), new Vector3(4f, 4f, 0f)); sw.Box(new Vector3(-1.17f, 3.05f, -.3f), new Vector3(1.17f, 4f, 0f));
            pp.Box(new Vector3(-4f, -.2f, -4f), new Vector3(4f, 0f, 4f), 0);
            BraxMiastoA.Obiekt(root, "ściana z otworem", sw, sc); BraxMiastoA.Obiekt(root, "posadzka", pp, po);
            var d1 = BraxDrzwiLokalu.Podwojne(root, "R1_PODGLAD_A", "LOKAL A", new Vector3(0f, 0f, 0f), Vector3.forward, 2.34f, 3.05f, .3f);
            var r2 = new GameObject("drugie drzwi").transform; r2.SetParent(root, false);
            sw = new UlSiatka(); sw.Box(new Vector3(6f - 2.83f, 0f, -.3f), new Vector3(6f - 1.17f, 4f, 0f)); sw.Box(new Vector3(6f + 1.17f, 0f, -.3f), new Vector3(10f, 4f, 0f)); sw.Box(new Vector3(6f - 1.17f, 3.05f, -.3f), new Vector3(6f + 1.17f, 4f, 0f));
            pp = new UlSiatka(); pp.Box(new Vector3(4f, -.2f, -4f), new Vector3(10f, 0f, 4f), 0);
            BraxMiastoA.Obiekt(r2, "ściana 2", sw, sc); BraxMiastoA.Obiekt(r2, "posadzka 2", pp, po);
            var d2 = BraxDrzwiLokalu.Podwojne(r2, "R1_PODGLAD_B", "LOKAL B", new Vector3(6f, 0f, 0f), Vector3.forward, 2.34f, 3.05f, .3f);
            d2.leaves[0].localRotation = Quaternion.Euler(0, 60, 0); d2.leaves[1].localRotation = Quaternion.Euler(0, -60, 0);
            // modele 1 (drewniane) i 2 (stalowe): obok, x 12 i 18; skrzydło lewe uchylone
            foreach (var (xm, model) in new[] { (12f, BraxDrzwiLokalu.Drewniane), (18f, BraxDrzwiLokalu.Stalowe), (24f, BraxDrzwiLokalu.Przesuwne) })
            {
                var r3 = new GameObject("drzwi model " + model).transform; r3.SetParent(root, false);
                sw = new UlSiatka(); sw.Box(new Vector3(xm - 4.83f, 0f, -.3f), new Vector3(xm - 1.17f, 4f, 0f)); sw.Box(new Vector3(xm + 1.17f, 0f, -.3f), new Vector3(xm + 3f, 4f, 0f)); sw.Box(new Vector3(xm - 1.17f, 3.05f, -.3f), new Vector3(xm + 1.17f, 4f, 0f));
                pp = new UlSiatka(); pp.Box(new Vector3(xm - 2f, -.2f, -4f), new Vector3(xm + 3f, 0f, 4f), 0);
                BraxMiastoA.Obiekt(r3, "ściana m" + model, sw, sc); BraxMiastoA.Obiekt(r3, "posadzka m" + model, pp, po);
                var d3 = BraxDrzwiLokalu.Podwojne(r3, "R1_PODGLAD_M" + model, "MODEL " + model, new Vector3(xm, 0f, 0f), Vector3.forward, 2.34f, 3.05f, .3f, 0f, model);
                if (d3 != null) d3.leaves[0].localRotation = Quaternion.Euler(0, 35, 0);
                else foreach (var dp in r3.GetComponentsInChildren<BraxDrzwiPrzesuwne>()) { dp.skrzydla[0].localPosition += dp.przesuniecie[0] * .55f; dp.skrzydla[1].localPosition += dp.przesuniecie[1] * .55f; }
            }
        }
        else if (args.Length > 1 && args[1] == "chlodnie")
        {
            // podgląd chłodni z kodu (BraxChlodnie): lodówki z napojami 1/2/3 drzwi, szafa z mięsem 2 drzwi (mięso z kodu — bez paczki),
            // lada chłodnicza 2.4 m; ściana tylna z = 0 (lico do +z), posadzka
            root = new GameObject("PODGLĄD chłodni").transform; root.SetParent(miasto.transform, false);
            var sc = BraxUliceKit.Tex("ściana podglądu: płytki", 1.2f, null, new Color(.9f, .9f, .88f), .4f, "white_tiles_02", "tiles_white");
            var po = BraxUliceKit.Tex("posadzka podglądu", 2.0f, null, new Color(.6f, .6f, .6f), .9f, "concrete_pavement_02");
            var sw = new UlSiatka(); var pp = new UlSiatka();
            sw.Box(new Vector3(-6f, 0f, -.3f), new Vector3(10f, 3.6f, 0f)); pp.Box(new Vector3(-6f, -.2f, -.3f), new Vector3(20f, 0f, 8f), 0);
            BraxMiastoA.Obiekt(root, "ściana", sw, sc); BraxMiastoA.Obiekt(root, "posadzka", pp, po);
            BraxChlodnie.SzafaMiesna(root, "szafa z mięsem", new Vector3(-3.6f, 0f, .01f), Vector3.forward, 2, null, new System.Random(3));
            BraxChlodnie.LodowkaNapojow(root, "lodówka 1", new Vector3(-1.2f, 0f, .01f), Vector3.forward, 1, 0);
            BraxChlodnie.LodowkaNapojow(root, "lodówka 2", new Vector3(.9f, 0f, .01f), Vector3.forward, 2, 1);
            BraxChlodnie.LodowkaNapojow(root, "lodówka 3", new Vector3(4.6f, 0f, .01f), Vector3.forward, 3, 2);
            BraxChlodnie.LadaChlodnicza(root, "lada chłodnicza", new Vector3(-2.4f, 0f, 4.4f), Vector3.forward, 2.4f, null, new System.Random(5));
            // ściana oprawek optyka (BraxUlicePierzeje.Okulary): dwie półki po 8 par, x 7.4…9.4, przodem do +z
            var pol = new UlSiatka(); foreach (float y in new[] { 1.30f, 1.70f }) pol.Box(new Vector3(7.2f, y - .025f, .0f), new Vector3(9.6f, y, .45f));
            BraxMiastoA.Obiekt(root, "półki optyka", pol, BraxUliceKit.Mat("półka (biała)", new Color(.95f, .95f, .94f), .5f));
            // dolna półka: korekcyjne (modele 0–3), górna: przeciwsłoneczne (4–8); kolory losowane jak w grze
            var rq = new System.Random(11); int q = 0;
            foreach (float y in new[] { 1.30f, 1.70f })
                for (float x = 7.45f; x < 9.5f; x += .27f * BraxClaudeOsiedle.K, q++)
                {
                    BraxUlicePierzeje.LosujOkulary(rq, y > 1.5f, out int mo, out var ko, out int pz);
                    mo = y > 1.5f ? 4 + q % 5 : q % 4;
                    BraxUlicePierzeje.Okulary(new Vector3(x, y + .0005f, .40f), Vector3.forward, ko, BraxClaudeOsiedle.K, mo, pz);
                }
            BraxUliceKit.Zapisz(root, "okulary");
            // wulkanizacja z kodu: montażownica i wyważarka (przodem do +z), stos kół i 3 koła pionowo (wzory × kolory)
            BraxWarsztatKod.Montazownica(root, "podgląd", new Vector3(11.5f, 0f, 1.2f), Vector3.forward, 0);
            BraxWarsztatKod.Wywazarka(root, "podgląd", new Vector3(13.6f, 0f, 1.2f), Vector3.forward, 4);
            BraxWarsztatKod.Zaba(root, "podgląd", new Vector3(14.6f, 0f, 3.0f), Vector3.right);
            for (int i = 0; i < 4; i++) BraxWarsztatKod.Kolo(root, "stos", new Vector3(15.6f, (BraxWarsztatKod.Sz * .5f + i * (BraxWarsztatKod.Sz + .004f)) * BraxClaudeOsiedle.K, 1.2f), Vector3.up, i < 2 ? 3 : 7, i * 40f);
            for (int i = 0; i < 3; i++) BraxWarsztatKod.Kolo(root, "pion", new Vector3(16.8f + i * .92f, BraxWarsztatKod.R * BraxClaudeOsiedle.K, 1.2f), Vector3.forward, i * 3 + i, 0f);
            // muzeum (sala z kodu jak w KINNAIRD, wymiary prawdziwe: zabytek 55.67 × 41.18 m → sala 52.7 × 38.1 × 4.3 m): fasada z otworem
            // 3.1 × 3.8 m w z −6.4…−8, sala x 9.7…62.3, z −46.1…−8
            {
                var fas = new UlSiatka(); float bx = 36f, zf = -6.4f, gm = 1.6f, dw = 3.1f, dh = 3.8f, gf = .7f, xs0 = bx - 27.8f, xs1 = bx + 27.8f;
                fas.Box(new Vector3(xs0, .001f, zf - gf), new Vector3(bx - dw * .5f - .3f, 9f, zf)); fas.Box(new Vector3(bx + dw * .5f + .3f, .001f, zf - gf), new Vector3(xs1, 9f, zf)); fas.Box(new Vector3(bx - dw * .5f - .3f, dh + .45f, zf - gf), new Vector3(bx + dw * .5f + .3f, 9f, zf));
                BraxMiastoA.Obiekt(root, "fasada zabytku (podgląd)", fas, BraxUliceKit.Tex("fasada podglądu", 2f, null, new Color(.75f, .7f, .62f), .8f, "sandstone_blocks_08"));
                var pm = new UlSiatka(); pm.Box(new Vector3(xs0 - 2f, -.2f, -48f), new Vector3(xs1 + 2f, 0f, -.3f), 0); BraxMiastoA.Obiekt(root, "posadzka muzeum (podgląd)", pm, BraxUliceKit.Tex("posadzka podglądu", 2.0f, null, new Color(.6f, .6f, .6f), .9f, "concrete_pavement_02"));
                var mg = new GameObject("MUZEUM (podgląd)").transform; mg.SetParent(root, false);
                BraxRegionMiastoB.PodgladSali(mg, bx, 0f, bx - 26.335f, bx + 26.335f, zf - gm - 38.08f, zf - gm, 4.3f, zf, dw, dh, gm);
            }
            Console.WriteLine("CHŁODNIE: siatek " + BraxChlodnie.Siatek + ", kopii " + BraxChlodnie.Kopii);
        }
        else
        {
            BraxMiastoDworzec.Apply(miasto.transform);
            foreach (var l in UnityEngine.Debug.Log_) if (l.StartsWith("LOG BRAXHORN_DWORZEC_R1")) File.WriteAllText(Path.Combine(outDir, "opis.txt"), l.Replace(" | ", "\n"));
            root = BraxMiastoDworzec.root;
        }
        if (args.Length > 1 && args[1] == "otwarte")
        {
            // podgląd: drzwi windy przy peronie 1 (przystanek 0 i kabina) otwarte na 90 %, drzwi WC od hali i pierwsze kabiny uchylone
            foreach (var w in root.GetComponentsInChildren<BraxWinda>())
            {
                if (!w.nazwa.Contains("PERON 1")) continue;
                float d = w.przesuw * .9f;
                w.przystanekL[0].localPosition += new Vector3(-d, 0, 0); w.przystanekP[0].localPosition += new Vector3(d, 0, 0);
                w.kabinaL.localPosition += new Vector3(-d, 0, 0); w.kabinaP.localPosition += new Vector3(d, 0, 0);
            }
            foreach (var dr in root.GetComponentsInChildren<BraxCityDoor>())
                if (dr.id.EndsWith("WC_PANIE") || dr.id.EndsWith("WC_PANOWIE") || dr.id.Contains("kabina_1")) dr.leaves[0].localRotation = Quaternion.Euler(0, 80, 0);
        }

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
            return "{\"n\":" + Js(m.name) + ",\"rodzaj\":\"" + m.rodzaj + "\",\"slug\":" + Js(m.slugi.FirstOrDefault() ?? "") + ",\"metry\":" + F(m.metry) + ",\"kolor\":[" + F(c.r) + "," + F(c.g) + "," + F(c.b) + "," + F(c.a) + "],\"rough\":" + F(m.GetFloat("_Roughness")) + ",\"metal\":" + F(m.GetFloat("_Metallic")) + ",\"emisja\":[" + F(e.r) + "," + F(e.g) + "," + F(e.b) + "],\"plik\":" + Js(m.mainTexture != null ? m.mainTexture.name ?? "" : "") + "}";
        })));
        js.Append("],\n\"modele\":[" + string.Join(",\n", Rejestr.Modele.Select(m => "{\"id\":" + Js(m.id) + ",\"n\":" + Js(m.nazwa) + ",\"p\":[" + F(m.floor.x) + "," + F(m.floor.y) + "," + F(m.floor.z) + "],\"yaw\":" + F(m.yaw) + ",\"s\":" + F(m.scale) + ",\"sink\":" + F(m.sink) + ",\"maxH\":" + F(m.maxH) + "}")));
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
        var lista = pary.OrderByDescending(p => p.Value.pole).ToList();
        int zle = 0, szkloN = 0, tenSam = 0, ruch = 0;
        raport.AppendLine("KONTROLA MIGOTANIA dworca R1 — pary powierzchni równoległych, odstęp < " + limitCm + " cm, wspólne pole dokładne");
        foreach (var kv in lista)
        {
            var A = pows[kv.Key.Item1]; var B = pows[kv.Key.Item2];
            bool same = A.m == B.m; bool szk = A.m.renderQueue >= 2900 || B.m.renderQueue >= 2900; bool ru = A.ruchomy || B.ruchomy;
            string typ = same ? "TEN SAM MATERIAŁ (te same UV — niewidoczne)" : szk ? "SZKŁO" : "MIGOTANIE";
            if (same) tenSam++; else if (szk) szkloN++; else zle++;
            if (ru) ruch++;
            var ob = obw[kv.Key]; string osN = (ob.znak == 1 ? "+" : "-") + "XYZ"[ob.os]; string uN = ob.os == 0 ? "z" : "x", vN = ob.os == 1 ? "z" : "y";
            raport.AppendLine($"{typ}{(ru ? " [ruchome]" : "")}: {Krot(A.nazwa)} <{Mat(A.m)}> ↔ {Krot(B.nazwa)} <{Mat(B.m)}> | odstęp {kv.Value.dd * 100f:F1} cm, pole {kv.Value.pole:F4} m² | lico {osN}={ob.d:F3}, {uN} {ob.mn.x:F2}…{ob.mx.x:F2}, {vN} {ob.mn.y:F2}…{ob.mx.y:F2}");
        }
        raport.Insert(0, $"PODSUMOWANIE: pary {lista.Count} — migotanie (różne materiały) {zle}, szkło {szkloN}, ten sam materiał {tenSam}, w tym ruchome {ruch}\n");
        File.WriteAllText(Path.Combine(outDir, "migotanie.txt"), raport.ToString());
        Console.WriteLine(raport.ToString().Split('\n')[0]);
        foreach (var l in UnityEngine.Debug.Log_.Where(x => x.StartsWith("WARN") || x.StartsWith("ERR"))) Console.WriteLine(l);
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
