using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — plaża: parasole, leżaki, ręczniki (decyzja właściciela 31.10).
// W katalogu Poly Haven nie ma parasoli ani leżaków, więc geometria jest autorska, ale każda powierzchnia
// ma fotoskan Poly Haven (płótno: rough_linen/gingham_check, drewno: teak_veneer/wood_planks_grey, ręcznik: terry_cloth).
// Nic nie świeci. Stawiane tylko na piasku (promień w dół na kolizję piasku), z dala od pomostu.
public static class BraxBeachFurniture
{
    struct Spot { public float x, z, yaw; public int canvas; public Spot(float x, float z, float yaw, int canvas) { this.x = x; this.z = z; this.yaw = yaw; this.canvas = canvas; } }

    static readonly Spot[] Spots =
    {
        new Spot(93f, 125f, 10f, 0), new Spot(102f, 131f, -20f, 1), new Spot(110f, 123f, 15f, 2),
        new Spot(129f, 124f, -10f, 3), new Spot(137f, 134f, 20f, 0), new Spot(139f, 148f, -30f, 1),
        new Spot(126f, 142f, 0f, 2), new Spot(97f, 140f, 25f, 3),
    };
    static readonly Color[] Canvas =
    {
        new Color(.95f, .92f, .84f), new Color(.18f, .52f, .56f), new Color(.80f, .20f, .18f), new Color(.14f, .20f, .38f),
    };
    static readonly Color[] Towels =
    {
        new Color(.92f, .55f, .20f), new Color(.25f, .60f, .85f), new Color(.93f, .93f, .90f), new Color(.60f, .20f, .50f),
    };

    public static int Place(Transform harborRoot)
    {
        Physics.SyncTransforms();
        var root = new GameObject("CLAUDE plaża: parasole, leżaki, ręczniki").transform;
        root.SetParent(harborRoot, false); root.position = Vector3.zero;
        Directory.CreateDirectory(BraxRegion.Out + "/Meshes");
        var pole = BraxRegion.Fitting("Parasol: słupek (biały metal)", new Color(.86f, .87f, .88f), .38f, .85f);
        var canvasMats = new Material[Canvas.Length];
        for (int i = 0; i < Canvas.Length; i++)
        {
            canvasMats[i] = Pbr("Parasol: płótno " + i, .55f, Canvas[i], "rough_linen", "gingham_check", "fabric_pattern_05", "terry_cloth");
            if (canvasMats[i]) { canvasMats[i].SetFloat("_Cull", 0); EditorUtility.SetDirty(canvasMats[i]); }
        }
        var wood = Pbr("Leżak: drewno", 1f, null, "teak_veneer", "wood_planks_grey", "wooden_planks", "wood_planks")
                   ?? BraxRegion.Fitting("Leżak: drewno (bez tekstury)", new Color(.55f, .42f, .28f), .8f, 0f);
        var towels = new Material[Towels.Length];
        for (int i = 0; i < Towels.Length; i++) towels[i] = Pbr("Ręcznik " + i, .45f, Towels[i], "terry_cloth", "rough_linen", "fabric_pattern_05");
        int placed = 0, idx = 0;
        foreach (var s in Spots)
        {
            float y; if (!OnSand(s.x, s.z, out y)) continue;
            var g = new GameObject("Plaża: parasol i leżaki").transform;
            g.SetParent(root, false); g.position = new Vector3(s.x, y, s.z); g.rotation = Quaternion.Euler(0, s.yaw, 0);
            var canvas = canvasMats[s.canvas % canvasMats.Length] ?? BraxRegion.Fitting("Parasol: płótno", Canvas[s.canvas % Canvas.Length], .9f, 0f);
            Umbrella(g, canvas, pole);
            // dwa leżaki przed parasolem (lokalne −Z = w stronę brzegu), lekko rozstawione
            foreach (float dx in new[] { -1.25f, 1.25f })
            {
                var l = new GameObject("Leżak").transform; l.SetParent(g, false);
                l.localPosition = new Vector3(dx, 0, -1.5f); l.localRotation = Quaternion.Euler(0, dx < 0 ? -8f : 8f, 0);
                // leżak stoi na piasku w swoim miejscu (piasek ma mikrorelief)
                float ly; if (OnSand(l.position.x, l.position.z, out ly)) l.position = new Vector3(l.position.x, ly, l.position.z);
                Lounger(l, wood, (idx % 3 == 0) ? null : towels[idx % towels.Length]);
                idx++;
            }
            // ręcznik rozłożony na piasku obok
            if (idx % 2 == 0)
            {
                var t = new GameObject("Ręcznik na piasku").transform; t.SetParent(g, false);
                t.localPosition = new Vector3(2.9f, 0, -1.2f); t.localRotation = Quaternion.Euler(0, 35f, 0);
                float ty; if (OnSand(t.position.x, t.position.z, out ty)) t.position = new Vector3(t.position.x, ty + .012f, t.position.z);
                BraxCityArchitecture.Box("Ręcznik", t, new Vector3(0, .008f, 0), new Vector3(.75f, .016f, 1.6f), towels[(idx + 1) % towels.Length] ?? wood, false, .45f);
            }
            placed++;
        }
        return placed;
    }

    static Material Pbr(string name, float metres, Color? tint, params string[] slugs)
    {
        try { return BraxRegion.Pbr(name, metres, tint, slugs); }
        catch (Exception) { return null; }
    }

    static bool OnSand(float x, float z, out float y)
    {
        y = 0;
        bool found = false; float bestY = float.NegativeInfinity;
        foreach (var h in Physics.RaycastAll(new Vector3(x, 30f, z), Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.normal.y < .8f) continue;
            string n = h.collider.name;
            if (!n.Contains("piasek")) continue;          // tylko kolizja piasku (nie łąka, nie podłoga)
            if (h.point.y > bestY) { bestY = h.point.y; found = true; }
        }
        if (!found || bestY < -.3f) return false;         // nie w wodzie
        y = bestY; return true;
    }

    // ---------- parasol: słupek + czasza (bryła obrotowa, dwustronna) ----------
    static void Umbrella(Transform parent, Material canvas, Material pole)
    {
        var p = Lathe("Parasol: słupek", new[] { new Vector2(.022f, -.25f), new Vector2(.022f, 2.18f), new Vector2(0f, 2.18f) }, 12, false);
        Attach(parent, p, pole, false);
        var c = Lathe("Parasol: czasza", new[] { new Vector2(0f, 2.34f), new Vector2(.55f, 2.20f), new Vector2(1.05f, 1.98f), new Vector2(1.45f, 1.76f), new Vector2(1.50f, 1.70f) }, 24, true);
        Attach(parent, c, canvas, false);
        var col = parent.gameObject.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 1.0f, 0); col.radius = .06f; col.height = 2.2f;
    }

    // ---------- leżak: siedzisko, oparcie pod kątem, nogi, opcjonalny ręcznik ----------
    static void Lounger(Transform parent, Material wood, Material towel)
    {
        var w = wood ?? BraxRegion.Fitting("Leżak: drewno", new Color(.55f, .42f, .28f), .8f, 0f);
        BraxCityArchitecture.Box("Leżak: siedzisko", parent, new Vector3(0, .36f, -.1f), new Vector3(.64f, .045f, 1.25f), w, false, 1f);
        foreach (float sx in new[] { -.29f, .29f })
            foreach (float sz in new[] { -.65f, .45f })
                BraxCityArchitecture.Box("Leżak: noga", parent, new Vector3(sx, .17f, sz), new Vector3(.045f, .34f, .045f), w, false, 1f);
        foreach (float sx in new[] { -.33f, .33f })
            BraxCityArchitecture.Box("Leżak: podłokietnik", parent, new Vector3(sx, .60f, -.05f), new Vector3(.04f, .04f, .95f), w, false, 1f);
        var back = new GameObject("Leżak: oparcie").transform; back.SetParent(parent, false);
        back.localPosition = new Vector3(0, .38f, .52f); back.localRotation = Quaternion.Euler(-52f, 0, 0);
        BraxCityArchitecture.Box("Leżak: oparcie deska", back, new Vector3(0, .36f, 0), new Vector3(.64f, .045f, .74f), w, false, 1f);
        if (towel)
        {
            BraxCityArchitecture.Box("Leżak: ręcznik", parent, new Vector3(0, .395f, -.15f), new Vector3(.56f, .018f, 1.05f), towel, false, .45f);
            var tb = new GameObject("Leżak: ręcznik na oparciu").transform; tb.SetParent(back, false);
            BraxCityArchitecture.Box("Ręcznik", tb, new Vector3(0, .395f, 0), new Vector3(.56f, .018f, .62f), towel, false, .45f);
        }
        var col = parent.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0, .35f, 0); col.size = new Vector3(.7f, .7f, 1.5f);
    }

    static void Attach(Transform parent, Mesh mesh, Material m, bool collider)
    {
        var g = new GameObject(mesh.name); g.transform.SetParent(parent, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        if (collider) g.AddComponent<MeshCollider>().sharedMesh = mesh;
        GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
    }

    static int serial;
    /// <summary>Bryła obrotowa wokół osi Y z profilu (promień, wysokość); twoSided dodaje wnętrze (czasza od spodu).</summary>
    static Mesh Lathe(string name, Vector2[] profile, int segments, bool twoSided)
    {
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        int rows = profile.Length;
        for (int r = 0; r < rows; r++)
            for (int s = 0; s <= segments; s++)
            {
                float a = s * Mathf.PI * 2f / segments;
                verts.Add(new Vector3(Mathf.Cos(a) * profile[r].x, profile[r].y, Mathf.Sin(a) * profile[r].x));
                uvs.Add(new Vector2((float)s / segments * 4f, (float)r / (rows - 1) * 1.6f));
            }
        int W = segments + 1;
        for (int r = 0; r < rows - 1; r++)
            for (int s = 0; s < segments; s++)
            {
                int a = r * W + s, b = a + 1, c = a + W, d = c + 1;
                tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d);
            }
        if (twoSided)
        {
            int n = verts.Count;
            for (int i = 0; i < n; i++) { verts.Add(verts[i]); uvs.Add(uvs[i]); }
            int t = tris.Count;
            for (int i = 0; i < t; i += 3) { tris.Add(tris[i] + n); tris.Add(tris[i + 2] + n); tris.Add(tris[i + 1] + n); }
        }
        var mesh = new Mesh { name = name };
        mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, BraxRegion.Out + "/Meshes/beach_" + name.Replace(' ', '_').Replace(':', '_') + "_" + (serial++) + ".asset");
        return mesh;
    }
}
