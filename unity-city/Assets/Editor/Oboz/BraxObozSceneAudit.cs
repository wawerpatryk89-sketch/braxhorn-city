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
    [Serializable] sealed class LogEntry { public string level,message,stackTrace; }
    [Serializable] sealed class Diagnostics
    {
        public string revision,firstFatalException;
        public bool generationCompleted;
        public int warningCount,errorCount;
        public List<LogEntry> entries=new List<LogEntry>();
    }

    [Serializable] public sealed class Item
    {
        public string name,path,classification,status,source;
        public string[] materialSources;
        public float x,y,z,bottomY,gapMetres,widthMetres,heightMetres,depthMetres;
    }
    [Serializable] public sealed class SizeEntry { public string path; public long sourceBytes; }
    [Serializable] public sealed class PlacementItem
    {
        public string path,label,kind,status,supportPath;
        public bool measured;
        public float gapMetres;
        public bool heightMeasured;
        public float expectedHeightMetres=-1f,actualHeightMetres,heightErrorMetres;
    }
    [Serializable] public sealed class CharacterItem
    {
        public string path,status;
        public bool feetMeasured,groundMeasured;
        public float soleY,groundY,footGapMetres,capsuleGapMetres;
    }
    [Serializable] public sealed class BullItem
    {
        public bool present,scalePreserved;
        public string source;
        public Vector3 measuredWorldSize;
        // Actual original owner's 4X mesh bounds * K, measured independently
        // from the two delivered GLBs. Never used to rescale the statue.
        public Vector3 expectedWorldSize=new Vector3(2.4342448f,3.9047648f,6.2011680f);
    }
    [Serializable] public sealed class Report
    {
        public string scene,revision;
        public bool unitySceneInspected,apkBuilt,phoneVerified;
        public int renderers,unclassified,levitating,embedded,missingGroundRay,expectedMounted;
        public int expectedMurals=4,presentMurals,muralWarnings,scaleWarnings,overlapCandidates;
        public int checkedBags,bagWarnings;
        public int checkedGroundObjects,checkedWallObjects,unmeasuredObjects,wallWarnings;
        public int checkedCharacters,characterWarnings;
        public bool allPlacementsVerified;
        public long dependentSourceBytes;
        public Item[] items;
        public SizeEntry[] largestSources;
        public PlacementItem[] placements;
        public CharacterItem[] characters;
        public BullItem bull;
        public BraxObozGroundAudit.Result ground;
        public string[] muralFindings,scaleFindings,overlapFindings,bagFindings;
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
        // Only the actual part name may classify construction. A furniture
        // child of a roof/cabin, Monika or NPC is never exempt by ancestry.
        string name=r.name.ToLowerInvariant();
        return new[]{"ściana","szyld","mural","dach","połać","rynna",
            "ściana","okno","szyba","futryna","wspornik","belka","poręcz",
            "lampa","kabel","żarów","łańcuch","worek","flaga","maszt",
            "oświetl","latarnia","drzwi","wiata","kalenic","szn","górn",
            "ścien","słup","siatka","lina","krokiew","fartuch","lustro",
            "parapet","bateria","wieszak","liście","korona",
            "gałąź","billboard","tree_","fir_tree","pine_tree"}.Any(name.Contains);
    }
    static bool IsNaturalTerrain(Renderer r)
    {
        string n=r.name.ToLowerInvariant();
        return n.Contains("podłoże")||n.Contains("terrain")||n.Contains("grunt")||
            n.Contains("strumień");
    }
    static string DescribeMaterial(Material material)
    {
        if(!material)return "MATERIAL_MISSING";
        string shader=material.shader?material.shader.name:"SHADER_MISSING";
        var maps=material.GetTexturePropertyNames().Where(key=>material.GetTexture(key))
            .Select(key=>key+"="+AssetDatabase.GetAssetPath(material.GetTexture(key)));
        return material.name+"; shader="+shader+"; imported="+
            AssetDatabase.GetAssetPath(material)+"; textures="+string.Join(",",maps);
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
            source=AssetDatabase.GetAssetPath(r.sharedMaterial),
            materialSources=r.sharedMaterials.Select(DescribeMaterial).ToArray()
        };
        if(r.GetComponentInParent<BraxObozPlacementRecord>())
        {
            item.classification="część zarejestrowanego modelu";
            item.status="pomiar całego modelu w placements";
            return item;
        }
        if(r.GetComponentInParent<CharacterController>())
        {
            item.classification="postać";
            item.status="pomiar podeszew i kapsuły w characters";
            return item;
        }
        if(IsMounted(r)) {item.status="konstrukcja/montaż — nie podlega testowi gruntu";return item;}
        if(IsNaturalTerrain(r)) {item.status="grunt";return item;}
        Physics.SyncTransforms();
        // Raycast w pionie spod bryły; kolider przypisany do tego samego
        // renderera nie może sztucznie potwierdzić podparcia przez samego siebie.
        var origin=new Vector3(b.center.x,b.min.y+.50f*K,b.center.z);
        var hits=Physics.RaycastAll(origin,Vector3.down,10f*K,
            ~0,QueryTriggerInteraction.Ignore)
            .Where(h=>h.collider && h.collider.gameObject.scene==r.gameObject.scene &&
                      !h.collider.transform.IsChildOf(r.transform) &&
                      !r.transform.IsChildOf(h.collider.transform) &&
                      h.normal.y>.65f && h.point.y<=b.min.y+.35f*K)
            .OrderByDescending(h=>h.point.y).ToArray();
        if(hits.Length==0){item.status="brak powierzchni w raycast";return item;}
        item.gapMetres=(b.min.y-hits[0].point.y)/K;
        if(item.gapMetres>.03f)item.status="LEWITUJE > 3 cm";
        else if(item.gapMetres<-.03f)item.status="ZAPADNIĘTE > 3 cm";
        else item.status="kontakt w tolerancji 3 cm";
        return item;
    }

    static PlacementItem InspectPlacement(BraxObozPlacementRecord record)
    {
        var item=new PlacementItem {path=PathOf(record.transform),label=record.label,
            kind=record.support.ToString(),status="niezmierzony"};
        var renderers=record.GetComponentsInChildren<Renderer>(true)
            .Where(r=>r&&r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(renderers.Length==0) {item.status="brak aktywnej geometrii";return item;}
        var bounds=RenderedBounds(record.transform);
        item.expectedHeightMetres=record.heightAboveFloorMetres;
        if(record.heightAboveFloorMetres>=0)
        {
            var origin=new Vector3(bounds.center.x,bounds.min.y+.15f*K,bounds.center.z);
            var floor=Physics.RaycastAll(origin,Vector3.down,5f*K,~0,
                QueryTriggerInteraction.Ignore)
                .Where(h=>h.collider && h.collider.gameObject.scene==record.gameObject.scene &&
                    !h.collider.transform.IsChildOf(record.transform) && h.normal.y>.65f &&
                    (!record.verticalSupport||h.collider==record.verticalSupport))
                .OrderByDescending(h=>h.point.y).FirstOrDefault();
            item.heightMeasured=floor.collider;
            if(floor.collider)
            {
                var measuredBounds=record.heightRenderers!=null&&record.heightRenderers.Length>0
                    ? BraxObozPlacement.Bounds(record.heightRenderers):bounds;
                float reference=record.heightReference==0?measuredBounds.min.y:
                    record.heightReference==1?measuredBounds.center.y:measuredBounds.max.y;
                item.actualHeightMetres=(reference-floor.point.y)/K;
                item.heightErrorMetres=item.actualHeightMetres-record.heightAboveFloorMetres;
            }
        }
        if(record.support==BraxObozPlacementRecord.SupportKind.Ground)
        {
            // Cast from above the complete object and reject its entire
            // hierarchy, including a parent collider of any mesh child.
            var origin=new Vector3(bounds.center.x,bounds.max.y+.5f*K,bounds.center.z);
            var hit=Physics.RaycastAll(origin,Vector3.down,bounds.size.y+10f*K,~0,
                QueryTriggerInteraction.Ignore)
                .Where(h=>h.collider && h.collider.gameObject.scene==record.gameObject.scene &&
                    !h.collider.transform.IsChildOf(record.transform) && h.normal.y>.65f &&
                    (!record.verticalSupport||h.collider==record.verticalSupport) &&
                    h.point.y<=bounds.min.y+.35f*K)
                .OrderByDescending(h=>h.point.y).FirstOrDefault();
            if(!hit.collider){item.status="brak podłoża";return item;}
            item.measured=true;item.supportPath=PathOf(hit.collider.transform);
            item.gapMetres=(bounds.min.y-hit.point.y)/K;
        }
        else if(record.support==BraxObozPlacementRecord.SupportKind.Wall)
        {
            var d=record.wallDirection.normalized;
            if(d.sqrMagnitude<.9f){item.status="brak kierunku montażu";return item;}
            float extent=Vector3.Dot(bounds.extents,
                new Vector3(Mathf.Abs(d.x),Mathf.Abs(d.y),Mathf.Abs(d.z)));
            var rear=bounds.center+d*extent;
            var origin=bounds.center-d*(extent+.25f*K);
            var hit=Physics.RaycastAll(origin,d,extent*2f+2.5f*K,~0,
                QueryTriggerInteraction.Ignore)
                .Where(h=>h.collider && h.collider.gameObject.scene==record.gameObject.scene &&
                    !h.collider.transform.IsChildOf(record.transform) &&
                    Vector3.Dot(h.normal,-d)>.65f)
                .OrderBy(h=>h.distance).FirstOrDefault();
            if(!hit.collider){item.status="brak ściany";return item;}
            item.measured=true;item.supportPath=PathOf(hit.collider.transform);
            item.gapMetres=Vector3.Dot(hit.point-rear,d)/K;
        }
        else {item.status="brak zadeklarowanego podparcia — wymaga pomiaru";return item;}
        item.status=item.gapMetres>.03f?"LEWITUJE > 3 cm":
            item.gapMetres<-.03f?"ZAPADNIĘTE > 3 cm":"kontakt w tolerancji 3 cm";
        return item;
    }

    static CharacterItem InspectCharacter(CharacterController body)
    {
        var item=new CharacterItem{path=PathOf(body.transform),status="brak pomiaru stóp"};
        var feet=body.GetComponent<BraxObozIdleFootContact>();
        item.feetMeasured=feet && feet.TryGetSoleHeight(out item.soleY);
        var origin=body.transform.position+Vector3.up*(.6f*K);
        var hit=Physics.RaycastAll(origin,Vector3.down,3f*K,~(1<<8),
            QueryTriggerInteraction.Ignore)
            .Where(h=>h.collider && h.collider.gameObject.scene==body.gameObject.scene &&
                !h.collider.transform.IsChildOf(body.transform) && h.normal.y>.65f)
            .OrderByDescending(h=>h.point.y).FirstOrDefault();
        item.groundMeasured=hit.collider;
        if(!item.groundMeasured){item.status="brak podłoża postaci";return item;}
        item.groundY=hit.point.y;
        item.capsuleGapMetres=(body.bounds.min.y-hit.point.y)/K;
        if(!item.feetMeasured)return item;
        item.footGapMetres=(item.soleY-hit.point.y)/K;
        item.status=Mathf.Abs(item.footGapMetres)>.03f?"stopy poza tolerancją 3 cm":
            "kontakt w tolerancji 3 cm; poślizg w ruchu wymaga Play Mode";
        return item;
    }

    static BullItem InspectBull(Scene scene)
    {
        var result=new BullItem();
        var bull=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t=>t.name=="Oryginalny byk GLB — 100% tekstur i geometrii");
        if(!bull)return result;
        result.present=true;
        result.source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(bull.gameObject);
        result.measuredWorldSize=RenderedBounds(bull).size;
        var delta=result.measuredWorldSize-result.expectedWorldSize;
        result.scalePreserved=Mathf.Abs(delta.x)<.003f&&Mathf.Abs(delta.y)<.003f&&Mathf.Abs(delta.z)<.003f;
        return result;
    }
    static long AllDependentSourceBytes()
    {
        if(!File.Exists(BraxObozActors.ScenePath))return 0;
        return AssetDatabase.GetDependencies(BraxObozActors.ScenePath,true)
            .Where(p=>!string.IsNullOrWhiteSpace(p) && File.Exists(p))
            .Sum(p=>new FileInfo(p).Length);
    }

    static SizeEntry[] LargestSources()
    {
        if(!File.Exists(BraxObozActors.ScenePath))return Array.Empty<SizeEntry>();
        string[] deps=AssetDatabase.GetDependencies(BraxObozActors.ScenePath,true);
        return deps.Where(p=>!string.IsNullOrWhiteSpace(p) && File.Exists(p))
            .Select(p=>new SizeEntry{path=p,sourceBytes=new FileInfo(p).Length})
            .OrderByDescending(x=>x.sourceBytes).Take(20).ToArray();
    }

    static Bounds RenderedBounds(Transform t)
    {
        var renderers=t.GetComponentsInChildren<Renderer>(true)
            .Where(r=>r && r.enabled && r.gameObject.activeInHierarchy).ToArray();
        if(renderers.Length==0)return new Bounds(t.position,Vector3.zero);
        var b=renderers[0].bounds;
        foreach(var renderer in renderers.Skip(1))b.Encapsulate(renderer.bounds);
        return b;
    }

    static string[] ReviewDimensions(Scene scene)
    {
        var results=new List<string>();
        var transforms=scene.GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        void Check(Transform t,float measured,float metres,string which)
        {
            if(measured<=.0001f || Mathf.Abs(measured-metres*K)>.15f*metres*K)
                results.Add("Skala: "+PathOf(t)+" "+which+
                    " zmierzono="+(measured/K).ToString("F3")+
                    " m; oczekiwano="+metres.ToString("F2")+"m");
        }
        foreach(var t in transforms)
        {
            var n=t.name;
            if(n.StartsWith("ORYGINAŁ IKEA IDANAS — łóżko",StringComparison.Ordinal))
            {
                var bounds=RenderedBounds(t);
                Check(t,Mathf.Max(bounds.size.x,bounds.size.z),2f,"długość łóżka");
                Check(t,Mathf.Min(bounds.size.x,bounds.size.z),.9f,"szerokość łóżka");
            }
            else if(n=="Właściciel / oryginalny metal_cabinet_low_poly.glb")
                Check(t,RenderedBounds(t).size.y,2f,"wysokość szafy");
            else if(n.StartsWith("Oryginalny stół polowy 3D",StringComparison.Ordinal))
                Check(t,RenderedBounds(t).size.y,.75f,"wysokość stołu");
        }
        foreach(var record in transforms.Select(t=>t.GetComponent<BraxObozPlacementRecord>())
            .Where(r=>r&&r.expectedDimensionMetres>0))
        {
            var geometry=record.GetComponentsInChildren<Renderer>(true);
            if(geometry.Length==0) {results.Add("Brak geometrii: "+PathOf(record.transform));continue;}
            float measured=BraxObozPlacement.Dimension(geometry,
                (BraxObozPlacement.Axis)record.dimensionAxis,record.dimensionFrame);
            Check(record.transform,measured,record.expectedDimensionMetres,record.label);
        }
        foreach(var door in transforms.Select(t=>t.GetComponent<BraxCityDoor>())
            .Where(d=>d&&d.name.StartsWith("Domek ",StringComparison.Ordinal)))
        {
            var leaf=door.GetComponentsInChildren<Renderer>(true)
                .Where(r=>r.name=="Skrzydło drewniane").ToArray();
            if(leaf.Length==0){results.Add("Brak skrzydła drzwi: "+PathOf(door.transform));continue;}
            Check(door.transform,BraxObozPlacement.Dimension(leaf,BraxObozPlacement.Axis.X,door.transform),.9f,"szerokość drzwi");
            Check(door.transform,BraxObozPlacement.Dimension(leaf,BraxObozPlacement.Axis.Y,door.transform),2.1f,"wysokość drzwi");
        }
        foreach(var arena in scene.GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<BraxObozArena>(true))
            .Where(x=>x&&x.label.StartsWith("Ring",StringComparison.Ordinal)))
        {
            var platform=arena.GetComponentsInChildren<Renderer>(true)
                .FirstOrDefault(x=>x.name=="Podest drewniany");
            if(platform)Check(platform.transform,platform.bounds.size.y,1f,"wysokość podestu");
            else results.Add("Nie odnaleziono drewnianego podestu: "+arena.label);
        }
        foreach(var body in scene.GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<CharacterController>(true)))
        {
            float height=body.height*Mathf.Abs(body.transform.lossyScale.y)/K;
            if(Mathf.Abs(height-1.8f)>.01f)
                results.Add("Wzrost postaci: "+PathOf(body.transform)+" kapsuła="+
                    height.ToString("F3")+" m; tabela właściciela A.1=1.800 m");
        }
        return results.ToArray();
    }

    static string[] ReviewOriginalMurals(Renderer[] renderers)
    {
        var results=new List<string>();
        var murals=renderers.Where(x=>x.name.StartsWith("JPEG 1:1 — ",StringComparison.Ordinal))
            .ToArray();
        if(murals.Length!=4)
            results.Add("Niezgodna liczba oryginalnych murali: "+murals.Length+"; wymagane 4");
        foreach(var r in murals)
        {
            string key=r.name;
            var source=r.sharedMaterial?r.sharedMaterial.mainTexture as Texture2D:null;
            if(!source)
            {
                results.Add("Mural bez oryginalnej tekstury: "+key);
                continue;
            }
            float actual=Mathf.Abs(r.transform.lossyScale.x)/
                Mathf.Max(.0001f,Mathf.Abs(r.transform.lossyScale.y));
            float expected=source.width/(float)source.height;
            if(Mathf.Abs(actual-expected)>.02f)
                results.Add("Mural rozciągnięty: "+key+
                    " siatka="+actual+" oryginał="+expected);
            var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(source))
                as TextureImporter;
            if(importer)
            {
                importer.GetSourceTextureWidthAndHeight(out int w,out int h);
                if(w<4096)
                    results.Add("Mural za mały (<4096px szer.): "+key+" "+w+"x"+h);
            }
            var facing=r.transform.forward;
            var from=r.bounds.center+facing*(.4f*K);
            var walls=Physics.RaycastAll(from,-facing,1.25f*K,~0,
                QueryTriggerInteraction.Ignore).Where(h=>h.collider &&
                    !h.collider.transform.IsChildOf(r.transform) &&
                    Vector3.Dot(h.normal,facing)>.55f).ToArray();
            if(walls.Length==0)
                results.Add("Mural bez potwierdzonego styku ze ścianą: "+key);
        }
        return results.ToArray();
    }

    // WOREK: korpus 1,2 m; dół 0,6 m nad fizycznym podłożem,
    // nie nad umownym y=0. Oryginalny GLB nadal pozostaje nietknięty.
    static string[] ReviewRealBagClearance(Scene scene)
    {
        var bags=scene.GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<BraxWorek>(true))
            .Where(b=>b&&b.isActiveAndEnabled).ToArray();
        var findings=new List<string>();
        if(bags.Length!=48)
            findings.Add("BAG_COUNT_MISMATCH: "+bags.Length+"/48");
        foreach(var bag in bags)
        {
            float body=(bag.dol-bag.gora)/K;
            if(Mathf.Abs(body-1.2f)>.03f)
                findings.Add("Worek korpus !=1,2m: "+PathOf(bag.transform)+
                    " measured="+body.ToString("F3"));
            var origin=bag.transform.position+Vector3.up*.30f*K;
            var support=Physics.RaycastAll(origin,Vector3.down,5f*K,
                ~0,QueryTriggerInteraction.Ignore)
                .Where(h=>h.collider && !h.collider.transform.IsChildOf(bag.transform)
                    && h.normal.y>.65f && h.point.y<bag.transform.position.y)
                .OrderByDescending(h=>h.point.y).FirstOrDefault();
            if(!support.collider)
            {
                findings.Add("Worek bez podłoża: "+PathOf(bag.transform));
                continue;
            }
            float clearance=(bag.transform.position.y-bag.dol-support.point.y)/K;
            if(Mathf.Abs(clearance-.6f)>.03f)
                findings.Add("Worek dół !=0,6m nad podłożem: "+
                    PathOf(bag.transform)+" gap="+clearance.ToString("F3"));
        }
        return findings.ToArray();
    }

    static string BuildingScope(Transform t)
    {
        for(var p=t;p;p=p.parent)
        {
            if(p.name.StartsWith("Domek ",StringComparison.Ordinal) ||
                p.name=="Stołówka" || p.name=="Magazyn" ||
                p.name=="Punkt medyczny" || p.name.Contains("ŁAŹNIA"))
                return PathOf(p);
        }
        return null;
    }

    static string[] ReviewFurnitureOverlaps(Scene scene)
    {
        var candidates=scene.GetRootGameObjects()
            .SelectMany(x=>x.GetComponentsInChildren<Transform>(true))
            .Where(t=>t && (t.name.StartsWith("ORYGINAŁ IKEA IDANAS — łóżko") ||
                t.name=="Właściciel / oryginalny metal_cabinet_low_poly.glb" ||
                t.name.StartsWith("Oryginalny stół polowy 3D") ||
                t.name.Contains("drawer_cabinet_4k") ||
                t.name.Contains("picnic_set_1.glb")))
            .Select(t=>new {transform=t,scope=BuildingScope(t),bounds=RenderedBounds(t)})
            .Where(x=>x.scope!=null && x.bounds.size.sqrMagnitude>.01f)
            .ToArray();
        var results=new List<string>();
        for(int i=0;i<candidates.Length;i++)
        for(int j=i+1;j<candidates.Length;j++)
        {
            var a=candidates[i];var c=candidates[j];
            if(a.scope!=c.scope || a.transform.IsChildOf(c.transform) ||
                c.transform.IsChildOf(a.transform) ||
                !a.bounds.Intersects(c.bounds))continue;
            float ox=Mathf.Min(a.bounds.max.x,c.bounds.max.x)-
                     Mathf.Max(a.bounds.min.x,c.bounds.min.x);
            float oy=Mathf.Min(a.bounds.max.y,c.bounds.max.y)-
                     Mathf.Max(a.bounds.min.y,c.bounds.min.y);
            float oz=Mathf.Min(a.bounds.max.z,c.bounds.max.z)-
                     Mathf.Max(a.bounds.min.z,c.bounds.min.z);
            if(Mathf.Min(ox,Mathf.Min(oy,oz))<=Tolerance)continue;
            results.Add("Kandydat nachodzenia (>3cm AABB): "+
                PathOf(a.transform)+" vs "+PathOf(c.transform)+
                "; przenikanie="+(Mathf.Min(ox,Mathf.Min(oy,oz))/K).ToString("F3")+"m");
        }
        return results.ToArray();
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
            Physics.SyncTransforms();
            var placements=scene.GetRootGameObjects()
                .SelectMany(g=>g.GetComponentsInChildren<BraxObozPlacementRecord>(true))
                .Where(r=>r&&r.gameObject.activeInHierarchy).Select(InspectPlacement).ToArray();
            var characters=scene.GetRootGameObjects()
                .SelectMany(g=>g.GetComponentsInChildren<CharacterController>(true))
                .Where(c=>c&&c.gameObject.activeInHierarchy).Select(InspectCharacter).ToArray();
            var biggest=LargestSources();
            var muralIssues=ReviewOriginalMurals(renderers);
            var scaleIssues=ReviewDimensions(scene);
            var overlaps=ReviewFurnitureOverlaps(scene);
            var bagIssues=ReviewRealBagClearance(scene);
            var report=new Report {
                scene=BraxObozActors.ScenePath,
                revision=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local",
                unitySceneInspected=true,apkBuilt=false,phoneVerified=false,
                renderers=all.Length,items=all
            };
            report.placements=placements;report.characters=characters;
            report.bull=InspectBull(scene);
            report.ground=BraxObozGroundAudit.Inspect(scene);
            report.checkedGroundObjects=placements.Count(x=>x.measured&&x.kind=="Ground");
            report.checkedWallObjects=placements.Count(x=>x.measured&&x.kind=="Wall");
            report.unmeasuredObjects=placements.Count(x=>!x.measured);
            report.wallWarnings=placements.Count(x=>x.kind=="Wall"&&
                (!x.measured||Mathf.Abs(x.gapMetres)>.03f||
                 (x.expectedHeightMetres>=0&&(!x.heightMeasured||Mathf.Abs(x.heightErrorMetres)>.03f))));
            report.checkedCharacters=characters.Count(x=>x.feetMeasured&&x.groundMeasured);
            report.characterWarnings=characters.Count(x=>!x.feetMeasured||!x.groundMeasured||
                Mathf.Abs(x.footGapMetres)>.03f);
            report.levitating=all.Count(x=>x.status=="LEWITUJE > 3 cm")+
                placements.Count(x=>x.measured&&x.gapMetres>.03f);
            report.embedded=all.Count(x=>x.status=="ZAPADNIĘTE > 3 cm")+
                placements.Count(x=>x.measured&&x.gapMetres<-.03f);
            report.missingGroundRay=all.Count(x=>x.status=="brak powierzchni w raycast");
            report.expectedMounted=all.Count(x=>x.classification=="montowany");
            report.unclassified=all.Count(x=>x.status=="brak testu");
            report.largestSources=biggest;
            report.dependentSourceBytes=AllDependentSourceBytes();
            report.checkedBags=scene.GetRootGameObjects()
                .SelectMany(g=>g.GetComponentsInChildren<BraxWorek>(true)).Count();
            report.bagFindings=bagIssues;report.bagWarnings=bagIssues.Length;
            report.presentMurals=renderers.Count(r=>r.name.StartsWith("JPEG 1:1 — "));
            report.muralFindings=muralIssues;report.muralWarnings=muralIssues.Length;
            report.scaleFindings=scaleIssues;report.scaleWarnings=scaleIssues.Length;
            report.overlapFindings=overlaps;report.overlapCandidates=overlaps.Length;
            report.allPlacementsVerified=report.levitating==0&&report.embedded==0&&
                report.missingGroundRay==0&&report.unmeasuredObjects==0&&
                report.characterWarnings==0&&report.wallWarnings==0&&
                report.overlapCandidates==0&&report.expectedMounted==0;
            string folder=System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath,"../../audit"));
            Directory.CreateDirectory(folder);
            try
            {
                var info=scene.GetRootGameObjects()
                    .SelectMany(g=>g.GetComponentsInChildren<BraxObozSceneInfo>(true))
                    .FirstOrDefault();
                if(info)BraxObozCollisionReview.Write(info,
                    System.IO.Path.Combine(folder,"oboz-wejscia-kolizje.json"));
                else BraxBuildQuality.Warn("OBÓZ_SCENE_AUDIT_NO_SCENE_INFO");
            }
            catch(Exception ex) {
                BraxBuildQuality.Warn("OBÓZ_SCENE_AUDIT_DOOR_REPORT_FAILED: "+ex);
            }
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
            md.AppendLine("Całe modele zmierzone: grunt="+report.checkedGroundObjects+
                "; ściana="+report.checkedWallObjects+"; niezmierzone="+report.unmeasuredObjects);
            md.AppendLine("Postacie z pomiarem podeszwy: "+report.checkedCharacters+
                "; zastrzeżenia="+report.characterWarnings+"; poślizg w ruchu: NIEZBADANY");
            md.AppendLine("Byk — zachowana własna skala 4×: "+report.bull.scalePreserved+
                "; zmierzone="+report.bull.measuredWorldSize.ToString("F6")+
                "; wzorzec="+report.bull.expectedWorldSize.ToString("F6"));
            md.AppendLine("Wszystkie kontakty potwierdzone: "+report.allPlacementsVerified+
                " — zero błędów nie oznacza odbioru elementów niezmierzonych.");
            md.AppendLine("Murale: "+report.presentMurals+"/4; zastrzeżenia: "+report.muralWarnings);
            md.AppendLine("Grunt i dojścia: "+report.ground.approaches.Count(x=>x.continuous)+
                "/"+report.ground.approaches.Length+" ciągłych podejść w raycast; maty z miejscem na 3 cm="+
                report.ground.mats.Count(x=>x.roomForFullDepression)+"/"+report.ground.mats.Length+
                "; zastrzeżenia="+report.ground.warnings+"; pomiar FPS/telefon: NIE");
            foreach(var finding in report.ground.findings)md.AppendLine("- GRUNT: "+finding);
            md.AppendLine("Błędy skali: "+report.scaleWarnings+
                "; kandydaci przenikania mebli >3cm: "+report.overlapCandidates);
            md.AppendLine("Worki: "+report.checkedBags+"/48; odstęp/korpus błędne: "+
                report.bagWarnings);
            md.AppendLine("Łączne bajty zależnych PLIKÓW ŹRÓDŁOWYCH, NIE APK: "+
                report.dependentSourceBytes);
            md.AppendLine("Weryfikacja wizualna/telefon: NIE");
            md.AppendLine("20 największych zasobów zależnych — BAJTY ŹRÓDŁOWE, NIE ROZMIAR APK:");
            foreach(var x in biggest)md.AppendLine("- "+x.sourceBytes+" B — "+x.path);
            md.AppendLine("Przykładowe niezgodności (pełny wykaz w JSON):");
            foreach(var x in muralIssues.Take(10))md.AppendLine("- MURAL: "+x);
            foreach(var x in scaleIssues.Take(12))md.AppendLine("- SKALA: "+x);
            foreach(var x in overlaps.Take(12))md.AppendLine("- NACHODZENIE: "+x);
            foreach(var x in bagIssues.Take(12))md.AppendLine("- WOREK: "+x);
            foreach(var x in placements.Where(x=>!x.measured||Mathf.Abs(x.gapMetres)>.03f))
                md.AppendLine("- MODEL: "+x.path+"; "+x.status+"; gap="+x.gapMetres.ToString("F3"));
            foreach(var x in characters)
                md.AppendLine("- POSTAĆ: "+x.path+"; "+x.status+"; gap="+x.footGapMetres.ToString("F3"));
            foreach(var x in all.Where(x=>x.status.Contains("3 cm")||
                x.status=="brak powierzchni w raycast").Take(56))
                md.AppendLine("- "+x.status+" "+x.path+" gap="+x.gapMetres.ToString("F3"));
            File.WriteAllText(System.IO.Path.Combine(folder,"oboz-scena.md"),md.ToString());
            Debug.LogWarning("BRAXHORN_OBOZ_SCENE_AUDIT renderers="+report.renderers+
                " float="+report.levitating+" embedded="+report.embedded+
                " murals="+report.presentMurals+"/4 scale="+report.scaleWarnings+
                " overlaps="+report.overlapCandidates+
                " bagWarnings="+report.bagWarnings+
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
        var diagnostics=new Diagnostics {
            revision=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local"};
        void Capture(string message,string stackTrace,LogType type)
        {
            if(type==LogType.Warning)diagnostics.warningCount++;
            else if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)diagnostics.errorCount++;
            else return;
            diagnostics.entries.Add(new LogEntry {level=type.ToString(),message=message,stackTrace=stackTrace});
        }
        Application.logMessageReceived+=Capture;
        try
        {
            SaveUntitledSceneInBatch();
            BraxObozPlacementRegression.Run();
            BraxObozEnvironment.Prepare();
            BraxObozActors.Assemble();
            Run();
            diagnostics.generationCompleted=true;
        }
        catch(Exception ex) {diagnostics.firstFatalException=ex.ToString();throw;}
        finally
        {
            Application.logMessageReceived-=Capture;
            string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../audit"));
            Directory.CreateDirectory(folder);
            File.WriteAllText(System.IO.Path.Combine(folder,"oboz-diagnostyka.json"),JsonUtility.ToJson(diagnostics,true));
        }
    }
}
