using System.Collections.Generic;
using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - the villas' shared parts (owner 17.11 "każda ma być inna", "konkretniejsza"): walls with openings,
// gabled roofs in standing-seam metal, gable ends (rendered, glazed, framed in black steel), straight stairs, the
// cantilevered infinity pool, multi-bay garages, the sale board. Nominal metres in the villa's frame (the view to -z),
// built into the current villa's batches (BraxChampionsWille.Willa).
public static partial class BraxChampionsWille
{
    // ---------------------------------------------------------------- walls
    /// <summary>An outside wall (BraxChampionsWille.Sciana: two skins, the outer face on plane) with rectangular openings
    /// (a0, a1 along the wall, y0, y1 = sill and head); glazed when szklo.</summary>
    static void ScianaO(Material zew, bool alongX, float plane, float a0, float a1, float y0, float y1, float outward, bool szklo, params Vector4[] okna)
        => ScianaOE(zew, alongX, plane, a0, a1, y0, y1, outward, szklo, new Vector4(a0, a1, a0, a1), okna);
    /// <summary>ScianaO with the skins' own extents ext = (outer from, outer to, inner from, inner to) along the wall: at an
    /// outside corner the wall along z keeps its outer skin to the corner and stops its inner skin T short, the wall along x
    /// runs its outer skin between the other's outer skins and its inner between the rooms' faces - no end face of a
    /// painted inner skin in the facade, no two facade faces on one plane (17.11: light stripes at the corners).</summary>
    static void ScianaOE(Material zew, bool alongX, float plane, float a0, float a1, float y0, float y1, float outward, bool szklo, Vector4 ext, params Vector4[] okna)
    {
        // the wall as a grid of cells on every opening's edges: neighbouring pieces share whole edges (17.11: pieces abutting
        // with T-junctions showed hairline cracks - the light inner skin - in the facade)
        var xs = new List<float> { a0, a1 }; var ys = new List<float> { y0, y1 };
        foreach (var o in okna) { xs.Add(Mathf.Clamp(o.x, a0, a1)); xs.Add(Mathf.Clamp(o.y, a0, a1)); ys.Add(Mathf.Clamp(o.z, y0, y1)); ys.Add(Mathf.Clamp(o.w, y0, y1)); }
        xs.Sort(); ys.Sort();
        for (int i = 0; i + 1 < xs.Count; i++)
        {
            if (xs[i + 1] - xs[i] < .01f) continue;
            for (int j = 0; j + 1 < ys.Count; j++)
            {
                if (ys[j + 1] - ys[j] < .01f) continue;
                float cx = (xs[i] + xs[i + 1]) * .5f, cy = (ys[j] + ys[j + 1]) * .5f; bool hole = false;
                foreach (var o in okna) if (cx > o.x && cx < o.y && cy > o.z && cy < o.w) { hole = true; break; }
                if (!hole) ScianaE(zew, alongX, plane, xs[i], xs[i + 1], ys[j], ys[j + 1], outward, ext);
            }
        }
        if (szklo) foreach (var o in okna) Szyba(alongX, plane - outward * T * .5f, o.x + .031f, o.y - .031f, o.z, o.w);
    }
    /// <summary>BraxChampionsWille.Sciana with the outer skin clipped to ext.x..ext.y and the inner to ext.z..ext.w.</summary>
    static void ScianaE(Material zew, bool alongX, float plane, float a0, float a1, float y0, float y1, float outward, Vector4 ext)
    {
        float t = T * .5f, o0 = plane, o1 = plane - outward * t, i0 = plane - outward * (t + .004f), i1 = plane - outward * T;
        float oa = Mathf.Max(a0, ext.x), ob = Mathf.Min(a1, ext.y), ia = Mathf.Max(a0, ext.z), ib = Mathf.Min(a1, ext.w);
        if (alongX)
        {
            Out_(true); if (ob - oa > .005f) MMd(zew, oa, ob, y0, y1, Mathf.Min(o0, o1), Mathf.Max(o0, o1), true);
            Out_(false); if (ib - ia > .005f) MMd(renderIn, ia, ib, y0, y1, Mathf.Min(i0, i1), Mathf.Max(i0, i1), false);
        }
        else
        {
            Out_(true); if (ob - oa > .005f) MMd(zew, Mathf.Min(o0, o1), Mathf.Max(o0, o1), y0, y1, oa, ob, true);
            Out_(false); if (ib - ia > .005f) MMd(renderIn, Mathf.Min(i0, i1), Mathf.Max(i0, i1), y0, y1, ia, ib, false);
        }
    }
    /// <summary>An axis box built from its corner coordinates (W.MM builds from centre +- half size: two cells meeting on
    /// one edge got float-different vertices - hairline cracks in the preview); a collider when col.</summary>
    static void MMd(Material m, float x0, float x1, float y0, float y1, float z0, float z1, bool col)
    {
        Vector3 P(int sx, int sy, int sz) => new Vector3(sx < 0 ? x0 : x1, sy < 0 ? y0 : y1, sz < 0 ? z0 : z1);
        W.Quad(m, P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1));
        W.Quad(m, P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1));
        W.Quad(m, P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1));
        W.Quad(m, P(1, -1, 1), P(1, -1, -1), P(1, 1, -1), P(1, 1, 1));
        W.Quad(m, P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), P(-1, 1, -1));
        W.Quad(m, P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1));
        if (col) W.Kolizja(new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f), new Vector3(x1 - x0, y1 - y0, z1 - z0), Quaternion.identity);
    }
    /// <summary>Skin extents at the corners: a wall along z from z0 to z1 (its front end glazed or a corner), a wall along x.</summary>
    static Vector4 RogZ(float z0, float z1, bool przodSzklo) => new Vector4(z0, z1, przodSzklo ? z0 : z0 + T, z1 - T);
    static Vector4 RogX(float x0, float x1) => new Vector4(x0 + T * .5f, x1 - T * .5f, x0 + T, x1 - T);
    /// <summary>A partition between rooms with door openings, .12 thick, from y0 to y1 (doors 2.25 high): Przegroda.</summary>
    static void Scianka(bool alongX, float plane, float a0, float a1, float y0, float y1, params Vector2[] drzwi) => Przegroda(alongX, plane, a0, a1, y0, y1, drzwi);

    // ---------------------------------------------------------------- gabled roofs (the ridge along z)
    /// <summary>Gabled roof over the wall plates x0, x1 (eaves at e), from z0 to z1, pitch in degrees: two slabs of
    /// standing-seam metal (seams every .55 m) meeting at the ridge, ov over the eaves, og over the gable ends; lining (if
    /// any) under the slabs between the walls (a vaulted ceiling). Returns the rise from the eaves to the ridge.</summary>
    static float Dach2(float x0, float x1, float z0, float z1, float e, float pitch, Material lining = null, float ov = .35f, float og = .3f)
    {
        Out_(true);
        float a = pitch * Mathf.Deg2Rad, h = (x1 - x0) * .5f, xm = (x0 + x1) * .5f, r = h * Mathf.Tan(a), t = .26f;
        float ls = (h + ov) / Mathf.Cos(a) + t * Mathf.Tan(a), len = z1 - z0 + 2f * og, zm = (z0 + z1) * .5f;
        foreach (float s in new[] { -1f, 1f })            // -1 the west slope, +1 the east
        {
            var u = new Vector3(-s * Mathf.Cos(a), Mathf.Sin(a), 0f);           // up the slope toward the ridge
            var n = new Vector3(s * Mathf.Sin(a), Mathf.Cos(a), 0f);            // the slope's outward normal
            var start = new Vector3(s < 0f ? x0 - ov : x1 + ov, e - ov * Mathf.Tan(a), zm);
            var c = start + u * (ls * .5f) + n * (t * .5f);
            var q = Quaternion.Euler(0f, 0f, -s * pitch);
            b.Box(dachBlacha, c, new Vector3(ls, t, len), q);
            W.Kolizja(c, new Vector3(ls, t, len), q);
            for (float z = z0 - og + .3f; z < z1 + og - .2f; z += .55f)           // the standing seams
                b.Box(dachBlacha, c + n * (t * .5f + .022f) + new Vector3(0f, 0f, z - zm), new Vector3(ls - .02f, .044f, .028f), q, false);
            if (lining != null)
            {
                // the underside between the walls, 2 cm under the slab: from the wall plate to the ridge
                float xa = s < 0f ? x0 + T : x1 - T, ya = e + T * Mathf.Tan(a) - .02f, yb = e + r - .02f;
                var A = new Vector3(xa, ya, z0 + T); var B = new Vector3(xm, yb, z0 + T); var C = new Vector3(xm, yb, z1 - T); var D = new Vector3(xa, ya, z1 - T);
                if (s < 0f) b.Quad(lining, A, B, C, D); else b.Quad(lining, D, C, B, A);
            }
        }
        return r;
    }
    /// <summary>The gable's triangle over a wall x0..x1 (its eaves e, rise r) on the plane z: the outer face in zew,
    /// the inner (T behind) in wew; outward -1 = the front (facing -z), +1 the back.</summary>
    static void Szczyt(Material zew, Material wew, float x0, float x1, float e, float r, float z, float outward)
    {
        Out_(true);
        float xm = (x0 + x1) * .5f, zi = z - outward * T;
        Vector3 A(float zz) => new Vector3(x0, e, zz); Vector3 B(float zz) => new Vector3(xm, e + r, zz); Vector3 C(float zz) => new Vector3(x1, e, zz);
        if (outward < 0f) { b.Triangle(zew, A(z), B(z), C(z)); b.Triangle(wew, C(zi), B(zi), A(zi)); }
        else { b.Triangle(zew, C(z), B(z), A(z)); b.Triangle(wew, A(zi), B(zi), C(zi)); }
    }
    /// <summary>A glazed gable's triangle: panes between black mullions every ~2.2 m under the rakes, both faces.</summary>
    static void SzczytSzyba(float x0, float x1, float e, float r, float z)
    {
        Out_(true);
        float xm = (x0 + x1) * .5f, hw = (x1 - x0) * .5f;
        float Top(float x) => e + r * (1f - Mathf.Abs(x - xm) / hw);
        int n = Mathf.Max(2, Mathf.RoundToInt((x1 - x0) / 2.2f)); if (n % 2 == 1) n++;
        float w = (x1 - x0) / n;
        for (int i = 0; i < n; i++)
        {
            float p0 = x0 + i * w + .03f, p1 = x0 + (i + 1) * w - .03f;
            var a = new Vector3(p0, e + .05f, z); var bb = new Vector3(p0, Top(p0) - .08f, z); var c = new Vector3(p1, Top(p1) - .08f, z); var d = new Vector3(p1, e + .05f, z);
            b.Quad(glass, a, bb, c, d); b.Quad(glass, d, c, bb, a);
            if (i > 0) { float x = x0 + i * w; b.Box(alu, new Vector3(x, (e + Top(x)) * .5f, z), new Vector3(.06f, Top(x) - e, .1f), Quaternion.identity); }
        }
        b.Box(alu, new Vector3(xm, e + .025f, z), new Vector3(x1 - x0, .05f, .1f), Quaternion.identity);
        float a2 = Mathf.Atan2(r, hw) * Mathf.Rad2Deg, l = Mathf.Sqrt(hw * hw + r * r);
        b.Box(alu, new Vector3((x0 + xm) * .5f, e + r * .5f - .03f, z), new Vector3(l, .06f, .1f), Quaternion.Euler(0f, 0f, a2));
        b.Box(alu, new Vector3((x1 + xm) * .5f, e + r * .5f - .03f, z), new Vector3(l, .06f, .1f), Quaternion.Euler(0f, 0f, -a2));
    }
    /// <summary>The black steel frame round a gable end (Forest Knoll: "large steel frames at the gable ends"): two posts
    /// from the ground to the eaves and the two rakes, .3 x .45, standing .45 proud of the facade on the plane z.</summary>
    static void RamaSzczytu(float x0, float x1, float e, float r, float z, float outward)
    {
        Out_(true);
        const float w = .3f, d = .45f;
        float xm = (x0 + x1) * .5f, hw = (x1 - x0) * .5f, zc = z + outward * (d * .5f + .006f);
        foreach (float x in new[] { x0 + w * .5f - .06f, x1 - w * .5f + .06f })
        {
            b.Box(stalCzarna, new Vector3(x, e * .5f, zc), new Vector3(w, e, d), Quaternion.identity);
            W.Kolizja(new Vector3(x, e * .5f, zc), new Vector3(w, e, d), Quaternion.identity);
        }
        float a = Mathf.Atan2(r, hw), l = Mathf.Sqrt(hw * hw + r * r) + .25f;
        var nW = new Vector3(-Mathf.Sin(a), Mathf.Cos(a), 0f) * (w * .5f - .02f);    // its underside 2 cm under the roof slab's
        b.Box(stalCzarna, new Vector3((x0 + xm) * .5f, e + r * .5f, zc) + nW, new Vector3(l, w, d), Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg));
        b.Box(stalCzarna, new Vector3((x1 + xm) * .5f, e + r * .5f, zc) + new Vector3(-nW.x, nW.y, 0f), new Vector3(l, w, d), Quaternion.Euler(0f, 0f, -a * Mathf.Rad2Deg));
    }

    // ---------------------------------------------------------------- a straight stair along +z (or -z when dz < 0)
    /// <summary>Floating stone treads xa..xb from (zb, y0) rising n risers to y1 (going dz per tread, toward +z for dz > 0),
    /// a black steel stringer on the open side (xOpen), a glass balustrade over it; W.Rampa for walking.</summary>
    static void Schody(Material stopien, float xa, float xb, float zb, float y0, float y1, int n, float dz, float xOpen)
    {
        Out_(false);
        float riser = (y1 - y0) / n;
        for (int i = 0; i < n; i++)
        {
            float za = zb + i * dz, zc = zb + (i + 1) * dz + Mathf.Sign(dz) * .02f, top = y0 + (i + 1) * riser;
            MM(stopien, xa, xb, top - .07f, top, Mathf.Min(za, zc), Mathf.Max(za, zc), false);
        }
        float run = n * dz, xm = (xa + xb) * .5f, zt = zb + run;
        W.Rampa(new Vector3(xm, y1, zt), new Vector3(xm, y0, zb), xb - xa);
        float slope = Mathf.Atan2(y1 - y0, Mathf.Abs(run)) * Mathf.Rad2Deg * Mathf.Sign(dz), len = Mathf.Sqrt((y1 - y0) * (y1 - y0) + run * run);
        var mid = new Vector3(xOpen, (y0 + y1) * .5f - .2f, (zb + zt) * .5f);
        b.Box(stalCzarna, mid, new Vector3(.04f, .3f, len), Quaternion.Euler(-slope, 0f, 0f));
        b.Box(glass, mid + new Vector3(0f, .7f, 0f), new Vector3(.018f, 1.0f, len), Quaternion.Euler(-slope, 0f, 0f));
        b.Box(steel, mid + new Vector3(0f, 1.22f, 0f), new Vector3(.05f, .04f, len), Quaternion.Euler(-slope, 0f, 0f));
    }

    // ---------------------------------------------------------------- a garage door (an oak up-and-over leaf on AKCJA)
    /// <summary>One up-and-over door xa..xb, h high, in the wall plane z (the outside toward +z): the leaf hangs from its
    /// top edge inside the opening and swings in and up (BraxChampionsSzafa, os = right), its own AKCJA points.</summary>
    static void BramaGarazu(string id, float xa, float xb, float z, float h)
    {
        var holder = new GameObject("garaż | " + id).transform; holder.SetParent(g, false);
        var hinge = new GameObject("garaż | " + id + " (zawias u góry)").transform; hinge.SetParent(holder, false);
        float xm = (xa + xb) * .5f, w = xb - xa;
        hinge.localPosition = new Vector3(xm, h, z - .2f);
        var leaf = new BraxChampionsMeshBatch();
        int n = Mathf.RoundToInt(h / .2f);
        for (int i = 0; i < n; i++) leaf.Box(soffit, new Vector3(0f, -.1f - i * .2f, 0f), new Vector3(w - .02f, .185f, .05f), Quaternion.identity);
        leaf.Box(alu, new Vector3(0f, -h * .5f, -.04f), new Vector3(w - .1f, h - .04f, .02f), Quaternion.identity);
        leaf.Box(steel, new Vector3(0f, -h + .15f, .045f), new Vector3(.5f, .04f, .04f), Quaternion.identity);
        var leafT = leaf.Build(hinge, "garaż | " + id + " (dąb)", Out + "/Meshes", true, 1f);
        var bc = leafT.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, -h * .5f, 0f); bc.size = new Vector3(w, h, .1f);
        var drzwi = holder.gameObject.AddComponent<BraxChampionsSzafa>();
        drzwi.zawias = hinge; drzwi.os = Vector3.right; drzwi.katOtwarcia = 90f; drzwi.napis = "BRAMA GARAŻU"; drzwi.komunikat = ""; drzwi.zasieg = 2.2f;
        drzwi.punkty = new[] { Punkt("garaż | " + id + " | AKCJA przed bramą", new Vector3(xm, 0f, z + 1.4f)), Punkt("garaż | " + id + " | AKCJA w garażu", new Vector3(xm, 0f, z - 3.2f)) };
    }

    // ---------------------------------------------------------------- the infinity pool, its front over the pad's edge
    /// <summary>An infinity pool x0..x1, z0..z1 (water at Water, floor at PoolFloor): its front (z0) wall 15 mm under the
    /// water spills into a catch trough outside; a concrete slab under it all (the part past the pad's edge hangs free -
    /// the cantilever); steps in at the x1 end; the swim zone; coping (the deck's own edge) is the caller's.</summary>
    static void BasenWiszacy(string id, float x0, float x1, float z0, float z1)
    {
        Out_(true);
        const float sk = .3f, tl = .02f;
        float pf = PoolFloor, bot = pf - .45f;
        // the structure: the slab under the floor and the trough, the side walls' and the trough lip's outer skins (render)
        MM(elewacja, x0 - sk - .024f, x1 + sk + .024f, bot, pf - tl - .004f, z0 - .9f, z1 + sk);
        foreach (float s in new[] { -1f, 1f })
        {
            float xo0 = s < 0f ? x0 - sk - .024f : x1 + tl + .004f, xo1 = s < 0f ? x0 - tl - .004f : x1 + sk + .024f;
            MM(elewacja, xo0, xo1, pf - tl, -.3f, z0 - .9f, z1 + sk);
        }
        // the tiled shell: floor, side and back walls up to the coping, the front (infinity) wall 15 mm under the water
        MM(poolTile, x0, x1, pf - tl, pf, z0, z1);
        MM(poolTile, x0 - tl, x0, pf - tl, -.3f, z0 - sk, z1); MM(poolTile, x1, x1 + tl, pf - tl, -.3f, z0 - sk, z1);
        MM(poolTile, x0 - tl, x1 + tl, pf - tl, -.3f, z1, z1 + sk);
        MM(poolTile, x0, x1, pf, Water - .015f, z0 - sk, z0);
        // the catch trough outside the front: its floor 45 cm under the water, the outer lip 30 cm under it (the pool meets the sky)
        MM(poolTile, x0, x1, pf - tl, -.5f, z0 - .6f, z0 - sk);
        MM(elewacja, x0, x1, pf - tl, -.33f, z0 - .9f, z0 - .6f);
        MM(poolTile, x0, x1, -.33f, -.30f, z0 - .9f, z0 - .6f, false);
        Plane(bw, waterMat, new Vector3(x0, -.45f, z0 - .6f), new Vector3(x1 - x0, 0, 0), new Vector3(0, 0, .3f));
        // the steps in at the x1 end (four treads along the back wall)
        for (int i = 0; i < 4; i++)
        {
            float top = -.32f - i * .32f;
            MM(poolTile, x1 - 2.0f, x1, pf, top, z1 - .45f * (i + 1), z1);
        }
        MM(steel, x1 - .45f, x1 - .40f, Water, .95f, z1 + .25f, z1 + .30f, false);
        b.Tube(steel, new Vector3(x1 - .425f, .95f, z1 + .275f), new Vector3(x1 - .425f, .95f, z1 - .55f), .022f, 10);
        b.Tube(steel, new Vector3(x1 - .425f, .95f, z1 - .55f), new Vector3(x1 - .425f, -.55f, z1 - 1.35f), .022f, 10);
        // the water, its body, the swim zone; lights in the back wall
        Plane(bw, waterMat, new Vector3(x0, Water, z0), new Vector3(x1 - x0, 0, 0), new Vector3(0, 0, z1 - z0));
        Ton(x0 + .05f, x1 - .05f, pf + .05f, Water - .005f, z0 + .05f, z1 - .05f);
        var surf = new GameObject(id + " | lustro wody").transform; surf.SetParent(g, false); surf.localPosition = new Vector3((x0 + x1) * .5f, Water, (z0 + z1) * .5f);
        var zone = new GameObject(id + " | strefa pływania").AddComponent<BoxCollider>(); zone.transform.SetParent(g, false);
        zone.transform.localPosition = new Vector3((x0 + x1) * .5f, (pf + .05f + Water + .35f) * .5f, (z0 + z1) * .5f);
        zone.size = new Vector3(x1 - x0 - .7f, Water + .35f - pf - .05f, z1 - z0 - .7f); zone.isTrigger = true;
        var sw = zone.gameObject.AddComponent<BraxChampionsPoolSwimVolume>(); sw.waterSurface = surf; sw.basinId = id;
        for (float x = x0 + 3f; x < x1 - 2.5f; x += 5f) b.Cylinder(BraxKatalog.Led, new Vector3(x, -.75f, z1 - .001f), .12f, .012f, 16, Quaternion.Euler(90f, 0f, 0f), true);
    }

    /// <summary>The raised stone jacuzzi at c (BraxChampionsWille.Jacuzzi_ at any spot), steps on its +z side.</summary>
    static void JacuzziW(Vector3 c)
    {
        Out_(true);
        const float R = 1.35f, r = 1.10f, top = .90f, wl = .82f;
        b.Cylinder(stone, c, R, top, 40, Quaternion.identity, false);
        b.Surface(deck, (u, v) => c + new Vector3(Mathf.Cos(u * Mathf.PI * 2f) * Mathf.Lerp(R, r, v), top, Mathf.Sin(u * Mathf.PI * 2f) * Mathf.Lerp(R, r, v)), 40, 1, true);
        b.Surface(poolTile, (u, v) => c + new Vector3(Mathf.Cos(u * Mathf.PI * 2f) * r, Mathf.Lerp(.03f, top, v), Mathf.Sin(u * Mathf.PI * 2f) * r), 40, 2);
        b.Surface(poolTile, (u, v) => c + new Vector3(Mathf.Cos(u * Mathf.PI * 2f) * Mathf.Lerp(r, .45f, v), .45f, Mathf.Sin(u * Mathf.PI * 2f) * Mathf.Lerp(r, .45f, v)), 40, 1, true);
        b.Cylinder(poolTile, c + Vector3.up * .03f, .45f, .42f, 32, Quaternion.identity, false);
        b.Surface(poolTile, (u, v) => c + new Vector3(Mathf.Cos(u * Mathf.PI * 2f) * .45f * v, .03f, Mathf.Sin(u * Mathf.PI * 2f) * .45f * v), 32, 1);
        b.Surface(spaWater, (u, v) => c + new Vector3(Mathf.Cos(u * Mathf.PI * 2f) * (r - .01f) * v, wl, Mathf.Sin(u * Mathf.PI * 2f) * (r - .01f) * v), 40, 1);
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad, rm = (R + r) * .5f;
            W.Kolizja(c + new Vector3(Mathf.Cos(a) * rm, top * .5f, Mathf.Sin(a) * rm), new Vector3(.26f, top, .68f), Quaternion.Euler(0f, -i * 30f, 0f));
        }
        MM(stone, c.x - .7f, c.x + .7f, 0f, .30f, c.z + R - .05f, c.z + R + .45f);
        MM(stone, c.x - .7f, c.x + .7f, 0f, .60f, c.z + R - .2f, c.z + R + .05f);
        var spa = new GameObject("jacuzzi | lustro wody").transform; spa.SetParent(g, false); spa.localPosition = c + Vector3.up * wl;
        var zone = new GameObject("jacuzzi | strefa").AddComponent<BoxCollider>(); zone.transform.SetParent(g, false);
        zone.transform.localPosition = c + new Vector3(0f, (top + .5f + .05f) * .5f, 0f); zone.size = new Vector3(1.5f, top + .5f - .05f, 1.5f); zone.isTrigger = true;
        var sw = zone.gameObject.AddComponent<BraxChampionsPoolSwimVolume>(); sw.waterSurface = spa; sw.basinId = "willa_jacuzzi";
        W.Siedzisko(c + new Vector3(0f, .45f, -.78f), Vector3.forward, "JACUZZI"); W.Siedzisko(c + new Vector3(.78f, .45f, 0f), Vector3.left, "JACUZZI"); Siedziska += 2;
    }

    /// <summary>Teak loungers in a row from p along +x (step apart), facing f.</summary>
    static void Lezaki(Vector3 p, int n, float step, Vector3 f)
    {
        Out_(true);
        var q = Quaternion.LookRotation(-f, Vector3.up);
        for (int i = 0; i < n; i++)
        {
            var c = p + Vector3.right * (i * step);
            b.RoundedBox(teak, c + Vector3.up * .17f, new Vector3(.72f, .08f, 2.0f), .02f, q, 2);
            foreach (float sx in new[] { -.3f, .3f }) foreach (float sz in new[] { -.9f, .9f }) b.Box(teak, c + q * new Vector3(sx, .07f, sz), new Vector3(.05f, .14f, .05f), q);
            b.RoundedBox(cushion, c + q * new Vector3(0f, .25f, -.25f), new Vector3(.68f, .08f, 1.45f), .03f, q, 2);
            b.RoundedBox(cushion, c + q * new Vector3(0f, .48f, .72f), new Vector3(.68f, .08f, .62f), .03f, q * Quaternion.Euler(-48f, 0f, 0f), 2);
            W.Kolizja(c + Vector3.up * .15f, new Vector3(.72f, .3f, 2.0f), q);
            W.Siedzisko(c + Vector3.up * .3f, f, "LEŻAK", true); Siedziska++;
        }
    }

    /// <summary>The sale board (white, two black posts, 3D letters) at p, its face toward f; AKCJA asks Monika.</summary>
    static void TablicaSprzedazy(int nr, Vector3 p, Vector3 f, string opis)
    {
        Out_(true);
        f.y = 0f; f.Normalize(); var q = Quaternion.LookRotation(f, Vector3.up); var right = q * Vector3.right;
        foreach (float sx in new[] { -.75f, .75f }) b.Box(black, p + right * sx + Vector3.up * .9f, new Vector3(.08f, 1.8f, .08f), q);
        b.Box(signWhite, p + Vector3.up * 1.45f + f * .05f, new Vector3(1.8f, 1.0f, .04f), q);
        b.Box(BraxKatalog.LedRed, p + Vector3.up * 1.86f + f * .0745f, new Vector3(1.8f, .14f, .004f), q);
        float yaw = BraxChampionsLetters3D.YawFacing(f);
        BraxChampionsLetters3D.Text(b, black, black, "NA SPRZEDAŻ", p + Vector3.up * 1.45f + f * .072f, yaw, .15f, .01f, .1f, 1.6f);
        BraxChampionsLetters3D.Text(b, black, black, "WAWER NIERUCHOMOŚCI", p + Vector3.up * 1.17f + f * .072f, yaw, .07f, .006f, .1f, 1.6f);
        BraxChampionsLetters3D.Text(b, black, black, Nazwy[nr - 1], p + Vector3.up * 1.01f + f * .072f, yaw, .06f, .006f, .1f, 1.6f);
        W.Grupa = g;
        W.Akcja("willa_" + nr + "_sprzedaz", Nazwy[nr - 1] + " · NA SPRZEDAŻ", opis, p + f * 1.2f);
        W.Kolizja(p + Vector3.up * .9f, new Vector3(1.8f, 1.8f, .12f), q);
    }

    /// <summary>A tree at (x, z) on the ground at y: the region's own drzewo_verdant (Poly Haven island tree / jacaranda
    /// leaves, LOD 49k / 28k / 13k - the streets', D2's), its real size (the villa is built in metres, scaled by K once);
    /// a collider on its trunk.</summary>
    static void Drzewo(float x, float z, float y = 0f)
    {
        var t = BraxRegion.Model("drzewo_verdant");
        if (!t) { BraxBuildQuality.Warn("DO POPRAWKI WILLA_TREE_MISSING: drzewo_verdant"); return; }
        if (!BraxRegion.Put(g, t, "WILLA | drzewo (drzewo_verdant)", new Vector3(x, y, z), (x * 31f + z * 17f) % 360f, 1f, false, 0f, 0f, false)) return;
        W.Kolizja(new Vector3(x, y + 1.5f, z), new Vector3(.5f, 3f, .5f), Quaternion.identity);
        Drzewa++;
    }
    /// <summary>A palm at (x, z) on the ground at y (the region's miasto_palmy).</summary>
    static void Palma(float x, float z, float y = 0f)
    {
        var palm = BraxMiastoAssety.Model("miasto_palmy", "palma");
        if (!palm) { BraxBuildQuality.Warn("DO POPRAWKI WILLA_PALMS_MISSING: miasto_palmy/palma"); return; }
        var at = new Vector3(x, y, z);
        var o = BraxChampionsModel.Postaw(g, palm, "willa palma", at, (x * 11f + z * 5f) % 360f, 1f, false, true);
        if (!o) return;
        o.transform.localScale = Vector3.one;
        BraxMiastoAssety.LodyZWezlow(o, new[] { .25f, .08f, .012f });
        BraxChampionsPalmTrunk.OnSpot(o, at);
        Palmy++;
    }
    /// <summary>The ceiling's LED downlights over x0..x1, z0..z1 at height y (a step grid), skipping the box skip (x0,x1,z0,z1).</summary>
    static void Oczka(float x0, float x1, float z0, float z1, float y, float step = 2.4f, Vector4? skip = null)
    {
        for (float x = x0; x <= x1 + .01f; x += step) for (float z = z0; z <= z1 + .01f; z += step)
        {
            if (skip.HasValue && x > skip.Value.x && x < skip.Value.y && z > skip.Value.z && z < skip.Value.w) continue;
            b.Cylinder(downlight, new Vector3(x, y - .007f, z), .065f, .003f, 16, Quaternion.identity, false, true);
        }
    }
}
