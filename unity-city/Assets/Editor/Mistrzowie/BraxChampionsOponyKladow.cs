using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using T = BraxChampionsTorKladow;
using W = BraxWnetrzaKit;

/// <summary>THE QUAD TRACK'S TYRES AND TAPE (owner 24.11.2026 17:11 "Jakie rampy barierki na zakrętach może?", his pick
/// "Opony na zakrętach" + "Ściany z opon"). What real motocross and quad tracks put at their turns (the track standards of
/// Motorcycling Victoria 2024, 4.8.1-4.8.2, 4.14.1):
///   - a bundle of tyres at the apex of each turn, interlocked pyramid-style - 3-2-1 (six) at the field's and the start's
///     turns, 2-1 (three) at the woods' switchbacks - on the inside, past the stakes;
///   - breakable plastic tape between the field's stakes (red / white, sagging a little), in a picture with its own
///     transparency: a thin bright line drawn as geometry flickers from 50 m (ZASADA-MIGOTANIE 3.1), in a texture its
///     mipmaps fade it out;
///   - tyre walls: round the outside of the fast turns (a long run in, a hard braking: 90 deg+ where the race's speed falls
///     4 m/s+ from 45 km/h+) and between two sections of the track that run close side by side (shoulders under 10 m
///     apart, 80 m+ apart along the loop; the standard's "adjacent sections ... separated by straw bales or tyre walls").
///     Two rows of upright tyres, the top row nested in the bottom one's gaps (1.1 m), every 3 m a box collider: a quad
///     that hits one stops.
/// The tyre is Poly Haven's scanned Old Tyre (CC0, a worn 0.6 m car tyre, its own 4K maps: prepare_assets.py 'old_tyre'),
/// real size x K. Three levels per group of tyres (LODGroup): the scan (2880 triangles) close up, the same simplified with its
/// UV seams kept (896, unity-tools/mistrzowie/opony/opona_lod.py), a plain 10-sided ring of its mean rubber colour far off -
/// the walls start at the simplified one; at most 28 tyres a group (16-bit indices).</summary>
public static class BraxChampionsOponyKladow
{
    static float K => BraxClaudeOsiedle.K;
    public const string Dane = "Assets/Source/Mistrzowie/Opony/opona.bytes";
    const string Out = "Assets/PlayableGenerated/Mistrzowie";
    static string Folder => Out + "/Meshes/TorKladow";
    public const float R = .3f, Szer = .165f, Wiersz = .52f;   // the scan: 0.6 m across, 0.165 m wide; a nested row 0.6 sin 60 up
    public const float OdPobocza = 1.1f;                        // the tyres past the shoulder (the stakes stand at 0.5 m)
    public const float MinKatPiramidy = 35f, MinKatSciany = 90f, MinVWjazdu = 12.5f, MinHamowanie = 4f;
    public const float MaxSzczelina = 10f, MinWzdluz = 80f, NaGrupe = 28f;
    // the levels: a bundle (a focal point at the apex) the scan close up, the simplified one, the ring; a wall (in bulk) the
    // simplified one up close and the ring beyond ~40 m (x K) - a phone draws the walls' 2000-odd tyres as rings
    static readonly int[] PoziomyPiramidy = { 0, 1, 2 }, PoziomySciany = { 1, 2 };
    static readonly float[] ProgiPiramidy = { .15f, .05f, .008f }, ProgiSciany = { .45f, .008f };
    public const float TasmaH = .6f, TasmaPas = .24f, TasmaZwis = .04f;

    // ---------------------------------------------------------------- the tyre's levels
    sealed class Poziom { public Vector3[] v, n; public Vector2[] uv; public int[] t; }
    static Poziom[] poziomy;
    static Poziom[] Poziomy()
    {
        if (poziomy != null) return poziomy;
        if (!File.Exists(Dane)) return null;
        var lista = new List<Poziom>();
        using (var r = new BinaryReader(File.OpenRead(Dane)))
        {
            if (new string(r.ReadChars(4)) != "OPN1") throw new InvalidDataException("not a tyre: " + Dane);
            int ile = r.ReadInt32();
            for (int l = 0; l < ile; l++)
            {
                int nv = r.ReadInt32(), ni = r.ReadInt32();
                var p = new Poziom { v = new Vector3[nv], n = new Vector3[nv], uv = new Vector2[nv], t = new int[ni] };
                for (int k = 0; k < nv; k++)
                {
                    p.v[k] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                    p.n[k] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                    p.uv[k] = new Vector2(r.ReadSingle(), r.ReadSingle());
                }
                for (int k = 0; k < ni; k++) p.t[k] = r.ReadUInt16();
                lista.Add(p);
            }
        }
        lista.Add(Pierscien(10));
        return poziomy = lista.ToArray();
    }
    /// <summary>The far level: a ring of n sides (its tread, its bore, its two walls), the scan's size.</summary>
    static Poziom Pierscien(int n)
    {
        const float ri = .18f, hw = .075f;
        var v = new List<Vector3>(); var nn = new List<Vector3>(); var t = new List<int>();
        void Pas(float r0, float z0, float r1, float z1, Func<float, Vector3> nrm)
        {
            int b = v.Count;
            for (int k = 0; k <= n; k++)
            {
                float a = k * Mathf.PI * 2f / n, c = Mathf.Cos(a), s = Mathf.Sin(a);
                v.Add(new Vector3(c * r0, s * r0, z0)); nn.Add(nrm(a)); v.Add(new Vector3(c * r1, s * r1, z1)); nn.Add(nrm(a));
            }
            for (int k = 0; k < n; k++) { int i = b + k * 2; t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i + 1); t.Add(i + 2); t.Add(i + 3); }
        }
        Pas(R, hw, R, -hw, a => new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f));             // the tread
        Pas(ri, -hw, ri, hw, a => new Vector3(-Mathf.Cos(a), -Mathf.Sin(a), 0f));          // the bore
        Pas(ri, hw, R, hw, a => Vector3.forward);                                          // the walls
        Pas(R, -hw, ri, -hw, a => Vector3.back);
        return new Poziom { v = v.ToArray(), n = nn.ToArray(), uv = new Vector2[v.Count], t = t.ToArray() };
    }

    // ---------------------------------------------------------------- where they stand (nominal metres)
    public struct Opona { public Vector3 p; public float yaw; }     // upright, its axis across the yaw (towards the track)
    public sealed class Grupa { public string nazwa; public List<Opona> opony = new List<Opona>(); }
    public static List<Grupa> Piramidy = new List<Grupa>(), Sciany = new List<Grupa>();
    public static int Opon, Trojkaty, PiramidSzesc, PiramidTrzy, ScianZakretow, ScianMiedzy;

    /// <summary>The turns: runs of the line bending one way (radius under 60 m, gaps up to 6 m), (first, last, apex)
    /// samples and the angle turned.</summary>
    public static List<(int a, int b, int szczyt, float kat)> Zakrety()
    {
        var o = T.Linia(); int N = o.N; var wynik = new List<(int, int, int, float)>();
        bool Z(int i) => Mathf.Abs(o.kr[i]) > 1f / 60f;
        int s0 = 0; while (s0 < N && Z(s0)) s0++;
        float Ds(int i) => o.s[(i + 1) % N] - o.s[i] < 0f ? o.dlugosc - o.s[i] : o.s[(i + 1) % N] - o.s[i];
        for (int k = 0; k < N; k++)
        {
            int i = (s0 + k) % N; if (!Z(i)) continue;
            int b = k; float sg = Mathf.Sign(o.kr[i]);
            while (true)
            {
                int nast = -1; float dl = 0f;
                for (int m = b + 1; m < N && dl <= 6f; m++) { dl += Ds((s0 + m - 1) % N); if (Z((s0 + m) % N)) { nast = m; break; } }
                if (nast < 0 || Mathf.Sign(o.kr[(s0 + nast) % N]) != sg) break;
                b = nast;
            }
            float kat = 0f; for (int m = k; m <= b; m++) kat += o.kr[(s0 + m) % N] * Ds((s0 + m) % N);
            float pol = 0f; int szczyt = (s0 + k) % N;
            for (int m = k; m <= b; m++) { pol += o.kr[(s0 + m) % N] * Ds((s0 + m) % N); if (Mathf.Abs(pol) >= Mathf.Abs(kat) * .5f) { szczyt = (s0 + m) % N; break; } }
            wynik.Add(((s0 + k) % N, (s0 + b) % N, szczyt, Mathf.Abs(kat) * Mathf.Rad2Deg));
            k = b;
        }
        return wynik;
    }
    static Vector3 Lewo(int i) { var o = T.Linia(); return new Vector3(-o.tz[i], 0f, o.tx[i]); }
    static Vector3 Na(int i, float u) { var o = T.Linia(); return new Vector3(o.x[i] - o.tz[i] * u, 0f, o.z[i] + o.tx[i] * u); }
    /// <summary>How far (x, z) is past the shoulder of the track's nearest section other than the one about sample i (more than
    /// `wzdluz` metres away along the loop; i -1: any).</summary>
    static float OdInnych(float x, float z, int i, float wzdluz)
    {
        var o = T.Linia(); float best = 1e9f;
        for (int j = 0; j < o.N; j += 2)
        {
            if (i >= 0) { float ds = Mathf.Abs(o.s[j] - o.s[i]); ds = Mathf.Min(ds, o.dlugosc - ds); if (ds < wzdluz) continue; }
            float dx = x - o.x[j], dz = z - o.z[j], e = Mathf.Sqrt(dx * dx + dz * dz) - o.w[j] * .5f - T.Pobocze;
            if (e < best) best = e;
        }
        return best;
    }

    static void Rozmiesc()
    {
        var o = T.Linia(); int N = o.N;
        Piramidy.Clear(); Sciany.Clear(); PiramidSzesc = PiramidTrzy = ScianZakretow = ScianMiedzy = 0;
        var zakrety = Zakrety();
        // ---- the apex bundles
        foreach (var (a, b, sz, kat) in zakrety)
        {
            if (kat < MinKatPiramidy) continue;
            bool duza = o.w[sz] >= 6f && kat >= 60f; float sg = Mathf.Sign(o.kr[sz]);
            float u = sg * (o.w[sz] * .5f + T.Pobocze + OdPobocza + Szer * .5f);
            var c = Na(sz, u);
            if (OdInnych(c.x, c.z, sz, 30f) < 1.5f || T.DoPaddocku(c.x, c.z) < 2f) continue;
            float yaw = Mathf.Atan2(o.tx[sz], o.tz[sz]) * Mathf.Rad2Deg;
            var t = new Vector3(o.tx[sz], 0f, o.tz[sz]);
            float y0 = float.MaxValue; foreach (float d in new[] { -R * 2f, 0f, R * 2f }) y0 = Mathf.Min(y0, T.Grunt(c.x + t.x * d, c.z + t.z * d));
            var g = new Grupa { nazwa = "TOR DO KŁADÓW | opony na zakręcie " + (Piramidy.Count + 1) };
            int dol = duza ? 3 : 2;
            for (int rz = 0; rz < dol; rz++)                                   // 3-2-1 / 2-1, each row nested in the one below
                for (int k = 0; k < dol - rz; k++)
                {
                    float d = (k - (dol - rz - 1) * .5f) * R * 2f;
                    g.opony.Add(new Opona { p = new Vector3(c.x + t.x * d, y0 + R - .04f + rz * Wiersz, c.z + t.z * d), yaw = yaw });
                }
            Piramidy.Add(g); if (duza) PiramidSzesc++; else PiramidTrzy++;
        }
        var zajete = new List<Vector3>(); foreach (var g in Piramidy) foreach (var op in g.opony) zajete.Add(op.p);
        // ---- the walls round the fast turns' outsides, and the start's first turn (the whole pack arrives there at once)
        float sBramki = o.s[T.IndeksBramki()]; int pierwszy = -1; float najblizej = float.MaxValue;
        for (int k = 0; k < zakrety.Count; k++)
        {
            float ds = ((o.s[zakrety[k].a] - sBramki) % o.dlugosc + o.dlugosc) % o.dlugosc;
            if (zakrety[k].kat >= MinKatPiramidy && ds < najblizej) { najblizej = ds; pierwszy = k; }
        }
        for (int k = 0; k < zakrety.Count; k++)
        {
            var (a, b, sz, kat) = zakrety[k]; bool start = k == pierwszy;
            if (!start)
            {
                if (kat < MinKatSciany || o.w[sz] < 6f) continue;
                float vIn = 0f;
                for (float d = 0f; d < 60f; d += .5f) vIn = Mathf.Max(vIn, T.VNaLinii(T.Indeks(o.s[a] - d)));
                float vMin = float.MaxValue; for (int m = a; m != (b + 1) % N; m = (m + 1) % N) vMin = Mathf.Min(vMin, T.VZakretu(m));
                if (vIn < MinVWjazdu || vIn - vMin < MinHamowanie) continue;
            }
            float sg = -Mathf.Sign(o.kr[sz]); var linia = new List<Vector3>();
            // from 6 m before the turn to 60 % round it (where a rider who missed his braking goes straight on); the start's
            // turn all round
            float s0 = o.s[a] - 6f, s1 = o.s[b] + 4f, pol = 0f;
            for (int m = a; !start && m != (b + 1) % N; m = (m + 1) % N)
            {
                pol += Mathf.Abs(o.kr[m]) * .5f;
                if (pol * Mathf.Rad2Deg >= kat * .6f) { s1 = o.s[m] + 2f; break; }
            }
            if (s1 < s0) s1 += o.dlugosc;
            for (float s = s0; s <= s1; s += .25f) { int i = T.Indeks(s); linia.Add(Na(i, sg * (o.w[i] * .5f + T.Pobocze + OdPobocza + Szer * .5f))); }
            if (Postaw(linia, "TOR DO KŁADÓW | ściana z opon (zakręt)", i => true, zajete)) ScianZakretow++;
        }
        // ---- the walls between two sections side by side
        var bieg = new List<Vector3>();
        void Zamknij() { if (bieg.Count >= 6 && Postaw(Wygladz(bieg), "TOR DO KŁADÓW | ściana z opon (między odcinkami)", i => true, zajete)) ScianMiedzy++; bieg = new List<Vector3>(); }
        for (int i = 0; i < N; i += 2)
        {
            int jb = -1; float db = 1e9f;
            for (int j = 0; j < N; j += 2)
            {
                float ds = Mathf.Abs(o.s[j] - o.s[i]); ds = Mathf.Min(ds, o.dlugosc - ds); if (ds < MinWzdluz) continue;
                float dx = o.x[j] - o.x[i], dz = o.z[j] - o.z[i], d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < db) { db = d; jb = j; }
            }
            float gap = jb < 0 ? 1e9f : db - o.w[i] * .5f - o.w[jb] * .5f - 2f * T.Pobocze;
            if (jb < i || gap > MaxSzczelina || gap < 1.4f) { Zamknij(); continue; }   // each pair once (from its earlier sample)
            var ci = new Vector3(o.x[i], 0f, o.z[i]); var dj = new Vector3(o.x[jb] - o.x[i], 0f, o.z[jb] - o.z[i]).normalized;
            bieg.Add(ci + dj * (o.w[i] * .5f + T.Pobocze + gap * .5f));
        }
        Zamknij();
    }
    static List<Vector3> Wygladz(List<Vector3> p)
    {
        var w = new List<Vector3>(p.Count);
        for (int i = 0; i < p.Count; i++) { var s = Vector3.zero; int n = 0; for (int k = -2; k <= 2; k++) { int j = i + k; if (j < 0 || j >= p.Count) continue; s += p[j]; n++; } w.Add(s / n); }
        return w;
    }

    /// <summary>A wall along the polyline: the bottom row every 0.6 m, the top row in its gaps; tyres off the other sections and
    /// the apex bundles; pieces of 28 at most. False when nothing stood.</summary>
    static bool Postaw(List<Vector3> linia, string nazwa, Func<int, bool> _, List<Vector3> zajete)
    {
        var dol = new List<(Vector3 p, float yaw)>(); float nast = 0f, s = 0f;
        for (int k = 1; k < linia.Count; k++)
        {
            var a = linia[k - 1]; var b = linia[k]; float l = Vector3.Distance(a, b); if (l < 1e-4f) continue;
            while (nast <= s + l)
            {
                var p = Vector3.Lerp(a, b, (nast - s) / l); var d = (b - a) / l;
                dol.Add((p, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg)); nast += R * 2f;
            }
            s += l;
        }
        bool Wolne(Vector3 p)
        {
            if (OdInnych(p.x, p.z, -1, 0f) < .45f || T.DoPaddocku(p.x, p.z) < 1f) return false;
            foreach (var z in zajete) if ((z.x - p.x) * (z.x - p.x) + (z.z - p.z) * (z.z - p.z) < 1.2f * 1.2f) return false;
            return true;
        }
        var opony = new List<(Opona o, bool dolna, int k)>();
        for (int k = 0; k < dol.Count; k++)
        {
            var (p, yaw) = dol[k]; if (!Wolne(p)) { dol[k] = (new Vector3(float.NaN, 0f, 0f), yaw); continue; }
            opony.Add((new Opona { p = new Vector3(p.x, T.Grunt(p.x, p.z) + R - .04f, p.z), yaw = yaw }, true, k));
        }
        var gora = new List<Opona>();
        for (int k = 0; k + 1 < dol.Count; k++)
        {
            if (float.IsNaN(dol[k].p.x) || float.IsNaN(dol[k + 1].p.x)) continue;
            var p = (dol[k].p + dol[k + 1].p) * .5f;
            float y = (T.Grunt(dol[k].p.x, dol[k].p.z) + T.Grunt(dol[k + 1].p.x, dol[k + 1].p.z)) * .5f + R - .04f + Wiersz;
            gora.Add(new Opona { p = new Vector3(p.x, y, p.z), yaw = Mathf.LerpAngle(dol[k].yaw, dol[k + 1].yaw, .5f) });
        }
        if (opony.Count < 3) return false;
        // in pieces along the wall (each its own levels); a box per 5 bottom tyres
        var wszystkie = new List<Opona>(); int gi = 0;
        foreach (var t in opony) { wszystkie.Add(t.o); if (gi < gora.Count) wszystkie.Add(gora[gi++]); }
        for (int k = 0; k < wszystkie.Count; k += (int)NaGrupe)
        {
            var g = new Grupa { nazwa = nazwa + " " + (Sciany.Count + 1) };
            for (int m = k; m < Mathf.Min(wszystkie.Count, k + (int)NaGrupe); m++) g.opony.Add(wszystkie[m]);
            Sciany.Add(g);
        }
        for (int k = 0; k < opony.Count; k += 5)
        {
            int e = Mathf.Min(opony.Count - 1, k + 5); var a = opony[k].o.p; var b = opony[e].o.p;
            if (opony[e].k - opony[k].k != e - k) { e = k; b = a; }                    // a gap in the row: this tyre alone
            var mid = (a + b) * .5f; float l = new Vector2(b.x - a.x, b.z - a.z).magnitude + R * 2f;
            float yaw = l > R * 2f + .01f ? Mathf.Atan2(b.x - a.x, b.z - a.z) * Mathf.Rad2Deg : opony[k].o.yaw;
            W.Kolizja(new Vector3(mid.x, mid.y - R + (R * 2f + Wiersz) * .5f, mid.z) * K, new Vector3(Szer + .1f, R * 2f + Wiersz, l) * K, Quaternion.Euler(0f, yaw, 0f));
        }
        foreach (var t in opony) zajete.Add(t.o.p);
        return true;
    }

    // ---------------------------------------------------------------- the build
    public static void Zbuduj(Transform g)
    {
        Opon = Trojkaty = 0;
        var lv = Poziomy();
        if (lv == null) { Debug.LogWarning("DO POPRAWKI MISTRZOWIE_TOR_KLADOW_OPONY no tyre " + Dane + " (unity-tools/mistrzowie/opony): no tyres"); return; }
        var opona = BraxChampionsFoundation.SourceMaterial("tor_kladow_opona", "old_tyre", 1f, Color.white);   // the scan's own 4K maps, its own UVs
        var daleka = BraxChampionsHarbour.Surface("tor_kladow_opona_daleka", new Color(.125f, .113f, .1f), .85f, 0f);   // its mean colour
        Directory.CreateDirectory(Folder);
        Rozmiesc();
        var grupa = new GameObject("TOR DO KŁADÓW | opony (Poly Haven Old Tyre)").transform; grupa.SetParent(g, false);
        int nr = 0;
        foreach (var gr in Piramidy) Grupuj(grupa, gr, lv, PoziomyPiramidy, ProgiPiramidy, opona, daleka, nr++);
        foreach (var gr in Sciany) Grupuj(grupa, gr, lv, PoziomySciany, ProgiSciany, opona, daleka, nr++);
        foreach (var gr in Piramidy)
        {
            // the bundle's collider: its row's length, its height
            var a = gr.opony[0]; int dolnych = gr.opony.Count == 6 ? 3 : 2; var b = gr.opony[dolnych - 1];
            var mid = (a.p + b.p) * .5f; float h = R * 2f + Wiersz * (dolnych - 1);
            W.Kolizja(new Vector3(mid.x, mid.y - R + h * .5f, mid.z) * K, new Vector3(Szer + .1f, h, dolnych * R * 2f) * K, Quaternion.Euler(0f, a.yaw, 0f));
        }
        Tasma(g);
        Debug.Log("MISTRZOWIE_TOR_KLADOW_OPONY tyres=" + Opon + " apex6=" + PiramidSzesc + " apex3=" + PiramidTrzy + " walls_turns=" + ScianZakretow +
                  " walls_between=" + ScianMiedzy + " groups=" + nr + " triangles_lod0=" + Trojkaty + " (Poly Haven old_tyre; owner 24.11 17:11)");
    }

    static void Grupuj(Transform rodzic, Grupa gr, Poziom[] lv, int[] ktore, float[] progi, Material opona, Material daleka, int nr)
    {
        var go = new GameObject(gr.nazwa); go.transform.SetParent(rodzic, false);
        var lods = new LOD[ktore.Length];
        for (int l = 0; l < ktore.Length; l++)
        {
            var p = lv[ktore[l]]; bool gruba = ktore[l] < 2; var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            foreach (var op in gr.opony)
            {
                var q = Quaternion.Euler(0f, op.yaw + 90f, 0f); int b = v.Count;        // its axis (local z) across the row
                for (int k = 0; k < p.v.Length; k++) { v.Add((op.p + q * p.v[k]) * K); n.Add(q * p.n[k]); uv.Add(p.uv[k]); }
                foreach (int k in p.t) t.Add(b + k);
            }
            string plik = "tor_kladow_opony_" + nr + "_lod" + l;
            var mesh = new Mesh { name = plik, indexFormat = v.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            string path = Folder + "/" + plik + ".asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path)) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            var c = new GameObject(gr.nazwa + " LOD" + l); c.transform.SetParent(go.transform, false);
            c.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = c.AddComponent<MeshRenderer>(); mr.sharedMaterial = gruba ? opona : daleka;
            mr.shadowCastingMode = gruba ? ShadowCastingMode.On : ShadowCastingMode.Off; mr.receiveShadows = true;
            GameObjectUtility.SetStaticEditorFlags(c, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            lods[l] = new LOD(progi[l], new Renderer[] { mr });
            if (l == 0) Trojkaty += t.Count / 3;
        }
        var lg = go.AddComponent<LODGroup>();
        lg.SetLODs(lods);
        lg.RecalculateBounds();
        Opon += gr.opony.Count;
    }

    // ---------------------------------------------------------------- the tape between the field's stakes
    public static int Tasm;
    static void Tasma(Transform g)
    {
        var mat = TasmaMaterial(); if (!mat) return;
        var b = new BraxChampionsMeshBatch(); Tasm = 0;
        foreach (var rzad in T.Paliki)
            for (int k = 0; k + 1 < rzad.Count; k++)
            {
                var a = rzad[k]; var c = rzad[k + 1];
                if (float.IsNaN(a.x) || float.IsNaN(c.x)) continue;
                float l = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(c.x, c.z)); if (l > 12f || l < 1f) continue;
                const int kawalki = 4;
                for (int m = 0; m < kawalki; m++)
                {
                    float t0 = m / (float)kawalki, t1 = (m + 1) / (float)kawalki;
                    Vector3 P(float t) { var p = Vector3.Lerp(a, c, t); p.y += TasmaH - TasmaZwis * 4f * t * (1f - t); return p; }
                    var p0 = P(t0); var p1 = P(t1); var up = Vector3.up * (TasmaPas * .5f);
                    b.QuadUV(mat, p0 - up, p0 + up, p1 + up, p1 - up, new Vector2(t0 * l / .5f, 0f), new Vector2(t0 * l / .5f, 1f), new Vector2(t1 * l / .5f, 1f), new Vector2(t1 * l / .5f, 0f));
                }
                Tasm++;
            }
        b.Build(g, "TOR DO KŁADÓW | taśma między tyczkami", Folder, false, K);
        Trojkaty += b.Triangles;
    }
    /// <summary>The tape's picture: 0.5 m of it (two stripes, red and white), its 6 cm band in the middle of a 24 cm quad, the
    /// rest transparent with the band's colour under it (no dark fringe in its mipmaps).</summary>
    static Material TasmaMaterial()
    {
        const int w = 64, h = 32; var px = new Color32[w * h];
        var czerwona = new Color32(206, 28, 26, 255); var biala = new Color32(238, 238, 232, 255);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var c = x < w / 2 ? czerwona : biala;
                c.a = (byte)(y >= 12 && y < 20 ? 255 : 0);
                px[y * w + x] = c;
            }
        Directory.CreateDirectory(Out + "/Textures");
        string path = Out + "/Textures/tor_kladow_tasma.png";
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false); tex.SetPixels32(px); tex.Apply(false);
        File.WriteAllBytes(path, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(path) is TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.alphaSource = TextureImporterAlphaSource.FromInput;
            imp.alphaIsTransparency = false; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Repeat;   // along the tape; across it both edges are clear
            imp.filterMode = FilterMode.Trilinear; imp.anisoLevel = 4; imp.maxTextureSize = 64;
            var a = imp.GetPlatformTextureSettings("Android"); a.overridden = true; a.maxTextureSize = 64; a.format = TextureImporterFormat.ASTC_4x4;
            imp.SetPlatformTextureSettings(a); imp.SaveAndReimport();
        }
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (!t) return null;
        var m = BraxChampionsHarbour.Surface("tor_kladow_tasma", Color.white, .55f, 0f, null, true);
        m.mainTexture = t;
        return m;
    }
}
