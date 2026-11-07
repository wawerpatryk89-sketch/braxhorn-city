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
    const float NumericalToleranceMetres=.0005f;
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
        public string rawStatus,resolution;
        public bool verified;
        public float rawGapMetres;
        public string[] materialSources;
        public float x,y,z,bottomY,gapMetres,widthMetres,heightMetres,depthMetres;
    }
    [Serializable] public sealed class SizeEntry { public string path; public long sourceBytes; }
    [Serializable] public sealed class PlacementItem
    {
        public string path,label,kind,status,supportPath;
        public string supportMeasurementMethod;
        public int supportTrianglesTested;
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
    [Serializable] public sealed class BagItem
    {
        public string path,source,status,groundSupportPath,beamSupportPath,meshName;
        public string attachmentMeasurementMethod;
        public bool bodyMeasured,groundMeasured,attachmentMeasured;
        public float bodyMetres,clearanceMetres,attachmentGapMetres=-1f;
        public int triangles,attachmentTrianglesTested;
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
        public int rawLevitatingRenderers,rawEmbeddedRenderers,rawMissingGroundRenderers;
        public int verifiedConstructionRenderers,unverifiedRenderers,placementHeightWarnings;
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
        public BagItem[] bags;
        public BraxObozStructureAudit.Result structures;
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
        // Historical label retained for comparison only. A name never proves
        // that a wall, roof or chain is actually connected to its support.
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
        var ground=r.GetComponent<BraxObozStructureGround>();
        return ground&&ground.visibleSurface==r&&ground.geometry&&ground.surface&&
            ground.surface.sharedMesh==ground.geometry.sharedMesh&&
            ground.surface.transform==ground.geometry.transform;
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

    static Item InspectRaw(Renderer r)
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
        if(IsNaturalTerrain(r)) {item.status="grunt";return item;}
        if(r is ParticleSystemRenderer)
        {item.classification="efekt cząsteczkowy";item.status="efekt bez stałej bryły";return item;}
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
        if(item.gapMetres>.03f+NumericalToleranceMetres)item.status="LEWITUJE > 3 cm";
        else if(item.gapMetres<-.03f-NumericalToleranceMetres)item.status="ZAPADNIĘTE > 3 cm";
        else item.status="kontakt w tolerancji 3 cm";
        return item;
    }

    static Item Inspect(Renderer renderer,BraxObozStructureAudit.Result structures,
        Dictionary<BraxObozPlacementRecord,PlacementItem> placements,
        Dictionary<string,BagItem> bags,Dictionary<CharacterController,CharacterItem> characters)
    {
        var item=InspectRaw(renderer);
        item.rawStatus=item.status;item.rawGapMetres=item.gapMetres;
        var fluid=renderer.GetComponent<BraxObozFluidSurface>();
        if(fluid && fluid.surface==renderer && fluid.bed && fluid.bed.enabled &&
            fluid.bed.gameObject.scene==renderer.gameObject.scene)
        {
            var b=renderer.bounds;
            var ray=new Ray(new Vector3(b.center.x,b.max.y+K,b.center.z),Vector3.down);
            if(fluid.bed.Raycast(ray,out var hit,4f*K) && hit.normal.y>.65f)
            {
                float depth=(b.min.y-hit.point.y)/K;
                bool ok=depth>=fluid.minimumDepthMetres-NumericalToleranceMetres &&
                    depth<=fluid.maximumDepthMetres+NumericalToleranceMetres;
                item.classification="jawna tafla cieczy";
                item.status=ok?"zmierzona głębokość nad terenem":"TAFLA CIECZY POZA ZAKRESEM";
                item.verified=ok;
                item.gapMetres=depth;
                item.resolution="głębokość="+depth.ToString("F3")+" m; zakres="+
                    fluid.minimumDepthMetres.ToString("F2")+"–"+fluid.maximumDepthMetres.ToString("F2")+" m";
                return item;
            }
            item.classification="jawna tafla cieczy";item.status="brak zmierzonego dna cieczy";
            item.verified=false;item.resolution="jawny MeshCollider terenu nie został trafiony";
            return item;
        }
        if(IsNaturalTerrain(renderer))
        {item.verified=true;item.resolution="zgodna siatka widocznego gruntu i collidera";return item;}
        if(renderer is ParticleSystemRenderer)
        {item.verified=true;item.resolution="cząsteczki efektu; nie są obiektem stojącym";return item;}
        if(structures.TryGet(renderer,out var structure))
        {
            item.classification="jawnie zarejestrowana konstrukcja";
            item.verified=structure.verified;item.resolution=structure.status;
            if(structure.verified)item.status="zmierzona droga podparcia wszystkich części";
            // Failed geometry coverage must not fall back to a parent record.
            return item;
        }
        var placement=renderer.GetComponentsInParent<BraxObozPlacementRecord>(true)
            .FirstOrDefault(record=>record.Contains(renderer));
        if(placement&&placements.TryGetValue(placement,out var measured))
        {
            item.classification="jawna część ustawionego modelu";
            item.status="pomiar całego modelu w placements";
            item.verified=measured.measured&&Mathf.Abs(measured.gapMetres)<=.03f&&
                (measured.expectedHeightMetres<0||measured.heightMeasured&&Mathf.Abs(measured.heightErrorMetres)<=.03f);
            item.resolution=measured.status;return item;
        }
        var bag=renderer.GetComponentsInParent<BraxObozBagGeometry>(true)
            .FirstOrDefault(geometry=>geometry.Contains(renderer));
        if(bag&&bags.TryGetValue(PathOf(bag.transform),out var bagMeasurement))
        {
            item.classification="oryginalny worek z zawiesiem";
            item.status="pomiar korpusu, odstępu i zaczepu w bags";
            item.verified=bagMeasurement.bodyMeasured&&bagMeasurement.groundMeasured&&bagMeasurement.attachmentMeasured&&
                Mathf.Abs(bagMeasurement.bodyMetres-1.2f)<=.03f&&
                Mathf.Abs(bagMeasurement.clearanceMetres-.6f)<=.03f&&bagMeasurement.attachmentGapMetres<=.03f;
            item.resolution=bagMeasurement.status;return item;
        }
        var feet=renderer.GetComponentsInParent<BraxObozIdleFootContact>(true)
            .FirstOrDefault(contact=>contact.Contains(renderer));
        if(feet&&feet.body&&characters.TryGetValue(feet.body,out var character))
        {
            item.classification="oryginalny skin mierzonej postaci";
            item.status="pomiar podeszew w characters";
            item.verified=character.feetMeasured&&character.groundMeasured&&Mathf.Abs(character.footGapMetres)<=.03f;
            item.resolution=character.status;return item;
        }
        item.verified=item.status=="kontakt w tolerancji 3 cm";
        item.resolution=item.verified?"bezpośredni pomiar dolnego obrysu względem podłoża":
            "surowy kandydat pozostaje; brak niezależnego potwierdzenia podparcia";
        return item;
    }

    static PlacementItem InspectPlacement(BraxObozPlacementRecord record)
    {
        var item=new PlacementItem {path=PathOf(record.transform),label=record.label,
            kind=record.support.ToString(),status="niezmierzony"};
        var renderers=(record.members??Array.Empty<Renderer>())
            .Where(r=>r&&r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(renderers.Length==0) {item.status="brak aktywnej geometrii";return item;}
        var bounds=record.contactRenderers!=null&&record.contactRenderers.Length>0
            ? BraxObozPlacement.Bounds(record.contactRenderers):BraxObozPlacement.Bounds(renderers);
        var contact=record.contactPoint?record.contactPoint.position:bounds.center;
        item.expectedHeightMetres=record.heightAboveFloorMetres;
        if(record.heightAboveFloorMetres>=0)
        {
            var origin=new Vector3(contact.x,bounds.min.y+.15f*K,contact.z);
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
            var origin=new Vector3(contact.x,bounds.max.y+.5f*K,contact.z);
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
        else if(record.support==BraxObozPlacementRecord.SupportKind.Suspended)
        {
            if(!record.suspensionPoint||!record.suspensionSupport)
            {item.status="brak geometrii zaczepu lub podpory";return item;}
            var point=record.suspensionPoint.position;
            var support=record.suspensionSupport;
            if(!support.enabled||!support.gameObject.activeInHierarchy||support.isTrigger||
                support.gameObject.scene!=record.gameObject.scene)
            {item.status="nieaktywna podpora podwieszenia";return item;}
            item.supportPath=PathOf(support.transform);
            if(!BraxObozSupportSurface.TryMeasure(support,point,out var supportMeasurement))
            {
                item.measured=false;item.gapMetres=-1f;
                item.status=supportMeasurement.status;return item;
            }
            item.measured=true;item.gapMetres=supportMeasurement.distanceWorld/K;
            item.supportMeasurementMethod=supportMeasurement.method;
            item.supportTrianglesTested=supportMeasurement.trianglesTested;
        }
        else {item.status="brak zadeklarowanego podparcia — wymaga pomiaru";return item;}
        item.status=item.gapMetres>.03f+NumericalToleranceMetres?"LEWITUJE > 3 cm":
            item.gapMetres<-.03f-NumericalToleranceMetres?"ZAPADNIĘTE > 3 cm":"kontakt w tolerancji 3 cm";
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
            var placement=t.GetComponent<BraxObozPlacementRecord>();
            // The medical room uses the same original under a different name.
            // Audit its mattress too; the cabin prefix is not the asset's identity.
            if(placement&&placement.label=="IKEA IDANAS")
            {
                var bounds=RenderedBounds(t);
                Check(t,Mathf.Max(bounds.size.x,bounds.size.z),2f,"długość łóżka");
                // The delivered bed's blanket overhang is 1.262567 m wide.
                // Its actual mattress is 0.927123 m, 0.895771 m after the
                // uniform 2/2.07 length calibration. Never squash the blanket
                // or misreport that full visual envelope as mattress width.
                var mattress=t.GetComponentsInChildren<Renderer>(true)
                    .Where(r=>r&&r.name=="Mesh21_Color_M03_0").ToArray();
                if(mattress.Length==0)results.Add("Brak pomiaru oryginalnego materaca: "+PathOf(t));
                else
                {
                    Check(t,BraxObozPlacement.Dimension(mattress,BraxObozPlacement.Axis.X,t.parent),
                        .9f,"szerokość materaca IDANAS");
                    float top=(BraxObozPlacement.Bounds(mattress).max.y-bounds.min.y)/K;
                    if(Mathf.Abs(top-.45f)>.03f)
                        results.Add("Oryginalny IDANAS: powierzchnia materaca "+top.ToString("F4")+
                            " m, tabela 0.45 m; zachowano proporcje oryginału: "+PathOf(t));
                }
            }
            else if(n=="Właściciel / oryginalny metal_cabinet_low_poly.glb")
                Check(t,RenderedBounds(t).size.y,2f,"wysokość szafy");
            else if(n.StartsWith("Oryginalny stół polowy 3D",StringComparison.Ordinal))
                Check(t,RenderedBounds(t).size.y,.75f,"wysokość stołu");
        }
        foreach(var record in transforms.Select(t=>t.GetComponent<BraxObozPlacementRecord>())
            .Where(r=>r&&r.expectedDimensionMetres>0))
        {
            var geometry=record.members??Array.Empty<Renderer>();
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
            var platform=arena.GetComponent<BraxObozStructureRecord>();
            var deck=platform?platform.heightMembers:Array.Empty<Renderer>();
            if(deck!=null&&deck.Length>0)Check(arena.transform,
                BraxObozPlacement.Dimension(deck,BraxObozPlacement.Axis.Y,arena.transform),1f,"wysokość podestu");
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
            var filter=r.GetComponent<MeshFilter>();
            if(!filter || !filter.sharedMesh || !filter.sharedMesh.isReadable ||
                filter.sharedMesh.normals.Length==0)
            {results.Add("Mural bez geometrii normalnej: "+key);continue;}
            // Actual visible face of the Quad, not Transform.forward. Unity's
            // primitive has local -Z normals; an unturned image faces the wall.
            var facing=r.localToWorldMatrix.inverse.transpose
                .MultiplyVector(filter.sharedMesh.normals[0]).normalized;
            var from=r.bounds.center+facing*(.4f*K);
            var walls=Physics.RaycastAll(from,-facing,1.25f*K,~0,
                QueryTriggerInteraction.Ignore).Where(h=>h.collider &&
                    !h.collider.transform.IsChildOf(r.transform) &&
                    Vector3.Dot(h.normal,facing)>.55f).ToArray();
            if(walls.Length==0)
                results.Add("Mural bez potwierdzonego styku ze ścianą: "+key);
            else
            {
                var wall=walls.OrderBy(h=>h.distance).First();
                float gap=Vector3.Dot(r.bounds.center-wall.point,facing)/K;
                if(gap<-.003f||gap>.03f)
                    results.Add("Mural odsunięty lub wbity: "+key+" odstęp="+gap.ToString("F4")+" m");
            }
        }
        return results.ToArray();
    }

    // WOREK: korpus 1,2 m; dół 0,6 m nad fizycznym podłożem,
    // nie nad umownym y=0. Oryginalny GLB nadal pozostaje nietknięty.
    static string[] ReviewRealBagClearance(Scene scene,out BagItem[] measurements)
    {
        var bags=scene.GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<BraxWorek>(true))
            .Where(b=>b&&b.isActiveAndEnabled).ToArray();
        var findings=new List<string>();var results=new List<BagItem>();
        var sharedMeshes=new HashSet<Mesh>();var sharedMaterials=new HashSet<Material>();
        if(bags.Length!=48)findings.Add("BAG_COUNT_MISMATCH: "+bags.Length+"/48");
        foreach(var bag in bags)
        {
            var item=new BagItem{path=PathOf(bag.transform),status="brak pomiaru oryginalnej geometrii"};
            results.Add(item);
            var geometry=bag.GetComponent<BraxObozBagGeometry>();
            if(!geometry||!geometry.TryMeasureBodyBounds(out var body))
            {findings.Add("Worek bez zmierzonego korpusu: "+item.path);continue;}
            item.source=geometry.sourcePath;item.bodyMeasured=true;
            item.bodyMetres=body.size.y/K;
            if(Mathf.Abs(item.bodyMetres-1.2f)>.03f)
                findings.Add("Worek korpus !=1,2m: "+item.path+" measured="+item.bodyMetres.ToString("F4"));
            var ground=geometry.groundSupport;
            if(ground&&ground.enabled&&ground.gameObject.activeInHierarchy&&
                ground.gameObject.scene==scene&&ground.Raycast(new Ray(
                    new Vector3(body.center.x,body.max.y+.35f*K,body.center.z),Vector3.down),
                    out var hit,body.size.y+2f*K))
            {
                item.groundMeasured=true;item.groundSupportPath=PathOf(ground.transform);
                item.clearanceMetres=(body.min.y-hit.point.y)/K;
                if(Mathf.Abs(item.clearanceMetres-.6f)>.03f)
                    findings.Add("Worek dół !=0,6m nad zmierzoną powierzchnią: "+
                        item.path+" gap="+item.clearanceMetres.ToString("F4"));
            }
            else findings.Add("Worek bez podłoża pod rzeczywistym korpusem: "+item.path);
            var beam=geometry.beamSupport;
            if(beam&&beam.enabled&&beam.gameObject.activeInHierarchy&&beam.gameObject.scene==scene&&
                geometry.TryMeasureAttachment(out var attachment))
            {
                item.beamSupportPath=PathOf(beam.transform);
                item.attachmentMeasured=BraxObozSupportSurface.TryMeasure(
                    beam,attachment,out var jointMeasurement);
                item.attachmentMeasurementMethod=jointMeasurement.method;
                item.attachmentTrianglesTested=jointMeasurement.trianglesTested;
                if(item.attachmentMeasured)
                    item.attachmentGapMetres=jointMeasurement.distanceWorld/K;
                else findings.Add("Niezmierzony styk zawiesia worka z belką: "+
                    item.path+"; "+jointMeasurement.status);
                if(item.attachmentMeasured&&item.attachmentGapMetres>.03f)
                    findings.Add("Zawiesie worka nie dotyka belki: "+item.path+
                        " gap="+item.attachmentGapMetres.ToString("F4"));
            }
            else findings.Add("Worek bez zmierzonego zaczepu lub belki: "+item.path);
            var mesh=geometry.SharedMesh;
            if(mesh)
            {
                sharedMeshes.Add(mesh);item.meshName=mesh.name;
                for(int sm=0;sm<mesh.subMeshCount;sm++)item.triangles+=checked((int)mesh.GetIndexCount(sm))/3;
            }
            var renderers=bag.GetComponentsInChildren<MeshRenderer>(true);
            foreach(var renderer in renderers)
            foreach(var material in renderer.sharedMaterials)
            {
                if(material)sharedMaterials.Add(material);
                if(!material||!material.enableInstancing)
                    findings.Add("Worek bez wspólnego materiału GPU: "+item.path);
            }
            item.status=item.bodyMeasured&&item.groundMeasured&&item.attachmentMeasured&&
                Mathf.Abs(item.bodyMetres-1.2f)<=.03f&&Mathf.Abs(item.clearanceMetres-.6f)<=.03f&&
                item.attachmentGapMetres<=.03f?"korpus, prześwit i zaczep potwierdzone":"wymaga poprawy";
        }
        if(bags.Length>0&&(sharedMeshes.Count!=1||sharedMaterials.Count!=1))
            findings.Add("Worki nie współdzielą jednej siatki/materiału: meshes="+
                sharedMeshes.Count+" materials="+sharedMaterials.Count);
        measurements=results.ToArray();return findings.ToArray();
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
            Physics.SyncTransforms();
            var placementMeasurements=scene.GetRootGameObjects()
                .SelectMany(g=>g.GetComponentsInChildren<BraxObozPlacementRecord>(true))
                .Where(r=>r&&r.gameObject.activeInHierarchy).ToDictionary(r=>r,InspectPlacement);
            var characterMeasurements=scene.GetRootGameObjects()
                .SelectMany(g=>g.GetComponentsInChildren<CharacterController>(true))
                .Where(c=>c&&c.gameObject.activeInHierarchy).ToDictionary(c=>c,InspectCharacter);
            var placements=placementMeasurements.Values.ToArray();
            var characters=characterMeasurements.Values.ToArray();
            var bagIssues=ReviewRealBagClearance(scene,out var bagMeasurements);
            var bagsByPath=bagMeasurements.ToDictionary(b=>b.path);
            var structures=BraxObozStructureAudit.Inspect(scene);
            var all=renderers.Select(r=>Inspect(r,structures,placementMeasurements,bagsByPath,characterMeasurements)).ToArray();
            var biggest=LargestSources();
            var muralIssues=ReviewOriginalMurals(renderers);
            var scaleIssues=ReviewDimensions(scene);
            var overlaps=ReviewFurnitureOverlaps(scene);
            var report=new Report {
                scene=BraxObozActors.ScenePath,
                revision=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local",
                unitySceneInspected=true,apkBuilt=false,phoneVerified=false,
                renderers=all.Length,items=all
            };
            report.placements=placements;report.characters=characters;
            report.structures=structures;
            report.rawLevitatingRenderers=all.Count(x=>x.rawStatus=="LEWITUJE > 3 cm");
            report.rawEmbeddedRenderers=all.Count(x=>x.rawStatus=="ZAPADNIĘTE > 3 cm");
            report.rawMissingGroundRenderers=all.Count(x=>x.rawStatus=="brak powierzchni w raycast");
            report.verifiedConstructionRenderers=structures.renderers.Count(r=>r.verified);
            report.unverifiedRenderers=all.Count(x=>!x.verified);
            report.bull=InspectBull(scene);
            report.ground=BraxObozGroundAudit.Inspect(scene);
            report.checkedGroundObjects=placements.Count(x=>x.measured&&x.kind=="Ground");
            report.checkedWallObjects=placements.Count(x=>x.measured&&x.kind=="Wall");
            report.unmeasuredObjects=placements.Count(x=>!x.measured);
            report.wallWarnings=placements.Count(x=>x.kind=="Wall"&&
                (!x.measured||Mathf.Abs(x.gapMetres)>.03f||
                 (x.expectedHeightMetres>=0&&(!x.heightMeasured||Mathf.Abs(x.heightErrorMetres)>.03f))));
            report.placementHeightWarnings=placements.Count(x=>x.expectedHeightMetres>=0&&
                (!x.heightMeasured||Mathf.Abs(x.heightErrorMetres)>.03f));
            report.checkedCharacters=characters.Count(x=>x.feetMeasured&&x.groundMeasured);
            report.characterWarnings=characters.Count(x=>!x.feetMeasured||!x.groundMeasured||
                Mathf.Abs(x.footGapMetres)>.03f);
            report.levitating=all.Count(x=>x.status=="LEWITUJE > 3 cm")+
                placements.Count(x=>x.measured&&x.gapMetres>.03f);
            report.embedded=all.Count(x=>x.status=="ZAPADNIĘTE > 3 cm")+
                placements.Count(x=>x.measured&&x.gapMetres<-.03f);
            report.missingGroundRay=all.Count(x=>x.status=="brak powierzchni w raycast");
            report.expectedMounted=all.Count(x=>x.classification=="montowany"&&!x.verified);
            report.unclassified=all.Count(x=>x.status=="brak testu");
            report.largestSources=biggest;
            report.dependentSourceBytes=AllDependentSourceBytes();
            report.checkedBags=scene.GetRootGameObjects()
                .SelectMany(g=>g.GetComponentsInChildren<BraxWorek>(true)).Count();
            report.bags=bagMeasurements;report.bagFindings=bagIssues;report.bagWarnings=bagIssues.Length;
            report.presentMurals=renderers.Count(r=>r.name.StartsWith("JPEG 1:1 — "));
            report.muralFindings=muralIssues;report.muralWarnings=muralIssues.Length;
            report.scaleFindings=scaleIssues;report.scaleWarnings=scaleIssues.Length;
            report.overlapFindings=overlaps;report.overlapCandidates=overlaps.Length;
            report.allPlacementsVerified=report.bull.present&&report.bull.scalePreserved&&
                report.levitating==0&&report.embedded==0&&
                report.missingGroundRay==0&&report.unmeasuredObjects==0&&
                report.characterWarnings==0&&report.wallWarnings==0&&report.bagWarnings==0&&
                report.overlapCandidates==0&&report.expectedMounted==0&&report.unverifiedRenderers==0&&
                report.placementHeightWarnings==0&&report.scaleWarnings==0&&report.muralWarnings==0&&
                structures.unsupportedPieces==0&&structures.embeddedPieces==0&&structures.invalidGrounds==0&&
                structures.unmeasuredPieces==0&&structures.unmeasuredRenderers==0&&structures.heightWarnings==0;
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
            md.AppendLine("Surowe renderery przed pomiarem całych modeli/konstrukcji: lewitacje="+
                report.rawLevitatingRenderers+"; zagłębienia="+report.rawEmbeddedRenderers+
                "; brak promienia="+report.rawMissingGroundRenderers+". Każdy zachowany jako rawStatus/rawGapMetres w JSON.");
            md.AppendLine("Konstrukcje: renderery ze zmierzonym podparciem="+report.verifiedConstructionRenderers+
                "; części bez drogi podparcia="+structures.unsupportedPieces+"; zagłębione="+structures.embeddedPieces+
                "; części niezmierzone="+structures.unmeasuredPieces+"; pary poza budżetem="+structures.unmeasuredPairChecks);
            md.AppendLine("Niepotwierdzone renderery="+report.unverifiedRenderers+
                "; niepotwierdzony montaż="+report.expectedMounted+"; wysokości modeli poza tolerancją="+report.placementHeightWarnings);
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
