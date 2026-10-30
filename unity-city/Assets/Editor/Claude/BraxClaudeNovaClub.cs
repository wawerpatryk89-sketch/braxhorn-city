using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — wyposażenie klubu NOVA (tylko testowy build Claude).
// Stanowisko DJ z mikserem, lasery na rogach konsoli, kula nad parkietem,
// bar z marmurowym blatem i hokerami, loża z sofami z biblioteki.
// Każdy element stawiany tylko w wolnym miejscu (kolizje + podłoga pod spodem),
// więc schody do piwnicy, wejście i trasy NOVA zostają drożne.
public static class BraxClaudeNovaClub
{
    const string ND = "Assets/Source/Claude/Nova/";
    const string PH = "Assets/Source/Claude/PolyHaven/";
    const string Sofa = "Assets/Source/LibrarySofa02.glb";
    static readonly Vector3 HallCenter = new Vector3(43.5f, 1.6f, 48.5f);
    static readonly Vector3 HallSize = new Vector3(25f, 3.8f, 15f);

    [Serializable] sealed class Report
    {
        public string status = "NOT_RUN", error = "";
        public float floorY, ceilingY, wallW, wallE, wallS, wallN;
        public List<string> placed = new List<string>(), skipped = new List<string>();
    }

    static Report rep;
    static Transform root;
    static float floorY, ceilY;

    public static void Apply()
    {
        if (!BraxClaudeTowerTerrace.Enabled) return;
        rep = new Report();
        try { Build(); rep.status = "BUILT"; }
        catch (Exception e) { rep.status = "FAILED"; rep.error = e.Message; Debug.LogError("BRAXHORN_CLAUDE_NOVA " + e); }
        Directory.CreateDirectory("Assets/PlayableGenerated/ClaudeNova");
        File.WriteAllText("Assets/PlayableGenerated/ClaudeNova/nova-club.json", JsonUtility.ToJson(rep, true));
        Debug.Log("BRAXHORN_CLAUDE_NOVA " + JsonUtility.ToJson(rep));
    }

    // ---------- pomocnicze ----------
    static Texture2D Tex(string p)
    {
        if (!File.Exists(p)) throw new FileNotFoundException("brak pliku", p);
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        if (!t) throw new InvalidOperationException("tekstura nie zaimportowana " + p);
        return t;
    }
    static Material Pbr(string name, string slug, float metres, Color? tint = null)
    {
        string d = PH + "textures/" + slug + "/";
        var m = BraxCityMaterials.Textured(name, Tex(d + "diffuse.jpg"), Tex(d + "normal.jpg"), Tex(d + "arm.jpg"), 1f / metres);
        if (tint.HasValue) { m.SetColor("_Color", tint.Value); EditorUtility.SetDirty(m); }
        return m;
    }
    static GameObject Load(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("brak modelu", path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                 AssetDatabase.LoadAllAssetsAtPath(path).OfType<GameObject>().FirstOrDefault();
        if (!go) throw new InvalidOperationException("glTFast nie zaimportował " + path);
        return go;
    }
    static Bounds RB(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) throw new InvalidOperationException("model bez siatek " + go.name);
        var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b;
    }
    static bool Ray(Vector3 from, Vector3 dir, float max, out RaycastHit hit)
        => Physics.Raycast(from, dir, out hit, max, ~0, QueryTriggerInteraction.Ignore);

    // Wolne miejsce: brak kolizji w bryle i podłoga pod wszystkimi narożnikami.
    static bool Free(Vector3 center, Vector3 size)
    {
        var half = size * .5f - new Vector3(.03f, .03f, .03f);
        var c = new Vector3(center.x, floorY + .08f + half.y, center.z);
        if (Physics.CheckBox(c, half, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) return false;
        foreach (var o in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
        {
            var p = new Vector3(center.x + o.x * half.x, floorY + .5f, center.z + o.y * half.z);
            if (!Ray(p, Vector3.down, 1.0f, out var h) || Mathf.Abs(h.point.y - floorY) > .12f) return false;
        }
        return true;
    }
    static void Box(string name, Vector3 at, Vector3 size, Material m, bool solid = true)
        => BraxCityArchitecture.Box(name, root, at, size, m, solid);

    static GameObject Model(string path, string label, Vector3 floorPos, float yaw,
                            float? width = null, float? height = null, bool solid = true, bool topAt = false)
    {
        var o = (GameObject)UnityEngine.Object.Instantiate(Load(path), root, false);
        o.name = label; o.transform.position = Vector3.zero; o.transform.rotation = Quaternion.Euler(0, yaw, 0) * o.transform.rotation;
        var b = RB(o);
        float s = 1f;
        if (width.HasValue) s = width.Value / Mathf.Max(b.size.x, b.size.z);
        else if (height.HasValue) s = height.Value / b.size.y;
        o.transform.localScale *= s;
        b = RB(o);
        float y = topAt ? floorPos.y - b.max.y : floorPos.y - b.min.y;
        o.transform.position += new Vector3(floorPos.x - b.center.x, y, floorPos.z - b.center.z);
        foreach (var r in o.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
        if (solid)
        {
            b = RB(o);
            var c = new GameObject(label + " kolizja").AddComponent<BoxCollider>();
            c.transform.SetParent(root, false); c.transform.position = b.center; c.size = b.size;
        }
        rep.placed.Add(label);
        return o;
    }

    // ---------- budowa ----------
    static void Build()
    {
        Physics.SyncTransforms();
        var probe = new Vector3(HallCenter.x, HallCenter.y + HallSize.y * .5f - .2f, HallCenter.z);
        if (!Ray(probe, Vector3.down, 5f, out var fh)) throw new InvalidOperationException("nie znaleziono podłogi sali NOVA");
        floorY = fh.point.y;
        ceilY = Ray(new Vector3(HallCenter.x, floorY + 1.0f, HallCenter.z), Vector3.up, 6f, out var ch) ? ch.point.y : floorY + 3.4f;
        var eye = new Vector3(HallCenter.x, floorY + 1.2f, HallCenter.z);
        float Wall(Vector3 dir, float fallback) => Ray(eye, dir, 20f, out var h) ? Vector3.Dot(h.point, dir) : fallback;
        float wallW = -Wall(Vector3.left, -(HallCenter.x - HallSize.x * .5f));
        float wallE = Wall(Vector3.right, HallCenter.x + HallSize.x * .5f);
        float wallS = -Wall(Vector3.back, -(HallCenter.z - HallSize.z * .5f));
        float wallN = Wall(Vector3.forward, HallCenter.z + HallSize.z * .5f);
        rep.floorY = floorY; rep.ceilingY = ceilY; rep.wallW = wallW; rep.wallE = wallE; rep.wallS = wallS; rep.wallN = wallN;

        root = new GameObject("CLAUDE wyposażenie klubu NOVA").transform;
        root.position = Vector3.zero;
        var hall = GameObject.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.name == "BRAXHORN CITY reference renovation");
        if (hall) root.SetParent(hall, true);

        var granite = Pbr("NOVA: granit konsoli i baru", "granite_tile", 1.0f, new Color(.42f, .42f, .44f));
        var marble = Pbr("NOVA: marmurowy blat baru", "marble_01", 1.2f);
        var wood = Pbr("NOVA: ciemne drewno zabudowy", "wood_planks", 1.5f, new Color(.38f, .30f, .25f));
        var steel = BraxCityMaterials.Surface("NOVA: czarna stal", new Color(.10f, .10f, .11f), .38f, .8f);

        float cx = (wallW + wallE) * .5f;

        // 1) Konsola DJ przy ścianie północnej, przodem do parkietu.
        var boothSize = new Vector3(2.6f, 1.05f, .9f);
        Vector3? booth = null;
        foreach (float dx in new[] { 0f, -3f, 3f, -6f, 6f, -8.5f, 8.5f })
        {
            var p = new Vector3(cx + dx, floorY, wallN - .25f - boothSize.z * .5f);
            if (Free(p, boothSize + new Vector3(0, 0, .8f))) { booth = p; break; }
        }
        if (booth.HasValue)
        {
            var b = booth.Value;
            Box("NOVA: konsola DJ korpus", b + Vector3.up * (boothSize.y * .5f), boothSize, granite);
            Box("NOVA: konsola DJ blat", b + Vector3.up * (boothSize.y + .025f), new Vector3(boothSize.x + .1f, .05f, boothSize.z + .08f), steel, false);
            Model(ND + "dj_mixer.glb", "NOVA: mikser DJ", b + Vector3.up * (boothSize.y + .05f), 180f, width: 1.05f, solid: false);
            float laserH = Mathf.Clamp(ceilY - (floorY + boothSize.y + .05f) - .1f, .8f, 2.4f);
            Model(ND + "laser_light.glb", "NOVA: laser lewy", b + new Vector3(-boothSize.x * .5f + .15f, boothSize.y + .05f, 0), 0, height: laserH, solid: false);
            Model(ND + "laser_light.glb", "NOVA: laser prawy", b + new Vector3(boothSize.x * .5f - .15f, boothSize.y + .05f, 0), 0, height: laserH, solid: false);
        }
        else rep.skipped.Add("konsola DJ: brak wolnego miejsca przy ścianie północnej");

        // 2) Kula nad środkiem parkietu, zawieszona pod sufitem.
        float danceZ = booth.HasValue ? Mathf.Lerp(booth.Value.z, wallS, .45f) : HallCenter.z;
        float danceX = booth.HasValue ? booth.Value.x : cx;
        Model(ND + "disco_ball.glb", "NOVA: kula dyskotekowa", new Vector3(danceX, ceilY - .02f, danceZ), 0,
              height: Mathf.Min(1.2f, (ceilY - floorY) * .35f), solid: false, topAt: true);
        var reflection = new GameObject("NOVA: odbicia dla kuli").AddComponent<ReflectionProbe>();
        reflection.transform.SetParent(root, false);
        reflection.transform.position = new Vector3(danceX, floorY + 1.6f, danceZ);
        reflection.size = new Vector3(Mathf.Max(4, wallE - wallW), Mathf.Max(3, ceilY - floorY), Mathf.Max(4, wallN - wallS));
        reflection.mode = ReflectionProbeMode.Realtime;
        reflection.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        reflection.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        reflection.resolution = 64;

        // 3) Bar przy ścianie zachodniej (zapas: wschodnia), blat marmurowy, hokery.
        bool barDone = false;
        foreach (bool west in new[] { true, false })
        {
            float sign = west ? 1 : -1;
            float wall = west ? wallW : wallE;
            float L = Mathf.Min(6f, (wallN - wallS) - 3f);
            float counterX = wall + sign * (.4f + 1.15f + .35f);
            var cz = (wallN + wallS) * .5f;
            var counter = new Vector3(counterX, floorY, cz);
            var shelf = new Vector3(wall + sign * .22f, floorY, cz);
            if (!Free(counter, new Vector3(.7f, 1.1f, L)) || !Free(shelf, new Vector3(.4f, 2.0f, L))) continue;
            Box("NOVA: tylna zabudowa baru", shelf + Vector3.up * 1.0f, new Vector3(.38f, 2.0f, L), wood);
            Box("NOVA: półka barowa", shelf + new Vector3(sign * .02f, 1.45f, 0), new Vector3(.42f, .04f, L - .2f), steel, false);
            Box("NOVA: front baru", counter + Vector3.up * .53f, new Vector3(.6f, 1.06f, L), granite);
            Box("NOVA: marmurowy blat", counter + new Vector3(sign * .05f, 1.085f, 0), new Vector3(.78f, .05f, L + .1f), marble, false);
            int n = Mathf.Max(3, Mathf.FloorToInt(L / 1.2f));
            for (int i = 0; i < n; i++)
            {
                float z = cz - L * .5f + (i + .5f) * (L / n);
                var sp = new Vector3(counterX + sign * .75f, floorY, z);
                if (Free(sp, new Vector3(.5f, .9f, .5f)))
                    Model(PH + "models/bar_chair_round_01/bar_chair_round_01.gltf", "NOVA: hoker " + (i + 1), sp, west ? 90 : -90);
            }
            barDone = true; break;
        }
        if (!barDone) rep.skipped.Add("bar: brak wolnego miejsca przy ścianach bocznych");

        // 4) Loża: 2 sofy z biblioteki przy ścianie wschodniej (lub zachodniej, jeśli bar stanął po wschodniej).
        if (File.Exists(Sofa))
        {
            float wall = wallE; float sign = -1;
            for (int k = 0; k < 2; k++)
            {
                var p = new Vector3(wall + sign * .65f, floorY, (wallN + wallS) * .5f + (k == 0 ? -2.4f : 2.4f));
                if (Free(p, new Vector3(1.0f, .9f, 2.2f)))
                    Model(Sofa, "NOVA: sofa w loży " + (k + 1), p, -90, width: 2.1f);
                else rep.skipped.Add("sofa " + (k + 1) + ": zajęte miejsce");
            }
        }
        else rep.skipped.Add("sofa: brak " + Sofa);
    }
}
