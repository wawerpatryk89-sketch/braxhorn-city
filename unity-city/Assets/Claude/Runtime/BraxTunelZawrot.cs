using UnityEngine;

// TUNEL NA OSIEDLE bez osiedla w tej aplikacji (pkt 42, do decyzji właściciela A/B): tunel jest długi i oświetlony (bez czarnej
// ściany), a gracz, który wejdzie głębiej niż kilka metrów, dostaje komunikat „OSIEDLE — osobna część gry” i wraca przed portal
// (krótkie przyciemnienie). Gdy scena osiedla jest w aplikacji, nic nie robi — przejazd obsługuje BraxCityWorld.
public sealed class BraxTunelZawrot : MonoBehaviour
{
    public Vector3 portal;                  // środek lica portalu (oś tunelu, poziom jezdni)
    public float polSzer = 4.8f, glebokosc = 6f, dlugosc = 62f;
    public Vector3 powrot; public float yawPowrotu = 180f;
    public string scenaCelu = "BraxhornEstate";
    public string tekst = "OSIEDLE — osobna część gry (połączenie wkrótce). Wracasz do miasta.";

    BraxGymPlayer g; float szukaj, tekstDo, ciemnoDo; bool sprawdzone, jestCel;
    static Texture2D biel; GUIStyle styl;

    void Update()
    {
        if (!sprawdzone) { sprawdzone = true; jestCel = Application.CanStreamedLevelBeLoaded(scenaCelu); }
        if (jestCel) return;
        if (!g && Time.unscaledTime > szukaj) { szukaj = Time.unscaledTime + 1f; g = FindFirstObjectByType<BraxGymPlayer>(); }
        if (!g) return;
        var p = g.transform.position;
        if (Mathf.Abs(p.x - portal.x) < polSzer && p.z < portal.z - glebokosc && p.z > portal.z - dlugosc && p.y > portal.y - 1.5f && p.y < portal.y + 8f)
        {
            tekstDo = Time.unscaledTime + 3.5f; ciemnoDo = Time.unscaledTime + .45f;
            g.TravelTo(powrot, yawPowrotu);
            Debug.Log("BRAXHORN_TUNEL_OSIEDLE zawrót przed portal (osiedle — osobna aplikacja)");
        }
    }

    void OnGUI()
    {
        float t = Time.unscaledTime; if (t > tekstDo && t > ciemnoDo) return;
        if (!biel) { biel = new Texture2D(1, 1); biel.SetPixel(0, 0, Color.white); biel.Apply(); }
        if (t < ciemnoDo) { GUI.color = new Color(.05f, .05f, .06f, 1f); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), biel); }
        GUI.color = Color.white;
        if (t > tekstDo) return;
        if (styl == null) styl = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true };
        float s = Mathf.Min(Screen.width, Screen.height); styl.fontSize = Mathf.RoundToInt(s * .042f);
        var r = new Rect(Screen.width * .1f, Screen.height * .18f, Screen.width * .8f, s * .14f);
        GUI.color = new Color(0, 0, 0, .55f); var rr = r; rr.x += 2; rr.y += 2; GUI.Label(rr, tekst, styl);
        GUI.color = new Color(1f, .86f, .3f, 1f); GUI.Label(r, tekst, styl); GUI.color = Color.white;
    }
}
