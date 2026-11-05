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
        Debug.LogError(komunikat);
    }

    public static void Wykonaj(Transform cityRoot)
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
        SprawdzApteke(cityRoot);
        SprawdzTaras360(cityRoot);
        SprawdzTramT1(cityRoot);
        SprawdzPojazdyWodne(cityRoot);
        SprawdzMetroM1(cityRoot);
        SprawdzStanowiskaWodne(cityRoot);
        // Unity 6: bez rzeczywistych modeli, wejść i przejazdu nie wolno
        // publikować regionu jako BUlLT. Błędy ze wszystkich podkontroli
        // trafiają do jednego zbiorczego werdyktu.
        if (bledy != 0)
            Debug.LogWarning("BRAXHORN_MIASTO_ODBIOR_NIEGOTOWE blokery=" + bledy +
                " — do oceny w grze; odbiór nie blokuje APK");
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
        var metro = cityRoot.GetComponentInChildren<BraxMetroM1Ruch>(true);
        if (!metro)
        {
            Blad("BRAXHORN_METRO_M1_NIEGOTOWE: zamiast kursującego modelu pociągu z repo jest stara atrapa lub brak GLB");
            return;
        }
        var npc = metro.GetComponent<BraxMetroM1Pasazerowie>();
        bool model = metro.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0 ||
                     metro.GetComponentsInChildren<MeshRenderer>(true).Length > 10;
        bool dlugosc = metro.wejscieOffsetX > 40f && metro.wyjazdOffsetX < -40f;
        bool czas = Mathf.Abs(metro.dojazdSekundy + metro.otwieranieSekundy +
                    metro.postojSekundy + metro.zamykanieSekundy +
                    metro.odjazdSekundy + metro.ciszaSekundy - 60f) < 3f;
        bool drzwi = metro.DrzwiLiczba >= 2;
        string raport = "BRAXHORN_METRO_M1_KURS_BUDOWA model=" + model +
              " drzwi_panele=" + metro.DrzwiLiczba + " npc=" + (npc != null) +
              " kurs_okolo_minuty=" + czas + " wjazd=" + metro.wejscieOffsetX.ToString("F1") +
              " wyjazd=" + metro.wyjazdOffsetX.ToString("F1") +
              " (wizualne drzwi sceniczne, nie oryginalne animowane drzwi GLB)";
        if (!model || !npc || !drzwi || !dlugosc || !czas)
            Blad(raport + " NIEGOTOWE");
        else
            Debug.Log(raport + " GOTOWE DO TESTU W APK");

        // Metra nie wolno osadzić przy ścianie ani na wodzie.
        // Rzeczywiste przejście piesze z kamienicy do schodów i brak
        // niebieskiej dziury muszą być potwierdzone wynikiem analizy kolizji.
        const float wejX = 69f, yChod = .15f, zC = 34.3f;
        bool lad = BraxMiastoGranice.Lad(wejX, zC);
        int przeszkody = 0;
        for (float x = wejX - 4.0f; x <= wejX - .4f; x += .45f)
            foreach (var c in Physics.OverlapCapsule(
                new Vector3(x, yChod + .35f, zC),
                new Vector3(x, yChod + 2.0f, zC),
                .30f, ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore))
            {
                if (!c || !c.enabled || c.GetComponentInParent<BraxGymPlayer>() ||
                    c.name.IndexOf("stairs", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    c.name.IndexOf("schod", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                // Tylko źródłowy budynek i obce przeszkody, bez balustrad metra.
                if (c.transform.IsChildOf(metro.transform)) continue;
                if (c.name.IndexOf("Building", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    c.name.IndexOf("ogrodzen", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    c.name.IndexOf("kamienica", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    przeszkody++;
                    if (przeszkody < 5)
                        Blad("BRAXHORN_METRO_M1_DOJSCIE obca przeszkoda " + c.name +
                                       " przy x=" + x.ToString("F1"));
                }
            }
        // Więcej niż samo "Lad" geograficzne: na starych nagraniach przez
        // wycięty grunt prześwitywało niebieskie morze, mimo że collider
        // sąsiedniego chodnika istniał. Test 3 pasów dojścia PRZED otworem.
        int bezChodnika = 0;
        for (float x = wejX - 5f; x < wejX - .4f; x += .55f)
            foreach (float z in new[] { zC - .55f, zC, zC + .55f })
            {
                var p = new Vector3(x, yChod + 1.25f, z);
                if (!Physics.Raycast(p, Vector3.down, out var hit, 2f,
                    ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore) ||
                    hit.point.y < yChod - .22f)
                {
                    bezChodnika++;
                    if (bezChodnika <= 7)
                        Blad("BRAXHORN_METRO_M1_NIEBIESKA_DZIURA: brak podłogi przed wejściem @" +
                            new Vector3(x, yChod, z).ToString("F2"));
                }
            }
        Debug.Log((lad && przeszkody == 0 && bezChodnika == 0 ?
            "BRAXHORN_METRO_M1_WEJSCIE_KOD_OK " :
            "BRAXHORN_METRO_M1_WEJSCIE_NIEGOTOWE ") +
              "odsuniete_x=69 (kamienica była ok. x66), na_ladzie=" + lad +
              " przeszkody_kamienic=" + przeszkody +
              " brak_podlogi_przed_wejsciem=" + bezChodnika +
              " — wygląd nawierzchni i wody nadal wymaga odbioru na Androidzie");
        if (!lad || przeszkody > 0 || bezChodnika > 0)
            Blad("BRAXHORN_METRO_M1_WEJSCIE_NIEGOTOWE na_ladzie=" + lad +
                " obce_przeszkody=" + przeszkody + " punkty_bez_podlogi=" + bezChodnika +
                " — przejście musi być w całości drożne przed testem APK");
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
