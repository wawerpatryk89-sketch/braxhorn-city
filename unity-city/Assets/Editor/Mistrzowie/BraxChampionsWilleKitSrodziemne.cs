using System;
using System.Collections.Generic;
using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - the Mediterranean villas' parts (owner 17.11, his photos: "No bo dobra, zrób takie" - the villa on the
// cliff and the warm beige villa): hip roofs in clay barrel tiles (Poly Haven clay_roof_tiles_02) with ridge and hip
// tiles, boxed eaves on dark timber corbels; round arches (arcades, arched doors and windows) cut through the wall's whole
// thickness; stone columns, baluster balustrades, chimneys; a free-form pool; a courtyard fountain; a spiral stair.
// Nominal metres in the villa's frame (the view to -z).
public static partial class BraxChampionsWille
{
    // ---------------------------------------------------------------- hip roofs
    /// <summary>A hip roof over the wall plates x0..x1, z0..z1 (eaves at e, pitch in degrees, ov over the walls): four
    /// tiled planes (u along the eave, v down the slope in metres), the ridge and hips capped with barrel tiles, a
    /// boxed eave (soffit, fascia) on corbels every 1.2 m. przyMurze (-x, +x, -z, +z: 1..4, 0 none) = the side that butts
    /// against a taller wall: no overhang and no hip there, the ridge runs into the wall. Returns the ridge's height.</summary>
    static float Dach4(float x0, float x1, float z0, float z1, float e, float pitch, Material sciana, float ov = .6f, int przyMurze = 0)
    {
        Out_(true);
        float a = pitch * Mathf.Deg2Rad, ta = Mathf.Tan(a), h0 = .2f;
        float X0 = x0 - (przyMurze == 1 ? 0f : ov), X1 = x1 + (przyMurze == 2 ? 0f : ov), Z0 = z0 - (przyMurze == 3 ? 0f : ov), Z1 = z1 + (przyMurze == 4 ? 0f : ov);
        bool alongX = (x1 - x0) >= (z1 - z0);
        float half = alongX ? (z1 - z0) * .5f : (x1 - x0) * .5f;
        float ye = e + h0 - ov * ta, yr = e + h0 + half * ta;
        float xm = (x0 + x1) * .5f, zm = (z0 + z1) * .5f;
        // the ridge's ends: half the span (plus the overhang) in from the hipped ends, at the wall on a butting end
        Vector3 R0, R1;
        if (alongX) { R0 = new Vector3(przyMurze == 1 ? x0 : X0 + half + ov, yr, zm); R1 = new Vector3(przyMurze == 2 ? x1 : X1 - half - ov, yr, zm); }
        else { R0 = new Vector3(xm, yr, przyMurze == 3 ? z0 : Z0 + half + ov); R1 = new Vector3(xm, yr, przyMurze == 4 ? z1 : Z1 - half - ov); }
        var c00 = new Vector3(X0, ye, Z0); var c10 = new Vector3(X1, ye, Z0); var c11 = new Vector3(X1, ye, Z1); var c01 = new Vector3(X0, ye, Z1);
        // a butting end: the planes' corners there stay at the wall at the eaves' height of the wall plate
        if (przyMurze == 1) { c00.x = c01.x = x0; } if (przyMurze == 2) { c10.x = c11.x = x1; }
        if (przyMurze == 3) { c00.z = c10.z = z0; } if (przyMurze == 4) { c01.z = c11.z = z1; }
        if (alongX)
        {
            Plaszcz(new[] { c00, R0, R1, c10 }, Vector3.right, Vector3.forward, a);         // south (-z)
            Plaszcz(new[] { c11, R1, R0, c01 }, Vector3.left, Vector3.back, a);             // north (+z)
            if (przyMurze != 1) Plaszcz(new[] { c01, R0, c00 }, Vector3.back, Vector3.right, a);
            if (przyMurze != 2) Plaszcz(new[] { c10, R1, c11 }, Vector3.forward, Vector3.left, a);
        }
        else
        {
            Plaszcz(new[] { c01, R1, R0, c00 }, Vector3.back, Vector3.right, a);             // west (-x)
            Plaszcz(new[] { c10, R0, R1, c11 }, Vector3.forward, Vector3.left, a);           // east (+x)
            if (przyMurze != 3) Plaszcz(new[] { c00, R0, c10 }, Vector3.right, Vector3.forward, a);
            if (przyMurze != 4) Plaszcz(new[] { c11, R1, c01 }, Vector3.left, Vector3.back, a);
        }
        // the ridge and hip tiles (half-round barrels .1 over the planes)
        var up = Vector3.up * .06f;
        b.Tube(dachowka, R0 + up, R1 + up, .11f, 10);
        foreach (var (c, r, r0) in new[] { (c00, R0, true), (c01, alongX ? R0 : R1, alongX), (c10, alongX ? R1 : R0, !alongX), (c11, R1, false) })
        {
            bool hip = alongX ? (r0 ? przyMurze != 1 : przyMurze != 2) : (r0 ? przyMurze != 3 : przyMurze != 4);
            if (hip && (c - r).sqrMagnitude > 1f) b.Tube(dachowka, c + up, r + up, .1f, 8);
        }
        // the boxed eave: the soffit ring and the fascia under the tiles' edge, dark timber corbels under it; 18.11 (his zoom:
        // the stucco ran straight into the tiles) the fascia and a cornice under the soffit in the contrasting stone/ivory
        // and the barrel tiles' round ends along the eaves
        float ys = ye - .16f;
        var gzyms = sciana == tynkKrem ? deck : tynkKrem;
        var podbitka = cegla != null && sciana == cegla ? tynkKrem : sciana;     // 24.11 under the old brick villa's eaves a cream soffit
        void Okap(Vector3 p0, Vector3 p1, Vector3 w0, Vector3 w1, Vector3 outN)
        {
            p0.y = p1.y = w0.y = w1.y = ys;
            var q = new[] { w0, w1, p1, p0 };
            if (Vector3.Dot(Vector3.Cross(q[1] - q[0], q[2] - q[0]), Vector3.down) < 0f) Array.Reverse(q);
            b.Quad(podbitka, q[0], q[1], q[2], q[3]);
            var f0 = p0; var f1 = p1; var g0 = p0 + Vector3.up * .16f; var g1 = p1 + Vector3.up * .16f;
            var fq = new[] { f0, f1, g1, g0 };
            if (Vector3.Dot(Vector3.Cross(fq[1] - fq[0], fq[2] - fq[0]), outN) < 0f) Array.Reverse(fq);
            b.Quad(gzyms, fq[0], fq[1], fq[2], fq[3]);
            // the cornice: a 10 cm band 12 cm proud under the soffit and a 6 cm bead under it 7 cm proud (clear of the oculi's 4.5 cm rings); the eaves along
            // x run 12 cm on past the corners so the sides along z butt into them (no overlapping faces at the corners)
            bool wzdluzX = Mathf.Abs(outN.z) > .5f;
            float ext = wzdluzX ? .12f : 0f, ln = Vector3.Distance(w0, w1) + 2f * ext; var mid = (w0 + w1) * .5f;
            Vector3 Sz(float across, float h, float l) => wzdluzX ? new Vector3(l, h, across) : new Vector3(across, h, l);
            b.Box(gzyms, mid + outN * .06f + Vector3.down * .05f, Sz(.12f, .1f, ln), Quaternion.identity);
            b.Box(gzyms, mid + outN * .035f + Vector3.down * .13f, Sz(.07f, .06f, Vector3.Distance(w0, w1) + (wzdluzX ? .14f : 0f)), Quaternion.identity);
            float len = Vector3.Distance(w0, w1); var dir = (w1 - w0).normalized;
            for (float t = .6f; t < len - .3f; t += 1.2f)
            {
                var wp = w0 + dir * t; var c = wp + outN * ((ov - .05f) * .5f) + Vector3.down * .1f;
                b.Box(walnut, c, Mathf.Abs(outN.x) > .5f ? new Vector3(ov - .05f, .2f, .12f) : new Vector3(.12f, .2f, ov - .05f), Quaternion.identity);
            }
        }
        if (przyMurze != 3) Okap(new Vector3(X0, 0, Z0), new Vector3(X1, 0, Z0), new Vector3(x0, 0, z0), new Vector3(x1, 0, z0), Vector3.back);
        if (przyMurze != 4) Okap(new Vector3(X1, 0, Z1), new Vector3(X0, 0, Z1), new Vector3(x1, 0, z1), new Vector3(x0, 0, z1), Vector3.forward);
        if (przyMurze != 1) Okap(new Vector3(X0, 0, Z1), new Vector3(X0, 0, Z0), new Vector3(x0, 0, z1), new Vector3(x0, 0, z0), Vector3.left);
        if (przyMurze != 2) Okap(new Vector3(X1, 0, Z0), new Vector3(X1, 0, Z1), new Vector3(x1, 0, z0), new Vector3(x1, 0, z1), Vector3.right);
        // the eaves' barrel tiles: their round ends every 21.7 cm (two of the texture's courses, 2.5 m / 23), radius 7.5 cm,
        // 20 cm of each cap up the slope, closed at both ends; 30 cm clear of the hips
        void Gasiory(Vector3 e0, Vector3 e1, Vector3 outN)
        {
            float len = Vector3.Distance(e0, e1); if (len < .8f) return;
            var e = (e1 - e0) / len; var s = -outN * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a);
            var nn = Vector3.Cross(s, e); if (nn.y < 0f) nn = -nn;
            const float R = .075f, Ls = .2f; const int M = 6;
            void Tri(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 face)
            {
                if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), face) < 0f) b.Triangle(dachowka, p0, p2, p1); else b.Triangle(dachowka, p0, p1, p2);
            }
            for (float t = .3f; t <= len - .3f; t += .217f)
            {
                var c = e0 + e * t + nn * .005f;
                Vector3 Pt(float u, float v) { float th = Mathf.PI * u; return c + e * (R * Mathf.Cos(th)) + nn * (R * Mathf.Sin(th)) + s * (Ls * v); }
                for (int i = 0; i < M; i++)
                {
                    float u0 = (float)i / M, u1 = (float)(i + 1) / M, th = Mathf.PI * (u0 + u1) * .5f;
                    QF(dachowka, Pt(u0, 0f), Pt(u1, 0f), Pt(u1, 1f), Pt(u0, 1f), e * Mathf.Cos(th) + nn * Mathf.Sin(th));
                    Tri(c, Pt(u0, 0f), Pt(u1, 0f), -s); Tri(c + s * Ls, Pt(u0, 1f), Pt(u1, 1f), s);
                }
            }
        }
        if (przyMurze != 3) Gasiory(c00, c10, Vector3.back);
        if (przyMurze != 4) Gasiory(c11, c01, Vector3.forward);
        if (przyMurze != 1) Gasiory(c01, c00, Vector3.left);
        if (przyMurze != 2) Gasiory(c10, c11, Vector3.right);
        return yr;
    }
    // one tiled roof plane (3 or 4 points), turned to face up and out; its UVs: u along the eave, v down the slope (m);
    // its collider: a slab under it
    static void Plaszcz(Vector3[] p, Vector3 eave, Vector3 upSlope, float a)
    {
        var n = Vector3.Cross(p[1] - p[0], p[2] - p[0]);
        if (n.y < 0f) Array.Reverse(p);
        Vector2 UV(Vector3 q) => new Vector2(Vector3.Dot(q, eave), Vector3.Dot(q, upSlope) / Mathf.Cos(a));
        if (p.Length == 4) b.QuadUV(dachowka, p[0], p[1], p[2], p[3], UV(p[0]), UV(p[1]), UV(p[2]), UV(p[3]));
        else b.TriangleUV(dachowka, p[0], p[1], p[2], UV(p[0]), UV(p[1]), UV(p[2]));
        // the collider: a 25 cm slab under the plane over its bounding trapezoid
        var nn = Vector3.Cross(p[1] - p[0], p[2] - p[0]).normalized; var c = Vector3.zero; foreach (var q in p) c += q; c /= p.Length;
        float lu = 0f, lv = 0f; foreach (var q in p) { lu = Mathf.Max(lu, Mathf.Abs(Vector3.Dot(q - c, eave))); lv = Mathf.Max(lv, Mathf.Abs(Vector3.Dot(q - c, Vector3.ProjectOnPlane(upSlope, Vector3.up).normalized))); }
        var rot = Quaternion.LookRotation(Vector3.Cross(eave, nn), nn);
        W.Kolizja(c - nn * .125f, new Vector3(lu * 2f, .25f, lv * 2f / Mathf.Cos(a)), rot);
    }

    // ---------------------------------------------------------------- round arches
    /// <summary>Fills a round arch's spandrels through the wall: over the opening a0..a1 (springing at ys, the crown at
    /// ys + r) up to yt on the wall's outer plane and its inner (T behind), and the arch's soffit between them.</summary>
    static void Luk(bool alongX, float plane, float a0, float a1, float ys, float yt, float outward, Material zew, Material wew, float grubosc = -1f)
    {
        float t = grubosc > 0f ? grubosc : T, r = (a1 - a0) * .5f, am = (a0 + a1) * .5f; const int N = 16;
        Vector3 P(float a, float y, float off) => alongX ? new Vector3(a, y, plane - outward * off) : new Vector3(plane - outward * off, y, a);
        var outN = alongX ? new Vector3(0, 0, outward) : new Vector3(outward, 0, 0);
        void Q(Material m, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 face, bool inside)
        {
            var q = new[] { p0, p1, p2, p3 };
            if (Vector3.Dot(Vector3.Cross(q[1] - q[0], q[2] - q[0]), face) < 0f) Array.Reverse(q);
            Out_(!inside); W.Quad(m, q[0], q[1], q[2], q[3]);
        }
        for (int i = 0; i < N; i++)
        {
            float t0 = Mathf.PI * i / N, t1 = Mathf.PI * (i + 1) / N;
            float u0 = am - r * Mathf.Cos(t0), u1 = am - r * Mathf.Cos(t1), y0 = ys + r * Mathf.Sin(t0), y1 = ys + r * Mathf.Sin(t1);
            Q(zew, P(u0, y0, 0f), P(u1, y1, 0f), P(u1, yt, 0f), P(u0, yt, 0f), outN, false);
            Q(wew, P(u0, y0, t), P(u1, y1, t), P(u1, yt, t), P(u0, yt, t), -outN, true);
            var c = P((u0 + u1) * .5f, (y0 + y1) * .5f, t * .5f); var centre = P(am, ys, t * .5f);
            // the soffit: 24.11 its inner half in the room's plaster (the old brick showed inside round the windows' arches)
            if (wew == zew) Q(zew, P(u0, y0, 0f), P(u1, y1, 0f), P(u1, y1, t), P(u0, y0, t), centre - c, false);
            else { Q(zew, P(u0, y0, 0f), P(u1, y1, 0f), P(u1, y1, t * .5f), P(u0, y0, t * .5f), centre - c, false); Q(wew, P(u0, y0, t * .5f), P(u1, y1, t * .5f), P(u1, y1, t), P(u0, y0, t), centre - c, true); }
        }
        // 18.11 an open arcade's arch (outside on both faces): the stone archivolt round it on the outer face
        if (wew == zew && zew != renderIn) { Out_(true); Pierscien(Opaska_, alongX, plane, outward, am, ys, r, r + OpW, 0f, Mathf.PI, OpD, 20); }
    }
    /// <summary>An arched opening in an outside wall (its wall built by the caller with the opening a0..a1 from sill to the
    /// crown): the spandrels, glass in the rectangle and the half-round (two-sided), a dark frame - or open (szklo false).</summary>
    static void OknoLuk(bool alongX, float plane, float a0, float a1, float sill, float ys, float yt, float outward, Material zew, bool szklo = true, float opW = OpW)
    {
        Luk(alongX, plane, a0, a1, ys, yt, outward, zew, renderIn);
        float r = (a1 - a0) * .5f, am = (a0 + a1) * .5f, gp = plane - outward * T * .5f;
        // 18.11 the stone surround round it: the jambs from the sill (a fanlight over a door: from the floor), the band
        // round the arch, the keystone at the crown; a window's stone sill
        bool fanlight = ys - sill < .1f;
        Opaska(alongX, plane, outward, a0, a1, fanlight ? 0f : sill, ys, opW);
        if (szklo && !fanlight && sill > .05f) Parapet(alongX, plane, outward, a0, a1, sill, opW);
        if (!szklo) return;
        // the glass and the walnut casement (was black aluminium - 18.11 his zoom: a thin black square)
        Out_(true);
        if (!fanlight)
        {
            if (alongX) MM(glass, a0 + .04f, a1 - .04f, sill + .05f, ys - .03f, gp - .012f, gp + .012f, false);
            else MM(glass, gp - .012f, gp + .012f, sill + .05f, ys - .03f, a0 + .04f, a1 - .04f, false);
            Skrzydla(alongX, gp, a0, a1, sill, ys);
        }
        const int N = 24;
        Vector3 P(float a, float y) => alongX ? new Vector3(a, y, gp) : new Vector3(gp, y, a);
        var centre = P(am, ys);
        for (int i = 0; i < N; i++)
        {
            float t0 = Mathf.PI * i / N, t1 = Mathf.PI * (i + 1) / N;
            var p0 = P(am - (r - .04f) * Mathf.Cos(t0), ys + (r - .04f) * Mathf.Sin(t0)); var p1 = P(am - (r - .04f) * Mathf.Cos(t1), ys + (r - .04f) * Mathf.Sin(t1));
            b.Triangle(glass, centre, p0, p1); b.Triangle(glass, centre, p1, p0);
            // the curved glazing bead: a walnut strip on the glass's edge (a surface, not chord boxes - 17.11 they showed teeth)
        }
        b.Box(walnut, P(am, ys), alongX ? new Vector3(a1 - a0, .06f, .08f) : new Vector3(.08f, .06f, a1 - a0), Quaternion.identity);     // the transom at the springing
        // the fanlight's glazing bars: three spokes from the transom's middle (45, 90, 135 degrees)
        foreach (float deg in new[] { 45f, 90f, 135f })
        {
            float th = deg * Mathf.Deg2Rad, len = r - .05f;
            var mid = P(am + Mathf.Cos(th) * len * .5f, ys + Mathf.Sin(th) * len * .5f);
            var rot = alongX ? Quaternion.AngleAxis(deg, Vector3.forward) : Quaternion.AngleAxis(-deg, Vector3.right);
            b.Box(walnut, mid, alongX ? new Vector3(len, .028f, .04f) : new Vector3(.04f, .028f, len), rot);
        }
        var nrm = alongX ? Vector3.forward : Vector3.right;
        b.Surface(walnut, (u, v) => { float t = Mathf.PI * u, rr = r - .05f * v; return P(am - rr * Mathf.Cos(t), ys + rr * Mathf.Sin(t)) - nrm * outward * .045f; }, 24, 1, outward > 0f);
        b.Surface(walnut, (u, v) => { float t = Mathf.PI * u, rr = r - .05f * v; return P(am - rr * Mathf.Cos(t), ys + rr * Mathf.Sin(t)) + nrm * outward * .045f; }, 24, 1, outward < 0f);
    }

    /// <summary>The lower half of a round opening (its wall built with the square a0..a1 x yb..ys): the wall under the
    /// half-circle down to yb, both faces and its soffit (Luk mirrored).</summary>
    static void LukDol(bool alongX, float plane, float a0, float a1, float ys, float yb, float outward, Material zew, Material wew)
    {
        float t = T, r = (a1 - a0) * .5f, am = (a0 + a1) * .5f; const int N = 16;
        Vector3 P(float a, float y, float off) => alongX ? new Vector3(a, y, plane - outward * off) : new Vector3(plane - outward * off, y, a);
        var outN = alongX ? new Vector3(0, 0, outward) : new Vector3(outward, 0, 0);
        void Q(Material m, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 face, bool inside)
        {
            var q = new[] { p0, p1, p2, p3 };
            if (Vector3.Dot(Vector3.Cross(q[1] - q[0], q[2] - q[0]), face) < 0f) Array.Reverse(q);
            Out_(!inside); W.Quad(m, q[0], q[1], q[2], q[3]);
        }
        for (int i = 0; i < N; i++)
        {
            float t0 = Mathf.PI * i / N, t1 = Mathf.PI * (i + 1) / N;
            float u0 = am - r * Mathf.Cos(t0), u1 = am - r * Mathf.Cos(t1), y0 = ys - r * Mathf.Sin(t0), y1 = ys - r * Mathf.Sin(t1);
            Q(zew, P(u0, y0, 0f), P(u1, y1, 0f), P(u1, yb, 0f), P(u0, yb, 0f), outN, false);
            Q(wew, P(u0, y0, t), P(u1, y1, t), P(u1, yb, t), P(u0, yb, t), -outN, true);
            var c = P((u0 + u1) * .5f, (y0 + y1) * .5f, t * .5f); var centre = P(am, ys, t * .5f);
            // the soffit: 24.11 its inner half in the room's plaster (the old brick showed inside round the windows' arches)
            if (wew == zew) Q(zew, P(u0, y0, 0f), P(u1, y1, 0f), P(u1, y1, t), P(u0, y0, t), centre - c, false);
            else { Q(zew, P(u0, y0, 0f), P(u1, y1, 0f), P(u1, y1, t * .5f), P(u0, y0, t * .5f), centre - c, false); Q(wew, P(u0, y0, t * .5f), P(u1, y1, t * .5f), P(u1, y1, t), P(u0, y0, t), centre - c, true); }
        }
    }
    /// <summary>A round window (owner's photo 2: the oculi) at (ac, yc), radius r, in an outside wall built with the square
    /// opening ac-r..ac+r x yc-r..yc+r: the wall round the circle, the glass disc, a walnut bead both sides.</summary>
    static void OknoOkragle(bool alongX, float plane, float ac, float yc, float r, float outward, Material zew, float opW = .1f)
    {
        Luk(alongX, plane, ac - r, ac + r, yc, yc + r, outward, zew, renderIn);
        LukDol(alongX, plane, ac - r, ac + r, yc, yc - r, outward, zew, renderIn);
        // 18.11 its stone ring (10 cm, as the oculi in his photo 2 sit close under the eaves)
        if (opW > 0f) { Out_(true); Pierscien(Opaska_, alongX, plane, outward, ac, yc, r, r + opW, 0f, Mathf.PI * 2f, OpD, 32); }
        float gp = plane - outward * T * .5f; const int N = 32;
        Vector3 P(float a, float y) => alongX ? new Vector3(a, y, gp) : new Vector3(gp, y, a);
        var centre = P(ac, yc);
        Out_(true);
        for (int i = 0; i < N; i++)
        {
            float t0 = Mathf.PI * 2f * i / N, t1 = Mathf.PI * 2f * (i + 1) / N;
            var p0 = P(ac - (r - .04f) * Mathf.Cos(t0), yc + (r - .04f) * Mathf.Sin(t0)); var p1 = P(ac - (r - .04f) * Mathf.Cos(t1), yc + (r - .04f) * Mathf.Sin(t1));
            b.Triangle(glass, centre, p0, p1); b.Triangle(glass, centre, p1, p0);
        }
        var nrm = alongX ? Vector3.forward : Vector3.right;
        b.Surface(walnut, (u, v) => { float t = Mathf.PI * 2f * u, rr = r - .05f * v; return P(ac - rr * Mathf.Cos(t), yc + rr * Mathf.Sin(t)) - nrm * outward * .045f; }, N, 1, outward > 0f);
        b.Surface(walnut, (u, v) => { float t = Mathf.PI * 2f * u, rr = r - .05f * v; return P(ac - rr * Mathf.Cos(t), yc + rr * Mathf.Sin(t)) + nrm * outward * .045f; }, N, 1, outward < 0f);
    }

    // ---------------------------------------------------------------- the finish round the openings (owner 18.11 04:40-04:46:
    // "Co to za wykończenie jak to wygląda jak zabawki", "elewacja wykończenie premium" - his zoom: bare holes in the
    // stucco with a thin black square in them). As his photo 2 and the classic Mediterranean villa: a stone surround 16 cm
    // wide, 4.5 cm proud of the stucco, round every arched opening, a keystone at the crown, a stone sill 8 cm proud under
    // every window, walnut casements in two leaves with glazing bars, the fanlight's spokes. The stone: the terrace's
    // travertine (Poly Haven), the warm beige villa's surrounds and the ivory villa's alike.
    const float OpW = .16f, OpD = .045f;
    static Material Opaska_ => deck;
    /// <summary>A quad turned to face 'face' (its winding fixed by the normal, as Luk's).</summary>
    static void QF(Material m, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 face)
    {
        var q = new[] { p0, p1, p2, p3 };
        if (Vector3.Dot(Vector3.Cross(q[1] - q[0], q[2] - q[0]), face) < 0f) Array.Reverse(q);
        b.Quad(m, q[0], q[1], q[2], q[3]);
    }
    /// <summary>A box in a wall's frame: a along the wall, y up, off across it (positive into the wall from its outer
    /// plane, negative proud of it).</summary>
    static void WB(Material m, bool alongX, float plane, float outward, float a0, float a1, float y0, float y1, float off0, float off1)
    {
        float n0 = plane - outward * off0, n1 = plane - outward * off1;
        if (alongX) MM(m, a0, a1, y0, y1, Mathf.Min(n0, n1), Mathf.Max(n0, n1), false);
        else MM(m, Mathf.Min(n0, n1), Mathf.Max(n0, n1), y0, y1, a0, a1, false);
    }
    /// <summary>An annulus sector (radii r0..r1, angles t0..t1 from -a over the top) round (ac, yc) on a wall: its face d
    /// proud of the wall and both rims back to the wall.</summary>
    static void Pierscien(Material m, bool alongX, float plane, float outward, float ac, float yc, float r0, float r1, float t0, float t1, float d, int N)
    {
        Vector3 P(float a, float y, float off) => alongX ? new Vector3(a, y, plane - outward * off) : new Vector3(plane - outward * off, y, a);
        Vector3 A(float rr, float u, float off) => P(ac - rr * Mathf.Cos(u), yc + rr * Mathf.Sin(u), off);
        var outN = alongX ? new Vector3(0, 0, outward) : new Vector3(outward, 0, 0);
        for (int i = 0; i < N; i++)
        {
            float u0 = Mathf.Lerp(t0, t1, (float)i / N), u1 = Mathf.Lerp(t0, t1, (float)(i + 1) / N), um = (u0 + u1) * .5f;
            QF(m, A(r0, u0, -d), A(r0, u1, -d), A(r1, u1, -d), A(r1, u0, -d), outN);
            var radial = A(1f, um, 0f) - P(ac, yc, 0f);
            QF(m, A(r1, u0, -d), A(r1, u1, -d), A(r1, u1, .005f), A(r1, u0, .005f), radial);
            QF(m, A(r0, u0, -d), A(r0, u1, -d), A(r0, u1, 0f), A(r0, u0, 0f), -radial);      // the opening's soffit goes on from 0
        }
    }
    /// <summary>The stone surround of an arched opening a0..a1 (springing ys): the jambs from yb, the band round the arch,
    /// the keystone (on the full 16 cm surround).</summary>
    static void Opaska(bool alongX, float plane, float outward, float a0, float a1, float yb, float ys, float w)
    {
        if (w <= 0f) return;
        Out_(true);
        float r = (a1 - a0) * .5f, am = (a0 + a1) * .5f;
        WB(Opaska_, alongX, plane, outward, a0 - w, a0, yb, ys, 0f, -OpD);
        WB(Opaska_, alongX, plane, outward, a1, a1 + w, yb, ys, 0f, -OpD);
        Pierscien(Opaska_, alongX, plane, outward, am, ys, r, r + w, 0f, Mathf.PI, OpD, 20);
        if (w >= OpW) WB(Opaska_, alongX, plane, outward, am - .11f, am + .11f, ys + r, ys + r + w + .08f, 0f, -(OpD + .025f));
    }
    /// <summary>A window's stone sill: from the glass out 8 cm proud of the stucco, 5 cm past the surround each side.</summary>
    static void Parapet(bool alongX, float plane, float outward, float a0, float a1, float sill, float w)
    {
        Out_(true);
        float e = Mathf.Max(w, 0f) + .05f;
        WB(Opaska_, alongX, plane, outward, a0 - e, a1 + e, sill - .07f, sill + .01f, T * .5f - .012f, -(OpD + .035f));     // to the glass, clear of the wall's two skins' joint
    }
    /// <summary>The walnut casement in the rectangle a0..a1 x y0..y1 at the glass plane gp: the frame, the meeting
    /// stile of two leaves (over 85 cm), glazing bars dividing each leaf into panes about half a metre tall.</summary>
    static void Skrzydla(bool alongX, float gp, float a0, float a1, float y0, float y1)
    {
        Out_(true);
        void R(float p0, float p1, float q0, float q1, float d)
        {
            if (alongX) MM(walnut, p0, p1, q0, q1, gp - d, gp + d, false);
            else MM(walnut, gp - d, gp + d, q0, q1, p0, p1, false);
        }
        float am = (a0 + a1) * .5f;
        R(a0, a0 + .07f, y0, y1, .035f); R(a1 - .07f, a1, y0, y1, .035f);
        R(a0 + .07f, a1 - .07f, y0, y0 + .11f, .035f);
        if (a1 - a0 > .85f) R(am - .045f, am + .045f, y0 + .11f, y1, .035f);
        int n = Mathf.Max(1, Mathf.RoundToInt((y1 - y0 - .11f) / .5f));
        for (int k = 1; k < n; k++)
        {
            float y = y0 + .11f + k * (y1 - y0 - .11f) / n;
            R(a0 + .07f, a1 - .07f, y - .015f, y + .015f, .022f);
        }
    }

    // ---------------------------------------------------------------- stone
    /// <summary>A Tuscan column at p (its base on p.y): plinth, base ring, the tapering shaft, the capital and its abacus.</summary>
    static void Kolumna(Vector3 p, float h, float r)
    {
        Out_(true);
        b.Box(deck, p + Vector3.up * .12f, new Vector3(r * 2.6f, .24f, r * 2.6f), Quaternion.identity);
        b.Cylinder(deck, p + Vector3.up * .24f, r * 1.22f, .1f, 24, Quaternion.identity, true);
        b.Cylinder(tynkKrem, p + Vector3.up * .34f, r, h - .34f - .3f, 24, Quaternion.identity, false, false, r * .86f);
        b.Cylinder(deck, p + Vector3.up * (h - .3f), r * .92f, .14f, 24, Quaternion.identity, true, false, r * 1.2f);
        b.Box(deck, p + Vector3.up * (h - .08f), new Vector3(r * 2.7f, .16f, r * 2.7f), Quaternion.identity);
        W.Kolizja(p + Vector3.up * (h * .5f), new Vector3(r * 2.2f, h, r * 2.2f), Quaternion.identity);
    }
    /// <summary>A stone baluster balustrade along x (or z) on the plane from a0 to a1 at the floor y: plinth, vase balusters
    /// every .3 m, the handrail; its collider.</summary>
    static void Tralki(bool alongX, float plane, float a0, float a1, float y)
    {
        Out_(true);
        Vector3 P(float a, float yy) => alongX ? new Vector3(a, yy, plane) : new Vector3(plane, yy, a);
        Vector3 S(float len, float hh, float d) => alongX ? new Vector3(len, hh, d) : new Vector3(d, hh, len);
        float am = (a0 + a1) * .5f, len = a1 - a0;
        b.Box(deck, P(am, y + .09f), S(len, .18f, .26f), Quaternion.identity);
        b.Box(deck, P(am, y + .98f), S(len + .04f, .12f, .3f), Quaternion.identity);
        var prof = new[] { new Vector2(0f, 0f), new Vector2(.06f, 0f), new Vector2(.06f, .05f), new Vector2(.045f, .08f), new Vector2(.085f, .3f),
                           new Vector2(.05f, .5f), new Vector2(.04f, .58f), new Vector2(.065f, .66f), new Vector2(.06f, .74f), new Vector2(0f, .74f) };
        int n = Mathf.Max(1, Mathf.RoundToInt(len / .3f));
        for (int i = 0; i < n; i++) b.Lathe(deck, P(a0 + (i + .5f) * len / n, y + .18f), prof, 10, Quaternion.identity, false, false);
        W.Kolizja(P(am, y + .52f), S(len, 1.04f, .3f), Quaternion.identity);
    }
    /// <summary>A stuccoed chimney from its foot (inside the roof) to the top at p (x, z) with a little hipped tile cap.</summary>
    static void Komin(float x, float z, float foot, float top, float w = .9f, float d = .7f)
    {
        Out_(true);
        MMd(nrWilli == 3 && cegla != null ? cegla : tynkKrem, x - w * .5f, x + w * .5f, foot, top, z - d * .5f, z + d * .5f, false);   // 24.11 the seafront villa's in old brick
        MMd(deck, x - w * .5f - .06f, x + w * .5f + .06f, top, top + .1f, z - d * .5f - .06f, z + d * .5f + .06f, false);
        var c = new Vector3(x, top + .1f, z);
        b.Cylinder(dachowka, c, Mathf.Max(w, d) * .62f, .32f, 4, Quaternion.Euler(0f, 45f, 0f), true, false, .02f);
    }

    // ---------------------------------------------------------------- the free-form pool
    /// <summary>A free-form pool (a convex outline in xz, counter-clockwise seen from above): the tiled shell (walls,
    /// floor), the water and its body, the travertine coping .45 wide and the terrace round it to the box rect (x0,x1,z0,z1)
    /// as radial strips (each with its collider), the swim zone; steps at the outline's first point.</summary>
    static void BasenKsztalt(string id, Vector2[] obrys, Vector4 rect)
    {
        Out_(true);
        int n = obrys.Length; var c = Vector2.zero; foreach (var p in obrys) c += p; c /= n;
        float pf = PoolFloor, top = 0f, cop = .45f;
        Vector3 V(Vector2 p, float y) => new Vector3(p.x, y, p.y);
        Vector2 Out(Vector2 p, float d) => p + (p - c).normalized * d;
        // the point on the rect's border along the ray from the centre through p
        Vector2 Brzeg(Vector2 p)
        {
            var d = (p - c).normalized; float t = float.MaxValue;
            if (d.x > 1e-4f) t = Mathf.Min(t, (rect.y - c.x) / d.x); if (d.x < -1e-4f) t = Mathf.Min(t, (rect.x - c.x) / d.x);
            if (d.y > 1e-4f) t = Mathf.Min(t, (rect.w - c.y) / d.y); if (d.y < -1e-4f) t = Mathf.Min(t, (rect.z - c.y) / d.y);
            return c + d * t;
        }
        // every face turned toward face: a quad or a triangle in batch m
        void Q4(BraxChampionsMeshBatch m, Material mat, Vector3 a, Vector3 bb, Vector3 cc, Vector3 d, Vector3 face)
        { if (Vector3.Dot(Vector3.Cross(bb - a, cc - a), face) < 0f) m.Quad(mat, d, cc, bb, a); else m.Quad(mat, a, bb, cc, d); }
        void T3(BraxChampionsMeshBatch m, Material mat, Vector3 a, Vector3 bb, Vector3 cc, Vector3 face)
        { if (Vector3.Dot(Vector3.Cross(bb - a, cc - a), face) < 0f) m.Triangle(mat, a, cc, bb); else m.Triangle(mat, a, bb, cc); }
        for (int i = 0; i < n; i++)
        {
            Vector2 p0 = obrys[i], p1 = obrys[(i + 1) % n];
            var inward = V(c - (p0 + p1) * .5f, 0f); inward.y = 0f;
            // the wall (facing the water) from the floor to the coping, the coping's lip
            Out_(true);
            Q4(b, poolTile, V(p1, pf), V(p0, pf), V(p0, top - .05f), V(p1, top - .05f), inward);
            Q4(b, deck, V(p1, top - .05f), V(p0, top - .05f), V(p0, top), V(p1, top), inward);
            var wc = (p0 + p1) * .5f; var wd = p1 - p0; float wl = wd.magnitude;
            var along = new Vector3(wd.x, 0, wd.y);
            W.Kolizja(V(Out(wc, .15f), (pf + top) * .5f), new Vector3(.3f, top - pf, wl + .05f), Quaternion.LookRotation(along, Vector3.up));
            // the floor (a fan) and the water
            T3(b, poolTile, V(c, pf), V(p1, pf), V(p0, pf), Vector3.up);
            T3(bw, waterMat, V(c, Water), V(p1, Water), V(p0, Water), Vector3.up);
            // the coping, then the terrace to the rect (each strip its collider)
            Vector2 q0 = Out(p0, cop), q1 = Out(p1, cop), r0 = Brzeg(p0), r1 = Brzeg(p1);
            Q4(b, deck, V(p0, top), V(q0, top), V(q1, top), V(p1, top), Vector3.up);
            Q4(b, deck, V(q0, top), V(r0, top), V(r1, top), V(q1, top), Vector3.up);
            var tc = (p0 + p1 + r0 + r1) * .25f; float tl = Mathf.Max(wl, (r1 - r0).magnitude), tw = ((r0 + r1) * .5f - (p0 + p1) * .5f).magnitude;
            W.Kolizja(V(tc, top - .15f), new Vector3(tl + .1f, .3f, tw + .1f), Quaternion.LookRotation(-inward.normalized, Vector3.up));
        }
        // the floor's collider (the outline's bounding box under the floor) and the swim zone
        float bx0 = float.MaxValue, bx1 = float.MinValue, bz0 = float.MaxValue, bz1 = float.MinValue;
        foreach (var p in obrys) { bx0 = Mathf.Min(bx0, p.x); bx1 = Mathf.Max(bx1, p.x); bz0 = Mathf.Min(bz0, p.y); bz1 = Mathf.Max(bz1, p.y); }
        W.Kolizja(new Vector3((bx0 + bx1) * .5f, pf - .15f, (bz0 + bz1) * .5f), new Vector3(bx1 - bx0, .3f, bz1 - bz0), Quaternion.identity);
        var surf = new GameObject(id + " | lustro wody").transform; surf.SetParent(g, false); surf.localPosition = V(c, Water);
        var zone = new GameObject(id + " | strefa pływania").AddComponent<BoxCollider>(); zone.transform.SetParent(g, false);
        zone.transform.localPosition = V(c, (pf + .05f + Water + .35f) * .5f);
        zone.size = new Vector3((bx1 - bx0) * .7f, Water + .35f - pf - .05f, (bz1 - bz0) * .7f); zone.isTrigger = true;
        var sw = zone.gameObject.AddComponent<BraxChampionsPoolSwimVolume>(); sw.waterSurface = surf; sw.basinId = id;
        // the steps in (four stone treads) at the first point, a rail
        var s0 = obrys[0]; var sd = (c - s0).normalized; var sq = Quaternion.LookRotation(new Vector3(sd.x, 0, sd.y), Vector3.up);
        for (int i = 0; i < 4; i++)
        {
            var sp = s0 + sd * (.25f + i * .45f);
            float tt = -.32f - i * .32f;
            b.Box(poolTile, V(sp, (pf + tt) * .5f), new Vector3(2.0f, tt - pf, .45f), sq);
            W.Kolizja(V(sp, (pf + tt) * .5f), new Vector3(2.0f, tt - pf, .45f), sq);
        }
        b.Cylinder(BraxKatalog.Led, V(c, pf + .002f), .12f, .012f, 16, Quaternion.identity, true);
    }

    // ---------------------------------------------------------------- the courtyard fountain
    static void Fontanna(Vector3 p, float r)
    {
        Out_(true);
        b.Lathe(deck, p, new[] { new Vector2(0f, 0f), new Vector2(r, 0f), new Vector2(r, .55f), new Vector2(r - .25f, .55f), new Vector2(r - .25f, .1f), new Vector2(0f, .1f) }, 40, Quaternion.identity, false, false);
        bw.Surface(waterMat, (u, v) => p + new Vector3(Mathf.Cos(u * Mathf.PI * 2f) * (r - .26f) * v, .42f, Mathf.Sin(u * Mathf.PI * 2f) * (r - .26f) * v), 40, 1, true);
        b.Lathe(deck, p + Vector3.up * .1f, new[] { new Vector2(0f, 0f), new Vector2(.28f, 0f), new Vector2(.2f, .3f), new Vector2(.16f, 1.1f), new Vector2(.75f, 1.2f), new Vector2(.8f, 1.32f), new Vector2(.12f, 1.36f), new Vector2(.1f, 1.8f), new Vector2(.22f, 1.86f), new Vector2(0f, 1.95f) }, 28, Quaternion.identity, false, false);
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            W.Kolizja(p + new Vector3(Mathf.Cos(a) * (r - .12f), .28f, Mathf.Sin(a) * (r - .12f)), new Vector3(.25f, .56f, r * .55f), Quaternion.Euler(0f, -i * 30f, 0f));
        }
        W.Kolizja(p + Vector3.up * .9f, new Vector3(.6f, 1.8f, .6f), Quaternion.identity);
    }

    // ---------------------------------------------------------------- a spiral stair (a tower's)
    /// <summary>A spiral stair round a stone newel at c from y0 to y1 in n treads turning sweep degrees from start (deg),
    /// radius r: wedge treads, each its own collider (a step), a black rail on the outside.</summary>
    static void SchodySpiralne(Vector3 c, float y0, float y1, int n, float r, float start, float sweep)
    {
        Out_(false);
        b.Cylinder(deck, new Vector3(c.x, y0, c.z), .14f, y1 - y0 + 1f, 16, Quaternion.identity, true);
        float rise = (y1 - y0) / n, step = sweep / n;
        for (int i = 0; i < n; i++)
        {
            float a = (start + (i + .5f) * step) * Mathf.Deg2Rad, top = y0 + (i + 1) * rise;
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            var q = Quaternion.LookRotation(dir, Vector3.up);
            float w = 2f * r * Mathf.Sin(Mathf.Abs(step) * Mathf.Deg2Rad * .5f) + .06f;
            b.Box(floorGf, new Vector3(c.x, top - .03f, c.z) + dir * (r * .55f), new Vector3(w, .06f, r * .9f), q);
            W.Kolizja(new Vector3(c.x, top - .1f, c.z) + dir * (r * .55f), new Vector3(w + .05f, .2f, r * .9f), q);
            var pr = new Vector3(c.x, top + .95f, c.z) + dir * r;
            b.Cylinder(black, new Vector3(pr.x, top, pr.z), .015f, .95f, 6, Quaternion.identity, false);
            if (i > 0)
            {
                float ap = (start + (i - .5f) * step) * Mathf.Deg2Rad;
                b.Tube(black, new Vector3(c.x + Mathf.Cos(ap) * r, top - rise + .95f, c.z + Mathf.Sin(ap) * r), pr, .02f, 6);
            }
        }
    }
}
