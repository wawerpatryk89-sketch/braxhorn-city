using System.Collections.Generic;
using UnityEngine;

// Owner 23.11 ("żeby ta trawa nie była płaska", "długa", "to jest dobra trawa"): where the region's 3D grass grows, and the
// clumps of one piece of it. The lawn is cut into square pieces (kawałek, 8 game units); the lawn of one piece at one height
// is a patch (łatka): all lawn (latkaBity -1) or 32 x 32 cells of 25 cm, one bit each (1 = grass grows here). Written by the
// editor (BraxChampionsTrawa3DBuild) from the finished scene: the lawn surfaces, less everything that stands on them, the
// soil under the trees and a one-cell margin at every edge. Drawn by BraxChampionsTrawa3D.
public class BraxChampionsTrawa3DDane : ScriptableObject
{
    public const int Komorek = 32;                          // cells along a piece's side
    public const int BajtowLatki = Komorek * Komorek / 8;   // 128 bytes a partial patch
    public float x0, z0, kawalek = 8f;
    public int nx, nz;
    public int[] latkaKawalek = new int[0];                 // patch -> piece (iz * nx + ix), ascending
    public float[] latkaY = new float[0];                   // patch -> the lawn's height (world)
    public int[] latkaBity = new int[0];                    // patch -> its first byte in bity, -1 = all lawn
    public byte[] bity = new byte[0];

    public float Komorka => kawalek / Komorek;
    public int Latek => latkaKawalek.Length;

    public bool Trawa(int latka, int cx, int cz)
    {
        int o = latkaBity[latka];
        if (o < 0) return true;
        int b = cz * Komorek + cx;
        return (bity[o + (b >> 3)] & (1 << (b & 7))) != 0;
    }

    public static uint Hash(uint x)
    {
        x ^= x >> 16; x *= 0x7feb352dU; x ^= x >> 15; x *= 0x846ca68bU; x ^= x >> 16;
        return x;
    }

    // The clumps of one patch: one in every krok x krok square of the piece, anywhere in it (no rows), on a cell that is
    // lawn; its own turn, a lean of up to 5 degrees, size and height. Always the same for the same patch (no clock, no
    // global random), so a piece made again after it was freed looks exactly as before.
    public void Kepy(int latka, float krok, float skalaMin, float skalaMax, List<Matrix4x4> wynik)
    {
        int k = latkaKawalek[latka], ix = k % nx, iz = k / nx;
        float bx = x0 + ix * kawalek, bz = z0 + iz * kawalek, y = latkaY[latka], kom = Komorka;
        int n = Mathf.Max(1, Mathf.RoundToInt(kawalek / krok));
        float s = kawalek / n;
        uint ziarno = Hash((uint)k * 2654435761u ^ (uint)Mathf.RoundToInt(y * 100f) * 40503u);
        for (int gz = 0; gz < n; gz++)
            for (int gx = 0; gx < n; gx++)
            {
                uint h = Hash(ziarno + (uint)(gz * n + gx) * 747796405u);
                float x = (gx + (h & 1023u) / 1024f) * s, z = (gz + ((h >> 10) & 1023u) / 1024f) * s;
                int cx = Mathf.Min(Komorek - 1, (int)(x / kom)), cz = Mathf.Min(Komorek - 1, (int)(z / kom));
                if (!Trawa(latka, cx, cz)) continue;
                uint h2 = Hash(h ^ 0x9e3779b9u);
                float yaw = (h2 & 1023u) / 1024f * 360f;
                float sc = Mathf.Lerp(skalaMin, skalaMax, ((h2 >> 10) & 1023u) / 1023f);
                float wys = .85f + .35f * ((h2 >> 20) & 1023u) / 1023f;
                float tx = ((h >> 20) & 63u) / 63f * 10f - 5f, tz = ((h2 >> 26) & 63u) / 63f * 10f - 5f;
                wynik.Add(Matrix4x4.TRS(new Vector3(bx + x, y - .01f, bz + z), Quaternion.Euler(tx, yaw, tz), new Vector3(sc, sc * wys, sc)));
            }
    }
}
