using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// E27(1) (build 45 runda 2): PRZEJŚCIE Z SALI GIER DO ZAPLECZA — drzwi wewnętrzne 1-skrzydłowe 1.0 × 2.1 m × K w tylnej
// ścianie sali (model gameready_casino_scene, lico z −51.12) i w ścianie zaplecza (z −51.17), x ≈ 85.6 (hol zaplecza),
// tabliczka „SALA VIP · POKER” od strony sali, ościeżnica stal grafit przez całą grubość ściany. BEZPIECZNIK: przed
// wycięciem sprawdzane, czy po stronie sali przed otworem nic nie stoi (kolizje, trójkąty siatek, promienie) — zajęte =
// bez przejścia (log DO POPRAWKI), ściana zaplecza pełna jak dotąd. Drzwi od zaułka: zamknięte na stałe (bez interakcji,
// TYLKO DLA PERSONELU) — tylko gdy przejście z sali powstało (inaczej zaplecze zostałoby bez wejścia).
public static partial class BraxClaudeOsiedle
{
    static bool kasynoPrzejscie;

    // zwraca true, gdy przejście zrobione (ściana zaplecza z otworem dx0–dx1, do wysokości dh); inaczej ściana pełna
    static bool PrzejscieSalaZaplecze(Transform estate, Transform physics, Material farba, float X0, float X1, float FY, float CY, float ZS)
    {
        kasynoPrzejscie = false;
        float w = 1.0f * K, h = 2.1f * K, ram = .08f, cx = 85.6f;
        bool zajete = false; string powod = "";
        // prowadzący (3.11, build 51fda47: „kolizja przed otworem” przy x 85.6 — przejście nie powstało): kilka pozycji wzdłuż
        // ściany holu zaplecza (±2.4 m), pierwsza wolna; w logu nazwa tego, co blokuje
        (bool, string) Test(float c)
        {
            float d0 = c - w * .5f - ram, d1 = c + w * .5f + ram;
            if (d0 < X0 + .3f || d1 > X1 - .3f) return (true, "poza ścianą zaplecza");
            Physics.SyncTransforms();
            var strefa = MinMax(d0, d1, FY + .2f, FY + h, -50.74f, -49.2f);   // przed licem sali (ściana modelu do −50.75 wycinana)
            var kol = Physics.OverlapBox(strefa.center, strefa.extents, Quaternion.identity, ~((1 << 8) | (1 << 29) | (1 << 2)), QueryTriggerInteraction.Ignore);
            if (kol.Length > 0) return (true, "kolizja przed otworem: " + string.Join(", ", kol.Take(3).Select(k => k.name)));
            foreach (var mf in estate.GetComponentsInChildren<MeshFilter>(true))
            {
                var r = mf.GetComponent<MeshRenderer>(); var m = mf.sharedMesh; if (!r || !m || !m.isReadable || !r.bounds.Intersects(strefa)) continue;
                var M = mf.transform.localToWorldMatrix; var v = m.vertices; var tr = m.triangles;
                for (int i = 0; i + 2 < tr.Length; i += 3)
                {
                    Vector3 a = M.MultiplyPoint3x4(v[tr[i]]), b = M.MultiplyPoint3x4(v[tr[i + 1]]), cc = M.MultiplyPoint3x4(v[tr[i + 2]]);
                    if (strefa.Contains((a + b + cc) / 3f)) return (true, "siatka " + mf.name + " przed otworem");
                    foreach (var px in new[] { c - .4f, c, c + .4f })
                        foreach (var py in new[] { FY + .5f, FY + 1.6f })
                            if (OdcinekTrojkat(new Vector3(px, py, -50.74f), new Vector3(px, py, -49.2f), a, b, cc)) return (true, "siatka " + mf.name + " na drodze przejścia");
                }
            }
            return (false, "");
        }
        var proby = new List<string>();
        try
        {
            zajete = true;
            foreach (float c in new[] { 85.6f, 84.4f, 86.8f, 83.2f, 88.0f })
            {
                var (z, pw) = Test(c);
                if (!z) { cx = c; zajete = false; break; }
                proby.Add("x " + c.ToString("F1") + ": " + pw);
            }
            powod = string.Join("; ", proby);
        }
        catch (Exception e) { zajete = true; powod = "błąd testu: " + e.Message; }
        float dx0 = cx - w * .5f - ram, dx1 = cx + w * .5f + ram, dh = FY + h + ram;
        if (!zajete) Debug.Log("BRAXHORN_KASYNO przejście sala gier → zaplecze x " + cx.ToString("F1") + (proby.Count > 0 ? " (zajęte wcześniej: " + string.Join("; ", proby) + ")" : ""));
        if (zajete)
        {
            BoxMinMax(farba, X0, X1, FY, CY, ZS - .03f, ZS, true);
            Debug.LogError("BRAXHORN_KASYNO DO POPRAWKI przejście sala gier → zaplecze NIE zrobione (" + powod + ") — ściana zaplecza pełna, drzwi od zaułka bez zmian");
            return false;
        }
        // otwór w tylnej ścianie sali gier (model) i w ścianie zaplecza
        CutVolumes(estate, physics, new List<Bounds> { MinMax(dx0, dx1, FY + .005f, dh, -51.45f, -50.75f) });
        BoxMinMax(farba, X0, dx0, FY, CY, ZS - .03f, ZS, true); BoxMinMax(farba, dx1, X1, FY, CY, ZS - .03f, ZS, true);
        BoxMinMax(farba, dx0, dx1, dh, CY, ZS - .03f, ZS, true);
        // ościeżnica przez całą grubość (z −51.45 … −50.88), próg
        BoxMinMax(Frame, dx0, dx0 + ram, FY, dh, -51.45f, -50.73f, true); BoxMinMax(Frame, dx1 - ram, dx1, FY, dh, -51.45f, -50.73f, true);
        BoxMinMax(Frame, dx0, dx1, dh - ram, dh, -51.45f, -50.73f, false, true);
        BoxMinMax(Steel, dx0 + ram, dx1 - ram, FY - .01f, FY + .012f, -51.45f, -50.73f, false);
        // ściana nad otworem od strony sali (model wycięty do dh): tynk ciemny między licami, z kolizją
        BoxMinMax(Plain("wstawka nad drzwiami sali gier", new Color(.08f, .07f, .07f), .3f), dx0, dx1, dh, dh + .45f, -51.45f, -50.75f, true, true);   // nadproże z tabliczką
        // skrzydło: osobny obiekt BraxCityDoor (zawias przy lewym słupku, otwiera się na obie strony)
        var drzwi = new GameObject("Osiedle door casino-zaplecze-hall"); drzwi.transform.SetParent(Group(group), false);
        drzwi.transform.position = new Vector3(cx, FY, -51.17f);
        var zawias = new GameObject("skrzydło (zawias)").transform; zawias.SetParent(drzwi.transform, false); zawias.position = new Vector3(dx0 + ram + .005f, FY + .008f, -51.17f);
        var pl = new GameObject("skrzydło — płyta orzech"); pl.transform.SetParent(zawias, false); pl.transform.localPosition = new Vector3((w - .01f) * .5f, (h - .012f) * .5f, 0f);
        pl.AddComponent<MeshFilter>().sharedMesh = KlocekSiatka(new Vector3(w - .01f, h - .012f, .045f));
        var orzech = Plain("boazeria orzech lakier", new Color(.20f, .10f, .05f), .7f);
        pl.AddComponent<MeshRenderer>().sharedMaterials = new[] { orzech, orzech };
        foreach (float z in new[] { -.035f, .035f })
        {
            var kl = new GameObject("klamka"); kl.transform.SetParent(zawias, false); kl.transform.localPosition = new Vector3(w - .12f, 1.05f * K, z);
            kl.AddComponent<MeshFilter>().sharedMesh = KlocekSiatka(new Vector3(.14f, .025f, .025f));
            var zl = Plain("złoto kasyna (mosiądz szczotkowany)", new Color(.80f, .62f, .25f), .72f, .9f); kl.AddComponent<MeshRenderer>().sharedMaterials = new[] { zl, zl };
        }
        var bc = zawias.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3((w - .01f) * .5f, (h - .012f) * .5f, 0f); bc.size = new Vector3(w - .01f, h - .012f, .05f);
        var dd = drzwi.AddComponent<BraxCityDoor>(); dd.id = "casino-zaplecze-hall"; dd.label = "SALA VIP · POKER"; dd.leaves = new[] { zawias }; dd.sides = new[] { 1f };
        // tabliczka od strony sali
        Sign("vip_poker", new Vector3(cx, dh + .22f, -50.735f), 1.35f, .3f, Vector3.forward, false);
        kasynoPrzejscie = true;
        Debug.Log("BRAXHORN_KASYNO przejście sala gier → zaplecze: drzwi 1-skrzydłowe " + w.ToString("F2") + " × " + h.ToString("F2") + " m (1.0 × 2.1 m × K) x " + cx.ToString("F1") + ", tabliczka SALA VIP · POKER");
        Debug.Log("BRAXHORN_DRZWI typ=wewnętrzne szer=" + w.ToString("F2") + " wys=" + h.ToString("F2") + " budynek=casino id=casino-zaplecze-hall");
        return true;
    }

    // E25: tylne drzwi (…-rear) budynków z wejściem od frontu (…-front) — zamknięte na stałe jako drzwi serwisowe (komponent
    // wyłączony: bez interakcji i bez samootwierania). Dyskoteka (club) — 2 wejścia dozwolone; kasyno — osobno (ZaulekZamkniety).
    static void TylneZamkniete()
    {
        var wszystkie = UnityEngine.Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x && !string.IsNullOrEmpty(x.id)).ToList();
        var fronty = new HashSet<string>(wszystkie.Where(x => x.enabled && Klasa(x.id) == KlasaDrzwi.Wejscie).Select(x => x.id.Split('-')[0]));
        int zamk = 0; var zostaja = new List<string>();
        foreach (var d in wszystkie)
        {
            if (!d.id.EndsWith("-rear", StringComparison.OrdinalIgnoreCase)) continue;
            string bud = d.id.Split('-')[0];
            if (bud == "club" || bud == "casino" || !fronty.Contains(bud)) { zostaja.Add(d.id + (bud == "club" ? " (dyskoteka: 2 wejścia)" : bud == "casino" ? " (kasyno: osobno)" : " (brak wejścia od frontu)")); continue; }
            d.enabled = false; zamk++;
            Debug.Log("BRAXHORN_DRZWI typ=serwisowe budynek=" + bud + " id=" + d.id + " zamknięte na stałe (E25: jedno wejście — front)");
        }
        Debug.Log("BRAXHORN_WEJSCIA_SPIS tylne_zamkniete=" + zamk + " tylne_otwarte=" + zostaja.Count + (zostaja.Count > 0 ? " [" + string.Join("; ", zostaja) + "]" : "") + " wejscia_front=" + fronty.Count + " [" + string.Join(", ", fronty.OrderBy(x => x)) + "]");
    }

    // drzwi od zaułka (Codex, x 84–88, z −57): zamknięte na stałe jako drzwi serwisowe — komponent wyłączony (bez interakcji)
    static void ZaulekZamkniety()
    {
        try { TylneZamkniete(); } catch (Exception e) { Debug.LogError("BRAXHORN_DRZWI DO POPRAWKI tylne drzwi: " + e.Message); }
        if (!kasynoPrzejscie) { Debug.Log("BRAXHORN_KASYNO drzwi od zaułka: bez zmian (brak przejścia z sali — zaplecze musi mieć wejście)"); return; }
        var d = UnityEngine.Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(x => x && x.id != "casino-zaplecze-hall" && Mathf.Abs(x.transform.position.x - 86f) < 3f && Mathf.Abs(x.transform.position.z + 57f) < 1.5f).FirstOrDefault();
        if (!d) { Debug.Log("BRAXHORN_KASYNO drzwi od zaułka: nie znalezione przy (86, −57) — bez zmian"); return; }
        d.enabled = false;
        Debug.Log("BRAXHORN_KASYNO drzwi od zaułka " + d.id + ": zamknięte na stałe (TYLKO DLA PERSONELU, bez interakcji)");
        Debug.Log("BRAXHORN_DRZWI typ=serwisowe budynek=casino id=" + d.id + " zamknięte (E25: jedno wejście — front)");
    }
}
