using System;
using System.Linq;
using UnityEngine;

// BRAXHORN CITY / Unity 6 — lekki audyt sceny, uruchamiany podczas generowania miasta.
// Zgłasza błędy bez wyłączania losowych kolizji i bez przerabiania oryginalnych grafik.
internal static class BraxMiastoOdbior
{
    public static void Wykonaj(Transform cityRoot)
    {
        if (!cityRoot) { Debug.LogError("BRAXHORN_MIASTO_ODBIOR brak korzenia miasta"); return; }
        Physics.SyncTransforms();
        var statki = cityRoot.GetComponentsInChildren<BraxVessel>(true);
        int J(string p) => statki.Count(v => v && v.name.StartsWith(p, StringComparison.Ordinal));
        int jachty = J("Marina: jacht"), motorowki = J("Marina: motorówka"), skutery = J("Marina: skuter wodny");
        string mar = "BRAXHORN_MIASTO_ODBIOR marina jachty=" + jachty + " motorowki=" + motorowki + " skutery=" + skutery;
        if (jachty < 3) Debug.LogError(mar + " — NIEGOTOWE: minimum 3 jachty"); else Debug.Log(mar);

        var renderery = cityRoot.GetComponentsInChildren<MeshRenderer>(true);
        bool Mural(string nazwa) => renderery.Any(r => r && r.enabled && r.name == nazwa &&
            r.sharedMaterial && r.sharedMaterial.mainTexture);
        bool muralGym = Mural("mural_19"), muralNova = Mural("mural mural_nova");
        string mur = "BRAXHORN_MIASTO_ODBIOR mural_19=" + muralGym + " mural_NOVA_za_DJ=" + muralNova;
        if (!muralGym || !muralNova) Debug.LogError(mur + " — NIEGOTOWE: brak oryginalnej grafiki na ścianie");
        else Debug.Log(mur);

        // Stopy gracza: promień 0.30 m, wysokość 2.21 m, przejście przez całe światło drzwi.
        // Pomijamy skrzydła zamknięte w trybie edycji; inne statyczne przeszkody są usterką.
        SprawdzWejscie("NOVA", 42.0f, .15f, 35.73f, 36.60f);
        SprawdzWejscie("TOWER", 59.0f, .15f, 35.83f, 36.88f);
        SprawdzWejscie("MIESZKANIE_BRAXA_KLATKA", 33.73f, .15f, 35.72f, 36.65f);
        SprawdzApteke(cityRoot);
        SprawdzTaras360(cityRoot);
    }

    static void SprawdzApteke(Transform cityRoot)
    {
        var budynek = cityRoot.Find("Building apothecary");
        if (!budynek)
        {
            Debug.LogError("BRAXHORN_APTEKA_ODBIOR NIEGOTOWA: nie znaleziono Building apothecary");
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
            Debug.LogError(log + " NIEGOTOWA (wymagane działające drzwi oraz opakowania z etykietami)");
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
            Debug.LogError(log + " — NIEGOTOWE");
        else if (!hd)
            Debug.LogWarning(log + " — panorama jest, ale nie potwierdzono nieba minimum 4K");
        else
            Debug.Log(log + " — elementy dostępne do testu z dachu");
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
            Debug.LogError("BRAXHORN_MIASTO_ODBIOR " + nazwa + " = NIEDROŻNE, kolizje=" + przeszkody);
        else Debug.Log("BRAXHORN_MIASTO_ODBIOR " + nazwa +
                       " = bez przeszkód statycznych w osi wejścia (nadal wymagany test telefonu)");
    }
}
