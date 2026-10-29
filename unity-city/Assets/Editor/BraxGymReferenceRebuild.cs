using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Editor-only, deterministic finishing pass over the current imported gym.
// World coordinates below are Unity coordinates: the original glTF X conversion
// has already happened. City, player, animation and input assets are not edited.
public static class BraxGymReferenceRebuild
{
    const string TextureRoot = "Assets/Kickboxing/Textures/";
    const string ModelRoot = "Assets/Kickboxing/Models/";
    static readonly Color BrandGreen = new Color(139f / 255f, 1f, 58f / 255f);
    static readonly Bounds RingBounds = new Bounds(new Vector3(-9f, 1.4f, -5.1f), new Vector3(8.8f, 3.2f, 9.8f));
    static readonly Bounds BagFrameBounds = new Bounds(new Vector3(14f, 2.6f, -3f), new Vector3(.9f, 5.3f, 14.8f));
    static readonly Bounds AnnexPortal = new Bounds(new Vector3(-18.2f, 1.35f, 5.1f), new Vector3(1.2f, 2.7f, 1.8f));

    [Serializable] public sealed class Report
    {
        public string revision = "kickboxing-reference-annex-01";
        public string coordinateSystem = "Unity metres, existing glTF X mirror retained";
        public Vector3 annexMin = new Vector3(-25.5f, 0, -3f);
        public Vector3 annexMax = new Vector3(-18f, 3.1f, 8f);
        public Vector3 portalMin = new Vector3(-18.8f, 0, 4.2f);
        public Vector3 portalMax = new Vector3(-17.6f, 2.7f, 6f);
        public Vector3 mainDoorBefore, mainDoorAfter, ringCenterBefore, ringCenterAfter;
        public int sourceMeshesClipped, removedSourceTriangles, generatedMeshes, generatedTriangles, privacyRays, privacyBlocked;
        public bool privacyWithAllDoorsOpen, originalEntrancePreserved, originalRingPositionPreserved;
        public bool billboardArtworkInstalled = false, physicalIPhoneValidated = false;
        public string[] registeredDoors, requiredAssets;
        public string[] pending = { "approved billboard artwork", "physical iPhone performance test", "visual inspection of assembled fixtures" };
    }

    public static void Apply(GameObject gym, Transform solids, BraxGymPlayer player, string generated)
    {
        if (!gym || !solids || !player || !player.cityWorld || !player.door)
            throw new InvalidOperationException("Reference gym requires the existing connected city, gym, physics and entrance.");
        if (GameObject.Find("BRAXHORN")) throw new InvalidOperationException("Reference gym already exists in this generated scene.");
        var b = new Builder(gym, solids, player, generated);
        b.Build();
    }

    sealed class Builder
    {
        readonly GameObject gym;
        readonly Transform solids;
        readonly BraxGymPlayer player;
        readonly string folder;
        readonly Report report = new Report();
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        readonly List<Bounds> privacyWalls = new List<Bounds>();
        readonly List<BraxCityDoor> newDoors = new List<BraxCityDoor>();
        readonly List<Transform> batches = new List<Transform>();
        Transform root, hall, annex, fixtures, physics;
        Material brick, rubber, matRubber, blackSteel, steel, green, white, vinyl, wood, tile, plaster, ceiling, lit;
        Font font;
        int assetNumber;

        public Builder(GameObject gym, Transform solids, BraxGymPlayer player, string generated)
        {
            this.gym = gym; this.solids = solids; this.player = player;
            folder = generated + "/ReferenceRebuild";
        }

        public void Build()
        {
            ValidateSource();
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            root = Group(null, "BRAXHORN");
            hall = Group(root, "Hall"); annex = Group(root, "Annex");
            fixtures = Group(root, "Fixtures"); physics = Group(root, "Local physics");
            batches.Add(hall); batches.Add(annex);
            PrepareMaterials();
            CutAnnexDoorway();
            ClearObsoleteEquipment();
            FinishExistingSurfaces();
            BuildTrainingMat();
            BuildRingFinishing();
            BuildIndustrialCeiling();
            BuildBillboardMount();
            BuildAnnex();
            BuildReception();
            BuildMirrorAnchor();
            player.cityWorld.doors = (player.cityWorld.doors ?? Array.Empty<BraxCityDoor>()).Concat(newDoors).ToArray();
            report.registeredDoors = newDoors.Select(d => d.id).ToArray();
            CheckPrivacy();
            foreach (var group in batches) BatchByMaterial(group);
            var finalMeshes = root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh && f.GetComponent<Renderer>() && f.GetComponent<Renderer>().enabled).ToArray();
            report.generatedMeshes = finalMeshes.Length;
            report.generatedTriangles = finalMeshes.Sum(f => f.sharedMesh.triangles.Length / 3);
            report.mainDoorAfter = player.door.position;
            report.ringCenterAfter = FindSource(76).GetComponent<Renderer>().bounds.center;
            report.originalEntrancePreserved = Vector3.Distance(report.mainDoorBefore, report.mainDoorAfter) < .001f;
            report.originalRingPositionPreserved = Vector3.Distance(report.ringCenterBefore, report.ringCenterAfter) < .001f;
            if (!report.originalEntrancePreserved || !report.originalRingPositionPreserved)
                throw new InvalidOperationException("Gym finishing changed an existing entrance or the original ring position.");
            File.WriteAllText(folder + "/layout-report.json", JsonUtility.ToJson(report, true));
            AssetDatabase.SaveAssets();
            Debug.Log("BRAXHORN_REFERENCE_REBUILD " + JsonUtility.ToJson(report));
        }

        void ValidateSource()
        {
            var wall = FindSource(9).GetComponent<Renderer>().bounds;
            if (wall.min.x > -18.45f || wall.max.x < -17.6f || wall.size.y < 8f)
                throw new InvalidOperationException("Original right-wall bounds changed; refuse an inferred doorway cut.");
            if (Vector3.Distance(player.door.position, new Vector3(6.8f, 0, 11.56f)) > .02f)
                throw new InvalidOperationException("The original city entrance is not at its audited position.");
            if (player.bags == null || player.bags.Length != 4)
                throw new InvalidOperationException("Expected four original bag pivots; preserve their current training integration.");
            report.mainDoorBefore = player.door.position;
            report.ringCenterBefore = FindSource(76).GetComponent<Renderer>().bounds.center;
            if (Mathf.Abs(report.ringCenterBefore.x + 9) > .1f || Mathf.Abs(report.ringCenterBefore.z + 5.5f) > .1f)
                throw new InvalidOperationException("Original ring geometry moved; re-audit before finishing it.");
        }

        Transform FindSource(int index)
        {
            var t = gym.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "Gym_Mesh_" + index);
            if (!t) throw new InvalidOperationException("Audited original geometry absent: Gym_Mesh_" + index);
            return t;
        }

        void PrepareMaterials()
        {
            if (!Shader.Find("Standard")) throw new InvalidOperationException("Built-in Standard shader is required by the current renderer.");
            report.requiredAssets = new[] { "BrickAlbedo.jpg", "BrickNormal.png", "BrickARM.jpg", "Mark.png", "Wordmark.png" };
            foreach (string name in report.requiredAssets) RequiredTexture(name, name.Contains("Normal"), name.Contains("ARM"));
            brick = Material("Natural dark brick", new Color(.52f, .49f, .46f), .80f, 0);
            brick.mainTexture = RequiredTexture("BrickAlbedo.jpg");
            SetNormal(brick, RequiredTexture("BrickNormal.png", true), .65f);
            AddArm(brick, RequiredTexture("BrickARM.jpg", false, true), "Brick");
            rubber = Material("Dry graphite rubber", new Color(.058f, .063f, .067f), .90f, 0);
            matRubber = Material("Training mat graphite", new Color(.080f, .087f, .092f), .91f, 0);
            var micro = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Source/Premium/RubberMicroNormal.png");
            if (!micro) throw new InvalidOperationException("Existing rubber grain is missing; do not replace it with glossy flat flooring.");
            SetNormal(rubber, micro, .24f); SetNormal(matRubber, micro, .20f);
            rubber.SetTextureScale("_BumpMap", new Vector2(5, 5)); matRubber.SetTextureScale("_BumpMap", new Vector2(3, 3));
            blackSteel = Material("Black powder coated steel", new Color(.026f, .031f, .034f), .64f, .48f);
            steel = Material("Neutral brushed steel", new Color(.35f, .38f, .40f), .39f, .83f);
            green = Material("BRAXHORN green 8BFF3A", BrandGreen, .71f, .02f);
            white = Material("Warm white print", new Color(.86f, .87f, .85f), .82f, 0);
            vinyl = Material("Black protective vinyl", new Color(.023f, .026f, .030f), .68f, 0);
            wood = Material("Bench warm stained oak", new Color(.25f, .135f, .068f), .74f, 0);
            tile = Material("Annex charcoal porcelain", new Color(.18f, .19f, .20f), .77f, 0);
            plaster = Material("Annex graphite walls", new Color(.28f, .29f, .29f), .91f, 0);
            ceiling = Material("Annex neutral ceiling", new Color(.55f, .56f, .55f), .94f, 0);
            lit = Material("Neutral fixture diffuser", new Color(.78f, .78f, .74f), .8f, 0);
            lit.EnableKeyword("_EMISSION"); lit.SetColor("_EmissionColor", new Color(.7f, .7f, .66f));
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!font) throw new InvalidOperationException("Unity's built-in font is unavailable for controlled signage.");
        }

        Texture2D RequiredTexture(string name, bool normal = false, bool linear = false)
        {
            if (textures.TryGetValue(name, out var cached)) return cached;
            string path = TextureRoot + name;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Required reviewed surface is missing: " + path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !(normal || linear); importer.mipmapEnabled = true; importer.anisoLevel = 4;
            importer.maxTextureSize = 2048; importer.wrapMode = name == "Mark.png" || name == "Wordmark.png" ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.isReadable = name.Contains("ARM"); importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!texture) throw new InvalidOperationException("Unity failed to import reviewed surface: " + path);
            textures.Add(name, texture); return texture;
        }

        Material Material(string name, Color color, float roughness, float metallic)
        {
            var m = new Material(Shader.Find("Standard")) { name = name, color = color, enableInstancing = true };
            m.SetFloat("_Glossiness", 1 - roughness); m.SetFloat("_Metallic", metallic);
            AssetDatabase.CreateAsset(m, folder + "/" + Safe(name) + ".mat"); materials.Add(name, m); return m;
        }

        void SetNormal(Material m, Texture2D map, float strength)
        { m.SetTexture("_BumpMap", map); m.SetFloat("_BumpScale", strength); m.EnableKeyword("_NORMALMAP"); }

        void AddArm(Material m, Texture2D arm, string name)
        {
            var src = arm.GetPixels32(); var pixels = new Color32[src.Length]; var occlusion = new Color32[src.Length];
            for (int i = 0; i < src.Length; i++)
            { pixels[i] = new Color32(src[i].b, 0, 0, (byte)(255 - src[i].g)); occlusion[i] = new Color32(255, src[i].r, 255, 255); }
            var gloss = ControlTexture(name + "MetallicSmoothness", arm.width, arm.height, pixels);
            var ao = ControlTexture(name + "Occlusion", arm.width, arm.height, occlusion);
            m.SetTexture("_MetallicGlossMap", gloss); m.EnableKeyword("_METALLICGLOSSMAP"); m.SetFloat("_GlossMapScale", .62f);
            m.SetTexture("_OcclusionMap", ao); m.SetFloat("_OcclusionStrength", .82f);
        }

        Texture2D ControlTexture(string name, int width, int height, Color32[] pixels)
        {
            // Imported PNGs receive the existing WebGL ETC2 platform compression;
            // serialised raw RGBA Texture2D assets would waste mobile memory.
            string path = folder + "/" + name + ".png";
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            image.SetPixels32(pixels); image.Apply(false, false); File.WriteAllBytes(path, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = false; importer.mipmapEnabled = true; importer.isReadable = false;
            importer.maxTextureSize = 1024; importer.textureCompression = TextureImporterCompression.Compressed;
            importer.wrapMode = TextureWrapMode.Repeat; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        void CutAnnexDoorway()
        {
            // These three meshes are the audited right-hand wall, lower panels and trim.
            // True polygon clipping is required: a large wall triangle can span the
            // doorway while all three original vertices lie outside the opening.
            foreach (int index in new[] { 9, 10, 11 })
            {
                var t = FindSource(index); ClipOutBox(t.GetComponent<MeshFilter>(), AnnexPortal);
            }
            var candidates = solids.GetComponentsInChildren<BoxCollider>(true).Where(c =>
                Mathf.Abs(c.bounds.min.x + 21) < .05f && Mathf.Abs(c.bounds.max.x + 18) < .05f &&
                c.bounds.min.z < -15.9f && c.bounds.max.z > 13.9f && c.bounds.max.y > 8.4f).ToArray();
            if (candidates.Length != 1) throw new InvalidOperationException("Could not identify exactly one audited right-wall safety collider.");
            UnityEngine.Object.DestroyImmediate(candidates[0].gameObject);
            ColliderBox("Original side limit behind annex", new Vector3(-19.5f, 4.25f, -9.5f), new Vector3(3, 8.5f, 13));
            ColliderBox("Original side limit ahead of annex", new Vector3(-19.5f, 4.25f, 11), new Vector3(3, 8.5f, 6));
            ColliderBox("Right wall below doorway", new Vector3(-18.3f, 4.25f, .6f), new Vector3(.6f, 8.5f, 7.2f), true);
            ColliderBox("Right wall above doorway in plan", new Vector3(-18.3f, 4.25f, 7), new Vector3(.6f, 8.5f, 2), true);
            ColliderBox("Right wall doorway lintel", new Vector3(-18.3f, 5.6f, 5.1f), new Vector3(.6f, 5.8f, 1.8f), true);
            // Finished returns close the exposed shell cut without moving the opening.
            Box(hall, "Doorway return south", new Vector3(-18.3f, 1.35f, 4.18f), new Vector3(.70f, 2.7f, .09f), blackSteel, .012f);
            Box(hall, "Doorway return north", new Vector3(-18.3f, 1.35f, 6.02f), new Vector3(.70f, 2.7f, .09f), blackSteel, .012f);
            Box(hall, "Doorway head return", new Vector3(-18.3f, 2.72f, 5.1f), new Vector3(.70f, .10f, 1.92f), blackSteel, .012f);
        }

        void ClearObsoleteEquipment()
        {
            foreach (int id in Enumerable.Range(114, 19).Concat(new[] { 14, 15, 16, 143, 145, 146, 147, 154, 156, 157, 158, 159, 160 }))
            { var t = FindSource(id); var r = t.GetComponent<Renderer>(); if (r) r.enabled = false; }
            foreach (int id in new[] { 133, 135, 138, 140, 141, 142, 144 })
            {
                bool frame = id == 138 || id == 142;
                KeepTriangles(FindSource(id).GetComponent<MeshFilter>(), p => RingBounds.Contains(p) || (frame && BagFrameBounds.Contains(p)));
            }
            // Remove only movable old interior equipment physics. Structural walls,
            // original ring, original bag support and all alley obstacles remain.
            foreach (var c in solids.GetComponentsInChildren<BoxCollider>(true).ToArray())
            {
                var b = c.bounds;
                if (b.max.y > 3.4f || b.min.z >= 11 || b.max.z <= -13 || b.min.x <= -17.55f || b.max.x >= 17.55f) continue;
                if (c.name.Contains("ring") || c.name.Contains("rope") || c.name.Contains("stairs") || c.name.Contains("ground") || c.name.Contains("door")) continue;
                if (RingBounds.Contains(b.center) || BagFrameBounds.Contains(b.center)) continue;
                UnityEngine.Object.DestroyImmediate(c.gameObject);
            }
        }

        void FinishExistingSurfaces()
        {
            foreach (var r in gym.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!r.enabled) continue;
                var mats = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var name = mats[i] ? mats[i].name.Split('_') : Array.Empty<string>();
                    if (name.Length < 2 || name[0] != "BX" || !int.TryParse(name[1], out int id)) continue;
                    Material target = id == 9 || id == 35 || id == 36 ? brick : id == 10 ? vinyl : id == 11 ? green : id == 69 ? rubber : null;
                    if (target == null) continue;
                    // Exterior brick is a separate source material. Preserve its identity.
                    if (target == brick && mats.Length == 1)
                    {
                        FinishBrickSurface(r.GetComponent<MeshFilter>(), r, mats[i]);
                        changed = false; break;
                    }
                    mats[i] = target; changed = true;
                }
                if (changed)
                {
                    r.sharedMaterials = mats; r.gameObject.layer = 10;
                }
            }
            foreach (var m in Resources.FindObjectsOfTypeAll<Material>())
                if (m.name == "Green_vinyl") m.color = BrandGreen;
            // A deliberate base detail, not a continuous neon wash.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 17.93f;
                if (side > 0) Box(hall, "Left wall steel skirting", new Vector3(x, .095f, -1), new Vector3(.065f, .18f, 23.7f), blackSteel, .014f);
                else
                {
                    Box(hall, "Right skirting rear", new Vector3(x, .095f, -4.4f), new Vector3(.065f, .18f, 17.1f), blackSteel, .014f);
                    Box(hall, "Right skirting entry", new Vector3(x, .095f, 8.5f), new Vector3(.065f, .18f, 4.8f), blackSteel, .014f);
                }
            }
            Box(hall, "Entry threshold", new Vector3(6, .025f, 11.15f), new Vector3(1.6f, .045f, .32f), steel, .012f);
        }

        void FinishBrickSurface(MeshFilter filter, MeshRenderer renderer, Material original)
        {
            // World-sized courses avoid stretching a whole brick image over the
            // original baked wall UVs. Outside-facing shell triangles retain their
            // exact source material and UVs; no exterior facade is repainted.
            var source = filter.sharedMesh; var sourceVertices = ReadVertices(source); var ids = source.triangles;
            var vertices = new List<Vertex>(); var inner = new List<int>(); var outer = new List<int>();
            for (int i = 0; i < ids.Length; i += 3)
            {
                var center = filter.transform.TransformPoint((sourceVertices[ids[i]].p + sourceVertices[ids[i + 1]].p + sourceVertices[ids[i + 2]].p) / 3);
                var faceNormal = filter.transform.TransformDirection(sourceVertices[ids[i]].n);
                bool outside = Mathf.Abs(center.x) > 18.02f || center.z > 11.15f || center.z < -16.55f || (center.z < -13.25f && faceNormal.z < -.5f);
                var triangles = outside ? outer : inner;
                for (int k = 0; k < 3; k++)
                {
                    var vertex = sourceVertices[ids[i + k]];
                    if (!outside)
                    {
                        var p = filter.transform.TransformPoint(vertex.p); var n = filter.transform.TransformDirection(vertex.n);
                        vertex.uv = Mathf.Abs(n.y) > .7f ? new Vector2(p.x, p.z) / 2.2f :
                            Mathf.Abs(n.x) > Mathf.Abs(n.z) ? new Vector2(p.z, p.y) / 2.2f : new Vector2(p.x, p.y) / 2.2f;
                    }
                    triangles.Add(vertices.Count); vertices.Add(vertex);
                }
            }
            var mesh = new Mesh { name = source.name + " interior finish", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices.Select(v => v.p).ToList()); mesh.SetNormals(vertices.Select(v => v.n).ToList());
            mesh.SetUVs(0, vertices.Select(v => v.uv).ToList()); mesh.SetUVs(1, vertices.Select(v => v.uv2).ToList()); mesh.SetColors(vertices.Select(v => v.color).ToList());
            mesh.subMeshCount = outer.Count > 0 ? 2 : 1; mesh.SetTriangles(inner, 0);
            if (outer.Count > 0) mesh.SetTriangles(outer, 1);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            AssetDatabase.CreateAsset(mesh, folder + "/BrickUV_" + Safe(filter.name) + ".asset"); filter.sharedMesh = mesh;
            renderer.sharedMaterials = outer.Count > 0 ? new[] { brick, original } : new[] { brick }; renderer.gameObject.layer = 10;
        }

        void BuildTrainingMat()
        {
            const float x0 = -8.2f, x1 = 10.8f, z0 = -1f, z1 = 8.6f;
            Box(hall, "Open kickboxing mat", new Vector3((x0 + x1) / 2, .028f, (z0 + z1) / 2), new Vector3(x1 - x0, .05f, z1 - z0), matRubber, .012f);
            for (float x = x0 + 1.18f; x < x1; x += 1.18f)
                Box(hall, "Mat joining seam", new Vector3(x, .055f, (z0 + z1) / 2), new Vector3(.008f, .002f, z1 - z0), rubber, .0008f);
            for (float z = z0 + 1.2f; z < z1; z += 1.2f)
                Box(hall, "Mat joining seam", new Vector3((x0 + x1) / 2, .055f, z), new Vector3(x1 - x0, .002f, .008f), rubber, .0008f);
            foreach (float x in new[] { x0 + .08f, x1 - .08f })
                Box(hall, "Green mat perimeter", new Vector3(x, .058f, (z0 + z1) / 2), new Vector3(.105f, .003f, z1 - z0 - .08f), green, .001f);
            foreach (float z in new[] { z0 + .08f, z1 - .08f })
                Box(hall, "Green mat perimeter", new Vector3((x0 + x1) / 2, .058f, z), new Vector3(x1 - x0 - .08f, .003f, .105f), green, .001f);
            Decal(hall, "Original brand mark on mat", RequiredTexture("Mark.png"), new Vector3(1.2f, .061f, 2.9f), 2.25f, 1.65f, Vector3.up, Vector3.back, Color.white);
            Decal(hall, "Original wordmark on mat", RequiredTexture("Wordmark.png"), new Vector3(1.2f, .062f, 4.45f), 7.1f, 1.1f, Vector3.up, Vector3.back, Color.white);
            Text(hall, "KICKBOXING", new Vector3(1.2f, .067f, 5.4f), 5.4f, BrandGreen, Quaternion.Euler(90, 180, 0));
        }

        void BuildRingFinishing()
        {
            var previous = GameObject.Find("Premium ring and hall finishing");
            if (previous)
            {
                foreach (var r in previous.GetComponentsInChildren<MeshRenderer>(true))
                    if (r.sharedMaterial && (r.sharedMaterial.name == "Rope_ivory" || r.sharedMaterial.name == "Rope_green")) r.enabled = false;
            }
            foreach (int id in new[] { 136, 137 }) FindSource(id).GetComponent<Renderer>().enabled = false;
            const float cx = -9f, cz = -5.5f, half = 3.45f;
            float[] heights = { 1.22f, 1.60f, 1.98f, 2.36f };
            for (int i = 0; i < heights.Length; i++)
            {
                float y = heights[i]; var ropeMaterial = i == 0 || i == 3 ? green : white;
                var a = new Vector3(cx - half, y, cz - half); var b = new Vector3(cx + half, y, cz - half);
                var c = new Vector3(cx + half, y, cz + half); var d = new Vector3(cx - half, y, cz + half);
                Tube(hall, "Four-rope ring rear", a, b, .042f, ropeMaterial, 36, 10, .022f);
                Tube(hall, "Four-rope ring side", a, d, .042f, ropeMaterial, 36, 10, .022f);
                Tube(hall, "Four-rope ring side", b, c, .042f, ropeMaterial, 36, 10, .022f);
                Tube(hall, "Four-rope ring entry left", d, new Vector3(-9.85f, y, cz + half), .042f, ropeMaterial, 20, 10, .015f);
                Tube(hall, "Four-rope ring entry right", new Vector3(-8.15f, y, cz + half), c, .042f, ropeMaterial, 20, 10, .015f);
                foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                {
                    var corner = new Vector3(cx + sx * half, y, cz + sz * half);
                    var padCenter = corner - new Vector3(sx * .11f, 0, sz * .11f);
                    Box(hall, "Protected ring turnbuckle", padCenter, new Vector3(.28f, .19f, .28f), sx == sz ? green : vinyl, .055f);
                }
            }
            foreach (float along in new[] { -2.2f, 2.2f }) foreach (int side in new[] { -1, 1 })
            {
                Box(hall, "Four rope spacer", new Vector3(cx + along, 1.79f, cz + side * 3.5f), new Vector3(.055f, 1.22f, .018f), vinyl, .006f);
                Box(hall, "Four rope spacer", new Vector3(cx + side * 3.5f, 1.79f, cz + along), new Vector3(.018f, 1.22f, .055f), vinyl, .006f);
            }
            foreach (float x in new[] { cx - 4.2f, cx + 4.2f })
                Box(hall, "Apron green lower binding", new Vector3(x, .105f, cz), new Vector3(.018f, .055f, 8.35f), green, .005f);
            foreach (float z in new[] { cz - 4.2f, cz + 4.2f })
                Box(hall, "Apron green lower binding", new Vector3(cx, .105f, z), new Vector3(8.35f, .055f, .018f), green, .005f);
        }

        void BuildIndustrialCeiling()
        {
            foreach (float z in new[] { -10f, -4f, 2f, 8f })
            {
                Box(hall, "Steel beam lower flange", new Vector3(0, 7.18f, z), new Vector3(35.7f, .075f, .34f), blackSteel, .014f);
                Box(hall, "Steel beam web", new Vector3(0, 7.45f, z), new Vector3(35.7f, .47f, .09f), blackSteel, .014f);
                Box(hall, "Steel beam upper flange", new Vector3(0, 7.72f, z), new Vector3(35.7f, .075f, .34f), blackSteel, .014f);
                foreach (float x in new[] { -17.65f, 17.65f })
                {
                    Box(hall, "Beam connection plate", new Vector3(x, 7.44f, z), new Vector3(.16f, .68f, .52f), blackSteel, .014f);
                    foreach (float y in new[] { 7.21f, 7.66f }) foreach (float dz in new[] { -.17f, .17f })
                        Tube(hall, "Beam fixing bolt", new Vector3(x - .10f, y, z + dz), new Vector3(x + .10f, y, z + dz), .025f, steel, 1, 6);
                }
            }
            Tube(hall, "Main ventilation duct", new Vector3(16.1f, 7.1f, -12.4f), new Vector3(16.1f, 7.1f, 10.2f), .27f, blackSteel, 1, 20);
            for (float z = -11.8f; z < 10.1f; z += 2.2f)
            {
                Ring(hall, "Duct clamp", new Vector3(16.1f, 7.1f, z), .276f, .016f, steel, Quaternion.identity, 24, 6);
                Tube(hall, "Duct ceiling support", new Vector3(16.1f, 7.38f, z), new Vector3(16.1f, 8.2f, z), .012f, blackSteel, 1, 6);
            }
            int n = 0;
            foreach (float x in new[] { -9f, 6f, 13.8f, 16.1f })
                Box(hall, "Continuous fixture support rail", new Vector3(x, 7.27f, -1f), new Vector3(.09f, .10f, 21.8f), blackSteel, .012f);
            foreach (float x in new[] { -9f, 6f }) foreach (float z in new[] { -8f, -1f, 6f })
            {
                Tube(hall, "Pendant suspension", new Vector3(x, 6.0f, z), new Vector3(x, 7.27f, z), .011f, blackSteel, 1, 8);
                var anchor = Group(fixtures, "PendantLampAnchor_" + n++); anchor.position = new Vector3(x, 5.98f, z);
                AddRequiredModel("PendantLamp/PendantLamp.gltf", anchor, .64f, false);
                LocalLight("Neutral hall task light", new Vector3(x, 5.8f, z), 1.7f, 11f);
            }
            foreach (float z in new[] { -7f, 5f })
            {
                Box(hall, "Linear task light body", new Vector3(13.8f, 5.45f, z), new Vector3(.16f, .12f, 2.4f), blackSteel, .022f);
                Box(hall, "Linear neutral diffuser", new Vector3(13.8f, 5.38f, z), new Vector3(.11f, .012f, 2.30f), lit, .004f);
                foreach (float dz in new[] { -.85f, .85f }) Tube(hall, "Linear light cable", new Vector3(13.8f, 5.5f, z + dz), new Vector3(13.8f, 7.3f, z + dz), .008f, blackSteel, 1, 6);
            }
        }

        void BuildBillboardMount()
        {
            var center = new Vector3(-3.4f, 5.35f, -12.68f);
            Box(hall, "Billboard recessed backing", center, new Vector3(21.4f, 4.60f, .10f), blackSteel, .025f);
            foreach (float x in new[] { center.x - 10.76f, center.x + 10.76f })
                Box(hall, "Billboard vertical frame", new Vector3(x, center.y, center.z + .04f), new Vector3(.11f, 4.76f, .14f), blackSteel, .017f);
            foreach (float y in new[] { center.y - 2.36f, center.y + 2.36f })
                Box(hall, "Billboard horizontal frame", new Vector3(center.x, y, center.z + .04f), new Vector3(21.62f, .11f, .14f), blackSteel, .017f);
            var anchor = Group(root, "BillboardArtAnchor"); anchor.position = center + Vector3.forward * .065f;
            anchor.rotation = Quaternion.Euler(0, 180, 0); anchor.localScale = new Vector3(21.4f, 4.6f, 1);
            foreach (float x in new[] { -11f, -3.4f, 4.2f })
            {
                Tube(hall, "Billboard lamp bracket", new Vector3(x, 7.85f, -12.8f), new Vector3(x, 7.85f, -12.15f), .035f, blackSteel, 1, 10);
                Box(hall, "Billboard wash lamp", new Vector3(x, 7.78f, -12.12f), new Vector3(.52f, .18f, .18f), blackSteel, .035f);
                Box(hall, "Billboard lamp diffuser", new Vector3(x, 7.68f, -12.12f), new Vector3(.43f, .018f, .13f), lit, .006f);
            }
            // Source logos remain separate controlled assets; the wall art is not
            // installed until its owner has approved the flat image.
            Decal(hall, "Original right wall brand", RequiredTexture("Mark.png"), new Vector3(-17.89f, 5.1f, 0), 2.25f, 2.25f, Vector3.right, Vector3.up, Color.white);
            Decal(hall, "Original right wall wordmark", RequiredTexture("Wordmark.png"), new Vector3(-17.88f, 3.55f, 0), 4.8f, .78f, Vector3.right, Vector3.up, Color.white);
        }

        void BuildAnnex()
        {
            Wall("Annex west exterior", new Vector3(-25.35f, 1.55f, 2.5f), new Vector3(.3f, 3.1f, 11), brick);
            Wall("Annex north exterior", new Vector3(-22.05f, 1.55f, 7.85f), new Vector3(6.9f, 3.1f, .3f), brick);
            Wall("Annex south exterior", new Vector3(-22.05f, 1.55f, -2.85f), new Vector3(6.9f, 3.1f, .3f), brick);
            Wall("Annex complete ceiling", new Vector3(-22.05f, 3.21f, 2.5f), new Vector3(6.9f, .22f, 11), ceiling);
            Wall("Annex floor", new Vector3(-22.05f, -.075f, 2.5f), new Vector3(6.9f, .15f, 11), tile);
            // The dogleg is architectural. Door leaf colliders are intentionally
            // excluded from the privacy proof, so opening all doors cannot expose WC.
            Wall("Dogleg sight screen south", new Vector3(-20.45f, 1.55f, .75f), new Vector3(.18f, 3.1f, 6.9f), plaster);
            Wall("Dogleg sight screen north", new Vector3(-20.45f, 1.55f, 6.85f), new Vector3(.18f, 3.1f, 1.7f), plaster);
            Wall("Separate locker and WC wall", new Vector3(-23.675f, 1.55f, 1.70f), new Vector3(3.05f, 3.1f, .18f), plaster);
            float[] ends = { -2.7f, -1.75f, -.55f, 2.8f, 4.0f, 7.7f };
            for (int i = 0; i < ends.Length; i += 2)
                Wall("Rooms corridor wall", new Vector3(-22.175f, 1.55f, (ends[i] + ends[i + 1]) / 2), new Vector3(.15f, 3.1f, ends[i + 1] - ends[i]), plaster);
            foreach (var gap in new[] { new Vector2(-1.75f, -.55f), new Vector2(2.8f, 4.0f) })
                Wall("Room doorway lintel", new Vector3(-22.175f, 2.75f, (gap.x + gap.y) / 2), new Vector3(.15f, .7f, gap.y - gap.x), plaster);
            for (float z = -2.4f; z < 7.7f; z += .6f)
                Box(annex, "Porcelain grout joint", new Vector3(-22.0f, .003f, z), new Vector3(6.1f, .002f, .006f), rubber, .0005f);
            for (float x = -24.9f; x < -18.7f; x += .6f)
                Box(annex, "Porcelain grout joint", new Vector3(x, .003f, 2.5f), new Vector3(.006f, .002f, 10.3f), rubber, .0005f);
            Box(annex, "Annex west skirting", new Vector3(-25.17f, .07f, 2.5f), new Vector3(.025f, .13f, 10.1f), blackSteel, .005f);
            for (int i = 0; i < ends.Length; i += 2)
                Box(annex, "Room wall skirting", new Vector3(-22.27f, .07f, (ends[i] + ends[i + 1]) / 2), new Vector3(.025f, .13f, ends[i + 1] - ends[i]), blackSteel, .005f);
            foreach (var span in new[] { new Vector2(-2.7f, 4.2f), new Vector2(6f, 7.7f) })
                Box(annex, "Dogleg screen skirting", new Vector3(-20.55f, .07f, (span.x + span.y) / 2), new Vector3(.025f, .13f, span.y - span.x), blackSteel, .005f);
            Door("gym-backrooms", "ZAPLECZE", -18.28f, 4.2f, 6f, 2.7f);
            Door("gym-locker", "SZATNIA", -22.175f, 2.8f, 4f, 2.4f);
            Door("gym-wc", "TOALETA", -22.175f, -1.75f, -.55f, 2.4f);
            Text(annex, "SZATNIA  /  WC", new Vector3(-20.33f, 2.5f, 3.5f), .95f, Color.white, Quaternion.Euler(0, -90, 0));
            BuildLockers();
            BuildSanitaryAnchors();
            foreach (var p in new[] { new Vector3(-19.4f, 2.94f, 5.1f), new Vector3(-21.3f, 2.94f, 1.8f), new Vector3(-23.7f, 2.94f, 4.9f), new Vector3(-23.7f, 2.94f, -.5f) })
            {
                Box(annex, "Annex ceiling luminaire", p, new Vector3(.62f, .07f, .36f), blackSteel, .025f);
                Box(annex, "Annex neutral diffuser", p + Vector3.down * .041f, new Vector3(.55f, .012f, .29f), lit, .008f);
                LocalLight("Neutral annex room light", p + Vector3.down * .12f, 1.1f, 5.3f);
            }
        }

        void BuildLockers()
        {
            for (int i = 0; i < 8; i++)
            {
                float z = 2.28f + .70f * i;
                Box(annex, "Locker folded steel shell " + (i + 1), new Vector3(-24.92f, 1.04f, z), new Vector3(.52f, 1.92f, .66f), blackSteel, .024f);
                Box(annex, "Locker inset door", new Vector3(-24.646f, 1.04f, z), new Vector3(.025f, 1.82f, .602f), vinyl, .014f);
                Box(annex, "Locker top green label", new Vector3(-24.628f, 1.78f, z), new Vector3(.005f, .035f, .34f), green, .001f);
                foreach (float y in new[] { .22f, .28f, .34f, 1.53f, 1.59f, 1.65f })
                    Box(annex, "Locker vent opening", new Vector3(-24.625f, y, z), new Vector3(.004f, .016f, .33f), rubber, .004f);
                foreach (float y in new[] { .38f, 1.65f })
                    Tube(annex, "Locker hinge", new Vector3(-24.615f, y, z + .27f), new Vector3(-24.615f, y + .09f, z + .27f), .018f, steel, 1, 8);
                Tube(annex, "Locker recessed pull", new Vector3(-24.59f, .99f, z - .21f), new Vector3(-24.59f, 1.13f, z - .21f), .013f, steel, 1, 10);
                Text(annex, (i + 1).ToString("00"), new Vector3(-24.612f, 1.87f, z), .12f, Color.white, Quaternion.Euler(0, -90, 0));
            }
            ColliderBox("Locker bank collision", new Vector3(-24.92f, 1.04f, 4.73f), new Vector3(.56f, 1.94f, 5.58f));
            Bench(annex, new Vector3(-22.67f, 0, 5.8f), 2.8f, false);
            foreach (float z in new[] { 4.68f, 5.35f, 6.02f, 6.69f })
            {
                Box(annex, "Hook backing plate", new Vector3(-22.29f, 1.68f, z), new Vector3(.025f, .12f, .11f), blackSteel, .014f);
                Tube(annex, "Locker coat hook stem", new Vector3(-22.31f, 1.68f, z), new Vector3(-22.41f, 1.62f, z), .012f, steel, 4, 8);
                Tube(annex, "Locker coat hook upturn", new Vector3(-22.41f, 1.62f, z), new Vector3(-22.44f, 1.68f, z), .012f, steel, 4, 8);
            }
        }

        void BuildSanitaryAnchors()
        {
            var wc = Group(annex, "WCFixtureAnchor"); wc.position = new Vector3(-24.4f, 0, -1.92f);
            AddRequiredModel("Toilet.glb", wc, .74f, true);
            ColliderBox("WC porcelain collision", new Vector3(-24.4f, .39f, -1.92f), new Vector3(.68f, .78f, .90f));
            var sink = Group(annex, "SinkFixtureAnchor"); sink.position = new Vector3(-24.80f, 0, .45f); sink.rotation = Quaternion.Euler(0, 90, 0);
            AddRequiredModel("Sink.glb", sink, .70f, true);
            ColliderBox("Washbasin collision", new Vector3(-24.88f, .66f, .45f), new Vector3(.58f, 1.12f, .68f));
            // Original manufactured fixtures are supplied by the asset pass. These
            // small fittings are modelled with curved profiles and remain separate.
            Box(annex, "Soap dispenser body", new Vector3(-25.12f, 1.19f, 1.13f), new Vector3(.15f, .22f, .12f), steel, .035f);
            Tube(annex, "Dispenser pump", new Vector3(-25.04f, 1.31f, 1.13f), new Vector3(-24.97f, 1.31f, 1.13f), .012f, blackSteel, 1, 10);
            Tube(annex, "Paper holder axle", new Vector3(-25.11f, .66f, -1.20f), new Vector3(-25.11f, .66f, -.99f), .012f, steel, 1, 10);
            Tube(annex, "Paper roll", new Vector3(-25.10f, .66f, -1.18f), new Vector3(-25.10f, .66f, -1.02f), .062f, white, 1, 24);
            Tube(annex, "Paper holder wall arm", new Vector3(-25.19f, .66f, -1.20f), new Vector3(-25.10f, .66f, -1.20f), .012f, steel, 1, 10);
            var mirror = Group(annex, "WCMirrorAnchor"); mirror.position = new Vector3(-25.13f, 1.55f, .45f); mirror.rotation = Quaternion.Euler(0, 90, 0); mirror.localScale = new Vector3(.63f, .76f, 1);
            Box(annex, "WC mirror frame", new Vector3(-25.14f, 1.55f, .45f), new Vector3(.028f, .82f, .69f), blackSteel, .018f);
        }

        void BuildReception()
        {
            var p = new Vector3(-2.6f, 0, 9.65f);
            Box(hall, "Reception plinth", p + new Vector3(0, .08f, 0), new Vector3(3.25f, .16f, .82f), blackSteel, .025f);
            Box(hall, "Reception dark front", p + new Vector3(0, .58f, -.32f), new Vector3(3.35f, .94f, .13f), blackSteel, .035f);
            foreach (float x in new[] { -1.59f, 1.59f })
                Box(hall, "Reception end panel", p + new Vector3(x, .57f, 0), new Vector3(.12f, .95f, .79f), vinyl, .023f);
            Box(hall, "Reception oak transaction top", p + new Vector3(0, 1.08f, 0), new Vector3(3.55f, .065f, .94f), wood, .022f);
            Box(hall, "Reception green inset", p + new Vector3(0, .34f, -.392f), new Vector3(2.85f, .035f, .012f), green, .004f);
            Decal(hall, "Reception original wordmark", RequiredTexture("Wordmark.png"), p + new Vector3(0, .71f, -.395f), 2.05f, .31f, Vector3.back, Vector3.up, Color.white);
            Box(hall, "Reception payment terminal stand", p + new Vector3(.95f, 1.14f, -.12f), new Vector3(.13f, .07f, .15f), blackSteel, .02f);
            var terminal = Box(hall, "Reception payment terminal", p + new Vector3(.95f, 1.23f, -.12f), new Vector3(.12f, .035f, .19f), vinyl, .018f); terminal.localRotation = Quaternion.Euler(-25, 0, 0);
            ColliderBox("Reception collision", p + new Vector3(0, .55f, 0), new Vector3(3.55f, 1.10f, .94f));
            Bench(hall, new Vector3(-14.9f, 0, 8.7f), 2.45f, true);
        }

        void Bench(Transform parent, Vector3 p, float length, bool alongX)
        {
            for (int i = 0; i < 4; i++)
            {
                var local = new Vector3(-.18f + i * .12f, .46f, 0);
                var size = new Vector3(.095f, .045f, length);
                if (alongX) { local = new Vector3(0, .46f, local.x); size = new Vector3(length, .045f, .095f); }
                Box(parent, "Bench rounded oak slat", p + local, size, wood, .014f);
            }
            foreach (float along in new[] { -length * .34f, length * .34f })
            {
                foreach (float across in new[] { -.17f, .17f })
                {
                    var pos = alongX ? new Vector3(along, .23f, across) : new Vector3(across, .23f, along);
                    Box(parent, "Bench steel leg", p + pos, new Vector3(.045f, .43f, .045f), blackSteel, .008f);
                    Box(parent, "Bench rubber foot", p + new Vector3(pos.x, .025f, pos.z), new Vector3(.068f, .035f, .068f), rubber, .012f);
                }
                var cross = alongX ? new Vector3(along, .405f, 0) : new Vector3(0, .405f, along);
                Box(parent, "Bench seat support", p + cross, alongX ? new Vector3(.045f, .05f, .46f) : new Vector3(.46f, .05f, .045f), blackSteel, .008f);
            }
            ColliderBox("Bench collision", p + Vector3.up * .245f, alongX ? new Vector3(length, .49f, .48f) : new Vector3(.48f, .49f, length));
        }

        void BuildMirrorAnchor()
        {
            var anchor = Group(root, "MainMirrorAnchor"); anchor.position = new Vector3(17.86f, 1.88f, -3.25f);
            anchor.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up); anchor.localScale = new Vector3(14.5f, 2.90f, 1);
            foreach (float z in new[] { -10.55f, -6.9f, -3.25f, .4f, 4.05f })
                Box(hall, "Mirror steel vertical frame", new Vector3(17.88f, 1.88f, z), new Vector3(.055f, 3.02f, .042f), blackSteel, .009f);
            foreach (float y in new[] { .39f, 3.37f })
                Box(hall, "Mirror steel horizontal frame", new Vector3(17.88f, y, -3.25f), new Vector3(.055f, .055f, 14.62f), blackSteel, .009f);
            Box(hall, "Mirror green base detail", new Vector3(17.83f, .34f, -3.25f), new Vector3(.020f, .025f, 14.45f), green, .005f);
        }

        void Door(string id, string label, float x, float z0, float z1, float height)
        {
            float width = z1 - z0; var doorRoot = Group(root, "Door " + id);
            doorRoot.position = new Vector3(x, 0, (z0 + z1) / 2); doorRoot.rotation = Quaternion.Euler(0, 90, 0);
            var leaf = Group(doorRoot, "Opaque door leaf"); leaf.localPosition = new Vector3(-width / 2, 0, 0);
            Box(leaf, "Opaque door panel", new Vector3(width / 2, height / 2, 0), new Vector3(width - .045f, height - .025f, .065f), blackSteel, .018f);
            Box(leaf, "Door kick plate", new Vector3(width / 2, .17f, -.039f), new Vector3(width - .12f, .23f, .014f), steel, .008f);
            Box(leaf, "Door green identity line", new Vector3(width / 2, height - .30f, -.041f), new Vector3(width - .18f, .025f, .008f), green, .003f);
            foreach (float side in new[] { -.065f, .065f })
            {
                Tube(leaf, "Door lever rosette", new Vector3(width - .16f, 1.08f, side - .012f), new Vector3(width - .16f, 1.08f, side + .012f), .038f, steel, 1, 16);
                Tube(leaf, "Door lever", new Vector3(width - .16f, 1.08f, side), new Vector3(width - .30f, 1.08f, side), .012f, steel, 1, 12);
            }
            foreach (float y in new[] { .25f, height - .25f }) Tube(leaf, "Door hinge barrel", new Vector3(.017f, y, .035f), new Vector3(.017f, y + .11f, .035f), .021f, steel, 1, 12);
            var col = leaf.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(width / 2, height / 2, 0); col.size = new Vector3(width - .035f, height, .09f);
            foreach (float z in new[] { z0 - .045f, z1 + .045f }) Box(annex, "Door steel architrave", new Vector3(x + .085f, height / 2, z), new Vector3(.075f, height + .07f, .09f), blackSteel, .012f);
            Box(annex, "Door architrave head", new Vector3(x + .085f, height + .04f, (z0 + z1) / 2), new Vector3(.075f, .09f, width + .18f), blackSteel, .012f);
            Text(annex, label, new Vector3(x + .13f, height + .24f, (z0 + z1) / 2), Mathf.Min(1.35f, width + .15f), Color.white, Quaternion.Euler(0, -90, 0));
            var component = doorRoot.gameObject.AddComponent<BraxCityDoor>(); component.id = id; component.label = label;
            component.leaves = new[] { leaf }; component.sides = new[] { 1f }; newDoors.Add(component);
            // Batch within the moving leaf only; do not bake a door into static walls.
            BatchByMaterial(leaf);
        }

        void Wall(string name, Vector3 center, Vector3 size, Material material)
        { Box(annex, name, center, size, material, .012f); ColliderBox(name + " collision", center, size, true); }

        BoxCollider ColliderBox(string name, Vector3 center, Vector3 size, bool privacy = false)
        {
            var t = Group(physics, name); t.position = center; var c = t.gameObject.AddComponent<BoxCollider>(); c.size = size;
            if (privacy) privacyWalls.Add(new Bounds(center, size)); return c;
        }

        void LocalLight(string name, Vector3 p, float intensity, float range)
        {
            var t = Group(fixtures, name); t.position = p; var l = t.gameObject.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, .975f, .94f); l.intensity = intensity; l.range = range;
            l.shadows = LightShadows.None; l.cullingMask = 1 << 10; l.renderMode = LightRenderMode.Auto;
        }

        void AddRequiredModel(string file, Transform anchor, float width, bool floor)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelRoot + file) ?? AssetDatabase.LoadAllAssetsAtPath(ModelRoot + file).OfType<GameObject>().FirstOrDefault();
            if (!source) throw new InvalidOperationException("Reviewed real fixture is required, with no primitive fallback: " + ModelRoot + file);
            var o = UnityEngine.Object.Instantiate(source); o.name = "Reviewed source " + file; o.transform.SetParent(anchor, false);
            var rs = o.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) throw new InvalidOperationException("Source model contains no rendered meshes: " + file);
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float extent = Mathf.Max(b.size.x, b.size.z); if (extent < .001f) throw new InvalidOperationException("Source model has invalid scale: " + file);
            o.transform.localScale *= width / extent;
            b = rs[0].bounds; foreach (var r in rs) { r.gameObject.layer = 10; r.shadowCastingMode = ShadowCastingMode.Off; b.Encapsulate(r.bounds); }
            o.transform.position += anchor.position - new Vector3(b.center.x, floor ? b.min.y : b.center.y, b.center.z);
        }

        void CheckPrivacy()
        {
            // Enumerated sight lines to the actual sanitary bay, with NO door leaf
            // included as an occluder. Cameras above the annex are stopped by its roof.
            var targets = new List<Vector3>();
            foreach (float x in new[] { -24.8f, -24.0f, -23.0f }) foreach (float z in new[] { -2.2f, -1.2f, .5f }) foreach (float y in new[] { .55f, 1.15f, 1.8f }) targets.Add(new Vector3(x, y, z));
            for (float x = -17.3f; x <= 17.3f; x += 2.5f) for (float z = -12f; z <= 10.5f; z += 2.5f) foreach (float y in new[] { 1.3f, 2.15f, 3.8f, 6f })
            {
                var source = new Vector3(x, y, z);
                foreach (var target in targets)
                {
                    report.privacyRays++;
                    if (privacyWalls.Any(w => SegmentIntersects(w, source, target))) report.privacyBlocked++;
                }
            }
            report.privacyWithAllDoorsOpen = report.privacyRays > 0 && report.privacyRays == report.privacyBlocked;
            if (!report.privacyWithAllDoorsOpen) throw new InvalidOperationException("Architectural WC privacy check failed with all door leaves excluded: " + report.privacyBlocked + "/" + report.privacyRays);
        }

        static bool SegmentIntersects(Bounds b, Vector3 a, Vector3 c)
        {
            Vector3 d = c - a; float low = .0001f, high = .9999f;
            for (int axis = 0; axis < 3; axis++)
            {
                if (Mathf.Abs(d[axis]) < .00001f) { if (a[axis] < b.min[axis] || a[axis] > b.max[axis]) return false; continue; }
                float p = (b.min[axis] - a[axis]) / d[axis], q = (b.max[axis] - a[axis]) / d[axis];
                low = Mathf.Max(low, Mathf.Min(p, q)); high = Mathf.Min(high, Mathf.Max(p, q)); if (low > high) return false;
            }
            return low <= high;
        }

        Transform Group(Transform parent, string name)
        { var t = new GameObject(name).transform; if (parent) t.SetParent(parent, false); t.gameObject.layer = 10; return t; }

        static string Safe(string name) { return new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()); }

        Transform MeshObject(Transform parent, string name, Mesh mesh, Material material)
        {
            mesh.name = name; mesh.RecalculateBounds();
            if (mesh.tangents.Length != mesh.vertexCount) mesh.RecalculateTangents();
            AssetDatabase.CreateAsset(mesh, folder + "/Mesh_" + (assetNumber++).ToString("D4") + ".asset");
            var t = Group(parent, name); t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = t.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterial = material; r.shadowCastingMode = ShadowCastingMode.Off;
            report.generatedMeshes++; report.generatedTriangles += mesh.triangles.Length / 3; return t;
        }

        Transform Box(Transform parent, string name, Vector3 p, Vector3 size, Material material, float radius)
        {
            radius = Mathf.Min(radius, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * .47f);
            var vs = new List<Vector3>(); var ns = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            var half = size / 2; var inner = half - Vector3.one * radius;
            foreach (var n in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
            {
                var u = Mathf.Abs(n.y) > .5f ? Vector3.right : Vector3.Cross(Vector3.up, n).normalized; var v = Vector3.Cross(n, u);
                float hu = Mathf.Abs(Vector3.Dot(half, u)), hv = Mathf.Abs(Vector3.Dot(half, v));
                float[] us = { -hu, -hu + radius * .293f, -hu + radius, hu - radius, hu - radius * .293f, hu };
                float[] ys = { -hv, -hv + radius * .293f, -hv + radius, hv - radius, hv - radius * .293f, hv };
                int start = vs.Count;
                for (int y = 0; y < 6; y++) for (int x = 0; x < 6; x++)
                {
                    Vector3 q = Vector3.Scale(n, half) + u * us[x] + v * ys[y];
                    Vector3 clamp = new Vector3(Mathf.Clamp(q.x, -inner.x, inner.x), Mathf.Clamp(q.y, -inner.y, inner.y), Mathf.Clamp(q.z, -inner.z, inner.z));
                    var normal = (q - clamp).normalized; vs.Add(clamp + normal * radius); ns.Add(normal);
                    uv.Add(new Vector2(us[x], ys[y]) / (material == brick ? 2.2f : 1f));
                }
                for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++) { int a = start + y * 6 + x; triangles.AddRange(new[] { a, a + 1, a + 6, a + 1, a + 7, a + 6 }); }
            }
            var mesh = new Mesh(); mesh.SetVertices(vs); mesh.SetNormals(ns); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            var result = MeshObject(parent, name, mesh, material); result.localPosition = p; return result;
        }

        Transform Tube(Transform parent, string name, Vector3 a, Vector3 b, float radius, Material material, int segments, int sides, float sag = 0)
        {
            var vs = new List<Vector3>(); var ns = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            Vector3 axis = (b - a).normalized; Vector3 side = Vector3.Cross(axis, Mathf.Abs(axis.y) > .9f ? Vector3.right : Vector3.up).normalized; Vector3 up = Vector3.Cross(side, axis).normalized;
            for (int j = 0; j <= segments; j++) for (int k = 0; k <= sides; k++)
            {
                float t = (float)j / segments, u = (float)k / sides, angle = u * Mathf.PI * 2;
                var normal = side * Mathf.Cos(angle) + up * Mathf.Sin(angle); vs.Add(Vector3.Lerp(a, b, t) - Vector3.up * (Mathf.Sin(t * Mathf.PI) * sag) + normal * radius); ns.Add(normal); uv.Add(new Vector2(u, t * Vector3.Distance(a, b)));
                if (j < segments && k < sides) { int q = j * (sides + 1) + k; triangles.AddRange(new[] { q, q + sides + 1, q + 1, q + 1, q + sides + 1, q + sides + 2 }); }
            }
            // Close visible cylinder ends (handles, rods and tube caps).
            for (int end = 0; end < 2; end++)
            {
                int center = vs.Count; var p = end == 0 ? a : b; var n = end == 0 ? -axis : axis; vs.Add(p); ns.Add(n); uv.Add(Vector2.one * .5f);
                for (int k = 0; k <= sides; k++) { float angle = k * Mathf.PI * 2 / sides; var radial = side * Mathf.Cos(angle) + up * Mathf.Sin(angle); vs.Add(p + radial * radius); ns.Add(n); uv.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .5f + Vector2.one * .5f); }
                for (int k = 0; k < sides; k++) if (end == 0) triangles.AddRange(new[] { center, center + k + 1, center + k + 2 }); else triangles.AddRange(new[] { center, center + k + 2, center + k + 1 });
            }
            var mesh = new Mesh(); mesh.SetVertices(vs); mesh.SetNormals(ns); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); return MeshObject(parent, name, mesh, material);
        }

        void Ring(Transform parent, string name, Vector3 center, float major, float minor, Material material, Quaternion rotation, int around, int sides)
        {
            var vs = new List<Vector3>(); var ns = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            for (int j = 0; j <= around; j++) for (int k = 0; k <= sides; k++)
            {
                float a = 2 * Mathf.PI * j / around, b = 2 * Mathf.PI * k / sides;
                var n = new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(a) * Mathf.Cos(b), Mathf.Sin(b));
                vs.Add(center + rotation * (new Vector3(Mathf.Cos(a) * major, Mathf.Sin(a) * major, 0) + n * minor)); ns.Add(rotation * n); uv.Add(new Vector2((float)j / around, (float)k / sides));
                if (j < around && k < sides) { int q = j * (sides + 1) + k; tris.AddRange(new[] { q, q + sides + 1, q + 1, q + 1, q + sides + 1, q + sides + 2 }); }
            }
            var mesh = new Mesh(); mesh.SetVertices(vs); mesh.SetNormals(ns); mesh.SetUVs(0, uv); mesh.SetTriangles(tris, 0); MeshObject(parent, name, mesh, material);
        }

        void Decal(Transform parent, string name, Texture2D image, Vector3 p, float width, float height, Vector3 normal, Vector3 up, Color tint)
        {
            float fit = Mathf.Min(width / image.width, height / image.height); width = image.width * fit; height = image.height * fit;
            var material = new Material(Shader.Find("Unlit/Transparent")) { name = name, mainTexture = image };
            if (material.HasProperty("_Color")) material.color = tint;
            AssetDatabase.CreateAsset(material, folder + "/Decal_" + Safe(name) + ".mat");
            var mesh = new Mesh { vertices = new[] { new Vector3(-width / 2, -height / 2, 0), new Vector3(width / 2, -height / 2, 0), new Vector3(width / 2, height / 2, 0), new Vector3(-width / 2, height / 2, 0) }, uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up }, triangles = new[] { 0, 2, 1, 0, 3, 2 } };
            mesh.RecalculateNormals(); var t = MeshObject(parent, name, mesh, material); t.localPosition = p; t.localRotation = Quaternion.LookRotation(-normal, up);
        }

        void Text(Transform parent, string value, Vector3 p, float width, Color color, Quaternion rotation)
        {
            var t = Group(parent, "Sign " + value); t.localPosition = p;
            var tm = t.gameObject.AddComponent<TextMesh>(); tm.font = font; tm.text = value; tm.fontSize = 64; tm.characterSize = .12f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = color;
            var r = t.GetComponent<MeshRenderer>(); r.sharedMaterial = font.material; r.shadowCastingMode = ShadowCastingMode.Off;
            font.RequestCharactersInTexture(value, 64, FontStyle.Normal);
            // Measure before rotating wall signs, whose world X bounds can be zero.
            float advance = 0; foreach (char c in value) if (font.GetCharacterInfo(c, out var glyph, 64)) advance += glyph.advance;
            float actualWidth = r.localBounds.size.x;
            if (actualWidth <= .00001f) actualWidth = advance * tm.characterSize / 10f;
            if (actualWidth > 0) t.localScale = Vector3.one * (width / actualWidth);
            t.localRotation = rotation;
        }

        void BatchByMaterial(Transform group)
        {
            // Reviewed imported fixtures keep all their original material slots.
            var filters = group.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh && f.sharedMesh.subMeshCount == 1 &&
                AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(folder + "/", StringComparison.Ordinal) &&
                f.GetComponent<MeshRenderer>() && f.GetComponent<MeshRenderer>().enabled && f.GetComponent<MeshRenderer>().sharedMaterials.Length == 1).ToArray();
            foreach (var set in filters.GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial))
            {
                var items = set.ToArray(); if (items.Length < 2 || !set.Key) continue;
                var combined = new Mesh { indexFormat = IndexFormat.UInt32 };
                combined.CombineMeshes(items.Select(f => new CombineInstance { mesh = f.sharedMesh, transform = group.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray(), true, true);
                MeshObject(group, "Batched " + set.Key.name, combined, set.Key);
                foreach (var f in items) UnityEngine.Object.DestroyImmediate(f.gameObject);
            }
        }

        struct Vertex
        {
            public Vector3 p, n; public Vector4 tangent; public Vector2 uv, uv2; public Color color;
            public static Vertex Mix(Vertex a, Vertex b, float t) { return new Vertex { p = Vector3.LerpUnclamped(a.p, b.p, t), n = Vector3.LerpUnclamped(a.n, b.n, t).normalized, tangent = Vector4.LerpUnclamped(a.tangent, b.tangent, t), uv = Vector2.LerpUnclamped(a.uv, b.uv, t), uv2 = Vector2.LerpUnclamped(a.uv2, b.uv2, t), color = Color.LerpUnclamped(a.color, b.color, t) }; }
        }

        static Vertex[] ReadVertices(Mesh mesh)
        {
            var p = mesh.vertices; var n = mesh.normals; var t = mesh.tangents; var uv = mesh.uv; var uv2 = mesh.uv2; var c = mesh.colors; var result = new Vertex[p.Length];
            for (int i = 0; i < p.Length; i++) result[i] = new Vertex { p = p[i], n = n.Length == p.Length ? n[i] : Vector3.up, tangent = t.Length == p.Length ? t[i] : new Vector4(1, 0, 0, 1), uv = uv.Length == p.Length ? uv[i] : Vector2.zero, uv2 = uv2.Length == p.Length ? uv2[i] : Vector2.zero, color = c.Length == p.Length ? c[i] : Color.white };
            return result;
        }

        void KeepTriangles(MeshFilter filter, Func<Vector3, bool> keep)
        {
            var source = filter.sharedMesh; var vertices = ReadVertices(source); var ids = source.triangles; var result = new List<Vertex>();
            for (int i = 0; i < ids.Length; i += 3)
            {
                var center = filter.transform.TransformPoint((vertices[ids[i]].p + vertices[ids[i + 1]].p + vertices[ids[i + 2]].p) / 3);
                if (keep(center)) { result.Add(vertices[ids[i]]); result.Add(vertices[ids[i + 1]]); result.Add(vertices[ids[i + 2]]); }
            }
            ReplaceSourceMesh(filter, result, ids.Length / 3);
        }

        void ClipOutBox(MeshFilter filter, Bounds box)
        {
            var source = filter.sharedMesh; var vertices = ReadVertices(source); var ids = source.triangles; var result = new List<Vertex>();
            for (int i = 0; i < ids.Length; i += 3)
            {
                var remaining = new List<Vertex> { vertices[ids[i]], vertices[ids[i + 1]], vertices[ids[i + 2]] };
                for (int plane = 0; plane < 6 && remaining.Count > 0; plane++)
                {
                    int axis = plane / 2; bool lower = (plane % 2) == 0; var inside = new List<Vertex>(); var outside = new List<Vertex>();
                    for (int k = 0; k < remaining.Count; k++)
                    {
                        var a = remaining[k]; var b = remaining[(k + 1) % remaining.Count];
                        float da = lower ? filter.transform.TransformPoint(a.p)[axis] - box.min[axis] : box.max[axis] - filter.transform.TransformPoint(a.p)[axis];
                        float db = lower ? filter.transform.TransformPoint(b.p)[axis] - box.min[axis] : box.max[axis] - filter.transform.TransformPoint(b.p)[axis];
                        bool ai = da >= -.00001f, bi = db >= -.00001f;
                        if (ai) inside.Add(a); else outside.Add(a);
                        if (ai != bi) { var intersection = Vertex.Mix(a, b, da / (da - db)); inside.Add(intersection); outside.Add(intersection); }
                    }
                    Fan(outside, result); remaining = inside;
                }
                // The final remaining polygon lies within all six planes and is removed.
            }
            ReplaceSourceMesh(filter, result, ids.Length / 3);
        }

        static void Fan(List<Vertex> polygon, List<Vertex> result)
        {
            for (int i = 1; i < polygon.Count - 1; i++)
                if (Vector3.Cross(polygon[i].p - polygon[0].p, polygon[i + 1].p - polygon[0].p).sqrMagnitude > 1e-12f)
                { result.Add(polygon[0]); result.Add(polygon[i]); result.Add(polygon[i + 1]); }
        }

        void ReplaceSourceMesh(MeshFilter filter, List<Vertex> v, int oldTriangles)
        {
            if (v.Count == 0) { filter.GetComponent<Renderer>().enabled = false; report.removedSourceTriangles += oldTriangles; report.sourceMeshesClipped++; return; }
            var mesh = new Mesh { name = filter.sharedMesh.name + " gym-only edit", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(v.Select(x => x.p).ToList()); mesh.SetNormals(v.Select(x => x.n).ToList()); mesh.SetTangents(v.Select(x => x.tangent).ToList());
            mesh.SetUVs(0, v.Select(x => x.uv).ToList()); mesh.SetUVs(1, v.Select(x => x.uv2).ToList()); mesh.SetColors(v.Select(x => x.color).ToList());
            mesh.SetTriangles(Enumerable.Range(0, v.Count).ToArray(), 0); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, folder + "/SourceCut_" + Safe(filter.name) + ".asset"); filter.sharedMesh = mesh;
            report.sourceMeshesClipped++; report.removedSourceTriangles += oldTriangles - v.Count / 3;
        }
    }
}
