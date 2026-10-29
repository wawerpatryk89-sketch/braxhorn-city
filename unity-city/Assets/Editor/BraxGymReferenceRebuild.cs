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
        public string revision = "kickboxing-premium-05-speedbag-contact";
        public string coordinateSystem = "Unity metres, existing glTF X mirror retained";
        public Vector3 annexMin = new Vector3(-25.5f, 0, -3f);
        public Vector3 annexMax = new Vector3(-18f, 3.1f, 8f);
        public Vector3 portalMin = new Vector3(-18.8f, 0, 4.2f);
        public Vector3 portalMax = new Vector3(-17.6f, 2.7f, 6f);
        public Vector3 mainDoorBefore, mainDoorAfter, ringCenterBefore, ringCenterAfter;
        public int sourceMeshesClipped, removedSourceTriangles, generatedMeshes, generatedTriangles, privacyRays, privacyBlocked;
        public int ceilingFansConfigured, animatedCeilingFans, premiumStrengthModelsPlaced;
        public bool singleEntranceSofaRemoved, receptionCurved, loungeCorrected, speedBagStandInstalled;
        public bool legacyReceptionRemoved, receptionMeshVisible, speedPearMeshFinite;
        public bool privacyWithAllDoorsOpen, originalEntrancePreserved, originalRingPositionPreserved;
        public bool billboardArtworkInstalled = false, muralArtworkInstalled = false, physicalIPhoneValidated = false;
        public string[] registeredDoors, requiredAssets;
        public string[] pending = { "physical iPhone performance test", "visual acceptance of brick mural in compiled Unity", "visual inspection of assembled fixtures" };
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
        Material brick, rubber, matRubber, blackSteel, steel, green, white, vinyl, wood, tile, plaster, ceiling, lit, worldText;
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
            VerifyLegacyReceptionCleared();
            FinishExistingSurfaces();
            BuildTrainingMat();
            BuildRingFinishing();
            BuildIndustrialCeiling();
            BuildMuralWall();
            BuildAnnex();
            BuildReception();
            BuildPremiumFitnessFinalPass();
            FinishOriginalExitDoor();
            BuildContactGrounding();
            BuildMirrorAnchor();
            player.cityWorld.doors = (player.cityWorld.doors ?? Array.Empty<BraxCityDoor>()).Concat(newDoors).ToArray();
            report.registeredDoors = newDoors.Select(d => d.id).ToArray();
            CheckPrivacy();
            report.ceilingFansConfigured = root.GetComponentsInChildren<BraxGymCeilingFan>(true).Length;
            report.animatedCeilingFans = root.GetComponentsInChildren<BraxGymCeilingFan>(true).Count(f => f.rotor != null && f.revolutionsPerMinute > 0f);
            if (report.ceilingFansConfigured != 2 || report.animatedCeilingFans != 2)
                throw new InvalidOperationException("Two real high-ceiling ventilation fans with assigned animated rotors are required.");
            if (!report.singleEntranceSofaRemoved || !report.receptionCurved || !report.loungeCorrected ||
                !report.legacyReceptionRemoved || !report.receptionMeshVisible || !report.speedPearMeshFinite ||
                !report.speedBagStandInstalled || !player.speedBag || !player.speedBag.HitBoxAssigned ||
                report.premiumStrengthModelsPlaced != 5)
                throw new InvalidOperationException("Final premium fitness equipment and lounge requirements not satisfied.");
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
            report.requiredAssets = new[] { "BrickAlbedo.jpg", "BrickNormal.png", "BrickARM.jpg", "RubberAlbedo.jpg", "RubberNormal.png", "RubberARM.jpg", "WoodAlbedo.jpg", "WoodNormal.png", "WoodARM.jpg", "MuralPaint.jpg", "Mark.png", "Wordmark.png", "ApprovedNutritionBadge.png" };
            foreach (string name in report.requiredAssets) RequiredTexture(name, name.Contains("Normal"), name.Contains("ARM"));
            brick = Material("Natural dark brick", new Color(.70f, .66f, .61f), .80f, 0);
            brick.mainTexture = RequiredTexture("BrickAlbedo.jpg");
            SetNormal(brick, RequiredTexture("BrickNormal.png", true), .65f);
            AddArm(brick, RequiredTexture("BrickARM.jpg", false, true), "Brick");
            rubber = Material("Dry graphite rubber", new Color(.88f, .90f, .92f), .90f, 0);
            matRubber = Material("Training mat graphite", new Color(1.12f, 1.14f, 1.16f), .91f, 0);
            foreach (var surface in new[] { rubber, matRubber })
            {
                surface.mainTexture = RequiredTexture("RubberAlbedo.jpg");
                SetNormal(surface, RequiredTexture("RubberNormal.png", true), .40f);
            }
            AddArm(rubber, RequiredTexture("RubberARM.jpg", false, true), "Rubber");
            matRubber.SetTexture("_MetallicGlossMap", rubber.GetTexture("_MetallicGlossMap"));
            matRubber.SetTexture("_OcclusionMap", rubber.GetTexture("_OcclusionMap"));
            matRubber.EnableKeyword("_METALLICGLOSSMAP"); matRubber.SetFloat("_OcclusionStrength", .82f);
            rubber.SetFloat("_GlossMapScale", .36f); matRubber.SetFloat("_GlossMapScale", .36f);
            blackSteel = Material("Black powder coated steel", new Color(.026f, .031f, .034f), .64f, .48f);
            steel = Material("Neutral brushed steel", new Color(.35f, .38f, .40f), .39f, .83f);
            green = Material("BRAXHORN green 8BFF3A", BrandGreen, .71f, .02f);
            white = Material("Warm white print", new Color(.86f, .87f, .85f), .82f, 0);
            vinyl = Material("Black protective vinyl", new Color(.023f, .026f, .030f), .68f, 0);
            wood = Material("Bench natural dark wood", Color.white, .74f, 0);
            wood.mainTexture = RequiredTexture("WoodAlbedo.jpg");
            SetNormal(wood, RequiredTexture("WoodNormal.png", true), .42f);
            AddArm(wood, RequiredTexture("WoodARM.jpg", false, true), "Wood");
            foreach (string map in new[] { "_MainTex", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" })
                wood.SetTextureScale(map, Vector2.one / .6f);
            tile = Material("Annex charcoal porcelain", new Color(.18f, .19f, .20f), .77f, 0);
            plaster = Material("Annex graphite walls", new Color(.28f, .29f, .29f), .91f, 0);
            ceiling = Material("Annex neutral ceiling", new Color(.55f, .56f, .55f), .94f, 0);
            lit = Material("Neutral fixture diffuser", new Color(.78f, .78f, .74f), .8f, 0);
            lit.EnableKeyword("_EMISSION"); lit.SetColor("_EmissionColor", new Color(.7f, .7f, .66f));
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!font) throw new InvalidOperationException("Unity's built-in font is unavailable for controlled signage.");
            var printShader = Shader.Find("Braxhorn/WorldPrintedText");
            if (!printShader) throw new InvalidOperationException("Depth-tested world signage shader is missing.");
            worldText = new Material(printShader) { name = "World printed lettering", mainTexture = font.material.mainTexture };
            AssetDatabase.CreateAsset(worldText, folder + "/WorldPrintedText.mat");
            var textBinding = root.gameObject.AddComponent<BraxGymWorldText>();
            textBinding.font = font; textBinding.printMaterial = worldText;
        }

        Texture2D RequiredTexture(string name, bool normal = false, bool linear = false)
        {
            if (textures.TryGetValue(name, out var cached)) return cached;
            string path = TextureRoot + name;
            // This one approved mural previously decoded but intermittently returned
            // null from the Unity TextureImporter on Linux/WebGL (verified CI log).
            // Preserve the exact JPEG pixels using a serialized native Texture2D
            // instead of reimporting this asset repeatedly. Other textures retain
            // their original imported PBR maps and mobile compression.
            if (name == "MuralPaint.jpg")
            {
                byte[] ownerPixels = File.ReadAllBytes(path);
                using (var digest = System.Security.Cryptography.SHA256.Create())
                {
                    string sha = BitConverter.ToString(digest.ComputeHash(ownerPixels)).Replace("-", "").ToLowerInvariant();
                    if (sha != "e714741664dbedddc379a97a87f7d8757856c73438eeb34af10df058846628e8")
                        throw new InvalidOperationException("Approved mural source pixels changed; refusing an altered graphic.");
                }
                var paintTexture = new Texture2D(2, 2, TextureFormat.RGB24, true);
                paintTexture.name = "Owner-approved BRAXHORN wall pigment - original pixels";
                if (!ImageConversion.LoadImage(paintTexture, ownerPixels, false) ||
                    paintTexture.width != 1536 || paintTexture.height != 614)
                    throw new InvalidOperationException("Unity could not decode the exact approved BRAXHORN mural.");
                paintTexture.wrapMode = TextureWrapMode.Clamp;
                paintTexture.filterMode = FilterMode.Trilinear;
                paintTexture.anisoLevel = 4;
                paintTexture.Apply(true, false);
                AssetDatabase.CreateAsset(paintTexture, folder + "/OwnerApprovedMuralTexture.asset");
                textures.Add(name, paintTexture);
                return paintTexture;
            }
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Required reviewed surface is missing: " + path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !(normal || linear); importer.mipmapEnabled = true; importer.anisoLevel = 4;
            importer.maxTextureSize = 2048;
            // The owner-approved 1536x614 JPEG has a non-4-aligned height.
            // Explicit ETC2_RGB4 override made Unity 6000.0.58f2 import return
            // no Texture2D. Use native RGB24 for this single mural only; retain
            // mipmaps, the exact uncropped image and mobile ETC2 for other assets.
            importer.textureCompression = name == "MuralPaint.jpg" ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
            if (name == "MuralPaint.jpg")
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.alphaSource = TextureImporterAlphaSource.None;
                var web = importer.GetPlatformTextureSettings("WebGL");
                web.name = "WebGL"; web.overridden = true; web.maxTextureSize = 2048;
                web.format = TextureImporterFormat.RGB24;
                importer.SetPlatformTextureSettings(web);
            }
            importer.wrapMode = name == "Mark.png" || name == "Wordmark.png" || name == "ApprovedNutritionBadge.png" || name == "MuralPaint.jpg" ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
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
            foreach (int id in Enumerable.Range(114, 19).Concat(new[] { 6, 7, 8, 14, 15, 16, 35, 49, 50, 113, 143, 145, 146, 147, 154, 156, 157, 158, 159, 160 }))
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

        void VerifyLegacyReceptionCleared()
        {
            // The former rectangular reception is carried by three indexed source
            // furniture meshes. They must be removed *at its original location*;
            // otherwise the new curved desk overlaps the old one visually.
            var oldDesk = new Bounds(new Vector3(-2.0f, .69f, 10.24f),
                new Vector3(4.8f, 1.32f, 1.20f));
            int remaining = 0;
            foreach (int index in new[] { 138, 141, 144 })
            {
                var source = FindSource(index);
                var filter = source.GetComponent<MeshFilter>();
                var renderer = source.GetComponent<MeshRenderer>();
                if (!renderer || !renderer.enabled || !filter || !filter.sharedMesh) continue;
                var vertices = filter.sharedMesh.vertices;
                var triangles = filter.sharedMesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 middle = (vertices[triangles[i]] + vertices[triangles[i+1]] +
                        vertices[triangles[i+2]]) / 3f;
                    if (oldDesk.Contains(filter.transform.TransformPoint(middle))) remaining++;
                }
            }
            if (remaining != 0)
                throw new InvalidOperationException("Original box-shaped reception is still visible: " + remaining + " triangles");
            report.legacyReceptionRemoved = true;
            Debug.Log("BRAXHORN_RECEPTION_LEGACY_REMOVED: original furniture triangles=0");
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
            // This audited source mesh ends at the existing entrance threshold;
            // the alley is a different mesh and retains its original finish.
            var floor = FindSource(153).GetComponent<MeshFilter>();
            var floorMesh = UnityEngine.Object.Instantiate(floor.sharedMesh);
            floorMesh.name = "Original hall floor with metre-scaled rubber UV";
            floorMesh.uv = floorMesh.vertices.Select(p => {
                var world = floor.transform.TransformPoint(p); return new Vector2(world.x, world.z) / 2f;
            }).ToArray();
            floorMesh.RecalculateTangents();
            AssetDatabase.CreateAsset(floorMesh, folder + "/OriginalHallRubberUV.asset");
            floor.sharedMesh = floorMesh;
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
            // One owner-approved project badge, complete with NUTRITION.
            // Replaces three tiny overlapping decals; its bull is above the wordmark
            // exactly as supplied by the owner. Original pixels are never redrawn.
            Decal(hall, "Approved BRAXHORN NUTRITION training mat badge",
                RequiredTexture("ApprovedNutritionBadge.png"),
                new Vector3(1.2f, .068f, 3.73f), 4.25f, 4.25f,
                Vector3.up, Vector3.back, Color.white);
        }

        void BuildRingFinishing()
        {
            FinishOriginalRingSurfaces();
            var previous = GameObject.Find("Premium ring and hall finishing");
            if (previous)
            {
                foreach (var r in previous.GetComponentsInChildren<MeshRenderer>(true))
                    if (r.sharedMaterial && (r.sharedMaterial.name == "Rope_ivory" || r.sharedMaterial.name == "Rope_green")) r.enabled = false;
            }
            foreach (int id in new[] { 136, 137 }) FindSource(id).GetComponent<Renderer>().enabled = false;
            const float cx = -9f, cz = -5.5f, half = 3.45f;
            // The ropes are professional padded WHITE boxing ropes, not the
            // previous alternating green and thin line appearance. Reuse the
            // existing fabric normal map to retain close-range woven detail.
            var ropeNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Source/Premium/RopeNormal.png");
            if (!ropeNormal) throw new InvalidOperationException("Reviewed woven rope normal map is missing.");
            var ropeMaterial = Material("Premium white textile ring rope", new Color(.93f, .94f, .93f), .84f, 0f);
            SetNormal(ropeMaterial, ropeNormal, .25f);
            float[] heights = { 1.22f, 1.60f, 1.98f, 2.36f };
            for (int i = 0; i < heights.Length; i++)
            {
                float y = heights[i];
                var a = new Vector3(cx - half, y, cz - half); var b = new Vector3(cx + half, y, cz - half);
                var c = new Vector3(cx + half, y, cz + half); var d = new Vector3(cx - half, y, cz + half);
                Tube(hall, "Four-rope ring rear", a, b, .050f, ropeMaterial, 40, 12, .012f);
                Tube(hall, "Four-rope ring side", a, d, .050f, ropeMaterial, 40, 12, .012f);
                Tube(hall, "Four-rope ring side", b, c, .050f, ropeMaterial, 40, 12, .012f);
                Tube(hall, "Four-rope ring entry left", d, new Vector3(-9.85f, y, cz + half), .050f, ropeMaterial, 22, 12, .008f);
                Tube(hall, "Four-rope ring entry right", new Vector3(-8.15f, y, cz + half), c, .050f, ropeMaterial, 22, 12, .008f);
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

            // One controlled brand placement per exposed face. The front print
            // is beside the staircase, never behind its treads or gate opening.
            RingApronBrand("front", new Vector3(-5.15f, .46f, -1.282f),
                new Vector3(-6.80f, .46f, -1.282f), .49f, 2.50f, Vector3.forward);
            RingApronBrand("hall side", new Vector3(-4.782f, .46f, -8.0f),
                new Vector3(-4.782f, .46f, -4.75f), .64f, 4.70f, Vector3.right);

            const float canvasEdge = 3.28f, canvasY = .956f;
            foreach (float x in new[] { cx - canvasEdge, cx + canvasEdge })
                Box(hall, "Ring canvas green printed boundary", new Vector3(x, canvasY, cz),
                    new Vector3(.032f, .002f, canvasEdge * 2), green, .0005f);
            foreach (float z in new[] { cz - canvasEdge, cz + canvasEdge })
                Box(hall, "Ring canvas green printed boundary", new Vector3(cx, canvasY, z),
                    new Vector3(canvasEdge * 2, .002f, .032f), green, .0005f);
            Decal(hall, "Ring canvas original compact mark", RequiredTexture("Mark.png"),
                new Vector3(cx, .956f, cz), 1.35f, 1.35f, Vector3.up, Vector3.back, Color.white);
        }

        void FinishOriginalRingSurfaces()
        {
            // Audited source: deck76 has the canvas in material38. Each original
            // apron77..80 uses material39 and U=0..2.2, multiplying the already
            // repeated source lettering. Retain the exact meshes, UVs and bounds;
            // remove that print by replacing only these local material slots.
            var weave = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Source/Premium/CanvasNormal.png");
            if (!weave) throw new InvalidOperationException("Existing fine canvas weave is required for the original ring finish.");
            var apron = Material("Ring matte black fabric apron", new Color(.016f, .019f, .022f), .92f, 0);
            SetNormal(apron, weave, .18f);
            // Standard uses MainTex UV transform for its normal map as well.
            // Compensate the existing2.2 UV span; no old albedo is sampled.
            apron.mainTextureScale = new Vector2(35f / 2.2f, 4f);
            var canvas = Material("Ring graphite woven canvas", new Color(.045f, .052f, .058f), .94f, 0);
            SetNormal(canvas, weave, .24f); canvas.mainTextureScale = new Vector2(32f, 32f);

            var deck = FindSource(76).GetComponent<MeshRenderer>();
            var deckMesh = deck.GetComponent<MeshFilter>().sharedMesh;
            var deckBounds = deck.bounds; var deckMaterials = deck.sharedMaterials; int canvasSlots = 0;
            for (int i = 0; i < deckMaterials.Length; i++)
                if (deckMaterials[i] && deckMaterials[i].name.StartsWith("BX_38_", StringComparison.Ordinal))
                { deckMaterials[i] = canvas; canvasSlots++; }
            if (canvasSlots != 1) throw new InvalidOperationException("Expected exactly one original ring canvas material38; refuse an inferred deck replacement.");
            deck.sharedMaterials = deckMaterials; deck.gameObject.layer = 10;
            if (deck.GetComponent<MeshFilter>().sharedMesh != deckMesh || deck.bounds != deckBounds)
                throw new InvalidOperationException("Ring material finishing changed deck geometry or bounds.");

            foreach (int id in new[] { 77, 78, 79, 80 })
            {
                var renderer = FindSource(id).GetComponent<MeshRenderer>();
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh; var bounds = renderer.bounds;
                if (renderer.sharedMaterials.Length != 1 || !renderer.sharedMaterial ||
                    !renderer.sharedMaterial.name.StartsWith("BX_39_", StringComparison.Ordinal))
                    throw new InvalidOperationException("Expected original printed apron material39 on Gym_Mesh_" + id);
                renderer.sharedMaterial = apron; renderer.gameObject.layer = 10;
                if (renderer.GetComponent<MeshFilter>().sharedMesh != mesh || renderer.bounds != bounds)
                    throw new InvalidOperationException("Ring finishing changed an original apron mesh or its bounds: " + id);
            }
            Debug.Log("BRAXHORN_RING_SURFACES original deck76 and aprons77-80 retained; old repeating albedo removed; graphite canvas and black matte apron applied");
        }

        void RingApronBrand(string face, Vector3 markCenter, Vector3 textCenter, float markSize, float textWidth, Vector3 outward)
        {
            Decal(hall, "Ring " + face + " original brand mark", RequiredTexture("Mark.png"),
                markCenter, markSize, markSize, outward, Vector3.up, Color.white);
            var textRoot = Group(hall, "Ring " + face + " single BRAXHORN KICKBOXING print");
            textRoot.localPosition = textCenter;
            // Use the shared depth-tested print material and its runtime font
            // binding. Do not assign Unity's default overlay font material.
            Text(textRoot, "BRAXHORN KICKBOXING", Vector3.zero, textWidth,
                new Color(.86f, .88f, .86f), Quaternion.identity);
            var label = textRoot.Find("Sign BRAXHORN KICKBOXING");
            var text = label.GetComponent<TextMesh>(); text.fontStyle = FontStyle.Bold;
            label.localScale = Vector3.one;
            font.RequestCharactersInTexture(text.text, text.fontSize, FontStyle.Bold);
            var renderer = label.GetComponent<MeshRenderer>(); renderer.sharedMaterial.mainTexture = font.material.mainTexture;
            float advance = 0, low = 0, high = 0;
            foreach (char c in text.text)
                if (font.GetCharacterInfo(c, out var glyph, text.fontSize, FontStyle.Bold))
                { advance += glyph.advance; low = Mathf.Min(low, glyph.minY); high = Mathf.Max(high, glyph.maxY); }
            float width = renderer.localBounds.size.x, height = renderer.localBounds.size.y;
            if (width <= .00001f) width = advance * text.characterSize / 10f;
            if (height <= .00001f) height = (high - low) * text.characterSize / 10f;
            if (width <= .00001f || height <= .00001f)
                throw new InvalidOperationException("Could not measure the controlled ring brand print.");
            label.localScale = Vector3.one * Mathf.Min(textWidth / width, .42f / height);
            // TextMesh faces local-Z; turn that face towards the hall.
            textRoot.localRotation = Quaternion.LookRotation(-outward, Vector3.up);
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
                Tube(hall, "Duct ceiling support", new Vector3(16.1f, 7.38f, z), new Vector3(16.1f, 8.5f, z), .012f, blackSteel, 1, 6);
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
            foreach (float z in new[] { -7f, -1f, 5f })
            {
                Box(hall, "Linear task light body", new Vector3(13.8f, 5.45f, z), new Vector3(.16f, .12f, 2.4f), blackSteel, .022f);
                Box(hall, "Linear neutral diffuser", new Vector3(13.8f, 5.38f, z), new Vector3(.11f, .012f, 2.30f), lit, .004f);
                foreach (float dz in new[] { -.85f, .85f }) Tube(hall, "Linear light cable", new Vector3(13.8f, 5.5f, z + dz), new Vector3(13.8f, 7.3f, z + dz), .008f, blackSteel, 1, 6);
                LocalLight("Neutral leather and suspension task light", new Vector3(13.8f, 5.25f, z), 2.0f, 8.0f);
            }
            BuildPremiumCeilingFans();
            // Inexpensive neutral indirect fill is restricted to the new room
            // layer. The original character and city lights remain untouched.
            foreach (var direction in new[] { new Vector3(28f, 90f, 0), new Vector3(28f, 270f, 0) })
            {
                var fill = Group(fixtures, "Neutral room indirect fill").gameObject.AddComponent<Light>();
                fill.type = LightType.Directional; fill.color = new Color(1f, .97f, .93f);
                fill.intensity = .42f; fill.shadows = LightShadows.None; fill.cullingMask = 1 << 10;
                fill.renderMode = LightRenderMode.ForceVertex; fill.transform.rotation = Quaternion.Euler(direction);
            }
        }

        // Two high-clearance industrial air circulators and four real duct outlet grilles.
        // All fixtures are above the player/camera envelope; nothing in the ring,
        // boxing bag swing paths, player rig, original entrance or city is modified.
        void BuildPremiumCeilingFans()
        {
            var housing = Material("HVLS graphite enamel motor housing", new Color(.105f, .115f, .125f), .72f, .34f);
            var bladeSteel = Material("HVLS satin steel curved blades", new Color(.31f, .335f, .35f), .69f, .44f);
            var fastener = Material("HVLS brushed alloy machine screws", new Color(.47f, .49f, .50f), .55f, .75f);
            var mounts = new[] { new Vector2(-1.45f, -6.0f), new Vector2(9.35f, 3.45f) };
            for (int i = 0; i < mounts.Length; i++)
            {
                var mount = Group(fixtures, "Suspended industrial five-blade ceiling fan " + (i + 1));
                mount.localPosition = new Vector3(mounts[i].x, 6.36f, mounts[i].y);
                Box(mount, "Four-bolt steel ceiling mounting plate", new Vector3(0, 1.37f, 0),
                    new Vector3(.42f, .075f, .42f), housing, .025f);
                Tube(mount, "Structural steel drop shaft", new Vector3(0, .10f, 0),
                    new Vector3(0, 1.38f, 0), .048f, housing, 2, 20);
                foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                    Tube(mount, "Ceiling bracket bolt", new Vector3(sx * .155f, 1.41f, sz * .155f),
                        new Vector3(sx * .155f, 1.46f, sz * .155f), .016f, fastener, 1, 12);
                Tube(mount, "Circular ventilated motor housing", new Vector3(0, -.10f, 0),
                    new Vector3(0, .20f, 0), .198f, housing, 3, 28);
                Ring(mount, "Machined motor end collar", new Vector3(0, -.115f, 0),
                    .195f, .016f, fastener, Quaternion.Euler(90, 0, 0), 32, 8);
                for (int slot = 0; slot < 10; slot++)
                {
                    float a = slot * Mathf.PI * 2f / 10f;
                    float dx = Mathf.Cos(a) * .202f, dz = Mathf.Sin(a) * .202f;
                    Tube(mount, "Axial-motor cooling slot rim",
                        new Vector3(dx, .012f, dz), new Vector3(dx, .095f, dz),
                        .008f, fastener, 1, 6);
                }

                var rotor = Group(mount, "Five curved fan blades - animated rotor");
                rotor.localPosition = new Vector3(0, -.19f, 0);
                Tube(rotor, "Five-blade machined rotor hub", new Vector3(0, -.065f, 0),
                    new Vector3(0, .065f, 0), .17f, housing, 2, 24);
                MeshObject(rotor, "Five solid curved satin alloy fan blades", MakePremiumFanBlades(), bladeSteel);
                for (int blade = 0; blade < 5; blade++)
                {
                    float a = blade * 2f * Mathf.PI / 5f;
                    var p = new Vector3(Mathf.Cos(a) * .25f, .028f, Mathf.Sin(a) * .25f);
                    Tube(rotor, "Blade-root bolted fastener", p, p + Vector3.up * .025f,
                        .024f, fastener, 1, 12);
                }

                var animation = mount.gameObject.AddComponent<BraxGymCeilingFan>();
                animation.rotor = rotor;
                animation.revolutionsPerMinute = i == 0 ? 48f : 54f;
            }

            // Vented outlet faces belong to the existing longitudinal high duct,
            // with real metal louvers rather than emissive rectangles.
            foreach (float z in new[] { -9.0f, -3.3f, 2.4f, 7.9f })
            {
                Box(hall, "Ventilation branch metal outlet plenum", new Vector3(16.10f, 6.83f, z),
                    new Vector3(.38f, .32f, 1.08f), blackSteel, .025f);
                Box(hall, "Recessed ventilation grille frame", new Vector3(16.10f, 6.665f, z),
                    new Vector3(.34f, .020f, 1.00f), steel, .008f);
                Box(hall, "Dark air cavity behind louvered grille", new Vector3(16.10f, 6.647f, z),
                    new Vector3(.28f, .006f, .91f), housing, .002f);
                for (int l = -3; l <= 3; l++)
                {
                    float iz = z + l * .115f;
                    Box(hall, "Angled metal outlet louver", new Vector3(16.10f, 6.622f, iz),
                        new Vector3(.27f, .024f, .019f), fastener, .004f);
                }
            }
        }

        Mesh MakePremiumFanBlades()
        {
            // A single closed blade mesh per fan: aerodynamic taper, thickness,
            // subtle twist and root bolts; not flat planes or billboard textures.
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            float[] distance = { .18f, .35f, .60f, .89f, 1.12f };
            float[] halfWidth = { .070f, .145f, .175f, .140f, .055f };
            float[] twist = { .005f, .025f, .050f, .038f, .012f };
            for (int blade = 0; blade < 5; blade++)
            {
                int offset = vertices.Count;
                var rotation = Quaternion.AngleAxis(blade * 72f, Vector3.up);
                for (int segment = 0; segment < distance.Length; segment++)
                {
                    float r = distance[segment], w = halfWidth[segment], y = twist[segment];
                    vertices.Add(rotation * new Vector3(r, y, -w));
                    vertices.Add(rotation * new Vector3(r, y, w));
                    vertices.Add(rotation * new Vector3(r, y - .024f, -w));
                    vertices.Add(rotation * new Vector3(r, y - .024f, w));
                    uv.Add(new Vector2(r, 0)); uv.Add(new Vector2(r, 1));
                    uv.Add(new Vector2(r, 0)); uv.Add(new Vector2(r, 1));
                }
                for (int segment = 0; segment < distance.Length - 1; segment++)
                {
                    int a = offset + segment * 4, b = a + 4;
                    triangles.AddRange(new[] {
                        a, a+1, b, a+1, b+1, b,        // upper metal shell
                        a+2, b+2, a+3, a+3, b+2, b+3, // underside
                        a, b, a+2, a+2, b, b+2,        // leading bevel
                        a+1, a+3, b+1, b+1, a+3, b+3  // trailing bevel
                    });
                }
                int first = offset, last = offset + (distance.Length - 1) * 4;
                triangles.AddRange(new[] { first, first+2, first+1, first+1, first+2, first+3,
                    last, last+1, last+2, last+1, last+3, last+2 });
            }
            var mesh = new Mesh { name = "Solid aerodynamic premium industrial ceiling fan blades" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        void BuildMuralWall()
        {
            // This is a wall PAINTING, not a framed billboard, sign, photo panel
            // or a captured scene. Retain the existing brick behind the pigment.
            // A feathered, rough, alpha-composited coat reveals brick and mortar
            // and leaves no hard-edged black rectangle around the artwork.
            // Exactly the owner's approved 2.502:1 composition is installed with no crop;
            // 1536x614 is a mobile resample of the exact owner-approved PNG, not a smaller proxy or a panel.
            var art = RequiredTexture("MuralPaint.jpg");
            if (art.width != 1536 || art.height != 614)
                throw new InvalidOperationException("The approved mural lost its original image resolution or aspect.");
            var shader = Shader.Find("Braxhorn/PaintedBrickMural");
            if (!shader) throw new InvalidOperationException("The actual brick-painted mural shader is missing.");
            var material = new Material(shader) { name = "Original BRAXHORN rear wall painted mural" };
            material.SetTexture("_MainTex", art);
            material.SetTexture("_BrickTex", RequiredTexture("BrickAlbedo.jpg"));
            material.SetTexture("_BumpMap", RequiredTexture("BrickNormal.png", true));
            material.SetFloat("_PaintOpacity", .92f);
            AssetDatabase.CreateAsset(material, folder + "/PaintedBrickMural.mat");

            // Approved flat reference is 1536:614; preserve its exact aspect.
            // World +X appears camera LEFT at the existing front entrance, so
            // reverse texture U to keep Brax visually LEFT and Nyxa RIGHT.
            const float width = 19.2f, height = 7.675f;
            const float centerX = -3.4f, centerY = 4.48f;
            // Audited old decor spans x[-16.82,16.82], z[-13.0,-12.88].
            // Disable only this raised legacy overlay; the ORIGINAL rear brick
            // wall (Gym_Mesh_70, inner face z=-13.0) stays fully intact.
            var oldPanel = FindSource(71).GetComponent<MeshRenderer>();
            var oldBounds = oldPanel.bounds;
            if (oldBounds.size.x < 30f || oldBounds.min.z < -13.1f || oldBounds.max.z > -12.7f)
                throw new InvalidOperationException("Legacy rear decor changed: refusing an inferred removal.");
            oldPanel.enabled = false;
            var mural = new Mesh {
                name = "Direct pigment coat on original brick wall",
                vertices = new[] {
                    new Vector3(-width / 2, -height / 2, 0),
                    new Vector3(width / 2, -height / 2, 0),
                    new Vector3(width / 2, height / 2, 0),
                    new Vector3(-width / 2, height / 2, 0)
                },
                uv = new[] { new Vector2(1,0), Vector2.zero, new Vector2(0,1), Vector2.one },
                // Same world-scale brick normal and grout placement as source.
                uv2 = new[] {
                    new Vector2((centerX-width/2)/2.2f, (centerY-height/2)/2.2f),
                    new Vector2((centerX+width/2)/2.2f, (centerY-height/2)/2.2f),
                    new Vector2((centerX+width/2)/2.2f, (centerY+height/2)/2.2f),
                    new Vector2((centerX-width/2)/2.2f, (centerY+height/2)/2.2f)
                },
                triangles = new[] { 0,1,2,0,2,3 }
            };
            mural.RecalculateNormals();
            var paint = MeshObject(hall, "Painted rear-wall BRAXHORN STREET FIGHTER mural", mural, material);
            paint.localPosition = new Vector3(centerX, centerY, -12.986f);
            report.muralArtworkInstalled = true;

            // Remove the blurry split mark + wordmark. Place the EXACT owner
            // supplied bull-above-BRAXHORN-NUTRITION badge as one wall print;
            // no AI reinterpretation and no raised billboard frame.
            Decal(hall, "Approved BRAXHORN NUTRITION right wall badge",
                RequiredTexture("ApprovedNutritionBadge.png"),
                new Vector3(-17.875f, 5.03f, 0f), 3.85f, 3.85f,
                Vector3.right, Vector3.up, Color.white);
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
            AddRequiredModel("Toilet.gltf", wc, .74f, true);
            ColliderBox("WC porcelain collision", new Vector3(-24.4f, .39f, -1.92f), new Vector3(.68f, .78f, .90f));
            var sink = Group(annex, "SinkFixtureAnchor"); sink.position = new Vector3(-24.80f, 0, .45f); sink.rotation = Quaternion.Euler(0, 90, 0);
            AddRequiredModel("Sink.gltf", sink, .70f, true);
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
            // Owner: luxurious radius at one end, subtly projecting toward hall.
            // Existing position / real city exit remain unchanged.
            var p = new Vector3(-2.6f,0f,9.65f);
            var graphite=Material("BRAXHORN reception satin graphite",
                new Color(.052f,.058f,.064f),.70f,.38f);
            var warmMetal=Material("BRAXHORN reception champagne metal edge",
                new Color(.37f,.35f,.29f),.48f,.73f);
            var walnut=Material("BRAXHORN reception real figured dark walnut",
                new Color(.61f,.53f,.46f),.74f,0f);
            walnut.mainTexture=RequiredTexture("WoodAlbedo.jpg");
            SetNormal(walnut,RequiredTexture("WoodNormal.png",true),.45f);
            AddArm(walnut,RequiredTexture("WoodARM.jpg",false,true),"Wood");
            MeshObject(hall,"Premium reception shaped full-radius toe plinth",
                RoundedReceptionGeometry(.055f,.145f,0f),blackSteel).localPosition=p;
            var counter = MeshObject(hall,"Premium reception bowed one-end graphite body",
                RoundedReceptionGeometry(.145f,1.045f,0f),graphite);
            counter.localPosition=p;
            // Real shaped geometry must be visible, not merely a creation flag.
            var counterBounds = counter.GetComponent<MeshRenderer>().bounds;
            if (counterBounds.size.x < 3.45f || counterBounds.size.y < .85f ||
                counterBounds.size.z < 1.10f || float.IsNaN(counterBounds.center.x))
                throw new InvalidOperationException("Premium reception mesh has invalid or missing silhouette");
            report.receptionMeshVisible = true;
            MeshObject(hall,"Premium reception precision satin metal top edge",
                RoundedReceptionGeometry(1.041f,1.067f,.035f),warmMetal).localPosition=p;
            MeshObject(hall,"Premium reception curved genuine walnut worktop",
                RoundedReceptionGeometry(1.064f,1.136f,.052f),walnut).localPosition=p;
            Box(hall,"Premium reception inset lower shadow reveal",
                p+new Vector3(-.50f,.23f,-.773f),
                new Vector3(2.56f,.23f,.018f),blackSteel,.008f);
            Tube(hall,"Reception fine green inset line",
                p+new Vector3(-1.88f,.36f,-.783f),
                p+new Vector3(1.07f,.36f,-.783f),
                .011f,green,8,10);
            // Continuously follow the actually rounded right end. The accent is
            // restrained and matches the approved black/wood/green BRAXHORN design.
            const int arcSegments = 20;
            var centerZ = (-.75f + .52f) * .5f;
            var capRadius = (.52f + .75f) * .5f;
            for (int i = 0; i < arcSegments; i++)
            {
                float t0 = -Mathf.PI*.5f + Mathf.PI*i/arcSegments;
                float t1 = -Mathf.PI*.5f + Mathf.PI*(i+1)/arcSegments;
                Vector3 from = p+new Vector3(1.07f+capRadius*Mathf.Cos(t0),.79f,
                    centerZ+capRadius*Mathf.Sin(t0)-.013f);
                Vector3 to = p+new Vector3(1.07f+capRadius*Mathf.Cos(t1),.79f,
                    centerZ+capRadius*Mathf.Sin(t1)-.013f);
                Tube(hall,"Reception curved end fine green seam",from,to,.010f,green,1,8);
            }
            // Only the genuine project logo, never fake/generated lettering.
            Decal(hall,"BRAXHORN original reception wordmark",
                RequiredTexture("Wordmark.png"),p+new Vector3(-.46f,.77f,-.782f),
                1.92f,.31f,Vector3.back,Vector3.up,Color.white);
            Box(hall,"Real reception payment stand",
                p+new Vector3(.48f,1.19f,.10f),
                new Vector3(.17f,.10f,.17f),blackSteel,.025f);
            var terminal=Box(hall,"Reception premium POS terminal",
                p+new Vector3(.48f,1.25f,.10f),
                new Vector3(.14f,.045f,.21f),vinyl,.015f);
            terminal.localRotation=Quaternion.Euler(-18f,0,0);
            ColliderBox("Premium curved counter solid obstruction",
                p+new Vector3(-.1f,.58f,-.11f),new Vector3(3.62f,1.16f,1.30f));
            report.receptionCurved=true;
            BuildCornerLounge();
        }

        Mesh RoundedReceptionGeometry(float bottom,float top,float overhang)
        {
            // Rounded solid extrusion, NOT a scaled rectangular primitive:
            // left end straight, right end a genuine 180-degree curved cap.
            float front=-.75f-overhang,rear=.52f+overhang;
            float rad=(rear-front)*.5f,zMid=(front+rear)*.5f;
            var outline=new List<Vector2>{new Vector2(-1.94f-overhang,front)};
            for(int k=0;k<=24;k++)
            {
                float a=-Mathf.PI*.5f+Mathf.PI*k/24f;
                outline.Add(new Vector2(1.07f+rad*Mathf.Cos(a),
                    zMid+rad*Mathf.Sin(a)));
            }
            outline.Add(new Vector2(-1.94f-overhang,rear));
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            foreach(var q in outline)
            {
                vertices.Add(new Vector3(q.x,top,q.y));
                vertices.Add(new Vector3(q.x,bottom,q.y));
                uv.Add(q*1.2f);uv.Add(q*1.2f);
            }
            int n=outline.Count, topCenter=vertices.Count;
            vertices.Add(new Vector3(-.36f,top,zMid));uv.Add(new Vector2(-.36f,zMid));
            vertices.Add(new Vector3(-.36f,bottom,zMid));uv.Add(new Vector2(-.36f,zMid));
            for(int i=0;i<n;i++)
            {
                int a=i*2,b=((i+1)%n)*2;
                triangles.AddRange(new[]{
                    topCenter,b,a,topCenter+1,a+1,b+1,
                    a,b,a+1,b,b+1,a+1
                });
            }
            var mesh=new Mesh{name="Luxury rounded counter solid with shaped sides"};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);
            mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            return mesh;
        }

        void BuildCornerLounge()
        {
            // Use the actual reviewed CC0 sofa from the existing library.
            // Two sofas sit on TWO perpendicular walls of the same corner.
            // Keep the annex door (right wall z=4.2..6.0) clear.
            PlaceLibrarySofa("BRAXHORN lounge sofa against entrance wall",
                new Vector3(-13.80f, 0f, 10.31f), 180f);
            PlaceLibrarySofa("BRAXHORN lounge sofa against right wall",
                new Vector3(-17.11f, 0f, 7.79f), 90f);

            // Furnished coffee table with a rounded real wooden top, dark
            // powder-coated steel lower frame and individual protected feet.
            var p = new Vector3(-15.33f, 0f, 8.47f);
            Box(hall, "BRAXHORN lounge coffee table oak top",
                p + new Vector3(0, .46f, 0), new Vector3(1.72f, .078f, 1.04f), wood, .068f);
            Box(hall, "Coffee table discreet steel rim",
                p + new Vector3(0, .415f, 0), new Vector3(1.64f, .027f, .96f), blackSteel, .025f);
            foreach (float x in new[] { -.67f, .67f })
                foreach (float z in new[] { -.36f, .36f })
                {
                    Box(hall, "Coffee table slim steel leg",
                        p + new Vector3(x, .225f, z), new Vector3(.036f, .43f, .036f), blackSteel, .008f);
                    Box(hall, "Coffee table rubber floor foot",
                        p + new Vector3(x, .018f, z), new Vector3(.060f, .035f, .060f), rubber, .008f);
                }
            ColliderBox("Lounge coffee table collision",
                p + new Vector3(0, .26f, 0), new Vector3(1.75f, .51f, 1.07f));
            report.loungeCorrected=true;
        }

        void BuildPremiumFitnessFinalPass()
        {
            // Extra old chair by the entrance was created BEFORE the verified
            // two-sofa lounge. Remove only that accidental third chair.
            var extra=GameObject.Find("Library Sofa 02 - original PBR model");
            var oldCollider=GameObject.Find("Library sofa collision");
            if(!extra || !oldCollider)
                throw new InvalidOperationException("Missing third entrance chair or its obsolete collider");
            UnityEngine.Object.DestroyImmediate(extra);
            UnityEngine.Object.DestroyImmediate(oldCollider);
            report.singleEntranceSofaRemoved=true;

            // Real six-hook forged metal coat rack on the vacated entrance wall.
            Box(hall,"Former entry-chair area: dark walnut coat backing",
                new Vector3(17.72f,1.73f,8.36f),new Vector3(.07f,.31f,2.75f),wood,.02f);
            Box(hall,"Coat rack continuous satin steel plate",
                new Vector3(17.67f,1.73f,8.36f),new Vector3(.05f,.12f,2.67f),blackSteel,.017f);
            foreach(float z in new[]{7.26f,7.70f,8.14f,8.58f,9.02f,9.46f})
            {
                Tube(hall,"Real stainless coat hook mounting bolt",
                    new Vector3(17.65f,1.72f,z),new Vector3(17.55f,1.72f,z),
                    .027f,steel,1,12);
                Tube(hall,"Forged curved coat hook",
                    new Vector3(17.55f,1.72f,z),new Vector3(17.43f,1.57f,z),
                    .024f,blackSteel,6,12);
                Tube(hall,"Protective upturned coat hook end",
                    new Vector3(17.43f,1.57f,z),new Vector3(17.42f,1.68f,z),
                    .024f,steel,5,12);
            }

            // Genuine GLB models from the independently audited CC0 fitness
            // library, never the user's low-poly set and never its rings.
            FitnessModel("PremiumDumbbellRack",new Vector3(-15.62f,0,-10.75f),
                90f,1.96f,new Vector3(1.20f,1.18f,2.05f));
            FitnessModel("PremiumPlateTree",new Vector3(-16.55f,0,-8.02f),
                0f,.86f,new Vector3(.92f,1.49f,.92f));
            FitnessModel("PremiumKettlebellRack",new Vector3(-16.51f,0,-5.64f),
                90f,1.2f,new Vector3(.86f,1.2f,1.24f));
            FitnessModel("PremiumFlatBench",new Vector3(-15.50f,0,-3.58f),
                90f,1.34f,new Vector3(.89f,.52f,1.41f));
            FitnessModel("PremiumInclineBench",new Vector3(-15.46f,0,-1.38f),
                90f,1.36f,new Vector3(.93f,1.0f,1.45f));
            SpeedBagPearStation();
        }

        void FitnessModel(string filename,Vector3 p,float yaw,float width,Vector3 bounds)
        {
            var a=Group(hall,"BRAXHORN real CC0 strength apparatus "+filename);
            a.position=p;a.rotation=Quaternion.Euler(0,yaw,0);
            // Fail closed if the true PBR GLB did not import; no cubes replace it.
            AddRequiredModel("Fitness/"+filename+".gltf",a,width,true);
            ColliderBox("BRAXHORN gym strength obstacle "+filename,
                p+new Vector3(0,bounds.y*.5f,0),bounds);
            report.premiumStrengthModelsPlaced++;
        }

        void SpeedBagPearStation()
        {
            // Speed boxing pear suspended under a hard round speedball platform
            // on its own steel stand: this is NOT another floor heavy bag.
            var a=Group(hall,"Premium speed boxing pear stand");
            var basePos=new Vector3(11.20f,0,-10.65f);a.position=basePos;
            var leather=Material("Speedboxing authentic brown leather",
                new Color(.30f,.25f,.20f),.70f,0f);
            leather.mainTexture=RequiredTexture("LeatherAlbedo.jpg");
            SetNormal(leather,RequiredTexture("LeatherNormal.png",true),.55f);
            var polished=Material("Speedbag stand machined stainless steel",
                new Color(.35f,.37f,.38f),.47f,.81f);
            var darkWood=Material("Speedbag black walnut striking drum",
                new Color(.46f,.39f,.33f),.76f,0f);
            darkWood.mainTexture=RequiredTexture("WoodAlbedo.jpg");
            SetNormal(darkWood,RequiredTexture("WoodNormal.png",true),.45f);
            Box(a,"Heavy anti-tip rectangular stand base",
                new Vector3(0,.09f,-.20f),new Vector3(.76f,.18f,.98f),blackSteel,.065f);
            foreach(float x in new[]{-.32f,.32f})foreach(float z in new[]{-.59f,.19f})
                Box(a,"Speed bag stand reinforced rubber feet",
                    new Vector3(x,.035f,z),new Vector3(.14f,.06f,.14f),rubber,.02f);
            Tube(a,"Real tubular telescopic height-adjustable upright",
                new Vector3(0,.18f,-.28f),new Vector3(0,2.20f,-.28f),
                .074f,blackSteel,2,22);
            for(float y=.54f;y<1.96f;y+=.17f)
                Tube(a,"Mechanical stand height index pin",
                    new Vector3(-.07f,y,-.28f),new Vector3(.07f,y,-.28f),
                    .008f,polished,1,8);
            Tube(a,"Hard stainless projecting platform arm",
                new Vector3(0,2.12f,-.28f),new Vector3(0,2.12f,.32f),
                .048f,polished,2,18);
            Tube(a,"Genuine round premium walnut speedbag striking board",
                new Vector3(0,2.18f,.32f),new Vector3(0,2.27f,.32f),
                .46f,darkWood,2,36);
            Ring(a,"Speedball platform alloy edge",new Vector3(0,2.225f,.32f),
                .465f,.016f,polished,Quaternion.Euler(90f,0,0),36,8);
            Tube(a,"Swivel with steel bearing",new Vector3(0,2.00f,.32f),
                new Vector3(0,2.18f,.32f),.034f,polished,3,20);
            // Only the pear moves. The supporting bar, board and the real
            // steel swivel stay rigidly attached to the original stand.
            var reboundPivot=Group(a,"Real speed bag rebound rotation beneath swivel");
            reboundPivot.localPosition=new Vector3(0,2.00f,.32f);
            var pearMesh = SpeedPearMesh();
            var bounds = pearMesh.bounds;
            if (float.IsNaN(bounds.center.y) || float.IsInfinity(bounds.center.y) ||
                bounds.size.y < .48f || bounds.size.x < .35f)
                throw new InvalidOperationException("Speed pear mesh missing/invalid: refuse an invisible punching target");
            var pear=MeshObject(reboundPivot,"Full three-dimensional pear leather bag",
                pearMesh,leather);
            pear.localPosition=new Vector3(0,-.28f,0f);
            report.speedPearMeshFinite = true;
            // True 3D collider moves with the leather pear and is a trigger,
            // so it never creates an invisible wall blocking the player.
            var hitbox=pear.gameObject.AddComponent<SphereCollider>();
            hitbox.center=new Vector3(0,-.025f,0);
            hitbox.radius=.222f;
            hitbox.isTrigger=true;
            var interaction=a.gameObject.AddComponent<BraxGymSpeedBag>();
            interaction.pivot=reboundPivot;
            interaction.hitbox=hitbox;
            if(player.speedBag) throw new InvalidOperationException(
                "Duplicate speed bag gameplay binding in the same gym");
            player.speedBag=interaction;
            ColliderBox("Speedbag fixed steel support collision",
                basePos+new Vector3(0,.16f,-.20f),new Vector3(.79f,.32f,.99f));
            report.speedBagStandInstalled=interaction.HitBoxAssigned;
        }

        Mesh SpeedPearMesh()
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var ix=new List<int>();
            const int latitude=20,longitude=32;
            for(int y=0;y<=latitude;y++)
            {
                float t=y/(float)latitude;
                // sin(PI) is slightly negative in float32. Fractional Pow of a
                // negative value produced NaN vertices and an invisible 3D pear.
                float positiveSine = Mathf.Max(0f, Mathf.Sin(Mathf.PI*t));
                float r=.021f+.205f*Mathf.Pow(positiveSine,1.02f)*
                    (.78f+.22f*t);
                if (float.IsNaN(r) || float.IsInfinity(r))
                    throw new InvalidOperationException("Non-finite speed-pear radius at latitude "+y);
                for(int a=0;a<=longitude;a++)
                {
                    float angle=a*Mathf.PI*2/longitude;
                    v.Add(new Vector3(Mathf.Cos(angle)*r,.24f-.53f*t,
                        Mathf.Sin(angle)*r));
                    uv.Add(new Vector2(a/(float)longitude,t));
                    if(y<latitude&&a<longitude)
                    {
                        int i=y*(longitude+1)+a;
                        ix.AddRange(new[]{i,i+1,i+longitude+1,i+1,i+longitude+2,i+longitude+1});
                    }
                }
            }
            if (v.Any(p=>float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||
                    float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z)))
                throw new InvalidOperationException("Non-finite speed-pear vertices would hide the target");
            var m=new Mesh{name="Speedboxing real rounded pear leather mesh"};
            m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(ix,0);
            m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        void PlaceLibrarySofa(string name, Vector3 center, float yaw)
        {
            const string path = "Assets/Source/LibrarySofa02.glb";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path)
                ?? AssetDatabase.LoadAllAssetsAtPath(path).OfType<GameObject>().FirstOrDefault();
            if (!source) throw new InvalidOperationException(
                "Original sofa_02 from the approved CC0 library is required: " + path);
            var anchor = Group(hall, name);
            anchor.position = center;
            anchor.rotation = Quaternion.Euler(0, yaw, 0);
            var sofa = UnityEngine.Object.Instantiate(source, anchor, false);
            sofa.name = "Original library sofa_02 premium upholstery";
            var renderers = sofa.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("The imported library sofa has no renderers.");
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float size = Mathf.Max(bounds.size.x, bounds.size.z);
            if (size < .05f) throw new InvalidOperationException("Original CC0 sofa scale is invalid.");
            sofa.transform.localScale *= 2.32f / size;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                renderer.gameObject.layer = 10;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                bounds.Encapsulate(renderer.bounds);
            }
            sofa.transform.position += new Vector3(center.x - bounds.center.x,
                -bounds.min.y, center.z - bounds.center.z);
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            // Bounds-driven obstruction, rather than an unrelated invisible box.
            ColliderBox(name + " collision",
                new Vector3(bounds.center.x, Mathf.Min(.46f, bounds.center.y), bounds.center.z),
                new Vector3(Mathf.Max(.70f, bounds.size.x * .94f),
                    Mathf.Clamp(bounds.size.y, .32f, .92f),
                    Mathf.Max(.70f, bounds.size.z * .94f)));
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

        void FinishOriginalExitDoor()
        {
            // Source GLB uses eight layered door meshes at the original city exit.
            // Their overlapping faces can z-fight as the door rotates on mobile.
            // Keep the EXISTING animated hinge, interaction and physics intact;
            // hide only the old visible leaf surfaces and skin that same pivot
            // with one thick opaque door plus noncoplanar, recessed trim.
            if (!player.door || player.door.name != "Original opening door")
                throw new InvalidOperationException("Original animated city exit door pivot was replaced.");
            for (int index = 24; index <= 31; index++)
            {
                // Door pieces 24..31 were deliberately reparented out of
                // gym into player.door by the ORIGINAL scene builder.
                var part = player.door.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "Gym_Mesh_" + index);
                var renderer = part ? part.GetComponent<Renderer>() : null;
                if (!renderer) throw new InvalidOperationException(
                    "Required original exit door surface missing: " + index);
                renderer.enabled = false;
            }
            var hinge = player.door;
            Box(hinge, "BRAXHORN exterior door solid no-flicker leaf",
                new Vector3(-.79f, 1.50f, .11f),
                new Vector3(1.50f, 2.92f, .080f), vinyl, .018f);
            foreach (float y in new[] { .40f, 1.02f, 1.64f, 2.26f })
                Box(hinge, "Exterior door inset structural emboss",
                    new Vector3(-.79f, y, .164f),
                    new Vector3(1.26f, .017f, .012f), blackSteel, .006f);
            Box(hinge, "Exterior door discreet green identity line",
                new Vector3(-.79f, 2.80f, .166f),
                new Vector3(1.25f, .024f, .012f), green, .004f);
            foreach (float sign in new[] { -1f, 1f })
            {
                float z = .11f + sign * .061f;
                Tube(hinge, "Exterior exit steel handle socket",
                    new Vector3(-1.35f, 1.03f, z),
                    new Vector3(-1.35f, 1.03f, z + sign * .018f),
                    .027f, steel, 1, 12);
                Tube(hinge, "Exterior exit sturdy grab lever",
                    new Vector3(-1.35f, 1.03f, z + sign * .018f),
                    new Vector3(-1.12f, 1.03f, z + sign * .018f),
                    .012f, steel, 1, 12);
            }
            // Nothing about doorAngle, city-world transitions or input changes.
        }

        void BuildContactGrounding()
        {
            // A small shared soft contact mask restores depth at static object
            // bases without enabling expensive city-wide real-time shadows.
            var pixels = new Color32[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float edge = Mathf.Max(Mathf.Abs((x + .5f) / 64f - 1), Mathf.Abs((y + .5f) / 64f - 1));
                float alpha = (1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.73f, 1f, edge))) * .30f;
                pixels[y * 128 + x] = new Color32(0, 0, 0, (byte)(255f * alpha));
            }
            var texture = ControlTexture("StaticContactGrounding", 128, 128, pixels);
            var material = new Material(Shader.Find("Unlit/Transparent")) { name = "Static soft contact", mainTexture = texture };
            AssetDatabase.CreateAsset(material, folder + "/StaticContactGrounding.mat");
            Action<Transform, Vector3, float, float> footprint = (parent, p, width, depth) => {
                var mesh = new Mesh { vertices = new[] {
                    new Vector3(-width / 2, 0, -depth / 2), new Vector3(width / 2, 0, -depth / 2),
                    new Vector3(width / 2, 0, depth / 2), new Vector3(-width / 2, 0, depth / 2) },
                    uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up }, triangles = new[] { 0, 2, 1, 0, 3, 2 } };
                mesh.RecalculateNormals(); var t = MeshObject(parent, "Static contact grounding", mesh, material); t.localPosition = p;
            };
            footprint(hall, new Vector3(-9f, .012f, -5.5f), 9.1f, 9.1f);
            footprint(hall, new Vector3(-2.6f, .012f, 9.65f), 3.9f, 1.3f);
            footprint(hall, new Vector3(-13.80f, .012f, 10.31f), 2.45f, 1.1f);
            footprint(hall, new Vector3(-17.11f, .012f, 7.79f), 1.1f, 2.45f);
            footprint(hall, new Vector3(-15.40f, .012f, 8.46f), 1.34f, .9f);
            footprint(annex, new Vector3(-22.67f, .012f, 5.8f), .82f, 3.15f);
            footprint(annex, new Vector3(-24.92f, .012f, 4.73f), .90f, 5.95f);
            footprint(annex, new Vector3(-24.4f, .012f, -1.92f), .86f, 1.1f);
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
            if (privacy)
            {
                // Only full static opaque boundaries participate in privacy evidence.
                t.gameObject.layer = 11;
                privacyWalls.Add(new Bounds(center, size));
            }
            return c;
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
                    var texcoord = material == wood && Mathf.Abs(n.y) > .5f && size.x > size.z
                        ? new Vector2(ys[y], us[x]) : new Vector2(us[x], ys[y]);
                    uv.Add(texcoord / (material == brick ? 2.2f : material == rubber || material == matRubber ? 2f : 1f));
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
            var shader = Shader.Find("Braxhorn/ControlledBrandPrint");
            if (!shader) throw new InvalidOperationException("Controlled brand print shader missing.");
            var material = new Material(shader) { name = name, mainTexture = image };
            if (material.HasProperty("_Color")) material.color = tint;
            AssetDatabase.CreateAsset(material, folder + "/Decal_" + Safe(name) + ".mat");
            var mesh = new Mesh { vertices = new[] { new Vector3(-width / 2, -height / 2, 0), new Vector3(width / 2, -height / 2, 0), new Vector3(width / 2, height / 2, 0), new Vector3(-width / 2, height / 2, 0) }, uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up }, triangles = new[] { 0, 2, 1, 0, 3, 2 } };
            mesh.RecalculateNormals(); var t = MeshObject(parent, name, mesh, material); t.localPosition = p; t.localRotation = Quaternion.LookRotation(-normal, up);
        }

        void Text(Transform parent, string value, Vector3 p, float width, Color color, Quaternion rotation)
        {
            var t = Group(parent, "Sign " + value); t.localPosition = p;
            var tm = t.gameObject.AddComponent<TextMesh>(); tm.font = font; tm.text = value; tm.fontSize = 64; tm.characterSize = .12f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = color;
            if (value == "BRAXHORN" || value == "KICKBOXING") tm.fontStyle = FontStyle.Bold;
            var r = t.GetComponent<MeshRenderer>(); r.sharedMaterial = worldText; r.shadowCastingMode = ShadowCastingMode.Off;
            font.RequestCharactersInTexture(value, 64, tm.fontStyle);
            worldText.mainTexture = font.material.mainTexture;
            // Measure before rotating wall signs, whose world X bounds can be zero.
            float advance = 0; foreach (char c in value) if (font.GetCharacterInfo(c, out var glyph, 64, tm.fontStyle)) advance += glyph.advance;
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
                // Never collapse or destroy the movable pear's hitbox / rebounding
                // pivot. Other static hall meshes retain normal batching.
                !f.GetComponentInParent<BraxGymSpeedBag>() &&
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
