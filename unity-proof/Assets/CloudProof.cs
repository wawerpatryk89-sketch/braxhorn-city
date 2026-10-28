using UnityEngine;

// This is an isolated toolchain check, not the Braxhorn City game or its art.
public sealed class CloudProof : MonoBehaviour
{
    public string sourceCommit;
    public Renderer target;
    private int clicks;
    private readonly Color[] colors = {
        new Color(0.55f, 1f, 0.23f),
        new Color(0.1f, 0.8f, 1f),
        new Color(1f, 0.55f, 0.18f)
    };

    private void Start()
    {
        Application.targetFrameRate = 30;
    }

    private void Update()
    {
        if (target != null)
            target.transform.Rotate(0f, 25f * Time.deltaTime, 0f, Space.World);
    }

    private void OnGUI()
    {
        float scale = Mathf.Max(1f, Screen.width / 1100f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;
        float height = Screen.height / scale;
        GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
        GUIStyle body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
        GUIStyle button = new GUIStyle(GUI.skin.button) { fontSize = 20 };
        GUI.Box(new Rect(12, 12, Mathf.Min(width - 24, 620), 116), GUIContent.none);
        GUI.Label(new Rect(26, 22, width - 52, 36), "BRAXHORN / TEST UNITY", title);
        GUI.Label(new Rect(26, 60, width - 52, 46), "Test budowania w chmurze. To nie jest wyglad docelowej gry.", body);
        string id = string.IsNullOrEmpty(sourceCommit) ? "local" : sourceCommit.Substring(0, Mathf.Min(12, sourceCommit.Length));
        GUI.Label(new Rect(26, 103, width - 52, 24), "Wersja: " + id, body);
        if (GUI.Button(new Rect(20, height - 88, Mathf.Min(320, width - 40), 64), "Zmien kolor / klikniecia: " + clicks, button))
        {
            clicks++;
            if (target != null) target.material.color = colors[clicks % colors.Length];
        }
    }
}
