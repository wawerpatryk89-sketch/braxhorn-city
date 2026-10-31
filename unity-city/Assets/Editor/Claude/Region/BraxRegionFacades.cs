using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Elewacje głównej ulicy od wyjścia z siłowni do morza (decyzja właściciela 31.10, 17:04): budynki oryginału
/// (Nutrition, blok nad NOVA i inne nieprzebudowane) mają okna namalowane w teksturze. Tu dostają prawdziwe okna:
/// okładzina z cegły/tynku (fotoskan) od 4,4 m w górę, w niej wnęki okienne z glifami, ramą, parapetem i szybą
/// z pokojem za szybą (shader Brax/CityInteriorMap), gzymsy między piętrami i gzyms wieńczący.
/// Parter (witryny, wejścia, szyldy) zostaje bez zmian. Budynki z zestawu modularnego (BraxCityArchitecture.StreetBuildings)
/// są pomijane — mają już prawdziwe okna.
/// </summary>
public static class BraxRegionFacades
{
    const float StreetX0 = 6f, StreetX1 = 84f;         // od siłowni do morza
    const float SouthZ0 = 15f, SouthZ1 = 24f;          // front rzędu południowego (z ≈ 20,6)
    const float NorthZ0 = 32f, NorthZ1 = 40f;          // front rzędu północnego (z ≈ 36,1)
    const float GroundTop = 4.4f, Floor = 3.2f;
    const float WinW = 1.5f, WinH = 1.7f, Pitch = 2.8f, Margin = 1.0f, Depth = .28f, Clad = .07f;

    sealed class Facade { public float x0, x1, z, top; public bool south; public string name; }

    public static void Apply(Transform cityRoot)
    {
        var source = BraxClaudeGround.SourceCity;
        if (!source) { BraxRegion.Note("elewacje: brak hierarchii oryginału"); return; }
        var root = BraxRegion.Root(cityRoot, "CLAUDE elewacje ulicy (okna, gzymsy)");
        var facades = new List<Facade>();
        foreach (bool south in new[] { true, false })
        {
            var spans = new List<Facade>();
            foreach (var r in source.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!r.enabled) continue;
                var b = r.bounds;
                if (b.max.x < StreetX0 || b.min.x > StreetX1 || b.size.y < 6f) continue;
                bool ok = south ? (b.max.z >= SouthZ0 && b.max.z <= SouthZ1 && b.min.z < SouthZ0) : (b.min.z >= NorthZ0 && b.min.z <= NorthZ1 && b.max.z > NorthZ1);
                if (!ok) continue;
                spans.Add(new Facade { x0 = Mathf.Max(b.min.x, StreetX0), x1 = Mathf.Min(b.max.x, StreetX1), z = south ? b.max.z : b.min.z, top = b.max.y, south = south, name = r.name });
            }
            spans.Sort((a, c) => a.x0.CompareTo(c.x0));
            Facade cur = null;
            foreach (var s in spans)
            {
                if (cur != null && s.x0 <= cur.x1 + .5f) { cur.x1 = Mathf.Max(cur.x1, s.x1); cur.top = Mathf.Max(cur.top, s.top); cur.z = south ? Mathf.Max(cur.z, s.z) : Mathf.Min(cur.z, s.z); cur.name += "+" + s.name; }
                else { cur = new Facade { x0 = s.x0, x1 = s.x1, z = s.z, top = s.top, south = south, name = s.name }; facades.Add(cur); }
            }
        }
        // Wytnij zakresy zajęte przez budynki z zestawu modularnego (mają już prawdziwe okna).
        var kit = BraxCityArchitecture.StreetBuildings.Select(b => new Vector2(b.x0, b.x1)).ToList();
        var work = new List<Facade>();
        foreach (var f in facades)
        {
            var pieces = new List<Vector2> { new Vector2(f.x0, f.x1) };
            foreach (var k in kit)
            {
                var next = new List<Vector2>();
                foreach (var p in pieces)
                {
                    if (k.y <= p.x + .3f || k.x >= p.y - .3f) { next.Add(p); continue; }
                    if (k.x > p.x + 3f) next.Add(new Vector2(p.x, k.x));
                    if (k.y < p.y - 3f) next.Add(new Vector2(k.y, p.y));
                }
                pieces = next;
            }
            foreach (var p in pieces) if (p.y - p.x >= 4f) work.Add(new Facade { x0 = p.x, x1 = p.y, z = f.z, top = f.top, south = f.south, name = f.name });
        }

        var brick = BraxRegion.Pbr("Elewacja: cegła", 2.2f, new Color(1f, .93f, .88f), "red_brick_03", "brick_wall_001", "brick_wall_02");
        var plaster = BraxRegion.Pbr("Elewacja: tynk", 2.5f, new Color(.96f, .93f, .86f), "white_plaster_02", "plastered_wall_04", "painted_plaster_wall");
        var stone = BraxRegion.Pbr("Elewacja: kamień gzymsów", 1.5f, new Color(.92f, .9f, .86f), "concrete_wall_008", "granite_tile_02", "concrete_floor_02");
        var frame = BraxCityMaterials.Surface("Okno: rama (antracyt)", new Color(.16f, .17f, .18f), .45f, .3f);
        var glass = GlassMaterial();
        int windows = 0, done = 0;
        foreach (var f in work)
        {
            var cladMat = done % 2 == 0 ? brick : plaster;
            Build(root, f, cladMat, stone, frame, glass, ref windows);
            done++;
        }
        LobbyPicture(root);
        BraxRegion.Note("elewacje ulicy: budynków " + work.Count + ", okien " + windows + " (" + string.Join("; ", work.Select(w => (w.south ? "S " : "N ") + w.x0.ToString("F0") + ".." + w.x1.ToString("F0") + " h" + w.top.ToString("F0"))) + ")");
    }

    // Obraz właściciela (mural Brax) w ramie na ścianie lobby BRAXHORN TOWER (decyzja 31.10, 17:05: „pusto, tylko recepcja”).
    static void LobbyPicture(Transform root)
    {
        const string path = "Assets/CityRebuild/Brand/nutrition-mural.jpg";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (!tex) { BraxRegion.Note("lobby: brak obrazu " + path); return; }
        Physics.SyncTransforms();
        var from = new Vector3(59f, 2.0f, 44f); RaycastHit hit; Vector3 at = Vector3.zero, normal = Vector3.zero; float best = 99f;
        foreach (var d in new[] { Vector3.forward, Vector3.left, Vector3.right })
            if (Physics.Raycast(from, d, out hit, 14f, ~(1 << 8), QueryTriggerInteraction.Ignore) && hit.distance < best && Mathf.Abs(hit.normal.y) < .3f) { best = hit.distance; at = hit.point; normal = hit.normal; }
        if (best > 50f) { BraxRegion.Note("lobby: nie znaleziono ściany na obraz"); return; }
        float w = 3.2f, h = w * tex.height / tex.width;
        var frameM = BraxCityMaterials.Surface("Lobby: rama obrazu", new Color(.12f, .11f, .1f), .5f, .2f);
        var canvas = new Material(Shader.Find(BraxCityMaterials.ShaderName)) { name = "Lobby: obraz Brax" };
        canvas.SetTexture("_MainTex", tex); canvas.SetColor("_Color", Color.white); canvas.SetFloat("_Roughness", .85f); canvas.SetFloat("_Metallic", 0f);
        Directory.CreateDirectory(BraxRegion.Out + "/Materials"); AssetDatabase.CreateAsset(canvas, BraxRegion.Out + "/Materials/lobby-picture.mat");
        var rot = Quaternion.LookRotation(-normal, Vector3.up);
        var g = new GameObject("Lobby: obraz Brax w ramie"); g.transform.SetParent(root, false);
        g.transform.position = at + normal * .06f + Vector3.up * (2.1f - 2.0f); g.transform.rotation = rot;
        BraxCityArchitecture.Box("rama", g.transform, new Vector3(0, 0, -.03f), new Vector3(w + .16f, h + .16f, .06f), frameM, false);
        var pic = BraxCityArchitecture.Box("płótno", g.transform, new Vector3(0, 0, .005f), new Vector3(w, h, .01f), canvas, false, Mathf.Max(w, h));
        var mf = pic.GetComponent<MeshFilter>();
        if (mf && mf.sharedMesh)
        {   // UV płótna: cała tekstura na przedniej ścianie
            var m = mf.sharedMesh; var v = m.vertices; var uv = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++) uv[i] = new Vector2(Mathf.InverseLerp(-w * .5f, w * .5f, v[i].x), Mathf.InverseLerp(-h * .5f, h * .5f, v[i].y));
            m.uv = uv;
        }
        BraxRegion.Note("lobby: obraz " + w.ToString("F1") + "×" + h.ToString("F1") + " m na ścianie " + at);
    }

    static Material GlassMaterial()
    {
        var shader = Shader.Find("Brax/CityInteriorMap");
        string path = BraxRegion.Out + "/Materials/interior-map.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m && shader && m.shader == shader) return m;
        if (shader)
        {
            m = new Material(shader) { name = "Okno: szyba z pokojem (elewacje ulicy)" };
            m.SetVector("_RoomSize", new Vector4(4.8f, 3.2f, 5.0f, 0)); m.SetVector("_RoomOffset", new Vector4(0, .55f, 0, 0)); m.SetVector("_TexMetres", new Vector4(3f, 3f, 3f, 0));
            Directory.CreateDirectory(BraxRegion.Out + "/Materials");
            AssetDatabase.CreateAsset(m, BraxRegion.Out + "/Materials/facade-glass.mat");
            return m;
        }
        return BraxCityMaterials.Surface("Okno: szyba (ciemna)", new Color(.12f, .16f, .2f), .08f, .6f);
    }

    static void Build(Transform root, Facade f, Material clad, Material stone, Material frame, Material glass, ref int windows)
    {
        float dir = f.south ? 1f : -1f;                 // kierunek na ulicę (normalna elewacji)
        float zFace = f.z + dir * .02f;                 // tuż przed ścianą oryginału
        float zClad = zFace + dir * Clad * .5f;         // środek okładziny
        float width = f.x1 - f.x0, cx = (f.x0 + f.x1) * .5f;
        float top = f.top - .25f;
        var parent = new GameObject("Elewacja " + (f.south ? "S" : "N") + " x" + f.x0.ToString("F0") + ".." + f.x1.ToString("F0")).transform;
        parent.SetParent(root, false);
        // pas między parterem a piętrami (gzyms cokołowy)
        BraxCityArchitecture.Box("Gzyms nad parterem", parent, new Vector3(cx, GroundTop - .2f, zFace + dir * .16f), new Vector3(width, .4f, .32f), stone, false);
        int n = Mathf.Max(1, Mathf.FloorToInt((width - 2f * Margin + (Pitch - WinW)) / Pitch));
        float span = n * Pitch - (Pitch - WinW), startX = cx - span * .5f;
        for (float y = GroundTop; y + Floor <= top + .3f; y += Floor)
        {
            float wy0 = y + .9f, wy1 = wy0 + WinH, yTop = Mathf.Min(y + Floor, top);
            // pas pod oknami i nad oknami
            BraxCityArchitecture.Box("Okładzina: pas dolny", parent, new Vector3(cx, (y + wy0) * .5f, zClad), new Vector3(width, wy0 - y, Clad), clad, false);
            if (yTop > wy1) BraxCityArchitecture.Box("Okładzina: pas górny", parent, new Vector3(cx, (wy1 + yTop) * .5f, zClad), new Vector3(width, yTop - wy1, Clad), clad, false);
            // filary między oknami + skrajne
            float prev = f.x0;
            for (int i = 0; i <= n; i++)
            {
                float wx0 = i < n ? startX + i * Pitch : f.x1, wx1 = i < n ? wx0 + WinW : f.x1;
                if (wx0 > prev + .01f) BraxCityArchitecture.Box("Okładzina: filar", parent, new Vector3((prev + wx0) * .5f, (wy0 + wy1) * .5f, zClad), new Vector3(wx0 - prev, WinH, Clad), clad, false);
                prev = wx1;
                if (i >= n) break;
                // wnęka: glify (boki, nadproże), parapet, szyba w głębi, rama
                float wcx = (wx0 + wx1) * .5f, wcy = (wy0 + wy1) * .5f;
                float zIn = zFace - dir * Depth;        // głębia wnęki (wchodzi w ścianę oryginału)
                float zMid = (zFace + dir * Clad + zIn) * .5f, dz = Depth + Clad;
                BraxCityArchitecture.Box("Okno: glif L", parent, new Vector3(wx0 - .05f, wcy, zMid), new Vector3(.1f, WinH + .1f, dz), stone, false);
                BraxCityArchitecture.Box("Okno: glif P", parent, new Vector3(wx1 + .05f, wcy, zMid), new Vector3(.1f, WinH + .1f, dz), stone, false);
                BraxCityArchitecture.Box("Okno: nadproże", parent, new Vector3(wcx, wy1 + .05f, zMid), new Vector3(WinW + .2f, .1f, dz), stone, false);
                BraxCityArchitecture.Box("Okno: parapet", parent, new Vector3(wcx, wy0 - .04f, zFace + dir * (Clad + .06f) * .5f - dir * Depth * .5f), new Vector3(WinW + .24f, .08f, Depth + Clad + .06f), stone, false);
                BraxCityArchitecture.Box("Okno: szyba", parent, new Vector3(wcx, wcy, zIn + dir * .03f), new Vector3(WinW, WinH, .02f), glass, false);
                BraxCityArchitecture.Box("Okno: rama", parent, new Vector3(wcx, wcy, zIn + dir * .08f), new Vector3(WinW, .06f, .05f), frame, false);
                BraxCityArchitecture.Box("Okno: rama pion", parent, new Vector3(wcx, wcy, zIn + dir * .08f), new Vector3(.06f, WinH, .05f), frame, false);
                BraxCityArchitecture.Box("Okno: rama obwód L", parent, new Vector3(wx0 + .03f, wcy, zIn + dir * .08f), new Vector3(.06f, WinH, .05f), frame, false);
                BraxCityArchitecture.Box("Okno: rama obwód P", parent, new Vector3(wx1 - .03f, wcy, zIn + dir * .08f), new Vector3(.06f, WinH, .05f), frame, false);
                windows++;
            }
            // gzyms międzypiętrowy
            if (yTop < top - .5f) BraxCityArchitecture.Box("Gzyms piętra", parent, new Vector3(cx, yTop, zFace + dir * (Clad + .05f)), new Vector3(width, .12f, Clad + .1f), stone, false);
        }
        // gzyms wieńczący
        BraxCityArchitecture.Box("Gzyms wieńczący", parent, new Vector3(cx, top + .15f, zFace + dir * .2f), new Vector3(width + .2f, .4f, .4f), stone, false);
        foreach (var r in parent.GetComponentsInChildren<MeshRenderer>(true)) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
    }
}
