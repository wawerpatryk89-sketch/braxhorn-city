using System.Collections.Generic;
using UnityEngine;

// MIASTO R1 — KOŁA MOTOCYKLI, KTÓRE SIĘ KRĘCĄ (18.11.2026, właściciel 14:49: „Zrób mi motor, w którym się kręcą koła. Podstaw go
// pod siłownię, żebym mógł jeździć”; „A mamy tam ścigacze gdzieś są”). Motocykle z paczki właściciela (Tripo, pojazdy_motory_01)
// to jedna bryła — koła nie mogły się kręcić (stan 12.11: „koła motorów Tripo jeszcze się nie kręcą”). Przy buildzie każde koło
// jest wyszukiwane w samej siatce i wycinane do osobnego obiektu z osią w piaście:
//  1) bieżnik opony = najgęstszy pełny okrąg w płaszczyźnie YZ motocykla (oś koła na wysokości promienia nad ziemią: model stoi
//     na y = 0): siatka przeszukiwania środka i promienia (R 0.10…0.20 długości motocykla), wynik = liczba wierzchołków na okręgu
//     × (pokrycie 16 wycinków kąta / 16)² — pełny okrąg opony wygrywa z łukami owiewki, błotnika i wydechu;
//  2) szerokość opony z przekroju: od bieżnika na boki do pierwszej warstwy bez powierzchni (szczelina do lag / wahacza),
//     najwyżej 4 % (przód) / 5.8 % (tył) długości — lagi widelca i wahacz zostają w ramie;
//  3) do koła trafia trójkąt, którego wszystkie wierzchołki leżą w walcu koła (promień R + tolerancja, szerokość opony):
//     opona, felga, szprychy, piasta (tarcze i zębatka w obrysie opony kręcą się razem z kołem — jak w prawdziwym motocyklu);
//     błotnik, lagi, zacisk, wahacz zostają w nieruchomej ramie.
// Sprawdzenie: pełne okręgi, rozstaw osi 0.55…0.78 długości, oba koła mają trójkąty — inaczej motocykl zostaje jedną bryłą
// (jak dotąd) i log DO POPRAWKI. Log BRAXHORN_MOTOR_KOLA: promienie, osie, szerokości, trójkąty kół / ramy.
internal static class BraxKolaMotoru
{
    public struct Kolo
    {
        public bool ok; public float x, y, z, R, hw; public int naOkregu, wycinki; public string brak;
        public override string ToString() => ok ? "R " + R.ToString("0.000") + " m, oś (" + x.ToString("0.000") + ", " + y.ToString("0.000") + ", " + z.ToString("0.000") +
                                                  "), szer. opony " + (2f * hw).ToString("0.000") + " m, bieżnik " + naOkregu + " wierzch., " + wycinki + "/16 wycinków" : "BRAK (" + brak + ")";
    }

    const int Wycinkow = 16;

    static void Ocena(List<Vector2> yz, float zc, float yc, float R, float tol, int[] sek, out int n, out int pokrycie)
    {
        n = 0; for (int s = 0; s < Wycinkow; s++) sek[s] = 0;
        for (int i = 0; i < yz.Count; i++)
        {
            float dz = yz[i].y - zc, dy = yz[i].x - yc, d = Mathf.Sqrt(dz * dz + dy * dy);
            if (Mathf.Abs(d - R) > tol) continue;
            n++;
            int s = (int)((Mathf.Atan2(dy, dz) + Mathf.PI) / (2f * Mathf.PI) * Wycinkow); if (s >= Wycinkow) s = Wycinkow - 1;
            sek[s]++;
        }
        pokrycie = 0; for (int s = 0; s < Wycinkow; s++) if (sek[s] >= 2) pokrycie++;
    }

    /// <summary>Koło w wierzchołkach motocykla p (układ motocykla: x w prawo, y w górę, z do przodu, ziemia y = 0, długość L).
    /// przednie = szuka w przedniej połowie (z &gt; zSrodek).</summary>
    public static Kolo Szukaj(IList<Vector3> p, IList<int> tri, bool przednie, float zSrodek, float L)
    {
        var k = new Kolo();
        var yz = new List<Vector2>(); var xs = new List<float>(); float zMin = float.MaxValue, zMax = float.MinValue;
        for (int i = 0; i < p.Count; i++)
        {
            var v = p[i];
            if ((v.z > zSrodek) != przednie || Mathf.Abs(v.x) > .12f * L || v.y > .42f * L) continue;
            yz.Add(new Vector2(v.y, v.z)); xs.Add(v.x); zMin = Mathf.Min(zMin, v.z); zMax = Mathf.Max(zMax, v.z);
        }
        if (yz.Count < 200) { k.brak = "za mało wierzchołków w dolnej części (" + yz.Count + ")"; return k; }
        float tol = .006f * L; var sek = new int[Wycinkow];
        float best = -1f, bz = 0f, by = 0f, bR = 0f;
        // zgrubnie: R co 0.4 % L, środek co 0.5 % L, oś na wysokości R (opona stoi na ziemi)
        for (float R = .10f * L; R <= .20f * L; R += .004f * L)
            for (float zc = zMin + R * .8f; zc <= zMax - R * .8f; zc += .005f * L)
            {
                Ocena(yz, zc, R, R, tol, sek, out int n, out int pok);
                float s = n * (pok / (float)Wycinkow) * (pok / (float)Wycinkow);
                if (s > best) { best = s; bz = zc; by = R; bR = R; }
            }
        if (best <= 0f) { k.brak = "brak okręgu"; return k; }
        // dokładnie: środek ± 0.6 % L, wysokość osi ± 1 % L (ugięte zawieszenie / opona w ziemi), promień ± 0.5 % L
        float z0 = bz, y0 = by, R0 = bR; best = -1f;
        for (float R = R0 - .005f * L; R <= R0 + .0051f * L; R += .001f * L)
            for (float zc = z0 - .006f * L; zc <= z0 + .0061f * L; zc += .001f * L)
                for (float yc = R - .01f * L; yc <= R + .0101f * L; yc += .0025f * L)
                {
                    Ocena(yz, zc, yc, R, tol * .75f, sek, out int n, out int pok);
                    float s = n * (pok / (float)Wycinkow) * (pok / (float)Wycinkow);
                    if (s > best) { best = s; bz = zc; by = yc; bR = R; k.naOkregu = n; k.wycinki = pok; }
                }
        k.z = bz; k.y = by; k.R = bR;
        // oś opony w poprzek (mediana x bieżnika) i szerokość: od bieżnika na boki do pierwszej pustej warstwy przekroju
        var bx = new List<float>();
        for (int i = 0; i < yz.Count; i++)
        {
            float dz = yz[i].y - bz, dy = yz[i].x - by;
            if (Mathf.Abs(Mathf.Sqrt(dz * dz + dy * dy) - bR) <= tol * .75f) bx.Add(xs[i]);
        }
        if (bx.Count < 20) { k.brak = "bieżnik bez wierzchołków"; return k; }
        bx.Sort(); k.x = bx[bx.Count / 2];
        float hw0 = Mathf.Max(Mathf.Abs(bx[(int)(bx.Count * .05f)] - k.x), Mathf.Abs(bx[(int)(bx.Count * .95f)] - k.x));
        float krok = .002f * L, cap = (przednie ? .040f : .058f) * L; int nWarstw = Mathf.CeilToInt(cap / krok) + 1;
        // warstwy przekroju zajęte przez POWIERZCHNIĘ (trójkąty w pierścieniu 0.55 R … R): trójkąt zaznacza wszystkie warstwy, przez które
        // przechodzi — rzadsze wierzchołki boku opony nie robią fałszywej szczeliny; prawdziwa szczelina (do lag, wahacza) zostaje pusta
        var warstwy = new int[nWarstw]; int szczyt = 0;
        bool WPierscieniu(Vector3 q) { float dz = q.z - bz, dy = q.y - by, d = Mathf.Sqrt(dz * dz + dy * dy); return d >= .55f * bR && d <= bR + tol; }
        for (int t = 0; t + 2 < tri.Count; t += 3)
        {
            Vector3 a = p[tri[t]], b = p[tri[t + 1]], c = p[tri[t + 2]];
            if ((a.z > zSrodek) != przednie || !WPierscieniu(a) || !WPierscieniu(b) || !WPierscieniu(c)) continue;
            float xa = a.x - k.x, xb = b.x - k.x, xc = c.x - k.x;
            float lo = Mathf.Min(Mathf.Abs(xa), Mathf.Min(Mathf.Abs(xb), Mathf.Abs(xc))), hi = Mathf.Max(Mathf.Abs(xa), Mathf.Max(Mathf.Abs(xb), Mathf.Abs(xc)));
            if (Mathf.Min(xa, Mathf.Min(xb, xc)) < 0f && Mathf.Max(xa, Mathf.Max(xb, xc)) > 0f) lo = 0f;
            if (hi - lo > .03f * L) continue;                                   // długi trójkąt w poprzek (zlany z ramą) nie zamyka szczeliny
            for (int w = (int)(lo / krok); w <= (int)(hi / krok) && w < nWarstw; w++) { warstwy[w]++; szczyt = Mathf.Max(szczyt, warstwy[w]); }
        }
        k.hw = cap;
        for (int w = Mathf.Max(1, (int)(hw0 / krok)); w < nWarstw; w++)
            if (warstwy[w] <= Mathf.Max(1, szczyt / 100)) { k.hw = w * krok; break; }
        k.hw = Mathf.Clamp(k.hw, Mathf.Min(hw0, cap), cap);
        if (k.wycinki < 12) { k.brak = "okrąg niepełny (" + k.wycinki + "/16 wycinków)"; return k; }
        if (k.naOkregu < 120) { k.brak = "za mało wierzchołków bieżnika (" + k.naOkregu + ")"; return k; }
        if (bR < .105f * L || bR > .195f * L) { k.brak = "promień poza zakresem (" + (bR / L).ToString("0.000") + " L)"; return k; }
        k.ok = true;
        return k;
    }

    /// <summary>Czy punkt motocykla leży w walcu koła (z tolerancją).</summary>
    public static bool WKole(Kolo k, Vector3 v, float L)
    {
        float dz = v.z - k.z, dy = v.y - k.y;
        return dz * dz + dy * dy < (k.R + .006f * L) * (k.R + .006f * L) && Mathf.Abs(v.x - k.x) < k.hw + .002f * L;
    }

    /// <summary>Podział trójkątów jednej części siatki: 0 = rama, 1 = koło przednie, 2 = koło tylne (wszystkie wierzchołki w walcu).</summary>
    public static int[] Podzial(IList<Vector3> p, int[] tri, Kolo przod, Kolo tyl, float L)
    {
        var wynik = new int[tri.Length / 3];
        for (int t = 0; t < wynik.Length; t++)
        {
            Vector3 a = p[tri[3 * t]], b = p[tri[3 * t + 1]], c = p[tri[3 * t + 2]];
            if (przod.ok && WKole(przod, a, L) && WKole(przod, b, L) && WKole(przod, c, L)) wynik[t] = 1;
            else if (tyl.ok && WKole(tyl, a, L) && WKole(tyl, b, L) && WKole(tyl, c, L)) wynik[t] = 2;
        }
        return wynik;
    }

#if !BRAX_TEST_KOLA   // test poza Unity (unity-tools/claude/kola_motoru) kompiluje samo wyszukiwanie i podział
    // ten sam model (np. sportowy na parkingu i pod siłownią) — siatki ramy i kół liczone raz na build
    sealed class Gotowe { public bool ok; public string opis; public Kolo kp, kt; public float rozstaw; public Mesh[] rama; public readonly List<(int f, int nr, Mesh mesh)> kola = new List<(int, int, Mesh)>(); }
    static readonly Dictionary<string, Gotowe> gotowe = new Dictionary<string, Gotowe>();

    /// <summary>Wycina koła z modelu motocykla (model = dziecko pojazdu v, ustawiony jak przy każdym motocyklu tego modelu).
    /// Ustawia v.kolP / v.kolT, v.rozstaw, v.rKola. false = motocykl zostaje jedną bryłą (opis mówi dlaczego).</summary>
    public static bool Wydziel(BraxVessel v, GameObject model, float L, string nazwa, out string opis)
    {
        var mfs = model.GetComponentsInChildren<MeshFilter>(true);
        if (!gotowe.TryGetValue(nazwa, out var g) || g.rama == null || g.rama.Length != mfs.Length) { g = Licz(v, mfs, L, nazwa); gotowe[nazwa] = g; }
        else g.opis = g.opis.EndsWith(" (z pamięci)") ? g.opis : g.opis + " (z pamięci)";
        opis = g.opis;
        if (!g.ok) return false;
        var piastaP = new GameObject("koło przednie (kręci się)").transform; piastaP.SetParent(v.transform, false); piastaP.localPosition = new Vector3(g.kp.x, g.kp.y, g.kp.z);
        var piastaT = new GameObject("koło tylne (kręci się)").transform; piastaT.SetParent(v.transform, false); piastaT.localPosition = new Vector3(g.kt.x, g.kt.y, g.kt.z);
        for (int f = 0; f < mfs.Length; f++) if (g.rama[f]) mfs[f].sharedMesh = g.rama[f];
        foreach (var (f, nr, mesh) in g.kola)
        {
            var mr = mfs[f].GetComponent<MeshRenderer>();
            var o = new GameObject((nr == 1 ? "koło przednie: " : "koło tylne: ") + mesh.name); o.transform.SetParent(nr == 1 ? piastaP : piastaT, false);
            o.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = o.AddComponent<MeshRenderer>(); r.sharedMaterials = mr ? mr.sharedMaterials : new Material[0];
            r.shadowCastingMode = mr ? mr.shadowCastingMode : UnityEngine.Rendering.ShadowCastingMode.On;
        }
        v.kolP = piastaP; v.kolT = piastaT; v.rozstaw = g.rozstaw; v.rKola = (g.kp.R + g.kt.R) * .5f;
        return true;
    }

    static Gotowe Licz(BraxVessel v, MeshFilter[] mfs, float L, string nazwa)
    {
        var g = new Gotowe { rama = new Mesh[mfs.Length] };
        var w2l = v.transform.worldToLocalMatrix;
        var wszystkie = new List<Vector3>(); var wszystkieT = new List<int>(); var lokalne = new Vector3[mfs.Length][];
        for (int f = 0; f < mfs.Length; f++)
        {
            var m = mfs[f].sharedMesh; if (!m) continue;
            if (!m.isReadable) { g.opis = "siatka " + m.name + " nie do odczytu"; return g; }
            var M = w2l * mfs[f].transform.localToWorldMatrix; var src = m.vertices; var dst = new Vector3[src.Length]; int b0 = wszystkie.Count;
            for (int i = 0; i < src.Length; i++) { dst[i] = M.MultiplyPoint3x4(src[i]); wszystkie.Add(dst[i]); }
            for (int s = 0; s < m.subMeshCount; s++) foreach (int i in m.GetTriangles(s)) wszystkieT.Add(b0 + i);
            lokalne[f] = dst;
        }
        if (wszystkie.Count == 0) { g.opis = "brak siatki"; return g; }
        float zA = float.MaxValue, zB = float.MinValue; foreach (var q in wszystkie) { zA = Mathf.Min(zA, q.z); zB = Mathf.Max(zB, q.z); }
        float zS = (zA + zB) * .5f;
        var kp = Szukaj(wszystkie, wszystkieT, true, zS, L); var kt = Szukaj(wszystkie, wszystkieT, false, zS, L);
        g.opis = "przód " + kp + "; tył " + kt; g.kp = kp; g.kt = kt;
        if (!kp.ok || !kt.ok) return g;
        float rozstaw = kp.z - kt.z; g.rozstaw = rozstaw;
        if (rozstaw < .55f * L || rozstaw > .78f * L) { g.opis += "; rozstaw osi " + (rozstaw / L).ToString("0.00") + " L poza 0.55…0.78"; return g; }
        // podział każdej części; nowe siatki dopiero, gdy oba koła mają trójkąty
        var podzialy = new int[mfs.Length][][]; int nP = 0, nT = 0, nR = 0;
        for (int f = 0; f < mfs.Length; f++)
        {
            var m = mfs[f].sharedMesh; if (!m || lokalne[f] == null) continue;
            var sub = new int[m.subMeshCount][];
            for (int s = 0; s < m.subMeshCount; s++)
            {
                sub[s] = Podzial(lokalne[f], m.GetTriangles(s), kp, kt, L);
                foreach (int c in sub[s]) { if (c == 1) nP++; else if (c == 2) nT++; else nR++; }
            }
            podzialy[f] = sub;
        }
        g.opis += "; trójkąty: koło przednie " + nP + ", tylne " + nT + ", rama " + nR;
        if (nP < 50 || nT < 50) { g.opis += " — za mało trójkątów koła"; return g; }
        for (int f = 0; f < mfs.Length; f++)
        {
            var m = mfs[f].sharedMesh; var sub = podzialy[f]; if (sub == null) continue;
            bool cos = false; foreach (var s in sub) foreach (int c in s) if (c != 0) { cos = true; break; }
            if (!cos) continue;
            // rama: te same wierzchołki, bez trójkątów kół
            var rama = Object.Instantiate(m); rama.name = m.name + " (rama bez kół)";
            for (int s = 0; s < m.subMeshCount; s++)
            {
                var t = m.GetTriangles(s); var zost = new List<int>();
                for (int i = 0; i < sub[s].Length; i++) if (sub[s][i] == 0) { zost.Add(t[3 * i]); zost.Add(t[3 * i + 1]); zost.Add(t[3 * i + 2]); }
                rama.SetTriangles(zost, s);
            }
            rama.RecalculateBounds();
            Zapisz(rama, nazwa + " rama"); g.rama[f] = rama;
            var M = w2l * mfs[f].transform.localToWorldMatrix;
            foreach (var (nr, os) in new[] { (1, new Vector3(kp.x, kp.y, kp.z)), (2, new Vector3(kt.x, kt.y, kt.z)) })
            {
                var kolo = Kawalek(m, sub, nr, M, os);
                if (kolo == null) continue;
                Zapisz(kolo, nazwa + (nr == 1 ? " koło przednie" : " koło tylne"));
                g.kola.Add((f, nr, kolo));
            }
        }
        g.opis += "; rozstaw osi " + rozstaw.ToString("0.00") + " m";
        g.ok = true;
        return g;
    }

    static void Zapisz(Mesh m, string nazwa)
    {
        System.IO.Directory.CreateDirectory(BraxMiastoA.Out + "/Siatki");
        BraxZapisAssetu.Utworz(m, UnityEditor.AssetDatabase.GenerateUniqueAssetPath(BraxMiastoA.Out + "/Siatki/" + BraxUliceKit.Bezpieczna(nazwa) + ".asset"));
    }

    // trójkąty koła nr z części m — wierzchołki w układzie piasty (oś obrotu = lokalne X pojazdu)
    static Mesh Kawalek(Mesh m, int[][] sub, int nr, Matrix4x4 M, Vector3 piasta)
    {
        var v = m.vertices; var n = m.normals; var t4 = m.tangents; var uv = m.uv; var uv2 = m.uv2; var c32 = m.colors32;
        var mapa = new Dictionary<int, int>(); var V = new List<Vector3>(); var N = new List<Vector3>(); var T = new List<Vector4>();
        var U = new List<Vector2>(); var U2 = new List<Vector2>(); var C = new List<Color32>();
        var tri = new List<int>[m.subMeshCount]; int ile = 0;
        for (int s = 0; s < m.subMeshCount; s++)
        {
            tri[s] = new List<int>(); var t = m.GetTriangles(s);
            for (int i = 0; i < sub[s].Length; i++)
            {
                if (sub[s][i] != nr) continue;
                for (int j = 0; j < 3; j++)
                {
                    int a = t[3 * i + j];
                    if (!mapa.TryGetValue(a, out int b))
                    {
                        b = V.Count; mapa[a] = b; V.Add(M.MultiplyPoint3x4(v[a]) - piasta);
                        if (n.Length == v.Length) N.Add(M.MultiplyVector(n[a]).normalized);
                        if (t4 != null && t4.Length == v.Length) { var tv = M.MultiplyVector(new Vector3(t4[a].x, t4[a].y, t4[a].z)).normalized; T.Add(new Vector4(tv.x, tv.y, tv.z, t4[a].w)); }
                        if (uv.Length == v.Length) U.Add(uv[a]);
                        if (uv2 != null && uv2.Length == v.Length) U2.Add(uv2[a]);
                        if (c32 != null && c32.Length == v.Length) C.Add(c32[a]);
                    }
                    tri[s].Add(b);
                }
                ile++;
            }
        }
        if (ile == 0) return null;
        var k = new Mesh { name = m.name + (nr == 1 ? " (koło przednie)" : " (koło tylne)") };
        if (V.Count > 65000) k.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        k.SetVertices(V); if (N.Count == V.Count) k.SetNormals(N); if (T.Count == V.Count) k.SetTangents(T);
        if (U.Count == V.Count) k.SetUVs(0, U); if (U2.Count == V.Count) k.SetUVs(1, U2); if (C.Count == V.Count) k.SetColors(C);
        k.subMeshCount = m.subMeshCount; for (int s = 0; s < m.subMeshCount; s++) k.SetTriangles(tri[s], s);
        if (N.Count != V.Count) k.RecalculateNormals();
        k.RecalculateBounds();
        return k;
    }
#endif
}
