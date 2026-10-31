using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// BRAXHORN CITY: boiska na podwórku.
// 1) Dotychczasowy ceglasty kort -> boisko piłkarskie (sztuczna trawa z fotoskanu, nowe linie, 2 bramki).
// 2) Drugie boisko obok: koszykówka (nawierzchnia akrylowa, linie, 2 kosze, ławki) w wolnym miejscu.
// Bramki/kosze zbudowane ze stalowych elementów; można je potem podmienić na modele od właściciela.
public static class BraxClaudeCourts
{
    [Serializable] sealed class Report { public bool football, basketball; public Vector3 court1, court2; public Vector2 size1, size2; public List<string> notes = new List<string>(); }
    static Transform root; static float ground = .15f;

    static void Box(string n, Vector3 at, Vector3 size, Material m, bool solid = false) => BraxCityArchitecture.Box(n, root, at, size, m, solid);

    public static void Apply(Transform cityRoot)
    {
        if (!BraxClaudeTowerTerrace.Enabled) return;
        var rep = new Report();
        try { Run(cityRoot, rep); } catch (Exception e) { rep.notes.Add("BŁĄD: " + e.Message); Debug.LogError("BRAXHORN_CLAUDE_COURTS " + e); }
        Directory.CreateDirectory("Assets/PlayableGenerated/ClaudeGround");
        File.WriteAllText("Assets/PlayableGenerated/ClaudeGround/courts.json", JsonUtility.ToJson(rep, true));
        Debug.Log("BRAXHORN_CLAUDE_COURTS " + JsonUtility.ToJson(rep));
    }

    static bool BraxRegionCourtsSkip(Transform t)
    {
        for (var p = t; p; p = p.parent) if (p.name.StartsWith("CLAUDE")) return true;
        return false;
    }

    static bool Reddish(Material m)
    {
        if (!m) return false; var c = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
        string n = m.name.ToLowerInvariant();
        return n.Contains("clay") || n.Contains("court") || n.Contains("kort") || (c.r > .35f && c.r > c.g * 1.4f && c.r > c.b * 1.6f);
    }

    static void Run(Transform cityRoot, Report rep)
    {
        root = new GameObject("CLAUDE boiska na podwórku").transform; root.SetParent(cityRoot, false); root.position = Vector3.zero;
        Physics.SyncTransforms();
        var white = BraxCityMaterials.Surface("Boisko: biała farba linii", new Color(.86f, .86f, .84f), .9f);
        var steel = BraxCityMaterials.Surface("Boisko: ocynkowana stal", new Color(.62f, .63f, .64f), .4f, .85f);
        var net = BraxCityMaterials.Surface("Boisko: siatka bramki", new Color(.85f, .85f, .85f, .35f), .9f);

        // ---------- 1) kort -> boisko piłkarskie ----------
        MeshRenderer court = null; float best = 0;
        // Najpierw prawdziwy kort: duża płaska płyta (>= 14 m) przy (10, 80), niezależnie od koloru materiału
        // (kort ma teksturę, więc _Color jest białe i test „ceglasty” go nie łapie). Potem zapasowo po kolorze.
        foreach (var r in BraxClaudeGround.All<MeshRenderer>(cityRoot))
        {
            if (!r.enabled || BraxRegionCourtsSkip(r.transform)) continue;
            var b = r.bounds; if (b.size.y > .6f || b.max.y > 1f || Vector2.Distance(new Vector2(b.center.x, b.center.z), new Vector2(10, 80)) > 12f) continue;
            if (Mathf.Min(b.size.x, b.size.z) < 8f || Mathf.Max(b.size.x, b.size.z) < 14f) continue;
            float area = b.size.x * b.size.z; if (area > best && area < 1500) { best = area; court = r; }
        }
        if (!court)
            foreach (var r in BraxClaudeGround.All<MeshRenderer>(cityRoot))
            {
                var b = r.bounds; if (b.size.y > .6f || b.max.y > 1f || Vector2.Distance(new Vector2(b.center.x, b.center.z), new Vector2(10, 80)) > 12f) continue;
                if (!r.sharedMaterials.Any(Reddish)) continue;
                float area = b.size.x * b.size.z; if (area > best && area < 1500) { best = area; court = r; }
            }
        Bounds cb;
        if (court)
        {
            cb = court.bounds; ground = cb.max.y;
            string g = "Assets/Source/Claude/PolyHaven/textures/grass004/";
            if (File.Exists(g + "diffuse.jpg"))
            {
                AssetDatabase.ImportAsset(g + "diffuse.jpg"); AssetDatabase.ImportAsset(g + "normal.jpg"); AssetDatabase.ImportAsset(g + "arm.jpg");
                var turf = BraxClaudeGround.WorldMaterial("Boisko: sztuczna trawa", AssetDatabase.LoadAssetAtPath<Texture2D>(g + "diffuse.jpg"),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(g + "normal.jpg"), AssetDatabase.LoadAssetAtPath<Texture2D>(g + "arm.jpg"), 1.2f, new Color(.62f, .95f, .55f), .1f, .5f);
                court.sharedMaterials = court.sharedMaterials.Select(m => (Reddish(m) || !court.sharedMaterials.Any(Reddish)) ? turf : m).ToArray();   // kort z teksturą: cała nawierzchnia
            }
            // stare linie tenisowe (białe, płaskie) w obrębie boiska — wyłączone
            foreach (var r in BraxClaudeGround.All<MeshRenderer>(cityRoot))
            {
                if (r == court) continue; var b = r.bounds;
                if (!cb.Intersects(b) || b.size.y > .15f || b.max.y > ground + .1f) continue;
                if (r.sharedMaterials.All(m => m && m.HasProperty("_Color") && m.GetColor("_Color").grayscale > .7f)) r.enabled = false;
            }
            float L = cb.size.x >= cb.size.z ? cb.size.x : cb.size.z, W = cb.size.x >= cb.size.z ? cb.size.z : cb.size.x;
            bool alongX = cb.size.x >= cb.size.z; var c = cb.center; float y = ground + .006f;
            Vector3 P(float u, float v) => alongX ? new Vector3(c.x + u, y, c.z + v) : new Vector3(c.x + v, y, c.z + u);
            Vector3 S(float u, float v) => alongX ? new Vector3(u, .01f, v) : new Vector3(v, .01f, u);
            float hl = L * .5f - .6f, hw = W * .5f - .6f;
            Box("Boisko: linia boczna", P(0, hw), S(hl * 2, .1f), white); Box("Boisko: linia boczna", P(0, -hw), S(hl * 2, .1f), white);
            Box("Boisko: linia końcowa", P(hl, 0), S(.1f, hw * 2), white); Box("Boisko: linia końcowa", P(-hl, 0), S(.1f, hw * 2), white);
            Box("Boisko: linia środkowa", P(0, 0), S(.1f, hw * 2), white);
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2 / 48, rr = Mathf.Min(3f, hw * .45f);
                Box("Boisko: koło środkowe", P(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr), new Vector3(.22f, .01f, .22f), white);
            }
            foreach (float side in new[] { -1f, 1f })
            {
                float px = side * (hl - 3.5f);
                Box("Boisko: pole karne", P(px, 0), S(.1f, Mathf.Min(10f, hw * 1.6f)), white);
                Box("Boisko: pole karne bok", P(side * (hl - 1.75f), Mathf.Min(5f, hw * .8f)), S(3.5f, .1f), white);
                Box("Boisko: pole karne bok", P(side * (hl - 1.75f), -Mathf.Min(5f, hw * .8f)), S(3.5f, .1f), white);
                // bramka 3 × 2 m ze stalowych rur i siatką
                float gx = side * (hl + .05f), d = side * 1.0f; var gy = ground;
                Vector3 G(float u, float v, float h) => alongX ? new Vector3(c.x + u, gy + h, c.z + v) : new Vector3(c.x + v, gy + h, c.z + u);
                Vector3 GS(float u, float v, float h) => alongX ? new Vector3(u, h, v) : new Vector3(v, h, u);
                Box("Bramka: słupek", G(gx, 1.5f, 1f), GS(.1f, .1f, 2f), steel, true);
                Box("Bramka: słupek", G(gx, -1.5f, 1f), GS(.1f, .1f, 2f), steel, true);
                Box("Bramka: poprzeczka", G(gx, 0, 2f), GS(.1f, 3.1f, .1f), steel);
                Box("Bramka: rama tył", G(gx + d, 0, .05f), GS(.06f, 3.1f, .06f), steel);
                Box("Bramka: rama boczna", G(gx + d * .5f, 1.5f, 1.0f), GS(1f, .05f, .05f), steel);
                Box("Bramka: rama boczna", G(gx + d * .5f, -1.5f, 1.0f), GS(1f, .05f, .05f), steel);
                Box("Bramka: siatka tył", G(gx + d, 0, 1f), GS(.02f, 3f, 2f), net, true);
                Box("Bramka: siatka góra", G(gx + d * .5f, 0, 2f), GS(1f, 3f, .02f), net);
                Box("Bramka: siatka bok", G(gx + d * .5f, 1.5f, 1f), GS(1f, .02f, 2f), net);
                Box("Bramka: siatka bok", G(gx + d * .5f, -1.5f, 1f), GS(1f, .02f, 2f), net);
            }
            rep.football = true; rep.court1 = c; rep.size1 = new Vector2(L, W);
        }
        else { rep.notes.Add("nie znaleziono kortu przy (10, 80)"); cb = new Bounds(new Vector3(10, ground, 80), new Vector3(24, .1f, 12)); }

        // ---------- 2) drugie boisko: koszykówka ----------
        float bl = 22f, bw = 12f; Vector3? spot = null; bool rotated = false;
        var tries = new List<(Vector3, bool)>();
        for (float dx = 6; dx <= 44; dx += 2) foreach (float sx in new[] { 1f, -1f })
            foreach (bool rot in new[] { false, true })
                tries.Add((cb.center + new Vector3(sx * (cb.size.x * .5f + dx), 0, 0), rot));
        for (float dz = 6; dz <= 30; dz += 2) foreach (float sz in new[] { 1f, -1f })
            tries.Add((cb.center + new Vector3(0, 0, sz * (cb.size.z * .5f + dz)), false));
        foreach (var (p, rot) in tries)
        {
            var size = rot ? new Vector3(bw + 2, 3f, bl + 2) : new Vector3(bl + 2, 3f, bw + 2);
            var cc = new Vector3(p.x, ground + .1f + 1.5f, p.z);
            if (Physics.CheckBox(cc, size * .5f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;
            bool flat = true;
            for (float u = -.5f; u <= .5f && flat; u += .25f) for (float v = -.5f; v <= .5f && flat; v += .25f)
            {
                var q = new Vector3(p.x + u * size.x, 5f, p.z + v * size.z);
                if (!Physics.Raycast(q, Vector3.down, out var h, 8f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(h.point.y - ground) > .25f) flat = false;
            }
            if (flat) { spot = p; rotated = rot; break; }
        }
        if (!spot.HasValue) { rep.notes.Add("brak wolnego miejsca na drugie boisko 24 × 14 m"); return; }
        var s = spot.Value; float yb = ground + .02f;
        var acrylic = BraxCityMaterials.Surface("Boisko 2: akryl niebieski", new Color(.13f, .27f, .45f), .75f);
        var key = BraxCityMaterials.Surface("Boisko 2: akryl pomarańczowy", new Color(.70f, .33f, .14f), .75f);
        var board = BraxCityMaterials.Surface("Kosz: tablica", new Color(.9f, .9f, .9f), .3f);
        var ring = BraxCityMaterials.Surface("Kosz: obręcz", new Color(.85f, .35f, .08f), .35f, .6f);
        Vector3 Q(float u, float v, float h) => rotated ? new Vector3(s.x + v, yb + h, s.z + u) : new Vector3(s.x + u, yb + h, s.z + v);
        Vector3 QS(float u, float v, float h) => rotated ? new Vector3(v, h, u) : new Vector3(u, h, v);
        Box("Boisko 2: nawierzchnia", Q(0, 0, -.02f), QS(bl + 2, bw + 2, .04f), acrylic, true);
        Box("Boisko 2: linia boczna", Q(0, bw * .5f, .005f), QS(bl, .08f, .01f), white); Box("Boisko 2: linia boczna", Q(0, -bw * .5f, .005f), QS(bl, .08f, .01f), white);
        Box("Boisko 2: linia końcowa", Q(bl * .5f, 0, .005f), QS(.08f, bw, .01f), white); Box("Boisko 2: linia końcowa", Q(-bl * .5f, 0, .005f), QS(.08f, bw, .01f), white);
        Box("Boisko 2: linia środkowa", Q(0, 0, .005f), QS(.08f, bw, .01f), white);
        foreach (float side in new[] { -1f, 1f })
        {
            Box("Boisko 2: pole trzech sekund", Q(side * (bl * .5f - 2.9f), 0, .003f), QS(5.8f, 4.9f, .01f), key);
            Box("Kosz: słup", Q(side * (bl * .5f + .6f), 0, 1.6f), QS(.16f, .16f, 3.2f), steel, true);
            Box("Kosz: ramię", Q(side * (bl * .5f + .1f), 0, 3.15f), QS(1.0f, .1f, .1f), steel);
            Box("Kosz: tablica", Q(side * (bl * .5f - .4f), 0, 3.4f), QS(.04f, 1.8f, 1.05f), board, true);
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2 / 16, rr = .23f;
                Box("Kosz: obręcz", Q(side * (bl * .5f - .4f) - side * (.25f + rr) + Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, 3.05f), QS(.1f, .1f, .025f), ring);
            }
        }
        rep.basketball = true; rep.court2 = s; rep.size2 = new Vector2(bl, bw);
    }

}
