using System;
using System.Linq;
using UnityEngine;

// BRAXHORN CITY / Unity 6 — lekki audyt sceny, uruchamiany podczas generowania miasta.
// Zgłasza błędy bez wyłączania losowych kolizji i bez przerabiania oryginalnych grafik.
internal static class BraxMiastoOdbior
{
    static int bledy;
    static void Blad(string komunikat)
    {
        bledy++;
        // Raport kontroli geometrii/drzwi/kolizji, a nie błąd kompilacji Unity.
        if (BraxBuildQuality.Advisory) BraxBuildQuality.Warn(komunikat);
        else Debug.LogError(komunikat);
    }

    public static void Wykonaj(Transform cityRoot)
    {
        try
        {
        bledy = 0;
        if (!cityRoot) throw new InvalidOperationException("BRAXHORN_MIASTO_ODBIOR brak korzenia miasta");
        Physics.SyncTransforms();
        var statki = cityRoot.GetComponentsInChildren<BraxVessel>(true);
        int J(string p) => statki.Count(v => v && v.name.StartsWith(p, StringComparison.Ordinal));
        int jachty = J("Marina: jacht"), motorowki = J("Marina: motorówka"), skutery = J("Marina: skuter wodny");
        string mar = "BRAXHORN_MIASTO_ODBIOR marina jachty=" + jachty + " motorowki=" + motorowki + " skutery=" + skutery;
        if (jachty < 3 || motorowki < 6 || skutery < 4)
            Blad(mar + " — NIEGOTOWE: minimum 3 jachty, 6 motorówek i 4 skutery");
        else Debug.Log(mar);

        var renderery = cityRoot.GetComponentsInChildren<MeshRenderer>(true);
        bool Mural(string nazwa) => renderery.Any(r => r && r.enabled && r.name == nazwa &&
            r.sharedMaterial && r.sharedMaterial.mainTexture);
        bool muralGym = Mural("mural_19"), muralNova = Mural("mural mural_nova");
        string mur = "BRAXHORN_MIASTO_ODBIOR mural_19=" + muralGym + " mural_NOVA_za_DJ=" + muralNova;
        if (!muralGym || !muralNova) Blad(mur + " — NIEGOTOWE: brak oryginalnej grafiki na ścianie");
        else Debug.Log(mur);

        // Stopy gracza: promień 0.30 m, wysokość 2.21 m, przejście przez całe światło drzwi.
        // Pomijamy skrzydła zamknięte w trybie edycji; inne statyczne przeszkody są usterką.
        SprawdzWejscie("NOVA", 42.0f, .15f, 35.73f, 36.60f);
        SprawdzWejscie("TOWER", 59.0f, .15f, 35.83f, 36.88f);
        SprawdzWejscie("MIESZKANIE_BRAXA_KLATKA", 33.73f, .15f, 35.72f, 36.65f);
        SprawdzMieszkanieBraxa(cityRoot);
        SprawdzApteke(cityRoot);
        SprawdzTaras360(cityRoot);
        SprawdzTramT1(cityRoot);
        SprawdzPojazdyWodne(cityRoot);
        SprawdzMetroM1(cityRoot);
        SprawdzStanowiskaWodne(cityRoot);
        // Geometria i drożność w trybie testowym są oceniane w APK przez właściciela.
        // Wszystkie odchylenia pozostają w raporcie, bez blokowania kompilacji.
        if (bledy != 0)
            throw new InvalidOperationException(
                "BRAXHORN_MIASTO_ODBIOR_NIEGOTOWE blokery=" + bledy +
                " — wymaga ręcznej oceny w APK, a nie automatycznej blokady budowy");

        }
        catch (System.InvalidOperationException qualityError)
        {
            BraxBuildQuality.Issue(qualityError);
        }
    }

    static void SprawdzApteke(Transform cityRoot)
    {
        var budynek = cityRoot.Find("Building apothecary");
        if (!budynek)
        {
            Blad("BRAXHORN_APTEKA_ODBIOR NIEGOTOWA: nie znaleziono Building apothecary");
            return;
        }
        var wszystkie = budynek.GetComponentsInChildren<Transform>(true);
        var leki = wszystkie.Where(t => t && t.name.StartsWith("Towar: lek_", StringComparison.Ordinal)).ToArray();
        var rodzaje = leki.Select(t => t.name.Substring("Towar: ".Length))
            .Select(n => n.Length >= 6 ? n.Substring(0, 6) : n).Distinct().Count();
        var drzwi = budynek.GetComponentsInChildren<BraxCityDoor>(true)
            .FirstOrDefault(d => d && d.id == "R1_PUBLIC_DOOR_apothecary");
        bool drzwiGotowe = drzwi && drzwi.gameObject.activeInHierarchy &&
            drzwi.leaves != null && drzwi.leaves.Length > 0;
        // Po imporcie GLB sprawdzamy nie tylko bryły, ale i przypięte tekstury.
        // Jest to kontrola obecności tekstur; czytelność etykiet potwierdza telefon.
        int zTeksturami = leki.Count(g => g.GetComponentsInChildren<Renderer>(true)
            .Any(r => r && r.sharedMaterials.Any(m => m && m.GetTexturePropertyNames()
                .Any(n => m.GetTexture(n)))));
        string log = "BRAXHORN_APTEKA_ODBIOR czynne_drzwi=" + drzwiGotowe +
            " modele_lekow=" + leki.Length + " rozne_rodzaje=" + rodzaje +
            " z_teksturami=" + zTeksturami +
            " szyld=SOBCZAK/APTEKA (wizualna kontrola grafiki osobno)";
        if (!drzwiGotowe || leki.Length < 12 || rodzaje < 5 || zTeksturami < 12)
            Blad(log + " NIEGOTOWA (wymagane działające drzwi oraz opakowania z etykietami)");
        else
            Debug.Log(log + " KOD_MODELI_OK (widok i wejście nadal testuje Android)");
        SprawdzWejscie("APTEKA", 45.0f, .15f, 19.9f, 20.95f);
    }

    static void SprawdzTaras360(Transform cityRoot)
    {
        var wind = cityRoot.GetComponentsInChildren<BraxWinda>(true)
            .FirstOrDefault(w => w && w.nazwa == "BRAXHORN TOWER");
        bool windaNaDach = wind && wind.pietraY != null && wind.pietraY.Length > 36 &&
                           wind.uzytkowe != null && wind.uzytkowe.Length > 36 &&
                           wind.uzytkowe[0] && wind.uzytkowe[36];
        var taras = cityRoot.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t && t.name == "Tower: taras widokowy na dachu");
        int plyty = taras ? taras.GetComponentsInChildren<BoxCollider>(true)
            .Count(c => c.name.StartsWith("Taras: kolizja", StringComparison.Ordinal)) : 0;
        var panorama = cityRoot.GetComponentInChildren<BraxPierscien>(true);
        int elementy = panorama && panorama.elementy != null ? panorama.elementy.Length : 0;
        var sky = RenderSettings.skybox;
        Texture niebo = sky && sky.HasProperty("_NieboTex") ? sky.GetTexture("_NieboTex") : null;
        string rozdz = niebo ? niebo.width + "x" + niebo.height : "BRAK";
        bool hd = niebo && niebo.width >= 4096 && niebo.height >= 2048;
        string log = "BRAXHORN_TOWER_PANORAMA_360 winda_na_tarAS=" + windaNaDach +
            " segmenty_podlogi=" + plyty + " widoki_panoramy_3D=" + elementy +
            " niebo=" + rozdz + " HD=" + hd +
            " (kamera na dachu 580m; brak gwarancji wyglądu bez testu Android)";
        if (!windaNaDach || plyty != 4 || elementy < 5 || !niebo)
            Blad(log + " — NIEGOTOWE");
        else if (!hd)
            Debug.LogWarning(log + " — panorama jest, ale nie potwierdzono nieba minimum 4K");
        else
            Debug.Log(log + " — elementy dostępne do testu z dachu");
    }


    static void SprawdzTramT1(Transform cityRoot)
    {
        var tram = cityRoot.GetComponentInChildren<BraxTramwajT1>(true);
        if (!tram)
        {
            Blad("BRAXHORN_TRAM_T1_NIEGOTOWY: brak pojazdu i dynamicznej linii");
            return;
        }
        int n = tram.trasa == null ? 0 : tram.trasa.Length;
        int p = tram.przystankiIndeksy == null ? 0 : tram.przystankiIndeksy.Length;
        int znaki = cityRoot.GetComponentsInChildren<Transform>(true)
            .Count(t => t && t.name.StartsWith("T1: przystanek ", StringComparison.Ordinal));
        int szyny = cityRoot.GetComponentsInChildren<MeshRenderer>(true)
            .Count(t => t && t.name.StartsWith("T1: szyny jezdne", StringComparison.Ordinal));
        int drzwi = tram.skrzydlaDrzwi == null ? 0 : tram.skrzydlaDrzwi.Length;
        float dlugosc = 0f;
        if (n > 1) for (int i = 0; i < n; i++)
            dlugosc += Vector3.Distance(tram.trasa[i], tram.trasa[(i + 1) % n]);
        float domkniecie = n > 1 ? Vector3.Distance(tram.trasa[n - 1], tram.trasa[0]) : float.PositiveInfinity;
        bool pasazerowie = tram.GetComponent<BraxTramPasazerowie>();
        bool f = n > 100 && p == 6 && znaki == 6 && szyny >= 4 &&
                 dlugosc > 900f && domkniecie < 3f && tram.miejsceGracza &&
                 pasazerowie;
        string raport = "BRAXHORN_TRAM_T1_ODBIOR pętla_m=" + dlugosc.ToString("F1") +
            " domknięcie_m=" + domkniecie.ToString("F2") +
            " punktów=" + n + " przystanków=" + p +
            " znaków=" + znaki + " siatek_szyn=" + szyny +
            " niezależne_skrzydła_drzwi=" + drzwi +
            " siedzenie=" + (tram.miejsceGracza != null) +
            " choreografia_NPC=" + pasazerowie;
        if (!f) Blad(raport + " — NIEGOTOWE");
        else if (drzwi < 2)
            Debug.LogWarning(raport + " — BRAK ruchomych drzwi w oryginalnym CAF; NPC nie przechodzą przez zamknięty pojazd");
        else Debug.Log(raport + " — GOTOWE W KODZIE do testu Android");
    }

    static void SprawdzMetroM1(Transform cityRoot)
    {
        // Zakres właściciela 6.11: metro pieszo, peron i dwa tory do dalszej
        // rozbudowy. Pociąg nie kursuje i nie jest wymagany na tym etapie.
        var stacja = cityRoot.GetComponentsInChildren<BraxWidocznoscWnetrza>(true)
            .FirstOrDefault(s => s && s.nazwa == "metro M1 NABRZEŻE");
        if (!stacja)
        {
            Blad("BRAXHORN_METRO_M1_NIEGOTOWE: brak podziemnej stacji M1");
            return;
        }
        Bounds Scal(Bounds a, Bounds b) { a.Encapsulate(b); return a; }
        var pojazd = cityRoot.GetComponentInChildren<BraxMetroM1Ruch>(true);
        if (pojazd)
        {
            if (pojazd.kursyWlaczone)
                Blad("BRAXHORN_METRO_M1_ETAP: zakaz uruchamiania kursow bez decyzji wlasciciela");
            var pasazerowie = pojazd.GetComponent<BraxMetroM1Pasazerowie>();
            if (pasazerowie && pasazerowie.enabled)
                Blad("BRAXHORN_METRO_M1_ETAP: symulacja pasazerow ma pozostac wylaczona");
            // Skrajnia dotyczy oryginalnych siatek wagonu, a nie
            // osobnych paneli scenicznych udających drzwi (dzieci korzenia pociągu).
            // Panele wystają przed model i fałszywie zgłaszały kolizję z peronem.
            var wagony = pojazd.GetComponentsInChildren<Transform>(true)
                .Where(t => t && t.parent == pojazd.transform &&
                    t.name.StartsWith("metro M1 — wagon ", StringComparison.Ordinal)).ToArray();
            if (wagony.Length == 0)
                Blad("BRAXHORN_METRO_M1_REZERWA: brak oryginalnego modelu wagonu na torze");
            Bounds? wagon = null;
            foreach (var w in wagony)
                foreach (var r in w.GetComponentsInChildren<Renderer>(true))
                    if (r) wagon = wagon.HasValue ? Scal(wagon.Value, r.bounds) : r.bounds;
            if (wagon.HasValue &&
                (wagon.Value.min.y < -12.83f || wagon.Value.max.y > -5.25f ||
                 wagon.Value.min.z < 34.18f || wagon.Value.max.z > 38.80f))
                Blad("BRAXHORN_METRO_M1_SKRAJNIA: nieruchomy model wystaje poza tunel lub na peron");
            Debug.Log("BRAXHORN_METRO_M1_POSTOJ_TECHNICZNY kursy=" + pojazd.kursyWlaczone +
                " pasazerowie=" + (pasazerowie && pasazerowie.enabled));
        }

        var renderery = stacja.GetComponentsInChildren<MeshRenderer>(true);
        bool MaMaterial(string nazwa) => renderery.Any(r => r && r.sharedMaterial &&
            r.sharedMaterial.name.IndexOf(nazwa, StringComparison.OrdinalIgnoreCase) >= 0);
        bool peronSiatka = MaMaterial("metro posadzka granit") &&
                          MaMaterial("metro krawędź peronu");
        bool torySiatka = MaMaterial("metro szyna") &&
                         MaMaterial("metro podkłady betonowe");
        var szyny = renderery.Where(r => r && r.sharedMaterial &&
            r.sharedMaterial.name.IndexOf("metro szyna", StringComparison.OrdinalIgnoreCase) >= 0)
            .Select(r => r.bounds).ToArray();
        bool ZasiegSzyn(float x, float z) => szyny.Any(b =>
            b.min.x <= x + .1f && b.max.x >= x - .1f &&
            b.min.z <= z + .1f && b.max.z >= z - .1f);
        // Sprawdzenie obecności geometrii obu torów także w obu kierunkach
        // tuneli. Bounds jest kontrolą statyczną, nie próbą przejazdu.
        bool toryKontynuacja = new[] { -40f, 0f, 40f, 80f }
            .All(x => ZasiegSzyn(x, 20.9f) && ZasiegSzyn(x, 36.1f));
        bool PodlogaPeronu(float x, float z)
        {
            return Physics.RaycastAll(new Vector3(x, -10.05f, z), Vector3.down,
                2.4f, ~0, QueryTriggerInteraction.Ignore)
                .Any(h => h.collider && h.normal.y > .65f &&
                    Mathf.Abs(h.point.y - (-11.2f)) < .22f);
        }
        int probyPodlogi = 0, brakPodlogi = 0;
        foreach (float x in new[] { -14f, 0f, 20f, 40f })
            foreach (float z in new[] { 26.4f, 30.6f })
            {
                probyPodlogi++;
                if (!PodlogaPeronu(x, z)) brakPodlogi++;
            }
        var bramki = stacja.GetComponentsInChildren<BraxDrzwiPrzesuwne>(true)
            .Where(d => d && d.etykieta != null &&
                d.etykieta.StartsWith("bramka metra", StringComparison.OrdinalIgnoreCase)).ToArray();
        bool bramkiRuch = bramki.Length == 7 && bramki.All(d =>
            d.skrzydla != null && d.skrzydla.Length >= 2 &&
            d.przesuniecie != null && d.przesuniecie.Length == d.skrzydla.Length &&
            d.blokady != null && d.blokady.Length > 0);
        var log = "BRAXHORN_METRO_M1_INFRASTRUKTURA peron=" + peronSiatka +
            " podloga_peronu=" + (probyPodlogi - brakPodlogi) + "/" + probyPodlogi +
            " tory=" + torySiatka + " ciaglosc_2_torow=" + toryKontynuacja +
            " bramki=" + bramki.Length + "/7" + " bramki_ruch=" + bramkiRuch +
            " kursy=CELOWO_WYLACZONE; sprawdzic wejscie i przejscie na telefonie";
        if (!peronSiatka || !torySiatka || !toryKontynuacja ||
            brakPodlogi > 0 || !bramkiRuch)
            Blad(log + " NIEGOTOWE");
        else Debug.Log(log + " KOD_GEOMETRII_OK");

        // R1 6.11: PORT zamiast zatkanego chodnika. Nie sprawdzać starego wejścia x=69,z=34.3.
        // Kontrola korytarza z portu jest w BraxClaudeMetro.KontrolaPrzejscia(), tu
        // mierzymy dojście w terenie i dawne miejsce wejścia (musi być wolne).
        const float wejX = 70.3f, yChod = .15f, zC = 17.25f;
        bool lad = BraxMiastoGranice.Lad(wejX, zC);
        const int maska = ~((1 << 8) | (1 << 2) | (1 << 29) | (1 << 31));
        bool Podloga(float x, float z)
        {
            return Physics.RaycastAll(new Vector3(x, yChod + 1.25f, z), Vector3.down, 2.1f,
                maska, QueryTriggerInteraction.Ignore)
                .Any(h => h.collider && h.normal.y > .65f &&
                    Mathf.Abs(h.point.y - yChod) < .22f);
        }
        bool Przejscie(float x, float z)
        {
            foreach (var c in Physics.OverlapCapsule(
                new Vector3(x, yChod + .39f, z),
                new Vector3(x, yChod + 2.01f, z),
                .30f, maska, QueryTriggerInteraction.Ignore))
            {
                if (!c || !c.enabled || c.isTrigger ||
                    c.GetComponentInParent<BraxGymPlayer>() ||
                    c.GetComponentInParent<BraxCityDoor>() ||
                    c.GetComponentInParent<BraxDrzwiPrzesuwne>()) continue;
                return false;
            }
            return true;
        }

        int problemyDojscia = 0, problemyStaregoChodnika = 0;
        // Publiczny plac od ulicy do nowego wejścia.
        for (float z = 20.1f; z >= 17.2f; z -= .3f)
            foreach (float x in new[] { wejX - .48f, wejX, wejX + .48f })
                if (!Podloga(x, z) || !Przejscie(x, z))
                {
                    problemyDojscia++;
                    if (problemyDojscia <= 5)
                        Blad("BRAXHORN_METRO_PORT_DOJSCIE_NIEGOTOWE @" +
                            new Vector3(x, yChod, z).ToString("F2"));
                }

        // Dawny chodnik od początku do końca wzdłuż ulicy: 2 pasy bez schodów i ścian.
        for (float x = 67.2f; x <= 80.5f; x += .45f)
            foreach (float z in new[] { 34.0f, 35.3f })
                if (!Podloga(x, z) || !Przejscie(x, z))
                {
                    problemyStaregoChodnika++;
                    if (problemyStaregoChodnika <= 5)
                        Blad("BRAXHORN_METRO_STARY_CHODNIK_ZABLOKOWANY @" +
                            new Vector3(x, yChod, z).ToString("F2"));
                }

        string wynik = "BRAXHORN_METRO_M1_WEJSCIE_PORT " +
            "po_usunietym_biurze_x=70.3_z=16.5 na_ladzie=" + lad +
            " blokery_dojscia=" + problemyDojscia +
            " blokery_starego_chodnika=" + problemyStaregoChodnika +
            " (schody i przejście do peronu muszą przejść test w APK)";
        if (!lad || problemyDojscia != 0 || problemyStaregoChodnika != 0)
            Blad(wynik + " NIEGOTOWE");
        else Debug.Log(wynik + " KOD_GEOMETRII_OK");
    }

    // Odbiór modelowy Braxa w Marinie: nie dopuszczamy do starego zdjęcia
    // z krzywo siedzącą postacią. Sama geometria nie zastępuje testu telefonu,
    // ale wyłapuje brak źródłowych punktów, złą wysokość i brak dostępu.
    static void SprawdzStanowiskaWodne(Transform cityRoot)
    {
        var all = cityRoot.GetComponentsInChildren<BraxVessel>(true);
        int jac = all.Count(v => v && v.label == "JACHT");
        int riv = all.Count(v => v && v.label == "MOTORÓWKA");
        int jet = all.Count(v => v && v.label == "SKUTER");
        int bez = 0;
        foreach (var v in all)
        {
            if (!v) continue;
            bool siedzisko = v.seat.y > -.25f &&
                v.seat.y < 10.5f &&
                Mathf.Abs(v.seat.z) <= v.length * .52f;
            if (v.label == "MOTORÓWKA")
                siedzisko &= Mathf.Abs(v.seat.x) > .12f; // prawy fotel assise_4, nie środek kabiny
            if (v.label == "JACHT")
                siedzisko &= v.walkableDeck &&
                    v.helmCameraLocal.y > v.seat.y &&
                    Mathf.Abs(v.deckEntry.z) <= v.length * .48f;
            if (!siedzisko)
            {
                bez++;
                Blad("BRAXHORN_MARINA_STER_NIEGOTOWY " + v.name +
                    " seat=" + v.seat.ToString("F2") +
                    " poklad=" + v.walkableDeck +
                    " kamera=" + v.helmCameraLocal.ToString("F2"));
            }
        }
        string msg = "BRAXHORN_MARINA_PASAZER_KONTROLA jachty=" + jac +
            " motorowki=" + riv + " skutery=" + jet +
            " błędne_stanowiska=" + bez +
            " (Riva: fotel assise_4; skuter: siodło; jacht: pokład + ster + kamera wewnętrzna)";
        if (jac < 3 || riv < 6 || jet < 4 || bez > 0)
            Blad(msg + " NIEGOTOWE — nie zatwierdzać publikacji APK");
        else
            Debug.Log(msg + " GOTOWE W KODZIE do rzeczywistego testu Android");
    }

    static void SprawdzPojazdyWodne(Transform cityRoot)
    {
        var statki = cityRoot.GetComponentsInChildren<BraxVessel>(true);
        var motory = statki.Where(v => v && v.label == "MOTORÓWKA").ToArray();
        var skutery = statki.Where(v => v && v.label == "SKUTER").ToArray();
        var jachty = statki.Where(v => v && v.label == "JACHT").ToArray();

        // Model Riva ma osobny prawy fotel sternika. X=0 to środek kadłuba,
        // w którym Brax na zdjęciu użytkownika wyglądał, jakby klęczał.
        int fotelRiva = motory.Count(v => v.sit &&
            Mathf.Abs(v.seat.x) > Mathf.Max(.10f, v.width * .075f));
        int siodloSkuter = skutery.Count(v => v.sit &&
            v.seat.y > .20f && v.seat.z > -v.length * .5f);
        int poklady = jachty.Count(v => v.walkableDeck &&
            v.deckEntry.z >= -v.length * .5f &&
            v.deckEntry.z < v.seat.z &&
            v.helmCameraLocal.y > v.seat.y + .3f);
        string raport = "BRAXHORN_WODA_FOTELE motorówki=" + motory.Length +
            " fotel_sternika_z_modelu=" + fotelRiva +
            " skutery=" + skutery.Length + " siodła=" + siodloSkuter +
            " jachty=" + jachty.Length + " pokład_i_kamera_za_sterem=" + poklady +
            " (wysokość, poza i widok nadal do sprawdzenia w APK)";
        if (fotelRiva < motory.Length || siodloSkuter < skutery.Length ||
            poklady < jachty.Length || motory.Length < 1 || skutery.Length < 1 || jachty.Length < 3)
            Blad(raport + " NIEGOTOWE");
        else Debug.Log(raport + " KOD_GOTOWY_DO_TESTU");
    }

    // R1 6.11: wyjątkowo krótka, nieblokująca kontrola PO dołożeniu
    // wtórnych colliderów przez BraxKontrola.Wszystko(). Starszy audyt
    // obejmował tylko 4 próbki samego progu, nie dojście do sali klubu.
    // Nie wyłącza ani nie kasuje żadnej istniejącej kolizji.
    public static void SprawdzNovaPoKontroliKolizji(Transform cityRoot)
    {
        if (!cityRoot)
        {
            Debug.LogWarning("BRAXHORN_NOVA_R1_PO_KOLIZJACH NIEGOTOWE: brak korzenia miasta");
            return;
        }
        Physics.SyncTransforms();
        int bezPodlogi = 0, blokady = 0, próbki = 0;
        const int maska = ~((1 << 29) | (1 << 31));
        var trasa = new[] {
            new Vector3(42f, .15f, 35.75f),  // chodnik przed wejściem
            new Vector3(42f, .15f, 36.28f),  // drzwi rozsuwane
            new Vector3(42f, .15f, 37.30f),  // przedsionek
            new Vector3(42f, .15f, 39.90f),  // przejście przy szatni
            new Vector3(42f, .15f, 41.85f),  // otwór do sali
            new Vector3(42f, .15f, 43.25f),  // sala
            new Vector3(42f, .15f, 46.00f)   // parkiet
        };
        for (int i = 0; i + 1 < trasa.Length; i++)
        {
            int kroki = Mathf.Max(1, Mathf.CeilToInt(
                Vector3.Distance(trasa[i], trasa[i + 1]) / .35f));
            for (int j = 0; j <= kroki; j++)
            {
                Vector3 p = Vector3.Lerp(trasa[i], trasa[i + 1], j / (float)kroki);
                próbki++;
                bool podloga = Physics.RaycastAll(p + Vector3.up * .9f,
                    Vector3.down, 1.65f, maska, QueryTriggerInteraction.Ignore)
                    .Any(h => h.collider && h.normal.y > .65f &&
                        Mathf.Abs(h.point.y - p.y) <= .24f);
                if (!podloga)
                {
                    bezPodlogi++;
                    if (bezPodlogi <= 4) Debug.LogWarning(
                        "BRAXHORN_NOVA_R1_PO_KOLIZJACH BRAK_PODLOGI @" + p.ToString("F2"));
                }
                foreach (var c in Physics.OverlapCapsule(p + Vector3.up * .55f,
                    p + Vector3.up * 1.91f, .30f, maska, QueryTriggerInteraction.Ignore))
                {
                    if (!c || !c.enabled || c.isTrigger ||
                        c.GetComponentInParent<BraxGymPlayer>() ||
                        c.GetComponentInParent<BraxCityDoor>() ||
                        c.GetComponentInParent<BraxDrzwiPrzesuwne>() ||
                        c.GetComponentInParent<BraxDrzwiAutomatyczne>()) continue;
                    blokady++;
                    if (blokady <= 6) Debug.LogWarning(
                        "BRAXHORN_NOVA_R1_PO_KOLIZJACH PRZESZKODA " +
                        c.name + " @" + p.ToString("F2"));
                }
            }
        }
        var wynik = "BRAXHORN_NOVA_R1_PO_KOLIZJACH proby=" + próbki +
            " brak_podlogi=" + bezPodlogi + " przeszkody=" + blokady +
            " (bez automatycznej ingerencji; weryfikacja Android nadal konieczna)";
        if (bezPodlogi != 0 || blokady != 0) Debug.LogWarning(wynik);
        else Debug.Log(wynik);
        // Kontrola klatki i mieszkania powtórzona po globalnym dodaniu kolizji.
        SprawdzMieszkanieBraxa(cityRoot);
    }

    // R1: krótki audyt rzeczywistej drożności klatki i pomieszczeń.
    // Sam model drzwi nie wystarcza: sprawdzamy pionową kapsułę Braxa i podłogę
    // także na spocznikach, przy wejściu do salonu, łazienki i sypialni.
    // To pomiar geometrii w czasie generowania Unity, NIE test chodzenia Android.
    static void SprawdzMieszkanieBraxa(Transform cityRoot)
    {
        var flat = cityRoot.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t && t.name.StartsWith("CLAUDE mieszkanie Braxa nad NOVA", StringComparison.Ordinal));
        if (!flat)
        {
            Blad("BRAXHORN_MIESZKANIE_BRAXA_TRASA NIEGOTOWE: brak klatki i mieszkania");
            return;
        }
        var proby = new[] {
            ("ULICA", new Vector3(33.73f, .15f, 36.85f)),
            ("KLATKA_PARTER", new Vector3(33.20f, .15f, 37.44f)),
            ("SCHODY_BIEG_1", new Vector3(33.20f, 1.29f, 38.80f)),
            ("SPOCZNIK", new Vector3(33.20f, 2.625f, 40.80f)),
            ("SCHODY_BIEG_2", new Vector3(34.85f, 3.76f, 39.20f)),
            ("PIETRO_PODEST", new Vector3(35.60f, 5.10f, 37.25f)),
            ("DRZWI_MIESZKANIA", new Vector3(38.05f, 5.10f, 37.40f)),
            ("PRZEJSCIE_SALON", new Vector3(40.95f, 5.10f, 37.80f)),
            ("SALON", new Vector3(44.50f, 5.10f, 40.30f)),
            ("LAZIENKA", new Vector3(39.20f, 5.10f, 40.20f)),
            ("ANEKS", new Vector3(44.00f, 5.10f, 42.20f)),
            ("SYPIALNIA", new Vector3(48.40f, 5.10f, 41.55f))
        };
        int brakPodlogi = 0, przeszkody = 0, liczba = 0;
        foreach (var (nazwa, p) in proby)
        {
            liczba++;
            bool podloga = Physics.RaycastAll(p + Vector3.up * .85f,
                    Vector3.down, 1.6f, ~((1 << 29) | (1 << 31)),
                    QueryTriggerInteraction.Ignore)
                .Any(h => h.collider && h.normal.y > .65f &&
                          Mathf.Abs(h.point.y - p.y) < .26f);
            if (!podloga)
            {
                brakPodlogi++;
                Debug.LogWarning("BRAXHORN_MIESZKANIE_BRAXA_TRASA BRAK_PODLOGI " + nazwa +
                    " @" + p.ToString("F2"));
            }
            foreach (var c in Physics.OverlapCapsule(p + Vector3.up * .5f,
                p + Vector3.up * 1.91f, .30f,
                ~((1 << 29) | (1 << 31)), QueryTriggerInteraction.Ignore))
            {
                if (!c || !c.enabled || c.isTrigger ||
                    c.GetComponentInParent<BraxGymPlayer>() ||
                    c.GetComponentInParent<BraxCityDoor>() ||
                    c.GetComponentInParent<BraxDrzwiPrzesuwne>()) continue;
                przeszkody++;
                if (przeszkody <= 12)
                    Debug.LogWarning("BRAXHORN_MIESZKANIE_BRAXA_TRASA PRZESZKODA " +
                        nazwa + " " + c.name + " @" + p.ToString("F2"));
            }
        }
        string wynik = "BRAXHORN_MIESZKANIE_BRAXA_TRASA proby=" + liczba +
            " brak_podlogi=" + brakPodlogi + " przeszkody=" + przeszkody +
            " (bez automatycznego usuwania kolizji; wymagany odbiór Android)";
        if (brakPodlogi > 0 || przeszkody > 0) Debug.LogWarning(wynik);
        else Debug.Log(wynik);
    }

    static void SprawdzWejscie(string nazwa, float x, float y, float zA, float zB)
    {
        int przeszkody = 0;
        for (float z = zA; z <= zB + .001f; z += .23f)
        {
            Vector3 stopa = new Vector3(x, y, z);
            foreach (var col in Physics.OverlapCapsule(stopa + Vector3.up * .55f,
                         stopa + Vector3.up * 1.91f, .30f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!col || !col.enabled || col.GetComponentInParent<BraxDrzwiPrzesuwne>() ||
                    col.GetComponentInParent<BraxDrzwiAutomatyczne>() ||
                    col.GetComponentInParent<BraxGymPlayer>()) continue;
                var manualne = col.GetComponentInParent<BraxCityDoor>();
                if (manualne && manualne.id != "NOVA_SOURCE_ENTRY") continue;
                przeszkody++;
                if (przeszkody <= 4)
                    Debug.LogWarning("BRAXHORN_MIASTO_ODBIOR " + nazwa +
                        " przeszkoda: " + col.name + " @" + stopa.ToString("F2"));
            }
        }
        if (przeszkody > 0)
            Blad("BRAXHORN_MIASTO_ODBIOR " + nazwa + " = NIEDROŻNE, kolizje=" + przeszkody);
        else Debug.Log("BRAXHORN_MIASTO_ODBIOR " + nazwa +
                       " = bez przeszkód statycznych w osi wejścia (nadal wymagany test telefonu)");
    }
}
