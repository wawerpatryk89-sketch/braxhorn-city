using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Scene assembly for review, independent of APK/CI. No donor model is spawned.
public static class BraxObozActors
{
    public const string ScenePath = "Assets/PlayableGenerated/Oboz/BraxhornObozReview.unity";
    const float K = 1.294f;
    [MenuItem("BRAXHORN/Obóz/Połącz zabudowę z siedmioma postaciami (podgląd, bez APK)")]
    public static void Assemble()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Zakończ Play Mode");
        if (!File.Exists(BraxObozEnvironment.ScenePath)) throw new InvalidOperationException("Najpierw przygotuj zabudowę obozu");
        var manifest = BraxObozCharacters.ReadManifest();
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.OpenScene(BraxObozEnvironment.ScenePath, OpenSceneMode.Additive); SceneManager.SetActiveScene(scene);
        try
        {
            var info = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BraxObozSceneInfo>(true)).Single();
            if (info.player || info.npcs.Length != 0) throw new InvalidOperationException("Scena zawiera już obsadę; odtwórz bazową zabudowę, zamiast dublować postacie");
            var world = info.gameObject.AddComponent<BraxCityWorld>(); world.area = "oboz"; world.doors = info.cabinDoors;
            var progress = info.gameObject.AddComponent<BraxObozProgress>();
            var monika = manifest.characters.Single(c => c.id == "monika");
            var hero = Actor(monika, new Vector3(0,.06f,-18), info.transform, out var monikaBody);
            var player = hero.AddComponent<BraxGymPlayer>(); player.visual = hero.transform.GetChild(0);
            player.clips = player.visual.GetComponent<Animation>(); player.body = monikaBody;
            player.bags = Array.Empty<Transform>(); player.cityWorld = world; player.worldSpawn = hero.transform.position;
            player.buildId = "OBOZ_RECOVERY_REVIEW_NOT_RELEASE"; world.player = player; info.player = player;
            BraxObozServices.BindPlayer(info);
            foreach(var gate in info.GetComponentsInChildren<BraxObozSlidingGate>(true)) gate.player=player;
            player.ConfigureCamera(3.5f, monikaBody.height*.62f);
            // Owner Jump.fbx: take-off around 0.75 s, landing around 1.42 s,
            // full recovery through 2.583 s. Keep its natural playback rate.
            player.ConfigureAuthoredJump(1f,.75f);
            var camera = new GameObject("Main Camera"); camera.tag = "MainCamera";
            player.view = camera.AddComponent<Camera>(); player.view.nearClipPlane = .1f; player.view.farClipPlane = 600; player.view.fieldOfView = 58;
            camera.AddComponent<AudioListener>();
            var ids = new[] { "vorn", "zyro", "rakk", "kade", "gorr", "mis" };
            var places = new[] { new Vector3(27,.18f,-127), new Vector3(-12,.56f,51), new Vector3(20,.06f,-22), new Vector3(-24,.56f,15), new Vector3(-20,.06f,-22), new Vector3(0,.40f,-4) };
            var npcs = new BraxObozNpc[ids.Length];
            for (int i=0;i<ids.Length;i++)
            {
                var spec = manifest.characters.Single(c => c.id == ids[i]);
                var actor = Actor(spec, places[i], info.transform, out var body);
                var npc = actor.AddComponent<BraxObozNpc>(); npc.characterId = spec.id; npc.player = player; npc.body = body;
                npc.clips = actor.GetComponentInChildren<Animation>(); npc.glovesVerified = spec.glovesVerified; npc.hasGloves = spec.hasGloves;
                var routeRoot = new GameObject(spec.name + " — trasa do kontroli").transform; routeRoot.SetParent(info.transform,false);
                npc.route = new Transform[2];
                for(int j=0;j<2;j++)
                {
                    var point = new GameObject("Punkt " + j).transform; point.SetParent(routeRoot,false);
                    point.localPosition = (places[i]+new Vector3(j*.8f,0,0))*K; npc.route[j]=point;
                }
                npc.activities = new[] { BraxObozActivity.Rest, ids[i] == "vorn" ? BraxObozActivity.Rest : BraxObozActivity.Training };
                npcs[i]=npc;
            }
            info.npcs=npcs;
            BraxObozTrainingSetup.Configure(info);
            foreach (var station in info.GetComponentsInChildren<BraxObozTrainingStation>(true))
                station.player=player; // szesc plenerowych + trzy pod dachem
            foreach(var arena in info.arenas)
            {
                arena.player=player; arena.progress=progress;
                string id=arena.label=="Ring 1"?"kade":arena.label=="Ring 2"?"zyro":"mis";
                arena.opponent=npcs.Single(n=>n.characterId==id);
            }
            world.points = new[] {
                Point("octagon","OKTAGON",0,-5), Point("rings","RINGI",0,15),
                Point("cabins","DOMKI",-81,-30), Point("bags","WORKI",0,-40),
                Point("gym","BRAXHORN GYM",-42,76), Point("wc","ŁAŹNIA I WC",-92,-83),
                Point("oval","BIEŻNIA OWALNA",65,72), Point("sprint","BIEŻNIA 100 M",113,-35)
            };
            info.ownerAppearanceApproved=false; info.mobileRuntimeVerified=false;
            BraxObozMaterials.WriteTextureAudit(info.transform);
            if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Nie zapisano podglądu obozu");
            var report=BraxObozValidation.Check(info);
            File.WriteAllText(BraxObozCharacters.Generated+"/review-assembly.json",JsonUtility.ToJson(report,true));
            Debug.Log("BRAXHORN_OBOZ zapisano podgląd 1 gracz + 6 NPC, bez dawców animacji. Odchylenia źródeł i brakujące elementy pozostają niezatwierdzone.");
        }
        finally { EditorSceneManager.CloseScene(scene,true); if(previous.IsValid())SceneManager.SetActiveScene(previous); }
        AssetDatabase.SaveAssets();
    }
    static BraxCityPoint Point(string id,string label,float x,float z)=>new BraxCityPoint{id=id,label=label,position=new Vector3(x,.06f,z)*K};
    static GameObject Actor(BraxObozCharacters.CharacterSpec spec,Vector3 position,Transform parent,out CharacterController body)
    {
        var root=new GameObject(spec.name); root.transform.SetParent(parent,false);root.layer=8;
        var visual=BraxObozCharacters.CreateCached(spec,K,sourceReviewOnly:true);visual.transform.SetParent(root.transform,false);
        var renderers=visual.GetComponentsInChildren<SkinnedMeshRenderer>();
        Bounds bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
        float height=spec.realHeight>0?spec.realHeight*K:bounds.size.y;
        if(height<.8f*K||height>3*K)BraxBuildQuality.Issue(new InvalidOperationException(spec.name+": nieprawidłowa kapsuła"));
        body=root.AddComponent<CharacterController>();body.height=height;body.radius=.27f*K;
        body.center=Vector3.up*height*.5f;body.stepOffset=.25f*K;body.skinWidth=.035f;body.slopeLimit=48f;
        root.transform.position=position*K;
        // Ustaw stopy na rzeczywistej nawierzchni: podlodze hali,
        // ringu, macie lub gruncie. Nie przesuwaj calego skinned mesh
        // w kazdym Update; CharacterController ma pojedyncza wladze fizyki.
        Physics.SyncTransforms();
        var origin = root.transform.position + Vector3.up * (3.2f*K);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit ground, 7f*K, ~(1<<8), QueryTriggerInteraction.Ignore))
        {
            var at = root.transform.position;
            root.transform.position = new Vector3(at.x, ground.point.y + .025f*K, at.z);
        }
        else Debug.LogWarning("BRAXHORN_OBOZ_SPAWN bez podloza: " + spec.name);
        return root;
    }
}
