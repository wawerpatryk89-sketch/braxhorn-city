using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// DWORZEC R1 — LAS MIĘDZY MIASTEM A FAWELĄ (11.11.2026). Właściciel 14:49: „Parka nie chcę, las jakiś. Ale drzewa też, żeby były
// inne, bo mamy tylko jeden model drzew”; 15:35: „od strony fawel jeszcze lekko”; 15:53: rośliny wyłącznie naturalne, Poly Haven.
// Gatunki (unity-tools/claude/drzewa_las.py, Poly Haven CC0): jodła ×3 (fir_tree_01 a/b/c), sosna ×3 (pine_tree_01 a/b/c), młode
// drzewo liściaste (tree_small_02), kłody (dead_tree_trunk_02); podszyt: krzewy shrub_01–04, paproć fern_02, pnie tree_stump_01/02.
// Gęsto od strony miasta (siatka 4.6 m, 92 %), lekko od strony faweli (36 %), polany przy drogach, torach, placu i budynkach
// (obszary Zajete). Drzewa bez kolizji korony — kolizja tylko pnia. Brak modeli gatunków = wpis DO POPRAWKI w logu (bez zastępstw).
internal static partial class BraxMiastoDworzec
{
    static readonly string[] Jodly = { "las_jodla_a", "las_jodla_b", "las_jodla_c" };
    static readonly string[] Sosny = { "las_sosna_a", "las_sosna_b", "las_sosna_c" };
    static readonly string[] Lisciaste = { "las_drzewko" };

    /// <summary>Gęstość lasu 0…1 w pasie dworca: 0 na drogach, placach, peronach, w przekopie i przy budynkach.</summary>
    public static float GestoscLasu(float x, float z)
    {
        if (x < BraxMiastoGranice.FawelaX0 + 2f || x > BraxMiastoGranice.FawelaX1 - 2f) return 0f;
        float zTop = BraxMiastoGranice.FawelaKrawedz(x), zBot = BraxRegionFawela.KrawedzSwiat(x);
        if (z > zTop - 2.5f || z < zBot + 3.5f) return 0f;
        foreach (var r in Zajete) if (x > r.xMin - 1.2f && x < r.xMax + 1.2f && z > r.yMin - 1.2f && z < r.yMax + 1.2f) return 0f;
        foreach (var (c, rr) in ZajeteKola) { float dx = x - c.x, dz = z - c.y; if (dx * dx + dz * dz < (rr + 1.2f) * (rr + 1.2f)) return 0f; }
        if (x < PortalX - 2f) return 1f;                                   // wzgórze portalu zachodniego — gęsto
        if (z > T1 + 6f) return 1f;                                        // od miasta — gęsto (także wzgórze nad tunelem osiedla)
        return .36f * Mathf.Lerp(.7f, 1f, Mathf.Clamp01((z - zBot) / 20f));  // od faweli — lekko, przy samej fawelii rzadziej
    }

    static GameObject[] Wczytaj(string[] ids) => ids.Select(i => BraxRegion.Model(i)).Where(m => m).ToArray();

    static void Las()
    {
        var jodly = Wczytaj(Jodly); var sosny = Wczytaj(Sosny); var lisc = Wczytaj(Lisciaste);
        var kloda = BraxRegion.Model("las_kloda");
        var krzewy = new[] { "shrub_01", "shrub_02", "shrub_03", "shrub_04" }.Select(i => BraxRegion.Model(i)).Where(m => m).ToArray();
        var paproc = BraxRegion.Model("fern_02"); var pniaki = new[] { "tree_stump_01", "tree_stump_02" }.Select(i => BraxRegion.Model(i)).Where(m => m).ToArray();
        if (jodly.Length + sosny.Length + lisc.Length == 0)
        {
            Debug.LogWarning("DO POPRAWKI: BRAXHORN_DWORZEC_R1 las — brak modeli gatunków (Library/las_*.glb, unity-tools/claude/drzewa_las.py)");
            Opis("las: BRAK MODELI GATUNKÓW"); return;
        }
        var las = new GameObject("Dworzec: las (gatunki Poly Haven)").transform; las.SetParent(root, false);
        var rnd = new System.Random(20261111);
        float R() => (float)rnd.NextDouble();
        int drzew = 0, podszytu = 0, klod = 0; var licz = new Dictionary<string, int>();
        const float krok = 4.6f;
        for (float x = BraxMiastoGranice.FawelaX0 + 2.5f; x < BraxMiastoGranice.FawelaX1 - 2f; x += krok)
            for (float z = -68f - 2f; z > BraxRegionFawela.KrawedzSwiat(-100f); z -= krok)
            {
                float px = x + (R() - .5f) * krok * .7f, pz = z + (R() - .5f) * krok * .7f;
                float g = GestoscLasu(px, pz); if (g <= 0f) continue;
                float y = WysKorytarza(px, pz);
                if (R() < g * .92f)
                {
                    bool odMiasta = pz > T1 + 6f || px < PortalX - 2f;
                    float u = R(); GameObject[] grupa;
                    if (odMiasta) grupa = u < .38f ? jodly : u < .72f ? sosny : lisc; else grupa = u < .2f ? jodly : u < .55f ? sosny : lisc;
                    if (grupa.Length == 0) grupa = jodly.Length > 0 ? jodly : sosny.Length > 0 ? sosny : lisc;
                    var src = grupa[rnd.Next(grupa.Length)];
                    float sk = .82f + R() * .36f;
                    var o = BraxRegion.Put(las, src, "Las: " + src.name, V(px, y - .05f, pz), R() * 360f, sk, false, 0f, .05f);
                    if (o) { drzew++; licz[src.name] = licz.TryGetValue(src.name, out var c) ? c + 1 : 1; BraxRegionMeble.Kolizja(las, "Las: pień", V(px, y + 1.6f, pz), new Vector3(.55f, 3.2f, .55f)); }
                }
                // podszyt: krzewy, paproć, pniaki, kłody
                float v = R();
                if (krzewy.Length > 0 && v < .3f * g + .08f) { var k = krzewy[rnd.Next(krzewy.Length)]; if (BraxRegion.Put(las, k, "Las: krzew", V(px + 1.6f, WysKorytarza(px + 1.6f, pz + .8f) - .03f, pz + .8f), R() * 360f, .8f + R() * .5f, false, 0f, .03f)) podszytu++; }
                else if (paproc && v < .5f * g + .08f && g > .5f) { if (BraxRegion.Put(las, paproc, "Las: paproć", V(px - 1.3f, WysKorytarza(px - 1.3f, pz - 1f) - .02f, pz - 1f), R() * 360f, .9f + R() * .5f, false, 0f, .02f)) podszytu++; }
                else if (pniaki.Length > 0 && v < .53f * g + .08f) { if (BraxRegion.Put(las, pniaki[rnd.Next(pniaki.Length)], "Las: pniak", V(px + 1f, WysKorytarza(px + 1f, pz - 1.5f) - .05f, pz - 1.5f), R() * 360f, 1f, false, 0f, .05f)) podszytu++; }
                else if (kloda && v > .985f && g > .5f) { if (BraxRegion.Put(las, kloda, "Las: kłoda", V(px, WysKorytarza(px, pz) - .1f, pz), R() * 360f, 1f, true, 0f, .1f)) klod++; }
            }
        Opis("las: drzew " + drzew + " (" + string.Join(", ", licz.Select(kv => kv.Key + " " + kv.Value)) + "), podszytu " + podszytu + ", kłód " + klod);
    }

    // ============================================================================ KONTROLA SKALI (zasada globalna 11.11 — przed oddaniem)
    static void KontrolaSkali()
    {
        float k = K;
        float busDl = 12f * k, busSz = 2.55f * k, rZew = 12.5f * k, rWew = 5.3f * k;
        var l = new List<string>();
        void Sprawdz(string co, bool ok, string ile) { l.Add((ok ? "OK " : "DO POPRAWKI ") + co + ": " + ile); if (!ok) Debug.LogWarning("DO POPRAWKI: BRAXHORN_DWORZEC_R1 skala — " + co + " " + ile); }
        Sprawdz("pętla — okrąg wpisany ≥ promień zewn. autobusu", PetlaR >= rZew, PetlaR.ToString("F2") + " ≥ " + rZew.ToString("F2"));
        Sprawdz("pętla — wyspa ≤ promień wewn. autobusu", PetlaWyspa <= rWew, PetlaWyspa.ToString("F2") + " ≤ " + rWew.ToString("F2"));
        Sprawdz("łuki ul. Dworcowej — krawędź zewn. ≥ promień zewn. autobusu", RL + Pas >= rZew, (RL + Pas).ToString("F2") + " ≥ " + rZew.ToString("F2"));
        Sprawdz("łuki ul. Dworcowej — krawędź wewn. ≤ promień wewn. przy R zewn. = krawędź", RL - Pas <= (RL + Pas) - (rZew - rWew) * .92f, (RL - Pas).ToString("F2"));
        Sprawdz("pas ruchu = 3.5 m × K", Mathf.Abs(Pas - 3.5f * k) < .02f, Pas.ToString("F2"));
        Sprawdz("stanowisko BUS ≥ autobus + 3 m", StanowiskoDl >= busDl + 2.9f, StanowiskoDl + " ≥ " + (busDl + 2.9f).ToString("F2"));
        Sprawdz("zatoka BUS = wjazd 4 + 2 stanowiska + wyjazd 4", BusX1 - BusX0 >= 8f + 2f * StanowiskoDl, (BusX1 - BusX0).ToString("F1"));
        Sprawdz("zatoka szer. ≥ autobus + 1 m", ZatokaSz >= busSz + 1f, ZatokaSz.ToString("F2"));
        Sprawdz("peron ≥ skład pociągu", PX1 - PX0 >= SkladDl, (PX1 - PX0) + " ≥ " + SkladDl);
        Sprawdz("krawędź peronu 1 od osi toru 1", Mathf.Abs((P1Kraw - T1) - KrawedzOdOsi) < .001f, (P1Kraw - T1).ToString("F3"));
        Sprawdz("krawędź peronu 2 od osi toru 2", Mathf.Abs((T2 - P2Kraw) - KrawedzOdOsi) < .001f, (T2 - P2Kraw).ToString("F3"));
        Sprawdz("peron nad główką szyny", Mathf.Abs(YP - GlowkaY - PeronNadGlowka) < .001f, (YP - GlowkaY).ToString("F3"));
        Sprawdz("prześwit wagon–krawędź peronu", KrawedzOdOsi - SzerWagonu * .5f > .25f, (KrawedzOdOsi - SzerWagonu * .5f).ToString("F3"));
        Sprawdz("spód kładki nad przewodem nośnym", KladkaSpod > GlowkaY + 7.5f + .05f, KladkaSpod.ToString("F2"));
        Sprawdz("stopień schodów 0.16 m × K", Mathf.Abs((KladkaY - YP) / 38f - .16f * k) < .02f, ((KladkaY - YP) / 38f).ToString("F3"));
        Sprawdz("drzwi ≥ 2.0 m × K", 2.92f >= 2.0f * k, "2.92");
        Sprawdz("winda — kabina 1.6 × 1.4 m × K", KabW >= 1.6f * k - .01f && KabD >= 1.4f * k - .01f, KabW.ToString("F2") + " × " + KabD.ToString("F2"));
        Sprawdz("winda — drzwi ≥ 0.9 m × K", WDrzW >= .9f * k, WDrzW.ToString("F2"));
        Sprawdz("WC — przejście ≥ 0.9 m × K", WcPrzejscia[0].b - WcPrzejscia[0].a >= .9f * k && WcPrzejscia[1].b - WcPrzejscia[1].a >= .9f * k, (WcPrzejscia[0].b - WcPrzejscia[0].a).ToString("F2"));
        Opis("kontrola skali: " + string.Join("; ", l));
    }
}
