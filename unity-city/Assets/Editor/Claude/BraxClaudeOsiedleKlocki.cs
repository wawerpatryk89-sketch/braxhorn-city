using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ZAKAZ KLOCKÓW + PRACOWNIK ZA LADĄ (runda 5, koordynator: „pojazdy, meble, maszyny z gołych prostopadłościanów: wymień
// albo usuń, log BRAXHORN_KLOCKI; sklepy z pracownikiem za ladą wszędzie”).
//  • audyt przed scaleniem siatek: (1) partie generatora, których materiał nazywa mebel/pojazd/maszynę, a jest gładkim kolorem
//    bez tekstury i składa się wyłącznie z prostopadłościanów (≤ 2 bryły na obiekt — bez detali) = kandydaci do wymiany (lista
//    z grupą i liczbą brył); (2) osobne obiekty w scenie z siatką prostopadłościanu (24/8 wierzchołków albo „Cube”) o wymiarach
//    mebla/pojazdu, na posadzce: prymitywy „Cube” — USUNIĘTE (z kolizją w tym samym miejscu), reszta — lista;
//  • w wymienionych w tej rundzie obiektach B (warsztat: wózek, stół, podnośnik, opony; salon, wulkanizacja) klocków nie ma;
//  • obsada: Nutrition (pierzeja B) — sprzedawca za ladą zestawu; biuro przyjęć warsztatu — przyjmujący za ladą.
// Logi: BRAXHORN_KLOCKI, BRAXHORN_SKLEPY_OBSADA.
public static partial class BraxClaudeOsiedle
{
    static readonly string[] KlockiSlowa = { "regał", "lada", "stół", "stol", "biurk", "krzes", "kanap", "sofa", "fotel", "łóżk", "lozk", "szaf", "komod", "ławk", "ławka", "auto", "samoch", "wózek", "wozek", "maszyn", "kontener", "beczk", "bieżni", "rower", "lodówk", "pralk", "telewiz", "skrzyni", "półk" };
    static void KlockiAudyt()
    {
        var kand = new List<string>(); int partie = 0;
        foreach (var kv in batches)
        {
            var m = kv.Key.Item2; if (!m) continue; string n = m.name.ToLowerInvariant();
            if (!KlockiSlowa.Any(n.Contains)) continue;
            bool tekst = m.HasProperty("_MainTex") && m.mainTexture; if (tekst) continue;
            int quady = kv.Value.i.Count / 6, bryly = quady / 6;
            if (bryly == 0) continue;
            partie++; kand.Add(kv.Key.Item1 + " / " + m.name.Replace("Osiedle: ", "") + " ×" + bryly);
        }
        // osobne obiekty: prostopadłościany o wymiarach mebla/pojazdu
        int usuniete = 0; var lista = new List<string>();
        MeshFilter[] mfs = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mf in mfs)
        {
            if (!mf || !mf.sharedMesh) continue; var mesh = mf.sharedMesh; int vc = mesh.vertexCount;
            bool kostka = mesh.name == "Cube" || vc == 24 || vc == 8; if (!kostka) continue;
            var r = mf.GetComponent<MeshRenderer>(); if (!r) continue; var b = r.bounds;
            float mn = Mathf.Min(b.size.x, Mathf.Min(b.size.y, b.size.z)), mx = Mathf.Max(b.size.x, b.size.z);
            if (mn < .25f || mx > 6.5f || b.size.y > 3.2f || b.min.y > .6f) continue;
            string nazwa = mf.name, nl = nazwa.ToLowerInvariant();
            if (!r.enabled || nl.StartsWith("braxhorn") || nl.Contains("kolizj") || nl.Contains("marker") || nl.Contains("punkt") || nl.Contains("trigger") || mf.GetComponentInParent<CharacterController>()) continue;
            if (mesh.name == "Cube")
            {
                foreach (var c in Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToArray())
                    if (c && c.gameObject != mf.gameObject && (c.bounds.center - b.center).sqrMagnitude < .01f && (c.bounds.size - b.size).sqrMagnitude < .01f) Object.DestroyImmediate(c);
                Object.DestroyImmediate(mf.gameObject); usuniete++; lista.Add("usunięty prymityw: " + nazwa + " @" + b.center.ToString("F1"));
            }
            else lista.Add(nazwa + " " + b.size.ToString("F2") + " @" + b.center.ToString("F1"));
        }
        string log = "BRAXHORN_KLOCKI kandydaci_partie=" + partie + " obiekty_prostopadłościany=" + (lista.Count - usuniete) + " usunięte_prymitywy=" + usuniete +
                     " wymienione_w_rundzie_5=warsztat(wózek, stół, podnośnik, stojak opon)→modele/detale (kandydaci to partie bez tekstury nazwane jak mebel/pojazd/maszyna — do przejrzenia, nie usuwam automatycznie zabudowy lokali)" +
                     " [" + string.Join("; ", kand.Take(40)) + "] obiekty: [" + string.Join("; ", lista.Take(20)) + "]";
        Debug.Log(log); rep.notes.Add(log.Replace("BRAXHORN_KLOCKI ", "klocki: "));
    }

    static void ObsadaSklepow()
    {
        if (rbPostacie == null || rbPostacie.Count == 0) { Debug.Log("BRAXHORN_SKLEPY_OBSADA 0 (brak paczki Rocketbox)"); return; }
        var rnd = new System.Random(6161); var lada = new[] { "work_mid", "idle_neutral_01", "idle_neutral_02", "idle_look_around_01" }; var opis = new List<string>();
        // Nutrition (pierzeja B, lokal 3): sprzedawca za ladą zestawu (lada x b − 1.6, obrót 270 → za nią od strony ściany)
        float x0 = AxisX + Half + .5f, x1 = AxisX + Half + 62f, u = (x1 - x0) / 5f, zf = StreetZ + Half + 6f + .3f, bN = x0 + 3f * u - .1f;
        var m1 = Obsada("Nutrition — za ladą", RolaObsluga, "sprzedawca", new Vector3(bN - .7f, 0, zf + 2.4f), 270f, rnd, lada, .14f);
        opis.Add("Nutrition=" + (m1 ? 1 : 0));
        // biuro przyjęć warsztatu: przyjmujący za ladą (lada x 24.4…28.3, z −49.2…−48.5; klienci od drzwi z północy)
        if (WarsztatWlaczony)
        {
            var m2 = Obsada("warsztat — przyjęcie auta (za ladą)", RolaMechanik, "mechanik", new Vector3(26.2f, .17f, -50.0f), 0f, rnd, lada, .17f);
            opis.Add("warsztat_biuro=" + (m2 ? 1 : 0));
        }
        Debug.Log("BRAXHORN_SKLEPY_OBSADA " + string.Join(" ", opis) + " (już obsadzone wcześniej: Shake Bar, kawiarnia ×2, market, stacja, salon, siłownia, kasyno ×2, komisariat, dyskoteka; butik zamknięty — WKRÓTCE OTWARCIE)");
    }
}
