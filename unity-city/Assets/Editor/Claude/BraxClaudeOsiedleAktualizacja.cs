using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class BraxClaudeOsiedle
{
    // Exhaustive door inventory in the actual generated Unity scene.
    // Door panels must remain intact and functional. A geometric probe
    // cannot replace opening each door in Play Mode or on Android.
    static List<string> AudytWszystkichDrzwi(Scene scena)
    {
        Physics.SyncTransforms();
        var errors = new List<string>();
        var report = new List<string> {
            "OSIEDLE R2 / WSZYSTKIE DRZWI: statyczna kontrola Unity, bez Play Mode i APK"
        };
        var doors = scena.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<BraxCityDoor>(true)).Where(d => d).ToArray();
        if (doors.Length == 0)
            errors.Add("DO POPRAWKI: brak jakichkolwiek drzwi w wygenerowanej scenie osiedla");
        foreach (var duplicates in doors.Where(d => !string.IsNullOrEmpty(d.id))
            .GroupBy(d => d.id, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            errors.Add("DO POPRAWKI: powtórzony identyfikator drzwi " + duplicates.Key +
                " — " + duplicates.Count() + " obiektów");
        var casts = new RaycastHit[32];
        const int maska = ~((1 << 8) | (1 << 2) | (1 << 29) | (1 << 31));
        foreach (var d in doors)
        {
            string id = string.IsNullOrEmpty(d.id) ? "(brak id)" : d.id;
            var problems = new List<string>();
            if (string.IsNullOrEmpty(d.id)) problems.Add("brak identyfikatora");
            if (!d.enabled && Klasa(d.id) == KlasaDrzwi.Wejscie)
                problems.Add("główne wejście jest wyłączone");
            if (d.leaves == null || d.leaves.Length == 0 || d.leaves.All(t => !t))
                problems.Add("brak skrzydła drzwi");
            Bounds bounds = default;
            bool haveBounds = false;
            if (d.leaves != null) foreach (var leaf in d.leaves)
            {
                if (!leaf)
                {
                    problems.Add("puste odwołanie do skrzydła");
                    continue;
                }
                if (Vector3.Angle(leaf.up, Vector3.up) > 20f)
                    problems.Add("skrzydło przekrzywione względem pionu");
                var box = leaf.GetComponent<BoxCollider>();
                if (!box)
                {
                    problems.Add("skrzydło bez własnego BoxCollider");
                    continue;
                }
                if (!haveBounds) { bounds = box.bounds; haveBounds = true; }
                else bounds.Encapsulate(box.bounds);
            }
            // Sweep an adult-sized capsule across the doorway. Ignore the
            // door's OWN closed panels; any other blocking solid is suspect.
            // This is advisory only: functional door animation is a Play Mode task.
            if (d.enabled && haveBounds && problems.Count == 0)
            {
                Vector3 normal = d.transform.forward;
                normal.y = 0f;
                if (normal.sqrMagnitude < .5f) problems.Add("nieokreślony kierunek drzwi");
                else
                {
                    normal.Normalize();
                    var floor = new Vector3(bounds.center.x, bounds.min.y + .02f, bounds.center.z);
                    Vector3 start = floor - normal * 1.15f;
                    int n = Physics.CapsuleCastNonAlloc(start + Vector3.up * .42f,
                        start + Vector3.up * 1.65f, .28f, normal, casts, 2.3f,
                        maska, QueryTriggerInteraction.Ignore);
                    if (n == casts.Length) problems.Add("przepełniony bufor pomiaru przejścia");
                    else
                        for (int i = 0; i < n; i++)
                        {
                            var hit = casts[i].collider;
                            if (!hit || hit.gameObject.scene != scena ||
                                hit.transform.IsChildOf(d.transform) ||
                                hit.GetComponentInParent<BraxMieszkaniec>() ||
                                hit.GetComponentInParent<BraxPieszy>()) continue;
                            problems.Add("przejście blokuje " + hit.name);
                            if (problems.Count >= 3) break;
                        }
                }
            }
            report.Add(id + " typ=" + Klasa(d.id) + " aktywne=" + d.enabled +
                " skrzydła=" + (d.leaves == null ? 0 : d.leaves.Length) +
                " pozycja=" + d.transform.position.ToString("F2") +
                " status=" + (problems.Count == 0
                    ? "SONDA OK — otwieranie wymaga Play Mode" : string.Join("; ", problems)));
            foreach (var problem in problems)
                errors.Add("DO POPRAWKI: drzwi " + id + ": " + problem);
        }
        System.IO.Directory.CreateDirectory("city-review");
        System.IO.File.WriteAllLines("city-review/osiedle-drzwi-wszystkie.txt", report);
        Debug.Log("BRAXHORN_OSIEDLE_DRZWI_AUDYT sprawdzone=" + doors.Length +
            " zgloszone=" + errors.Count + " bez PlayMode=TAK");
        return errors;
    }

    static readonly List<string> raportSkaliAut = new List<string>();
    // Five shopfronts are open cut-outs, not BraxCityDoor components.
    // Track them explicitly: a door-only audit would miss a bricked-up market.
    static readonly List<(string nazwa, Vector3 prog, Vector3 doSrodka, bool zamkniete)> wejsciaSklepow =
        new List<(string, Vector3, Vector3, bool)>();

    // Cheap editor-scene probe. Reports actual openings and any obstruction;
    // never modifies scene objects or blocks Android compilation.
    static List<string> AudytWejscSklepow(Scene scena)
    {
        Physics.SyncTransforms();
        var bledy = new List<string>();
        var wiersze = new List<string> { "OSIEDLE B — 5 wejść pierzei, kontrola kapsułą bez renderu; odbiór telefonu nadal wymagany" };
        if (wejsciaSklepow.Count != 5)
            bledy.Add("DO POPRAWKI: wejścia sklepów B: zarejestrowano " + wejsciaSklepow.Count + "/5 lokali");
        var trafienia = new RaycastHit[32];
        const int mask = ~((1 << 8) | (1 << 2) | (1 << 29) | (1 << 31));
        foreach (var w in wejsciaSklepow)
        {
            var problem = new List<string>();
            if (w.zamkniete) problem.Add("wejście fizycznie zamknięte (brak zatwierdzonej paczki towaru)");
            if (w.doSrodka.sqrMagnitude < .9f || Mathf.Abs(w.doSrodka.y) > .01f)
                problem.Add("błędny kierunek wejścia");
            if (!w.zamkniete && problem.Count == 0)
            {
                var dir = w.doSrodka.normalized;
                var start = w.prog - dir * 1.25f;
                int count = Physics.CapsuleCastNonAlloc(start + Vector3.up * .42f,
                    start + Vector3.up * 1.6f, .28f, dir, trafienia, 2.5f,
                    mask, QueryTriggerInteraction.Ignore);
                if (count == trafienia.Length) problem.Add("przepełnienie sondy przejścia");
                else for (int i = 0; i < count; i++)
                {
                    var c = trafienia[i].collider;
                    if (!c || c.gameObject.scene != scena || c.GetComponentInParent<BraxMieszkaniec>()
                        || c.GetComponentInParent<BraxPieszy>()) continue;
                    problem.Add("kapsuła uderza w: " + c.name);
                    if (problem.Count >= 3) break;
                }
            }
            string wynik = w.nazwa + " próg=" + w.prog.ToString("F2") + " wynik=" +
                (problem.Count == 0 ? "SONDA WOLNA (wymaga Play Mode)" : string.Join("; ", problem));
            wiersze.Add(wynik);
            foreach (var p in problem) bledy.Add("DO POPRAWKI: lokal " + w.nazwa + ": " + p);
        }
        System.IO.Directory.CreateDirectory("city-review");
        System.IO.File.WriteAllLines("city-review/osiedle-wejscia-sklepow.txt", wiersze);
        Debug.Log("BRAXHORN_OSIEDLE_SKLEPY_WEJSCIA sprawdzone=" + wejsciaSklepow.Count +
            " bledy=" + bledy.Count + " (sonda geometrii, nie test telefonu)");
        return bledy;
    }
    static void RaportSkaliAut(List<AutoSzablon> auta)
    {
        raportSkaliAut.Clear();
        raportSkaliAut.Add("K=" + K.ToString("F4") + "; jednostki świata; metry rzeczywiste = jednostki/K");
        foreach (var a in auta)
            raportSkaliAut.Add("AUTO " + a.id + " długość=" + a.dl.ToString("F3") + " szerokość_z_lusterkami=" + a.szer.ToString("F3") + " wysokość=" + a.wys.ToString("F3"));
        float szer = auta.Count > 0 ? auta.Max(a => a.szer) : 0f;
        void Ulica(string nazwa, float pas)
        {
            float luz = pas - szer;
            raportSkaliAut.Add("DROGA " + nazwa + " jezdnia=" + (2f * pas).ToString("F3") + " pas=" + pas.ToString("F3") + " zapas_na_pas=" + luz.ToString("F3")
                + (luz < .4f * K ? " UWAGA: za mały zapas dla najszerszego auta" : ""));
        }
        if (ulice != null) foreach (var u in ulice) Ulica(u.nazwa ?? u.id, u.cw);
        Ulica("B-główna i B-N-S", Carriage);
        foreach (var line in raportSkaliAut) Debug.Log("BRAXHORN_SKALA_AUT " + line);
    }

    // Raport diagnostyczny; na polecenie właściciela błędy kontroli nie blokują APK do sprawdzenia na Motoroli.
    public static void SprawdzAktualizacje(Scene scene)
    {
        var bledy = new List<string>();
        if (!ruchZweryfikowany) bledy.Add("Nie wykonano testu ruchu");
        if (bladRuchu != null) bledy.Add(bladRuchu);
        // Nie maskuj niedostępnych modeli, shaderów, klipów ani pomieszczeń.
        // Dotąd zbierano wyłącznie "BŁĄD w kroku", pomijając wszystkie
        // jawne "DO POPRAWKI: ..." z wielu generatorów (auta, NPC, drzwi).
        bledy.AddRange(rep.notes.Where(n => !string.IsNullOrEmpty(n) &&
            (n.StartsWith("BŁĄD w kroku", StringComparison.OrdinalIgnoreCase)
             || n.StartsWith("DO POPRAWKI:", StringComparison.OrdinalIgnoreCase)
             || n.IndexOf("DO POPRAWKI:", StringComparison.OrdinalIgnoreCase) >= 0))
            .Distinct(StringComparer.Ordinal));
        // Do not discard overturned furniture, room deficiencies or missing
        // item colliders from BraxKontrola.Wszystko's separate scene report.
        // They remain advisory to APK production, never an extra build gate.
        bledy.AddRange(BraxKontrola.DoPoprawkiDoRaportuOsiedla());
        bledy.AddRange(AudytWejscSklepow(scene));
        bledy.AddRange(AudytZnakowDrogowych(scene));
        bledy.AddRange(AudytWszystkichDrzwi(scene));
        var renderery = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).ToArray();
        int szyldy = 0;
        foreach (var r in renderery)
        {
            if (!r.enabled) continue;
            var mf = r.GetComponent<MeshFilter>();
            if (mf && !mf.sharedMesh) bledy.Add("Brak siatki: " + r.name);
            bool maSiatke = mf && mf.sharedMesh || r is SkinnedMeshRenderer;
            if (!maSiatke) continue;
            if (r.sharedMaterials.Length == 0 || r.sharedMaterials.Any(m => !m || !m.shader
                || m.shader.name == "Hidden/InternalErrorShader" || !m.shader.isSupported))
                bledy.Add("Brak materiału albo shader nieobsługiwany: " + r.name);
            // Wideo osiedla: jaskraworóżowe auta. W Built-In nie można
            // pozostawić glTF/URP/ShaderGraph na żadnej części widocznego auta.
            if (r.transform.GetComponentsInParent<Transform>(true)
                .Any(t => t.name.StartsWith("Auto w ruchu", StringComparison.Ordinal)
                    || t.name.StartsWith("Auto zaparkowane", StringComparison.Ordinal)
                    || t.name.StartsWith("Auto Patryka", StringComparison.Ordinal)))
                foreach (var material in r.sharedMaterials)
                {
                    string shader = material && material.shader ? material.shader.name : "";
                    if (shader.IndexOf("gltf", StringComparison.OrdinalIgnoreCase) >= 0
                        || shader.IndexOf("Universal Render Pipeline", StringComparison.OrdinalIgnoreCase) >= 0
                        || shader.IndexOf("Shader Graphs/", StringComparison.OrdinalIgnoreCase) >= 0)
                        bledy.Add("Auto z shaderem powodującym różowy materiał: "
                            + r.name + " / " + shader);
                }
            if (r.name.StartsWith("szyld "))
            {
                szyldy++;
                if (!r.sharedMaterial || !r.sharedMaterial.mainTexture) bledy.Add("Brak obrazu szyldu: " + r.name);
            }
        }
        foreach (var nazwa in new[] { "salon_szyld", "stacja_pas", "stacja_pylon" })
            if (renderery.Count(r => r.name == "szyld " + nazwa && r.sharedMaterial && r.sharedMaterial.mainTexture) < 2)
                bledy.Add("Brak obu poprawnych stron szyldu " + nazwa);
        var mural = renderery.FirstOrDefault(r => r.name == "DJ: mural PULS CLUB (grafika właściciela)");
        if (!mural || mural.bounds.size.z < 29f || mural.bounds.size.y < 5f) bledy.Add("Mural nie pokrywa całej ściany Puls");
        var loze = renderery.SelectMany(r=>r.transform.GetComponentsInParent<Transform>(true))
            .Where(t=>t.name.StartsWith("loża właściciela ",StringComparison.Ordinal)).Distinct().ToArray();
        if (loze.Length != 14) bledy.Add("Niekompletny układ oryginalnych lóż Puls: " + loze.Length + "/14");
        // Weryfikacja WYMANY Patryka: dokładnie jeden model na scenie, jeden
        // kontroler i prawdziwe koła także w autach zaparkowanych.
        // Kontrola jest tania (bez renderowania / bez APK) i zapisuje błędy do raportu.
        var roots = scene.GetRootGameObjects();
        var gracze = roots.SelectMany(g => g.GetComponentsInChildren<BraxGymPlayer>(true)).ToArray();
        if (gracze.Length != 1) bledy.Add("Liczba graczy=" + gracze.Length + " (wymagany dokładnie jeden)");
        var gracz = gracze.FirstOrDefault();
        if (!gracz || !gracz.visual || gracz.visual.name != "Patryk")
            bledy.Add("Brak oryginalnego modelu Patryk przypisanego do gracza");
        var patryki = roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == "Patryk" && t.GetComponentInChildren<SkinnedMeshRenderer>(true))
            .ToArray();
        if (patryki.Length != 1)
            bledy.Add("Model Patryk: " + patryki.Length + " instancji (wymagana jedna, bez drugiego posągu)");
        // Druga figura może mieć zmienioną nazwę (np. "obsada", "statysta").
        // Zlicz również dokładne kopie siatki źródłowego Patryka, nie
        // myląc ich z innymi postaciami Rocketbox.
        if (gracz && gracz.visual)
        {
            var siatkiPatryka = new HashSet<Mesh>(gracz.visual
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(r => r.sharedMesh).Select(r => r.sharedMesh));
            var kopie = roots
                .SelectMany(g => g.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                .Where(r => r.sharedMesh && siatkiPatryka.Contains(r.sharedMesh)
                    && !r.transform.IsChildOf(gracz.visual)).ToArray();
            // Salon ma zamówione przez właściciela stanowisko PATRYK (sprzedawca).
            // Dopuszczamy TYLKO ten jeden nazwany egzemplarz, jeżeli odtwarza
            // ORYGINALNY klip "Typing" z ostatniej paczki 22 FBX.
            // Inne kopie siatki nadal są błędem, nawet pod zmienioną nazwą.
            var stanowiska = kopie
                .Select(r => r.transform.GetComponentsInParent<Transform>(true)
                    .FirstOrDefault(t => t.name == "Patryk (sprzedawca)"))
                .Where(t => t).Distinct().ToArray();
            if (stanowiska.Length > 1)
                bledy.Add("Patryk: więcej niż jedno oryginalne stanowisko sprzedawcy");
            foreach (var stanowisko in stanowiska)
            {
                var animacja = stanowisko.GetComponent<Animation>();
                if (!animacja || !animacja.GetClip("Typing"))
                    bledy.Add("Patryk: stanowisko sprzedawcy stoi bez oryginalnego FBX Typing");
            }
            var niedozwolone = kopie.Where(r =>
                !r.transform.GetComponentsInParent<Transform>(true)
                    .Any(t => t.name == "Patryk (sprzedawca)")).ToArray();
            if (niedozwolone.Length > 0)
                bledy.Add("Nieautoryzowana dodatkowa kopia modelu Patryka: " +
                    string.Join(", ", niedozwolone.Select(r => r.name).Distinct().Take(8)));
        }
        var samochodyGracza = roots.SelectMany(g => g.GetComponentsInChildren<BraxAutoGracza>(true)).ToArray();
        if (samochodyGracza.Length != 1)
            bledy.Add("Auto Patryka pod areną: " + samochodyGracza.Length + " zamiast jednego");
        foreach (var car in samochodyGracza)
        {
            if (!car.fotel || !car.obrys || !car.GetComponent<Rigidbody>())
                bledy.Add("Auto Patryka jest dekoracją: brak fotela / kolizji / napędu");
        }
        var wszystkieAuta = roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            // Kontroluj tylko instancje samochodów, nie grupę
            // "Auto Patryka / ARENA BRAXHORN". Kontener ma w dzieciach
            // renderery, ale nie ma kół bezpośrednio pod sobą i generował
            // cztery fałszywe błędy (LP/PP/LT/PT) przy każdym audycie.
            .Where(t => (t.name.StartsWith("Auto w ruchu · ", StringComparison.Ordinal)
                || t.name.StartsWith("Auto zaparkowane · ", StringComparison.Ordinal)
                || t.name.StartsWith("Auto Patryka · ", StringComparison.Ordinal))
                && t.GetComponentInChildren<MeshRenderer>(true))
            .ToArray();
        foreach (var auto in wszystkieAuta)
            foreach (string id in new[] { "LP", "PP", "LT", "PT" })
            {
                var wheel = auto.Find("koło " + id + "/obrót");
                var mf = wheel ? wheel.GetComponent<MeshFilter>() : null;
                var mr = wheel ? wheel.GetComponent<MeshRenderer>() : null;
                if (!mf || !mf.sharedMesh || mf.sharedMesh.vertexCount < 12 || !mr
                    || mr.sharedMaterials.Length == 0 || mr.sharedMaterials.Any(m => !m || !m.shader))
                    bledy.Add("Auto " + auto.name + ": brak prawidłowej siatki/materiału koła " + id);
            }
        Debug.Log("BRAXHORN_OSIEDLE_POSTAC_AUTA Patryk=" + patryki.Length
            + " auta_gracza=" + samochodyGracza.Length + " auta_widoczne=" + wszystkieAuta.Length);
        if (MiejscaParkingowe.Count == 0)
            bledy.Add("Brak wyznaczonych zatok parkingowych — nie można potwierdzić parkowania i auta Patryka");
        foreach (var m in MiejscaParkingowe)
            if (m.rozmiar.x < 3f * K - .001f || m.rozmiar.y < 5f * K - .001f) bledy.Add("Za mała zatoka " + m.srodek);
        try { TestTozsamosciMaterialu(); } catch (Exception e) { bledy.Add("Regresja materiału: " + e.Message); }
        try { TestKonfliktuDuzychAut(); } catch (Exception e) { bledy.Add("Regresja obrysów: " + e.Message); }
        System.IO.Directory.CreateDirectory("city-review");
        System.IO.File.WriteAllLines("city-review/skala-aut-i-drog.txt", raportSkaliAut.Concat(MiejscaParkingowe.Select(m =>
            "PARKING " + m.srodek.ToString("F2") + " szerokość=" + m.rozmiar.x.ToString("F3") + " długość=" + m.rozmiar.y.ToString("F3"))));
        System.IO.File.WriteAllLines("city-review/aktualizacja-osiedla.txt", new[] {
            "Kontrola sceny przed zapisem: szyldy=" + szyldy + ", loze=" + loze.Length + ", zatoki=" + MiejscaParkingowe.Count,
            "Wynik: " + (bledy.Count == 0 ? "PASS" : "FAIL"),
            "Obraz, animacje, autobus w ruchu i sterowanie: wymagany test APK na urządzeniu."
        }.Concat(bledy));
        if (bledy.Count > 0) Debug.LogWarning("BRAXHORN_AKTUALIZACJA_SCENA FAIL — " + bledy.Count + " błędów:\n" + string.Join("\n", bledy.Take(20)));
        else Debug.Log("BRAXHORN_AKTUALIZACJA_SCENA PASS szyldy=" + szyldy + " loze=" + loze.Length + " zatoki=" + MiejscaParkingowe.Count);
    }

    static void TestTozsamosciMaterialu()
    {
        string nazwa = "regresja_szyld_" + Guid.NewGuid().ToString("N");
        string path = null; GameObject go = null;
        try
        {
            var a = SalonZapisz(new Material(Shader.Find("Standard")), nazwa); path = AssetDatabase.GetAssetPath(a);
            go = new GameObject("test pierwszego lica szyldu"); var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = a;
            var b = SalonZapisz(new Material(Shader.Find("Standard")), nazwa);
            if (!r.sharedMaterial || r.sharedMaterial != b) throw new InvalidOperationException("Drugie lico zniszczyło materiał pierwszego szyldu");
        }
        finally { if (go) UnityEngine.Object.DestroyImmediate(go); if (path != null) AssetDatabase.DeleteAsset(path); }
    }

    static void TestKonfliktuDuzychAut()
    {
        var pkt = new List<Vector3>();
        for (int i = 0; i <= 80; i++) pkt.Add(new Vector3(-20f + i * .5f, 0, 0));
        for (int i = 0; i <= 80; i++) pkt.Add(new Vector3(20f - i * .5f, 0, 3f));
        var tr = new[] { new BraxTrasaAut { start = 0, ile = 81, dl = 40, krok = .5f }, new BraxTrasaAut { start = 81, ile = 81, dl = 40, krok = .5f } };
        var zd = new[] { new BraxZdarzenieAut { trasa = 0, typ = 0, wezel = 0, s0 = 18, s1 = 22, wlot = Vector3.right },
            new BraxZdarzenieAut { trasa = 1, typ = 0, wezel = 0, s0 = 18, s1 = 22, wlot = Vector3.left } };
        var sym = new BraxRuchAutSym(pkt.ToArray(), tr, zd, new[] { new BraxWezelAut() }, 1, 3.4f, 7f);
        if (sym.LiczbaKonfliktow != 1) throw new InvalidOperationException("Konflikt aut szerokości 3.4 m został pominięty");
    }
}
