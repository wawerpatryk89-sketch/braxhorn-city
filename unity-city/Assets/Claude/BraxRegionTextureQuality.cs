using UnityEngine;

// BRAXHORN CITY — jakość tekstur na telefonie: trilinear + filtrowanie anizotropowe dla
// wszystkich tekstur z mipmapami. Bez tego przejścia między mipmapami „gotują się” i
// migoczą przy ruchu kamery (ramy okien, gzymsy, kostka). Ustawienia samplera, nic nie
// jest ponownie importowane. Działa też w WebGL.
public sealed class BraxRegionTextureQuality : MonoBehaviour
{
    public int anisotropy = 4;
    public int changed;

    private void Awake()
    {
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        foreach (var t in Resources.FindObjectsOfTypeAll<Texture2D>())
        {
            if (!t || t.mipmapCount <= 1) continue;
            if (t.filterMode != FilterMode.Trilinear) t.filterMode = FilterMode.Trilinear;
            if (t.anisoLevel < anisotropy) t.anisoLevel = anisotropy;
            changed++;
        }
    }
}
