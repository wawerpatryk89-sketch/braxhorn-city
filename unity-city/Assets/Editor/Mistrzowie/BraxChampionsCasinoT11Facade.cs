using UnityEngine;
using W = BraxWnetrzaKit;

// T11 facade per the OWNER BOARD 9.11 "BRAXHORN CITY – KASYNO (T11)":
// cream limestone blocks with tall two-storey glazing between piers, a cream
// attic with warm gold LED lines, dark graphite plinth and pylons with gold
// vertical LED, glass rotunda with a chandelier lobby behind it.
// Replaces the 7-8.11 "masonry, no windows, all pillars gold" variant written
// by the previous executor (it contradicted this board).
// Nominal metres, K applied once by B()/BeveledBox() or by the K-scaled group.
public static partial class BraxChampionsCasinoT11
{
    // Owner 10.11 00:33: "A gdzie jest ten dolar złoty, co był taki duży na kasynie u góry? Czemu go nie ma teraz?" - back on the roof.
    const bool RoofDollarOnOwnerBoard = true;
    // Elevation levels (nominal metres).
    const float YPlinth = .80f, YSpanBottom = 6.60f, YSpanTop = 7.60f, YHead = 12.0f, YAttic = 14.0f;
    const float PierW = 1.4f, CornerPierW = 2.0f, PierDepth = .62f, GlassRecess = .44f;

    // One glazed two-storey bay on a facade. 'along' = facade direction,
    // 'outward' = facade normal; c0 = coordinate along the facade where the
    // bay starts, w = bay width; facePlane = outer pier face coordinate.
    static void GlazedBay(string side, float c0, float w, bool alongX, float facePlane, float outward)
    {
        float c = c0 + w * .5f;
        float glass = facePlane - outward * GlassRecess;
        Vector3 At(float along, float y, float normal) => alongX ? new Vector3(along, y, normal) : new Vector3(normal, y, along);
        Vector3 Size(float along, float h, float depth) => alongX ? new Vector3(along, h, depth) : new Vector3(depth, h, along);
        void Box(Transform parent, string name, Material m, float along, float y, float normal, Vector3 size, bool solid)
        {
            var p = At(along, y, normal);
            B(parent, name, m, p.x, p.y, p.z, size.x, size.y, size.z, solid);
        }
        // Glass (solid: the podium interior is not walkable), lower and upper storey.
        Box(shell, "T11 " + side + " tall window glass lower", frontGlass, c, (YPlinth + YSpanBottom) * .5f, glass, Size(w, YSpanBottom - YPlinth, .06f), true);
        Box(shell, "T11 " + side + " tall window glass upper", frontGlass, c, (YSpanTop + YHead) * .5f, glass, Size(w, YHead - YSpanTop, .06f), true);
        // Graphite storey spandrel with a champagne line.
        Box(shell, "T11 " + side + " graphite storey spandrel", graphite, c, (YSpanBottom + YSpanTop) * .5f, glass + outward * .07f, Size(w, YSpanTop - YSpanBottom, .20f), false);
        Box(trim, "T11 " + side + " spandrel champagne line", gold, c, (YSpanBottom + YSpanTop) * .5f, glass + outward * .185f, Size(w, .06f, .03f), false);
        // Black frames: 4 verticals (3 lights per storey) + sill/heads.
        for (int i = 0; i <= 3; i++)
        {
            float a = i == 0 ? c0 + .05f : i == 3 ? c0 + w - .05f : c0 + w * i / 3f;
            Box(trim, "T11 " + side + " black window mullion", windowFrame, a, (YPlinth + YHead) * .5f, glass + outward * .02f, Size(.10f, YHead - YPlinth, .14f), false);
        }
        foreach (float y in new[] { YPlinth + .05f, YSpanBottom, YSpanTop, YHead - .05f })
            Box(trim, "T11 " + side + " black window transom", windowFrame, c, y, glass + outward * .02f, Size(w, .10f, .14f), false);
        // 10.11: no board behind the glass any more. Every window looks into the real casino hall
        // (BraxChampionsCasinoT11.CasinoHall); two warm light lines stay at the window heads.
        foreach (float y in new[] { YSpanBottom - .15f, YHead - .15f })
            Box(trim, "T11 " + side + " interior warm ceiling light", led, c, y, glass - outward * .55f, Size(w - .12f, .05f, .50f), false);
        // Ivory curtains drawn to both sides of each storey (lit windows of a palace, not empty glowing boards).
        foreach (var (y0, y1) in new[] { (YPlinth + .10f, YSpanBottom - .22f), (YSpanTop + .05f, YHead - .22f) })
            foreach (float e in new[] { -1f, 1f })
                Box(trim, "T11 " + side + " ivory window curtain", curtain, c + e * (w * .5f - .50f), (y0 + y1) * .5f, glass - outward * .42f, Size(.86f, y1 - y0, .08f), false);
    }

    static void CreamPier(string name, float along, float w, bool alongX, float facePlane, float outward, float y0, float y1)
    {
        float n = facePlane - outward * PierDepth * .5f;
        var p = alongX ? new Vector3(along, (y0 + y1) * .5f, n) : new Vector3(n, (y0 + y1) * .5f, along);
        var s = alongX ? new Vector3(w, y1 - y0, PierDepth) : new Vector3(PierDepth, y1 - y0, w);
        B(shell, name, cream, p.x, p.y, p.z, s.x, s.y, s.z, true, true);
    }

    static void ReferenceFacades()
    {
        foreach (float s in new[] { -1f, 1f })
        {
            string wing = s < 0 ? "west" : "east";
            // ---- FRONT WINGS (z=-28.30 pier face): |x| 19.625 .. 38 ----
            const float frontFace = -28.30f, x0 = 19.625f, x1 = 38f;
            float bayW = (x1 - CornerPierW - x0 - 2 * PierW) / 3f; // 4.525 m
            B(shell, "T11 front graphite plinth " + wing, graphite, s * (x0 + x1) * .5f, YPlinth * .5f, frontFace + .15f, x1 - x0, YPlinth, .30f, true, true);
            // Corner pier runs to the side face (38.30) and stops under the attic: no coplanar overlaps.
            CreamPier("T11 front cream limestone corner pier " + wing, s * (38.30f - (CornerPierW + .30f) * .5f), CornerPierW + .30f, true, frontFace, -1f, YPlinth, YHead);
            B(shell, "T11 front cream limestone attic " + wing, cream, s * (x0 + 38.30f) * .5f, (YHead + YAttic) * .5f, frontFace + PierDepth * .5f, 38.30f - x0, YAttic - YHead, PierDepth, true, true);
            for (int i = 0; i < 3; i++)
            {
                float b0 = x0 + i * (bayW + PierW);
                float lo = s > 0 ? b0 : -(b0 + bayW);
                GlazedBay("front " + wing, lo, bayW, true, frontFace, -1f);
                if (i < 2) CreamPier("T11 front cream limestone pier " + wing, s * (b0 + bayW + PierW * .5f), PierW, true, frontFace, -1f, YPlinth, YHead);
            }
            BeveledBox(trim, "T11 front rounded champagne cornice " + wing, gold, s * (x0 + 38.41f) * .5f, YAttic - .03f, frontFace - .05f, 38.41f - x0, .10f, .12f, .03f, false);
            B(trim, "T11 front warm gold LED line under attic " + wing, led, s * (x0 + x1) * .5f, YHead + .06f, frontFace - .03f, x1 - x0 - .2f, .045f, .045f, false);
            B(trim, "T11 front warm gold LED line on cornice " + wing, led, s * (x0 + x1) * .5f, YAttic + .05f, frontFace - .03f, x1 - x0 - .2f, .045f, .045f, false);

            // ---- SIDES (x=+-38.30 pier face): z -28 .. 28, 8 tall windows ----
            float sideFace = s * 38.30f;
            const float z0 = -28.30f, z1 = 28.30f;
            float sideBay = (z1 - z0 - 2 * CornerPierW - 7 * PierW) / 8f; // ~5.35 m
            B(shell, "T11 side graphite plinth " + wing, graphite, sideFace - s * .15f, YPlinth * .5f, 0, .30f, YPlinth, z1 - z0, true, true);
            // Front corner: the front corner pier already covers z -28.30..-27.68.
            CreamPier("T11 side cream limestone corner pier front " + wing, (-27.68f - 26.30f) * .5f, 1.38f, false, sideFace, s, YPlinth, YHead);
            CreamPier("T11 side cream limestone corner pier rear " + wing, z1 - CornerPierW * .5f, CornerPierW, false, sideFace, s, YPlinth, YHead);
            // Side attic starts behind the front attic (-27.68) and runs to the rear corner (28.30).
            B(shell, "T11 side cream limestone attic " + wing, cream, sideFace - s * PierDepth * .5f, (YHead + YAttic) * .5f, (-27.68f + z1) * .5f, PierDepth, YAttic - YHead, z1 + 27.68f, true, true);
            for (int j = 0; j < 8; j++)
            {
                float b0 = z0 + CornerPierW + j * (sideBay + PierW);
                GlazedBay("side " + wing, b0, sideBay, false, sideFace, s);
                if (j < 7) CreamPier("T11 side cream limestone pier " + wing, b0 + sideBay + PierW * .5f, PierW, false, sideFace, s, YPlinth, YHead);
            }
            BeveledBox(trim, "T11 side rounded champagne cornice " + wing, gold, sideFace + s * .05f, YAttic - .03f, 0, .12f, .10f, z1 - z0, .03f, false);
            B(trim, "T11 side warm gold LED line under attic " + wing, led, sideFace + s * .03f, YHead + .06f, 0, .045f, .045f, z1 - z0 - .2f, false);
            B(trim, "T11 side warm gold LED line on cornice " + wing, led, sideFace + s * .03f, YAttic + .05f, 0, .045f, .045f, z1 - z0 - .2f, false);

            // ---- Dark graphite pylons flanking the rotunda, gold vertical LED ----
            BeveledBox(shell, "T11 graphite entrance pylon " + wing, graphite, s * 18.25f, 7.90f, -28f, 2.75f, 15.80f, 2.35f, .10f, true);
            foreach (float dx in new[] { -.55f, .55f })
                B(trim, "T11 pylon warm gold vertical LED " + wing, led, s * 18.25f + dx, 7.90f, -29.20f, .05f, 14.6f, .04f, false);
            BeveledBox(trim, "T11 pylon champagne cap " + wing, gold, s * 18.25f, 15.86f, -28f, 2.85f, .12f, 2.45f, .03f, false);
        }

        // ---- REAR (z=+28): service side, cream limestone, 6 m delivery opening at x -23..-17 ----
        B(shell, "T11 rear cream limestone wall LEFT of 6m delivery opening", cream, -30.5f, YAttic * .5f, 27.84f, 15f, YAttic, .32f, true, true);
        B(shell, "T11 rear cream limestone wall RIGHT of 6m delivery opening", cream, 10.5f, YAttic * .5f, 27.84f, 55f, YAttic, .32f, true, true);
        B(shell, "T11 rear cream limestone lintel above 6m delivery opening", cream, -20f, 9.775f, 27.84f, 6f, 8.45f, .32f, true, true);
        B(trim, "T11 rear graphite plinth LEFT", graphite, -30.5f, YPlinth * .5f, 28.0f, 15f, YPlinth, .30f, false, true);
        B(trim, "T11 rear graphite plinth RIGHT", graphite, 10.5f, YPlinth * .5f, 28.0f, 55f, YPlinth, .30f, false, true);
        foreach (float xx in new[] { -36f, -25f, -14f, -3f, 8f, 19f, 30f, 36f })
            B(shell, "T11 rear cream limestone pilaster", cream, xx, (YPlinth + YAttic) * .5f, 28.15f, .90f, YAttic - YPlinth, .30f, false, true);
        BeveledBox(trim, "T11 rear rounded champagne cornice", gold, 0, YAttic - .03f, 28.32f, 76f, .10f, .10f, .03f, false);
        B(trim, "T11 rear warm gold LED line", led, 0, YAttic + .05f, 28.33f, 75.6f, .045f, .045f, false);
        foreach (float xx in new[] { -32f, -8f, 17f, 32f })
            B(trim, "T11 rear warm architectural light", led, xx, 5.6f, 28.18f, .045f, 2.2f, .045f, false);
    }

    // ---- Lobby behind the rotunda glass (owner board: chandelier + violet glow) ----
    static void LobbyTri(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 facing)
    {
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), facing) < 0) { var t = b; b = c; c = t; }
        W.Quad(m, a, b, c, c);
    }

    static void RotundaLobby()
    {
        // Built in nominal metres at the world origin (W writes world-space
        // vertices), then hung under the shell and scaled by K once.
        var g = new GameObject("T11 rotunda lobby | chandelier, violet glow, gallery").transform;
        g.position = Vector3.zero; g.rotation = Quaternion.identity; g.localScale = Vector3.one;
        W.Start(g);
        try
        {
            const int n = 48;
            Vector3 P(float t, float y, float shrink) => new Vector3(17f * shrink * Mathf.Sin(t), y, -28f - 8f * shrink * Mathf.Cos(t));
            float T(int i) => -Mathf.PI * .5f + Mathf.PI * i / n;
            // Floor (granite) and ceiling (champagne) over the half-ellipse + the 4 m bay behind it.
            var floorC = new Vector3(0, .215f, -28f); var ceilC = new Vector3(0, 10.78f, -28f);
            for (int i = 0; i < n; i++)
            {
                LobbyTri(granite, floorC, P(T(i), .215f, .985f), P(T(i + 1), .215f, .985f), Vector3.up);
                LobbyTri(gold, ceilC, P(T(i), 10.78f, .985f), P(T(i + 1), 10.78f, .985f), Vector3.down);
            }
            W.MM(granite, -16.75f, 16.75f, .20f, .215f, -28f, -24.2f);
            W.MM(gold, -16.75f, 16.75f, 10.78f, 10.80f, -28f, -24.2f);
            // Warm LED cove ring around the chandelier opening and along the glass.
            for (int i = 0; i < 32; i++)
            {
                float a0 = 2 * Mathf.PI * i / 32f, a1 = 2 * Mathf.PI * (i + 1) / 32f;
                W.Rura(led, new Vector3(4.2f * Mathf.Cos(a0), 10.74f, -31f + 4.2f * Mathf.Sin(a0)),
                    new Vector3(4.2f * Mathf.Cos(a1), 10.74f, -31f + 4.2f * Mathf.Sin(a1)), .035f, 6, false);
            }
            // Back wall (graphite) with violet light strips and a gold portal to the gaming floor.
            // 10.11: the back wall is OPEN: a real 6 x 4.5 m portal into the casino hall (was a painted board).
            W.MM(graphite, -16.875f, -PortalHalf, .20f, 13.9f, -24.2f, -24.0f, true);
            W.MM(graphite, PortalHalf, 16.875f, .20f, 13.9f, -24.2f, -24.0f, true);
            W.MM(graphite, -PortalHalf, PortalHalf, PortalTop, 13.9f, -24.2f, -24.0f, true);
            foreach (float x in new[] { -13.5f, -8.5f, -3.9f, 3.9f, 8.5f, 13.5f })
                W.MM(violet, x - .06f, x + .06f, 1.0f, 10.4f, -24.24f, -24.2f);
            W.MM(violet, -2.6f, 2.6f, 4.80f, 4.90f, -24.25f, -24.2f);
            foreach (float x in new[] { -3.08f, 3.08f }) W.MM(gold, x - .08f, x + .08f, .215f, 4.66f, -24.30f, -24.2f);
            W.MM(gold, -3.16f, 3.16f, 4.5f, 4.66f, -24.30f, -24.2f);
            // Side returns closing the lobby against the empty podium.
            foreach (float s in new[] { -1f, 1f })
                W.MM(graphite, s * 16.875f - .1f, s * 16.875f + .1f, .20f, 13.9f, -26.82f, -24.0f, true);
            // Gallery (mezzanine) along the back wall: granite slab, LED edge, glass + gold rail.
            W.MM(granite, -16.75f, 16.75f, 6.45f, 6.65f, -26.4f, -24.2f);
            W.MM(led, -16.7f, 16.7f, 6.42f, 6.45f, -26.38f, -26.3f);
            W.MM(glass, -16.7f, 16.7f, 6.65f, 7.70f, -26.38f, -26.36f);
            W.Rura(gold, new Vector3(-16.7f, 7.72f, -26.37f), new Vector3(16.7f, 7.72f, -26.37f), .035f, 8);
            // Chandelier: three cascading gold rings with warm crystal drops.
            W.Rura(gold, new Vector3(0, 10.78f, -31f), new Vector3(0, 7.7f, -31f), .05f, 8);
            float[] radius = { 2.6f, 1.9f, 1.2f }, height = { 9.6f, 8.9f, 8.2f };
            for (int r = 0; r < 3; r++)
            {
                for (int i = 0; i < 36; i++)
                {
                    float a0 = 2 * Mathf.PI * i / 36f, a1 = 2 * Mathf.PI * (i + 1) / 36f;
                    var p0 = new Vector3(radius[r] * Mathf.Cos(a0), height[r], -31f + radius[r] * Mathf.Sin(a0));
                    var p1 = new Vector3(radius[r] * Mathf.Cos(a1), height[r], -31f + radius[r] * Mathf.Sin(a1));
                    W.Rura(gold, p0, p1, .045f, 6, false);
                    if (i % 2 == 0) W.Box(led, p0 + Vector3.down * .30f, new Vector3(.05f, .50f, .05f));
                }
                for (int k = 0; k < 4; k++)
                {
                    float a = Mathf.PI * .5f * k + .4f;
                    W.Rura(gold, new Vector3(0, 10.78f, -31f), new Vector3(radius[r] * Mathf.Cos(a), height[r], -31f + radius[r] * Mathf.Sin(a)), .012f, 4, false);
                }
            }
            W.Kula(led, new Vector3(0, 7.55f, -31f), .28f, 12, 8);
            W.Lampa("T11 lobby chandelier warm light", new Vector3(0, 8.6f, -31f), 1.6f, 16f * K, new Color(1f, .86f, .62f));
            W.Lampa("T11 lobby violet casino glow", new Vector3(0, 4.0f, -25.2f), .9f, 12f * K, new Color(.70f, .38f, 1f));
        }
        catch { W.Wyczysc(); Object.DestroyImmediate(g.gameObject); throw; }
        W.Zapisz("mistrzowie_t11_rotunda_lobby");
        g.SetParent(shell, false);
        g.localPosition = Vector3.zero; g.localRotation = Quaternion.identity; g.localScale = Vector3.one * K;
        Debug.Log("T11_ROTUNDA_LOBBY_OWNER_BOARD chandelierRings=3 crystals=54 violetStrips=6 gallery=33.5m portal=6x4.5 lights=2 scale=K_once");
    }
}
