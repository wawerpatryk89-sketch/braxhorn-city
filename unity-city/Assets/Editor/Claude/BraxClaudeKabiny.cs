using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// KABINA AUTA (build 45, właściciel 2.11 20:28: „każdy człowiek powinien się zmieścić do auta”). Ludzie i auta są w tej
// samej skali (×K) — człowiek ~1.80 m mieści się w każdym normalnym aucie; w sportowym dzięki niżej osadzonemu siedzisku
// i odchylonemu oparciu. Skala osoby = 1.0 ZAWSZE (build 44 zmniejszał do 0.904 — zakaz), bez dobierania niższych osób
// i bez zostawiania pustych aut z tego powodu. Korekta 21:15: kto mieści się w pozie z #44 — bez żadnej zmiany. Dalej
// (tylko przy braku miejsca) kolejność — najmniejsza zmiana pozy pierwsza:
//   1) miednica osadzona w siedzisku: zagłębienie poduszki 3 → 4.5 → 6 cm × K,
//   2) odchylenie tułowia (od Bip01 Spine) od bazowego dla kabiny do maksymalnego co 1° (sportowe 25–30°, osobowe 18–22°,
//      dostawcze 8–12° — BraxFotelAuta.Rodzaj z realnej wysokości auta),
//   3) głowa pochylona dodatkowo 5 → 10° (do wyrównania wzroku 0.6 × odchylenia zawsze).
// Głowa = wierzchołki skóry głowy (włosy, czapka) liczone z kości w każdej z 8 klatek każdego klipu siedzenia osoby (CPU,
// jak BakeMesh, ale w świecie — bez podwójnej skali), dach = mapa dachu nad TYM miejscem w punkcie każdego wierzchołka.
// Gdy przy maksymalnym odchyleniu nadal brakuje miejsca — LogError „DO POPRAWKI … brakuje X cm” (to znaczy: kabina źle
// zmierzona albo zła skala modelu auta), osoba zostaje w aucie w pozie maksymalnej (bez zmniejszania).
public static partial class BraxClaudeOsiedle
{
    public struct WynikKabiny { public float odchylenie, zaglebienie, pochylenie, glowa, dach, zapas; }
    static int kabMiejsc, kabZapasUjemny, kabOdchylenieMax; static float kabZapasMin = float.PositiveInfinity;
    static readonly Dictionary<string, float> kabBrakAuta = new Dictionary<string, float>();
    static void KabinyReset() { kabMiejsc = 0; kabZapasUjemny = 0; kabOdchylenieMax = 0; kabZapasMin = float.PositiveInfinity; kabBrakAuta.Clear(); }

    public const float KabinaWzrok = .6f;   // część odchylenia oddana głowie (wzrok poziomo)

    // Kolejne próby pozy: (odchylenie °, zagłębienie m świata, dodatkowe pochylenie głowy °).
    public static List<(float odch, float zagl, float poch)> KabinaProby(float baza, float max, float k)
    {
        // właściciel 21:15: „ludzie już dobrze siedzą w samochodach” — najpierw poza z #44 bez zmian (0°, bez zagłębienia);
        // odchylenie i zagłębienie dopiero, gdy głowa nie mieści się pod dachem
        var l = new List<(float, float, float)> { (0f, 0f, 0f) };
        foreach (float z in new[] { .03f, .045f, .06f }) l.Add((baza, z * k, 0f));
        for (float o = baza + 1f; o <= max + .01f; o += 1f) l.Add((o, .06f * k, 0f));
        foreach (float p in new[] { 5f, 10f }) l.Add((max, .06f * k, p));
        return l;
    }

    // Skóra głowy: wierzchołki, których największa waga należy do kości głowy albo jej potomków (włosy, kucyk, oczy).
    sealed class GlowaSkory { public SkinnedMeshRenderer r; public Vector3[] v; public BoneWeight[] w; public Matrix4x4[] bind; }
    static readonly Dictionary<Mesh, (Vector3[] v, BoneWeight[] w)> kabCache = new Dictionary<Mesh, (Vector3[], BoneWeight[])>();

    static List<GlowaSkory> GlowaWierzcholki(IEnumerable<SkinnedMeshRenderer> smrs, Transform glowa)
    {
        var res = new List<GlowaSkory>();
        if (!glowa) return res;
        foreach (var r in smrs)
        {
            var mesh = r.sharedMesh; if (!mesh) continue;
            var bones = r.bones; var bind = mesh.bindposes;
            if (bones == null || bind == null || bones.Length != bind.Length) continue;
            var glowowe = new bool[bones.Length];
            for (int i = 0; i < bones.Length; i++) glowowe[i] = bones[i] && (bones[i] == glowa || bones[i].IsChildOf(glowa));
            if (!glowowe.Any(x => x)) continue;
            if (!kabCache.TryGetValue(mesh, out var d)) { try { d = (mesh.vertices, mesh.boneWeights); } catch { continue; } kabCache[mesh] = d; }
            var vv = new List<Vector3>(); var ww = new List<BoneWeight>();
            for (int i = 0; i < d.v.Length && i < d.w.Length; i++)
            {
                var bw = d.w[i]; int top = bw.boneIndex0; float wt = bw.weight0;
                if (bw.weight1 > wt) { top = bw.boneIndex1; wt = bw.weight1; }
                if (bw.weight2 > wt) { top = bw.boneIndex2; wt = bw.weight2; }
                if (bw.weight3 > wt) { top = bw.boneIndex3; }
                if (top >= 0 && top < glowowe.Length && glowowe[top]) { vv.Add(d.v[i]); ww.Add(bw); }
            }
            if (vv.Count > 0) res.Add(new GlowaSkory { r = r, v = vv.ToArray(), w = ww.ToArray(), bind = bind });
        }
        return res;
    }

    // Najmniejszy zapas (dach − czubek) w bieżącej pozie: skinning wierzchołków głowy na CPU, układ fotela.
    static void ZapasPozy(List<GlowaSkory> skora, IEnumerable<Renderer> akcesoria, Transform fotel, BraxFotelAuta kab, ref WynikKabiny w)
    {
        Vector3 o = fotel.position, R = fotel.right, U = fotel.up, F = fotel.forward;
        var wl = w;
        void Punkt(Vector3 p)
        {
            var d = p - o; float x = Vector3.Dot(d, R), y = Vector3.Dot(d, U), z = Vector3.Dot(d, F);
            float dach = kab.Dach(x, z), zap = dach - y;
            if (zap < wl.zapas) { wl.zapas = zap; wl.glowa = y; wl.dach = dach; }
        }
        foreach (var s in skora)
        {
            var bones = s.r.bones; var M = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++) M[i] = bones[i] ? bones[i].localToWorldMatrix * s.bind[i] : Matrix4x4.zero;
            for (int i = 0; i < s.v.Length; i++)
            {
                var bw = s.w[i]; var v = s.v[i];
                var p = M[bw.boneIndex0].MultiplyPoint3x4(v) * bw.weight0 + M[bw.boneIndex1].MultiplyPoint3x4(v) * bw.weight1
                      + M[bw.boneIndex2].MultiplyPoint3x4(v) * bw.weight2 + M[bw.boneIndex3].MultiplyPoint3x4(v) * bw.weight3;
                Punkt(p);
            }
        }
        foreach (var a in akcesoria)   // czapka, kask, okulary na kości głowy — narożniki obrysu w świecie (ostrożnie)
        {
            if (!a || !a.enabled) continue;
            var b = a.bounds;
            for (int i = 0; i < 8; i++) Punkt(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
        }
        w = wl;
    }

    static WynikKabiny ZmierzKabine(BraxMieszkaniec osoba, Transform fotel, BraxFotelAuta kab, Transform kreg, Transform glowa,
                                    List<GlowaSkory> skora, Renderer[] akcesoria, float odch, float poch)
    {
        var w = new WynikKabiny { zapas = float.PositiveInfinity, odchylenie = odch, pochylenie = poch };
        int klatki = 0;
        foreach (string name in osoba.klipy)
        {
            var clip = osoba.anim.GetClip(name); if (!clip) continue;
            for (int frame = 0; frame < 8; frame++)
            {
                clip.SampleAnimation(osoba.anim.gameObject, clip.length * frame / 8f);
                BraxMieszkaniec.PozaWAucie(kreg, glowa, osoba.transform.right, odch, KabinaWzrok * odch + poch);
                ZapasPozy(skora, akcesoria, fotel, kab, ref w); klatki++;
            }
        }
        if (klatki == 0) w.zapas = 0f;
        return w;
    }

    static void DopasujKabine(BraxMieszkaniec osoba, Transform fotel)
    {
        var kab = fotel.GetComponent<BraxFotelAuta>();
        if (!kab || !osoba.anim) return;
        var kosci = osoba.anim.GetComponentsInChildren<Transform>(true);
        Transform T(string n) => kosci.FirstOrDefault(t => t.name == n);
        var kreg = T("Bip01 Spine") ?? osoba.kregoslup; var glowa = osoba.glowa ? osoba.glowa : T("Bip01 Head");
        var skora = GlowaWierzcholki(osoba.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => r.sharedMesh), glowa);
        var akcesoria = glowa ? glowa.GetComponentsInChildren<Renderer>(true).Where(r => !(r is SkinnedMeshRenderer)).ToArray() : new Renderer[0];
        var start = osoba.transform.position;
        string nazwaAuta = "?";
        for (var t = fotel; t; t = t.parent) if (t.name.StartsWith("Auto")) { nazwaAuta = t.name; break; }
        WynikKabiny wynik = default; bool miesci = false;
        foreach (var (odch, zagl, poch) in KabinaProby(kab.odchylenieBaza, kab.odchylenieMax, K))
        {
            osoba.transform.position = start - fotel.up * zagl;
            wynik = ZmierzKabine(osoba, fotel, kab, kreg, glowa, skora, akcesoria, odch, poch);
            wynik.zaglebienie = zagl;
            if (wynik.zapas >= 0f) { miesci = true; break; }
        }
        // wynik = ostatnia (maksymalna) próba, gdy żadna się nie zmieściła — osoba zostaje, skala 1.0
        osoba.transform.position = start - fotel.up * wynik.zaglebienie;
        osoba.kregoslupDol = kreg; osoba.odchylenie = wynik.odchylenie; osoba.pochylenieGlowy = KabinaWzrok * wynik.odchylenie + wynik.pochylenie;
        // poza zapisana w scenie (daleko od kamery animacja jest zamrożona): pierwszy klip, klatka 0 + odchylenie
        var first = osoba.anim.GetClip(osoba.klipy[0]); if (first) first.SampleAnimation(osoba.anim.gameObject, 0);
        BraxMieszkaniec.PozaWAucie(kreg, glowa, osoba.transform.right, osoba.odchylenie, osoba.pochylenieGlowy);
        var lod = osoba.GetComponent<LODGroup>(); if (lod) lod.RecalculateBounds();
        kabMiejsc++; kabZapasMin = Mathf.Min(kabZapasMin, wynik.zapas);
        if (wynik.odchylenie >= kab.odchylenieMax - .01f) kabOdchylenieMax++;
        string opis = "BRAXHORN_KABINA " + nazwaAuta + " / " + fotel.name.Replace("BRAXHORN ", "") + " osoba=" + osoba.name + " kabina=" + kab.kabina
            + " odchylenie=" + wynik.odchylenie.ToString("F0") + "° zagłębienie=" + wynik.zaglebienie.ToString("F3") + " pochylenie_głowy=" + osoba.pochylenieGlowy.ToString("F0")
            + "° głowa=" + wynik.glowa.ToString("F3") + " dach=" + wynik.dach.ToString("F3") + " zapas=" + wynik.zapas.ToString("F3") + " skala=1.000"
            + " (wierzchołki głowy " + skora.Sum(s => s.v.Length) + (skora.Count == 0 ? " — BRAK, pomiar niemożliwy" : "") + ")";
        if (miesci) Debug.Log(opis);
        else
        {
            kabZapasUjemny++;
            string model = nazwaAuta;
            kabBrakAuta[model] = Mathf.Max(kabBrakAuta.TryGetValue(model, out var b) ? b : 0f, -wynik.zapas);
            Debug.LogError(opis + " — DO POPRAWKI: model auta " + model + " (" + kab.kabina + ", prześwit nad biodrem " + kab.przeswit.ToString("F2")
                + ") brakuje " + (-wynik.zapas * 100f).ToString("F1") + " cm przy maks. odchyleniu " + kab.odchylenieMax.ToString("F0") + "° — kabina źle zmierzona albo zła skala modelu auta");
        }
    }

    static string KabinyPodsumowanie()
    {
        string s = "BRAXHORN_KABINA podsumowanie miejsc=" + kabMiejsc + " zapas_min=" + (kabMiejsc > 0 ? kabZapasMin.ToString("F3") : "—")
            + " brak_miejsca=" + kabZapasUjemny + " na_maks_odchyleniu=" + kabOdchylenieMax + " skala_osób=1.000 (bez zmniejszania, bez pustych aut)"
            + (kabBrakAuta.Count > 0 ? " brakuje: " + string.Join(", ", kabBrakAuta.Select(kv => kv.Key + " " + (kv.Value * 100f).ToString("F1") + " cm")) : "");
        Debug.Log(s);
        return s;
    }
}
