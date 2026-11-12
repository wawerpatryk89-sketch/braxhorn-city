using System;
using UnityEngine;

// THE ONE WASHBASIN (owner 12.11.2026 15:58: "dlaczego ty umywalek nie przenosisz z miejsc, gdzie masz zrobione"): the vessel
// basin of the G1 toilets and the apartments - a glazed bowl 42 cm across and 14 cm high standing on the counter, its chrome
// drain, a tall chrome tap (a column to 30 cm, an arc spout over the bowl's centre, the aerator) and a soap dispenser -
// for every place of the region that still had a plain cylinder or a box: the arena's changing rooms, the hotel's guest
// toilets and rooms (the station's toilets take the G1 set itself, with its running water).
// The caller's own quad and tube build it, so it lands in the caller's mesh (the interior kit W: W.Quad / W.Rura, or a
// BraxChampionsMeshBatch: m.Quad / m.Tube). top = the counter top under the bowl's centre, back = toward the wall.
public static class BraxUmywalka
{
    // the bowl's section (radius, height): the foot, the belly, the rolled rim, the glazed inside down to the waste
    static readonly Vector2[] Misa =
    {
        new Vector2(0f, 0f), new Vector2(.11f, 0f), new Vector2(.17f, .03f), new Vector2(.205f, .10f), new Vector2(.212f, .14f),
        new Vector2(.200f, .143f), new Vector2(.192f, .10f), new Vector2(.155f, .045f), new Vector2(.06f, .022f), new Vector2(0f, .02f)
    };

    public static void Postaw(Action<Material, Vector3, Vector3, Vector3, Vector3> quad, Action<Material, Vector3, Vector3, float> tube,
        Material porcelana, Material kran, Vector3 top, Vector3 back, float zasieg = .27f)
    {
        back.y = 0f; back.Normalize(); var r = Vector3.Cross(Vector3.up, back).normalized; var up = Vector3.up;
        Toczenie(quad, porcelana, top, Misa, 32);
        Toczenie(quad, kran, top + up * .014f, new[] { new Vector2(.032f, 0f), new Vector2(.032f, .006f), new Vector2(0f, .006f) }, 18);   // the drain
        // the tap: the flange, the column, the arc over to the spout, the spout to the bowl's centre, the aerator
        var tb = top + back * zasieg;
        Toczenie(quad, kran, tb, new[] { new Vector2(.032f, 0f), new Vector2(.032f, .012f), new Vector2(0f, .012f) }, 18);
        tube(kran, tb, tb + up * .30f, .017f);
        Vector3 prev = tb + up * .30f;
        for (int k = 1; k <= 8; k++)
        {
            float t = k / 8f * Mathf.PI * .5f;
            var q = tb + up * (.30f + .07f * Mathf.Sin(t)) - back * (.07f * (1f - Mathf.Cos(t)));
            tube(kran, prev, q, .016f); prev = q;
        }
        var aer = tb + up * .37f - back * (zasieg - .015f);
        tube(kran, prev, aer, .016f);
        tube(kran, aer + up * .012f, aer - up * .035f, .019f);
        // the soap dispenser beside it
        var sd = tb + r * .24f;
        Toczenie(quad, kran, sd, new[] { new Vector2(.022f, 0f), new Vector2(.022f, .012f), new Vector2(0f, .012f) }, 14);
        tube(kran, sd, sd + up * .17f, .012f);
        tube(kran, sd + up * .16f, sd + up * .17f - back * .08f, .008f);
    }

    // a solid of revolution from quads (the same winding as BraxChampionsMeshBatch.Lathe: a profile walked outward-up shows
    // its outside, walked inward its inside; the discs are walked from the rim to the axis so their tops look up)
    static void Toczenie(Action<Material, Vector3, Vector3, Vector3, Vector3> quad, Material m, Vector3 c, Vector2[] prof, int boki)
    {
        for (int i = 0; i + 1 < prof.Length; i++)
        {
            if (prof[i].x < 1e-6f && prof[i + 1].x < 1e-6f) continue;
            for (int j = 0; j < boki; j++)
            {
                float a0 = j * Mathf.PI * 2f / boki, a1 = (j + 1) * Mathf.PI * 2f / boki;
                var d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)); var d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 b0 = c + d0 * prof[i].x + Vector3.up * prof[i].y, b1 = c + d1 * prof[i].x + Vector3.up * prof[i].y;
                Vector3 t0 = c + d0 * prof[i + 1].x + Vector3.up * prof[i + 1].y, t1 = c + d1 * prof[i + 1].x + Vector3.up * prof[i + 1].y;
                if (prof[i].x < 1e-6f) quad(m, t0, t1, b1, b0);                 // from the axis: the same triangle, a non-zero normal
                else quad(m, b1, b0, t0, t1);
            }
        }
    }
}
