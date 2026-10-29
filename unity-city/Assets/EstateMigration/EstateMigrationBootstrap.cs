// BRAXHORN CITY - isolated estate migration, original geometry only.
// This is a SOURCE-LOCKED PORT, NOT an approved visual result.
// Unity 6000.0.58f2, com.unity.cloud.gltfast 6.20.0.
using System;
using System.IO;
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
            await AddOriginalBrax();
            ready = true;
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
                if (!renderer.enabled || renderer is ParticleSystemRenderer) continue;
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

        async Task AddOriginalBrax()
        {
            var body = new GameObject("BRAX_ORIGINAL_PLAYER_NoSubstitute");
            body.transform.SetParent(transform, false);
            body.transform.position = WorldPoint(0,.12f,92);
            characterRoot = body.transform;
            character = body.AddComponent<CharacterController>();
            character.radius = .60f;
            character.height = 2.25f;
            character.center = new Vector3(0, 1.125f, 0);
            character.stepOffset = .35f;
            character.slopeLimit = 55f;
            character.skinWidth = .07f;
            try
            {
                var avatar = await InstantiateOriginal("brax.glb.gz","BRAX_ORIGINAL_RIG_AND_ANIMATIONS",body.transform);
                avatar.transform.localPosition = Vector3.zero;
                avatar.transform.localScale = Vector3.one * .024f; // Existing Fighter.SCALE, no guessing.
                braxVisual = avatar.transform;
                braxAnimation = avatar.GetComponentInChildren<Animation>(true);
                braxAvailable = true;
                try
                {
                    var png = await Fetch("brax_color.png");
                    var tex = new Texture2D(2,2,TextureFormat.RGBA32,true,false);
                    if (!tex.LoadImage(png)) throw new IOException("Color PNG decode");
                    tex.wrapMode = TextureWrapMode.Repeat; tex.Apply(true);
                    var normalBytes = await Fetch("brax_normal.png");
                    var normal = new Texture2D(2,2,TextureFormat.RGBA32,true,true);
                    bool hasNormal = normal.LoadImage(normalBytes);
                    foreach (var render in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        var originals = render.materials;
                        for (int i = 0; i < originals.Length; i++)
                        {
                            var shader = Shader.Find("Standard");
                            if (shader == null) throw new IOException("Unity Standard shader absent");
                            var mat = new Material(shader);
                            mat.name = "BraxOriginal_ColorMatching_ThreeSource";
                            mat.mainTexture = tex;
                            mat.color = Color.white;
                            mat.SetFloat("_Metallic", 0f);
                            mat.SetFloat("_Glossiness", .4f);
                            if (hasNormal)
                            {
                                mat.SetTexture("_BumpMap", normal);
                                mat.EnableKeyword("_NORMALMAP");
                            }
                            originals[i] = mat;
                        }
                        render.materials = originals;
                    }
                }
                catch (Exception e)
                {
                    braxAvailable = false;
                    errors.Add("Original Brax texture missing/invalid: " + e.Message);
                }
            }
            catch (Exception e)
            {
                braxAvailable = false; // No substitute object used for the player's visual.
                errors.Add("Original Brax model missing: " + e.Message);
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
            if (!ready || character == null) return;
            Vector2 input = Vector2.zero;
            bool interaction = false, jump = false;
#if ENABLE_LEGACY_INPUT_MANAGER
            input = new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
            if (Input.GetKeyDown(KeyCode.E)) interaction = true;
            if (Input.GetKeyDown(KeyCode.Space)) jump = true;
            bool running = Input.GetKey(KeyCode.LeftShift) || runPressed;
            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * 2.5f;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * .6f,.05f,1.15f);
            }
            if (Input.GetKeyDown(KeyCode.Tab)) runPressed = !runPressed;
#else
            bool running = runPressed;
#endif
            ProcessTouch(ref input, ref interaction, ref jump);
            if (interaction) ToggleNearestDoor();
            UpdateDoors(Time.deltaTime);
            input = Vector2.ClampMagnitude(input,1);
            Vector3 camForward = Vector3.ProjectOnPlane(playerCamera.transform.forward,Vector3.up).normalized;
            if (camForward.sqrMagnitude < .01f) camForward = Vector3.back;
            Vector3 camRight = Vector3.Cross(Vector3.up,camForward).normalized * -1f;
            // Unity right is always positive screen right.
            camRight = playerCamera.transform.right;
            camRight.y = 0; camRight.Normalize();
            var velocity = (camForward * input.y + camRight * input.x) * (running ? runningSpeed : walkingSpeed);
            if (character.isGrounded)
            {
                if (verticalSpeed < 0) verticalSpeed = -1.5f;
                if (jump) verticalSpeed = 6.3f;
            }
            verticalSpeed -= 18f * Time.deltaTime;
            var displacement = velocity + Vector3.up * verticalSpeed;
            character.Move(displacement * Time.deltaTime);
            if (velocity.sqrMagnitude > .01f)
            {
                characterRoot.rotation = Quaternion.Slerp(characterRoot.rotation,
                    Quaternion.LookRotation(velocity,Vector3.up),Time.deltaTime*10);
            }
            // Play only original embedded animation clips (no generic procedurally animated walk).
            if (braxAnimation != null)
            {
                string clip = velocity.sqrMagnitude < .01f ? "Idle" : running ? "Running" : "Walking";
                if (braxAnimation.GetClip(clip)!=null && !braxAnimation.IsPlaying(clip))
                    braxAnimation.CrossFade(clip,.16f);
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
                                     Mathf.Cos(a)*Mathf.Cos(pitch))*dist;
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
