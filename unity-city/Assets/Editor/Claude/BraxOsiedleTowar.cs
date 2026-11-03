using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// TOWAR W SKLEPACH OSIEDLA (właściciel 3.11 14:50: „przenoś na osiedle to, co zrobiłeś w mieście — żadnych kwadratowych pudełek”).
// Ten sam towar co w mieście: paczka miasto_sklepy (zaszyfrowana, odszyfrowywana przez prywatne.py do Prywatne/Osiedle/miasto_sklepy),
// modele w prawdziwych metrach × K. Brak modelu = luka na półce + log, nigdy klocek.
public static class BraxOsiedleTowar
{
    public const string Paczka = "miasto_sklepy";
    static float K => BraxClaudeOsiedle.K;
    public static int Postawione;
    static readonly HashSet<string> braki = new HashSet<string>();
    static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
    public static bool Jest => BraxOsiedleAssety.Jest(Paczka);

    static string[] Asortyment(string typ) => typ switch
    {
        "sm_g0" => new[] { "sk:pud_b01*3", "sk:pud_d17*3", "sk:pud_d03*3", "sk:pud_d01*3", "sk:pud_b08*3", "sk:pud_b24*3", "sk:pud_d24*3", "sk:pud_a24*3", "sk:pud_d04*3", "sk:pud_d08*3", "sk:pud_d02*3", "sk:pud_b02*3", "sk:pud_a01*3", "sk:pud_b03*3", "sk:pud_a17*3", "sk:pud_a04*3", "sk:pud_a03*3", "sk:pud_a02*3", "sk:pud_b17*3", "sk:pud_a08*3", "sk:pud_b04*3" },
        "sm_g1" => new[] { "sk:pud_a19*3", "sk:pud_a07*3", "sk:pud_a16*3", "sk:pud_a22*3", "sk:pud_b12*3", "sk:pud_c01*3", "sk:pud_b20*3", "sk:pud_a23*3", "sk:pud_a05*3", "sk:pud_b07*3", "sk:pud_b13*3", "sk:pud_a18*3", "sk:pud_a14*3", "sk:pud_b19*3", "sk:pud_b09*3", "sk:pud_a12*3", "sk:pud_b10*3", "sk:pud_a11*3", "sk:pud_b11*3", "sk:pud_b22*3", "sk:pud_b06*3", "sk:pud_b21*3", "sk:pud_b18*3", "sk:pud_b15*3", "sk:pud_a20*3", "sk:pud_a15*3", "sk:pud_a10*3", "sk:pud_b16*3", "sk:pud_b05*3", "sk:pud_a06*3", "sk:pud_c02*3", "sk:pud_b23*3", "sk:pud_b14*3", "sk:pud_c04*3", "sk:pud_a21*3", "sk:pud_a13*3", "sk:pud_c03*3", "sk:pud_a09*3" },
        "sm_g2" => new[] { "sk:pud_d19*3", "sk:pud_d07*3", "sk:pud_c09*3", "sk:pud_d06*3", "sk:pud_c17*3", "sk:pud_c22*3", "sk:pud_d10*3", "sk:pud_d23*3", "sk:pud_c13*3", "sk:pud_c06*3", "sk:pud_d12*3", "sk:pud_c05*3", "sk:pud_c11*3", "sk:pud_c12*3", "sk:pud_d05*3", "sk:pud_d11*3", "sk:pud_c10*3", "sk:pud_c18*3", "sk:pud_d13*3", "sk:pud_c08*3", "sk:pud_d16*3", "sk:pud_d21*3", "sk:pud_d14*3", "sk:pud_c19*3", "sk:pud_d22*3", "sk:pud_c15*3", "sk:pud_d18*3", "sk:pud_c21*3", "sk:pud_c14*3", "sk:pud_c23*3", "sk:pud_c07*3", "sk:pud_d20*3", "sk:pud_c16*3", "sk:pud_c20*3", "sk:pud_c24*3", "sk:pud_d09*3", "sk:pud_d15*3" },
        "sm_g3" => new[] { "sk:pus_b16*3", "sk:pus_a11*3", "sk:pus_a17*3", "sk:pus_b15*3", "sk:pus_a14*3", "sk:pus_b11*3", "sk:pus_b08*3", "sk:pus_a09*3", "sk:pus_a07*3", "sk:pus_b07*3", "sk:pus_a03*3", "sk:pus_b14*3", "sk:pus_a16*3", "sk:pus_b02*3", "sk:pus_a08*3", "sk:pus_a04*3", "sk:pus_b17*3", "sk:pus_b09*3", "sk:pus_b12*3", "sk:pus_a01*3", "sk:pus_a12*3", "sk:pus_a13*3", "sk:pus_b10*3", "sk:pus_a15*3", "sk:pus_b04*3", "sk:pus_b01*3", "sk:pus_b03*3", "sk:pus_a10*3", "sk:pus_b05*3", "sk:pus_b06*3", "sk:pus_a02*3", "sk:pus_b18*3", "sk:pus_b13*3", "sk:pus_a18*3", "sk:pus_a06*3", "sk:pus_a05*3" },
        "sm_g4" => new[] { "sk:but_08*3", "sk:but_03*3", "sk:but_10*3", "sk:but_12*3", "sk:but_16*3", "sk:but_01*3", "sk:but_02*3", "sk:but_04*3", "sk:but_11*3", "sk:but_06*3", "sk:but_13*3", "sk:but_14*3", "sk:but_15*3", "sk:but_07*3", "sk:but_17*3", "sk:but_05*3", "sk:but_09*3", "sk:but_18*3" },
        "sm_g5" => new[] { "sk:kar_02*3", "sk:kar_04*3", "sk:kar_15*3", "sk:kar_06*3", "sk:kar_08*3", "sk:kar_09*3", "sk:kar_11*3", "sk:kar_07*3", "sk:kar_13*3", "sk:mleko*3", "sk:kar_12*3", "sk:kar_05*3", "sk:kar_03*3", "sk:kar_10*3", "sk:kar_14*3", "sk:kar_16*3", "sk:kar_01*3" },
        "sm_g6" => new[] { "sk:tor_01*3", "sk:tor_02*3", "sk:tor_03*3", "sk:tor_04*3", "sk:tor_05*3", "sk:tor_06*3", "sk:tor_07*3", "sk:tor_08*3", "sk:tor_09*3", "sk:tor_10*3", "sk:tor_11*3", "sk:tor_12*3", "sk:tor_13*3", "sk:tor_14*3", "sk:tor_15*3", "sk:tor_16*3", "sk:pud_b20*3", "sk:pud_c17*3", "sk:pud_d18*3", "sk:pud_c15*3", "sk:pud_a18*3", "sk:pud_b13*3", "sk:pud_d13*3", "sk:pud_b18*3", "sk:pud_d20*3", "sk:pud_c20*3", "sk:pud_d19*3", "sk:pud_c16*3", "sk:pud_b14*3", "sk:pud_d14*3", "sk:pud_b15*3", "sk:pud_a13*3", "sk:pud_c18*3", "sk:pud_a03*3", "sk:pud_b16*3", "sk:pud_a17*3", "sk:pud_a19*3", "sk:pud_c13*3", "sk:pud_d15*3", "sk:pud_a15*3", "sk:pud_a20*3", "sk:pud_a14*3", "sk:pud_a02*3", "sk:pud_c19*3", "sk:pud_c14*3", "sk:pud_a16*3", "sk:pud_b19*3", "sk:pud_a04*3", "sk:pud_a08*3", "sk:pud_d16*3", "sk:pud_a01*3" },
        "sm_g7" => new[] { "sk:pus_b14*3", "sk:but_07*3", "sk:but_09*3", "sk:pus_a12*3", "sk:pus_a13*3", "sk:but_06*3", "sk:but_01*3", "sk:but_03*3", "sk:but_02*3", "sk:but_05*3", "sk:pus_b15*3", "sk:pus_a15*3", "sk:pus_a17*3", "sk:but_08*3", "sk:pus_a16*3", "sk:pus_b13*3", "sk:but_04*3", "sk:pus_b12*3", "sk:pus_b17*3", "sk:pus_b16*3", "sk:pus_a14*3" },
        "sm_g8" => new[] { "sk:pus_a09*3", "sk:but_14*3", "sk:pus_a07*3", "sk:pus_a04*3", "sk:pus_a06*3", "sk:pus_a01*3", "sk:pus_a03*3", "sk:pus_a10*3", "sk:pus_a08*3", "sk:but_15*3", "sk:pus_a02*3", "sk:pus_a11*3", "sk:but_18*3", "sk:but_13*3", "sk:pus_a05*3" },
        "odz_dol" => new[] { "sk:odz_gainer_a1*2", "sk:odz_gainer_a2*2", "sk:odz_gainer_b1*2", "sk:odz_gainer_b2*2", "sk:odz_kg_1*2", "sk:odz_kg_3*2", "sk:odz_kg_4*2", "sk:odz_whey_1*2", "sk:odz_whey_2*2", "sk:odz_whey_3*2", "sk:odz_whey_4*2" },
        "odz_polka" => new[] { "sk:odz_whey_1*3", "sk:odz_whey_2*3", "sk:odz_whey_3*3", "sk:odz_whey_4*3", "sk:odz_mala_1*3", "sk:odz_mala_2*3", "sk:odz_mala_3*3", "sk:odz_mala_4*3", "sk:odz_mala_5*3", "sk:odz_mala_6*3", "sk:odz_dod_1*3", "sk:odz_dod_2*3", "sk:odz_dod_3*3", "sk:odz_dod_4*3", "sk:odz_dod_5*3", "sk:odz_dod_6*3", "sk:odz_kg_2*3", "sk:odz_kg_5*3", "sk:odz_kg_6*3", "sk:odz_kg_7*3", "sk:odz_kg_8*3" },
        "odz_gora" => new[] { "sk:odz_mala_1*4", "sk:odz_mala_2*4", "sk:odz_mala_3*4", "sk:odz_mala_4*4", "sk:odz_mala_5*4", "sk:odz_mala_6*4", "sk:odz_dod_1*4", "sk:odz_dod_2*4", "sk:odz_dod_3*4", "sk:odz_dod_4*4", "sk:odz_dod_5*4", "sk:odz_dod_6*4", "sk:odz_kg_2*4", "sk:odz_kg_5*4", "sk:odz_kg_6*4", "sk:odz_kg_7*4", "sk:odz_kg_8*4" },
        "market" => new[] { "sk:tor_01*3", "sk:tor_02*3", "sk:tor_03*3", "sk:tor_04*3", "sk:tor_05*3", "sk:tor_06*3", "sk:tor_07*3", "sk:tor_08*3", "sk:tor_09*3", "sk:tor_10*3", "sk:tor_11*3", "sk:tor_12*3", "sk:tor_13*3", "sk:tor_14*3", "sk:tor_15*3", "sk:tor_16*3", "sk:pud_a23*3", "sk:kar_09*3", "sk:pud_d16*3", "sk:pud_c19*3", "sk:pud_a14*3", "sk:pud_d21*3", "sk:pus_b10*3", "sk:pud_c03*3", "sk:but_07*3", "sk:pud_a19*3", "sk:pud_a10*3", "sk:pus_a01*3", "sk:pud_b09*3", "sk:pus_b04*3", "sk:but_16*3", "sk:kar_13*3", "sk:pus_a16*3", "sk:pus_b07*3", "sk:pus_a04*3", "sk:pus_b13*3", "sk:pud_b22*3", "sk:but_04*3", "sk:pud_a05*3", "sk:pus_b16*3", "sk:pud_c11*3", "sk:kar_11*3", "sk:pud_d12*3", "sk:pud_b13*3", "sk:pud_b18*3", "sk:kar_01*3", "sk:but_10*3", "sk:pus_b01*3", "sk:pud_d07*3", "sk:pud_c15*3", "sk:but_01*3", "sk:pus_a07*3", "sk:kar_03*3", "sk:kar_15*3", "sk:kar_07*3", "sk:pud_c23*3", "sk:pus_a10*3", "sk:pud_c07*3", "sk:pus_a13*3", "sk:kar_05*3", "sk:mleko*5" },
        "os_lodowka" => new[] { "sk:kar_01*2", "sk:kar_02*2", "sk:kar_03*2", "sk:kar_04*2", "sk:kar_05*2", "sk:kar_06*2", "sk:kar_07*2", "sk:kar_08*2", "sk:kar_09*2", "sk:kar_10*2", "sk:kar_11*2", "sk:kar_12*2", "sk:kar_13*2", "sk:kar_14*2", "sk:kar_15*2", "sk:kar_16*2", "sk:but_01*2", "sk:but_02*2", "sk:but_03*2", "sk:but_04*2", "sk:but_05*2", "sk:but_06*2", "sk:but_07*2", "sk:but_08*2", "sk:but_09*2", "sk:but_17*2", "sk:but_18*2", "sk:mleko*4" },
        // apteka osiedla (3.11 15:59, lokal po butiku NYXA): leki BRAXHORN z etykiet — ten sam zestaw co apteka w mieście
        "apteka" => new[] { "sk:lek_01*3", "sk:lek_02*3", "sk:lek_03*4", "sk:lek_04*3", "sk:lek_05*3", "sk:lek_06*4", "sk:lek_07*3", "sk:lek_08*3", "sk:lek_09*4", "sk:lek_10*3", "sk:lek_11*3", "sk:lek_12*4", "sk:lek_13*3", "sk:lek_14*3", "sk:lek_15*4", "sk:lek_16*3", "sk:lek_17*3", "sk:lek_18*4", "sk:lek_19*3", "sk:lek_20*3", "sk:lek_21*4", "sk:lek_22*3", "sk:lek_23*3", "sk:lek_24*4" },
        "apteka_lada" => new[] { "sk:lek_01*2", "sk:lek_13", "sk:lek_20*2" },
        _ => null
    };

    public static GameObject Model(string plik)
    {
        if (cache.TryGetValue(plik, out var g) && g) return g;
        g = Jest ? BraxOsiedleAssety.Model(Paczka, plik) : null;
        if (!g) braki.Add(plik); else cache[plik] = g;
        return g;
    }

    /// <summary>Model z paczki: podstawa (środek obrysu) w p, obrót yaw, skala × K; bez cieni dla drobnego towaru.</summary>
    public static GameObject Postaw(Transform parent, string plik, string nazwa, Vector3 p, float yaw, bool cienie = false)
    {
        var src = Model(plik); if (!src) return null;
        var o = (GameObject)UnityEngine.Object.Instantiate(src, parent, false);
        o.name = nazwa; o.transform.rotation = Quaternion.Euler(0f, yaw, 0f); o.transform.localScale = Vector3.one * K; o.transform.position = Vector3.zero;
        var b = BraxOsiedleAssety.Granice(o);
        o.transform.position += new Vector3(p.x - b.center.x, p.y - b.min.y, p.z - b.center.z);
        BraxOsiedleAssety.Przygotuj(o, cienie);
        if (!cienie) foreach (var r in o.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
        Postawione++;
        return o;
    }

    static float Wstaw(string spec, Transform parent, Vector3 p, Vector3 wzdl, Vector3 wglab, float zostalo, System.Random rng, float maxWys)
    {
        int n = 1, gw = spec.IndexOf('*');
        if (gw > 0) { n = int.Parse(spec.Substring(gw + 1)); spec = spec.Substring(0, gw); }
        string plik = spec.Substring(3); float yaw = Quaternion.LookRotation(-wglab, Vector3.up).eulerAngles.y, uzyte = 0f;
        for (int k = 0; k < n; k++)
        {
            var o = Postaw(parent, plik, "Towar: " + plik, p, yaw + (float)(rng.NextDouble() - .5) * 12f);
            if (!o) break;
            var b = BraxOsiedleAssety.Granice(o);
            float ext = Mathf.Abs(b.size.x * wzdl.x) + Mathf.Abs(b.size.z * wzdl.z);
            if (uzyte + ext > zostalo || b.size.y > maxWys) { UnityEngine.Object.DestroyImmediate(o); Postawione--; break; }
            Vector3 cel = p + wzdl * (uzyte + ext * .5f);
            o.transform.position += new Vector3(cel.x - b.center.x, 0f, cel.z - b.center.z);
            uzyte += ext + .01f;
        }
        return uzyte;
    }

    /// <summary>Rząd towaru na półce: start = lewy koniec, na wierzchu półki, w połowie głębokości; wglab = od klienta.</summary>
    public static void Polka(string typ, Transform parent, Vector3 start, Vector3 wzdl, Vector3 wglab, float dl, System.Random rng, float maxWys = 99f)
    {
        var a = Asortyment(typ); if (a == null) return;
        float x = 0f; int i = rng.Next(a.Length), puste = 0;
        while (x < dl - .08f && puste < a.Length)
        {
            float u = Wstaw(a[i++ % a.Length], parent, start + wzdl * x, wzdl, wglab, dl - x, rng, maxWys);
            if (u <= 0f) { puste++; x += .05f; continue; }
            puste = 0; x += u + .04f;
        }
    }

    /// <summary>Cały towar lokalu w jednej grupie LOD (znika dalej niż ~25 m) + log.</summary>
    public static void Zakoncz(Transform t, string nazwa)
    {
        var rs = t.GetComponentsInChildren<Renderer>(true);
        if (rs.Length > 0)
        {
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var lod = t.gameObject.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(Mathf.Clamp(Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) / (2f * 25f * .52f), .01f, .6f), rs) });
            lod.RecalculateBounds();
        }
        Debug.Log("BRAXHORN_OSIEDLE_TOWAR " + nazwa + ": modeli " + rs.Length + (braki.Count > 0 ? "; brak: " + string.Join(", ", braki) : ""));
    }
}
