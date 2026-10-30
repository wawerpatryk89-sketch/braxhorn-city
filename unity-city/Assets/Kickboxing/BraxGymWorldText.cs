using UnityEngine;

// The default legacy font shader draws through walls. This binding preserves
// Unity's generated glyphs while the room's printed-text material depth-tests.
public sealed class BraxGymWorldText : MonoBehaviour
{
    public Font font;
    public Material printMaterial;

    void OnEnable()
    {
        Font.textureRebuilt += FontRebuilt;
        RefreshTexture();
    }

    void Start() { RefreshTexture(); }
    void OnDisable() { Font.textureRebuilt -= FontRebuilt; }
    void FontRebuilt(Font changed) { if (changed == font) RefreshTexture(); }

    public void RefreshTexture()
    {
        if (font && font.material && printMaterial)
            printMaterial.mainTexture = font.material.mainTexture;
    }
}
