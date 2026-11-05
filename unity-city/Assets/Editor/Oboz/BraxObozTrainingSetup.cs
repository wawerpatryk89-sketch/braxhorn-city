using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class BraxObozTrainingSetup
{
    const float K = BraxObozSceneInfo.WorldScale;
    const string NavPath = BraxObozCharacters.Generated + "/CampNavigation.asset";

    [Serializable] class RouteCheck
    {
        public string from, to, status;
        public Vector3 start, end;
        public bool complete;
    }
    [Serializable] class RouteReport
    {
        public string commit;
        public bool complete, phoneTested;
        public List<RouteCheck> routes = new List<RouteCheck>();
    }

    public static void Configure(BraxObozSceneInfo scene)
    {
        var routine = scene.gameObject.AddComponent<BraxObozTrainingRoutine>(); routine.player = scene.player;
        string[] order = { "kade", "zyro", "vorn", "gorr", "rakk", "mis" };
        routine.participants = order.Select(id => scene.npcs.Single(n => n.characterId == id)).ToArray();
        var bagCandidates = scene.bags.Where(b => b.transform.position.z < -30 * K && b.transform.position.z > -45 * K).ToArray();
        routine.trainingBags = new[] { -23f, 23f }.Select(x => bagCandidates.OrderBy(b =>
            (new Vector2(b.transform.position.x / K, b.transform.position.z / K) - new Vector2(x,-38)).sqrMagnitude).First()).ToArray();
        if (routine.trainingBags[0] == routine.trainingBags[1]) BraxBuildQuality.Issue(new InvalidOperationException("Dwa zajęcia wymagają różnych worków"));
        var points = new[] { new Vector3(-.7f,.38f,-5)*K, new Vector3(.7f,.38f,-5)*K,
            BagPlace(routine.trainingBags[0]), BagPlace(routine.trainingBags[1]),
            new Vector3(-7,.06f,-20)*K, new Vector3(-4,.06f,-20)*K };
        routine.places = new Transform[6];
        for (int i=0;i<6;i++)
        {
            var node = new GameObject(i<2 ? "Miejsce sparingu "+i : i<4 ? "Miejsce worka "+i : "Miejsce odpoczynku "+i).transform;
            node.SetParent(scene.transform,false); node.position=points[i]; routine.places[i]=node;
        }
        routine.sparringArenas = new[] { "OKTAGON", "Ring 1", "Ring 2" }
            .Select(label => scene.arenas.Single(a => a.label == label)).ToArray();
        routine.sparringPlaces = new Transform[6];
        for (int i=0;i<6;i++)
        {
            var arena = routine.sparringArenas[i/2];
            var node = new GameObject(arena.label + " — miejsce pary " + (i%2)).transform;
            node.SetParent(scene.transform,false);
            node.position = arena.transform.position + new Vector3(i%2==0?-.7f:.7f, i<2?.38f:.56f, 0)*K;
            routine.sparringPlaces[i] = node; arena.trainingRoutine = routine;
        }
        foreach(var bag in scene.bags)bag.pauseWith=scene.player;
        var sources=new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(scene.transform,~(1<<8),NavMeshCollectGeometry.PhysicsColliders,0,new List<NavMeshBuildMarkup>(),sources);
        var settings=NavMesh.GetSettingsByIndex(0);
        settings.agentHeight=2.45f;settings.agentRadius=.43f;settings.agentClimb=.30f;settings.agentSlope=40;
        settings.overrideVoxelSize=true;settings.voxelSize=.09f;
        var bounds=new Bounds(new Vector3(0,5,10)*K,new Vector3(130,20,160)*K);
        var problems=settings.ValidationReport(bounds);
        if(problems.Length!=0)throw new InvalidOperationException("Nieprawidłowe ustawienia przejść: "+string.Join("; ",problems));
        var data=AssetDatabase.LoadAssetAtPath<NavMeshData>(NavPath);
        if(data)
        {
            if(!NavMeshBuilder.UpdateNavMeshData(data,settings,sources,bounds))throw new InvalidOperationException("Nie odświeżono siatki przejść obozu");
            EditorUtility.SetDirty(data);
        }
        else
        {
            data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);
            if(!data)throw new InvalidOperationException("Nie zbudowano siatki przejść obozu");
            data.name="Obóz — przejścia zgodne z kolizją";AssetDatabase.CreateAsset(data,NavPath);
        }
        routine.navigationData=data;
        var instance=NavMesh.AddNavMeshData(data);
        try
        {
            foreach (var place in routine.sparringPlaces)
            {
                if(!NavMesh.SamplePosition(place.position,out var arenaHit,.8f,NavMesh.AllAreas))
                    BraxBuildQuality.Issue(new InvalidOperationException("Arena poza trasą: "+place.name));
                else place.position=arenaHit.position;
            }
            for(int i=0;i<6;i++)
            {
                bool onNav = NavMesh.SamplePosition(points[i],out var hit,.8f,NavMesh.AllAreas);
                if(!onNav)BraxBuildQuality.Issue(new InvalidOperationException("Stanowisko poza trasą: "+i));
                var placement = onNav ? hit.position : points[i];
                routine.places[i].position=placement;
                var npc=routine.participants[i];npc.transform.position=placement+Vector3.up*(.015f*K); // 1.5 cm nad NavMesh; grawitacja CharacterController sprowadza na podłogę
                npc.routine=routine;
                var agent=npc.gameObject.AddComponent<NavMeshAgent>();agent.enabled=false;
                agent.agentTypeID=settings.agentTypeID;agent.updatePosition=false;agent.updateRotation=false;
                agent.radius=npc.body.radius;agent.height=npc.body.height;agent.baseOffset=0;
                agent.speed=npc.walkSpeed;agent.acceleration=12;agent.angularSpeed=240;agent.stoppingDistance=.16f;
                agent.obstacleAvoidanceType=ObstacleAvoidanceType.HighQualityObstacleAvoidance;agent.avoidancePriority=30+i*5;
                npc.navigation=agent;
                // Preserve the old serialized route API for other consumers; all points are real destinations.
                npc.route=routine.places; npc.activities=Array.Empty<BraxObozActivity>();
            }
            var destinations=routine.places.Skip(2).Concat(routine.sparringPlaces).ToArray();
            var path=new NavMeshPath();
            var report = new RouteReport { commit = Environment.GetEnvironmentVariable("GITHUB_SHA"), complete = true };
            for(int a=0;a<destinations.Length;a++)for(int b=0;b<destinations.Length;b++)if(a!=b)
            {
                bool found=NavMesh.CalculatePath(destinations[a].position,destinations[b].position,NavMesh.AllAreas,path);
                bool complete=found && path.status==NavMeshPathStatus.PathComplete;
                report.routes.Add(new RouteCheck { from=destinations[a].name, to=destinations[b].name,
                    start=destinations[a].position, end=destinations[b].position,
                    status=found?path.status.ToString():"NoPath", complete=complete });
                report.complete &= complete;
            }
            var output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../build/CampLifeReview"));
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"navigation-review.json"),JsonUtility.ToJson(report,true));
            if(!report.complete)
            {
                var failed=report.routes.First(r=>!r.complete);
                BraxBuildQuality.Issue(new InvalidOperationException("Brak przejścia: "+failed.from+" → "+failed.to+
                    " ("+failed.start+" → "+failed.end+", "+failed.status+"); raport navigation-review.json"));
            }
        }
        finally{if(instance.valid)instance.Remove();}
        BraxObozAudioSetup.Configure(scene,routine);
        AssetDatabase.SaveAssets();
        Debug.Log("BRAXHORN_OBOZ plan=6_postaci_3_areny_2_worki_2_odpoczynek trasy=patrz_navigation-review.json runtime_niepotwierdzony");
    }
    static Vector3 BagPlace(BraxWorek bag)
    {var p=bag.transform.position;p.y=.06f*K;p.z+=K;return p;}
}
