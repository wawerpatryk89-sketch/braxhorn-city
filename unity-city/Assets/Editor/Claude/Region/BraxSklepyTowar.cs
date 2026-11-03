using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// TOWAR W SKLEPACH ULICY (właściciel 3.11 13:27: „wyposaż miasto w te przybory, piekarnie…”; zasada: żadnych klocków).
// W lokalach spożywczych zamiast kolorowych prostopadłościanów na półkach stoją prawdziwe modele:
//   ph:<id> — Poly Haven (CC0, pobiera fetch_region.py; skala realna × K),
//   sk:<plik> — paczka właściciela miasto_sklepy (CC-BY, zaszyfrowana; sklepy_przetworz.py: prawdziwe metry, podstawa y = 0).
// „*n” = n sztuk obok siebie (np. kartony mleka). Brak modelu = luka na półce + wpis w logu (nigdy klocek zastępczy).
// Towar bez cieni, statyczny, cały lokal w jednej grupie LOD znikającej dalej niż ~25 m (wydajność telefonu).
public static class BraxSklepyTowar
{
    static float K => BraxClaudeOsiedle.K;
    public static int Postawione, Braki;
    static readonly HashSet<string> brakiId = new HashSet<string>();
    static readonly Dictionary<Transform, int> naLokal = new Dictionary<Transform, int>();

    static string[] Polki(string typ) => typ switch
    {
        "piekarnia" => new[] { "ph:wicker_basket_01", "ph:hamburger_buns", "ph:croissant*3", "ph:wicker_basket_02", "ph:hamburger_buns" },
        "cukiernia" => new[] { "ph:carrot_cake", "ph:strawberry_chocolate_cake", "ph:croissant*2" },
        "warzywniak" => new[] { "sk:owoce_skrzynka", "ph:bananas", "sk:owoce_skrzynka", "sk:owoce_skrzynka", "ph:bananas" },
        "kawiarnia" => new[] { "ph:tea_set_01", "ph:jug_01", "ph:wooden_bowl_01", "ph:ceramic_vase_01" },
        "pizzeria" or "bar" or "bistro" => new[] { "ph:wine_bottles_01", "ph:jug_01", "ph:wine_bottles_01", "ph:russian_food_cans_01" },
        // towar z etykiet właściciela (3.11 14:17, towar_gen.py): alejki supermarketu sm_g0…8, odżywki BRAXHORN, delikatesy
        "sm_g0" => new[] { "sk:pud_b01*3", "sk:pud_d17*3", "sk:pud_d03*3", "sk:pud_d01*3", "sk:pud_b08*3", "sk:pud_b24*3", "sk:pud_d24*3", "sk:pud_a24*3", "sk:pud_d04*3", "sk:pud_d08*3", "sk:pud_d02*3", "sk:pud_b02*3", "sk:pud_a01*3", "sk:pud_b03*3", "sk:pud_a17*3", "sk:pud_a04*3", "sk:pud_a03*3", "sk:pud_a02*3", "sk:pud_b17*3", "sk:pud_a08*3", "sk:pud_b04*3" },
        "sm_g1" => new[] { "sk:pud_a19*3", "sk:pud_a07*3", "sk:pud_a16*3", "sk:pud_a22*3", "sk:pud_b12*3", "sk:pud_c01*3", "sk:pud_b20*3", "sk:pud_a23*3", "sk:pud_a05*3", "sk:pud_b07*3", "sk:pud_b13*3", "sk:pud_a18*3", "sk:pud_a14*3", "sk:pud_b19*3", "sk:pud_b09*3", "sk:pud_a12*3", "sk:pud_b10*3", "sk:pud_a11*3", "sk:pud_b11*3", "sk:pud_b22*3", "sk:pud_b06*3", "sk:pud_b21*3", "sk:pud_b18*3", "sk:pud_b15*3", "sk:pud_a20*3", "sk:pud_a15*3", "sk:pud_a10*3", "sk:pud_b16*3", "sk:pud_b05*3", "sk:pud_a06*3", "sk:pud_c02*3", "sk:pud_b23*3", "sk:pud_b14*3", "sk:pud_c04*3", "sk:pud_a21*3", "sk:pud_a13*3", "sk:pud_c03*3", "sk:pud_a09*3" },
        "sm_g2" => new[] { "sk:pud_d19*3", "sk:pud_d07*3", "sk:pud_c09*3", "sk:pud_d06*3", "sk:pud_c17*3", "sk:pud_c22*3", "sk:pud_d10*3", "sk:pud_d23*3", "sk:pud_c13*3", "sk:pud_c06*3", "sk:pud_d12*3", "sk:pud_c05*3", "sk:pud_c11*3", "sk:pud_c12*3", "sk:pud_d05*3", "sk:pud_d11*3", "sk:pud_c10*3", "sk:pud_c18*3", "sk:pud_d13*3", "sk:pud_c08*3", "sk:pud_d16*3", "sk:pud_d21*3", "sk:pud_d14*3", "sk:pud_c19*3", "sk:pud_d22*3", "sk:pud_c15*3", "sk:pud_d18*3", "sk:pud_c21*3", "sk:pud_c14*3", "sk:pud_c23*3", "sk:pud_c07*3", "sk:pud_d20*3", "sk:pud_c16*3", "sk:pud_c20*3", "sk:pud_c24*3", "sk:pud_d09*3", "sk:pud_d15*3" },
        "sm_g3" => new[] { "sk:pus_b16*3", "sk:pus_a11*3", "sk:pus_a17*3", "sk:pus_b15*3", "sk:pus_a14*3", "sk:pus_b11*3", "sk:pus_b08*3", "sk:pus_a09*3", "sk:pus_a07*3", "sk:pus_b07*3", "sk:pus_a03*3", "sk:pus_b14*3", "sk:pus_a16*3", "sk:pus_b02*3", "sk:pus_a08*3", "sk:pus_a04*3", "sk:pus_b17*3", "sk:pus_b09*3", "sk:pus_b12*3", "sk:pus_a01*3", "sk:pus_a12*3", "sk:pus_a13*3", "sk:pus_b10*3", "sk:pus_a15*3", "sk:pus_b04*3", "sk:pus_b01*3", "sk:pus_b03*3", "sk:pus_a10*3", "sk:pus_b05*3", "sk:pus_b06*3", "sk:pus_a02*3", "sk:pus_b18*3", "sk:pus_b13*3", "sk:pus_a18*3", "sk:pus_a06*3", "sk:pus_a05*3" },
        "sm_g4" => new[] { "sk:but_08*3", "sk:but_03*3", "sk:but_10*3", "sk:but_12*3", "sk:but_16*3", "sk:but_01*3", "sk:but_02*3", "sk:but_04*3", "sk:but_11*3", "sk:but_06*3", "sk:but_13*3", "sk:but_14*3", "sk:but_15*3", "sk:but_07*3", "sk:but_17*3", "sk:but_05*3", "sk:but_09*3", "sk:but_18*3" },
        "sm_g5" => new[] { "sk:kar_02*3", "sk:kar_04*3", "sk:kar_15*3", "sk:kar_06*3", "sk:kar_08*3", "sk:kar_09*3", "sk:kar_11*3", "sk:kar_07*3", "sk:kar_13*3", "sk:mleko*3", "sk:kar_12*3", "sk:kar_05*3", "sk:kar_03*3", "sk:kar_10*3", "sk:kar_14*3", "sk:kar_16*3", "sk:kar_01*3" },
        "sm_g6" => new[] { "sk:pud_b20*3", "sk:pud_c17*3", "sk:pud_d18*3", "sk:pud_c15*3", "sk:pud_a18*3", "sk:pud_b13*3", "sk:pud_d13*3", "sk:pud_b18*3", "sk:pud_d20*3", "sk:pud_c20*3", "sk:pud_d19*3", "sk:pud_c16*3", "sk:pud_b14*3", "sk:pud_d14*3", "sk:pud_b15*3", "sk:pud_a13*3", "sk:pud_c18*3", "sk:pud_a03*3", "sk:pud_b16*3", "sk:pud_a17*3", "sk:pud_a19*3", "sk:pud_c13*3", "sk:pud_d15*3", "sk:pud_a15*3", "sk:pud_a20*3", "sk:pud_a14*3", "sk:pud_a02*3", "sk:pud_c19*3", "sk:pud_c14*3", "sk:pud_a16*3", "sk:pud_b19*3", "sk:pud_a04*3", "sk:pud_a08*3", "sk:pud_d16*3", "sk:pud_a01*3" },
        "sm_g7" => new[] { "sk:pus_b14*3", "sk:but_07*3", "sk:but_09*3", "sk:pus_a12*3", "sk:pus_a13*3", "sk:but_06*3", "sk:but_01*3", "sk:but_03*3", "sk:but_02*3", "sk:but_05*3", "sk:pus_b15*3", "sk:pus_a15*3", "sk:pus_a17*3", "sk:but_08*3", "sk:pus_a16*3", "sk:pus_b13*3", "sk:but_04*3", "sk:pus_b12*3", "sk:pus_b17*3", "sk:pus_b16*3", "sk:pus_a14*3" },
        "sm_g8" => new[] { "sk:pus_a09*3", "sk:but_14*3", "sk:pus_a07*3", "sk:pus_a04*3", "sk:pus_a06*3", "sk:pus_a01*3", "sk:pus_a03*3", "sk:pus_a10*3", "sk:pus_a08*3", "sk:but_15*3", "sk:pus_a02*3", "sk:pus_a11*3", "sk:but_18*3", "sk:but_13*3", "sk:pus_a05*3", "ph:multi_cleaner_bottle", "ph:bleach_bottle", "ph:wine_bottles_01" },
        "odz_dol" => new[] { "sk:odz_gainer_a1*2", "sk:odz_gainer_a2*2", "sk:odz_gainer_b1*2", "sk:odz_gainer_b2*2", "sk:odz_kg_1*2", "sk:odz_kg_3*2", "sk:odz_kg_4*2", "sk:odz_whey_1*2", "sk:odz_whey_2*2", "sk:odz_whey_3*2", "sk:odz_whey_4*2" },
        "odz_polka" => new[] { "sk:odz_whey_1*3", "sk:odz_whey_2*3", "sk:odz_whey_3*3", "sk:odz_whey_4*3", "sk:odz_mala_1*3", "sk:odz_mala_2*3", "sk:odz_mala_3*3", "sk:odz_mala_4*3", "sk:odz_mala_5*3", "sk:odz_mala_6*3", "sk:odz_dod_1*3", "sk:odz_dod_2*3", "sk:odz_dod_3*3", "sk:odz_dod_4*3", "sk:odz_dod_5*3", "sk:odz_dod_6*3", "sk:odz_kg_2*3", "sk:odz_kg_5*3", "sk:odz_kg_6*3", "sk:odz_kg_7*3", "sk:odz_kg_8*3" },
        "odz_gora" => new[] { "sk:odz_mala_1*4", "sk:odz_mala_2*4", "sk:odz_mala_3*4", "sk:odz_mala_4*4", "sk:odz_mala_5*4", "sk:odz_mala_6*4", "sk:odz_dod_1*4", "sk:odz_dod_2*4", "sk:odz_dod_3*4", "sk:odz_dod_4*4", "sk:odz_dod_5*4", "sk:odz_dod_6*4", "sk:odz_kg_2*4", "sk:odz_kg_5*4", "sk:odz_kg_6*4", "sk:odz_kg_7*4", "sk:odz_kg_8*4" },
        "market" => new[] { "sk:pud_a23*3", "sk:kar_09*3", "sk:pud_d16*3", "sk:pud_c19*3", "sk:pud_a14*3", "sk:pud_d21*3", "sk:pus_b10*3", "sk:pud_c03*3", "sk:but_07*3", "sk:pud_a19*3", "sk:pud_a10*3", "sk:pus_a01*3", "sk:pud_b09*3", "sk:pus_b04*3", "sk:but_16*3", "sk:kar_13*3", "sk:pus_a16*3", "sk:pus_b07*3", "sk:pus_a04*3", "sk:pus_b13*3", "sk:pud_b22*3", "sk:but_04*3", "sk:pud_a05*3", "sk:pus_b16*3", "sk:pud_c11*3", "sk:kar_11*3", "sk:pud_d12*3", "sk:pud_b13*3", "sk:pud_b18*3", "sk:kar_01*3", "sk:but_10*3", "sk:pus_b01*3", "sk:pud_d07*3", "sk:pud_c15*3", "sk:but_01*3", "sk:pus_a07*3", "sk:kar_03*3", "sk:kar_15*3", "sk:kar_07*3", "sk:pud_c23*3", "sk:pus_a10*3", "sk:pud_c07*3", "sk:pus_a13*3", "sk:kar_05*3", "sk:mleko*5", "ph:long_life_food", "ph:CheeseBox_01" },
        // supermarket (BraxSupermarket): trzy typy segmentów regału-wyspy (różne odstępy półek)
        "sm_a" => new[] { "sk:mleko*4", "ph:russian_food_cans_01", "sk:mleko*4" },
        "sm_b" => new[] { "ph:long_life_food", "sk:mleko*4", "ph:CheeseBox_01", "ph:russian_food_cans_01" },
        "sm_c" => new[] { "ph:wine_bottles_01", "ph:multi_cleaner_bottle", "ph:bleach_bottle", "ph:wine_bottles_01" },
        // apteka: 24 leki BRAXHORN PHARMA (etykiety właściciela 3.11 14:00, pudełka leki.py), po kilka sztuk obok siebie
        "apteka" => new[] { "sk:lek_01*3", "sk:lek_02*3", "sk:lek_03*4", "sk:lek_04*3", "sk:lek_05*3", "sk:lek_06*4", "sk:lek_07*3", "sk:lek_08*3", "sk:lek_09*4", "sk:lek_10*3", "sk:lek_11*3", "sk:lek_12*4", "sk:lek_13*3", "sk:lek_14*3", "sk:lek_15*4", "sk:lek_16*3", "sk:lek_17*3", "sk:lek_18*4", "sk:lek_19*3", "sk:lek_20*3", "sk:lek_21*4", "sk:lek_22*3", "sk:lek_23*3", "sk:lek_24*4" },
        // regał chłodniczy supermarketu (właściciel 13:56: mięsa, wędliny, sery, ryby)
        "ch_dol" => new[] { "sk:losos", "sk:mieso*2", "sk:ser_dojrzewajacy*2", "sk:stek*3", "sk:kurczak_surowy", "sk:mieso*2" },
        "ch_srodek" => new[] { "sk:kielbaski_paczka*3", "sk:parowki*3", "sk:ser_klin*4", "sk:mieso*2" },
        "ch_gora" => new[] { "sk:parowki*3", "sk:kielbaski_paczka*3", "sk:ser_klin*4", "sk:ser_dojrzewajacy" },
        _ => null
    };

    static string[] NaLadzie(string typ) => typ switch
    {
        "piekarnia" => new[] { "ph:hamburger_buns", "ph:croissant*3", "ph:hamburger_buns" },
        "cukiernia" => new[] { "ph:strawberry_chocolate_cake", "ph:carrot_cake" },
        "kawiarnia" => new[] { "ph:croissant*2", "ph:carrot_cake" },
        "warzywniak" => new[] { "sk:owoce_skrzynka", "ph:bananas" },
        "pizzeria" => new[] { "sk:pizza", "sk:pizza" },
        "bistro" => new[] { "sk:hamburger*2", "sk:frytki*2", "sk:kurczak", "sk:szawarma*2" },
        "bar" => new[] { "sk:kebab", "sk:frytki", "sk:hamburger" },
        "market" => new[] { "sk:mleko*3" },
        "apteka" => new[] { "sk:lek_01*2", "sk:lek_13", "sk:lek_20*2" },
        _ => null
    };

    /// <summary>Czy w tym lokalu towar to prawdziwe modele (wtedy regał nie dostaje klocków).</summary>
    public static bool Zastepuje(string typ) => Polki(typ) != null;
    public static bool Ma(string typ) => Polki(typ) != null && (BraxMiastoAssety.Jest("miasto_sklepy"));

    public static Transform Korzen(Transform bg, string szyld)
    {
        var t = new GameObject("Towar: " + szyld + " (modele Poly Haven i paczki miasto_sklepy)").transform;
        t.SetParent(bg, false); naLokal[t] = 0; return t;
    }

    static GameObject Zrodlo(string spec)
    {
        GameObject g = spec.StartsWith("sk:")
            ? (BraxMiastoAssety.Jest("miasto_sklepy") ? BraxMiastoAssety.Model("miasto_sklepy", spec.Substring(3)) : null)
            : BraxRegion.Model(spec.Substring(3));
        if (!g) { Braki++; brakiId.Add(spec); }
        return g;
    }

    /// <summary>Stawia spec (z „*n”) od punktu p w kierunku wzdl; wglab = w głąb (od klienta). Zwraca zajętą długość.</summary>
    static float Postaw(string spec, Transform parent, Vector3 p, Vector3 wzdl, Vector3 wglab, float zostalo, System.Random rng, float maxWys = 99f)
    {
        int n = 1, gw = spec.IndexOf('*');
        if (gw > 0) { n = int.Parse(spec.Substring(gw + 1)); spec = spec.Substring(0, gw); }
        var src = Zrodlo(spec); if (!src) return 0f;
        float yaw = Quaternion.LookRotation(-wglab, Vector3.up).eulerAngles.y, uzyte = 0f;
        for (int k = 0; k < n; k++)
        {
            float y = yaw + (float)(rng.NextDouble() - .5) * 14f;
            string nazwa = "Towar: " + spec.Substring(3) + (n > 1 ? " " + (k + 1) : "");
            var o = spec.StartsWith("sk:") ? BraxMiastoAssety.Postaw(parent, src, nazwa, p, y, 1f, true, false)
                                           : BraxRegion.Put(parent, src, nazwa, p, y, K, false);
            if (!o) break;
            var b = BraxRegion.Bounds(o);
            float ext = Mathf.Abs(b.size.x * wzdl.x) + Mathf.Abs(b.size.z * wzdl.z);
            if (uzyte + ext > zostalo || b.size.y > maxWys) { UnityEngine.Object.DestroyImmediate(o); break; }
            Vector3 cel = p + wzdl * (uzyte + ext * .5f);
            o.transform.position += new Vector3(cel.x - b.center.x, p.y - b.min.y, cel.z - b.center.z);
            foreach (var r in o.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
            uzyte += ext + .01f; Postawione++; if (naLokal.ContainsKey(parent)) naLokal[parent]++;
        }
        return uzyte;
    }

    static void Rzad(string[] a, Transform parent, Vector3 start, Vector3 wzdl, Vector3 wglab, float dl, float odstep, System.Random rng, float maxWys = 99f)
    {
        if (a == null || a.Length == 0) return;
        float x = 0f; int i = rng.Next(a.Length), puste = 0;
        while (x < dl - .08f && puste < a.Length)
        {
            float u = Postaw(a[i++ % a.Length], parent, start + wzdl * x, wzdl, wglab, dl - x, rng, maxWys);
            if (u <= 0f) { puste++; x += .05f; continue; }
            puste = 0; x += u + odstep;
        }
    }

    /// <summary>Jedna półka regału: start = lewy koniec półki na wierzchu, w połowie głębokości.</summary>
    public static void Polka(string typ, Transform parent, Vector3 start, Vector3 wzdl, Vector3 wglab, float dl, System.Random rng, float maxWys = 99f)
        => Rzad(Polki(typ), parent, start, wzdl, wglab, dl, .05f, rng, maxWys);

    /// <summary>Towar na ladzie (od startu wzdłuż lady), przodem do klienta.</summary>
    public static void Lada(string typ, Transform parent, Vector3 start, Vector3 wzdl, Vector3 wglab, float dl, System.Random rng)
        => Rzad(NaLadzie(typ), parent, start, wzdl, wglab, dl, .12f, rng);

    /// <summary>Wózki sklepowe (model właściciela) przy wejściu: wsunięte jeden w drugi wzdłuż wzdl (od p), z kolizją.</summary>
    public static void Wozki(Transform parent, Transform kolizje, Vector3 p, Vector3 wzdl, int ile)
    {
        var src = Zrodlo("sk:wozek"); if (!src) return;
        float yaw = Quaternion.LookRotation(wzdl, Vector3.up).eulerAngles.y;
        for (int k = 0; k < ile; k++)
        {
            var o = BraxMiastoAssety.Postaw(parent, src, "Wózek sklepowy " + (k + 1), p + wzdl * ((.45f + k * .32f) * K), yaw, 1f, true, true);
            if (!o) return;
            BraxMiastoAssety.Kolizja(kolizje, o, "kolizja: wózek sklepowy " + (k + 1), .02f);
            Postawione++;
        }
    }

    /// <summary>Grupa LOD (cały towar lokalu znika dalej niż ~25 m) + log lokalu.</summary>
    public static void Zakoncz(Transform t, string szyld)
    {
        var rs = t.GetComponentsInChildren<Renderer>(true);
        if (rs.Length > 0)
        {
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var lod = t.gameObject.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(Mathf.Clamp(Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) / (2f * 25f * .52f), .01f, .6f), rs) });
            lod.RecalculateBounds();
        }
        naLokal.TryGetValue(t, out int n);
        Debug.Log("BRAXHORN_SKLEPY_TOWAR " + szyld + ": modeli " + n + (brakiId.Count > 0 ? "; brak w całym mieście: " + string.Join(", ", brakiId) : ""));
    }
}
