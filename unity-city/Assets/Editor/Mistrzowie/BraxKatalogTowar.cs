using UnityEngine;

// BRAXHORN KATALOG — TOWAR (owner 11.11 19:52 "te sklepy są niewyposażone", 20:10 "wszystkie sklepy, które są potrzebne ...
// do domu wszystko", 20:12 "każdy detal"): the goods of the gallery's fashion, sport and toy shops as catalogue pieces, so
// the flats' wardrobes, the hotel rooms and the other region use the same clothes, shoes, balls and toys. Built from code
// with Poly Haven fabrics (cotton_jersey, crepe_satin, terlenka, poly_wool_herringbone, curly_teddy_natural, Leather037);
// zero blocks: every piece has its real shape (a garment on a wire hanger with sleeves and a collar, a dress form, a
// sneaker with a sole and a collar, a ball, a hex dumbbell, a teddy bear). Zero flicker: proud details >= 12 mm, stacked
// pieces rest face to face (hidden contact), nothing coplanar.
public static partial class BraxKatalog
{
    public static Material[] Tees, Shirts, Coats, Dresses, Trousers, SportCols;
    public static Material Teddy, TeddyLight, ToyRed, ToyBlue, ToyYellow, ToyGreen, Leather;
    public static void ClothesMaterials()
    {
        Materials();
        if (Tees != null) return;
        Tees = new[]
        {
            T("tee_black", "cotton_jersey", .5f, new Color(.10f, .10f, .11f)), T("tee_white", "cotton_jersey", .5f, new Color(1.55f, 1.55f, 1.52f)),
            T("tee_grey", "cotton_jersey", .5f, new Color(.62f, .63f, .65f)), T("tee_red", "cotton_jersey", .5f, new Color(1.10f, .14f, .16f)),
            T("tee_navy", "cotton_jersey", .5f, new Color(.16f, .22f, .42f)), T("tee_sand", "cotton_jersey", .5f, new Color(1.15f, 1.00f, .80f)),
        };
        Shirts = new[]
        {
            T("shirt_white", "terlenka", .4f, new Color(1.50f, 1.50f, 1.50f)), T("shirt_sky", "terlenka", .4f, new Color(.80f, .95f, 1.25f)),
            T("shirt_black", "terlenka", .4f, new Color(.12f, .12f, .13f)), T("shirt_olive", "terlenka", .4f, new Color(.55f, .58f, .40f)),
        };
        Coats = new[]
        {
            T("coat_charcoal", "poly_wool_herringbone", .5f, new Color(.30f, .30f, .32f)), T("coat_camel", "poly_wool_herringbone", .5f, new Color(1.05f, .80f, .55f)),
            T("coat_black", "poly_wool_herringbone", .5f, new Color(.12f, .12f, .13f)), T("coat_navy", "poly_wool_herringbone", .5f, new Color(.20f, .25f, .42f)),
        };
        Dresses = new[]
        {
            T("dress_black", "crepe_satin", .5f, new Color(.08f, .08f, .09f)), T("dress_wine", "crepe_satin", .5f, new Color(.55f, .08f, .14f)),
            T("dress_champagne", "crepe_satin", .5f, new Color(1.25f, 1.10f, .90f)), T("dress_red", "crepe_satin", .5f, new Color(1.0f, .10f, .12f)),
        };
        Trousers = new[]
        {
            T("trousers_black", "terlenka", .4f, new Color(.10f, .10f, .11f)), T("trousers_denim", "terlenka", .4f, new Color(.28f, .38f, .62f)),
            T("trousers_beige", "terlenka", .4f, new Color(1.05f, .95f, .78f)), T("trousers_grey", "terlenka", .4f, new Color(.45f, .46f, .48f)),
        };
        SportCols = new[]
        {
            T("sport_red", "cotton_jersey", .5f, new Color(1.15f, .10f, .12f)), T("sport_black", "cotton_jersey", .5f, new Color(.08f, .08f, .09f)),
            T("sport_white", "cotton_jersey", .5f, new Color(1.6f, 1.6f, 1.6f)), T("sport_blue", "cotton_jersey", .5f, new Color(.18f, .35f, .95f)),
        };
        Teddy = T("teddy_brown", "curly_teddy_natural", .25f, new Color(.95f, .75f, .55f));
        TeddyLight = T("teddy_cream", "curly_teddy_natural", .25f, new Color(1.35f, 1.25f, 1.10f));
        ToyRed = S("toy_red", new Color(.85f, .08f, .10f), .25f, 0f); ToyBlue = S("toy_blue", new Color(.10f, .32f, .85f), .25f, 0f);
        ToyYellow = S("toy_yellow", new Color(.98f, .78f, .10f), .25f, 0f); ToyGreen = S("toy_green", new Color(.15f, .65f, .30f), .25f, 0f);
        Leather = T("leather_black", "Leather037", .4f, new Color(.30f, .30f, .32f));
    }

    // ---------------------------------------------------------------- shared pieces (one mesh, many placements)
    // A piece repeated hundreds of times (clothes, shoes, toys, bags) is built ONCE at the origin into its meshes and then
    // placed as light renderers sharing them: no static batching, GPU instancing on (the phone's memory: a shop full of
    // clothes costs one mesh per kind, not one per hanger). Slot marks the piece's colour, swapped per placement.
    public static Material Slot;
    static readonly System.Collections.Generic.Dictionary<string, (Mesh mesh, Material mat)[]> wzory = new System.Collections.Generic.Dictionary<string, (Mesh, Material)[]>();
    public static int Placed;
    /// <summary>Places the shared piece `key` (built by `build` at the origin, its front +z) at p facing f, its colour slot
    /// in `colour`, under c.inst (a culled goods group) or c.root.</summary>
    public static GameObject Place(Ctx c, string key, System.Action<Ctx> build, Vector3 p, Vector3 f, Material colour, float scale = 1f, bool cloth = false)
    {
        Materials(); if (!Slot) Slot = S("colour_slot", new Color(1f, 0f, 1f), .5f, 0f);
        if (!wzory.TryGetValue(key, out var parts))
        {
            var lm = new BraxChampionsMeshBatch();
            build(new Ctx { m = lm, root = c.root, cols = c.cols, unit = c.unit, water = c.water, seat = c.seat });
            var tmp = new GameObject("BraxKatalog wzory (tmp)");
            var g = lm.Build(tmp.transform, "BraxKatalog wzór " + key, Folder, false, c.unit);
            var list = new System.Collections.Generic.List<(Mesh, Material)>();
            foreach (var mf in g.GetComponentsInChildren<MeshFilter>(true)) { var mt = mf.GetComponent<MeshRenderer>().sharedMaterial; if (mt) mt.enableInstancing = true; list.Add((mf.sharedMesh, mt)); }
            parts = list.ToArray(); wzory[key] = parts;
            Object.DestroyImmediate(tmp);
        }
        if (colour) { colour.enableInstancing = true; if (cloth) Cloth(colour); }
        f.y = 0f; if (f.sqrMagnitude < 1e-6f) f = Vector3.forward;
        var o = new GameObject(key); o.transform.SetParent(c.inst ? c.inst : c.root, false);
        o.transform.localPosition = p * c.unit; o.transform.localRotation = Quaternion.LookRotation(f.normalized, Vector3.up); o.transform.localScale = Vector3.one * scale;
        foreach (var (mesh, mt) in parts)
        {
            var ch = new GameObject(key + " | " + (mt == Slot && colour ? colour.name : mt ? mt.name : "")); ch.transform.SetParent(o.transform, false);
            ch.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = ch.AddComponent<MeshRenderer>(); r.sharedMaterial = mt == Slot && colour ? colour : mt;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = true;
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(ch, UnityEditor.StaticEditorFlags.OccludeeStatic);
        }
        Placed++;
        return o;
    }
    static int Var(Vector3 p, int n) { int h = (int)(p.x * 13.7f + p.z * 7.1f + p.y * 3.3f); return ((h % n) + n) % n; }

    /// <summary>A garment "sewn" on a hanger (see GarmentMesh); shared meshes: 3 drape variants per kind.</summary>
    public static void Garment(Ctx c, Vector3 hook, Vector3 along, int kind, Material mat)
    { int v = Var(hook, 3); Place(c, "ubranie " + kind + "-" + v, cc => GarmentMesh(cc, Vector3.zero, Vector3.forward, kind, Slot, v * 2.1f + .4f), hook, along, mat, 1f, true); }
    /// <summary>A stack of n (5-7) folded T-shirts: p = the base centre, f = the folded front.</summary>
    public static void FoldedStack(Ctx c, Vector3 p, Vector3 f, Material mat, int n)
    { Place(c, "stos " + n, cc => FoldedStackMesh(cc, Vector3.zero, Vector3.forward, Slot, n), p, f, mat, 1f, true); }
    /// <summary>A sneaker: p = the sole's centre on the shelf, f = where the toe points.</summary>
    public static void Sneaker(Ctx c, Vector3 p, Vector3 f, Material upper)
    { Place(c, "but sportowy", cc => SneakerMesh(cc, Vector3.zero, Vector3.forward, Slot), p, f, upper); }
    /// <summary>A ball: kind 0 football, 1 basketball, 2 volleyball; p = its contact point.</summary>
    public static void Ball(Ctx c, Vector3 p, int kind)
    { Place(c, "piłka " + kind, cc => BallMesh(cc, Vector3.zero, kind), p, Vector3.forward, null); }
    /// <summary>A plush teddy bear sitting: p = its seat point, f = where it looks, s = its scale (1 = 30 cm).</summary>
    public static void TeddyBear(Ctx c, Vector3 p, Vector3 f, float s, bool light = false)
    { Place(c, "miś " + (light ? "kremowy" : "brązowy"), cc => TeddyMesh(cc, Vector3.zero, Vector3.forward, 1f, light), p, f, null, s); }
    /// <summary>A die-cast toy car (1:18): p = its centre on the shelf, f = its nose.</summary>
    public static void ToyCar(Ctx c, Vector3 p, Vector3 f, Material body)
    { Place(c, "autko", cc => ToyCarMesh(cc, Vector3.zero, Vector3.forward, Slot), p, f, body); }
    /// <summary>A wooden stacking-ring tower: p = its base centre.</summary>
    public static void StackingRings(Ctx c, Vector3 p)
    { Place(c, "wieża z kółek", cc => RingsMesh(cc, Vector3.zero), p, Vector3.forward, null); }
    /// <summary>A sewn leather handbag: p = its base centre, f = its front, w = .28 / .32 / .36 / .40 m.</summary>
    public static void Handbag(Ctx c, Vector3 p, Vector3 f, Material leather, float w = .32f)
    { int wi = Mathf.Clamp(Mathf.RoundToInt((w - .28f) / .04f), 0, 3); Place(c, "torebka " + wi, cc => HandbagMesh(cc, Vector3.zero, Vector3.forward, Slot, .28f + wi * .04f), p, f, leather); }
    /// <summary>A hard-shell suitcase on spinner wheels: p = the floor point, f = its front, tall = 75 cm.</summary>
    public static void Suitcase(Ctx c, Vector3 p, Vector3 f, Material shell, bool tall)
    { Place(c, "walizka " + (tall ? "duża" : "kabinowa"), cc => SuitcaseMesh(cc, Vector3.zero, Vector3.forward, Slot, tall), p, f, shell); }

    // ---------------------------------------------------------------- clothes
    /// <summary>A garment "sewn" on a chrome wire hanger (owner 21:24: "możesz uszyć ciuchy ... pokroić, uszyć wszystko
    /// ładnie"): every piece a smooth surface cut to its pattern - the body an open tube (front and back panels joined at the
    /// side seams, open hem, the neckline cut out, sloping shoulder seams), the sleeves tapered open tubes set into the
    /// armholes, the drape folds strongest at the hem; the placket, pockets and lapels raised in the cloth itself (no overlay
    /// faces). The cloth is double-sided (the inside shows at the hem, the neck and the cuffs). hook = the top of the hook on
    /// the rail; along = the rail's direction (the garment stands across it). kind: 0 T-shirt, 1 shirt (collar, placket,
    /// buttons, curved tails), 2 coat (lapels, front overlap, buttons, flap pockets), 3 trousers (clip hanger, waistband,
    /// two legs, creases), 4 dress (straps, bodice, flared skirt), 5 sports jersey (raglan, badge), 6 hoodie (hood, kangaroo
    /// pocket, drawstrings).</summary>
    static void GarmentMesh(Ctx c, Vector3 hook, Vector3 along, int kind, Material mat, float ph)
    {
        ClothesMaterials(); Cloth(mat);
        var k = new Fr(hook, along);
        if (kind == 3)
        {
            // a clip hanger: the bar, two clips; the trousers hang from the waist
            c.m.Tube(Chrome, k.P(-.012f, .018f, 0f), k.P(0f, -.05f, 0f), .0022f, 5);
            c.m.Tube(Chrome, k.P(-.19f, -.05f, 0f), k.P(.19f, -.05f, 0f), .004f, 6);
            foreach (float s in new[] { -1f, 1f }) RB(c, BlackMatt, k, s * .15f - .018f, s * .15f + .018f, -.085f, -.045f, -.012f, .012f, .004f, 1);
            TrouserLegs(c, k, -.075f, mat, ph);
            return;
        }
        c.m.Tube(Chrome, k.P(-.012f, .018f, 0f), k.P(0f, -.045f, 0f), .0022f, 5);
        c.m.Tube(Chrome, k.P(0f, -.045f, 0f), k.P(-.205f, -.095f, 0f), .0022f, 5); c.m.Tube(Chrome, k.P(0f, -.045f, 0f), k.P(.205f, -.095f, 0f), .0022f, 5);
        c.m.Tube(Chrome, k.P(-.205f, -.095f, 0f), k.P(.205f, -.095f, 0f), .0022f, 5);
        float top = -.06f;
        // the pattern of the body
        float L = kind == 0 || kind == 5 ? .68f : kind == 1 ? .76f : kind == 6 ? .70f : kind == 4 ? 1.02f : .86f;
        float W = kind == 2 ? .235f : kind == 4 ? .165f : kind == 6 ? .235f : .215f;
        float T = kind == 2 ? .034f : kind == 6 ? .032f : kind == 4 ? .022f : .020f;
        float neckW = kind == 4 ? .10f : kind == 2 ? .075f : .068f, neckD = kind == 4 ? .14f : kind == 2 ? .26f : kind == 1 ? .06f : .075f;
        float Half(float v)
        {
            if (kind == 4) { float sk = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((.55f - v) / .55f)); return Mathf.Lerp(W, W * 1.75f, sk) - (v > .45f && v < .7f ? .012f : 0f); }
            float hem = kind == 2 ? W * 1.04f : W * .97f;
            float w = Mathf.Lerp(hem, W, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(v / .6f)));
            return w * (1f - .10f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((v - .88f) / .12f)));
        }
        Vector3 Body(float u, float v)
        {
            float a = u * Mathf.PI * 2f, cs = Mathf.Cos(a), sn = Mathf.Sin(a), w = Half(v), x = cs * w, ax = Mathf.Abs(x);
            float th = T * (.30f + .70f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(v * 1.15f))) + .002f;
            // the neck: the front and back stand apart over the neckline, the shoulder seams closed outside it
            float nk = Mathf.Exp(-Mathf.Pow(x / neckW, 4f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((v - .80f) / .20f));
            if (v > .97f) th = Mathf.Lerp(th, .003f + .016f * nk, (v - .97f) / .03f);
            float z = sn * th;
            float drape = (.006f + (kind == 4 ? .016f : .004f)) * Mathf.Pow(1f - v, 1.6f) * Mathf.Sin(x * (kind == 4 ? 46f : 30f) + ph);
            z += drape * Mathf.Abs(sn);
            if (sn > 0f)
            {
                if (kind == 1 && v < .97f) z += .0035f * Mathf.Exp(-Mathf.Pow(x / .014f, 2f));                                       // the placket
                if (kind == 2) { z += .006f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((x + .015f) / .006f));                               // the front overlap
                                 float lap = Mathf.Clamp01(1f - Mathf.Abs(ax - (.03f + .08f * (v - .55f) / .45f)) / .035f) * (v > .55f ? 1f : 0f);
                                 z += .005f * lap * sn;                                                                                    // the lapels
                                 if (v > .14f && v < .26f && ax > .08f && ax < .19f) z += .004f * sn; }                                    // the flap pockets
                if (kind == 6 && v > .07f && v < .32f && ax < .13f) z += .006f * sn * Mathf.Clamp01((.13f - ax) / .02f);                  // the kangaroo pocket
            }
            float y = top - L * (1f - v);
            if (kind == 1 && v < .2f) y += .05f * (ax / W) * (ax / W) * (1f - v / .2f);                                                  // curved shirt tails
            y -= .055f * Mathf.Pow(ax / Mathf.Max(W, .01f), 1.3f) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((v - .75f) / .25f));          // sloping shoulders
            y -= neckD * nk * Mathf.Max(sn, 0f) + .02f * nk * Mathf.Max(-sn, 0f);                                                       // the neckline (deep front)
            return k.P(x, y, z);
        }
        c.m.Surface(mat, Body, 12, 7);
        // the sleeves: open tapered tubes from the armholes (short and lifted: tees, jerseys; long and hanging: the rest)
        if (kind != 4)
            foreach (float s in new[] { -1f, 1f })
            {
                bool shortS = kind == 0 || kind == 5;
                float len = shortS ? .20f : kind == 2 ? .64f : .60f, tilt = shortS ? 50f : 9f;
                float r0 = shortS ? .085f : .075f, r1 = shortS ? .072f : kind == 2 ? .060f : .048f, dep = shortS ? .45f : .55f;
                var dir = Quaternion.AngleAxis(s * tilt, k.f) * Vector3.down;
                var e1 = Vector3.Cross(k.f, dir).normalized;
                var st = k.P(s * (Half(.9f) - .045f), top - .07f, 0f);
                float fz = ph + s;
                c.m.Surface(mat, (u, v) =>
                {
                    float a = u * Mathf.PI * 2f, r = Mathf.Lerp(r0, r1, v) * (1f + (shortS ? 0f : .06f * Mathf.Sin(v * 18f + fz) * v));
                    return st + dir * (len * v) + e1 * (Mathf.Cos(a) * r) + k.f * (Mathf.Sin(a) * r * dep);
                }, 6, shortS ? 2 : 4);
                if (!shortS && kind != 6) c.m.Surface(mat, (u, v) =>                                                                     // the cuff
                {
                    float a = u * Mathf.PI * 2f, r = r1 * 1.07f;
                    return st + dir * (len - .06f + .06f * v) + e1 * (Mathf.Cos(a) * r) + k.f * (Mathf.Sin(a) * r * dep);
                }, 6, 1);
            }
        else
            foreach (float s in new[] { -1f, 1f }) c.m.Tube(mat, k.P(s * .085f, top - .095f, 0f), k.P(s * .12f, top - .015f, 0f), .006f, 5);   // the straps
        if (kind == 1)
        {
            // the collar: a stand and a fall band round the neck, open at the front
            c.m.Surface(mat, (u, v) =>
            {
                float a = Mathf.Lerp(.30f, Mathf.PI * 2f - .30f, u) + Mathf.PI * .5f, cx = Mathf.Cos(a), sz = Mathf.Sin(a);
                float rx = neckW + .012f + .018f * v, rz = .024f + .014f * v;
                return k.P(cx * rx, top - .012f + .040f * v - .03f * Mathf.Max(sz, 0f) * (1f - v) - .02f * Mathf.Max(sz, 0f), sz * rz);
            }, 10, 1);
            for (int i = 0; i < 6; i++) c.m.Cylinder(WhiteGloss, Body(.25f, .88f - i * .135f) - k.f * .0005f, .0055f, .0035f, 8, Quaternion.FromToRotation(Vector3.up, k.f), true);   // on the placket
        }
        else if (kind == 2)
            for (int i = 0; i < 3; i++) c.m.Cylinder(BlackGloss, Body(.225f, .52f - i * .16f) - k.f * .001f, .0085f, .005f, 10, Quaternion.FromToRotation(Vector3.up, k.f), true);
        else if (kind == 6)
        {
            // the hood lies on the back; two drawstrings with metal tips down the front
            c.m.Surface(mat, (u, v) =>
            {
                float a = u * Mathf.PI * 2f, b = (v - .5f) * Mathf.PI;
                return k.P(Mathf.Cos(b) * Mathf.Cos(a) * .15f, top - .10f + Mathf.Sin(b) * .13f, -T - .03f + Mathf.Cos(b) * Mathf.Sin(a) * .045f);
            }, 8, 4);
            foreach (float s in new[] { -1f, 1f })
            {
                var a = Body(.25f - s * .03f, .95f) + k.f * .003f; var b = Body(.25f - s * .035f, .64f) + k.f * .003f;
                c.m.Tube(WhiteGloss, a, b, .0035f, 5); c.m.Tube(Chrome, b, b + Vector3.down * .025f, .0042f, 6);
            }
        }
        else if (kind == 5)
        {
            var q = Body(.27f, .80f) - k.p; float lx = Vector3.Dot(q, k.r), ly = q.y, lz = Vector3.Dot(q, k.f);
            RB(c, SportCols[2], k, lx - .025f, lx + .025f, ly - .025f, ly + .025f, lz - .004f, lz + .003f, .002f, 1);   // the badge (5 x 5 cm, sewn on)
        }
    }
    // trousers from the waist down: the seat (an open tube) and two legs with front creases and folds at the hem
    static void TrouserLegs(Ctx c, Fr k, float y0, Material mat, float ph)
    {
        c.m.Surface(mat, (u, v) =>
        {
            float a = u * Mathf.PI * 2f, w = .185f + .012f * v, t = .030f * (.6f + .4f * Mathf.Sin(Mathf.PI * v)) + .004f;
            return k.P(Mathf.Cos(a) * w, y0 - .30f * v, Mathf.Sin(a) * t);
        }, 10, 3);
        RB(c, mat, k, -.190f, .190f, y0 - .045f, y0, -.027f, .027f, .008f, 1);                        // the waistband (a sewn-on band)
        foreach (float s in new[] { -1f, 1f })
            c.m.Surface(mat, (u, v) =>
            {
                float a = u * Mathf.PI * 2f, w = Mathf.Lerp(.092f, .074f, v), t = Mathf.Lerp(.030f, .020f, v);
                float crease = .004f * Mathf.Max(Mathf.Sin(a), 0f) * Mathf.Exp(-Mathf.Pow(Mathf.Cos(a) / .2f, 2f));
                float fold = .008f * v * v * Mathf.Sin(v * 22f + ph + s);
                return k.P(s * .093f + Mathf.Cos(a) * w, y0 - .22f - .80f * v, Mathf.Sin(a) * (t + fold) + crease);
            }, 8, 5);
    }
    // the cloth shows both of its sides (the inside at the hem, the neck, the cuffs)
    static readonly System.Collections.Generic.HashSet<Material> twoSided = new System.Collections.Generic.HashSet<Material>();
    static void Cloth(Material m)
    {
        if (!m || twoSided.Contains(m)) return;
        m.SetFloat("_Cull", 0f); m.enableInstancing = true; UnityEditor.EditorUtility.SetDirty(m); twoSided.Add(m);
    }
    /// <summary>A stack of n folded T-shirts / jumpers on a table or shelf: p = the stack's base centre, f = its folded front.</summary>
    static void FoldedStackMesh(Ctx c, Vector3 p, Vector3 f, Material mat, int n)
    {
        var k = new Fr(p, f);
        for (int i = 0; i < n; i++)
        {
            float o = (i % 2 == 0 ? .003f : -.003f);
            RB(c, mat, k, -.15f + o, .15f + o, i * .034f, (i + 1) * .034f, -.12f, .12f, .012f);
        }
    }
    /// <summary>A dress form (tailor's mannequin) wearing a garment colour: p = its floor point, f = its front.</summary>
    public static void Mannequin(Ctx c, Vector3 p, Vector3 f, Material dress)
    {
        Materials();
        var k = new Fr(p, f);
        c.m.Cylinder(BlackMatt, p, .19f, .025f, 28, Quaternion.identity, true);
        c.m.Cylinder(Chrome, p + Vector3.up * .025f, .013f, .93f, 12, Quaternion.identity, false);
        c.m.Lathe(dress, p + Vector3.up * .95f, new[]
        {
            new Vector2(0f, 0f), new Vector2(.15f, 0f), new Vector2(.175f, .06f), new Vector2(.165f, .20f), new Vector2(.135f, .32f),
            new Vector2(.155f, .42f), new Vector2(.175f, .50f), new Vector2(.165f, .58f), new Vector2(.12f, .64f), new Vector2(.05f, .67f), new Vector2(0f, .675f),
        }, 28, k.rot, true, true);
        c.m.Cylinder(BlackMatt, p + Vector3.up * 1.62f, .045f, .07f, 16, Quaternion.identity, true);
        c.m.Cylinder(Chrome, p + Vector3.up * 1.69f, .02f, .025f, 12, Quaternion.identity, true);
        Col(c, k, -.2f, .2f, 0f, 1.7f, -.2f, .2f, "manekin");
    }

    // ---------------------------------------------------------------- shoes and sport
    /// <summary>A sneaker: p = the sole's centre on the shelf, f = where the toe points.</summary>
    static void SneakerMesh(Ctx c, Vector3 p, Vector3 f, Material upper)
    {
        Materials();
        var k = new Fr(p, f);
        RB(c, WhiteGloss, k, -.052f, .052f, 0f, .030f, -.145f, .145f, .014f, 1);                 // the sole
        RB(c, upper, k, -.047f, .047f, .030f, .085f, -.135f, .095f, .026f, 1);                   // the upper
        RB(c, upper, k, -.045f, .045f, .030f, .060f, .060f, .140f, .028f, 1);                    // the toe box
        RB(c, upper, k, -.044f, .044f, .060f, .118f, -.135f, -.070f, .02f, 1);                   // the heel collar
        RB(c, WhiteGloss, k, -.020f, .020f, .085f, .097f, -.050f, .060f, .005f);              // the laces
        RB(c, BlackMatt, k, -.059f, -.047f, .040f, .060f, -.060f, .030f, .004f);              // the side stripe (12 mm proud)
        RB(c, BlackMatt, k, .047f, .059f, .040f, .060f, -.060f, .030f, .004f);
    }
    /// <summary>A ball on its stand: kind 0 football (white, black patches as rings), 1 basketball (orange, black seams), 2 volleyball.</summary>
    static void BallMesh(Ctx c, Vector3 p, int kind)
    {
        ClothesMaterials();
        float r = kind == 1 ? .121f : kind == 0 ? .110f : .105f;
        var body = kind == 1 ? S("ball_orange", new Color(.92f, .42f, .10f), .55f, 0f) : WhiteGloss;
        var prof = new Vector2[9];
        for (int i = 0; i <= 8; i++) { float a = Mathf.PI * i / 8f; prof[i] = new Vector2(Mathf.Sin(a) * r, r - Mathf.Cos(a) * r); }
        c.m.Lathe(body, p, prof, 16, Quaternion.identity, false, false);
        var seam = kind == 1 ? BlackMatt : kind == 0 ? BlackGloss : SportCols[3];
        foreach (var rot in new[] { Quaternion.Euler(90, 0, 0), Quaternion.Euler(0, 0, 90), Quaternion.Euler(90, 60, 0) })
        {
            var ring = new Vector2[] { new Vector2(r * .995f, -.004f), new Vector2(r + .003f, -.003f), new Vector2(r + .003f, .003f), new Vector2(r * .995f, .004f) };
            c.m.Lathe(seam, p + Vector3.up * r, ring, 20, rot, false, false);
        }
    }
    /// <summary>A rubber hex dumbbell: p = its centre on the rack, along = the handle's direction, kg sets the heads' size.</summary>
    public static void Dumbbell(Ctx c, Vector3 p, Vector3 along, float kg)
    {
        Materials();
        along.Normalize(); float hr = .045f + kg * .0028f, hw = .04f + kg * .002f;
        c.m.Tube(Chrome, p - along * .07f, p + along * .07f, .016f, 12);
        foreach (float s in new[] { -1f, 1f })
            c.m.Cylinder(Rubber, p + along * s * .07f, hr, hw, 6, Quaternion.FromToRotation(Vector3.up, along * s), true, true);
    }
    /// <summary>A rolled yoga / training mat standing or lying: p = its base centre, axis = the roll's axis.</summary>
    public static void MatRoll(Ctx c, Vector3 p, Vector3 axis, Material mat)
    {
        Materials();
        c.m.Cylinder(mat, p, .065f, .61f, 20, Quaternion.FromToRotation(Vector3.up, axis.normalized), true, true);
        c.m.Cylinder(BlackMatt, p + axis.normalized * .611f, .03f, .002f, 16, Quaternion.FromToRotation(Vector3.up, axis.normalized), true, false);
    }

    // ---------------------------------------------------------------- toys
    /// <summary>A plush teddy bear sitting: p = its seat point, f = where it looks, s = its scale (1 = 30 cm).</summary>
    static void TeddyMesh(Ctx c, Vector3 p, Vector3 f, float s, bool light)
    {
        ClothesMaterials();
        var k = new Fr(p, f); var fur = light ? TeddyLight : Teddy; var muzzle = light ? Teddy : TeddyLight;
        Vector2[] Egg(float rx, float h) { var o = new Vector2[7]; for (int i = 0; i <= 6; i++) { float a = Mathf.PI * i / 6f; o[i] = new Vector2(Mathf.Sin(a) * rx, h * .5f - Mathf.Cos(a) * h * .5f); } return o; }
        c.m.Lathe(fur, k.P(0f, 0f, 0f), Egg(.085f * s, .17f * s), 12, Quaternion.identity, false, false);                 // body
        c.m.Lathe(fur, k.P(0f, .15f * s, .005f * s), Egg(.065f * s, .13f * s), 12, Quaternion.identity, false, false);    // head
        c.m.Lathe(muzzle, k.P(0f, .185f * s, .055f * s), Egg(.030f * s, .05f * s), 12, Quaternion.identity, false, false);
        c.m.Lathe(BlackGloss, k.P(0f, .215f * s, .084f * s), Egg(.010f * s, .016f * s), 8, Quaternion.identity, false, false);   // the nose
        foreach (float sd in new[] { -1f, 1f })
        {
            c.m.Lathe(BlackGloss, k.P(sd * .026f * s, .235f * s, .058f * s), Egg(.007f * s, .014f * s), 8, Quaternion.identity, false, false);   // eyes
            c.m.Lathe(fur, k.P(sd * .052f * s, .255f * s, -.005f * s), Egg(.025f * s, .045f * s), 12, Quaternion.identity, false, false);         // ears
            c.m.Tube(fur, k.P(sd * .06f * s, .12f * s, .01f * s), k.P(sd * .095f * s, .05f * s, .05f * s), .024f * s, 8);                    // arms
            c.m.Tube(fur, k.P(sd * .045f * s, .03f * s, .02f * s), k.P(sd * .06f * s, .02f * s, .11f * s), .03f * s, 8);                     // legs
            c.m.Lathe(muzzle, k.P(sd * .06f * s, .026f * s, .10f * s), Egg(.026f * s, .040f * s), 10, Quaternion.FromToRotation(Vector3.up, k.f), false, false);   // the foot pads
        }
    }
    /// <summary>A toy car (die-cast scale 1:18): p = its centre on the shelf, f = its nose.</summary>
    static void ToyCarMesh(Ctx c, Vector3 p, Vector3 f, Material body)
    {
        Materials();
        var k = new Fr(p, f);
        RB(c, body, k, -.045f, .045f, .018f, .050f, -.120f, .120f, .012f);
        RB(c, body, k, -.040f, .040f, .050f, .082f, -.060f, .040f, .016f);
        RB(c, GlassDark, k, -.036f, .036f, .056f, .076f, .040f - .002f, .052f, .006f);        // the windscreen (12 mm proud of the roof's front)
        foreach (float a in new[] { -.047f, .047f }) foreach (float d in new[] { -.075f, .075f })
            c.m.Cylinder(Rubber, k.P(a, .020f, d), .020f, .014f, 14, Quaternion.FromToRotation(Vector3.up, k.r * Mathf.Sign(a)), true, true);
    }
    /// <summary>A wooden stacking-ring tower: p = its base centre.</summary>
    static void RingsMesh(Ctx c, Vector3 p)
    {
        ClothesMaterials();
        c.m.Cylinder(Oak, p, .06f, .02f, 20, Quaternion.identity, true);
        c.m.Cylinder(Oak, p + Vector3.up * .02f, .011f, .20f, 10, Quaternion.identity, true);
        var cols = new[] { ToyRed, ToyYellow, ToyGreen, ToyBlue, ToyRed };
        for (int i = 0; i < 5; i++)
        {
            float R = .055f - i * .008f, y0 = .02f + i * .034f;
            c.m.Lathe(cols[i], p + Vector3.up * y0, new[] { new Vector2(.012f, 0f), new Vector2(R, .004f), new Vector2(R, .030f), new Vector2(.012f, .034f) }, 20, Quaternion.identity, false, false);
        }
        var cap = new Vector2[7]; for (int i = 0; i <= 6; i++) { float a = Mathf.PI * .5f * i / 6f; cap[i] = new Vector2(Mathf.Cos(a) * .022f, Mathf.Sin(a) * .022f); }
        c.m.Lathe(ToyYellow, p + Vector3.up * .21f, cap, 14, Quaternion.identity, true, false);
    }

    // ---------------------------------------------------------------- bags
    /// <summary>A leather handbag standing on a shelf: p = its base centre, f = its front; w = its width (.28-.40).</summary>
    static void HandbagMesh(Ctx c, Vector3 p, Vector3 f, Material leather, float w)
    {
        Materials();
        var k = new Fr(p, f);
        RB(c, leather, k, -w * .5f, w * .5f, 0f, w * .62f, -.07f, .07f, .03f, 1);                       // the body
        RB(c, leather, k, -w * .5f + .02f, w * .5f - .02f, w * .40f, w * .58f, .07f, .082f, .01f, 1);    // the flap (12 mm proud)
        c.m.Cylinder(Chrome, k.P(0f, w * .45f, .082f), .010f, .004f, 12, Quaternion.FromToRotation(Vector3.up, k.f), true);   // the clasp
        foreach (float s in new[] { -1f, 1f })
        {
            var a = k.P(s * w * .22f, w * .62f, 0f); var top = k.P(s * w * .12f, w * .62f + .12f, 0f);
            c.m.Tube(leather, a, top, .007f, 6);
        }
        c.m.Tube(leather, k.P(-w * .12f, w * .62f + .12f, 0f), k.P(w * .12f, w * .62f + .12f, 0f), .007f, 6);   // the handle
    }
    /// <summary>A hard-shell cabin / check-in suitcase on its four spinner wheels: p = the floor point, f = its front, tall = the 75 cm size.</summary>
    static void SuitcaseMesh(Ctx c, Vector3 p, Vector3 f, Material shell, bool tall)
    {
        Materials();
        var k = new Fr(p, f); float H = tall ? .75f : .55f, W = tall ? .50f : .40f, D = tall ? .30f : .23f;
        RB(c, shell, k, -W * .5f, W * .5f, .05f, .05f + H, -D * .5f, D * .5f, .045f, 1);
        for (int i = 0; i < 3; i++) RB(c, shell, k, -W * .5f + .05f + i * (W - .10f) * .5f - .012f, -W * .5f + .05f + i * (W - .10f) * .5f + .012f, .10f, H, D * .5f, D * .5f + .012f, .004f, 1);   // ribs
        RB(c, BlackMatt, k, -W * .5f - .012f, W * .5f + .012f, .05f + H * .5f - .008f, .05f + H * .5f + .008f, -D * .5f - .012f, D * .5f + .024f, .004f, 1);   // the zip band (12 mm round the shell and its ribs)
        foreach (float a in new[] { -W * .5f + .05f, W * .5f - .05f }) foreach (float d in new[] { -D * .5f + .05f, D * .5f - .05f })
            c.m.Cylinder(Rubber, k.P(a, .0f, d), .025f, .05f, 10, Quaternion.identity, true);
        foreach (float a in new[] { -.10f, .10f }) c.m.Tube(Alu, k.P(a, .05f + H, -D * .5f + .03f), k.P(a, .05f + H + .10f, -D * .5f + .03f), .009f, 8);   // the telescopic handle
        c.m.Tube(BlackMatt, k.P(-.11f, .05f + H + .10f, -D * .5f + .03f), k.P(.11f, .05f + H + .10f, -D * .5f + .03f), .014f, 8);
    }
}
