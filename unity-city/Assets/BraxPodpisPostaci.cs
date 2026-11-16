using UnityEngine;

// Podpis nad głową postaci w pokoju prób — żeby w APK było widać, kto jest kto.
public sealed class BraxPodpisPostaci : MonoBehaviour
{
    public string nazwa = "";
    Camera kam;
    GUIStyle styl;

    void OnGUI()
    {
        if (string.IsNullOrEmpty(nazwa)) return;
        if (!kam) kam = Camera.main;
        if (!kam) return;
        var p = kam.WorldToScreenPoint(transform.position);
        if (p.z <= 0f) return;
        if (styl == null)
            styl = new GUIStyle(GUI.skin.label)
            { fontSize = 20, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        GUI.Label(new Rect(p.x - 150f, Screen.height - p.y - 14f, 300f, 28f), nazwa, styl);
    }
}
