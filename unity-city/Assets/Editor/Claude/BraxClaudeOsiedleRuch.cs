using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// RUCH NA OSIEDLU (właściciel 1.11, 16:57–17:04: „ludzi wstaw, żeby chodzili po osiedlu… samochody, żeby sobie pojeździły —
// tylko żeby się ruch toczył; ludzie mają tylko żyć”). Na razie na nowych ulicach części A (DrogiA):
//   • przechodnie: postacie Mixamo z wersji przeglądarkowej (public/assets/people → dekoduj_glb.mjs → Source/Claude/Ludzie),
//     klipy Walking / Idle Braxa (ten sam szkielet Mixamo) przepięte na kości postaci — tylko obroty (różne skale modeli);
//   • auta w ruchu: 4 pętle prawym pasem (skrzyżowania łukami, rondo przeciwnie do wskazówek zegara) + auta zaparkowane.
//     Modele: public/assets/cars-src (licencje w plikach; NC — „DO WYMIANY PRZED SPRZEDAŻĄ”, decyzja właściciela 17:03).
public static partial class BraxClaudeOsiedle
{
    const string LudzieDir = "Assets/Source/Claude/Ludzie", AutaDir = "Assets/Source/Claude/Auta";

    static void RuchNaOsiedlu()
    {
        group = "Ruch: ludzie i auta";
        Przechodnie();
        Auta();
    }

    // ---------------- przechodnie ----------------
    static string NormBone(string n) => n.Replace(":", "").Replace("mixamorig", "").Replace("_", "").ToLowerInvariant();

    static AnimationClip ClipNaSzkielet(AnimationClip src, Transform root, string name, string key)
    {
        if (!src) return null;
        string path = Out + "/ruch_" + Safe(key) + "_" + Safe(name) + ".anim";
        var have = AssetDatabase.LoadAssetAtPath<AnimationClip>(path); if (have) return have;
        var bones = new Dictionary<string, Transform>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) { var k = NormBone(t.name); if (k.Length > 0 && !bones.ContainsKey(k)) bones[k] = t; }
        var dst = new AnimationClip { legacy = true, name = name, wrapMode = WrapMode.Loop, frameRate = src.frameRate };
        int n = 0;
        foreach (var b in AnimationUtility.GetCurveBindings(src))
        {
            if (b.type != typeof(Transform) || !b.propertyName.StartsWith("m_LocalRotation")) continue;   // tylko obroty
            string bone = b.path.Contains("/") ? b.path.Substring(b.path.LastIndexOf('/') + 1) : b.path;
            if (!bones.TryGetValue(NormBone(bone), out var t)) continue;
            var curve = AnimationUtility.GetEditorCurve(src, b); if (curve == null) continue;
            AnimationUtility.SetEditorCurve(dst, EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(t, root), typeof(Transform), b.propertyName), curve);
            n++;
        }
        if (n == 0) { rep.notes.Add("DO POPRAWKI: klip " + name + " nie pasuje do szkieletu " + key); return null; }
        dst.EnsureQuaternionContinuity();
        AssetDatabase.CreateAsset(dst, path);
        return dst;
    }

    // Build 14: przechodnie z paczki Rocketbox powstają na końcu budowy (LudzieNaOsiedlu, BraxClaudeOsiedleLudzieMiejsca);
    // te z build 13 (Mixamo z wersji przeglądarkowej) tylko, gdy paczki nie pobrano (fetch_rocketbox.py).
    static void Przechodnie() { if (!RocketboxDostepny()) PrzechodnieMixamo(); }

    static void PrzechodnieMixamo()
    {
        var walkSrc = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/PlayableGenerated/Walk.anim");   // 1.11 build 12: było „Walking.anim” (nie istnieje) → ludzie bez ruchu
        var idleSrc = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/PlayableGenerated/Idle.anim");
        if (!walkSrc || !idleSrc) { rep.notes.Add("DO POPRAWKI: brak klipów Braxa Walking/Idle — przechodnie bez ruchu"); }
        if (!AssetDatabase.IsValidFolder(LudzieDir)) { rep.notes.Add("DO POPRAWKI: brak " + LudzieDir + " (dekoduj_glb.mjs)"); return; }
        // lżejsze postacie najpierw (tłum); wszystkie mają szkielet Mixamo
        string[] kolej = { "Arissa", "Big_Vegas", "Lola_B_Styperek", "The_Boss", "Kachujin_G_Rosales", "Ch06_nonPBR", "Ch12_nonPBR", "Ch18_nonPBR", "Ch22_nonPBR", "Ch26_nonPBR", "Ch37_nonPBR", "Ch47_nonPBR", "Remy", "Brute" };
        var modele = new List<GameObject>();
        foreach (var id in kolej)
        {
            string p = LudzieDir + "/" + id + ".glb"; if (!File.Exists(p)) continue;
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p) ?? AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
            if (go && go.GetComponentInChildren<SkinnedMeshRenderer>(true)) modele.Add(go);
        }
        if (modele.Count == 0 || chodnikiA.Count == 0) { rep.notes.Add("DO POPRAWKI: przechodnie — modeli " + modele.Count + ", chodników " + chodnikiA.Count); return; }
        var rnd = new System.Random(1101);
        // część A (nowe chodniki) + część B: chodniki ulicy głównej, ulicy N-S i aleja parku
        var odcinki = new List<(Vector3 a, Vector3 b)>(chodnikiA.OrderBy(_ => rnd.Next()).Take(14));
        float wB = Carriage + Kerb + Green + Walk * .5f;
        foreach (int sd in new[] { -1, 1 })
        {
            odcinki.Add((new Vector3(X0 + 8f, .14f, StreetZ + sd * wB), new Vector3(AxisX - 10f, .14f, StreetZ + sd * wB)));
            odcinki.Add((new Vector3(AxisX + 10f, .14f, StreetZ + sd * wB), new Vector3(X1 - 8f, .14f, StreetZ + sd * wB)));
            odcinki.Add((new Vector3(AxisX + sd * wB, .14f, ParkZ0 - 2f), new Vector3(AxisX + sd * wB, .14f, StreetZ + Half + 2f)));
        }
        odcinki.Add((new Vector3(AxisX + 1f, .05f, FenceA - 3f), new Vector3(AxisX + 1f, .05f, -127f)));   // aleja parku do fontanny
        odcinki.Add((new Vector3(AxisX - 1f, .05f, -137f), new Vector3(AxisX - 1f, .05f, ParkZ0 + 2f)));
        int ile = odcinki.Count, zrobione = 0;
        var klipy = new Dictionary<GameObject, (AnimationClip w, AnimationClip i)>();
        for (int k = 0; k < ile; k++)
        {
            var src = modele[k % modele.Count];
            var o = (GameObject)UnityEngine.Object.Instantiate(src, Group(group), false); o.name = "Przechodzień (" + src.name + ")";
            o.transform.position = Vector3.zero; o.transform.rotation = Quaternion.identity;
            var rs = o.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) { UnityEngine.Object.DestroyImmediate(o); continue; }
            var b = RB(o);
            float h = Mathf.Lerp(1.68f, 1.86f, (float)rnd.NextDouble());
            if (b.size.y > .3f) o.transform.localScale *= h / b.size.y;
            b = RB(o); o.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            foreach (var smr in o.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { smr.updateWhenOffscreen = false; smr.shadowCastingMode = ShadowCastingMode.On; smr.quality = SkinQuality.Bone2; }
            // klipy: przepięte raz na model (ścieżki względem korzenia instancji = korzenia prefabu)
            if (!klipy.TryGetValue(src, out var kl)) { kl = (ClipNaSzkielet(walkSrc, o.transform, "Walking", src.name), ClipNaSzkielet(idleSrc, o.transform, "Idle", src.name)); klipy[src] = kl; }
            // korzeń ruchu: pusty obiekt (model jako dziecko — skala modelu nie wpływa na ruch)
            var root = new GameObject("Przechodzień " + (k + 1) + " · " + src.name); root.transform.SetParent(Group(group), false);
            Vector3 a = odcinki[k].a, bb = odcinki[k].b;
            root.transform.position = a; o.transform.SetParent(root.transform, false); o.transform.localPosition = new Vector3(o.transform.localPosition.x, o.transform.localPosition.y, o.transform.localPosition.z);
            var anim = o.AddComponent<Animation>(); anim.playAutomatically = true; anim.cullingType = AnimationCullingType.BasedOnRenderers;
            if (kl.w) { anim.AddClip(kl.w, "Walking"); anim.clip = kl.w; }
            if (kl.i) anim.AddClip(kl.i, "Idle");
            var col = root.AddComponent<CapsuleCollider>(); col.height = 1.75f; col.radius = .28f; col.center = new Vector3(0, .9f, 0);
            var rb = root.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
            var p = root.AddComponent<BraxPieszy>(); p.a = a; p.b = bb; p.anim = anim; p.predkosc = 1.1f + (float)rnd.NextDouble() * .35f;
            if (!kl.w) p.enabled = false;
            zrobione++;
        }
        rep.notes.Add("przechodnie: " + zrobione + " postaci na chodnikach części A (" + modele.Count + " modeli Mixamo)");
    }

    // ---------------- auta ----------------
    static readonly string[] AutaNC = { "car_918", "car_dc", "car_dia", "car_mar", "car_mus", "car_r8", "car_s4" };   // DO WYMIANY PRZED SPRZEDAŻĄ (CC-BY-NC-SA)

    // Model auta: długość wzdłuż +z (przód wg nazw części, domyślnie +z), skala realna (3.5–5.5 m), spód na y = 0.
    static GameObject AutoModel(GameObject src, out Transform[] kola)
    {
        kola = new Transform[0];
        var o = (GameObject)UnityEngine.Object.Instantiate(src); o.name = src.name;
        o.transform.position = Vector3.zero;
        if (o.GetComponentsInChildren<Renderer>(true).Length == 0) { UnityEngine.Object.DestroyImmediate(o); return null; }
        var b = RB(o);
        if (b.size.x > b.size.z * 1.15f) { o.transform.rotation = Quaternion.Euler(0, 90f, 0) * o.transform.rotation; b = RB(o); }
        float len = b.size.z;
        if (len > 7f || len < 2.5f) { o.transform.localScale *= 4.5f / Mathf.Max(.01f, len); b = RB(o); }
        // przód: części z „front/head/grill” vs „rear/back/tail”
        float fz = 0f, rz = 0f; int nf = 0, nr = 0;
        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
        {
            var nm = (r.name + " " + string.Join(" ", r.sharedMaterials.Where(m => m).Select(m => m.name))).ToLowerInvariant();
            if (nm.Contains("front") || nm.Contains("head") || nm.Contains("grill") || nm.Contains("przod")) { fz += r.bounds.center.z - b.center.z; nf++; }
            if (nm.Contains("rear") || nm.Contains("back") || nm.Contains("tail") || nm.Contains("brake") || nm.Contains("tyl")) { rz += r.bounds.center.z - b.center.z; nr++; }
        }
        if ((nf > 0 && fz / nf < -.2f) || (nf == 0 && nr > 0 && rz / nr > .2f)) { o.transform.rotation = Quaternion.Euler(0, 180f, 0) * o.transform.rotation; b = RB(o); }
        o.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
        foreach (var r in o.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
        // koła: węzły „wheel/koło/tire/rim” z osią blisko środka siatki (inaczej obrót byłby wokół złego punktu)
        var k = new List<Transform>();
        foreach (var t in o.GetComponentsInChildren<Transform>(true))
        {
            var nm = t.name.ToLowerInvariant(); if (!(nm.Contains("wheel") || nm.Contains("kolo") || nm.Contains("tire") || nm.Contains("tyre"))) continue;
            var r = t.GetComponent<Renderer>(); if (!r) continue;
            if ((r.bounds.center - t.position).magnitude < .08f && r.bounds.size.y > .3f && r.bounds.size.y < 1.2f && !k.Any(q => t.IsChildOf(q))) k.Add(t);
        }
        kola = k.Count >= 4 && k.Count <= 6 ? k.ToArray() : new Transform[0];
        return o;
    }

    static List<GameObject> AutaModele()
    {
        var l = new List<GameObject>();
        if (!AssetDatabase.IsValidFolder(AutaDir)) { rep.notes.Add("DO POPRAWKI: brak " + AutaDir); return l; }
        foreach (var id in new[] { "car_zuk", "car_rev", "car_s4", "car_mus", "car_r8", "car_918", "car_dc", "car_mar", "car_dia" })
        {
            string p = AutaDir + "/" + id + ".glb"; if (!File.Exists(p)) continue;
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p) ?? AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
            if (go) l.Add(go);
        }
        return l;
    }

    // Trasa pętli po osiach ulic (węzły) → prawy pas, łuki na skrzyżowaniach, rondo przeciwnie do wskazówek zegara.
    static Vector3[] TrasaPetli(Vector2[] wezlyTrasy, float y)
    {
        var pts = new List<Vector3>();
        int n = wezlyTrasy.Length;
        float Pas(Vector2 a, Vector2 b)
        {
            var mid = (a + b) * .5f;
            var u = ulice.FirstOrDefault(q => q.alongX ? Mathf.Abs(a.y - b.y) < .1f && Mathf.Abs(q.c - a.y) < .1f : Mathf.Abs(a.x - b.x) < .1f && Mathf.Abs(q.c - a.x) < .1f);
            return u != null ? u.cw * .5f : 1.6f;
        }
        Vector3 V(Vector2 p) => new Vector3(p.x, y, p.y);
        for (int i = 0; i < n; i++)
        {
            Vector2 prev = wezlyTrasy[(i + n - 1) % n], cur = wezlyTrasy[i], next = wezlyTrasy[(i + 1) % n];
            Vector2 din = (cur - prev).normalized, dout = (next - cur).normalized;
            Vector2 rin = new Vector2(din.y, -din.x), rout = new Vector2(dout.y, -dout.x);   // prawo względem kierunku (Unity: x prawo, z przód)
            float lin = Pas(prev, cur), lout = Pas(cur, next);
            bool rondo = Mathf.Abs(cur.x) < .1f && Mathf.Abs(cur.y - RondoZ) < .1f;
            if (rondo)
            {
                const float R = 9.1f;
                float aIn = Mathf.Atan2(-din.y, -din.x) * Mathf.Rad2Deg + 22f, aOut = Mathf.Atan2(dout.y, dout.x) * Mathf.Rad2Deg - 22f;
                while (aOut <= aIn + 1f) aOut += 360f;
                pts.Add(V(cur - din * (RondoChodnik + 2f) + rin * lin));
                for (float a = aIn; a <= aOut; a += 12f) pts.Add(new Vector3(cur.x + Mathf.Cos(a * Mathf.Deg2Rad) * R, y, cur.y + Mathf.Sin(a * Mathf.Deg2Rad) * R));
                pts.Add(new Vector3(cur.x + Mathf.Cos(aOut * Mathf.Deg2Rad) * R, y, cur.y + Mathf.Sin(aOut * Mathf.Deg2Rad) * R));
                pts.Add(V(cur + dout * (RondoChodnik + 2f) + rout * lout));
                continue;
            }
            float cl = 7.5f;
            Vector2 p0 = cur - din * cl + rin * lin, p2 = cur + dout * cl + rout * lout;
            if (Vector2.Dot(din, dout) > .99f) { pts.Add(V(cur + rin * lin)); continue; }   // prosto przez skrzyżowanie
            // punkt kontrolny: przecięcie prostych pasa wejścia i wyjścia
            Vector2 c = cur + rin * lin + rout * lout;
            for (int k = 0; k <= 8; k++)
            {
                float t = k / 8f; var q = (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * c + t * t * p2; pts.Add(V(q));
            }
        }
        // zagęszczenie długich prostych (płynny obrót i hamowanie)
        var dense = new List<Vector3>();
        for (int i = 0; i < pts.Count; i++)
        {
            var a = pts[i]; var b = pts[(i + 1) % pts.Count]; dense.Add(a);
            float d = Vector3.Distance(a, b); int m = Mathf.FloorToInt(d / 6f);
            for (int k = 1; k < m; k++) dense.Add(Vector3.Lerp(a, b, k / (float)m));
        }
        return dense.ToArray();
    }

    static void Auta()
    {
        var modele = AutaModele(); if (modele.Count == 0) return;
        const float y = YAsf + .01f;
        var petle = new[]
        {
            new[] { new Vector2(-58, -19), new Vector2(58, -19), new Vector2(58, RondoZ), new Vector2(0, RondoZ), new Vector2(-58, RondoZ) },
            new[] { new Vector2(-58, RondoZ), new Vector2(0, RondoZ), new Vector2(58, RondoZ), new Vector2(58, -19), new Vector2(-58, -19) },
            new[] { new Vector2(0, -19), new Vector2(0, RondoZ), new Vector2(0, 86), new Vector2(58, 86), new Vector2(58, -19) },
            new[] { new Vector2(0, -19), new Vector2(-58, -19), new Vector2(-58, 86), new Vector2(0, 86), new Vector2(0, RondoZ) },
        };
        var rnd = new System.Random(2026); int auta = 0, parked = 0, mi = 0;
        foreach (var petla in petle)
        {
            var trasa = TrasaPetli(petla, y);
            float dl = 0f; for (int i = 0; i < trasa.Length; i++) dl += Vector3.Distance(trasa[i], trasa[(i + 1) % trasa.Length]);
            for (int k = 0; k < 3; k++)
            {
                var src = modele[mi++ % modele.Count];
                var m = AutoModel(src, out var kola); if (!m) continue;
                var root = new GameObject("Auto w ruchu · " + src.name + (AutaNC.Contains(src.name) ? " (DO WYMIANY PRZED SPRZEDAŻĄ)" : "")); root.transform.SetParent(Group(group), false);
                m.transform.SetParent(root.transform, false);
                var b = RB(m);
                var bc = root.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = b.size;
                var rb = root.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
                var lod = root.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.012f, m.GetComponentsInChildren<Renderer>(true)) }); lod.RecalculateBounds();
                var a = root.AddComponent<BraxAuto>(); a.trasa = trasa; a.start = dl * (k / 3f) + (float)rnd.NextDouble() * 8f; a.kola = kola;
                a.vmax = 7.5f + (float)rnd.NextDouble() * 2f;
                root.transform.position = trasa[0];
                auta++;
            }
        }
        // zaparkowane: co trzecie miejsce na parkingach (losowo), przodem do alei
        foreach (var mp in miejscaA)
        {
            Vector3 p = mp.p; float yaw = mp.yaw;
            if (rnd.NextDouble() > .33) continue;
            var src = modele[rnd.Next(modele.Count)];
            var m = AutoModel(src, out _); if (!m) continue;
            m.transform.SetParent(Group(group), true);
            m.transform.rotation = Quaternion.Euler(0, yaw, 0) * m.transform.rotation;
            var b = RB(m); m.transform.position += new Vector3(p.x - b.center.x, p.y - b.min.y, p.z - b.center.z);
            m.name = "Auto zaparkowane · " + src.name + (AutaNC.Contains(src.name) ? " (DO WYMIANY PRZED SPRZEDAŻĄ)" : "");
            b = RB(m); Collider(b.center, b.size);
            var lod = m.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.012f, m.GetComponentsInChildren<Renderer>(true)) }); lod.RecalculateBounds();
            parked++;
        }
        rep.notes.Add("auta: " + auta + " w ruchu na 4 pętlach (rondo), " + parked + " zaparkowanych; modeli " + modele.Count);
    }
}
