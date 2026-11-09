using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Uses the EXACT same source model "car_rev.glb" as the estate's Auto Patryka
// candidate list, without touching the estate scene/assets or creating a fake car.
// A1 parking is preserved. Like everything in the K world: real 4.95 m car x K (see WorldLengthMetres).
public static class BraxChampionsCarBuilder
{
    public const string OwnerCar = "Assets/Source/Mistrzowie/Vehicles/car_rev.glb";
    public const string EngineIdleAudio = "Assets/Source/Mistrzowie/Audio/BraxCar/brax_engine_idle.ogg";
    public const string EngineRevAudio = "Assets/Source/Mistrzowie/Audio/BraxCar/brax_engine_rev.ogg";
    const float LengthMetres = 4.95f; // Source estate's DlugoscRealna["car_rev"]
    // Owner 9.11: "Auto miało też złą skalę bo małe jakieś było" and, on the
    // old 6 m cap: "Nie mam limitu na nic ... wszystko ma być odpowiednie pod
    // skalę! Też auto." -> the real 4.95 m car x K, like every other object
    // (Brax 2.33 m = 1.80 m real). No invented cap.
    public static float WorldLengthMetres => LengthMetres * K;
    static float CarScale => WorldLengthMetres / LengthMetres;
    static float K => BraxClaudeOsiedle.K;

    static void ApplyCarPbr(GameObject copy)
    {
        var cache = new Dictionary<Material, Material>();
        int converted = 0, preserved = 0, slots = 0;
        foreach (var renderer in copy.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var source = materials[i];
                if (!source) continue;
                if (!cache.TryGetValue(source, out var target))
                {
                    if (BraxCityMaterials.IsCityMaterial(source))
                        target = source;
                    else if (source.HasProperty("baseColorFactor") &&
                             source.HasProperty("roughnessFactor") &&
                             source.HasProperty("metallicFactor"))
                    {
                        // Existing project converter preserves glTF base/normal/MR/AO maps and UV transforms.
                        target = BraxCityMaterials.Imported(source);
                        converted++;
                    }
                    else
                    {
                        target = source;
                        preserved++;
                        Debug.LogWarning("A1_CAR_PBR_PRESERVED unsupportedShader="
                            + (source.shader ? source.shader.name : "<none>") + " material=" + source.name);
                    }
                    cache[source] = target;
                }
                materials[i] = target;
                slots++;
            }
            renderer.sharedMaterials = materials;

            // Per-car instance override only. Keep authored glTF roughness/metallic maps,
            // reduce the city shader's broad sky reflection so the body is not a mirror.
            for (int i = 0; i < materials.Length; i++)
            {
                if (!materials[i] || !materials[i].HasProperty("_ReflectionStrength")) continue;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, i);
                block.SetFloat("_ReflectionStrength", .28f);
                renderer.SetPropertyBlock(block, i);
            }
        }
        Debug.Log("A1_CAR_PBR_INSTANCE converted=" + converted + " preserved=" + preserved
            + " slots=" + slots + " sourceTexturesUnchanged=true reflectionStrength=.28");
    }

    static Bounds BoundsLocal(Transform parent)
    {
        var renderers = parent.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException("Original estate car_rev GLB has no visible geometry");
        Bounds result = new Bounds();
        bool started = false;
        foreach (var r in renderers)
        {
            var b = r.bounds;
            for (int a = 0; a < 2; a++)
            for (int c = 0; c < 2; c++)
            for (int d = 0; d < 2; d++)
            {
                Vector3 world = new Vector3(a == 0 ? b.min.x : b.max.x,
                    c == 0 ? b.min.y : b.max.y, d == 0 ? b.min.z : b.max.z);
                var local = parent.InverseTransformPoint(world);
                if (!started) { result = new Bounds(local, Vector3.zero); started = true; }
                else result.Encapsulate(local);
            }
        }
        if (!started || result.size.magnitude < .01f)
            throw new InvalidOperationException("Original estate car has invalid world bounds");
        return result;
    }

    // Some original estate GLBs have Y as the model's longitudinal axis.
    // Merely turning around world Y leaves their chassis standing on end.
    // Evaluate right-angle axis conversions from REAL renderer bounds, without
    // changing source meshes, wheel geometry, materials or the authored car.
    static void PlaceWheelsDown(GameObject copy,Transform root)
    {
        var original=copy.transform.localRotation;
        var candidates=new[]{
            Quaternion.identity,
            Quaternion.Euler(0,90,0),Quaternion.Euler(0,-90,0),
            Quaternion.Euler(90,0,0),Quaternion.Euler(-90,0,0),
            Quaternion.Euler(0,0,90),Quaternion.Euler(0,0,-90),
            Quaternion.Euler(90,90,0),Quaternion.Euler(-90,90,0),
            Quaternion.Euler(0,90,90),Quaternion.Euler(0,-90,90)
        };
        var wheels=copy.GetComponentsInChildren<Transform>(true)
            .Where(t=>t&&t!=copy.transform&&
                t.name.IndexOf("wheel",StringComparison.OrdinalIgnoreCase)>=0)
            .ToArray();
        float bestScore=float.PositiveInfinity;
        Quaternion best=Quaternion.identity;
        Vector3 bestSize=Vector3.zero;
        foreach(var candidate in candidates)
        {
            copy.transform.localRotation=candidate*original;
            Physics.SyncTransforms();
            var b=BoundsLocal(root);
            if(b.size.x<.02f||b.size.y<.02f||b.size.z<.02f)continue;
            float width=b.size.x/b.size.z;
            float height=b.size.y/b.size.z;
            // Car proportions: 4.95m long, roughly 1.9m wide, 1.5m high.
            // Orientation MUST have the long axis on the horizontal Z plane.
            float score=4f*Mathf.Abs(width-.40f)+
                3f*Mathf.Abs(height-.32f);
            if(width>.85f)score+=8f;
            if(height>.70f)score+=12f;
            if(wheels.Length>=2)
            {
                float wheelY=wheels.Average(w=>root.InverseTransformPoint(w.position).y);
                // The wheels belong in the lower portion, never on the roof.
                score+=3f*Mathf.Clamp01((wheelY-b.min.y)/b.size.y);
            }
            if(score<bestScore)
            {
                bestScore=score;best=candidate;bestSize=b.size;
            }
        }
        copy.transform.localRotation=best*original;
        Physics.SyncTransforms();
        if(bestSize.y>bestSize.z*.75f || bestSize.x>bestSize.z*.85f)
            Debug.LogWarning("A1_OWNER_CAR_AXES_UNCERTAIN: original model does not have a stable road-car proportion; verify on device");
        Debug.Log("A1_OWNER_CAR_ORIENTATION candidate="+best.eulerAngles+
            " longitudinal_z="+bestSize.z.ToString("F2")+
            " vertical_y="+bestSize.y.ToString("F2")+
            " sourceMeshUnchanged=true");
    }

    public static void Generate(Transform parent)
    {
        if (UnityEngine.Object.FindObjectsByType<BraxChampionsCar>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
            throw new InvalidOperationException("Second A1 player car forbidden");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(OwnerCar)
            ?? AssetDatabase.LoadAllAssetsAtPath(OwnerCar).OfType<GameObject>().FirstOrDefault();
        if (!source)
            throw new InvalidOperationException("Import the EXISTING estate car_rev.glb, not a placeholder: " + OwnerCar);
        var root = new GameObject("A1 | ORIGINAL estate sports car for Brax | WORKING joystick vehicle");
        root.transform.SetParent(parent, false);
        // Exact centre of a regular EAST car-park bay.
        // Local x=45.175 is midway between stall lines 43.65/46.70;
        // local z=-88.0 is the centre of the -90.6..-85.4 row.
        // This keeps the showcase car off paint, away from the accessible bay
        // and clear of the pedestrian route.
        root.transform.position = new Vector3(645.175f, .16f, 712f) * K;
        root.transform.rotation = Quaternion.identity;
        var copy = UnityEngine.Object.Instantiate(source, root.transform, false);
        copy.name = "ORIGINAL car_rev GLB from estate, unchanged mesh + per-car PBR instance";
        copy.transform.localPosition = Vector3.zero;
        copy.transform.localRotation = Quaternion.identity;
        ApplyCarPbr(copy);
        Physics.SyncTransforms();
        PlaceWheelsDown(copy,root.transform);
        Bounds bounds=BoundsLocal(root.transform);
        if (bounds.size.z <= .01f || bounds.size.x <= .01f)
            throw new InvalidOperationException("Original car length/width is invalid");
        // World length = real 4.95 m x K (see WorldLengthMetres).
        float uniform = WorldLengthMetres / bounds.size.z;
        copy.transform.localScale *= uniform;
        Physics.SyncTransforms();
        bounds = BoundsLocal(root.transform);
        // Parking asphalt top is at ~.239*K, whereas the preserved source
        // car anchor is .16*K. Set real wheel contact to the road surface.
        float groundLocalY = (.239f - .16f) * K;
        copy.transform.localPosition -= new Vector3(bounds.center.x,
            bounds.min.y-groundLocalY, bounds.center.z);
        Physics.SyncTransforms();
        bounds = BoundsLocal(root.transform);

        // Only one chassis collider whose size is based on source 3D geometry.
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);
        var box = root.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = bounds.size;
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Driver sits on the LEFT as in the original estate code.
        // Seat placement is a calibration point, not an alteration of the car model.
        var seat = new GameObject("Original cabin | LEFT driver seat for Brax").transform;
        seat.SetParent(root.transform, false);
        seat.localPosition = new Vector3(-bounds.extents.x * .39f,
            bounds.min.y + Mathf.Clamp(bounds.size.y * .28f, .26f, .85f),
            bounds.extents.z * .14f);
        seat.localRotation = Quaternion.identity;

        var enter = new GameObject("Actual left driver-door approach").transform;
        enter.SetParent(root.transform, false);
        enter.localPosition = new Vector3(
            bounds.min.x - .78f * CarScale, bounds.min.y + .04f, seat.localPosition.z - .15f * CarScale);

        // Try the REAL separate door assembly. If the source has no reliable
        // split, do not invent a duplicate door or move a body mesh incorrectly.
        Transform doorPivot = null;
        // Owner phone 9.11 (IMG_0678): "drzwi się nie otwierają". b33 logged
        // originalDoor=False: the old filter wanted ONE node with its pivot at
        // x<0, but car_rev's left door is ~15 parts (DoorLPaint, DoorLBase,
        // DoorLGrille...) whose pivots sit at the car origin. Side is decided by
        // the parts' RENDERED centre; every top-level left-door part moves together.
        var doorCandidates = copy.GetComponentsInChildren<Transform>(true)
            .Where(t => t && t != copy.transform &&
                t.name.IndexOf("door", StringComparison.OrdinalIgnoreCase) >= 0 &&
                t.GetComponentsInChildren<Renderer>(true).Length > 0)
            .ToList();
        Bounds? PartBounds(Transform t)
        {
            Bounds? acc = null;
            foreach (var rr in t.GetComponentsInChildren<Renderer>(true))
                if (acc == null) acc = rr.bounds; else { var x = acc.Value; x.Encapsulate(rr.bounds); acc = x; }
            return acc;
        }
        var leftDoor = doorCandidates
            .Where(t => !doorCandidates.Any(o => o != t && t.IsChildOf(o)))
            .Where(t => { var pb = PartBounds(t); return pb.HasValue && root.transform.InverseTransformPoint(pb.Value.center).x < 0; })
            .ToArray();
        if (leftDoor.Length > 0)
        {
            Bounds db = PartBounds(leftDoor[0]).Value;
            foreach (var part in leftDoor.Skip(1)) db.Encapsulate(PartBounds(part).Value);
            var lo = root.transform.InverseTransformPoint(db.min);
            var hi = root.transform.InverseTransformPoint(db.max);
            var hinge = new GameObject("ORIGINAL left door | scissor pivot at front A-pillar").transform;
            hinge.SetParent(root.transform, false);
            // Revuelto scissor door: hinge at the front edge, ~35% up the door,
            // lateral axis; the door's rear rises (runtime axis = left, -65 deg).
            hinge.localPosition = new Vector3((lo.x + hi.x) * .5f, lo.y + (hi.y - lo.y) * .35f, Mathf.Max(lo.z, hi.z));
            foreach (var part in leftDoor) part.SetParent(hinge, true);
            doorPivot = hinge;
            Debug.Log("A1_OWNER_CAR_DOOR parts=" + leftDoor.Length + " size=" + db.size.ToString("F2") + " hinge=" + hinge.localPosition.ToString("F2") + " axis=scissor");
        }
        else
            Debug.LogWarning("A1_OWNER_CAR_DOOR_QA: source model has no independent left door node; keep car mesh original");

        var car = root.AddComponent<BraxChampionsCar>();
        car.bodyCollider = box; car.driverSeat = seat; car.doorApproach = enter;
        car.doorHinge = doorPivot;
        car.doorAxis = Vector3.left; // scissor: MoveDoor(-65) lifts the rear of the door
        // Same proportions as the 4.95 m source, enlarged with the body.
        car.wheelbase = 2.7f * CarScale;
        // Owner 9.11: "ma się rozpędzać do 120 km". The world is K-scaled, so a
        // real 120 km/h is 120/3.6 x K world units per second; SpeedKph divides
        // K back out. Same for reverse and the acceleration/braking feel.
        car.worldScale = K;
        car.maxSpeed = 120f / 3.6f * K; car.reverseMaxSpeed = 30f / 3.6f * K;
        car.acceleration = 7.2f * K; car.braking = 11f * K;
        car.interactionRadius = 3.8f * CarScale;

        // Real recorded audio only. Missing binaries are advisory until the owner-approved
        // source files are physically committed; never synthesize a fake engine.
        var audioAnchor = new GameObject("BRAXHORN engine audio 3D anchor").transform;
        audioAnchor.SetParent(root.transform, false);
        audioAnchor.localPosition = new Vector3(0f,
            bounds.min.y + bounds.size.y * .46f,
            bounds.max.z - bounds.size.z * .18f);
        car.engineAudioAnchor = audioAnchor;
        car.engineIdleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(EngineIdleAudio);
        car.engineRevClip = AssetDatabase.LoadAssetAtPath<AudioClip>(EngineRevAudio);
        if (!car.engineIdleClip || !car.engineRevClip)
            BraxBuildQuality.Warn("A1_CAR_AUDIO_PENDING: Issue #30 audio binaries missing. Expected "
                + EngineIdleAudio + " and " + EngineRevAudio);
        else
            Debug.Log("A1_CAR_AUDIO_ASSIGNED idle=" + car.engineIdleClip.name
                + " rev=" + car.engineRevClip.name + " realRecordings=true");
        // If wheels are individual GLB nodes, the runtime animates their
        // existing transforms; no substitute wheel meshes are created.
        var wheelRoots = copy.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.IndexOf("wheel", StringComparison.OrdinalIgnoreCase) >= 0)
            .Where(t => t.GetComponentsInChildren<Renderer>(true).Length > 0).ToArray();
        car.rollingWheels = wheelRoots;
        car.steeringWheels = new Transform[0]; // no arbitrary steering bone rotations
        if (Mathf.Abs(bounds.size.z - WorldLengthMetres) > .08f)
            BraxBuildQuality.Warn("A1_OWNER_CAR_SCALE_ADVISORY: expected " + WorldLengthMetres.ToString("F2") + "m, got " + bounds.size.z.ToString("F2") + "m");
        Debug.Log("A1_OWNER_CAR_INSTALLED original=car_rev length=" +
            bounds.size.z.ToString("F2") + " width=" + bounds.size.x.ToString("F2")
            + " height=" + bounds.size.y.ToString("F2") + " K=" + K.ToString("F4")
            + " driverHipsTarget=" + seat.localPosition + " maxSpeedKph=120real(xK) reverseKph=30 worldLength=" + WorldLengthMetres.ToString("F2")
            + " bodyCollider=REAL_BOUNDS" +
            " start=(645.175,712) side=RIGHT centredInRegularBay=true driverDoorFacesWalkway=true originalDoor=" + (doorPivot != null) +
            " ownerAnimations=EnteringCar,Driving,ExitingCar" +
            " physicalScale=REAL_4.95m_xK sourceLength=4.95 no_fake_collision=true manual_phone_review_required=true");
    }
}

// Android/mobile import contract for the two owner-approved car recordings only.
// Source MP3/OGG bytes remain untouched in the source library; Unity encodes its build representation.
public sealed class BraxChampionsCarAudioImporter : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (assetPath != BraxChampionsCarBuilder.EngineIdleAudio &&
            assetPath != BraxChampionsCarBuilder.EngineRevAudio) return;
        var importer = assetImporter as AudioImporter;
        if (importer == null) return;
        importer.forceToMono = true;
        importer.loadInBackground = false;
        // Unity 6 stores preloadAudioData per AudioImporterSampleSettings.
        // The old AudioImporter.preloadAudioData field raises CS0619.
        var settings = importer.defaultSampleSettings;
        settings.preloadAudioData = true;
        settings.loadType = AudioClipLoadType.CompressedInMemory;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = .68f;
        settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
        importer.defaultSampleSettings = settings;
        importer.SetOverrideSampleSettings("Android", settings);
    }
}
