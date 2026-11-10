using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// MIASTO R1 10.11 — stanowisko sternika jachtu (właściciel 17:37: „jacht — ze środka kamera, widok zza steru”).
// Model yacht.glb (Sketchfab, jedna siatka) NIE MA koła ani konsoli: flybridge to pusty pokład pod hardtopem z szybą z przodu
// (sprawdzone 10.11: sondy w osi kadłuba). Dotychczas „ster” był zgadywany 0,8 m przed punktem na rufie, a kamera stała tam,
// gdzie steru nie było. Tu: konsola z ekranami, koło sterowe z kodu (kręci się ze sterem, dłonie Braxa na wieńcu), fotel
// sternika — tuż za przednią szybą flybridge'u, wymiary realne × K. Kamera: oczy siedzącego Braxa, patrzy przed dziób.
// Wszystko w układzie korzenia łodzi (płynie razem z jachtem). Bez kolizji (gracz na łodzi jest jej dzieckiem).
public static class BraxNabrzezeSter
{
    static float K => BraxNabrzeze.K;
    const string Out = "Assets/PlayableGenerated/ClaudeRegion/Nabrzeze/Ster/";
    static readonly Dictionary<string, Mesh> siatki = new Dictionary<string, Mesh>();
    static Material lakier, ekran, chrom, wieniec, skora;

    sealed class S
    {
        public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int> t = new List<int>();
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var nn = Vector3.Cross(b - a, c - a).normalized; int s = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d); for (int i = 0; i < 4; i++) n.Add(nn);
            float w = Vector3.Distance(a, b), h = Vector3.Distance(b, c);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(w, 0)); uv.Add(new Vector2(w, h)); uv.Add(new Vector2(0, h));
            t.Add(s); t.Add(s + 1); t.Add(s + 2); t.Add(s); t.Add(s + 2); t.Add(s + 3);
        }
        /// <summary>Prostopadłościan: środek, wymiary, obrót.</summary>
        public void Pudlo(Vector3 c, Vector3 r, Quaternion q)
        {
            Vector3 P(float x, float y, float z) => c + q * new Vector3(x * r.x * .5f, y * r.y * .5f, z * r.z * .5f);
            Quad(P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1));
            Quad(P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1));
            Quad(P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1));
            Quad(P(-1, -1, 1), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1));
            Quad(P(1, -1, 1), P(1, -1, -1), P(1, 1, -1), P(1, 1, 1));
            Quad(P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1));
        }
        /// <summary>Graniastosłup: wielokąt wypukły w płaszczyźnie (z, y), wyciągnięty wzdłuż x od x0 do x1.</summary>
        public void Profil(IList<Vector2> zy, float x0, float x1)
        {
            int m = zy.Count;
            for (int i = 0; i < m; i++)
            {
                var a = zy[i]; var b = zy[(i + 1) % m];
                Quad(new Vector3(x0, a.y, a.x), new Vector3(x1, a.y, a.x), new Vector3(x1, b.y, b.x), new Vector3(x0, b.y, b.x));
            }
            int s0 = v.Count; var n0 = Vector3.left;
            foreach (var p in zy) { v.Add(new Vector3(x0, p.y, p.x)); n.Add(n0); uv.Add(new Vector2(p.x, p.y)); }
            for (int i = 1; i + 1 < m; i++) { t.Add(s0); t.Add(s0 + i); t.Add(s0 + i + 1); }
            int s1 = v.Count;
            foreach (var p in zy) { v.Add(new Vector3(x1, p.y, p.x)); n.Add(Vector3.right); uv.Add(new Vector2(p.x, p.y)); }
            for (int i = 1; i + 1 < m; i++) { t.Add(s1); t.Add(s1 + i + 1); t.Add(s1 + i); }
        }
        public void Rura(Vector3 a, Vector3 b, float r, int boki = 14)
        {
            var os = b - a; if (os.sqrMagnitude < 1e-8f) return; os.Normalize();
            var u = Vector3.Cross(os, Mathf.Abs(os.y) > .95f ? Vector3.forward : Vector3.up).normalized; var w = Vector3.Cross(os, u);
            for (int i = 0; i < boki; i++)
            {
                float t0 = 2 * Mathf.PI * i / boki, t1 = 2 * Mathf.PI * (i + 1) / boki;
                Vector3 n0 = u * Mathf.Cos(t0) + w * Mathf.Sin(t0), n1 = u * Mathf.Cos(t1) + w * Mathf.Sin(t1);
                int s = v.Count;
                // kolejność a0, a1, b1, b0: normalna trójkąta na zewnątrz rury (widoczna z zewnątrz)
                v.Add(a + n0 * r); v.Add(a + n1 * r); v.Add(b + n1 * r); v.Add(b + n0 * r);
                n.Add(n0); n.Add(n1); n.Add(n1); n.Add(n0);
                uv.Add(new Vector2(t0 * r, 0)); uv.Add(new Vector2(t1 * r, 0)); uv.Add(new Vector2(t1 * r, 1)); uv.Add(new Vector2(t0 * r, 1));
                t.Add(s); t.Add(s + 1); t.Add(s + 2); t.Add(s); t.Add(s + 2); t.Add(s + 3);
                int c0 = v.Count; v.Add(a); v.Add(a + n1 * r); v.Add(a + n0 * r); for (int k = 0; k < 3; k++) { n.Add(-os); uv.Add(Vector2.zero); }
                t.Add(c0); t.Add(c0 + 1); t.Add(c0 + 2);
                int c1 = v.Count; v.Add(b); v.Add(b + n0 * r); v.Add(b + n1 * r); for (int k = 0; k < 3; k++) { n.Add(os); uv.Add(Vector2.zero); }
                t.Add(c1); t.Add(c1 + 1); t.Add(c1 + 2);
            }
        }
        /// <summary>Torus w płaszczyźnie XY (oś obrotu = Z), promień R, grubość r.</summary>
        public void Torus(float R, float r, int segU = 40, int segV = 12)
        {
            int s = v.Count;
            for (int i = 0; i <= segU; i++)
            {
                float a = 2 * Mathf.PI * i / segU; var kier = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                for (int j = 0; j <= segV; j++)
                {
                    float b = 2 * Mathf.PI * j / segV; var nn = kier * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
                    v.Add(kier * R + nn * r); n.Add(nn); uv.Add(new Vector2(a * R, b * r));
                }
            }
            for (int i = 0; i < segU; i++)
                for (int j = 0; j < segV; j++)
                {
                    int a = s + i * (segV + 1) + j, b = a + segV + 1;
                    t.Add(a); t.Add(b); t.Add(a + 1); t.Add(a + 1); t.Add(b); t.Add(b + 1);
                }
        }
        public Mesh Mesh(string nazwa)
        {
            var m = new Mesh { name = nazwa }; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateBounds(); m.RecalculateTangents(); return m;
        }
    }

    static GameObject Obiekt(Transform rodzic, string nazwa, string klucz, Func<S> buduj, Material mat)
    {
        if (!siatki.TryGetValue(klucz, out var mesh) || !mesh)
        {
            var s = buduj(); mesh = s.Mesh(nazwa);
            if (!AssetDatabase.IsValidFolder(Out.TrimEnd('/'))) { Directory.CreateDirectory(Out); AssetDatabase.Refresh(); }
            BraxZapisAssetu.Utworz(mesh, Out + klucz + ".asset");
            siatki[klucz] = mesh;
        }
        var g = new GameObject(nazwa); g.transform.SetParent(rodzic, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true;
        return g;
    }

    static void Materialy()
    {
        if (lakier) return;
        lakier = BraxCityMaterials.Surface("Jacht: konsola steru — biały lakier jachtowy", new Color(.93f, .93f, .92f), .22f);
        ekran = BraxCityMaterials.Surface("Jacht: ekrany nawigacji (czarne szkło)", new Color(.035f, .05f, .065f), .06f, .25f);
        chrom = BraxRegion.Fitting("Jacht: ster — chrom polerowany", new Color(.84f, .85f, .87f), .1f, 1f);
        wieniec = BraxCityMaterials.Surface("Jacht: wieniec koła — skóra czarna szyta", new Color(.05f, .045f, .045f), .55f);
        skora = BraxUliceKit.TexPlik("Jacht: fotel sternika — skóra pikowana biała", "Assets/Source/Claude/Nova/tekstury/", "nova_skora_pikowana", .5f,
            new Color(.93f, .91f, .87f), new Color(.92f, .9f, .86f), .45f);
    }

    /// <summary>Lokalne wysokości (y) wszystkich powierzchni modelu pod punktem (x, z) korzenia łodzi.</summary>
    static List<float> Sonda(Transform v, List<MeshCollider> nasze, float x, float z)
    {
        var p = v.TransformPoint(new Vector3(x, 0f, z));
        return Physics.RaycastAll(p + v.up * 40f, -v.up, 80f, ~0, QueryTriggerInteraction.Collide)
            .Where(h => nasze.Contains(h.collider as MeshCollider))
            .Select(h => v.InverseTransformPoint(h.point).y).OrderBy(y => y).ToList();
    }

    /// <summary>Buduje stanowisko sternika jachtu i ustawia: seat, uchwyty dłoni, koło, kamerę. false = model nie pasuje.</summary>
    public static bool Jacht(Transform v, GameObject model, BraxVessel vessel)
    {
        Materialy();
        float deckY = vessel.seat.y, l = vessel.length;   // Siedzenie(): pokład flybridge'u (rufa, najniższa powierzchnia)
        var tmp = new List<MeshCollider>();
        foreach (var f in model.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!f.sharedMesh) continue;
            var c = f.gameObject.AddComponent<MeshCollider>(); c.sharedMesh = f.sharedMesh; tmp.Add(c);
        }
        Physics.SyncTransforms();
        float zSzyba = float.NaN, deckPrzod = deckY;
        try
        {
            // od środka kadłuba do przodu: pierwsze miejsce z szybą/owiewką flybridge'u (0,25…1,5 m nad pokładem) bez hardtopu nad nią
            for (float z = -.05f * l; z <= .35f * l; z += .08f)
            {
                var h = Sonda(v, tmp, 0f, z);
                bool pokl = h.Any(y => Mathf.Abs(y - deckY) < .25f * K);
                // progi × K: jacht w prawdziwej wielkości (10.11) — szyba flybridge'u 1,45 m nad pokładem, hardtop 2,95 m
                bool szyba = h.Any(y => y > deckY + .25f * K && y < deckY + 1.7f * K);
                bool dach = h.Any(y => y >= deckY + 2.2f * K && y < deckY + 5f * K);
                if (szyba && !dach) { zSzyba = z; break; }
                if (pokl) deckPrzod = h.First(y => Mathf.Abs(y - deckY) < .25f * K);
            }
        }
        finally { foreach (var c in tmp) UnityEngine.Object.DestroyImmediate(c); }
        if (float.IsNaN(zSzyba))
        {
            Debug.LogWarning("BRAXHORN_JACHT_STER DO POPRAWKI: nie znaleziono szyby flybridge'u — stanowisko w 15 % długości od środka");
            zSzyba = .15f * l;
        }
        float yd = deckPrzod;
        // ---- wymiary realne × K ----
        // 10.11 (podgląd w prawdziwej wielkości): szyba flybridge'u sięga 1,45 m nad pokład — sternik siedzi wysoko (fotel 0,78 m
        // jak na flybridge'ach), oczy ~1,6 m, więc widzi dziób nad szybą; pulpit i koło podniesione razem z fotelem
        float szer = 1.5f * K, glebok = .55f * K, hPrzod = .98f * K, hTyl = 1.18f * K, hSiedz = .78f * K;
        float zP = zSzyba - .12f, zT = zP - glebok;                                  // przednia i tylna ściana konsoli
        var root = new GameObject("Jacht: stanowisko sternika (konsola, koło, fotel)").transform; root.SetParent(v, false);
        root.localPosition = new Vector3(0f, yd, 0f);
        // konsola: bryła z pochyłym pulpitem (przód niżej, przy sterniku wyżej)
        Obiekt(root, "Jacht: konsola steru", "konsola", () =>
        {
            var s = new S();
            s.Profil(new[] { new Vector2(0f, 0f), new Vector2(0f, hPrzod), new Vector2(-glebok, hTyl), new Vector2(-glebok, 0f) }, -szer * .5f, szer * .5f);
            return s;
        }, lakier).transform.localPosition = new Vector3(0f, 0f, zP);
        // dwa ekrany nawigacji w pulpicie (cienkie bryły 1 cm na pulpicie, nie w jego płaszczyźnie — bez migotania)
        float kat = Mathf.Atan2(hTyl - hPrzod, glebok) * Mathf.Rad2Deg;
        var qPulpit = Quaternion.Euler(kat, 0f, 0f);   // Euler(+x) przechyla „górę” ku +Z: pulpit wznosi się ku sternikowi (−Z)
        var nPulpit = qPulpit * Vector3.up;
        foreach (float ex in new[] { -.36f * K, .36f * K })
        {
            float zm = zP - glebok * .45f, ym = Mathf.Lerp(hPrzod, hTyl, .45f);
            var e = Obiekt(root, "Jacht: ekran nawigacji", "ekran", () => { var s = new S(); s.Pudlo(Vector3.zero, new Vector3(.46f * K, .012f, .30f * K), Quaternion.identity); return s; }, ekran).transform;
            e.localPosition = new Vector3(ex, ym, zm) + nPulpit * .012f; e.localRotation = qPulpit;
        }
        // relingi konsoli (chrom) wzdłuż krawędzi pulpitu przy sterniku
        Obiekt(root, "Jacht: reling konsoli (chrom)", "reling", () =>
        {
            var s = new S(); float y0 = hTyl + .05f * K, x = szer * .5f - .06f * K;
            s.Rura(new Vector3(-x, y0, zT + .04f * K), new Vector3(x, y0, zT + .04f * K), .014f * K);
            s.Rura(new Vector3(-x, hTyl - .01f, zT + .04f * K), new Vector3(-x, y0, zT + .04f * K), .014f * K);
            s.Rura(new Vector3(x, hTyl - .01f, zT + .04f * K), new Vector3(x, y0, zT + .04f * K), .014f * K);
            return s;
        }, chrom).transform.localPosition = Vector3.zero;
        // koło sterowe: oś pochylona do sternika (25° od pionu), środek 0,86 m nad pokładem, 10 cm za konsolą
        float R = .26f * K, katKola = 25f;
        var os = new Vector3(0f, Mathf.Sin(katKola * Mathf.Deg2Rad), -Mathf.Cos(katKola * Mathf.Deg2Rad));
        var srodekKola = new Vector3(0f, 1.10f * K, zT - .10f * K);
        Obiekt(root, "Jacht: kolumna steru (chrom)", "kolumna", () =>
        {
            var s = new S(); s.Rura(new Vector3(0f, srodekKola.y - os.y * .16f * K, zT + .02f), srodekKola, .035f * K); return s;
        }, chrom).transform.localPosition = Vector3.zero;
        var kolo = new GameObject("Jacht: koło sterowe").transform; kolo.SetParent(root, false);
        kolo.localPosition = srodekKola;
        kolo.localRotation = Quaternion.LookRotation(os, Vector3.up);   // lokalne Z = oś koła (do sternika), Y = góra wieńca
        Obiekt(kolo, "Jacht: wieniec koła (skóra)", "wieniec", () => { var s = new S(); s.Torus(R, .022f * K); return s; }, wieniec);
        Obiekt(kolo, "Jacht: piasta i szprychy (chrom)", "szprychy", () =>
        {
            var s = new S();
            s.Rura(new Vector3(0, 0, -.03f * K), new Vector3(0, 0, .04f * K), .055f * K);
            foreach (float a in new[] { 90f, 210f, 330f })
            {
                var d = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0f);
                s.Rura(d * .05f * K, d * (R - .015f * K), .012f * K, 10);
            }
            return s;
        }, chrom);
        // dłonie na wieńcu (godz. 10 i 2) — dzieci koła, więc obracają się razem z nim. Lokalne +X koła = lewa ręka sternika.
        Transform Uchwyt(string n, float x)
        {
            var g = new GameObject(n).transform; g.SetParent(kolo, false); g.localPosition = new Vector3(x * R * .866f, R * .5f, 0f); return g;
        }
        // ustalamy, która strona koła jest po lewej sternika (sternik patrzy w +Z łodzi, lewa = −X)
        float lewaZnak = Mathf.Sign(-(kolo.localRotation * Vector3.right).x);
        vessel.leftGrip = Uchwyt("LEWA dłoń na kole sterowym", lewaZnak);
        vessel.rightGrip = Uchwyt("PRAWA dłoń na kole sterowym", -lewaZnak);
        vessel.kolo = kolo;
        // fotel sternika: siedzisko 0,55 m, biodra 0,48 m za środkiem koła (podgląd 10.11: przy 0,60 m bark–wieniec 0,99 > ręka 0,75)
        float zBiodra = srodekKola.z - .48f * K;
        Obiekt(root, "Jacht: fotel sternika — podstawa i podnóżek (chrom)", "fotel_noga", () =>
        {
            var s = new S(); float zf = zBiodra - .05f * K;
            s.Rura(new Vector3(0, 0, zf), new Vector3(0, hSiedz - .06f * K, zf), .05f * K);
            s.Rura(new Vector3(0, .01f, zf), new Vector3(0, .02f, zf), .24f * K, 24);
            // podnóżek: pierścień na wysokości 0,35 m
            for (int i = 0; i < 16; i++)
            {
                float a0 = i * Mathf.PI / 8f, a1 = (i + 1) * Mathf.PI / 8f; float rr = .21f * K, y = .35f * K;
                s.Rura(new Vector3(Mathf.Cos(a0) * rr, y, zf + Mathf.Sin(a0) * rr), new Vector3(Mathf.Cos(a1) * rr, y, zf + Mathf.Sin(a1) * rr), .012f * K, 8);
            }
            foreach (float a in new[] { 0f, 120f, 240f })
            {
                var d = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0f, Mathf.Sin(a * Mathf.Deg2Rad));
                s.Rura(new Vector3(0, .35f * K, zf) + d * .05f * K, new Vector3(0, .35f * K, zf) + d * .21f * K, .01f * K, 8);
            }
            return s;
        }, chrom);
        Obiekt(root, "Jacht: fotel sternika — siedzisko i oparcie (skóra)", "fotel", () =>
        {
            var s = new S();
            float dy = hSiedz - .55f * K;   // cały fotel wyżej o różnicę względem niskiego fotela
            s.Pudlo(new Vector3(0, .495f * K + dy, zBiodra - .05f * K), new Vector3(.52f * K, .11f * K, .48f * K), Quaternion.identity);
            s.Pudlo(new Vector3(0, .80f * K + dy, zBiodra - .31f * K), new Vector3(.50f * K, .52f * K, .09f * K), Quaternion.Euler(-10f, 0, 0));
            foreach (float x in new[] { -.29f * K, .29f * K }) s.Pudlo(new Vector3(x, .66f * K + dy, zBiodra - .10f * K), new Vector3(.06f * K, .05f * K, .34f * K), Quaternion.identity);
            return s;
        }, skora);
        vessel.seat = new Vector3(0f, yd + hSiedz, zBiodra);
        vessel.pokladPrzodZ = zT - .25f * K;   // chodzenie po flybridge'u kończy się za konsolą (nie przez pulpit i szybę)
        vessel.helmTargetLocal = new Vector3(0f, yd, 0f) + srodekKola;
        // kamera zza steru: 1,86 m nad pokładem i 0,3 m za biodrami sternika — nad przyciemnianą szybą flybridge'u (górna krawędź
        // 1,45 m) widać dziób i morze, w dole kadru koło i pulpit (Brax w tym widoku niewidoczny, zostaje jego cień)
        vessel.helmCameraLocal = new Vector3(0f, yd + 1.86f * K, zBiodra - .30f * K);
        Debug.Log("BRAXHORN_JACHT_STER stanowisko: szyba z=" + zSzyba.ToString("F2") + " pokład y=" + yd.ToString("F2") +
                  " konsola z " + zT.ToString("F2") + "…" + zP.ToString("F2") + " koło " + vessel.helmTargetLocal.ToString("F2") +
                  " fotel " + vessel.seat.ToString("F2") + " kamera " + vessel.helmCameraLocal.ToString("F2"));
        return true;
    }
}
