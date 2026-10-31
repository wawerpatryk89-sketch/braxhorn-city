using UnityEngine;

// BRAXHORN R2 — kolor kadru (zasada kolorów 31.10): nasycenie +15 %, kontrast +6 %, jedno przejście na końcu kadru
// (shader Braxhorn/OsiedleKolor). Bez bloomu, bez tintu, nic nie świeci. Wartości w środku widełek zasady
// (nasycenie +10…+20 %, kontrast +5…+10 %); kontrast 1.06 obcina tylko skrajne 2.8 % jasności (g > 0.972 lub < 0.028).
[RequireComponent(typeof(Camera))]
public class BraxClaudeKolor : MonoBehaviour
{
    public Shader shader;
    public float nasycenie = 1.15f, kontrast = 1.06f;
    Material m;

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (!shader || !shader.isSupported) { Graphics.Blit(src, dst); return; }
        if (!m) m = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        m.SetFloat("_Nasycenie", nasycenie); m.SetFloat("_Kontrast", kontrast);
        Graphics.Blit(src, dst, m);
    }

    void OnDestroy() { if (m) Destroy(m); }
}
