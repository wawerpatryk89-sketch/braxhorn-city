using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Diagnostyka kazdego widocznego renderera, NIE blokada kompilacji.
// Wywolac po BraxObozEnvironment.Prepare i BraxObozActors.Assemble.
// Bez BuildPipeline.BuildPlayer, bez APK, bez zdjec w CI.
public static class BraxObozSceneAudit
{
    const float K=1.294f;
    const float Tolerance=.03f*K;

    [Serializable] public sealed class Item
    {
        public string name,path,classification,status,source;
        public float x,y,z,bottomY,gapMetres,widthMetres,heightMetres,depthMetres;
    }
    [Serializable] public sealed class SizeEntry { public string path; public long sourceBytes; }
    [Serializable] public sealed class Report
    {
        public string scene,revision;
        public bool unitySceneInspected,apkBuilt,phoneVerified;
        public int renderers,unclassified,levitating,embedded,missingGroundRay,expectedMounted;
        public long dependentSourceBytes;
        public Item[] items;
        public SizeEntry[] largestSources;
    }

    static string PathOf(Transform transform)
    {
        var names=new List<string>();
        for(var t=transform;t;t=t.parent) names.Add(t.name);
        names.Reverse();
        return string.Join("/",names);
    }
    static bool IsMounted(Renderer r)
    {
        // Ściany, dachy, łancuchy i podwieszone worki NIE dotykają gruntu
        // celowo; nadal są w JSON, ale nie liczymy ich jako lewitującą podłogę.
        string name=PathOf(r.transform).ToLowerInvariant();
        return new[]{"ściana","szyld","mural","dach","połać","rynna",
            "ściana","okno","szyba","futryna","wspornik","belka","poręcz",
            "lampa","kabel","żarów","łańcuch","worek","flaga","maszt",
            "oświetl","latarnia","drzwi","wiata","kalenic","szn","górn",
            "ścien","słup","siatka","lina","krokiew","fartuch","lustro",
            "parapet","półka","bateria","wieszak","animac","lud","ręk",
            "głowa","charact","monika","npcs","gracz","liście","korona",
            "gałąź","billboard","tree_","fir_tree","pine_tree"}.Any(name.Contains);
    }
    static bool IsNaturalTerrain(Renderer r)
    {
        string n=r.name.ToLowerInvariant();
        return n.Contains("podłoże")||n.Contains("terrain")||n.Contains("grunt")||
            n.Contains("strumień");
    }
    static Item Inspect(Renderer r)
    {
        Bounds b=r.bounds;
        var item=new Item {
            name=r.name,path=PathOf(r.transform),
            classification=IsMounted(r)?"montowany":"grunt",
            status="brak testu",
            x=b.center.x,y=b.center.y,z=b.center.z,
            bottomY=b.min.y,
            widthMetres=b.size.x/K,heightMetres=b.size.y/K,depthMetres=b.size.z/K,
            source=AssetDatabase.GetAssetPath(r.sharedMaterial)
        };
        if(IsMounted(r)) {item.status="konstrukcja/montaż — nie podlega testowi gruntu";return item;}
        if(IsNaturalTerrain(r)) {item.status="grunt";return item;}
        Physics.SyncTransforms();
        // Raycast w pionie spod bryły; kolider przypisany do tego samego
        // renderera nie może sztucznie potwierdzić podparcia przez samego siebie.
        var origin=new Vector3(b.center.x,b.min.y+.50f*K,b.center.z);
        var hits=Physics.RaycastAll(origin,Vector3.down,10f*K,
            ~0,QueryTriggerInteraction.Ignore)
            .Where(h=>h.collider && !h.collider.transform.IsChildOf(r.transform) &&
                      h.normal.y>.65f && h.point.y<=b.min.y+.35f*K)
            .OrderByDescending(h=>h.point.y).ToArray();
        if(hits.Length==0){item.status="brak powierzchni w raycast";return item;}
        item.gapMetres=(b.min.y-hits[0].point.y)/K;
        if(item.gapMetres>.03f)item.status="LEWITUJE > 3 cm";
        else if(item.gapMetres<-.03f)item.status="ZAPADNIĘTE > 3 cm";
        else item.status="kontakt w tolerancji 3 cm";
        return item;
    }
    static SizeEntry[] LargestSources()
    {
        if(!File.Exists(BraxObozActors.ScenePath))return Array.Empty<SizeEntry>();
        string[] deps=AssetDatabase.GetDependencies(BraxObozActors.ScenePath,true);
        return deps.Where(p=>!string.IsNullOrWhiteSpace(p) && File.Exists(p))
            .Select(p=>new SizeEntry{path=p,sourceBytes=new FileInfo(p).Length})
            .OrderByDescending(x=>x.sourceBytes).Take(20).ToArray();
    }
    [MenuItem("BRAXHORN/Obóz/Pełny audyt sceny — bez APK")]
    public static void Run()
    {
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.OpenScene(BraxObozActors.ScenePath,OpenSceneMode.Additive);
        try
        {
            var renderers=scene.GetRootGameObjects()
                .SelectMany(x=>x.GetComponentsInChildren<Renderer>(true))
                .Where(r=>r && r.enabled && r.gameObject.activeInHierarchy)
                .ToArray();
            var all=renderers.Select(Inspect).ToArray();
            var biggest=LargestSources();
            var report=new Report {
                scene=BraxObozActors.ScenePath,
                revision=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local",
                unitySceneInspected=true,apkBuilt=false,phoneVerified=false,
                renderers=all.Length,items=all
            };
            report.levitating=all.Count(x=>x.status=="LEWITUJE > 3 cm");
            report.embedded=all.Count(x=>x.status=="ZAPADNIĘTE > 3 cm");
            report.missingGroundRay=all.Count(x=>x.status=="brak powierzchni w raycast");
            report.expectedMounted=all.Count(x=>x.classification=="montowany");
            report.unclassified=all.Count(x=>x.status=="brak testu");
            report.largestSources=biggest;
            report.dependentSourceBytes=biggest.Sum(x=>x.sourceBytes);
            string folder=System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath,"../../audit"));
            Directory.CreateDirectory(folder);
            File.WriteAllText(System.IO.Path.Combine(folder,"oboz-scena.json"),
                JsonUtility.ToJson(report,true));
            var md=new StringBuilder();
            md.AppendLine("# OBÓZ TRENINGOWY — audyt sceny, bez APK");
            md.AppendLine("Scena: "+report.scene+"; SHA: "+report.revision);
            md.AppendLine("Przebadane renderery: "+report.renderers);
            md.AppendLine("Lewitujące >3 cm: "+report.levitating);
            md.AppendLine("Zapadnięte >3 cm: "+report.embedded);
            md.AppendLine("Bez podłoża do raycast: "+report.missingGroundRay);
            md.AppendLine("Montażowe/podwieszone (sprawdzić wizualnie): "+report.expectedMounted);
            md.AppendLine("Weryfikacja wizualna/telefon: NIE");
            md.AppendLine("20 największych zasobów zależnych — BAJTY ŹRÓDŁOWE, NIE ROZMIAR APK:");
            foreach(var x in biggest)md.AppendLine("- "+x.sourceBytes+" B — "+x.path);
            md.AppendLine("Przykładowe niezgodności (pełny wykaz w JSON):");
            foreach(var x in all.Where(x=>x.status.Contains("3 cm")||
                x.status=="brak powierzchni w raycast").Take(90))
                md.AppendLine("- "+x.status+" "+x.path+" gap="+x.gapMetres.ToString("F3"));
            File.WriteAllText(System.IO.Path.Combine(folder,"oboz-scena.md"),md.ToString());
            Debug.LogWarning("BRAXHORN_OBOZ_SCENE_AUDIT renderers="+report.renderers+
                " float="+report.levitating+" embedded="+report.embedded+
                " withoutGround="+report.missingGroundRay+
                " reports=audit/oboz-scena.json,audit/oboz-scena.md");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene,true);
            if(previous.IsValid())SceneManager.SetActiveScene(previous);
        }
    }
    static void SaveUntitledSceneInBatch()
    {
        // Unity uruchomione w CI ma zwykle niezapisaną scenę wstępną.
        // Additive NewScene/OpenScene nie może jej po cichu odrzucić.
        if(!Application.isBatchMode)return;
        Directory.CreateDirectory("Assets/PlayableGenerated/Oboz");
        AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(!scene.isLoaded || !string.IsNullOrEmpty(scene.path))continue;
            string path=AssetDatabase.GenerateUniqueAssetPath(
                "Assets/PlayableGenerated/Oboz/SceneOnlySession.unity");
            if(!EditorSceneManager.SaveScene(scene,path))
                throw new IOException("Nie można zapisać sceny początkowej CI");
        }
    }
    [MenuItem("BRAXHORN/Obóz/Generuj i audytuj scenę — bez APK")]
    public static void GenerateWithoutApk()
    {
        SaveUntitledSceneInBatch();
        BraxObozEnvironment.Prepare();
        BraxObozActors.Assemble();
        Run();
    }
}
