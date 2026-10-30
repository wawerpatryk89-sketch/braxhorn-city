// BRAXHORN CITY - isolated estate migration, original geometry only.
// This is a SOURCE-LOCKED PORT, NOT an approved visual result.
// Unity 6000.0.58f2, com.unity.cloud.gltfast 6.20.0.
using System;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;
using UnityEngine.Networking;

namespace Braxhorn.EstateMigration
{
    [Serializable] public sealed class EstateManifest
    {
        public string status;
        public string[] missingOriginalModels;
        public string generatedGlbSha256;
        public int doorCount;
        public float[] sourceEntrypoint;
        public EstateLevel level;
        public CollisionBox[] collision;
        public WalkSurface[] walkable;
        public SourceModel[] sourceModels;
        public SourceAsset[] binaryAssets;
        public SourceDoor[] doors;
        public VegetationPlacement[] vegetation;
    }
    [Serializable] public sealed class EstateLevel
    {
        public string id;
        public string version;
        public float[] spawn;
        public string[] streets;
    }
    [Serializable] public sealed class CollisionBox
    {
        public float minX, maxX, minY, maxY, minZ, maxZ;
    }
    [Serializable] public sealed class WalkSurface
    {
        public float x0, x1, z0, z1, y, x, z, radius;
    }
    [Serializable] public sealed class SourceModel
    {
        public string name, source;
        public float x, z, width, rotation;
    }
    [Serializable] public sealed class SourceAsset
    {
        public string name, source, sha256;
        public bool present;
    }
    [Serializable] public sealed class SourceDoor
    {
        public string id, label;
        public float x, z, yaw, width, height, @base;
        public bool single, glass, wood;
        public int initialSwing, leafCount;
    }
    [Serializable] public sealed class VegetationPlacement
    {
        public string kind;
        public float x,z,height,rotation;
    }

    public sealed class EstateMigrationBootstrap : MonoBehaviour
    {
        [Header("Source fidelity first: no substitute models")]
        public bool verifySourceHashes = true;
        public bool usePreciseImportedMeshColliders = true;
        public float walkingSpeed = 2f;
        public float runningSpeed = 5f;

        sealed class DoorState
        {
            public SourceDoor source;
            public Transform[] hinges;
            public Quaternion[] rest;
            public BoxCollider blocker;
            public float openFactor;
            public float target;
            public int swing;
        }

        readonly Dictionary<string, GltfImport> imports = new Dictionary<string, GltfImport>();
        readonly List<DoorState> doors = new List<DoorState>();
        readonly List<string> errors = new List<string>();
        EstateManifest manifest;
        Camera playerCamera;
        CharacterController character;
        Transform characterRoot, braxVisual;
        Animation braxAnimation;
        Transform sceneRoot;
        string message = "Wczytywanie oryginalnego osiedla...";
        bool ready, missingAssets, braxAvailable;
        bool runPressed;
        // WebGL bridge consumes the original Braxhorn 3D touch HUD in the HTML template.
        Vector2 webMove = Vector2.zero;
        bool webJoyActive, webPlaying, webPaused, webJumpQueued, webActQueued;
        bool webBlockPressed, heroSwitching, isIsoCamera;
        float physicalSpeed, animationLockUntil;
        string currentHero = "brax";
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void BRX_EstateReady();
        [DllImport("__Internal")] static extern void BRX_HeroLoaded();
#endif
        float verticalSpeed, yaw, pitch = 0.31f, dist = 5.8f;
        int joystickFinger = -1, lookFinger = -1;
        Vector2 joyCenter, joystickInput;
        float lastTouchTimestamp;

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            // Source has physically matte materials. No bloom or invented lighting.
            RenderSettings.ambientLight = new Color(.72f,.75f,.70f);
            var sunObject = new GameObject("EstateDaylight_SOURCE_NEUTRAL");
            sunObject.transform.SetParent(transform);
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f,.97f,.88f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(50f, -28f, 0f);
            var cameraObject = new GameObject("EstatePlayerCamera");
            cameraObject.transform.SetParent(transform);
            playerCamera = cameraObject.AddComponent<Camera>();
            playerCamera.fieldOfView = 55f;
            playerCamera.nearClipPlane = .08f;
            playerCamera.farClipPlane = 400f;
            playerCamera.backgroundColor = new Color(.74f,.80f,.82f);
            playerCamera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.tag = "MainCamera";
        }

        async void Start()
        {
            try { await LoadEstate(); }
            catch (Exception e)
            {
                errors.Add(e.Message);
                message = "Nie można załadować osiedla: " + e.Message;
                Debug.LogError(e);
            }
        }

        static string EstatePath(string filename)
        {
            var dir = Application.streamingAssetsPath.Replace("\\", "/").TrimEnd('/');
            // WebGL: HTTP path; Android: jar:file://; iOS/Editor: file://.
            if (!dir.Contains("://")) dir = "file://" + dir;
            return dir + "/Estate/" + Uri.EscapeDataString(filename);
        }

        async Task<byte[]> Fetch(string filename)
        {
            using (var request = UnityWebRequest.Get(EstatePath(filename)))
            {
                var task = request.SendWebRequest();
                while (!task.isDone) await Task.Yield();
                if (request.result != UnityWebRequest.Result.Success)
                    throw new IOException(filename + ": " + request.error);
                return request.downloadHandler.data;
            }
        }

        async Task<GltfImport> GetImport(string name)
        {
            if (imports.TryGetValue(name, out var cached)) return cached;
            var gltf = new GltfImport();
            bool ok;
            if (name.EndsWith(".glb.gz", StringComparison.OrdinalIgnoreCase))
            {
                var compressed = await Fetch(name);
                byte[] bytes;
                using (var input = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress))
                using (var output = new MemoryStream())
                { input.CopyTo(output); bytes = output.ToArray(); }
                // Original content is still original rig/animations, just gunzipped.
                var logicalName = name.Substring(0, name.Length - 3);
                ok = await gltf.Load(bytes, new Uri(EstatePath(logicalName)));
            }
            else
            {
                ok = await gltf.Load(new Uri(EstatePath(name)));
            }
            if (!ok) { gltf.Dispose(); throw new IOException("glTFast rejected: " + name); }
            imports.Add(name, gltf);
            return gltf;
        }

        async Task<GameObject> InstantiateOriginal(string filename, string title, Transform parent)
        {
            var import = await GetImport(filename);
            var root = new GameObject(title);
            root.transform.SetParent(parent, false);
            if (!await import.InstantiateMainSceneAsync(root.transform))
            {
                Destroy(root);
                throw new IOException("Cannot instantiate original " + filename);
            }
            return root;
        }

        async Task LoadEstate()
        {
            var manifestBytes = await Fetch("EstateSourceManifest.json");
            manifest = JsonUtility.FromJson<EstateManifest>(System.Text.Encoding.UTF8.GetString(manifestBytes));
            if (manifest == null || manifest.sourceModels == null ||
                manifest.level == null || manifest.collision == null || manifest.walkable == null ||
                manifest.doors == null || manifest.vegetation == null)
                throw new InvalidDataException("Original scene manifest missing essential data");
            if (manifest.sourceModels.Length != 21 || manifest.doorCount != 24)
                throw new InvalidDataException("Source layout changed: 21 models / 24 doors expected");
            missingAssets = manifest.missingOriginalModels != null &&
                            manifest.missingOriginalModels.Length != 0;
            sceneRoot = new GameObject("EstateSourceLOCK_ThreeJS_1to1").transform;
            sceneRoot.SetParent(transform, false);

            message = "Import: oryginalna architektura osiedla";
            var procedural = await InstantiateOriginal("EstateProceduralAndDoors.glb",
                                                       "ORIGINAL_ProceduralBuildingsStreetsDoors", sceneRoot);
            procedural.transform.localPosition = Vector3.zero;

            message = "Kolizje i podłoża oryginalnej mapy";
            BuildCollisionGeometry();

            message = "Domy i wyposażenie z oryginalnych plików GLB";
            int successCount = 0;
            for (int i = 0; i < manifest.sourceModels.Length; i++)
            {
                var instance = manifest.sourceModels[i];
                var filename = instance.name + ".glb";
                message = "Oryginalne modele " + (i + 1) + "/21: " + instance.name;
                if (string.IsNullOrEmpty(instance.source))
                {
                    errors.Add("BRAK oryginału: " + filename);
                    continue; // NEVER replace missing models with cubes or guesses.
                }
                try
                {
                    var model = await InstantiateOriginal(filename,
                                        "ORIGINAL_" + i.ToString("00") + "_" + instance.name, sceneRoot);
                    NormalizeExactlyAsSource(model.transform, instance);
                    if (usePreciseImportedMeshColliders) AddPreciseColliders(model);
                    successCount++;
                }
                catch (Exception e)
                {
                    errors.Add(filename + ": " + e.Message);
                    Debug.LogError(e);
                }
            }

            message = "Drzewa, krzewy i skały oryginalnej planszy";
            await LoadVegetation();
            ConnectOriginalDoors(procedural);
            await AddOriginalHero("brax");
            ready = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            BRX_EstateReady();
#else
            webPlaying = true;
#endif
            message = missingAssets || errors.Count > 0
                ? "Unity: WGLĄD TECHNICZNY — NIEKOMPLETNA MIGRACJA 1:1"
                : "Unity: geometria załadowana. Wymaga kontroli 1:1 i testów na telefonie";
            Debug.Log("[Estate] Models " + successCount + "/21; doors " + doors.Count +
                      "/24; collisions " + manifest.collision.Length +
                      "; missing sources " + (missingAssets ? string.Join(", ", manifest.missingOriginalModels) : "none"));
        }

        static Vector3 WorldPoint(float x, float y, float threeZ)
            => new Vector3(x, y, -threeZ);

        // Source formula in dev/osiedle/level.js:
        // yaw -> Box3 width -> scale=targetWidth/rotatedWorldBoundsX ->
        // center -> x-centerX, .08-minY, z-centerZ.
        // glTFast converts right-handed +Z to Unity -Z, hence yaw negation.
        static void NormalizeExactlyAsSource(Transform model, SourceModel source)
        {
            model.rotation = Quaternion.Euler(0, -source.rotation * Mathf.Rad2Deg, 0);
            model.position = Vector3.zero;
            model.localScale = Vector3.one;
            var initial = GetMeshBounds(model);
            if (initial.size.x <= .000001f) throw new InvalidDataException("Source GLB has zero width: " + source.name);
            float factor = source.width / initial.size.x;
            model.localScale *= factor;
            var scaledBounds = GetMeshBounds(model);
            model.position += new Vector3(source.x - scaledBounds.center.x,
                                          .08f - scaledBounds.min.y,
                                          -source.z - scaledBounds.center.z);
        }

        static Bounds GetMeshBounds(Transform parent)
        {
            var filters = parent.GetComponentsInChildren<Renderer>(true);
            bool gotOne = false;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var renderer in filters)
            {
                if (!renderer.enabled || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer)) continue;
                if (!gotOne) { bounds = renderer.bounds; gotOne = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!gotOne) throw new InvalidDataException("GLB contains no renderers: " + parent.name);
            return bounds;
        }

        void BuildCollisionGeometry()
        {
            var colliders = new GameObject("SOURCE_1013_COLLISION_BOXES").transform;
            colliders.SetParent(sceneRoot, false);
            int n = 0;
            foreach (var box in manifest.collision)
            {
                if (box.maxX <= box.minX || box.maxY <= box.minY || box.maxZ <= box.minZ)
                    throw new InvalidDataException("Invalid source collider " + n);
                var go = new GameObject("SourceBox_" + (n++).ToString("0000"));
                go.transform.SetParent(colliders, false);
                go.transform.localPosition = WorldPoint((box.minX + box.maxX) * .5f,
                    (box.minY + box.maxY) * .5f, (box.minZ + box.maxZ) * .5f);
                var collider = go.AddComponent<BoxCollider>();
                collider.size = new Vector3(box.maxX-box.minX,
                    box.maxY-box.minY, box.maxZ-box.minZ);
                go.isStatic = true;
            }

            var floors = new GameObject("SOURCE_WALKABLE_FLOORS").transform;
            floors.SetParent(sceneRoot, false);
            int k = 0;
            foreach (var w in manifest.walkable)
            {
                var round = w.radius > 0;
                var x0 = round ? w.x - w.radius : w.x0;
                var x1 = round ? w.x + w.radius : w.x1;
                var z0 = round ? w.z - w.radius : w.z0;
                var z1 = round ? w.z + w.radius : w.z1;
                if (x1 <= x0 || z1 <= z0) continue;
                var go = new GameObject("SourceFloor_" + (k++).ToString("00"));
                go.transform.SetParent(floors, false);
                go.transform.localPosition = WorldPoint((x0+x1)*.5f,w.y-.055f,(z0+z1)*.5f);
                var c = go.AddComponent<BoxCollider>();
                c.size = new Vector3(x1-x0,.11f,z1-z0);
                go.isStatic = true;
            }
        }

        static void AddPreciseColliders(GameObject imported)
        {
            foreach (var filter in imported.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                try
                {
                    if (filter.GetComponent<Collider>() == null)
                    {
                        var collider = filter.gameObject.AddComponent<MeshCollider>();
                        collider.sharedMesh = filter.sharedMesh;
                        collider.convex = false;
                    }
                    filter.gameObject.isStatic = true;
                }
                catch (Exception e) { Debug.LogWarning("Mesh collider " + filter.name + ": " + e.Message); }
            }
        }

        async Task LoadVegetation()
        {
            var places = manifest.vegetation;
            foreach (var kind in new[] { "tree", "shrub", "rock" })
            {
                var name = "park_" + kind + ".glb";
                var present = false;
                foreach (var asset in manifest.binaryAssets)
                    if (asset.name == "park_" + kind) { present = asset.present; break; }
                if (!present)
                {
                    errors.Add("BRAK oryginalnej roślinności: " + name);
                    continue;
                }
                int n = 0;
                foreach (var placement in places)
                {
                    if (placement.kind != kind) continue;
                    try
                    {
                        var instance = await InstantiateOriginal(name, "SOURCE_" + kind + "_" + n++, sceneRoot);
                        // Source vegetation.js scales source GLB by target height / source bounds.y,
                        // anchors at source bottom, rotates by source radians.
                        instance.transform.rotation = Quaternion.Euler(0, -placement.rotation * Mathf.Rad2Deg, 0);
                        instance.transform.position = Vector3.zero;
                        var before = GetMeshBounds(instance.transform);
                        if (before.size.y <= 0) throw new InvalidDataException("Original foliage has no height");
                        instance.transform.localScale *= placement.height / before.size.y;
                        var after = GetMeshBounds(instance.transform);
                        instance.transform.position += new Vector3(
                            placement.x - after.center.x,
                            .07f - after.min.y,
                            -placement.z - after.center.z);
                    }
                    catch (Exception e) { errors.Add(name + ": " + e.Message); break; }
                }
            }
        }

        static Transform FindDeep(Transform parent, string exact)
        {
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == exact) return t;
            return null;
        }

        void ConnectOriginalDoors(GameObject original)
        {
            foreach (var d in manifest.doors)
            {
                var door = new DoorState {
                    source = d, swing = d.initialSwing == -1 ? -1 : 1,
                    hinges = new Transform[d.leafCount], rest = new Quaternion[d.leafCount]
                };
                for (int i = 0; i < d.leafCount; i++)
                {
                    door.hinges[i] = FindDeep(original.transform, "HINGE_" + d.id + "_" + i);
                    if (door.hinges[i] == null)
                    {
                        errors.Add("Door hinge not exported: " + d.id + "/" + i);
                        continue;
                    }
                    door.rest[i] = door.hinges[i].localRotation;
                }

                var blocker = new GameObject("SourceDoorCollision_" + d.id);
                blocker.transform.SetParent(sceneRoot, false);
                blocker.transform.localPosition = WorldPoint(d.x, 0, d.z);
                blocker.transform.localRotation = Quaternion.Euler(0, -d.yaw * Mathf.Rad2Deg, 0);
                var c = blocker.AddComponent<BoxCollider>();
                c.center = new Vector3(0, d.@base + d.height*.5f, 0);
                c.size = new Vector3(d.width-.15f, d.height, .16f);
                door.blocker = c;
                doors.Add(door);
            }
        }

        // A source-faithful hero: exact original rig/animation GLB and original PNG-converted WebP.
        // Both characters are available in dev/osiedle/main.js and BRAXHORN CITY selection.
        async Task AddOriginalHero(string heroKey)
        {
            if (heroKey != "brax" && heroKey != "nyxa")
                throw new ArgumentException("Unrecognized original player: " + heroKey);
            var previous = characterRoot;
            var body = new GameObject(heroKey.ToUpperInvariant()+"_ORIGINAL_PLAYER_NoSubstitute");
            body.transform.SetParent(transform, false);
            body.SetActive(false);
            body.transform.position = WorldPoint(0,.12f,92);
            var controller = body.AddComponent<CharacterController>();
            controller.radius = .60f;
            controller.height = 2.25f;
            controller.center = new Vector3(0, 1.125f, 0);
            controller.stepOffset = .35f;
            controller.slopeLimit = 55f;
            controller.skinWidth = .07f;
            try
            {
                var avatar = await InstantiateOriginal(heroKey+".glb.gz",
                    heroKey.ToUpperInvariant()+"_ORIGINAL_RIG_AND_ANIMATIONS",body.transform);
                avatar.transform.localPosition = Vector3.zero;
                avatar.transform.localScale = Vector3.one * .024f; // dev/src/fighter.js original SCALE.
                var animation = avatar.GetComponentInChildren<Animation>(true);
                var colorBytes = await Fetch(heroKey+"_color.png");
                var color = new Texture2D(2,2,TextureFormat.RGBA32,true,false);
                if (!color.LoadImage(colorBytes)) throw new IOException("Original hero texture cannot decode");
                color.wrapMode = TextureWrapMode.Repeat; color.Apply(true);
                var normalBytes = await Fetch(heroKey+"_normal.png");
                var normal = new Texture2D(2,2,TextureFormat.RGBA32,true,true);
                bool normalAvailable = normal.LoadImage(normalBytes);
                var shader = Shader.Find("Standard");
                if (shader == null) throw new IOException("Original hero Unity Standard shader missing");
                foreach (var render in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var mats=render.materials;
                    for (int i=0; i<mats.Length; i++)
                    {
                        var mat=new Material(shader);
                        mat.name=heroKey+"_Original_Source_Material";
                        mat.mainTexture=color;mat.color=Color.white;
                        mat.SetFloat("_Metallic",0f);
                        mat.SetFloat("_Glossiness",.4f);
                        if(normalAvailable)
                        {
                            mat.SetTexture("_BumpMap",normal);
                            mat.EnableKeyword("_NORMALMAP");
                        }
                        mats[i]=mat;
                    }
                    render.materials=mats;
                }
                characterRoot=body.transform;
                character=controller;
                braxVisual=avatar.transform;
                braxAnimation=animation;
                braxAvailable=true;
                currentHero=heroKey;
                if(previous!=null) Destroy(previous.gameObject);
                body.SetActive(true);
                if(animation!=null && animation.GetClip("Idle")!=null)
                    animation.CrossFade("Idle",.15f);
                else
                {
                    // Never claim that an original animation played unless it really exists.
                    Debug.LogWarning("SOURCE ANIMATION BLOCKED: "+heroKey+
                        " - glTFast LegacyAnimation/Idle not present. Movement will work, clip QA fails.");
                }
                Debug.Log("[Estate] Original "+heroKey+" source model and WebP textures loaded.");
            }
            catch(Exception error)
            {
                Destroy(body);
                errors.Add("Original hero "+heroKey+" loading failed: "+error.Message);
                Debug.LogError(error);
                if(previous==null)throw;
                throw;
            }
        }

        // WebGL's HUD is based on the real dev/src/template.html.
        // Messages are received only by the isolated estate controller.
        static float ParseInput(string value)
        {
            if(!float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var v))
                return 0f;
            return float.IsFinite(v)?v:0f;
        }

        public void OnWebJoystick(string payload)
        {
            var p=(payload??"").Split(',');
            if(p.Length!=3)return;
            webMove=Vector2.ClampMagnitude(new Vector2(ParseInput(p[0]),ParseInput(p[1])),1);
            webJoyActive=p[2]=="1";
            if(!webJoyActive)webMove=Vector2.zero;
        }

        public void OnWebLook(string payload)
        {
            var p=(payload??"").Split(',');
            if(p.Length!=2||webPaused||!webPlaying)return;
            // Original source dev/osiedle/main.js: yaw-=dx*.006 rad; pitch+=dy*.004 rad.
            yaw -= ParseInput(p[0])*.006f*Mathf.Rad2Deg;
            pitch=Mathf.Clamp(pitch+ParseInput(p[1])*.004f,.05f,1.2f);
        }

        void PlayOriginalAction(string action)
        {
            if(braxAnimation==null)
            {
                Debug.LogWarning("No source Animation component for "+currentHero+" action "+action);
                return;
            }
            string clip=null;
            switch(action)
            {
                case "punch": clip="Jab Cross (1)"; break;
                case "kick": clip="Mma Kick (1)"; break;
                case "special": clip=currentHero=="nyxa"?"Martelo Do Chau":"Illegal Knee"; break;
                case "dodge": clip="Dodging"; break;
                case "hold:block:1": clip="Center Block"; break;
            }
            if(clip==null)return;
            var original=braxAnimation.GetClip(clip);
            if(original==null)
            {
                Debug.LogWarning("Original animation clip '"+clip+"' absent from runtime import.");
                return;
            }
            braxAnimation.CrossFade(clip,.12f);
            animationLockUntil=Time.time+Mathf.Min(3f,original.length+.06f);
        }

        public async void OnWebAction(string command)
        {
            if(!ready||string.IsNullOrWhiteSpace(command))return;
            if(command.StartsWith("hero:"))
            {
                if(heroSwitching)return;
                var next=command.Substring(5);
                if(next!="brax"&&next!="nyxa")return;
                heroSwitching=true;
                try
                {
                    if(next!=currentHero)await AddOriginalHero(next);
                    webPlaying=true;webPaused=false;
                    physicalSpeed=0;
#if UNITY_WEBGL && !UNITY_EDITOR
                    BRX_HeroLoaded();
#endif
                }
                catch(Exception e){Debug.LogError("Hero select failed: "+e);}
                finally {heroSwitching=false;}
                return;
            }
            if(command=="pause"){webPaused=true;webMove=Vector2.zero;webJoyActive=false;physicalSpeed=0;return;}
            if(command=="resume"){webPaused=false;return;}
            if(!webPlaying||webPaused)return;
            switch(command)
            {
                case "jump": webJumpQueued=true;PlayOriginalAction("jump");return;
                case "act": webActQueued=true;return;
                case "punch":case "kick":case "special":case "dodge":PlayOriginalAction(command);return;
                case "hold:block:1":webBlockPressed=true;PlayOriginalAction(command);return;
                case "hold:block:0":webBlockPressed=false;return;
                case "cam":isIsoCamera=!isIsoCamera;return;
                case "throw":Debug.LogWarning("Throw requires a real opponent and original two-body throw; not present in isolated estate preview.");return;
            }
            if(command.StartsWith("zoom:"))
            {
                dist=Mathf.Clamp(dist*(1+ParseInput(command.Substring(5))),3f,10f);
                return;
            }
            if(command.StartsWith("goto:"))
            {
                float x=0,z=92;
                switch(command.Substring(5))
                {
                    case "spawn":x=0;z=52;break;
                    case "park":x=20;z=-83;break;
                    case "court":x=-28;z=-72;break;
                    case "villa":x=0;z=-24;break;
                    case "terrace":x=0;z=-51;break;
                    case "garages":x=-24;z=-25;break;
                    case "contact":x=30;z=-28;break;
                    case "casino":x=-86;z=-23;break;
                    case "club":x=-86;z=33;break;
                    case "police":x=86;z=-23;break;
                    case "hospital":x=86;z=33;break;
                    case "boxing":x=-86;z=83;break;
                    case "arena":x=86;z=83;break;
                    default:return;
                }
                if(characterRoot!=null)
                {
                    character.enabled=false;
                    characterRoot.position=WorldPoint(x,.12f,z);
                    character.enabled=true;
                    physicalSpeed=0;
                }
            }
        }

        DoorState NearestDoor()
        {
            if (characterRoot == null) return null;
            float dist = float.PositiveInfinity;
            DoorState result = null;
            foreach (var d in doors)
            {
                var pos = WorldPoint(d.source.x, 0, d.source.z);
                float distance = Vector2.Distance(
                  new Vector2(characterRoot.position.x, characterRoot.position.z),
                  new Vector2(pos.x,pos.z));
                if (distance < 2.2f && distance < dist) { dist = distance; result = d; }
            }
            return result;
        }

        void ToggleNearestDoor()
        {
            var d = NearestDoor();
            if (d == null) return;
            var opening = d.target < .5f;
            if (!opening)
            {
                var player = characterRoot.position;
                var door = WorldPoint(d.source.x,0,d.source.z);
                if (Vector3.Distance(new Vector3(player.x,0,player.z),
                                     new Vector3(door.x,0,door.z)) < .9f) return;
            }
            if (d.source.single)
            {
                var relative = characterRoot.position - WorldPoint(d.source.x,0,d.source.z);
                var local = Quaternion.Inverse(Quaternion.Euler(0,-d.source.yaw*Mathf.Rad2Deg,0)) * relative;
                d.swing = local.z >= 0 ? 1 : -1;
            }
            d.target = opening ? 1f : 0f;
            if (opening) d.blocker.enabled = false; // collision remains on when closed.
        }

        void UpdateDoors(float dt)
        {
            foreach (var d in doors)
            {
                d.openFactor = Mathf.MoveTowards(d.openFactor, d.target, 1.3f * dt);
                for (int i = 0; i < d.hinges.Length; i++)
                {
                    if (!d.hinges[i]) continue;
                    int side = i == 0 ? -1 : 1;
                    // True mirrored Three.js source door leaf rotation, not generic slide.
                    d.hinges[i].localRotation = d.rest[i] *
                        Quaternion.Euler(0, side * 90f * d.openFactor * d.swing,0);
                }
                if (d.openFactor < .001f && d.target < .5f) d.blocker.enabled = true;
            }
        }

        void Update()
        {
            if(!ready||character==null||heroSwitching)return;
#if UNITY_WEBGL && !UNITY_EDITOR
            if(!webPlaying||webPaused)return;
#endif
            Vector2 input=Vector2.zero;
            bool interaction=false,jump=false;
            bool running=true;
#if ENABLE_LEGACY_INPUT_MANAGER
            input=new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
            running=!Input.GetKey(KeyCode.LeftShift); // original shift toggles walking.
            if(Input.GetKeyDown(KeyCode.O))interaction=true;
            if(Input.GetKeyDown(KeyCode.Space))jump=true;
            if(Input.GetMouseButton(1))
            {
                yaw-=Input.GetAxis("Mouse X")*2.5f;
                pitch=Mathf.Clamp(pitch+Input.GetAxis("Mouse Y")*.006f,.05f,1.2f);
            }
            if(Input.GetKeyDown(KeyCode.J))PlayOriginalAction("punch");
            if(Input.GetKeyDown(KeyCode.K))PlayOriginalAction("kick");
            if(Input.GetKeyDown(KeyCode.I))PlayOriginalAction("special");
            if(Input.GetKeyDown(KeyCode.X))PlayOriginalAction("dodge");
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
            // Exact original floating joystick is managed by DOM pointer events.
            if(webJoyActive)
            {
                input=webMove;
                running=input.magnitude>.62f;
            }
            jump |= webJumpQueued;
            interaction |= webActQueued;
            webJumpQueued=webActQueued=false;
#else
            ProcessTouch(ref input,ref interaction,ref jump);
#endif
            if(interaction)ToggleNearestDoor();
            UpdateDoors(Time.deltaTime);
            input=Vector2.ClampMagnitude(input,1f);
            float mag=input.magnitude;
            var camForward=Vector3.ProjectOnPlane(playerCamera.transform.forward,Vector3.up).normalized;
            if(camForward.sqrMagnitude<.01f)camForward=Vector3.forward;
            var camRight=Vector3.ProjectOnPlane(playerCamera.transform.right,Vector3.up).normalized;
            float rawSpeed=running?(mag>.62f?runningSpeed:walkingSpeed*1.1f):
                          walkingSpeed*Mathf.Min(1f,.35f+mag);
            var targetSpeed=mag>.08f?rawSpeed:0f;
            physicalSpeed=Mathf.MoveTowards(physicalSpeed,targetSpeed,
                (targetSpeed>physicalSpeed?12f:16f)*Time.deltaTime);
            var dir=camForward*input.y+camRight*input.x;
            if(dir.sqrMagnitude>.001f)dir.Normalize();
            var velocity=dir*physicalSpeed;
            if(character.isGrounded)
            {
                if(verticalSpeed<0)verticalSpeed=-1.5f;
                if(jump)
                {
                    verticalSpeed=6.3f;
                    PlayOriginalAction("jump");
                }
            }
            verticalSpeed-=18f*Time.deltaTime;
            character.Move((velocity+Vector3.up*verticalSpeed)*Time.deltaTime);
            if(velocity.sqrMagnitude>.01f)
                characterRoot.rotation=Quaternion.Slerp(characterRoot.rotation,
                    Quaternion.LookRotation(velocity,Vector3.up),Time.deltaTime*10f);
            // Real original GLB clips, never artificial sine-wave animations.
            if(braxAnimation!=null&&Time.time>=animationLockUntil)
            {
                var clip=physicalSpeed<.1f?"Idle":physicalSpeed>3f?"Running":"Walking";
                if(braxAnimation.GetClip(clip)!=null&&!braxAnimation.IsPlaying(clip))
                    braxAnimation.CrossFade(clip,.15f);
            }
        }

        void ProcessTouch(ref Vector2 motion, ref bool interact, ref bool jump)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount == 0)
            {
                joystickFinger = lookFinger = -1;
                joystickInput = Vector2.zero;
                return;
            }
            var w = Screen.width;
            var h = Screen.height;
            var interactionRect = new Rect(w*.79f, h*.20f, w*.20f,h*.22f);
            var jumpRect = new Rect(w*.79f, h*.02f,w*.20f,h*.15f);
            foreach (var touch in Input.touches)
            {
                var position = touch.position; // Unity native touch origin bottom-left
                if (touch.phase == TouchPhase.Began)
                {
                    if (interactionRect.Contains(position)) { interact = true; continue; }
                    if (jumpRect.Contains(position)) { jump = true; continue; }
                    if (position.x < w*.45f && position.y < h*.65f)
                    {
                        joystickFinger = touch.fingerId; joyCenter = position;
                    }
                    else if (position.x > w*.48f && position.y < h*.85f)
                    {
                        lookFinger = touch.fingerId;
                    }
                }
                if (touch.fingerId == joystickFinger)
                {
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        joystickFinger = -1; joystickInput = Vector2.zero;
                    }
                    else joystickInput = Vector2.ClampMagnitude((position-joyCenter)/75f,1);
                }
                else if (touch.fingerId == lookFinger)
                {
                    if (touch.phase == TouchPhase.Moved)
                    {
                        yaw += touch.deltaPosition.x*.17f;
                        pitch = Mathf.Clamp(pitch-touch.deltaPosition.y*.0023f,.05f,1.15f);
                    }
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) lookFinger = -1;
                }
            }
            if (joystickFinger != -1) motion = joystickInput;
#endif
        }

        void LateUpdate()
        {
            if (!playerCamera) return;
            Vector3 target = characterRoot ? characterRoot.position+Vector3.up*1.55f : Vector3.zero;
            float a = yaw * Mathf.Deg2Rad;
            var offset = new Vector3(Mathf.Sin(a)*Mathf.Cos(pitch),Mathf.Sin(pitch),
                                     -Mathf.Cos(a)*Mathf.Cos(pitch))*(isIsoCamera?17f:dist);
            var desired = target+offset;
            var dir = desired-target;
            if (Physics.SphereCast(target,.22f,dir.normalized,out var hit,
                dir.magnitude,~0,QueryTriggerInteraction.Ignore))
                desired = target+dir.normalized*Mathf.Max(.7f,hit.distance-.3f);
            playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position,desired,
                                            1f-Mathf.Exp(-15f*Time.deltaTime));
            playerCamera.transform.LookAt(target);
        }

        void OnGUI()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // The real BRAXHORN HUD is in the WebGL template, not temporary IMGUI boxes.
            return;
#endif
            var w = Screen.width; var h = Screen.height;
            GUI.skin.box.alignment = TextAnchor.MiddleCenter;
            var top = new Rect(12,12,Mathf.Min(675,w-24),60);
            GUI.Box(top,"BRAXHORN CITY  •  OSIEDLE  •  UNITY 6\n"+message);
            if (errors.Count > 0)
            {
                var text = string.Join("  |  ", errors);
                GUI.Box(new Rect(12,75,Mathf.Min(700,w-24),58),text);
            }
            if (!ready) return;
            if (joystickFinger >= 0)
            {
                var y = h-joyCenter.y;
                GUI.Box(new Rect(joyCenter.x-45,y-45,90,90),"RUCH");
                GUI.Box(new Rect(joyCenter.x+joystickInput.x*40-16,y-joystickInput.y*40-16,32,32),"●");
            }
            else GUI.Box(new Rect(22,h-124,116,90),"RUCH\njoystick");
            GUI.Box(new Rect(w-130,h-128,120,52),"KAMERA\nprzesuń palcem");
            GUI.Box(new Rect(w-130,h-182,120,46),"SKOK");
            var d = NearestDoor();
            if (d != null) GUI.Box(new Rect(w-130,h-290,120,60),
                 (d.target < .5f ? "OTWÓRZ" : "ZAMKNIJ")+"\n"+d.source.label);
            GUI.Box(new Rect(12,h-37,Mathf.Min(540,w-24),31),
              "WASD / Shift / Spacja / E  •  dotyk: lewy ruch, prawy kamera");
        }

        void OnDestroy()
        {
            foreach (var item in imports.Values) item.Dispose();
            imports.Clear();
        }
    }
}
