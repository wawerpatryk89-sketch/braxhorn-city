using System.Collections.Generic;
using UnityEngine;

// BRAXHORN KATALOG — DOM (owner 11.11 19:57-20:12: "meble masz porobione, to już możesz przenosić ... apartamenty tak
// samo wyposażysz", "z salonu meblowego ... urządzać pomieszczenia"): the BRAXHORN HOME showroom pieces (BraxChampionsG1Meble)
// as catalogue pieces any generator places - a sofa, a coffee table, a dining set, a bed with bedside tables, a rug, a
// reading lamp, a wall-hung WC. Each is a shared mesh (BraxKatalog.Place: built once, placed many times) plus, per
// placement, its BraxSeats (sitting / lying) and its collider, so a building full of flats stays light on the phone.
// p = the middle of the piece's FRONT edge on the floor, f = where it faces (a bed: toward its foot; the WC: from the wall).
public static partial class BraxKatalog
{
    public static Material Linen, Boucle, VelvetGreen, VelvetRust, Cotton, RugGrey, RugBeige, OakSolid, Shade;
    public static void HomeMaterials()
    {
        ClothesMaterials();
        if (Linen) return;
        Linen = T("home_linen", "hessian_230", .4f, new Color(1.45f, 1.45f, 1.45f));
        Boucle = T("home_boucle", "curly_teddy_natural", .35f, new Color(1.02f, 1.0f, .98f));
        VelvetGreen = T("home_velvet_emerald", "velour_velvet", .5f, new Color(.22f, 2.5f, 1.25f));
        VelvetRust = T("home_velvet_rust", "velour_velvet", .5f, new Color(.85f, .90f, .45f));
        Cotton = T("home_cotton_white", "cotton_jersey", .5f, new Color(1.22f, 1.30f, 1.36f));
        RugGrey = T("home_rug_grey", "poly_wool_herringbone", .9f, new Color(1.05f, 1.05f, 1.10f));
        RugBeige = T("home_rug_beige", "terlenka", 1.2f, new Color(1.05f, 1.0f, .95f));
        OakSolid = T("home_oak_solid", "oak_wood_planks", 2.4f, new Color(1.05f, .98f, .92f));
        Shade = T("home_lamp_linen", "hessian_230", .3f, new Color(1.70f, 1.66f, 1.60f));
    }
    // the showroom's frame: c on the floor at the middle of the FRONT edge, P(a, h, d): a to the right, h up, d BACK
    struct HF
    {
        public Vector3 c, f, u; public Quaternion rot;
        public HF(Vector3 c, Vector3 face) { face.y = 0; face.Normalize(); this.c = c; f = face; u = Vector3.Cross(Vector3.up, face).normalized; rot = Quaternion.LookRotation(face, Vector3.up); }
        public Vector3 P(float a, float h, float d) => c + u * a + Vector3.up * h - f * d;
        public Quaternion Lean(float back, float roll = 0f) => rot * Quaternion.Euler(-back, 0f, roll);
    }
    static void Leg(Ctx c, Material mat, Vector3 b, float h, float r0, float r1) => c.m.Cylinder(mat, b, r0, h, 12, Quaternion.identity, true, true, r1);
    // per placement: a seat and a collider in the piece's frame (front-edge frame, d back)
    static void SeatAt(Ctx c, Vector3 p, Vector3 f, float a, float h, float d, string label)
    { var k = new HF(p, f); c.seat?.Invoke(k.P(a, h, d), k.f, label); }
    static void ColAt(Ctx c, Vector3 p, Vector3 f, float a0, float a1, float d0, float d1, float h, string name)
    { var k = new Fr(p, f); Col(c, k, a0, a1, 0f, h, -d1, -d0, name); }

    // ---------------------------------------------------------------- sofa
    static void SofaMesh(Ctx c, float W, Material up, Material accent)
    {
        var k = new HF(Vector3.zero, Vector3.forward); float D = .98f, arm = .22f;
        int n = Mathf.Max(1, Mathf.RoundToInt((W - 2f * arm) / .78f)); float cw = (W - 2f * arm) / n;
        var la = new List<float> { -W * .5f + .09f, W * .5f - .09f }; if (W > 2.0f) la.Add(0f);
        foreach (float a in la) foreach (float d in new[] { .09f, D - .09f }) Leg(c, Walnut, k.P(a, 0f, d), .12f, .018f, .026f);
        c.m.RoundedBox(up, k.P(0f, .21f, D * .5f), new Vector3(W - .02f, .18f, D - .02f), .05f, k.rot, 2);
        c.m.RoundedBox(up, k.P(0f, .58f, D - .11f), new Vector3(W, .58f, .22f), .09f, k.rot, 3);
        foreach (float s in new[] { -1f, 1f }) c.m.RoundedBox(up, k.P(s * (W * .5f - arm * .5f), .39f, D * .5f), new Vector3(arm, .54f, D), .10f, k.rot, 3);
        for (int i = 0; i < n; i++)
        {
            float a = -W * .5f + arm + (i + .5f) * cw, dl = D - .22f;
            c.m.RoundedBox(up, k.P(a, .38f, dl * .5f), new Vector3(cw - .012f, .16f, dl), .075f, k.rot, 3);
            c.m.RoundedBox(up, k.P(a, .68f, D - .30f), new Vector3(cw - .02f, .46f, .21f), .095f, k.Lean(12f), 3);
        }
        foreach (float s in new[] { -1f, 1f }) c.m.RoundedBox(accent, k.P(s * (W * .5f - arm - .27f), .64f, D - .46f), new Vector3(.46f, .44f, .13f), .065f, k.Lean(18f, s * 7f), 3);
    }
    /// <summary>An upholstered sofa W wide (2.0-2.6): legs, base, rolled arms, seat and back cushions, two pillows; a seat per cushion.</summary>
    public static void Sofa(Ctx c, Vector3 p, Vector3 f, float W, int style, string label = "sofa")
    {
        HomeMaterials();
        var up = style == 1 ? Boucle : Linen; var acc = style == 1 ? VelvetRust : VelvetGreen;
        W = Mathf.Round(W * 10f) / 10f;
        Place(c, "sofa " + W.ToString("0.0") + "-" + style, cc => SofaMesh(cc, W, up, acc), p, f, null);
        int n = Mathf.Max(1, Mathf.RoundToInt((W - .44f) / .78f)); float cw = (W - .44f) / n;
        for (int i = 0; i < n; i++) SeatAt(c, p, f, -W * .5f + .22f + (i + .5f) * cw, .46f, .40f, label);
        ColAt(c, p, f, -W * .5f, W * .5f, 0f, .98f, .88f, label);
    }
    // ---------------------------------------------------------------- tables, dining set
    static void CoffeeMesh(Ctx c, float L, float Wd)
    {
        var k = new HF(Vector3.zero, Vector3.forward); float h = .42f;
        c.m.RoundedBox(Walnut, k.P(0f, h - .022f, Wd * .5f), new Vector3(L, .044f, Wd), .018f, k.rot, 2);
        foreach (float a in new[] { -L * .5f + .07f, L * .5f - .07f }) foreach (float d in new[] { .07f, Wd - .07f }) c.m.RoundedBox(BlackMatt, k.P(a, (h - .044f) * .5f, d), new Vector3(.03f, h - .044f, .03f), .008f, k.rot, 1);
        c.m.RoundedBox(Oak, k.P(0f, .12f, Wd * .5f), new Vector3(L - .10f, .025f, Wd - .10f), .01f, k.rot, 1);
    }
    /// <summary>A coffee table: walnut top, black steel frame, oak shelf (L x Wd, 42 cm).</summary>
    public static void CoffeeTable(Ctx c, Vector3 p, Vector3 f, float L = 1.1f, float Wd = .6f)
    { HomeMaterials(); Place(c, "stolik kawowy", cc => CoffeeMesh(cc, L, Wd), p, f, null); ColAt(c, p, f, -L * .5f, L * .5f, 0f, Wd, .42f, "stolik"); }
    static void ChairMesh(Ctx c, HF k, Material seat, Material wood)
    {
        c.m.RoundedBox(wood, k.P(0f, .425f, .23f), new Vector3(.46f, .045f, .44f), .012f, k.rot, 1);
        c.m.RoundedBox(seat, k.P(0f, .475f, .225f), new Vector3(.47f, .07f, .45f), .03f, k.rot, 3);
        foreach (float s in new[] { -1f, 1f })
        {
            Leg(c, wood, k.P(s * .20f, 0f, .05f), .405f, .014f, .02f);
            c.m.Tube(wood, k.P(s * .20f, 0f, .42f), k.P(s * .215f, .93f, .49f), .017f, 10);
            c.m.Tube(wood, k.P(s * .20f, .13f, .05f), k.P(s * .20f, .13f, .42f), .010f, 8);
        }
        c.m.Tube(wood, k.P(-.20f, .15f, .25f), k.P(.20f, .15f, .25f), .010f, 8);
        c.m.RoundedBox(seat, k.P(0f, .76f, .47f), new Vector3(.25f, .34f, .055f), .025f, k.Lean(8f), 3);
        foreach (float s in new[] { -1f, 1f }) c.m.RoundedBox(seat, k.P(s * .178f, .76f, .452f), new Vector3(.13f, .34f, .055f), .025f, k.rot * Quaternion.Euler(-8f, -s * 14f, 0f), 2);
    }
    static void DiningMesh(Ctx c)
    {
        var k = new HF(Vector3.zero, Vector3.forward); float L = 1.6f, Wd = .9f, h = .76f;
        c.m.RoundedBox(OakSolid, k.P(0f, h - .0275f, Wd * .5f), new Vector3(L, .055f, Wd), .02f, k.rot, 2);
        foreach (float d in new[] { .14f, Wd - .14f }) c.m.RoundedBox(OakSolid, k.P(0f, h - .10f, d), new Vector3(L - .30f, .09f, .035f), .01f, k.rot, 1);
        foreach (float a in new[] { -L * .5f + .13f, L * .5f - .13f }) foreach (float d in new[] { .13f, Wd - .13f })
            c.m.RoundedBox(OakSolid, k.P(a, (h - .055f) * .5f + .02f, d), new Vector3(.075f, h - .095f, .075f), .012f, k.rot, 1);
        // four chairs: two on each long side, pulled out 15 cm
        foreach (float a in new[] { -.42f, .42f })
        {
            ChairMesh(c, new HF(k.P(a, 0f, -.15f), -Vector3.forward), Linen, OakSolid);
            ChairMesh(c, new HF(k.P(a, 0f, Wd + .15f), Vector3.forward), Linen, OakSolid);
        }
    }
    /// <summary>A dining set: a solid oak table 160 x 90 and four upholstered chairs (a seat each).</summary>
    public static void DiningSet(Ctx c, Vector3 p, Vector3 f, string label = "krzesło")
    {
        HomeMaterials();
        Place(c, "zestaw jadalniany", DiningMesh, p, f, null);
        var k = new HF(p, f);
        foreach (float a in new[] { -.42f, .42f })
        {
            var front = new HF(k.P(a, 0f, -.15f), -k.f); c.seat?.Invoke(front.P(0f, .51f, .22f), front.f, label);
            var back = new HF(k.P(a, 0f, .9f + .15f), k.f); c.seat?.Invoke(back.P(0f, .51f, .22f), back.f, label);
        }
        ColAt(c, p, f, -.8f, .8f, 0f, .9f, .76f, "stół");
    }
    // ---------------------------------------------------------------- bed
    static void BedMesh(Ctx c, float Wb)
    {
        var k = new HF(Vector3.zero, Vector3.forward); float Lb = 2.0f;
        foreach (float a in new[] { -Wb * .5f, 0f, Wb * .5f }) foreach (float d in new[] { .05f, Lb }) Leg(c, Walnut, k.P(a * .96f, 0f, d), .07f, .02f, .026f);
        c.m.RoundedBox(Linen, k.P(0f, .20f, (Lb + .10f) * .5f - .05f), new Vector3(Wb + .12f, .26f, Lb + .10f), .05f, k.rot, 2);
        c.m.RoundedBox(Cotton, k.P(0f, .45f, Lb * .5f), new Vector3(Wb, .24f, Lb - .04f), .07f, k.rot, 3);
        float dl = Lb - .55f;
        c.m.RoundedBox(Cotton, k.P(0f, .605f, dl * .5f - .03f), new Vector3(Wb + .10f, .07f, dl + .06f), .035f, k.rot, 3);
        foreach (float s in new[] { -1f, 1f }) c.m.RoundedBox(Cotton, k.P(s * (Wb * .5f + .035f), .49f, dl * .5f - .03f), new Vector3(.03f, .25f, dl + .04f), .014f, k.rot, 2);
        c.m.RoundedBox(Cotton, k.P(0f, .645f, dl - .08f), new Vector3(Wb + .08f, .08f, .22f), .04f, k.rot, 3);
        c.m.RoundedBox(VelvetGreen, k.P(0f, .655f, .34f), new Vector3(Wb + .16f, .028f, .56f), .013f, k.rot, 2);
        foreach (float s in new[] { -1f, 1f }) c.m.RoundedBox(VelvetGreen, k.P(s * (Wb * .5f + .072f), .50f, .34f), new Vector3(.026f, .30f, .56f), .012f, k.rot, 2);
        foreach (float s in new[] { -1f, 1f }) c.m.RoundedBox(Cotton, k.P(s * Wb * .25f, .66f, Lb - .30f), new Vector3(Wb * .5f - .06f, .16f, .46f), .08f, k.Lean(22f), 3);
        foreach (float s in new[] { -1f, 1f }) c.m.RoundedBox(VelvetGreen, k.P(s * Wb * .2f, .70f, Lb - .52f), new Vector3(.50f, .40f, .14f), .07f, k.Lean(16f, s * 6f), 3);
        float hw = Wb + .30f; int nc = Mathf.RoundToInt(hw / .15f); float cwid = hw / nc;
        c.m.RoundedBox(Boucle, k.P(0f, .90f, Lb + .14f), new Vector3(hw + .02f, 1.16f, .06f), .025f, k.rot, 2);
        for (int i = 0; i < nc; i++) c.m.RoundedBox(Boucle, k.P(-hw * .5f + (i + .5f) * cwid, .90f, Lb + .07f), new Vector3(cwid - .008f, 1.12f, .12f), .055f, k.rot, 3);
        // bedside tables: walnut, two drawers with black pulls, black legs, a lamp each
        foreach (float s in new[] { -1f, 1f })
        {
            var n = new HF(k.P(s * (hw * .5f + .30f), 0f, Lb - .42f), Vector3.forward);
            foreach (float a in new[] { -.21f, .21f }) foreach (float d in new[] { .05f, .37f }) c.m.RoundedBox(BlackMatt, n.P(a, .07f, d), new Vector3(.025f, .14f, .025f), .007f, n.rot, 1);
            c.m.RoundedBox(Walnut, n.P(0f, .35f, .22f), new Vector3(.50f, .42f, .40f), .02f, n.rot, 2);
            foreach (float yy in new[] { .255f, .455f })
            {
                c.m.RoundedBox(Walnut, n.P(0f, yy, .008f), new Vector3(.46f, .18f, .024f), .008f, n.rot, 1);
                c.m.RoundedBox(BlackMatt, n.P(0f, yy + .04f, -.012f), new Vector3(.14f, .014f, .018f), .006f, n.rot, 1);
            }
            var lb = n.P(.06f, .56f, .24f);
            c.m.Lathe(Stone, lb, new[] { new Vector2(.07f, 0f), new Vector2(.085f, .03f), new Vector2(.10f, .12f), new Vector2(.075f, .20f), new Vector2(.02f, .24f) }, 20, Quaternion.identity, true, true);
            c.m.Cylinder(BlackMatt, lb + Vector3.up * .24f, .007f, .06f, 8, Quaternion.identity, true, false);
            c.m.Cylinder(Led, lb + Vector3.up * .27f, .022f, .05f, 10, Quaternion.identity, true, true);
            c.m.Cylinder(Shade, lb + Vector3.up * .25f, .17f, .17f, 24, Quaternion.identity, false, false, .13f);
        }
    }
    /// <summary>A double bed (Wb 1.6 / 1.8) with its headboard and two bedside tables with lamps; p = the middle of its foot, f = toward the foot.</summary>
    public static void Bed(Ctx c, Vector3 p, Vector3 f, float Wb, string label = "łóżko")
    {
        HomeMaterials();
        Wb = Wb > 1.7f ? 1.8f : 1.6f;
        Place(c, "łóżko " + Wb.ToString("0.0"), cc => BedMesh(cc, Wb), p, f, null);
        foreach (float s in new[] { -1f, 1f }) SeatAt(c, p, f, s * Wb * .32f, .57f, .84f, label);
        ColAt(c, p, f, -Wb * .5f - .08f, Wb * .5f + .08f, -.03f, 2.18f, .60f, label);
    }
    // ---------------------------------------------------------------- rug, lamp
    /// <summary>A rug 12 mm on the floor, sx x sz (axis-aligned to p's frame).</summary>
    public static void Rug(Ctx c, Vector3 p, Vector3 f, float sx, float sz, bool beige)
    {
        HomeMaterials();
        sx = Mathf.Round(sx * 5f) / 5f; sz = Mathf.Round(sz * 5f) / 5f;
        Place(c, "dywan " + sx.ToString("0.0") + "x" + sz.ToString("0.0") + (beige ? " b" : " g"), cc => cc.m.RoundedBox(beige ? RugBeige : RugGrey, Vector3.up * .006f, new Vector3(sx, .012f, sz), .005f, Quaternion.identity, 1), p, f, null);
    }
    /// <summary>A reading floor lamp: a disc base, a steel stem, a lit linen drum.</summary>
    public static void FloorLamp(Ctx c, Vector3 p)
    {
        HomeMaterials();
        Place(c, "lampa podłogowa", cc =>
        {
            cc.m.Cylinder(BlackMatt, Vector3.zero, .16f, .025f, 24, Quaternion.identity, true, false, .15f);
            cc.m.Cylinder(Steel, Vector3.up * .025f, .012f, 1.40f, 10, Quaternion.identity, true, false);
            cc.m.Cylinder(Led, Vector3.up * 1.38f, .03f, .06f, 12, Quaternion.identity, true, true);
            cc.m.Cylinder(Shade, Vector3.up * 1.34f, .22f, .30f, 24, Quaternion.identity, false, false, .19f);
        }, p, Vector3.forward, null);
        var k = new Fr(p, Vector3.forward); Col(c, k, -.16f, .16f, 0f, 1.65f, -.16f, .16f, "lampa");
    }
    // ---------------------------------------------------------------- WC
    static void WcMesh(Ctx c)
    {
        var f = Vector3.forward; var rot = Quaternion.identity; var r = Vector3.right; var wall = Vector3.zero;
        c.m.RoundedBox(Porcelain, wall + f * .28f + Vector3.up * .27f, new Vector3(.36f, .28f, .54f), .13f, rot, 3);
        c.m.RoundedBox(Porcelain, wall + f * .06f + Vector3.up * .27f, new Vector3(.40f, .34f, .10f), .04f, rot, 2);
        var ring = new List<Vector3>();
        for (int k = 0; k <= 28; k++) { float t = k * Mathf.PI * 2f / 28f; ring.Add(wall + f * (.31f + .22f * Mathf.Sin(t)) + r * (.165f * Mathf.Cos(t)) + Vector3.up * .428f); }
        for (int k = 0; k + 1 < ring.Count; k++) c.m.Tube(Porcelain, ring[k], ring[k + 1], .016f, 8);
        c.m.RoundedBox(Porcelain, wall + f * .31f + Vector3.up * .452f, new Vector3(.35f, .022f, .46f), .010f, rot, 2);
        c.m.RoundedBox(Chrome, wall + f * .008f + Vector3.up * 1.02f, new Vector3(.25f, .16f, .016f), .008f, rot, 2);
        c.m.RoundedBox(Chrome, wall + f * .022f + Vector3.up * 1.02f - r * .035f, new Vector3(.10f, .12f, .014f), .006f, rot, 2);
        c.m.RoundedBox(Chrome, wall + f * .022f + Vector3.up * 1.02f + r * .06f, new Vector3(.06f, .12f, .014f), .006f, rot, 2);
        var rh = wall + r * .42f + Vector3.up * .70f;
        c.m.Tube(Chrome, rh + f * .012f, rh + f * .10f, .008f, 8); c.m.Tube(Chrome, rh + f * .10f, rh + f * .10f - r * .13f, .008f, 8);
        c.m.Cylinder(Porcelain, rh + f * .10f - r * .005f, .055f, .11f, 18, Quaternion.FromToRotation(Vector3.up, -r), true, true);
    }
    /// <summary>A wall-hung WC (pan, seat ring and lid, the chrome flush plate, a roll holder): wall = the wall point at the floor, f = into the room.</summary>
    public static void WcPan(Ctx c, Vector3 wall, Vector3 f)
    {
        Materials();
        Place(c, "WC podwieszane", WcMesh, wall, f, null);
        var k = new Fr(wall, f); Col(c, k, -.2f, .2f, 0f, .46f, .02f, .58f, "WC");
        c.seat?.Invoke(wall + k.f * .31f + Vector3.up * .46f, k.f, "WC");
    }
}
