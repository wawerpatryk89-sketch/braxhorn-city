using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// A complete exploration scene, alongside the player-free region for later additive integration.
public static class BraxChampionsExplorer
{
    public const string ScenePath = "Assets/PlayableGenerated/Mistrzowie/Mistrzowie_Explore.unity";
    public const string SourceCommit = "e93d6c01f1c64fc5527987c9a0a7a2c57daf86e8";
    static float K => BraxClaudeOsiedle.K;
    const int GroundMask = ~((1 << 8) | (1 << 2) | (1 << 29) | (1 << 31));
    static readonly string[] RequiredClips = { "Idle", "Walk", "Run", "Jump", "Punch", "Kick", "CombatIdle" };

    public static void Generate(string buildId)
    {
        if (File.Exists(ScenePath)) throw new InvalidOperationException("Keep existing exploration scene edits; reviewed regeneration required.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != BraxChampionsFoundation.ScenePath) throw new InvalidOperationException("Open the completed Champions foundation first.");
        if (All<BraxGymPlayer>(scene).Length != 0 || All<Camera>(scene).Length != 0)
            throw new InvalidOperationException("Region already contains a player/camera; do not duplicate it.");
        var rig = new GameObject("Miasto Mistrzow | Patryk exploration rig");
        var player = new GameObject("BraxhornPlayer"); player.transform.SetParent(rig.transform, false); player.layer = 8;
        var body = player.AddComponent<CharacterController>();
        body.height = 2.37f; body.radius = .35f; body.center = new Vector3(0, 1.185f, 0);
        body.stepOffset = .36f; body.slopeLimit = 48; body.skinWidth = .035f;
        var hero = BraxChampionsPatryk.PatrykBohater(player.transform, body);
        var animation = hero.GetComponent<Animation>();
        if (!animation) throw new InvalidOperationException("Patryk has no original animations.");
        var camera = new GameObject("Patryk exploration camera").AddComponent<Camera>();
        camera.transform.SetParent(rig.transform, false); camera.tag = "MainCamera";
        camera.fieldOfView = 55; camera.nearClipPlane = .15f; camera.farClipPlane = 1800;
        camera.allowHDR = false; camera.allowMSAA = true;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.64f, .73f, .79f);
        camera.gameObject.AddComponent<AudioListener>(); camera.gameObject.AddComponent<BraxClaudeKamera>();
        // Patryk receives the same world light as the surroundings. Camera-attached
        // portrait lights stacked three extra directional lights on his skin only.
        var runtime = player.AddComponent<BraxGymPlayer>();
        runtime.visual = hero.transform; runtime.clips = animation; runtime.body = body; runtime.view = camera;
        runtime.bags = Array.Empty<Transform>(); runtime.buildId = buildId;
        runtime.walkOnly = false; runtime.playFullActionClips = true; runtime.hideWhenCameraClose = false;
        runtime.initialCameraYaw = 180f;
        var world = new GameObject("Miasto Mistrzow exploration").AddComponent<BraxCityWorld>();
        world.transform.SetParent(rig.transform, false); world.area = "mistrzowie"; world.player = runtime; runtime.cityWorld = world;
        Physics.SyncTransforms();
        var start = Ground(new Vector3(600, .16f, 700) * K);
        body.enabled = false; player.transform.position = start + Vector3.up * .04f; body.enabled = true;
        player.transform.rotation = Quaternion.identity; runtime.worldSpawn = player.transform.position;
        camera.transform.position = player.transform.position + new Vector3(0, 3.5f, -5.5f);
        camera.transform.LookAt(player.transform.position + Vector3.up * 1.65f);
        var points = new List<BraxCityPoint> {
            new BraxCityPoint { id="champion_north", label="CHAMPION ARENA — PÓŁNOC", position=start },
            new BraxCityPoint { id="hotel_braxhorn", label="Hotel BRAXHORN — wejście", position=Ground(new Vector3(828,.16f,815)*K) }
        };
        foreach (var station in All<Transform>(scene).Where(t=>t.name.EndsWith("| reused city metro")))
            points.Add(new BraxCityPoint { id="metro_"+station.name.Substring(0,2), label=station.name.Split('|')[0].Trim(),
                position=Ground(station.TransformPoint(BraxClaudeMetro.ChampionsWalkingRoute()[0])) });
        world.points = points.ToArray();
        // Existing Android HUD creates one persistent instance at runtime. Editor/PC uses the same player command API.
        player.AddComponent<BraxChampionsDesktopInput>().player = runtime;
        AddDaylight(rig.transform);
        var review=rig.AddComponent<BraxChampionsVisualReview>();review.view=camera;review.player=runtime;
        var foundation=JsonUtility.FromJson<BraxChampionsFoundation.Foundation>(File.ReadAllText("Assets/Source/Mistrzowie/Foundation.json"));var plant=foundation.plants[0];
        review.labels=new[]{"01-map","02-arena","03-hotel","04-grass-near","05-grass-far"};
        review.positions=new[]{new Vector3(1100,480,1050)*K,new Vector3(600,1.8f,705)*K,
            new Vector3(817,1.8f,815)*K,new Vector3(plant.x-2,1.5f,plant.z-2)*K,
            new Vector3(plant.x-16,2.5f,plant.z-16)*K};
        review.targets=new[]{new Vector3(600,0,450)*K,new Vector3(600,25,800)*K,
            new Vector3(885,22,815)*K,new Vector3(plant.x,.16f,plant.z)*K,
            new Vector3(plant.x,.16f,plant.z)*K};
        Physics.SyncTransforms(); Validate(scene);
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Exploration scene save failed.");
    }

    static void AddDaylight(Transform rig)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.55f,.63f,.71f);
        RenderSettings.ambientEquatorColor = new Color(.38f,.44f,.43f);
        RenderSettings.ambientGroundColor = new Color(.23f,.27f,.22f); RenderSettings.fog = false;
        var sun = new GameObject("Champions exploration daylight").AddComponent<Light>(); sun.transform.SetParent(rig, false);
        sun.type = LightType.Directional; sun.intensity = 1.08f; sun.color = new Color(1,.97f,.92f);
        sun.transform.rotation = Quaternion.Euler(48,-35,0); sun.shadows = LightShadows.Soft;
        sun.shadowStrength = .67f; sun.shadowBias = .04f; sun.shadowNormalBias = .25f; RenderSettings.sun = sun;
        QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowDistance = 60;
        QualitySettings.shadowResolution = ShadowResolution.Medium; QualitySettings.shadowCascades = 2;
    }

    static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<T>(true)).ToArray();

    static Vector3 Ground(Vector3 approximate)
    {
        if (!Physics.Raycast(approximate+Vector3.up,Vector3.down,out var hit,2f,GroundMask,QueryTriggerInteraction.Ignore))
            throw new InvalidOperationException("Missing exploration floor: "+approximate);
        if (Mathf.Abs(hit.point.y-approximate.y)>.45f) throw new InvalidOperationException("Unexpected exploration floor height: "+hit.collider.name);
        return hit.point;
    }

    static void ClearCapsule(Vector3 feet, CharacterController body)
    {
        var a=feet+Vector3.up*(body.radius+body.skinWidth+.02f);
        var b=feet+Vector3.up*(body.height-body.radius);
        if (Physics.CheckCapsule(a,b,body.radius,GroundMask,QueryTriggerInteraction.Ignore))
            throw new InvalidOperationException("Patryk spawn/approach is obstructed: "+feet);
    }

    public static int Validate(Scene scene)
    {
        var players=All<BraxGymPlayer>(scene); if(players.Length!=1) throw new InvalidOperationException("Exactly one Patryk required.");
        var p=players[0];
        if(!p.visual||p.visual.name!="Patryk"||!p.body||!p.clips||!p.view||!p.cityWorld||p.cityWorld.player!=p||p.cityWorld.area!="mistrzowie")
            throw new InvalidOperationException("Incomplete Patryk/world/camera references.");
        if((p.transform.lossyScale-Vector3.one).sqrMagnitude>.000001f||Mathf.Abs(p.body.height-2.37f)>.001f)
            throw new InvalidOperationException("Patryk has been scaled twice or capsule differs from estate.");
        if(p.walkOnly||!p.playFullActionClips||p.initialCameraYaw!=180f||p.bags==null)
            throw new InvalidOperationException("Missing estate locomotion configuration.");
        if(All<Camera>(scene).Length!=1||All<AudioListener>(scene).Length!=1||!p.view.CompareTag("MainCamera")||!p.view.GetComponent<BraxClaudeKamera>())
            throw new InvalidOperationException("Single player camera/listener required.");
        if(All<BraxTouchControls>(scene).Length!=0) throw new InvalidOperationException("Android HUD is bootstrapped once; no serialized duplicate.");
        var desktop=p.GetComponent<BraxChampionsDesktopInput>(); if(!desktop||desktop.player!=p) throw new InvalidOperationException("Desktop exploration input missing.");
        var skin=p.visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if(skin.Length==0||skin.Any(r=>!r.sharedMesh||r.bones==null||r.bones.Length==0||r.bones.Any(b=>!b)))
            throw new InvalidOperationException("Original Patryk skin/bones missing.");
        foreach(var r in skin)
        {
            if(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(r.sharedMesh)))throw new InvalidOperationException("Unsaved Patryk mesh.");
            foreach(var m in r.sharedMaterials)
            {
                if(!m||!m.shader||m.shader.name!="Braxhorn/ChampionsPatryk"||ShaderUtil.ShaderHasError(m.shader))throw new InvalidOperationException("Missing Patryk shader.");
                foreach(var property in new[]{"_MainTex","_BumpMap","_Orm"})
                {
                    var texture=m.GetTexture(property);int size=4096;
                    if(!texture||texture.width!=size||texture.height!=size)throw new InvalidOperationException("Missing source texture: "+property);
                }
            }
        }
        foreach(var name in RequiredClips)
        {
            var clip=p.clips.GetClip(name);
            if(!clip||!clip.legacy||clip.length<=0||string.IsNullOrEmpty(AssetDatabase.GetAssetPath(clip)))
                throw new InvalidOperationException("Missing saved Patryk animation: "+name);
        }
        if(p.clips.GetClip("Walk")==p.clips.GetClip("Run")) throw new InvalidOperationException("Run must use the real estate run clip.");
        var points=p.cityWorld.points;
        if(points==null||points.Length!=5||points.Select(x=>x.id).Distinct().Count()!=5)throw new InvalidOperationException("Five unique exploration points required.");
        foreach(var point in points)ClearCapsule(Ground(point.position),p.body);
        if((p.transform.position-p.worldSpawn).sqrMagnitude>.0001f)throw new InvalidOperationException("Reset spawn differs from initial spawn.");
        for(int i=0;i<=20;i++)ClearCapsule(Ground(new Vector3(600,.16f,700+i*.25f)*K),p.body);
        return RequiredClips.Length;
    }
}

