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
