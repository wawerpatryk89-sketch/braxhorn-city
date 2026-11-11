using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// BRAXHORN KATALOG WYPOSAŻENIA (owner 11.11 20:07-20:12: "ten sklep z meblami ... z tego sklepu będziemy mogli brać na
// mieszkania", "jak mamy sklep z telewizorami, z laptopami, to potem wszystko możemy wykorzystać w mieszkaniach ... w
// biurach, wszędzie", "żeby nie robić coś dwa razy", "telewizory ... mają mieć swoją markę", "każdy detal"). Every piece of
// equipment as one public function, built first in G1 (BRAXHORN HOME, the electronics shops) and then used by any
// generator (flats, hotel, offices; the other region from the branch, after the owner's word). Positions in nominal metres
// (the caller's batch builds them x K); p = the piece's base point (on the floor / the surface it stands on / the wall),
// f = the direction its front faces (horizontal). Brand: BRAXHORN (own brand, never a real one). Materials: Poly Haven
// photoscans for wood, stone, fabric; clean PBR for metal, glass, plastic, screens. Zero flicker: overlays >= 12 mm, the
// screens' pictures 14 mm in front of the panel behind them, bezels round (never over) the pictures.
public static class BraxKatalog
{
    public sealed class Ctx
    {
        public BraxChampionsMeshBatch m;      // the caller's batch (built x K by the caller)
        public Transform root, cols;          // root: letters / doors / water go under it; cols: colliders and seats
        public float unit = 1.2944f;          // x K
        public Material water;                // a transparent water material (taps)
        public Action<Vector3, Vector3, string> seat;   // a BraxSeat at a sitting point, facing
    }
    const string Folder = "Assets/PlayableGenerated/Mistrzowie/Katalog";

    // ---------------------------------------------------------------- materials
    public static Material BlackGloss, BlackMatt, Alu, AluDark, Chrome, Steel, Oak, Walnut, Stone, StoneDark, Glass, GlassDark,
        Led, LedRed, Rubber, Graphite, WhiteGloss, TileSplash, Fabric, Porcelain, Hob;
    static Material S(string id, Color c, float r, float met, Color? e = null, bool t = false)
    { var x = BraxChampionsHarbour.Surface("kat_" + id, c, r, met, e, t); BraxChampionsInteriorLight.Apply(x, .9f); return x; }
    static Material T(string id, string slug, float metres, Color tint)
    { var x = BraxChampionsFoundation.SourceMaterial("kat_" + id, slug, metres, tint); BraxChampionsInteriorLight.Apply(x, .85f); return x; }
    public static void Materials()
    {
        if (BlackGloss) return;
        BlackGloss = S("black_gloss", new Color(.025f, .025f, .03f), .10f, .2f);
        BlackMatt = S("black_matt", new Color(.06f, .06f, .065f), .55f, .3f);
        Alu = S("aluminium_brushed", new Color(.78f, .79f, .81f), .30f, .85f);
        AluDark = S("aluminium_space_grey", new Color(.34f, .35f, .37f), .32f, .85f);
        Chrome = S("chrome", new Color(.88f, .89f, .91f), .06f, 1f);
        Steel = S("stainless_brushed", new Color(.72f, .73f, .75f), .28f, .9f);
        Oak = T("oak_veneer", "teak_veneer", 1.2f, new Color(1.22f, 1.10f, .96f));
        Walnut = T("walnut_veneer", "smoked_walnut_veneer", 1.4f, new Color(1.05f, .98f, .92f));
        Stone = T("stone_top", "marble_01", 1.2f, new Color(1.26f, 1.25f, 1.24f));
        StoneDark = T("stone_top_dark", "granite_tile_03", 1.0f, new Color(.55f, .56f, .58f));
        Glass = S("glass_clear", new Color(.70f, .78f, .82f, .16f), .02f, .05f, null, true);
        GlassDark = S("glass_black", new Color(.03f, .03f, .035f), .05f, .1f);
        Led = S("led_white", new Color(1f, 1f, .98f), .3f, 0f, new Color(1.6f, 1.6f, 1.55f));
        LedRed = S("led_red", new Color(.9f, .05f, .06f), .3f, 0f, new Color(1.6f, .08f, .1f));
        Rubber = S("rubber_black", new Color(.04f, .04f, .045f), .8f, 0f);
        Graphite = S("graphite_lacquer", new Color(.16f, .165f, .175f), .45f, .2f);
        WhiteGloss = S("porcelain_white", new Color(.95f, .95f, .94f), .12f, 0f);
        Porcelain = WhiteGloss;
        TileSplash = T("splash_tiles_graphite", "long_white_tiles", .8f, new Color(.32f, .32f, .34f));
        Fabric = T("fabric_grey", "poly_wool_herringbone", .6f, new Color(.62f, .62f, .64f));
        Hob = S("hob_glass", new Color(.02f, .02f, .025f), .04f, .1f);
    }
    // a lit screen material from a picture (w x h pixels, row 0 at the bottom)
    static readonly Dictionary<string, Material> screens = new Dictionary<string, Material>();
    public static Material Screen(string id, int w, int h, Color32[] px)
    {
        if (screens.TryGetValue(id, out var have) && have) return have;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "kat " + id, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        tex.SetPixels32(px); tex.Apply(true);
        System.IO.Directory.CreateDirectory(Folder);
        string path = Folder + "/kat_" + id + ".asset";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(tex, path);
        var mt = BraxChampionsHarbour.Surface("kat_screen_" + id, Color.white, .18f, 0f, new Color(1.0f, 1.0f, 1.05f));
        mt.SetTexture("_MainTex", tex); mt.SetTexture("_EmissionMap", tex); EditorUtility.SetDirty(mt);
        screens[id] = mt; return mt;
    }

    /// <summary>the four TV pictures' materials (BraxChampionsReklamyTV plays the owner's ads into them)</summary>
    public static Material[] TvMaterials()
    {
        Materials(); var o = new Material[4];
        for (int i = 0; i < 4; i++) o[i] = Screen("tv_" + i, 320, 180, TvPicture(i));
        return o;
    }

    // ---------------------------------------------------------------- frame helpers
    struct Fr
    {
        public Vector3 p, f, r; public Quaternion rot;
        public Fr(Vector3 p, Vector3 f) { f.y = 0; f.Normalize(); this.p = p; this.f = f; r = Vector3.Cross(Vector3.up, f).normalized; rot = Quaternion.LookRotation(f, Vector3.up); }
        public Vector3 P(float a, float h, float d) => p + r * a + Vector3.up * h + f * d;   // a across (r), h up, d toward the front
    }
    // a rounded box in the piece's frame: x0..x1 across, y0..y1 up, d0..d1 front-back (d toward the front)
    static void RB(Ctx c, Material mat, Fr k, float x0, float x1, float y0, float y1, float d0, float d1, float r = -1f, int seg = 2)
    {
        float sx = x1 - x0, sy = y1 - y0, sz = d1 - d0; if (sx <= 0 || sy <= 0 || sz <= 0) return;
        float th = Mathf.Min(sx, Mathf.Min(sy, sz)); if (r < 0) r = Mathf.Clamp(th * .3f, .002f, .03f);
        c.m.RoundedBox(mat, k.P((x0 + x1) * .5f, (y0 + y1) * .5f, (d0 + d1) * .5f), new Vector3(sx, sy, sz), Mathf.Min(r, th * .49f), k.rot, r > .004f ? seg : 1);
    }
    static void Col(Ctx c, Fr k, float x0, float x1, float y0, float y1, float d0, float d1, string name)
    {
        var centre = k.P((x0 + x1) * .5f, (y0 + y1) * .5f, (d0 + d1) * .5f);
        BraxChampionsMeshBatch.Collider(c.cols, name, centre, new Vector3(x1 - x0, y1 - y0, d1 - d0), Mathf.Atan2(k.f.x, k.f.z) * Mathf.Rad2Deg);
    }
    // a picture facing the viewer at k.f: centre s, w x h (u to the viewer's right)
    static void Picture(Ctx c, Material mat, Fr k, Vector3 s, float w, float h)
    {
        var rv = -k.r; var up = Vector3.up;
        var a = s - rv * (w * .5f) - up * (h * .5f); var b = s + rv * (w * .5f) - up * (h * .5f); var cc = s + rv * (w * .5f) + up * (h * .5f); var d = s - rv * (w * .5f) + up * (h * .5f);
        c.m.QuadUV(mat, a, d, cc, b, new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));
    }
    // a bar handle (black matt or brushed): length L along `along`, 12 mm round bar on two posts 30 mm off the front
    static void BarHandle(Ctx c, Material mat, Vector3 mid, Vector3 along, Vector3 outward, float L)
    {
        along.Normalize(); outward.Normalize();
        var a = mid - along * (L * .5f) + outward * .030f; var b = mid + along * (L * .5f) + outward * .030f;
        c.m.Tube(mat, a, b, .006f, 10);
        foreach (var e in new[] { a + along * .02f, b - along * .02f }) c.m.Tube(mat, e - outward * .030f, e, .005f, 8);
    }
    static void Knob(Ctx c, Material mat, Vector3 at, Vector3 outward)
    {
        c.m.Cylinder(mat, at, .007f, .022f, 12, Quaternion.FromToRotation(Vector3.up, outward), false);
        c.m.Cylinder(mat, at + outward * .022f, .016f, .012f, 16, Quaternion.FromToRotation(Vector3.up, outward), true, true);
    }
    static void Brand(Ctx c, Material face, Material side, Vector3 baseline, Vector3 f, float cap, string label, string text = "BRAXHORN")
    {
        var lm = new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(lm, face, side, text, baseline, Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg, cap, Mathf.Max(.0015f, cap * .12f), .14f, 10f);
        BraxChampionsLetters3D.Build(lm, c.root, "BRAXHORN logo | " + label);
    }

    // ---------------------------------------------------------------- pictures
    static Color Add(Color a, Color b) => new Color(a.r + b.r, a.g + b.g, a.b + b.b, 1f);
    public static Color32[] TvPicture(int kind, int w = 320, int h = 180)
    {
        var px = new Color32[w * h];
        Color32 C(Color c) => new Color32((byte)Mathf.Clamp(c.r * 255, 0, 255), (byte)Mathf.Clamp(c.g * 255, 0, 255), (byte)Mathf.Clamp(c.b * 255, 0, 255), 255);
        void Set(int x, int y, Color c) { if (x >= 0 && y >= 0 && x < w && y < h) px[y * w + x] = C(c); }
        switch (kind % 4)
        {
            case 0:   // a mountain lake at sunset
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                    {
                        float t = y / (float)h; var sky = Color.Lerp(new Color(1f, .55f, .25f), new Color(.25f, .18f, .45f), Mathf.Pow(t, .8f));
                        float sun = Mathf.Exp(-((x - w * .62f) * (x - w * .62f) + (y - h * .52f) * (y - h * .52f)) / 180f);
                        float m1 = h * (.45f + .10f * Mathf.Sin(x * .021f) + .05f * Mathf.Sin(x * .063f + 1f)), m2 = h * (.38f + .06f * Mathf.Sin(x * .034f + 2f));
                        Color c = Add(sky, new Color(1f, .9f, .6f) * sun);
                        if (y < m1) c = Color.Lerp(new Color(.20f, .14f, .30f), new Color(.30f, .20f, .35f), y / m1);
                        if (y < m2) c = new Color(.10f, .08f, .16f);
                        if (y < h * .30f) { float ry = h * .60f - y; var refl = Color.Lerp(new Color(1f, .55f, .25f), new Color(.25f, .18f, .45f), Mathf.Clamp01(ry / h)) * .55f; c = Add(refl, new Color(.6f, .5f, .3f) * (Mathf.Exp(-((x - w * .62f) * (x - w * .62f)) / 60f) * (.5f + .5f * Mathf.Sin(y * 1.7f)))); }
                        Set(x, y, c);
                    }
                BraxChampionsPiksele.Tekst(px, w, h, "4K HDR", w - 46, 6, 1, new Color32(240, 244, 255, 255));
                break;
            case 1:   // football as broadcast (owner 20:36: not from a drone above): the main camera high on the side - the
                      // stands at the top, the pitch in perspective (mowing stripes converging), the lines, players, the score bug
                {
                    float hz = h * .70f, vx = w * .5f;
                    var rnd = new System.Random(7);
                    for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                        {
                            if (y >= hz)
                            {
                                // the stands: rows of spectators (coloured specks), the roof edge, the advertising boards
                                float ry = (y - hz) / (h - hz);
                                if (y < hz + 5) { Set(x, y, ((x / 24) % 2 == 0) ? new Color(.75f, .05f, .08f) : new Color(.95f, .95f, .95f)); continue; }
                                if (ry > .86f) { Set(x, y, new Color(.10f, .11f, .14f)); continue; }
                                var c = ((x + (y / 3) * 7) % 5 == 0) ? new Color(.85f, .80f, .70f) : new Color(.12f + .5f * (float)rnd.NextDouble(), .10f + .3f * (float)rnd.NextDouble(), .12f + .4f * (float)rnd.NextDouble());
                                if (y % 6 == 0) c = new Color(.20f, .20f, .24f);
                                Set(x, y, c); continue;
                            }
                            // the pitch: depth from the bottom (near) to the horizon (far); stripes along the pitch's width
                            float t = y / hz, depth = 1f / (1.05f - t), wx = (x - vx) / (w * .5f) * depth * 6f;
                            bool stripe = ((int)Mathf.Floor(wx + 50f)) % 2 == 0;
                            var g = stripe ? new Color(.17f, .50f, .19f) : new Color(.21f, .58f, .23f);
                            g = g * (.85f + .15f * t);
                            float half = Mathf.Abs(x - vx) / (w * .5f) * depth;
                            if (Mathf.Abs(x - vx) < 1.2f + (1f - t) * 1.0f) g = Color.white;                     // the halfway line
                            if (Mathf.Abs(y - hz * .08f) < 1.0f || Mathf.Abs(y - hz * .93f) < .7f) g = Color.white;   // the touchlines
                            float cy = hz * .45f, cr = 40f * (1f - t * .5f), ex = (x - vx) / cr, ey = (y - cy) / (cr * .38f);
                            if (Mathf.Abs(ex * ex + ey * ey - 1f) < .06f) g = Color.white;                      // the centre circle in perspective
                            Set(x, y, g);
                        }
                    // the players: shirts and shorts, smaller further away, a shadow under each; the ball
                    int[,] pl = { { 60, 30 }, { 95, 60 }, { 130, 22 }, { 150, 80 }, { 175, 45 }, { 200, 95 }, { 230, 35 }, { 260, 70 }, { 110, 100 }, { 185, 18 }, { 290, 50 }, { 40, 85 } };
                    for (int i = 0; i < pl.GetLength(0); i++)
                    {
                        int px0 = pl[i, 0], py0 = pl[i, 1]; float sc = 1.6f - py0 / hz;
                        int ph = Mathf.Max(5, (int)(11 * sc)), pw = Mathf.Max(2, (int)(4 * sc));
                        var shirt = i % 2 == 0 ? new Color(.85f, .08f, .10f) : new Color(.96f, .96f, .98f);
                        for (int dx = -pw; dx <= pw; dx++) Set(px0 + dx, py0 - 1, new Color(.08f, .25f, .10f));
                        for (int dy = 0; dy < ph; dy++) for (int dx = 0; dx < pw; dx++)
                                Set(px0 + dx - pw / 2, py0 + dy, dy < ph * .35f ? new Color(.10f, .10f, .12f) : dy < ph * .80f ? shirt : new Color(.85f, .65f, .50f));
                    }
                    for (int dy = 0; dy < 3; dy++) for (int dx = 0; dx < 3; dx++) Set(168 + dx, 52 + dy, Color.white);
                    BraxChampionsPiksele.Prost(px, w, h, 8, h - 22, 128, h - 8, new Color32(8, 8, 12, 255));
                    BraxChampionsPiksele.Prost(px, w, h, 8, h - 22, 12, h - 8, new Color32(200, 16, 24, 255));
                    BraxChampionsPiksele.Tekst(px, w, h, "BRX 2:1 MIS  67:12", 16, h - 19, 1, new Color32(240, 244, 255, 255));
                }
                break;
            case 2:   // the city at night
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) Set(x, y, Color.Lerp(new Color(.02f, .03f, .08f), new Color(.08f, .10f, .25f), y / (float)h));
                for (int b = 0; b < 18; b++)
                {
                    int x0 = b * 18 + (b * 7) % 9, bw = 12 + (b * 5) % 9, bh = 40 + (b * 37) % 90;
                    for (int y = 0; y < bh; y++) for (int x = x0; x < x0 + bw; x++)
                        {
                            bool win = (x - x0) % 4 == 1 && y % 6 == 2 && ((x * 13 + y * 7 + b) % 5) != 0;
                            Set(x, y, win ? new Color(1f, .85f, .5f) : new Color(.05f, .06f, .10f));
                        }
                }
                break;
            default:  // the brand splash
                BraxChampionsPiksele.Wypelnij(px, new Color32(6, 6, 9, 255));
                BraxChampionsPiksele.Prost(px, w, h, 0, h / 2 - 34, w, h / 2 - 30, new Color32(200, 16, 24, 255));
                BraxChampionsPiksele.Srodek(px, w, h, "BRAXHORN", w / 2, h / 2 - 8, 4, new Color32(240, 244, 255, 255));
                BraxChampionsPiksele.Srodek(px, w, h, "OLED 4K · 120 HZ", w / 2, h / 2 - 56, 1, new Color32(170, 175, 190, 255));
                break;
        }
        return px;
    }
    // a computer's desktop: a wallpaper, two windows (a document, a picture), the task bar with icons
    public static Color32[] Desktop(int kind, int w = 256, int h = 160)
    {
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float t = y / (float)h, s = x / (float)w;
                var c = kind % 2 == 0 ? Color.Lerp(new Color(.05f, .12f, .35f), new Color(.30f, .55f, .85f), t * .7f + s * .3f) : Color.Lerp(new Color(.30f, .05f, .10f), new Color(.85f, .35f, .25f), t * .6f + s * .4f);
                px[y * w + x] = new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), 255);
            }
        void Win(int x0, int y0, int x1, int y1, Color32 body)
        {
            BraxChampionsPiksele.Prost(px, w, h, x0, y0, x1, y1, body);
            BraxChampionsPiksele.Prost(px, w, h, x0, y1 - 9, x1, y1, new Color32(30, 34, 48, 255));
            BraxChampionsPiksele.Prost(px, w, h, x1 - 8, y1 - 7, x1 - 3, y1 - 2, new Color32(220, 60, 60, 255));
        }
        Win(18, 40, 140, 130, new Color32(245, 246, 250, 255));
        for (int i = 0; i < 7; i++) BraxChampionsPiksele.Prost(px, w, h, 26, 108 - i * 9, 26 + 60 + (i * 23) % 44, 111 - i * 9, new Color32(120, 126, 140, 255));
        Win(120, 60, 236, 140, new Color32(20, 22, 30, 255));
        BraxChampionsPiksele.Prost(px, w, h, 126, 66, 230, 120, new Color32(60, 150, 90, 255));
        BraxChampionsPiksele.Prost(px, w, h, 126, 66, 230, 86, new Color32(40, 90, 160, 255));
        BraxChampionsPiksele.Prost(px, w, h, 0, 0, w, 14, new Color32(16, 18, 26, 255));
        for (int i = 0; i < 7; i++) BraxChampionsPiksele.Prost(px, w, h, 90 + i * 12, 3, 98 + i * 12, 11, i == 0 ? new Color32(200, 16, 24, 255) : new Color32((byte)(90 + i * 20), 140, (byte)(220 - i * 15), 255));
        BraxChampionsPiksele.Tekst(px, w, h, "12:00", w - 34, 4, 1, new Color32(240, 244, 255, 255));
        BraxChampionsPiksele.Tekst(px, w, h, "BRAXHORN", 26, 120, 1, new Color32(30, 34, 48, 255));
        return px;
    }

    // ---------------------------------------------------------------- TELEWIZOR BRAXHORN (43 / 55 / 65 / 75 / 85 cali)
    // An OLED panel 6 mm at the edges: the picture, a 5 mm black bezel round it (never over it), the panel's back 14 mm
    // behind the picture, the electronics' box low on the back with the ports, a brushed chin with the 3D BRAXHORN logo, the
    // red standby LED; on a brushed pedestal (onStand) or 45 mm off the wall on a black bracket. p = the stand's foot point
    // on the surface (onStand) or the wall point under the screen's centre at its centre height (wall).
    public static void Tv(Ctx c, Vector3 p, Vector3 f, float inches, int picture, bool onStand = true, string label = "telewizor")
    {
        Materials();
        float W = inches * .0254f * .8716f, H = inches * .0254f * .4903f;
        var k = new Fr(p, f);
        float yc = onStand ? .10f + H * .5f + .03f : 0f, d = onStand ? 0f : .045f + .03f;     // the picture plane's offset
        var s = k.P(0f, yc, d);
        Picture(c, Screen("tv_" + picture, 320, 180, TvPicture(picture)), k, s, W, H);
        // the bezel (5 mm, 2 mm proud of the picture), the panel's back plate, the electronics box, the chin with the logo
        RB(c, BlackGloss, k, -W * .5f - .005f, W * .5f + .005f, yc + H * .5f, yc + H * .5f + .005f, d - .014f, d + .002f, .0015f, 1);
        RB(c, BlackGloss, k, -W * .5f - .005f, W * .5f + .005f, yc - H * .5f - .005f, yc - H * .5f, d - .014f, d + .002f, .0015f, 1);
        RB(c, BlackGloss, k, -W * .5f - .005f, -W * .5f, yc - H * .5f, yc + H * .5f, d - .014f, d + .002f, .0015f, 1);
        RB(c, BlackGloss, k, W * .5f, W * .5f + .005f, yc - H * .5f, yc + H * .5f, d - .014f, d + .002f, .0015f, 1);
        RB(c, BlackMatt, k, -W * .5f, W * .5f, yc - H * .5f, yc + H * .5f, d - .020f, d - .014f, .002f, 1);
        RB(c, AluDark, k, -W * .30f, W * .30f, yc - H * .45f, yc + H * .10f, d - .060f, d - .020f, .012f, 2);
        for (int i = 0; i < 4; i++) RB(c, BlackMatt, k, -W * .25f + i * .03f, -W * .25f + i * .03f + .018f, yc - H * .40f, yc - H * .40f + .008f, d - .064f, d - .058f, .001f, 1);   // HDMI ports
        RB(c, Alu, k, -W * .5f - .005f, W * .5f + .005f, yc - H * .5f - .022f, yc - H * .5f - .005f, d - .016f, d + .002f, .003f, 1);
        Brand(c, Chrome, AluDark, k.P(0f, yc - H * .5f - .019f, d + .0035f), k.f, .010f, label);
        c.m.Cylinder(LedRed, k.P(W * .40f, yc - H * .5f - .023f, d - .004f), .0025f, .003f, 8, Quaternion.Euler(180, 0, 0), true);
        if (onStand)
        {
            RB(c, Alu, k, -.20f, .20f, 0f, .010f, -.15f, .09f, .006f, 2);                                   // the foot plate
            RB(c, Alu, k, -.035f, .035f, .010f, yc - H * .25f, -.060f, -.040f, .006f, 1);                     // the neck to the box
            RB(c, Rubber, k, -.19f, .19f, -.002f, .0005f, -.14f, .08f, .001f, 1);
            Col(c, k, -W * .5f, W * .5f, 0f, yc + H * .5f, -.15f, .09f, label);
        }
        else
        {
            RB(c, BlackMatt, k, -.20f, .20f, -.15f, .15f, 0f, .045f, .004f, 1);                             // the wall bracket
            Col(c, k, -W * .5f, W * .5f, -H * .5f, H * .5f, 0f, .08f, label);
        }
    }

    // ---------------------------------------------------------------- LAPTOP BRAXHORN (14")
    // An aluminium unibody 32.4 x 22.4 x 1.5 cm: the keyboard well (black) with 78 keys in six rows, the glass touchpad, the
    // speaker grilles, the hinge; the lid open at `open` degrees: the black glass bezel with the camera, the desktop, the
    // BRAXHORN logo on its back. p = the centre of its footprint on the desk, f = toward the user.
    public static void Laptop(Ctx c, Vector3 p, Vector3 f, int desktop, float open = 112f, Material body = null)
    {
        Materials(); body = body ? body : Alu;
        var k = new Fr(p, f);
        const float W = .324f, D = .224f, Hb = .015f;
        float d0 = -D * .5f, d1 = D * .5f;
        RB(c, body, k, -W * .5f, W * .5f, 0f, .009f, d0, d1, .006f, 2);                 // the base's lower shell
        // the top deck, tiled round the keyboard well (d -.09..+.0) and the touchpad (d .035..+.098): no faces stacked
        float kw0 = -.135f, kw1 = .135f, kd0 = -.100f, kd1 = .005f, tp0 = -.060f, tp1 = .060f, td0 = .020f, td1 = .096f;
        RB(c, body, k, -W * .5f, kw0, .009f, Hb, d0 + .004f, d1 - .004f, .002f, 1); RB(c, body, k, kw1, W * .5f, .009f, Hb, d0 + .004f, d1 - .004f, .002f, 1);
        RB(c, body, k, kw0, kw1, .009f, Hb, d0 + .004f, kd0, .002f, 1);
        RB(c, body, k, kw0, tp0, .009f, Hb, kd1, d1 - .004f, .002f, 1); RB(c, body, k, tp1, kw1, .009f, Hb, kd1, d1 - .004f, .002f, 1);
        RB(c, body, k, tp0, tp1, .009f, Hb, kd1, td0, .002f, 1); RB(c, body, k, tp0, tp1, .009f, Hb, td1, d1 - .004f, .002f, 1);
        RB(c, GlassDark, k, tp0, tp1, .009f, Hb - .0003f, td0, td1, .002f, 1);          // the glass touchpad
        RB(c, BlackMatt, k, kw0, kw1, .009f, .0125f, kd0, kd1, .001f, 1);               // the keyboard well's floor
        int[] cols = { 14, 14, 14, 13, 12, 9 };
        for (int row = 0; row < 6; row++)
        {
            int n = cols[row]; float pitch = (kw1 - kw0 - .006f) / 14f, dd = kd0 + .006f + row * ((kd1 - kd0 - .012f) / 6f), kh = (kd1 - kd0 - .012f) / 6f - .002f;
            float x = kw0 + .004f + (14 - n) * pitch * .5f;
            for (int i = 0; i < n; i++)
            {
                float kwid = row == 5 && i == 4 ? pitch * 5f : pitch - .002f;            // the space bar
                if (row == 5 && i > 4) x += pitch * 4f * 0f;
                RB(c, BlackMatt, k, x, x + kwid, .0125f, .0158f, dd, dd + kh, .0012f, 1);
                x += row == 5 && i == 4 ? pitch * 5f : pitch;
            }
        }
        for (int s2 = -1; s2 <= 1; s2 += 2) for (int i = 0; i < 6; i++) RB(c, BlackMatt, k, s2 * .150f - .004f, s2 * .150f + .004f, Hb - .0002f, Hb + .0006f, -.080f + i * .015f, -.074f + i * .015f, .0005f, 1);   // grilles
        c.m.Cylinder(AluDark, k.P(-W * .40f, .010f, d0 + .004f), .005f, W * .80f, 12, Quaternion.FromToRotation(Vector3.up, k.r), true, true);   // the hinge
        // the lid: rotated about the hinge line by `open`
        float a = (open - 90f) * Mathf.Deg2Rad; var up = Vector3.up * Mathf.Cos(a) - k.f * Mathf.Sin(a); var fw = Vector3.Cross(k.r, up).normalized;   // fw: the screen's face
        if (Vector3.Dot(fw, k.f) < 0) fw = -fw;
        var hinge = k.P(0f, .012f, d0 + .002f);
        const float LH = .214f, LT = .006f;
        var lc = hinge + up * (LH * .5f) - fw * (LT * .5f + .0006f);
        c.m.RoundedBox(body, lc, new Vector3(W, LH, LT - .0012f), .004f, Quaternion.LookRotation(fw, up), 2);
        var sc = hinge + up * (LH * .5f + .004f) + fw * .0040f;
        var lrot = Quaternion.LookRotation(fw, up);
        c.m.RoundedBox(GlassDark, hinge + up * (LH * .5f) + fw * .0002f, new Vector3(W - .006f, LH - .006f, .0016f), .002f, lrot, 1);   // the black glass
        var rv = -k.r; float sw = W - .024f, sh = LH - .034f; var scr = Screen("pulpit_" + desktop, 256, 160, Desktop(desktop));
        var q0 = sc - rv * (sw * .5f) - up * (sh * .5f); var q1 = sc + rv * (sw * .5f) - up * (sh * .5f); var q2 = sc + rv * (sw * .5f) + up * (sh * .5f); var q3 = sc - rv * (sw * .5f) + up * (sh * .5f);
        c.m.QuadUV(scr, q0, q3, q2, q1, new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));
        c.m.Cylinder(BlackMatt, hinge + up * (LH - .007f) + fw * .0012f, .0018f, .0012f, 8, Quaternion.FromToRotation(Vector3.up, fw), true);   // the camera
        var back = hinge + up * (LH * .55f) - fw * (LT + .0005f);
        var lm = new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(lm, Chrome, AluDark, "BRAXHORN", back, Mathf.Atan2(-fw.x, -fw.z) * Mathf.Rad2Deg, .014f, .0012f, .16f, .2f);
        BraxChampionsLetters3D.Build(lm, c.root, "BRAXHORN logo | laptop");
        Col(c, k, -W * .5f, W * .5f, 0f, .02f, d0, d1, "laptop");
    }

    // ---------------------------------------------------------------- MONITOR BRAXHORN 27"
    public static void Monitor(Ctx c, Vector3 p, Vector3 f, int desktop, float inches = 27f)
    {
        Materials();
        float W = inches * .0254f * .8716f, H = inches * .0254f * .4903f; var k = new Fr(p, f);
        float yc = .12f + H * .5f; float d = .02f;
        Picture(c, Screen("pulpit_" + desktop, 256, 160, Desktop(desktop)), k, k.P(0f, yc, d), W, H);
        RB(c, BlackMatt, k, -W * .5f - .006f, W * .5f + .006f, yc + H * .5f, yc + H * .5f + .006f, d - .014f, d + .002f, .0015f, 1);
        RB(c, BlackMatt, k, -W * .5f - .006f, W * .5f + .006f, yc - H * .5f - .018f, yc - H * .5f, d - .014f, d + .002f, .0015f, 1);
        RB(c, BlackMatt, k, -W * .5f - .006f, -W * .5f, yc - H * .5f, yc + H * .5f, d - .014f, d + .002f, .0015f, 1);
        RB(c, BlackMatt, k, W * .5f, W * .5f + .006f, yc - H * .5f, yc + H * .5f, d - .014f, d + .002f, .0015f, 1);
        RB(c, Alu, k, -W * .5f, W * .5f, yc - H * .5f, yc + H * .5f, d - .030f, d - .014f, .006f, 2);
        Brand(c, Chrome, AluDark, k.P(0f, yc - H * .5f - .013f, d + .0035f), k.f, .007f, "monitor");
        RB(c, Alu, k, -.11f, .11f, 0f, .008f, -.12f, .06f, .006f, 2);
        RB(c, Alu, k, -.03f, .03f, .008f, yc, -.075f, -.050f, .006f, 1);
        Col(c, k, -W * .5f, W * .5f, 0f, yc + H * .5f, -.12f, .06f, "monitor");
    }
    // a keyboard and a mouse in front of a monitor
    public static void KeyboardMouse(Ctx c, Vector3 p, Vector3 f)
    {
        Materials(); var k = new Fr(p, f);
        RB(c, AluDark, k, -.21f, .21f, 0f, .012f, -.06f, .06f, .005f, 2);
        for (int row = 0; row < 5; row++) for (int i = 0; i < 15; i++) RB(c, BlackMatt, k, -.20f + i * .0265f, -.20f + i * .0265f + .023f, .012f, .016f, -.052f + row * .021f, -.052f + row * .021f + .018f, .001f, 1);
        c.m.RoundedBox(BlackMatt, k.P(.30f, .016f, .0f), new Vector3(.062f, .032f, .11f), .028f, k.rot, 3);
    }

    // ---------------------------------------------------------------- KUCHNIA (a straight run with wall units)
    // modules (60 cm each unless noted): "szuflady" (3 drawers), "zlew" (an undermount steel sink, a gooseneck tap that runs,
    // the cabinet's doors), "zmywarka" (integrated), "plyta" (an induction hob in black glass over drawers), "piekarnik" (a
    // built-in oven in black glass under the hob), "lodowka" (a tall fridge-freezer column), "slupek" (a tall larder),
    // "drzwi"; the plinth recessed, a 4 cm stone top, a tiled splashback on the wall, wall units with handles and LED under
    // them, a chimney hood over the hob. p = the run's start on the floor at the wall, along = the run's direction, f = out.
    public static void Kitchen(Ctx c, Vector3 p, Vector3 along, Vector3 f, string[] modules, Material front, Material handle = null, Material top = null, string label = "kuchnia", bool wallUnits = true)
    {
        Materials(); handle = handle ? handle : BlackMatt; top = top ? top : Stone;
        along.y = 0; along.Normalize(); f.y = 0; f.Normalize();
        const float MW = .60f, Dp = .58f, Hp = .10f, Hc = .87f, Tt = .04f, gap = .003f;
        float x = 0f, hobX = -1f;
        Vector3 Q(float u, float h, float dd) => p + along * u + Vector3.up * h + f * dd;
        var rot = Quaternion.LookRotation(f, Vector3.up);
        void Box(Material mat, float u0, float u1, float y0, float y1, float d0, float d1, float r = .003f)
        {
            float sx = u1 - u0, sy = y1 - y0, sz = d1 - d0; if (sx <= 0 || sy <= 0 || sz <= 0) return;
            c.m.RoundedBox(mat, Q((u0 + u1) * .5f, (y0 + y1) * .5f, (d0 + d1) * .5f), new Vector3(sx, sy, sz), Mathf.Min(r, Mathf.Min(sx, Mathf.Min(sy, sz)) * .45f), rot, 1);
        }
        void Front(float u0, float u1, float y0, float y1, bool drawer, bool handleTop = true)
        {
            Box(front, u0 + gap, u1 - gap, y0 + gap, y1 - gap, Dp, Dp + .019f, .003f);
            float hy = drawer ? y1 - .045f : (handleTop ? y1 - .09f : y0 + .09f);
            if (drawer) BarHandle(c, handle, Q((u0 + u1) * .5f, hy, Dp + .019f), along, f, Mathf.Min(.40f, (u1 - u0) * .6f));
            else BarHandle(c, handle, Q(u1 - .05f, (y0 + y1) * .5f + (handleTop ? .15f : 0f), Dp + .019f), Vector3.up, f, .20f);
        }
        foreach (var mod in modules)
        {
            float u0 = x, u1 = x + MW; bool tall = mod == "lodowka" || mod == "slupek";
            Box(BlackMatt, u0 + .002f, u1 - .002f, 0f, Hp, .05f, Dp - .04f, .002f);                 // the recessed plinth
            Box(tall ? front : Graphite, u0, u1, Hp, tall ? 2.20f : Hc, 0f, Dp, .002f);              // the carcass
            switch (mod)
            {
                case "szuflady": Front(u0, u1, Hp, Hp + .26f, true); Front(u0, u1, Hp + .26f, Hp + .52f, true); Front(u0, u1, Hp + .52f, Hc, true); break;
                case "plyta": Front(u0, u1, Hp, Hp + .38f, true); Front(u0, u1, Hp + .38f, Hc, true); hobX = (u0 + u1) * .5f; break;
                case "piekarnik":
                    Front(u0, u1, Hp, Hp + .14f, true);
                    Box(Hob, u0 + gap, u1 - gap, Hp + .14f, Hc - .10f, Dp, Dp + .022f, .004f);       // the oven's black glass door
                    BarHandle(c, Steel, Q((u0 + u1) * .5f, Hc - .14f, Dp + .022f), along, f, .46f);
                    Box(Steel, u0 + gap, u1 - gap, Hc - .10f, Hc - gap, Dp, Dp + .019f, .002f);       // the control strip
                    for (int i = 0; i < 2; i++) c.m.Cylinder(BlackMatt, Q(u0 + .10f + i * .40f, Hc - .05f, Dp + .019f), .016f, .014f, 16, Quaternion.FromToRotation(Vector3.up, f), true);
                    Box(GlassDark, (u0 + u1) * .5f - .06f, (u0 + u1) * .5f + .06f, Hc - .075f, Hc - .03f, Dp + .019f, Dp + .021f, .001f);
                    hobX = (u0 + u1) * .5f; break;
                case "zlew": Front(u0, (u0 + u1) * .5f, Hp, Hc, false); Front((u0 + u1) * .5f, u1, Hp, Hc, false); break;
                case "zmywarka": Front(u0, u1, Hp, Hc, true); break;
                case "lodowka":
                    Front(u0, u1, Hp, 1.00f, false, true); Front(u0, u1, 1.00f, 2.20f, false, false);
                    Box(front, u0, u1, 2.20f, 2.20f + .02f, 0f, Dp + .019f, .002f); break;
                case "slupek": Front(u0, u1, Hp, 1.40f, false, true); Front(u0, u1, 1.40f, 2.20f, false, false); break;
                default: Front(u0, u1, Hp, Hc, false); break;
            }
            if (!tall)
            {
                // the worktop: the sink cut-out tiled round the bowl, the hob set flush into it
                if (mod == "zlew")
                {
                    float bu0 = u0 + .08f, bu1 = u1 - .08f, bd0 = .10f, bd1 = .52f;
                    Box(top, u0, bu0, Hc, Hc + Tt, 0f, Dp + .03f); Box(top, bu1, u1, Hc, Hc + Tt, 0f, Dp + .03f);
                    Box(top, bu0, bu1, Hc, Hc + Tt, 0f, bd0); Box(top, bu0, bu1, Hc, Hc + Tt, bd1, Dp + .03f);
                    Box(Steel, bu0, bu1, Hc - .20f, Hc - .19f, bd0, bd1, .002f);                                // the bowl's bottom
                    Box(Steel, bu0, bu0 + .008f, Hc - .19f, Hc, bd0, bd1, .001f); Box(Steel, bu1 - .008f, bu1, Hc - .19f, Hc, bd0, bd1, .001f);
                    Box(Steel, bu0 + .008f, bu1 - .008f, Hc - .19f, Hc, bd0, bd0 + .008f, .001f); Box(Steel, bu0 + .008f, bu1 - .008f, Hc - .19f, Hc, bd1 - .008f, bd1, .001f);
                    c.m.Cylinder(Chrome, Q((bu0 + bu1) * .5f, Hc - .19f, (bd0 + bd1) * .5f), .035f, .008f, 18, Quaternion.identity, true);
                    // the gooseneck tap: column, the arc, the head over the bowl; the water runs while you stand at it
                    var tb = Q((u0 + u1) * .5f, Hc + Tt, .055f);
                    c.m.Cylinder(Chrome, tb, .028f, .012f, 16, Quaternion.identity, true);
                    c.m.Cylinder(Chrome, tb, .016f, .30f, 14, Quaternion.identity, false);
                    Vector3 prev = tb + Vector3.up * .30f;
                    for (int i = 1; i <= 12; i++) { float t = i / 12f * Mathf.PI; var q = tb + Vector3.up * (.30f + .10f * Mathf.Sin(t)) + f * (.11f * (1f - Mathf.Cos(t))); c.m.Tube(Chrome, prev, q, .014f, 12); prev = q; }
                    c.m.Cylinder(Chrome, prev - Vector3.up * .04f, .017f, .04f, 14, Quaternion.identity, true, true);
                    c.m.Tube(Chrome, tb + Vector3.up * .22f + along * .016f, tb + Vector3.up * .26f + along * .09f, .008f, 8);   // the lever
                    if (c.water)
                    {
                        var outp = prev - Vector3.up * .042f;
                        BraxChampionsMeshBatch.FountainWater(c.root, label + " | kran — woda", outp, c.unit, new[] { Vector3.zero }, new[] { new Vector3(.002f, (Hc - .19f) - outp.y + .01f, 0f) },
                            new[] { .004f }, new[] { .018f }, new[] { 12 }, new[] { true }, new[] { new Vector3(0f, (Hc - .185f) - outp.y, 0f) }, new[] { .12f }, c.water, 1.6f);
                    }
                }
                else if (mod == "plyta" || mod == "piekarnik")
                {
                    float hu0 = u0 + .03f, hu1 = u1 - .03f, hd0 = .04f, hd1 = .55f;
                    Box(top, u0, hu0, Hc, Hc + Tt, 0f, Dp + .03f); Box(top, hu1, u1, Hc, Hc + Tt, 0f, Dp + .03f);
                    Box(top, hu0, hu1, Hc, Hc + Tt, 0f, hd0); Box(top, hu0, hu1, Hc, Hc + Tt, hd1, Dp + .03f);
                    Box(Hob, hu0, hu1, Hc + Tt - .006f, Hc + Tt, hd0, hd1, .002f);
                    foreach (var (du, dd, r) in new[] { (-.13f, .16f, .09f), (.13f, .16f, .075f), (-.13f, .40f, .075f), (.13f, .40f, .10f) })
                    {
                        var cz = Q((hu0 + hu1) * .5f + du, Hc + Tt + .0008f, dd);
                        for (int i = 0; i < 24; i++) { float a0 = i * Mathf.PI / 12f, a1 = (i + 1) * Mathf.PI / 12f; c.m.Tube(Graphite, cz + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r, cz + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r, .0012f, 4); }
                    }
                    for (int i = 0; i < 5; i++) c.m.Cylinder(Led, Q((hu0 + hu1) * .5f - .08f + i * .04f, Hc + Tt, .51f), .006f, .0012f, 10, Quaternion.identity, true);
                }
                else Box(top, u0, u1, Hc, Hc + Tt, 0f, Dp + .03f);
            }
            x = u1;
        }
        float L = x;
        // the splashback (graphite tiles, 14 mm on the wall) and the wall units (35 deep, 1.45..2.17) with LED under them
        x = 0f;
        if (wallUnits) foreach (var mod in modules)
        {
            float u0 = x, u1 = x + MW; x = u1;
            if (mod == "lodowka" || mod == "slupek") continue;
            Box(TileSplash, u0, u1, Hc + Tt, 1.45f, 0f, .014f, .001f);
            if (Mathf.Abs((u0 + u1) * .5f - hobX) < .01f)
            {
                // the chimney hood: a stainless canopy and its flue to the ceiling line
                Box(Steel, u0 + .02f, u1 - .02f, 1.60f, 1.66f, .014f, .50f, .006f);
                Box(Steel, (u0 + u1) * .5f - .13f, (u0 + u1) * .5f + .13f, 1.66f, 2.40f, .014f, .27f, .004f);
                Box(GlassDark, u0 + .10f, u1 - .10f, 1.598f, 1.60f, .10f, .45f, .001f);
                continue;
            }
            Box(Graphite, u0, u1, 1.45f, 2.17f, .014f, .35f, .002f);
            Box(front, u0 + gap, u1 - gap, 1.45f + gap, 2.17f - gap, .35f, .369f, .003f);
            BarHandle(c, handle, Q(u1 - .05f, 1.55f, .369f), Vector3.up, f, .16f);
            Box(Led, u0 + .03f, u1 - .03f, 1.438f, 1.45f, .26f, .32f, .001f);
        }
        var cc = Q(L * .5f, Hc * .5f, Dp * .5f);
        BraxChampionsMeshBatch.Collider(c.cols, label, cc, new Vector3(L, Hc + Tt, Dp + .05f), Mathf.Atan2(along.x, along.z) * Mathf.Rad2Deg + 90f);
    }

    // ---------------------------------------------------------------- SZAFA (a wardrobe with hinged doors that open)
    // A carcass on a recessed plinth, a crown, doors 19 mm with 3 mm gaps, long black bar handles at the meeting edges; inside
    // a chrome hanging rail, a shelf, the back in a lighter veneer; each door a BraxCityDoor leaf (opens on approach).
    public static void Wardrobe(Ctx c, Vector3 p, Vector3 f, float width, Material front, Material carcass, float height = 2.20f, string label = "szafa")
    {
        Materials(); var k = new Fr(p, f); const float D = .60f;
        int n = Mathf.Max(1, Mathf.RoundToInt(width / .5f)); float dw = width / n;
        RB(c, BlackMatt, k, -width * .5f + .03f, width * .5f - .03f, 0f, .08f, -D + .03f, -.05f, .002f, 1);
        RB(c, carcass, k, -width * .5f, width * .5f, .08f, height, -D, -.022f, .003f, 1);
        RB(c, carcass, k, -width * .5f - .01f, width * .5f + .01f, height, height + .03f, -D - .01f, .0f, .004f, 1);
        // the inside (seen through the open doors): the rail, a shelf, the lighter back
        RB(c, Oak, k, -width * .5f + .02f, width * .5f - .02f, .10f, height - .02f, -D + .002f, -D + .02f, .002f, 1);
        RB(c, Oak, k, -width * .5f + .02f, width * .5f - .02f, height - .34f, height - .32f, -D + .02f, -.03f, .002f, 1);
        c.m.Tube(Chrome, k.P(-width * .5f + .03f, height - .42f, -D * .5f), k.P(width * .5f - .03f, height - .42f, -D * .5f), .012f, 10);
        var g = new GameObject(label + " | drzwi (otwierają się)"); g.transform.SetParent(c.root, false);
        g.transform.localPosition = k.P(0f, .08f, -.002f) * c.unit; g.transform.localRotation = k.rot;
        var leaves = new List<Transform>(); var sides = new List<float>();
        for (int i = 0; i < n; i++)
        {
            bool hingeLeft = i % 2 == 0; float a0 = -width * .5f + i * dw, a1 = a0 + dw, hx = hingeLeft ? a0 : a1;
            var h = new GameObject("skrzydło " + (i + 1)).transform; h.SetParent(g.transform, false); h.localPosition = new Vector3(hx, 0, 0) * c.unit;
            var lm = new BraxChampionsMeshBatch(); float w = dw - .003f, s = hingeLeft ? 1f : -1f;
            lm.RoundedBox(front, new Vector3(s * (w * .5f + .0015f), (height - .08f - .006f) * .5f, .0095f), new Vector3(w, height - .08f - .006f, .019f), .003f, Quaternion.identity, 1);
            float hxl = s * (w - .045f);
            var hb0 = new Vector3(hxl, (height - .08f) * .5f - .45f, .049f); var hb1 = new Vector3(hxl, (height - .08f) * .5f + .45f, .049f);
            lm.Tube(BlackMatt, hb0, hb1, .007f, 10); lm.Tube(BlackMatt, hb0 + Vector3.up * .02f - Vector3.forward * .03f, hb0 + Vector3.up * .02f, .005f, 8); lm.Tube(BlackMatt, hb1 - Vector3.up * .02f - Vector3.forward * .03f, hb1 - Vector3.up * .02f, .005f, 8);
            lm.Build(h, label + " skrzydło " + (i + 1), Folder, false, c.unit);
            BraxChampionsMeshBatch.Collider(h, "skrzydło", new Vector3(s * w * .5f, (height - .08f) * .5f, .01f), new Vector3(w, height - .08f, .02f));
            foreach (var t in h.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            leaves.Add(h); sides.Add(hingeLeft ? 1f : -1f);
        }
        var d = g.AddComponent<BraxCityDoor>(); d.id = label; d.label = label; d.leaves = leaves.ToArray(); d.sides = sides.ToArray();
        Col(c, k, -width * .5f, width * .5f, 0f, height + .03f, -D, -.36f, label);
    }

    // ---------------------------------------------------------------- KOMODA (a chest of drawers)
    // rows x columns of drawers 19 mm with 3 mm gaps, round knobs (black / brushed) or bar handles, a 25 mm top, tapered legs
    // (or a plinth); every drawer a separate front.
    public static void Dresser(Ctx c, Vector3 p, Vector3 f, float width, int rows, int columns, Material body, Material knob = null, bool legs = true, float height = .82f, bool bars = false, string label = "komoda")
    {
        Materials(); knob = knob ? knob : BlackMatt; var k = new Fr(p, f); const float D = .45f;
        float y0 = legs ? .16f : .06f, y1 = height - .025f;
        if (legs) foreach (float a in new[] { -width * .5f + .05f, width * .5f - .05f }) foreach (float dd in new[] { -D + .05f, -.05f })
                    c.m.Cylinder(Walnut, k.P(a, 0f, dd), .016f, y0, 12, Quaternion.identity, true, true, .024f);
        else RB(c, BlackMatt, k, -width * .5f + .03f, width * .5f - .03f, 0f, y0, -D + .03f, -.05f, .002f, 1);
        RB(c, body, k, -width * .5f, width * .5f, y0, y1, -D, -.022f, .003f, 1);
        RB(c, body, k, -width * .5f - .008f, width * .5f + .008f, y1, height, -D - .008f, .006f, .006f, 2);
        float cw = width / columns, rh = (y1 - y0) / rows;
        for (int i = 0; i < columns; i++) for (int j = 0; j < rows; j++)
            {
                float a0 = -width * .5f + i * cw, b0 = y0 + j * rh;
                RB(c, body, k, a0 + .0015f, a0 + cw - .0015f, b0 + .0015f, b0 + rh - .0015f, -.022f, -.003f, .003f, 1);
                var mid = k.P(a0 + cw * .5f, b0 + rh * .5f, -.003f);
                if (bars) BarHandle(c, knob, mid, k.r, k.f, Mathf.Min(.30f, cw * .5f)); else Knob(c, knob, mid, k.f);
            }
        Col(c, k, -width * .5f, width * .5f, 0f, height, -D, 0f, label);
    }

    // ---------------------------------------------------------------- SZAFKA RTV (a low media unit) + the TV and a soundbar
    public static void MediaUnit(Ctx c, Vector3 p, Vector3 f, float width, Material body, float tvInches, int picture, string label = "szafka RTV")
    {
        Materials(); var k = new Fr(p, f); const float D = .42f, H = .50f;
        foreach (float a in new[] { -width * .5f + .06f, width * .5f - .06f }) foreach (float dd in new[] { -D + .05f, -.05f }) c.m.Cylinder(BlackMatt, k.P(a, 0f, dd), .012f, .12f, 10, Quaternion.identity, true, true);
        RB(c, body, k, -width * .5f, width * .5f, .12f, H, -D, -.02f, .004f, 1);
        // fluted doors left and right, an open middle bay with the soundbar's sub and a console, drawers under it
        float bay = width * .32f;
        foreach (float s in new[] { -1f, 1f })
        {
            float a0 = s < 0 ? -width * .5f : bay * .5f, a1 = s < 0 ? -bay * .5f : width * .5f;
            for (float x = a0 + .003f; x < a1 - .02f; x += .032f) RB(c, body, k, x, Mathf.Min(x + .029f, a1 - .003f), .123f, H - .003f, -.02f, .0f, .012f, 2);
            Knob(c, BlackMatt, k.P(s < 0 ? a1 - .04f : a0 + .04f, (H + .12f) * .5f, .0f), k.f);
        }
        RB(c, Graphite, k, -bay * .5f, bay * .5f, .26f, H - .02f, -D + .02f, -.04f, .002f, 1);
        RB(c, body, k, -bay * .5f + .002f, bay * .5f - .002f, .122f, .26f, -.022f, -.003f, .003f, 1);
        BarHandle(c, BlackMatt, k.P(0f, .20f, -.003f), k.r, k.f, .16f);
        RB(c, BlackMatt, k, -.18f, .18f, .27f, .33f, -.30f, -.08f, .01f, 2);   // a games console
        c.m.Cylinder(LedRed, k.P(.15f, .30f, -.079f), .002f, .002f, 6, Quaternion.FromToRotation(Vector3.up, k.f), true);
        // the soundbar on the top, the TV on its stand behind it
        float sb = Mathf.Min(width - .2f, tvInches * .0254f * .70f);
        RB(c, Fabric, k, -sb * .5f, sb * .5f, H, H + .065f, -.14f, -.04f, .025f, 3);
        RB(c, BlackMatt, k, -sb * .5f - .002f, sb * .5f + .002f, H + .060f, H + .068f, -.142f, -.038f, .004f, 1);
        Tv(c, k.P(0f, H, -.25f), f, tvInches, picture, true, label + " telewizor");
        Col(c, k, -width * .5f, width * .5f, 0f, H, -D, 0f, label);
    }

    // ---------------------------------------------------------------- SZAFKA ŁAZIENKOWA (a wall-hung vanity)
    // an oak (or given) cabinet with two drawers on push latches, a stone top, a vessel basin, a tall tap that runs with the
    // sensor, a soap dispenser; p = the wall point under the vanity's centre at floor level, f = into the room
    public static void Vanity(Ctx c, Vector3 p, Vector3 f, float width, Material body, string label = "umywalka")
    {
        Materials(); var k = new Fr(p, f); const float D = .50f;
        RB(c, body, k, -width * .5f, width * .5f, .40f, .80f, 0f, D - .02f, .004f, 1);
        for (int j = 0; j < 2; j++) RB(c, body, k, -width * .5f + .0015f, width * .5f - .0015f, .40f + j * .20f + .0015f, .60f + j * .20f - .0015f, D - .02f, D - .001f, .003f, 1);
        RB(c, BlackMatt, k, -width * .5f + .02f, width * .5f - .02f, .60f - .010f, .60f + .010f, D - .02f, D + .002f, .002f, 1);   // the finger pull between them
        RB(c, Stone, k, -width * .5f - .01f, width * .5f + .01f, .80f, .84f, 0f, D + .01f, .006f, 2);
        var top = k.P(0f, .84f, D * .55f);
        c.m.Lathe(Porcelain, top, new[] { new Vector2(0f, 0f), new Vector2(.11f, 0f), new Vector2(.17f, .03f), new Vector2(.205f, .10f), new Vector2(.212f, .14f), new Vector2(.200f, .143f), new Vector2(.192f, .10f), new Vector2(.155f, .045f), new Vector2(.06f, .022f), new Vector2(0f, .02f) }, 36, Quaternion.identity, false, false);
        c.m.Cylinder(Chrome, top + Vector3.up * .014f, .032f, .012f, 18, Quaternion.identity, true);
        var tb = k.P(0f, .84f, .07f);
        c.m.Cylinder(Chrome, tb, .030f, .012f, 16, Quaternion.identity, true);
        c.m.Cylinder(Chrome, tb, .016f, .30f, 14, Quaternion.identity, false);
        var sp = tb + Vector3.up * .30f; c.m.Tube(Chrome, sp - Vector3.up * .01f, sp + k.f * (D * .55f - .07f), .015f, 12);
        var aer = sp + k.f * (D * .55f - .07f);
        c.m.Cylinder(Chrome, aer - Vector3.up * .035f, .018f, .045f, 14, Quaternion.identity, true, true);
        if (c.water)
        {
            var o = aer - Vector3.up * .036f;
            BraxChampionsMeshBatch.FountainWater(c.root, label + " | kran z czujnikiem — woda", o, c.unit, new[] { Vector3.zero }, new[] { new Vector3(.002f, top.y + .025f - o.y, 0f) },
                new[] { .004f }, new[] { .016f }, new[] { 12 }, new[] { true }, new[] { new Vector3(top.x - o.x, top.y + .03f - o.y, top.z - o.z) }, new[] { .10f }, c.water, 1.6f);
        }
        var sd = k.P(width * .5f - .10f, .84f, .10f);
        c.m.Cylinder(Chrome, sd, .022f, .012f, 14, Quaternion.identity, true); c.m.Cylinder(Chrome, sd, .012f, .17f, 12, Quaternion.identity, true);
        c.m.Tube(Chrome, sd + Vector3.up * .16f, sd + Vector3.up * .17f + k.f * .08f, .008f, 8);
        Col(c, k, -width * .5f, width * .5f, .40f, .84f, 0f, D, label);
    }
}
