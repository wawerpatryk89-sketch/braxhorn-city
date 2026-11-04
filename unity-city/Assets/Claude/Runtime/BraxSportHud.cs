using System;
using UnityEngine;

// Osobny plik (1.11, 22:20): Unity zapisuje skrypt w scenie tylko, gdy nazwa pliku = nazwa klasy; inaczej w APK „brakujący skrypt” → awaria przy wczytywaniu sceny (osiedle 13, miasto od 9).
// Panel wyniku: mały, góra-środek, tylko na boisku / korcie (styl przycisków BraxTouchControls).
public sealed class BraxSportHud : MonoBehaviour
{
    GUIStyle lab, big; float rozmiar;
    static readonly Color Ink = new Color(1f, 1f, 1f, .92f), Glass = new Color(.06f, .07f, .07f, .5f), Accent = new Color(.55f, 1f, .2f, .95f);

    void OnGUI()
    {
        var b = BraxSport.Aktywne;
        bool blysk = Time.unscaledTime < BraxSport.BlyskDo && !string.IsNullOrEmpty(BraxSport.Blysk);
        if (!b && !blysk) return;
        float s = Mathf.Min(Screen.width, Screen.height);
        if (lab == null || rozmiar != s)
        {
            rozmiar = s;
            lab = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(s * .034f), wordWrap = false };
            big = new GUIStyle(lab) { fontSize = Mathf.RoundToInt(s * .06f) };
        }
        var old = GUI.color;
        if (b)
        {
            string txt = b.Panel;
            float w = Mathf.Min(Screen.width - s * .1f, lab.CalcSize(new GUIContent(txt)).x + s * .08f), h = s * .07f;
            var r = new Rect((Screen.width - w) * .5f, s * .1f, w, h);
            GUI.color = Glass; GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Ink; lab.normal.textColor = Ink; GUI.Label(r, txt, lab);
            // Sterowanie dla Patryka — przycisk CIOS/KOP ma w sportach inne
            // znaczenie niż w walce. Wskazówka na korcie bez dodatkowego menu.
            string wskazowka = b.gra == BraxSportBoisko.Gra.Kosz
                ? "AKCJA: piłka · RUCH: kozłuj · CIOS: rzut"
                : b.gra == BraxSportBoisko.Gra.Nozna
                    ? "RUCH: prowadź · CIOS: podaj · KOP: strzel"
                    : b.gra == BraxSportBoisko.Gra.Tenis
                        ? "CIOS: forhend/bekhend · AKCJA: wyrzutnia"
                        : "AKCJA: zagrywka · CIOS: odbicie";
            float h2 = s * .045f;
            var help = new Rect((Screen.width - w) * .5f - s * .12f, r.yMax + s * .004f,
                Mathf.Min(Screen.width - s * .06f, w + s * .24f), h2);
            GUI.color = Glass; GUI.DrawTexture(help, Texture2D.whiteTexture);
            GUI.color = Ink;
            lab.normal.textColor = Ink; GUI.Label(help, wskazowka, lab);
        }
        if (blysk)
        {
            var r = new Rect(0, s * .18f, Screen.width, s * .1f);
            GUI.color = new Color(0, 0, 0, .55f); big.normal.textColor = new Color(0, 0, 0, .6f);
            GUI.Label(new Rect(r.x + s * .004f, r.y + s * .004f, r.width, r.height), BraxSport.Blysk, big);
            GUI.color = Accent; big.normal.textColor = Accent; GUI.Label(r, BraxSport.Blysk, big);
        }
        GUI.color = old;
    }
}
