using UnityEngine;

// Separate file (Unity keeps a MonoBehaviour in a scene only when the file name = the class name). The E1 games panel:
// small, top-centre, while the player plays a lane or a table (BraxChampionsGry.Ustaw), and the big flash under it (red, the E1 accent: no gold).
public sealed class BraxChampionsGryHud : MonoBehaviour
{
    GUIStyle lab, big; float rozmiar;
    static readonly Color Ink = new Color(1f, 1f, 1f, .94f), Glass = new Color(.05f, .05f, .07f, .55f), Gold = new Color(1f, .30f, .32f, .97f);

    void OnGUI()
    {
        bool panel = Time.unscaledTime < BraxChampionsGry.PanelDo && !string.IsNullOrEmpty(BraxChampionsGry.Panel);
        bool blysk = Time.unscaledTime < BraxChampionsGry.BlyskDo && !string.IsNullOrEmpty(BraxChampionsGry.Blysk);
        if (!panel && !blysk) return;
        float s = Mathf.Min(Screen.width, Screen.height);
        if (lab == null || rozmiar != s)
        {
            rozmiar = s;
            lab = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(s * .032f), wordWrap = false };
            big = new GUIStyle(lab) { fontSize = Mathf.RoundToInt(s * .065f) };
        }
        var old = GUI.color;
        if (panel)
        {
            string txt = BraxChampionsGry.Panel;
            float w = Mathf.Min(Screen.width - s * .1f, lab.CalcSize(new GUIContent(txt)).x + s * .08f), h = s * .068f;
            var r = new Rect((Screen.width - w) * .5f, s * .1f, w, h);
            GUI.color = Glass; GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Gold; GUI.DrawTexture(new Rect(r.x, r.yMax - s * .004f, r.width, s * .004f), Texture2D.whiteTexture);
            GUI.color = Ink; lab.normal.textColor = Ink; GUI.Label(r, txt, lab);
        }
        if (blysk)
        {
            var r = new Rect(0, s * .19f, Screen.width, s * .11f);
            GUI.color = new Color(0, 0, 0, .6f); big.normal.textColor = new Color(0, 0, 0, .6f);
            GUI.Label(new Rect(r.x + s * .004f, r.y + s * .004f, r.width, r.height), BraxChampionsGry.Blysk, big);
            GUI.color = Gold; big.normal.textColor = Gold; GUI.Label(r, BraxChampionsGry.Blysk, big);
        }
        GUI.color = old;
    }
}
