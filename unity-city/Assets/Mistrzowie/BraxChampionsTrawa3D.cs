using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Owner 23.11 17:59-18:38 ("tak samo trawa ... zmienić globalnie", "żeby ta trawa nie była płaska", "długa, nie jest krótka",
// "to jest dobra trawa"): the region's lawns grow real 3D grass around the player. Every clump is three crossed cards of
// Poly Haven Grass Bermuda 01 plants (ChampionsTrawaKarta.shader); the lawn under them stays the flat photo, which is
// all that is seen farther away. The clumps of a piece of lawn (BraxChampionsTrawa3DDane) are made when the piece comes
// within reach (at most naKlatke pieces a frame, so walking or a teleport never stalls a frame) and freed two pieces
// beyond it; each frame only the pieces in reach and in the camera's view are drawn, at most 1023 clumps a call, for the
// main camera only. Shadows: received, not cast. The fade with distance (each clump shrinks into the ground at its own
// distance, fewer farther away) is the shader's. Order 10000: after the player's camera has moved (BraxGymPlayer).
// The reach follows the graphics mode (BraxGrafika: low 9, medium 12, high 15 units) and the frame rate (owner: at least 30
// frames a second): two seconds under the target take 3 units off (down to 7.5), six seconds on it give them back - the
// reach glides there (1.5 units a second), the farthest clumps sink into the ground instead of vanishing.
[DefaultExecutionOrder(10000)]
public class BraxChampionsTrawa3D : MonoBehaviour
{
    public BraxChampionsTrawa3DDane dane;
    public Mesh siatka;
    public Material material;
    public float krok = .285f, zasieg = 15f, skalaMin = .85f, skalaMax = 1.2f;
    public int naKlatke = 8;
    public float zasiegMin = 7.5f;

    int[] pierwsza = new int[0];                     // piece -> its first patch, -1 none
    Matrix4x4[][][] porcje = new Matrix4x4[0][][];   // patch -> calls of <= 1023 clumps (null: not made)
    readonly List<int> zrobione = new List<int>();
    readonly List<Matrix4x4> bufor = new List<Matrix4x4>(1024);
    readonly Plane[] plaszczyzny = new Plane[6];
    public int ostatnioKep, ostatnioWywolan, latekWPamieci;
    public float biezacyZasieg;
    float cel = -1f, wolno, rowno, blisko0 = 7f;
    MaterialPropertyBlock mpb;
    static readonly int IdZasieg = Shader.PropertyToID("_Zasieg"), IdBlisko = Shader.PropertyToID("_Blisko");

    void Awake()
    {
        if (!dane || dane.nx <= 0 || dane.nz <= 0) return;
        pierwsza = new int[dane.nx * dane.nz];
        for (int i = 0; i < pierwsza.Length; i++) pierwsza[i] = -1;
        for (int l = dane.Latek - 1; l >= 0; l--) pierwsza[dane.latkaKawalek[l]] = l;
        porcje = new Matrix4x4[dane.Latek][][];
        mpb = new MaterialPropertyBlock();
        if (material && material.HasProperty("_Blisko")) blisko0 = material.GetFloat("_Blisko");
    }

    float Limit()
    {
        switch (BraxGrafika.Aktualny)
        {
            case BraxGrafika.Tryb.Niska: return Mathf.Min(zasieg, 9f);
            case BraxGrafika.Tryb.Srednia: return Mathf.Min(zasieg, 12f);
            default: return zasieg;
        }
    }

    void Plynnosc()
    {
        float limit = Mathf.Max(zasiegMin, Limit());
        if (cel < 0f) { cel = biezacyZasieg = limit; }
        float dt = Time.unscaledDeltaTime, fps = Application.targetFrameRate > 0 ? Mathf.Min(Application.targetFrameRate, 60) : 60;
        if (dt > 0f && dt < .25f)
        {
            if (dt > 1f / (fps * .87f)) { wolno += dt; rowno = 0f; }
            else if (dt < 1f / (fps * .95f)) { rowno += dt; wolno = 0f; }
            else { wolno = 0f; rowno = 0f; }
        }
        if (wolno > 2f) { cel = Mathf.Max(zasiegMin, cel - 3f); wolno = 0f; }
        if (rowno > 6f) { cel = Mathf.Min(limit, cel + 3f); rowno = 0f; }
        cel = Mathf.Min(cel, limit);
        biezacyZasieg = Mathf.MoveTowards(biezacyZasieg, cel, 1.5f * Mathf.Min(dt, .1f));
        mpb.SetFloat(IdZasieg, biezacyZasieg);
        mpb.SetFloat(IdBlisko, blisko0 * biezacyZasieg / Mathf.Max(1f, zasieg));
    }

    Matrix4x4[][] Zrob(int latka)
    {
        bufor.Clear();
        dane.Kepy(latka, krok, skalaMin, skalaMax, bufor);
        var wynik = new Matrix4x4[(bufor.Count + 1022) / 1023][];
        for (int p = 0; p < wynik.Length; p++)
        {
            int n = Mathf.Min(1023, bufor.Count - p * 1023);
            wynik[p] = new Matrix4x4[n];
            bufor.CopyTo(p * 1023, wynik[p], 0, n);
        }
        return wynik;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (!cam || !dane || !siatka || !material || pierwsza.Length == 0) return;
        Plynnosc();
        Vector3 p = cam.transform.position;
        float kaw = dane.kawalek, pol = kaw * .5f, r = biezacyZasieg + kaw * .71f, r2 = r * r, zw = zasieg + kaw * .71f + 2f * kaw;
        for (int i = zrobione.Count - 1; i >= 0; i--)
        {
            int l = zrobione[i], k = dane.latkaKawalek[l];
            float dx = dane.x0 + (k % dane.nx) * kaw + pol - p.x, dz = dane.z0 + (k / dane.nx) * kaw + pol - p.z, dy = dane.latkaY[l] - p.y;
            if (dx * dx + dy * dy + dz * dz > zw * zw) { porcje[l] = null; zrobione.RemoveAt(i); }
        }
        GeometryUtility.CalculateFrustumPlanes(cam, plaszczyzny);
        int ix0 = Mathf.Max(0, Mathf.FloorToInt((p.x - r - dane.x0) / kaw)), ix1 = Mathf.Min(dane.nx - 1, Mathf.FloorToInt((p.x + r - dane.x0) / kaw));
        int iz0 = Mathf.Max(0, Mathf.FloorToInt((p.z - r - dane.z0) / kaw)), iz1 = Mathf.Min(dane.nz - 1, Mathf.FloorToInt((p.z + r - dane.z0) / kaw));
        int nowe = 0, kep = 0, wyw = 0, warstwa = gameObject.layer;
        for (int iz = iz0; iz <= iz1; iz++)
            for (int ix = ix0; ix <= ix1; ix++)
            {
                int k = iz * dane.nx + ix, l = pierwsza[k];
                if (l < 0) continue;
                float cx = dane.x0 + ix * kaw + pol, cz = dane.z0 + iz * kaw + pol;
                for (; l < dane.Latek && dane.latkaKawalek[l] == k; l++)
                {
                    float y = dane.latkaY[l], dx = cx - p.x, dy = y - p.y, dz = cz - p.z;
                    if (dx * dx + dy * dy + dz * dz > r2) continue;
                    if (porcje[l] == null)
                    {
                        if (nowe >= naKlatke) continue;              // the rest in the next frames (made before they are close)
                        porcje[l] = Zrob(l); zrobione.Add(l); nowe++;
                    }
                    if (!GeometryUtility.TestPlanesAABB(plaszczyzny, new Bounds(new Vector3(cx, y + .25f, cz), new Vector3(kaw + 1f, .9f, kaw + 1f)))) continue;
                    foreach (var arr in porcje[l])
                    {
                        Graphics.DrawMeshInstanced(siatka, 0, material, arr, arr.Length, mpb, ShadowCastingMode.Off, true, warstwa, cam, LightProbeUsage.Off);
                        kep += arr.Length; wyw++;
                    }
                }
            }
        ostatnioKep = kep; ostatnioWywolan = wyw; latekWPamieci = zrobione.Count;
    }
}
