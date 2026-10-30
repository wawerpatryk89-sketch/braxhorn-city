using System.Collections.Generic;
using UnityEngine;

// BRAXHORN CITY — napisy 3D (TextMesh) z testem głębi.
// Domyślny materiał czcionki Unity (GUI/Text Shader) ma ZTest Always: napisy prześwitują przez ściany
// (np. „WINDA DACH” z holu widoczne z ulicy na szyldzie apteki). Ten komponent podmienia materiał
// wszystkich TextMesh na wariant z GUI/3D Text Shader (ZTest LEqual) i aktualizuje atlas czcionki,
// gdy Unity go przebuduje (czcionka dynamiczna).
public sealed class BraxRegionSignage : MonoBehaviour
{
    public Material depthTested;      // szablon z GUI/3D Text Shader (zapisany w edytorze, żeby shader trafił do buildu)
    public int replaced;
    readonly Dictionary<Font, Material> perFont = new Dictionary<Font, Material>();

    void Awake()
    {
        if (!depthTested) { enabled = false; return; }
        foreach (var tm in FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var font = tm.font; var mr = tm.GetComponent<MeshRenderer>();
            if (!font || !mr || !font.material) continue;
            Material m;
            if (!perFont.TryGetValue(font, out m))
            {
                m = new Material(depthTested) { name = "Napis 3D z testem głębi (" + font.name + ")" };
                m.mainTexture = font.material.mainTexture;
                perFont[font] = m;
            }
            mr.sharedMaterial = m;
            replaced++;
        }
        Font.textureRebuilt += OnRebuilt;
    }

    void OnDestroy() { Font.textureRebuilt -= OnRebuilt; }

    void OnRebuilt(Font font)
    {
        Material m;
        if (font && font.material && perFont.TryGetValue(font, out m) && m) m.mainTexture = font.material.mainTexture;
    }
}
