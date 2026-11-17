using UnityEngine;

// MIASTO MISTRZÓW - KASYNO WAWER, the owner's hidden office (17.11: "Monitoring sali na żywo"): six cameras in the hall (in
// their black housings on the columns and the front wall), each rendering into its screen on the office's monitoring wall -
// the real game seen live, never a film. Only while the player is in the office, one camera a frame (each screen about ten
// pictures a second, as a real CCTV multiview); 320 x 180 each.
public sealed class BraxChampionsMonitoring : MonoBehaviour
{
    public Camera[] kamery = new Camera[0];
    public Renderer[] ekrany = new Renderer[0];
    public Transform srodek; public float zasieg = 7f;   // metres (x the transform's scale)
    public int szer = 320, wys = 180;
    RenderTexture[] rt = new RenderTexture[0]; int nastepna;

    void Start()
    {
        int n = Mathf.Min(kamery.Length, ekrany.Length); rt = new RenderTexture[n];
        for (int i = 0; i < n; i++)
        {
            if (!kamery[i] || !ekrany[i]) continue;
            kamery[i].enabled = false;
            rt[i] = new RenderTexture(szer, wys, 16, RenderTextureFormat.ARGB32) { name = "monitoring " + kamery[i].name };
            kamery[i].targetTexture = rt[i];
            var m = new Material(ekrany[i].sharedMaterial) { name = "monitoring ekran " + (i + 1) };
            m.mainTexture = rt[i]; if (m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", rt[i]);
            ekrany[i].sharedMaterial = m;
            kamery[i].Render();                                  // a first picture on every screen
        }
    }
    public bool WBiurze()
    {
        var g = BraxChampionsGry.Gracz; if (!g || !srodek) return false;
        float s = Mathf.Max(.01f, transform.lossyScale.y);
        return (g.transform.position - srodek.position).sqrMagnitude < zasieg * zasieg * s * s;
    }
    void Update()
    {
        if (rt.Length == 0 || !WBiurze()) return;
        for (int k = 0; k < rt.Length; k++)
        {
            nastepna = (nastepna + 1) % rt.Length;
            if (kamery[nastepna] && rt[nastepna]) { kamery[nastepna].Render(); break; }
        }
    }
    void OnDestroy() { foreach (var r in rt) if (r) { r.Release(); Destroy(r); } }
}
