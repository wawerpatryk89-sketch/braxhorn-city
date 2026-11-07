using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BraxObozServices
{
    // Rebuilt from our existing board-building source. Owner GLBs stay intact
    // in Assets/Source/Prywatne/Oboz/OwnerAssets and are instantiated separately.
    const string Root="Assets/PlayableGenerated/Oboz/ServiceModels/";
    const string OwnerRoot="Assets/Source/Prywatne/Oboz/OwnerAssets/";
    const string KitchenFile="kitchen_-_assets.glb";
    const string ReflectionFolder="Assets/PlayableGenerated/Oboz/Environment/Reflections";
    const float K=1.294f;
    [Serializable] public sealed class Box { public float[] position,size;public float yaw; }
    [Serializable] public sealed class Door { public string pivot;public float[] position,colliderCenter,colliderSize; }
    [Serializable] public sealed class Seat { public string name;public float[] position;public float yaw;public bool lie; }
    [Serializable] public sealed class Facility { public string kind,name;public float[] position,effectPosition; }
    [Serializable] public sealed class Fixture { public float[] position;public float range,intensity; }
    [Serializable] public sealed class Furniture { public string node,kind,label;public float[] position,size;public float height; }
    [Serializable] public sealed class Model { public string id;public int revision;public float worldScale,floorHeight;public Box[] colliders;public Door[] doors;public Furniture[] furniture;public Seat[] seats;public Facility[] facilities;public Fixture[] lights;public string[] missingModels; }
    static Vector3 V(float[] a)
    {
        if(a==null||a.Length!=3||a.Any(x=>float.IsNaN(x)||float.IsInfinity(x)))throw new InvalidOperationException("Błędne współrzędne wyposażenia");
        return new Vector3(a[0],a[1],a[2]);
    }
    static Transform Node(string name,Transform parent,Vector3 position)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;
    }
    static void CloneOriginalParts(GameObject source,Transform destination,string[] prefixes)
    {
        var all=source.GetComponentsInChildren<Transform>(true);
        var selected=prefixes.Select(prefix=>all.FirstOrDefault(t=>
            t.name.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))).ToArray();
        for(int i=0;i<selected.Length;i++)
            if(!selected[i])throw new InvalidOperationException(
                "Brak wymaganej części oryginalnego GLB: "+prefixes[i]);
        // In the actual owner GLB legs/bolts are CHILDREN of CookingTable,
        // stove fittings are CHILDREN of PortableStove_Base, and the faucet
        // is a CHILD of Sink. Copy each source branch exactly once.
        var sourceRoot=Matrix4x4.TRS(Vector3.zero,source.transform.localRotation,
            source.transform.localScale);
        foreach(var original in selected.Where(t=>!selected.Any(other=>
            other!=t && t.IsChildOf(other))))
        {
            var part=Object.Instantiate(original.gameObject,destination,false);
            // Source geometry is independent of the destination's location,
            // rotation and K. Do not cancel the cabin transform here.
            var relative=sourceRoot*source.transform.worldToLocalMatrix*original.localToWorldMatrix;
            part.transform.localPosition=relative.GetColumn(3);
            part.transform.localRotation=relative.rotation;
            part.transform.localScale=relative.lossyScale;
        }
    }
    static void ContactBox(GameObject model,Renderer[] renderers)
    {
        if(model.GetComponentInChildren<Collider>())return;
        var local=BraxObozPlacement.BoundsInFrame(model.transform,renderers);
        var collider=model.AddComponent<BoxCollider>();
        collider.center=local.center;
        collider.size=local.size;
    }
    static void CaptureConstruction(Transform building)
    {
        // Exact snapshot of the built shell and partitions. Original furniture
        // and wall-mounted objects retain their independent placement records.
        var parts=building.GetComponentsInChildren<MeshFilter>(true)
            .Where(f=>f.sharedMesh&&f.GetComponent<Renderer>()&&
                !f.GetComponentsInParent<BraxObozPlacementRecord>(true)
                    .Any(r=>r.Contains(f.GetComponent<Renderer>()))).ToArray();
        BraxObozStructureAuthoring.Capture(building,parts,building.GetComponentsInChildren<Collider>(true));
    }
    static void MeasureDoorJamb(Transform leaf,Renderer[] visible,Transform building)
    {
        var record=BraxObozPlacementRecord.For(leaf);
        record.SetMembers(visible);
        record.suspensionPoint=null;record.suspensionSupport=null;
        if(!BraxObozDoorGeometry.TryHinge(leaf,visible,building,out var hinge,out var problem))
        {
            BraxBuildQuality.Warn("OBÓZ DRZWI: "+leaf.name+" — "+problem);
            return;
        }
        record.suspensionPoint=Node("Zmierzony wierzchołek przy ościeżu",leaf,
            leaf.InverseTransformPoint(hinge.vertexWorld));
        record.suspensionSupport=hinge.support;
        if(hinge.gapWorld>.03f*K)
            BraxBuildQuality.Warn("OBÓZ DRZWI: "+leaf.name+" — "+hinge.status+
                "; szczelina="+(hinge.gapWorld/K).ToString("F4")+" m");
    }
    static Renderer[] SourceBody(GameObject model,string prefix)
    {
        var part=model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>
            t.name.StartsWith(prefix,StringComparison.OrdinalIgnoreCase));
        if(!part)throw new InvalidOperationException("Brak korpusu modelu: "+prefix);
        // glTFast may attach the renderer to the named node or to its direct
        // mesh child. Exclude nested faucet/handles from the basin reference.
        var visible=part.GetComponentsInChildren<Renderer>(true).Where(r=>
            r.transform==part || r.transform.parent==part).ToArray();
        if(visible.Length==0)throw new InvalidOperationException("Brak geometrii korpusu: "+prefix);
        return visible;
    }
    static void SourceSurfaceColliders(Transform surface)
    {
        foreach(var filter in surface.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!filter.sharedMesh || filter.GetComponent<Collider>())continue;
            filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
        }
        Physics.SyncTransforms();
    }
    public static void AddAll(Transform parent)
    {
        var mats=new Dictionary<string,Material>{
            ["wood"]=BraxObozMaterials.Pbr("wood"),["floor"]=BraxObozMaterials.Pbr("floor"),["metal"]=BraxObozMaterials.Pbr("metal"),["towel"]=BraxObozMaterials.Pbr("towel"),
            ["white"]=BraxObozMaterials.Plain("wyposazenie-jasne",new Color(.78f,.77f,.70f),.32f),
            ["burgundy"]=BraxObozMaterials.Plain("bordo",new Color(.478f,.118f,.118f)),
            ["rubber"]=BraxObozMaterials.Plain("guma",new Color(.035f,.035f,.038f),.08f),
            ["canvas"]=BraxObozMaterials.Plain("plotno-robocze",new Color(.356f,.372f,.227f),.12f)};
        var glass=BraxObozMaterials.Plain("szklo-budynkow",new Color(.25f,.4f,.45f,.18f),.8f);
        glass.SetFloat("_Mode",3);glass.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.One);glass.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=3000;mats["glass"]=glass;
        var lamp=BraxObozMaterials.Plain("zarowki",new Color(1,.7f,.32f));lamp.EnableKeyword("_EMISSION");lamp.SetColor("_EmissionColor",new Color(1,.7f,.32f)*2);mats["lamp"]=lamp;
        foreach(var item in new[]{("canteen","Stołówka",-40f,-70f),("sauna","Sauna",-47f,-85f),("medical","Punkt medyczny",-25f,-85f),("storage","Magazyn",-25f,-67f)})
        {
            var at=new Vector3(item.Item3,0,item.Item4);
            OptionalBuilding(item.Item2,parent,()=>Add(item.Item1,item.Item2,parent,at,mats));
        }
        // Błąd jednej budowli nie usuwa istniejących ani pozostałych.
        OptionalBuilding("Murowane WC/ŁAŹNIA/UMYWALNIA",parent,
            ()=>SanitaryBlock(parent,mats));
    }
    static void OptionalBuilding(string label,Transform parent,Action action)
    {
        var before=Enumerable.Range(0,parent.childCount)
            .Select(i=>parent.GetChild(i)).ToArray();
        try { action(); }
        catch(Exception ex)
        {
            for(int i=parent.childCount-1;i>=0;i--)
            {
                var child=parent.GetChild(i);
                if(!before.Contains(child))Object.DestroyImmediate(child.gameObject);
            }
            BraxBuildQuality.Warn("OBÓZ NIEZBUDOWANE "+label+": "+ex.ToString());
        }
    }
    static void Add(string id,string label,Transform parent,Vector3 position,Dictionary<string,Material> mats)
    {
        var data=JsonUtility.FromJson<Model>(File.ReadAllText(Root+id+".json"));
        if(data.id!=id||data.revision!=2||Mathf.Abs(data.worldScale-K)>.00001f||
           Mathf.Abs(data.floorHeight-.16f)>.00001f||data.doors.Length!=1)
            throw new InvalidOperationException("Nieprawidłowa zabudowa usług — wymagany generator revision=2: "+id);
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+id+".glb");if(!asset)throw new InvalidOperationException("Brak modelu "+id);
        var group=Node(label,parent,position*K);group.localScale=Vector3.one*K;
        var model=(GameObject)PrefabUtility.InstantiatePrefab(asset);model.transform.SetParent(group,false);
        foreach(var renderer in model.GetComponentsInChildren<Renderer>())
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{
                string key=m.name.Replace("OBOZ_","");int suffix=key.IndexOf(" (",StringComparison.Ordinal);if(suffix>=0)key=key.Substring(0,suffix);
                if(!mats.TryGetValue(key,out var material))throw new InvalidOperationException("Nieznany materiał "+m.name);return material;
            }).ToArray();
        float width=id=="canteen"?12:id=="sauna"?7:8;
        float depth=id=="canteen"?9:6;
        BraxObozEnvironment.ReplaceServiceRoof(group,model.transform,width,depth);
        foreach(var box in data.colliders)
        {
            var t=Node("Kolizja zgodna z otworami",group,V(box.position));t.localRotation=Quaternion.Euler(0,box.yaw,0);t.gameObject.AddComponent<BoxCollider>().size=V(box.size);
        }
        foreach(var d in data.doors)
        {
            var pivot=model.GetComponentsInChildren<Transform>().Single(t=>t.name==d.pivot);
            var controller=Node("Drzwi — "+label,group,V(d.position)).gameObject.AddComponent<BraxCityDoor>();
            controller.id="oboz-"+id;controller.label=label;controller.leaves=new[]{pivot};controller.sides=new[]{1f};
            var collider=pivot.gameObject.AddComponent<BoxCollider>();collider.center=V(d.colliderCenter);collider.size=V(d.colliderSize);
            var leaf=pivot.GetComponentsInChildren<Renderer>(true);
            BraxObozPlacement.VerifyDimension(leaf,.90f,BraxObozPlacement.Axis.X,
                "drzwi "+label+" szerokość",.01f,group);
            BraxObozPlacement.VerifyDimension(leaf,2.10f,BraxObozPlacement.Axis.Y,
                "drzwi "+label+" wysokość",.01f,group);
            var record=BraxObozPlacementRecord.For(pivot);
            record.SetMembers(leaf);
            record.label="drzwi "+label+" — skrzydło na zawiasie";
            record.support=BraxObozPlacementRecord.SupportKind.Suspended;
            record.expectedDimensionMetres=2.10f;
            record.dimensionAxis=(int)BraxObozPlacement.Axis.Y;
            record.dimensionFrame=group;
            MeasureDoorJamb(pivot,leaf,group);
        }
        // Furniture is a separate original board assembly in revision 2.
        // Calibrate and place it before adding its own collider or any dishes.
        foreach(var item in data.furniture ?? Array.Empty<Furniture>())
        {
            var part=model.GetComponentsInChildren<Transform>(true).Single(t=>t.name==item.node);
            var visible=part.GetComponentsInChildren<Renderer>(true);
            BraxObozPlacement.RealDimension(part,visible,item.height,
                BraxObozPlacement.Axis.Y,item.label,group);
            BraxObozPlacement.OnGround(part,visible,group.TransformPoint(V(item.position)),item.label);
            ContactBox(part.gameObject,visible);
        }
        CaptureConstruction(group);
        foreach(var s in data.seats)
        {
            var t=Node(s.name,group,V(s.position));t.localRotation=Quaternion.Euler(0,s.yaw,0);
            var seat=t.gameObject.AddComponent<BraxSeat>();seat.label=s.name;seat.lie=s.lie;seat.surfaceY=t.position.y;seat.hipsAboveRoot=.95f*K;
        }
        foreach(var f in data.facilities)
        {
            // Meal and storage markers reserve the supplied layout. They are
            // not exposed as working actions until their behavior is supplied.
            if(!Enum.TryParse(f.kind,out BraxObozFacility.Kind kind))continue;
            var t=Node(f.name,group,V(f.position));var facility=t.gameObject.AddComponent<BraxObozFacility>();facility.kind=kind;
            if(kind!=BraxObozFacility.Kind.Drink)facility.effect=Effect(group,V(f.effectPosition),kind==BraxObozFacility.Kind.Sauna);
        }
        foreach(var f in data.lights ?? Array.Empty<Fixture>())
        {
            var t=Node("Lampa wnętrza",group,V(f.position));var light=t.gameObject.AddComponent<Light>();light.type=LightType.Point;
            light.color=new Color(1,.82f,.60f);light.intensity=f.intensity;light.range=f.range*K;light.shadows=LightShadows.None;
            t.gameObject.AddComponent<BraxObozLocalLight>().source=light;
        }
        foreach(var missing in data.missingModels ?? Array.Empty<string>())
            BraxBuildQuality.Warn("OBÓZ BRAK MODELU — H: "+missing+
                "; budynek="+label+"; wymagany model i akceptacja właściciela; bez zastępstwa");
        if(id=="canteen")FurnishCanteen(group);
        if(id=="medical")FurnishMedical(group);
        // The owner also delivered a real 4K Blender drawer cabinet.
        // Its restored GLB is kept separate from the military cabin lockers:
        // one in the medical office, another in the storage building.
        if(id=="medical"||id=="storage")
        {
            OptionalBuilding("oryginalna szafka "+label,group,()=>{
                OwnerFixture(group,"drawer_cabinet_4k/drawer_cabinet_4k.glb",
                    new Vector3(id=="medical"?2.4f:0f,.16f,2.5f),new Vector3(1.18f,1.90f,.50f));
                Debug.Log("BRAXHORN_OWNER_DRAWER_CABINET_4K room="+id+
                    " sourceBlendOriginal=true visualValidationPending=true");
            });
        }
    }
    static void FurnishMedical(Transform room)
    {
        foreach(float x in new[]{-2.4f,2.4f})
            OptionalBuilding("oryginalne łóżko punktu medycznego "+x,room,()=>{
                var bed=OwnerSingleBed(room,new Vector3(x,.16f,.7f));
                bed.name="ORYGINAŁ IKEA IDANAS — punkt medyczny "+x;
                var bounds=BraxObozPlacement.Bounds(bed.GetComponentsInChildren<Renderer>(true));
                // Measure the original visible mattress/blanket at the bed's
                // centre. A headboard's maximum Y is not a lying surface.
                var temporary=new List<MeshCollider>();float? surface=null;
                try
                {
                    var colliders=new List<MeshCollider>();
                    foreach(var filter in bed.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if(!filter.sharedMesh)continue;
                        var collider=filter.GetComponent<MeshCollider>();
                        if(!collider){collider=filter.gameObject.AddComponent<MeshCollider>();
                            collider.sharedMesh=filter.sharedMesh;temporary.Add(collider);}
                        colliders.Add(collider);
                    }
                    Physics.SyncTransforms();
                    var ray=new Ray(new Vector3(bounds.center.x,bounds.max.y+.2f*K,bounds.center.z),Vector3.down);
                    foreach(var collider in colliders)
                        if(collider.Raycast(ray,out var hit,bounds.size.y+.5f*K) && hit.normal.y>.7f &&
                           (!surface.HasValue || hit.point.y>surface.Value))surface=hit.point.y;
                }
                finally {foreach(var collider in temporary)Object.DestroyImmediate(collider);}
                if(!surface.HasValue)
                {
                    BraxBuildQuality.Warn("OBÓZ PUNKT MEDYCZNY: brak pomiaru powierzchni oryginalnego łóżka; bez fikcyjnego miejsca leżenia");return;
                }
                var seatPoint=new Vector3(bounds.center.x,surface.Value,bounds.center.z);
                var seat=Node("LEŻANKA — oryginalna powierzchnia IDANAS",room,room.InverseTransformPoint(seatPoint))
                    .gameObject.AddComponent<BraxSeat>();
                seat.label="LEŻANKA";seat.lie=true;seat.surfaceY=surface.Value;seat.hipsAboveRoot=.95f*K;
            });
        OptionalBuilding("oryginalna metalowa szafa punktu medycznego",room,()=>
            OwnerFixture(room,"metal_cabinet_low_poly.glb",new Vector3(0,.16f,2.5f),new Vector3(1.6f,2f,.65f)));
    }

    // Called after the cabin's static decoration has been combined. Only the
    // owner's hanging cloth is copied; no rail, quad or generated towel mesh.
    public static void OwnerPorchTowel(Transform cabin,Vector3 linePointSourceMetres,Collider ropeCollider)
    {
        OptionalBuilding("oryginalny ręcznik na lince ganku",cabin,()=>{
            GameObject source=null,referenceRail=null;
            try
            {
                source=OwnerPrototype("towel-rail-with-towel.glb");
                var cloth=OwnerAssembly(source,cabin,"Oryginalny ręcznik — lewa linka ganku",
                    new[]{"Bath towel"},90);
                // The source rail is symmetric about its bar Y. Measure the
                // source frame with the same import transforms as the cloth.
                referenceRail=OwnerAssembly(source,cabin,"Pomiar oryginalnej osi ręcznika",
                    new[]{"Towel rail"},90);
                var rail=BraxObozPlacement.BoundsInFrame(cloth.transform,
                    referenceRail.GetComponentsInChildren<Renderer>(true));
                // Delivered bar's front radius is 0.012803 source metres.
                var foldAxis=new Vector3(rail.center.x,rail.center.y,rail.max.z-.012803f);
                float units=K/Mathf.Max(.0001f,Mathf.Abs(cabin.lossyScale.y));
                var target=cabin.TransformPoint(linePointSourceMetres*units);
                cloth.transform.position+=target-cloth.transform.TransformPoint(foldAxis);
                RecordNativeDimension(cloth,.47f,BraxObozPlacement.Axis.Z,cabin,"oryginalny ręcznik na lince");
                var record=BraxObozPlacementRecord.For(cloth.transform);
                record.support=BraxObozPlacementRecord.SupportKind.Suspended;
                record.suspensionSupport=ropeCollider;
                // Record a real vertex on the original cloth fold, rather
                // than reporting its pivot or the empty rail centre as cloth.
                Vector3? foldVertex=null;float closest=float.PositiveInfinity;
                foreach(var filter in cloth.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(!filter.sharedMesh || !filter.sharedMesh.isReadable)continue;
                    foreach(var vertex in filter.sharedMesh.vertices)
                    {
                        var local=cloth.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                        if(local.y<foldAxis.y)continue;
                        float distance=(local-foldAxis).sqrMagnitude;
                        if(distance<closest){closest=distance;foldVertex=local;}
                    }
                }
                if(foldVertex.HasValue)
                    record.suspensionPoint=Node("Pomiar prawdziwego fałdu ręcznika",cloth.transform,foldVertex.Value);
                else BraxBuildQuality.Warn("OBÓZ RĘCZNIK: nie zmierzono fałdu źródłowej siatki; punkt zawieszenia niezweryfikowany");
                Debug.Log("BRAXHORN_OBOZ_PORCH_TOWEL originalNode=Bath towel source=towel-rail-with-towel.glb"+
                    " railNotCopied=true foldAxisWorld="+target+" sourceWidth=.47 visualReview=pending");
            }
            finally
            {
                if(referenceRail)Object.DestroyImmediate(referenceRail);
                if(source)Object.DestroyImmediate(source);
            }
        });
    }
    static GameObject OwnerPrototype(string filename)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(OwnerRoot+filename);
        if(!asset)throw new InvalidOperationException("Brak oryginalnego modelu właściciela: "+filename);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset);
        instance.SetActive(false);
        return instance;
    }

    static GameObject OwnerAssembly(GameObject source,Transform parent,string label,
        string[] prefixes,float yaw=0)
    {
        var item=Node(label,parent,Vector3.zero).gameObject;
        CloneOriginalParts(source,item.transform,prefixes);
        item.transform.localRotation=Quaternion.Euler(0,yaw,0);
        BraxObozPlacement.NativeMetres(item.transform,item.GetComponentsInChildren<Renderer>(true),label);
        return item;
    }

    static Transform SourcePart(GameObject item,string prefix)
    {
        var part=item.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>
            t.name.StartsWith(prefix,StringComparison.OrdinalIgnoreCase));
        if(!part)throw new InvalidOperationException("Brak oryginalnej części: "+prefix);
        return part;
    }

    static void RecordNativeDimension(GameObject item,float metres,BraxObozPlacement.Axis axis,
        Transform frame,string label)
    {
        // Measurements of the delivered meshes, not a request to resize them.
        var record=BraxObozPlacementRecord.For(item.transform);
        record.SetMembers(item.GetComponentsInChildren<Renderer>(true));
        record.expectedDimensionMetres=metres;record.dimensionAxis=(int)axis;
        record.dimensionFrame=frame;record.label=label;
    }

    // A grate and a tray are not solid boxes. Sample the real supporting
    // surface under the object's base; the highest contact carries the item.
    // Fixed grid samples alone miss the original grate's 2 mm top ridges, so
    // include centroids of real upward triangles inside the contact footprint.
    internal static bool PlaceOnOwnerSurface(GameObject item,Transform support,
        Vector3 bottomWorld,string label,Renderer[] baseRenderers=null)
    {
        var visible=item.GetComponentsInChildren<Renderer>(true);
        BraxObozPlacement.AtBottom(item.transform,visible,bottomWorld);
        SourceSurfaceColliders(support);
        var full=BraxObozPlacement.Bounds(visible);
        var contact=BraxObozPlacement.Bounds(baseRenderers ?? visible);
        var samples=new[]{Vector2.zero,new Vector2(-.9f,0),new Vector2(.9f,0),
            new Vector2(0,-.9f),new Vector2(0,.9f),new Vector2(-.63f,-.63f),
            new Vector2(-.63f,.63f),new Vector2(.63f,-.63f),new Vector2(.63f,.63f)};
        var points=samples.Select(sample=>new Vector2(contact.center.x+sample.x*contact.extents.x,
            contact.center.z+sample.y*contact.extents.z)).ToList();
        foreach(var filter in support.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh=filter.sharedMesh;if(!mesh || !mesh.isReadable)continue;
            var vertices=mesh.vertices;var indices=mesh.triangles;var matrix=filter.transform.localToWorldMatrix;
            for(int i=0;i<indices.Length;i+=3)
            {
                var a=matrix.MultiplyPoint3x4(vertices[indices[i]]);
                var b=matrix.MultiplyPoint3x4(vertices[indices[i+1]]);
                var c=matrix.MultiplyPoint3x4(vertices[indices[i+2]]);
                if(Vector3.Cross(b-a,c-a).normalized.y<=.7f)continue;
                var point=(a+b+c)/3f;
                float x=(point.x-contact.center.x)/Mathf.Max(.0001f,contact.extents.x);
                float z=(point.z-contact.center.z)/Mathf.Max(.0001f,contact.extents.z);
                if(x*x+z*z<=1f)points.Add(new Vector2(point.x,point.z));
            }
        }
        RaycastHit? highest=null;
        foreach(var point in points)
        {
            var origin=new Vector3(point.x,full.max.y+.35f*K,point.y);
            foreach(var hit in Physics.RaycastAll(origin,Vector3.down,full.size.y+2f*K,
                ~0,QueryTriggerInteraction.Ignore))
                if(hit.collider && hit.collider.transform.IsChildOf(support) &&
                   !hit.collider.transform.IsChildOf(item.transform) && hit.normal.y>.7f &&
                   (!highest.HasValue || hit.point.y>highest.Value.point.y))highest=hit;
        }
        var record=BraxObozPlacementRecord.For(item.transform);
        record.SetMembers(visible);
        record.support=BraxObozPlacementRecord.SupportKind.Ground;record.label=label;
        if(!highest.HasValue)
        {
            BraxBuildQuality.Warn("OBÓZ BRAK PODPARCIA — "+label+"; powierzchnia="+support.name);
            return false;
        }
        record.verticalSupport=highest.Value.collider;
        item.transform.position+=Vector3.up*(highest.Value.point.y-full.min.y);
        record.contactPoint=Node("Zmierzony punkt styku z oryginalną powierzchnią",item.transform,
            item.transform.InverseTransformPoint(highest.Value.point));
        Physics.SyncTransforms();
        return true;
    }

    static bool PlaceOrRemove(GameObject item,Transform support,Vector3 bottomWorld,
        string label,Renderer[] baseRenderers=null)
    {
        if(PlaceOnOwnerSurface(item,support,bottomWorld,label,baseRenderers))return true;
        Object.DestroyImmediate(item);return false;
    }

    internal static void UseOriginalContactSurface(GameObject item)
    {
        // A box over a concave tray spans empty air above its actual floor.
        foreach(var proxy in item.GetComponentsInChildren<BoxCollider>(true))Object.DestroyImmediate(proxy);
        SourceSurfaceColliders(item.transform);
    }

    static void CheckInsideCanteen(Transform room,GameObject item)
    {
        var b=BraxObozPlacement.BoundsInFrame(room,item.GetComponentsInChildren<Renderer>(true));
        if(b.min.x< -5.94f-.005f || b.max.x>5.94f+.005f ||
           b.min.z< -4.44f-.005f || b.max.z>4.44f+.005f)
            throw new InvalidOperationException("Oryginalny model poza ścianami stołówki: "+
                item.name+" localBounds="+b);
    }

    // Four tables and eight benches already come from the rebuilt board
    // building. Never add another set over their meshes/colliders/seats.
    static void FurnishCanteen(Transform room)
    {
        OptionalBuilding("oryginalna kuchnia stołówki",room,()=>OwnerCanteenKitchen(room));
        OptionalBuilding("oryginalna witryna stołówki",room,()=>{
            var display=OwnerFixture(room,"glass-food-display-showcase-with-trays.glb",
                new Vector3(3.35f,.16f,3.85f),new Vector3(1.502f,1.3185f,.869f));
            RecordNativeDimension(display,1.502f,BraxObozPlacement.Axis.X,room,"witryna właściciela");
            CheckInsideCanteen(room,display);
        });
        var tables=room.GetComponentsInChildren<Transform>(true)
            .Where(t=>t.name.StartsWith("Table_",StringComparison.Ordinal) &&
                t.GetComponent<BraxObozPlacementRecord>()!=null)
            .OrderBy(t=>t.name,StringComparer.Ordinal).ToArray();
        if(tables.Length!=4)
            BraxBuildQuality.Warn("OBÓZ STOŁÓWKA: stoły z desek="+tables.Length+"; oczekiwano 4; bez duplikowania");
        int set=0,completeSets=0;
        for(int i=0;i<tables.Length && set<6;i++)
        {
            var table=tables[i];var b=BraxObozPlacement.BoundsInFrame(room,table.GetComponentsInChildren<Renderer>(true));
            var offsets=i<2?new[]{-.9f,.9f}:new[]{0f};
            foreach(float offset in offsets)
            {
                int sourceSet=++set;
                var at=new Vector3(b.center.x+offset,b.max.y,b.center.z);
                OptionalBuilding("oryginalna zastawa "+sourceSet,room,
                    ()=>{if(DiningPlaceSetting(room,table,at,sourceSet))completeSets++;});
            }
        }
        Debug.Log("BRAXHORN_OBOZ_CANTEEN: generatedPlankTables="+tables.Length+
            " attemptedSets="+set+" completeOwnerCutlerySets="+completeSets+
            " tableHeightAboveFloor=.75 benchHeightAboveFloor=.45"+
            " originalKitchen=true originalPlates=true originalTrays=true visualReview=pending");
    }

    static bool DiningPlaceSetting(Transform room,Transform table,Vector3 centre,int sourceSet)
    {
        GameObject kitchen=null,cutlery=null;
        try
        {
            kitchen=OwnerPrototype(KitchenFile);cutlery=OwnerPrototype("24-piece-cutlery-set.glb");
            // 0.55 m is the explicit camp design dimension; the author's GLB
            // is 2.2 source units wide. This is not a three-axis box fitting.
            var tray=OwnerFixture(room,"food_tray.glb",centre+new Vector3(.13f,0,0),
                new Vector3(.55f,.021875f,.425f));
            UseOriginalContactSurface(tray);
            if(!PlaceOrRemove(tray,table,room.TransformPoint(centre+new Vector3(.13f,0,0)),"taca na stole"))return false;
            SourceSurfaceColliders(tray.transform);
            var plate=OwnerAssembly(kitchen,room,"Talerz właściciela — komplet "+sourceSet,
                new[]{"Plate_Regular.001_"});
            RecordNativeDimension(plate,.21617f,BraxObozPlacement.Axis.X,room,"talerz właściciela");
            bool complete=PlaceOrRemove(plate,table,room.TransformPoint(centre+new Vector3(-.27f,0,0)),"talerz na stole");
            var trayBounds=BraxObozPlacement.BoundsInFrame(room,tray.GetComponentsInChildren<Renderer>(true));
            string[] kinds={"Dinner fork ","Dinner knife ","Dinner spoon ","Teaspoon "};
            float[] lengths={.19996f,.23545f,.19988f,.14991f};
            for(int i=0;i<kinds.Length;i++)
            {
                var utensil=OwnerAssembly(cutlery,room,"Sztućce właściciela "+sourceSet+" / "+kinds[i],
                    new[]{kinds[i]+sourceSet+","});
                RecordNativeDimension(utensil,lengths[i],BraxObozPlacement.Axis.Z,room,kinds[i]+sourceSet);
                var at=new Vector3(trayBounds.center.x+(i-1.5f)*.075f,
                    trayBounds.max.y,trayBounds.center.z);
                if(!PlaceOrRemove(utensil,tray.transform,room.TransformPoint(at),"sztućce na dnie tacy"))complete=false;
            }
            CheckInsideCanteen(room,tray);
            return complete;
        }
        finally
        {
            if(kitchen)Object.DestroyImmediate(kitchen);
            if(cutlery)Object.DestroyImmediate(cutlery);
        }
    }

    static void OwnerCanteenKitchen(Transform room)
    {
        var source=OwnerPrototype(KitchenFile);
        try
        {
            var group=Node("Oryginalna kuchnia właściciela — zachodnia ściana",room,Vector3.zero);
            // Source back -X becomes +X in glTFast. Rotate to put it on the
            // west wall; controls and sink fronts then face the room (+X).
            var counter=OwnerAssembly(source,group,"Kuchenka z oryginalnym blatem — 0,90 m",
                new[]{"CookingTable.001_","CookingTable_Shelf.001_","PortableStove_Base.001_"},180);
            var counterRenderers=counter.GetComponentsInChildren<Renderer>(true);
            BraxObozPlacement.RealDimension(counter.transform,counterRenderers,.9f,
                BraxObozPlacement.Axis.Y,"kuchenka stołówki",room);
            BraxObozPlacement.OnGround(counter.transform,counterRenderers,
                room.TransformPoint(new Vector3(-5.55f,.16f,2.25f)),"kuchenka stołówki na podłodze");
            BraxObozPlacement.ToWall(counter.transform,counterRenderers,
                BraxObozPlacement.Bounds(counterRenderers).center,-room.right,"kuchenka stołówki przy ścianie");
            ContactBox(counter,counterRenderers);CheckInsideCanteen(room,counter);

            var sink=OwnerAssembly(source,group,"Zlew stołówki z oryginalną baterią — 0,85 m",
                new[]{"Sink.001_"},180);
            var sinkRenderers=sink.GetComponentsInChildren<Renderer>(true);
            BraxObozPlacement.StandingSurfaceHeight(sink.transform,sinkRenderers,
                SourceBody(sink,"Sink.001_"),.85f,"zlew stołówki");
            BraxObozPlacement.OnGround(sink.transform,sinkRenderers,
                room.TransformPoint(new Vector3(-5.55f,.16f,3.45f)),"zlew stołówki na podłodze");
            BraxObozPlacement.ToWall(sink.transform,sinkRenderers,
                BraxObozPlacement.Bounds(sinkRenderers).center,-room.right,"zlew stołówki przy ścianie");
            ContactBox(sink,sinkRenderers);CheckInsideCanteen(room,sink);

            var rack=OwnerAssembly(source,group,"Oryginalna półka, garnki i zapasy kuchni",new[]{
                "PotRack.001_","Kitchenware_Pot.001_","Kitchenware_Pan_IronCast.001_",
                "CannedFood_Flat.001_","CannedFood_Flat.002_","CannedFood_Flat.003_","CannedFood_Flat.004_",
                "CannedFood_Tall.001_","CannedFood_Tall.002_","CannedFood_Tall.003_","CannedFood_Tall.004_","CannedFood_Tall.005_",
                "Food_CardboardPackage_Thick.001_","Package_Smooth.001_","Package_Smooth.002_",
                "TinCan.001_","GlassJar.001_","FlowerBag.001_"},180);
            var rackRenderers=rack.GetComponentsInChildren<Renderer>(true);
            BraxObozPlacement.AtHeightAboveGround(rack.transform,rackRenderers,
                room.TransformPoint(new Vector3(-5.55f,.16f,2.25f)),1.20f,
                BraxObozPlacement.HeightReference.Bottom,"półka i wiszące garnki stołówki");
            BraxObozPlacement.ToWall(rack.transform,rackRenderers,
                BraxObozPlacement.Bounds(rackRenderers).center,-room.right,"półka stołówki na ścianie");
            CheckInsideCanteen(room,rack);

            var cylinder=OwnerAssembly(source,group,"Oryginalna butla kuchenki stołówki",
                new[]{"GasCylinder.001_"});
            var cylinderRenderers=cylinder.GetComponentsInChildren<Renderer>(true);
            BraxObozPlacement.OnGround(cylinder.transform,cylinderRenderers,
                room.TransformPoint(new Vector3(-5.55f,.16f,1.55f)),"butla stołówki na podłodze");
            ContactBox(cylinder,cylinderRenderers);CheckInsideCanteen(room,cylinder);

            var grate=SourcePart(counter,"PortableStove_Grate_L_LP.001_");
            var grateBounds=BraxObozPlacement.Bounds(grate.GetComponentsInChildren<Renderer>(true));
            var pot=OwnerFixture(group,"stockpot.glb",group.InverseTransformPoint(grateBounds.center),
                new Vector3(.438f,.2805f,.29f));
            RecordNativeDimension(pot,.438f,BraxObozPlacement.Axis.X,room,"garnek właściciela");
            PlaceOrRemove(pot,grate,new Vector3(grateBounds.center.x,grateBounds.max.y,grateBounds.center.z),
                "garnek na rzeczywistym ruszcie",SourceBody(pot,"Layered pan base"));
        }
        finally { Object.DestroyImmediate(source); }
    }

    // Rzeczywisty budynek WC: dwie kabiny, dwa stanowiska prysznicowe,
    // umywalki, lustra, wentylacja i dostępne dla gracza fizyczne wejście.
    // Wszystkie wymiary lokalne są w metrach; K stosuje wyłącznie rodzic.
    static GameObject FixtureBox(Transform parent,string name,Vector3 position,Vector3 size,
        Material material,bool solid=true)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name=name;g.transform.SetParent(parent,false);
        g.transform.localPosition=position;g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=material;
        if(!solid)Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }
    // PBR tiles use measured metre UV on EVERY face, not one stretched 2K
    // image over an entire 12-metre bathroom wall. Source mesh is retained.
    static GameObject PbrWall(Transform parent,string label,Vector3 pos,
        Vector3 dimensions,Material source,bool solid=true)
    {
        var wall=FixtureBox(parent,label,pos,dimensions,source,solid);
        var filter=wall.GetComponent<MeshFilter>();
        var mesh=Object.Instantiate(filter.sharedMesh);
        var verts=mesh.vertices;var normals=mesh.normals;
        var uv=new Vector2[verts.Length];
        for(int i=0;i<verts.Length;i++)
        {
            Vector3 metre=Vector3.Scale(verts[i],dimensions);
            Vector3 n=normals[i];
            uv[i]=Mathf.Abs(n.y)>.90f?new Vector2(metre.x,metre.z):
                Mathf.Abs(n.x)>.90f?new Vector2(metre.z,metre.y):
                new Vector2(metre.x,metre.y);
        }
        mesh.uv=uv;mesh.RecalculateTangents();
        filter.sharedMesh=BraxObozStableMesh.Save(mesh);
        return wall;
    }

    static GameObject FixtureCylinder(Transform parent,string name,Vector3 position,
        float radius,float height,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;
        g.transform.localScale=new Vector3(radius*2,height*.5f,radius*2);
        g.GetComponent<Renderer>().sharedMaterial=material;
        return g;
    }
    // True owner's MILITARY/KITCHEN field equipment, extracted directly from
    // kitchen_-_assets.glb. A separate working kitchenette is installed in
    // EACH of the four two-person cabins without placing the whole 48-mesh
    // commercial kitchen inside a 6.2 x 8.2 m room.
    public static void OwnerCabinKitchenette(Transform cabin)
    {
        const string path="Assets/Source/Prywatne/Oboz/OwnerAssets/kitchen_-_assets.glb";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!prefab)throw new InvalidOperationException(
            "Brak oryginalnego właścicielskiego zestawu kuchennego dla domków: "+path);
        var source=PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if(!source)throw new InvalidOperationException("Nie można zaimportować źródłowej kuchni GLB");
        var region=new GameObject("ANEKS POLOWY — oryginalne źródłowe modele kuchni").transform;
        region.SetParent(cabin,false);
        try
        {
            // The source GLB splits ONE piece of equipment into several
            // sibling nodes. Never import the tabletop without legs, the
            // burner base without grates, or a sink without its faucet.
            // The original mesh/UV/material of EVERY sibling remains intact.
            GameObject Assembly(string[] prefixes,string label,Vector3 bottomMetres,
                Vector3 envelopeMetres,bool solid)
            {
                var carrier=new GameObject(label+" — kompletne oryginalne części GLB");
                carrier.transform.SetParent(region,false);
                CloneOriginalParts(source,carrier.transform,prefixes);
                var renderers=carrier.GetComponentsInChildren<Renderer>(true);
                if(renderers.Length<prefixes.Length)
                    throw new InvalidOperationException(
                        "Brak pełnej geometrii w oryginalnym komplecie: "+label);
                Bounds Bounds()
                {
                    Bounds b=renderers[0].bounds;
                    foreach(var rr in renderers.Skip(1))b.Encapsulate(rr.bounds);
                    return b;
                }
                var bounds=Bounds();
                if(bounds.size.x<.0001f||bounds.size.y<.0001f||bounds.size.z<.0001f)
                    throw new InvalidOperationException("Zerowy obrys kompletnego modelu: "+label);
                // Referencyjna wysokosc tylko dla elementow o podanych realnych wymiarach.
                // Wymiary envelope sa wylacznie diagnostyczne, nie steruja skala.
                BraxObozPlacement.NativeMetres(carrier.transform,renderers,label);
                if(label.StartsWith("Zlew polowy",StringComparison.Ordinal))
                    BraxObozPlacement.StandingSurfaceHeight(carrier.transform,renderers,
                        SourceBody(carrier,"Sink.001_"),.85f,label);
                bounds=Bounds();
                var target=cabin.TransformPoint(bottomMetres*K);
                if(bottomMetres.y<=.25f)
                    BraxObozPlacement.OnGround(carrier.transform,renderers,target,label);
                else
                    BraxObozPlacement.AtBottom(carrier.transform,renderers,target);
                bounds=Bounds();
                // Zle proporcje źródła sa ostrzezeniem, nie pretekstem do
                // wyginania modelu lub przerywania całego generowania.
                if(bounds.size.x > envelopeMetres.x*K*1.35f ||
                   bounds.size.z > envelopeMetres.z*K*1.35f)
                    Debug.LogWarning("BRAXHORN_OBOZ_KITCHEN_SOURCE_REVIEW "+label+
                        " dimensions="+Bounds()+" reference-envelope="+envelopeMetres);
                if(solid)ContactBox(carrier,renderers);
                return carrier;
            }
            GameObject Part(string prefix,string label,Vector3 bottom,Vector3 envelope,bool solid)
            {
                return Assembly(new[]{prefix},label,bottom,envelope,solid);
            }

            // The group bounds are measured AFTER assembling the real
            // Sketchfab siblings. Keep the central corridor/entrance open.
            var cookingTable=Assembly(new[]{
                "CookingTable.001_","CookingTable_Bolts1.001_",
                "CookingTable_Bolts2.001_","CookingTable_Legs.001_",
                "CookingTable_Shelf.001_"
            },"Oryginalny stół polowy z nogami i półką",
                new Vector3(-2f,.16f,-.82f),new Vector3(1.45f,.91f,.78f),true);
            var stove=Assembly(new[]{
                "PortableStove_Base.001_","PortableStove_Burners.001_",
                "PortableStove_Grate_L_LP.001_","PortableStove_Grate_R_LP.001_",
                "PortableStove_Tumbler_LP.001_","PortableStove_Wire.001_"
            },"Oryginalna kuchenka polowa z palnikami i rusztami",
                new Vector3(-2f,.16f,-.82f),new Vector3(.65f,.24f,.60f),false);
            var plate=Part("Plate_Regular.001_","Talerz z oryginalnego kompletu",
                new Vector3(-2f,.16f,-.82f),new Vector3(.26f,.07f,.26f),false);
            var tableRenderers=cookingTable.GetComponentsInChildren<Renderer>(true);
            var tableBounds=BraxObozPlacement.BoundsInFrame(cabin,tableRenderers);
            var stoveRenderers=stove.GetComponentsInChildren<Renderer>(true);
            var plateRenderers=plate.GetComponentsInChildren<Renderer>(true);
            // The source table is longer along Z. Its genuine 0.598 m width
            // supports the genuine 0.578 m stove. Preserve their common scale;
            // shrinking only the table to a dining-table height made it too narrow.
            var stoveBounds=BraxObozPlacement.BoundsInFrame(cabin,stoveRenderers);
            var plateBounds=BraxObozPlacement.BoundsInFrame(cabin,plateRenderers);
            var stoveAt=new Vector3(tableBounds.center.x,tableBounds.max.y,
                tableBounds.min.z+stoveBounds.extents.z+.01f*K);
            var plateAt=new Vector3(tableBounds.center.x,tableBounds.max.y,
                tableBounds.max.z-plateBounds.extents.z-.01f*K);
            BraxObozPlacement.OnSupport(stove.transform,stoveRenderers,
                cookingTable.transform,cabin.TransformPoint(stoveAt),"kuchenka na blacie");
            BraxObozPlacement.OnSupport(plate.transform,plateRenderers,
                cookingTable.transform,cabin.TransformPoint(plateAt),"talerz na blacie");
            var counter=Node("Aneks — wysokość gotowania 0,90 m",region,Vector3.zero);
            cookingTable.transform.SetParent(counter,true);
            stove.transform.SetParent(counter,true);
            plate.transform.SetParent(counter,true);
            var counterRenderers=counter.GetComponentsInChildren<Renderer>(true);
            BraxObozPlacement.RealDimension(counter,counterRenderers,.9f,
                BraxObozPlacement.Axis.Y,"kuchenka z oryginalnym blatem",cabin);
            BraxObozPlacement.OnGround(counter,counterRenderers,
                cabin.TransformPoint(new Vector3(-2f,.16f,-.82f)*K),"aneks na podłodze");
            Assembly(new[]{"GasCylinder.001_","GasCylinder_Tap.001_"},
                "Oryginalna butla z zaworem",
                new Vector3(-2.40f,.16f,-1.40f),new Vector3(.30f,.65f,.30f),true);
            var rack=Assembly(new[]{"PotRack.001_","PotRack_Hooks.001_","PotRack_Rails.001_"},
                "Oryginalny stojak z hakami i poręczami",
                new Vector3(-2f,2.08f,-.72f),new Vector3(1.04f,.75f,.85f),false);
            // The original wall rails are on +X after glTFast's X mirroring.
            // Turn the complete rack so that this back faces the cabin's west
            // wall, ABOVE the actual window opening (which ends at y=2.02 m).
            rack.transform.localRotation=Quaternion.Euler(0,180,0)*rack.transform.localRotation;
            var rackRenderers=rack.GetComponentsInChildren<Renderer>(true);
            BraxObozPlacement.AtBottom(rack.transform,rackRenderers,
                cabin.TransformPoint(new Vector3(-2f,2.08f,-.72f)*K));
            BraxObozPlacement.ToWall(rack.transform,rackRenderers,
                BraxObozPlacement.Bounds(rackRenderers).center,-cabin.right,"stojak na garnki na ścianie");
            var originalSink=Assembly(new[]{
                "Sink.001_","Faucet_LP.001_","Faucet_Tap_LP.001_",
                "Sink_Drain_LP.001_","Sink_Legs_LP.001_"
            },"Zlew polowy z oryginalną baterią i odpływem",
                new Vector3(2.10f,.16f,-.78f),new Vector3(.92f,1.12f,.64f),true);
            var sinkRenderers=originalSink.GetComponentsInChildren<Renderer>(true);
            BraxObozPlacement.ToWall(originalSink.transform,sinkRenderers,
                BraxObozPlacement.Bounds(sinkRenderers).center,cabin.right,"zlew polowy przy ścianie");
            var faucet=originalSink.AddComponent<BraxObozFacility>();
            faucet.kind=BraxObozFacility.Kind.Sink;
            faucet.effect=Effect(region,new Vector3(2.1f,1.20f,-.78f)*1.294f,false,true);
            CabinWasteBin(region);
            Debug.Log("BRAXHORN_OBOZ_CABIN_KITCHEN: cabin="+cabin.name+
                " realOwnerCookingTable=true realOwnerStove=true realOwnerSink=true"+
                " cookware=true corridorOpen=true playModeCheckPending=true");
        }
        finally { Object.DestroyImmediate(source); }
    }

    // One open-top metal waste receptacle in each cabin. Unlike a capped
    // cylinder fake, this has real exterior/interior sidewalls, a solid bottom
    // and a visible metal lip. The owner did not supply a separate bin GLB.
    static void CabinWasteBin(Transform region)
    {
        const int sides=24;
        const float K=1.294f;
        float outer=.19f*K,inner=.168f*K,h=.46f*K,baseH=.027f*K;
        var verts=new List<Vector3>(sides*16);
        var triangles=new List<int>(sides*24);
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            int n=verts.Count;
            verts.Add(a);verts.Add(b);verts.Add(c);verts.Add(d);
            triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
            triangles.Add(n);triangles.Add(n+2);triangles.Add(n+3);
        }
        for(int i=0;i<sides;i++)
        {
            float a=2f*Mathf.PI*i/sides,b=2f*Mathf.PI*(i+1)/sides;
            var o0=new Vector3(Mathf.Cos(a)*outer,0,Mathf.Sin(a)*outer);
            var o1=new Vector3(Mathf.Cos(b)*outer,0,Mathf.Sin(b)*outer);
            var i0=new Vector3(Mathf.Cos(a)*inner,baseH,Mathf.Sin(a)*inner);
            var i1=new Vector3(Mathf.Cos(b)*inner,baseH,Mathf.Sin(b)*inner);
            Quad(o0,o1,o1+Vector3.up*h,o0+Vector3.up*h); // outside
            Quad(i1,i0,i0+Vector3.up*(h-baseH),i1+Vector3.up*(h-baseH)); // inside
            Quad(o0+Vector3.up*h,o1+Vector3.up*h,i1+Vector3.up*(h-baseH),i0+Vector3.up*(h-baseH)); // rim
            Quad(Vector3.zero,o1,o0,Vector3.zero); // bottom
        }
        var mesh=new Mesh {
            name="Kosz metalowy z prawdziwym otwartym wnętrzem",
            indexFormat=UnityEngine.Rendering.IndexFormat.UInt32
        };
        mesh.SetVertices(verts);mesh.SetTriangles(triangles,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        var node=Node("Kosz metalowy na odpady — 3D z otwartym wnętrzem",
            region,new Vector3(2.40f,.16f,-1.55f)*K);
        var go=node.gameObject;
        go.AddComponent<MeshFilter>().sharedMesh=BraxObozStableMesh.Save(mesh);
        go.AddComponent<MeshRenderer>().sharedMaterial=BraxObozMaterials.Pbr("metal");
        go.AddComponent<MeshCollider>().sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;
    }

    // The cabins and open-air training stations have unscaled parent roots:
    // their primitive geometry is authored in source metres and multiplied
    // by K inside Box(). OwnerFixture() by contrast expects Unity LOCAL units.
    // Convert just the placement to K here; do NOT rescale original GLB mesh.
    public static GameObject OwnerFixtureSourceMetres(Transform parent,string filename,
        Vector3 bottomCenterMetres,Vector3 maxSourceMetres)
    {
        return OwnerFixture(parent,filename,bottomCenterMetres*K,maxSourceMetres);
    }

    // Model łóżka IKEA IDANAS — dokładnie dostarczony przez właściciela GLB,
    // nie generować nóg, materaca, poduszki ani koca z brył zastępczych.
    // Kabiny mają obrót +/-90°, więc wszystkie wymiary mierzymy w ich
    // lokalnym układzie, a nie względem osi globalnych.
    public static GameObject OwnerSingleBed(Transform cabin,Vector3 bottomCenterSourceMetres)
    {
        const string filename="OwnerExtras/Bedroom/ikea_idanas_single_bed.glb";
        const string sourcePath="Assets/Source/Prywatne/Oboz/OwnerAssets/"+filename;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if(!source)throw new InvalidOperationException(
            "Brak oryginalnego łóżka jednoosobowego IKEA IDANAS — przerwij generowanie obozu: "+sourcePath);
        var model=PrefabUtility.InstantiatePrefab(source) as GameObject;
        if(!model)throw new InvalidOperationException("Nie można instancjonować oryginalnego łóżka: "+sourcePath);
        model.name="Łóżko 3D właściciela — IKEA IDANAS — oryginalny GLB";
        model.transform.SetParent(cabin,false);
        var renderers=model.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0)throw new InvalidOperationException("Łóżko GLB nie ma renderera");

        Bounds LocalBounds() { return BraxObozPlacement.BoundsInFrame(cabin,renderers); }
        var bounds=LocalBounds();
        if(bounds.size.x>.001f&&bounds.size.z>.001f && bounds.size.x>bounds.size.z*1.10f)
        {
            model.transform.localRotation=Quaternion.Euler(0,90,0)*model.transform.localRotation;
            bounds=LocalBounds();
        }
        if(bounds.size.x<.001f||bounds.size.y<.001f||bounds.size.z<.001f)
            throw new InvalidOperationException("Błędny rozmiar źródłowego łóżka IDANAS");
        // Cabin source metres are scaled exactly once by the game's K=1.294.
        // Jedna realna miara: dlugosc 2,0 m przed K, szerokosc 0,9 m do kontroli.
        // Kalibruj wzgledem OSI lozka, a nie najwezszej strony pudelka.
        BraxObozPlacement.RealDimension(model.transform,renderers,2f,
            BraxObozPlacement.Axis.Z,"IKEA IDANAS",cabin);
        bounds=LocalBounds();
        // Nominal bed width refers to the mattress. The supplied blanket
        // overhang makes the full footprint 1.219871 m at this unchanged scale.
        // Keep that full footprint for clearance/collision and report both.
        var mattress=SourceBody(model,"Mesh21_Color_M03_0");
        BraxObozPlacement.VerifyDimension(mattress,.9f,
            BraxObozPlacement.Axis.X,"IKEA IDANAS szerokość materaca",axisFrame:cabin);
        // Cabin roots have scale 1; authored service roots already have K.
        // The public API always accepts metres, so apply K exactly once.
        float placementUnits=K/Mathf.Max(.0001f,Mathf.Abs(cabin.lossyScale.y));
        var centerSource=bottomCenterSourceMetres*placementUnits;
        BraxObozPlacement.OnGround(model.transform,renderers,
            cabin.TransformPoint(centerSource),"IKEA IDANAS");
        bounds=LocalBounds();
        var mattressBounds=BraxObozPlacement.BoundsInFrame(cabin,mattress);
        float parentUnit=Mathf.Abs(cabin.lossyScale.y)/K;
        float mattressTop=(mattressBounds.max.y-bounds.min.y)*parentUnit;
        if(Mathf.Abs(mattressTop-.45f)>.03f)
            BraxBuildQuality.Warn("OBÓZ WYMIAR ŹRÓDŁA — IDANAS: góra materaca="+mattressTop.ToString("F6")+
                " m nad stopami łóżka; tabela=.45 m; jednolita skala długości2m zachowana, bez deformacji");
        // Contact proxy for a single bed only; never block the doorway or
        // make an invisible collider spanning an entire cabin.
        if(!model.GetComponentInChildren<Collider>())
        {
            var collider=model.AddComponent<BoxCollider>();
            var local=BraxObozPlacement.BoundsInFrame(model.transform,renderers);
            var mattressLocal=BraxObozPlacement.BoundsInFrame(model.transform,mattress);
            float bodyHeight=mattressLocal.max.y-local.min.y;
            collider.center=new Vector3(local.center.x,local.min.y+bodyHeight*.5f,local.center.z);
            collider.size=new Vector3(local.size.x,bodyHeight,local.size.z);
        }
        Debug.Log("BRAXHORN_OBOZ_OWNER_BED: "+filename+
            " worldScale=K cabin="+cabin.name+" footprintLocal="+bounds.size+
            " mattressWidthMetres="+(mattressBounds.size.x*parentUnit)+" mattressTopMetres="+mattressTop+
            " visualDeviceReview=pending");
        return model;
    }

    // Prywatne 3D wyposażenie sanitariatów — bez zastępowania siatek, UV i PBR.
    // Jednorodna skala: wyłącznie dopasowanie do stanowiska w murowanym WC.
    public static GameObject OwnerFixture(Transform parent,string filename,
        Vector3 bottomCenter,Vector3 maxLocalSize)
    {
        const string prefix="Assets/Source/Prywatne/Oboz/OwnerAssets/";
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(prefix+filename);
        if(!source)throw new InvalidOperationException("Brak właścicielskiego modelu sanitariatu: "+filename);
        var model=PrefabUtility.InstantiatePrefab(source) as GameObject;
        if(!model)throw new InvalidOperationException("Błąd importu GLB sanitariatu: "+filename);
        model.name="Właściciel / oryginalny "+filename;
        model.transform.SetParent(parent,false);
        // Verified original source backs face -Z (basin's rear tap deck and
        // urinal inlet). Face those backs toward their actual bathroom walls.
        if(filename=="wall-urinal.glb")model.transform.localRotation=Quaternion.Euler(0,90,0)*model.transform.localRotation;
        if(filename=="trough-washbasin.glb")model.transform.localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation;
        // The supplied tractor tire is authored upright, its axle along X.
        // Lay the original tire on its side for the flipping station; keep
        // its real two-metre diameter and all source geometry/materials.
        if(filename=="OwnerExtras/tractor_tire.glb")
            model.transform.localRotation=Quaternion.Euler(0,0,90)*model.transform.localRotation;
        var renderers=model.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0)throw new InvalidOperationException("Brak renderera "+filename);
        Bounds bounds=renderers[0].bounds;
        foreach(var rr in renderers.Skip(1))bounds.Encapsulate(rr.bounds);
        if(bounds.size.x<=.0001f||bounds.size.y<=.0001f||bounds.size.z<=.0001f)
            throw new InvalidOperationException("Zerowe wymiary modelu: "+filename);
        // Parametr maxLocalSize jest tylko historyczna ramka diagnostyczna.
        // Nie redukujemy siatki do tej ramki. Realne dane: szafa 2m, lawa 0,45m.
        if(filename=="metal_cabinet_low_poly.glb")
            BraxObozPlacement.RealDimension(model.transform,renderers,2f,
                BraxObozPlacement.Axis.Y,filename);
        else if(filename=="picnic_set_1.glb")
            BraxObozPlacement.RealDimension(model.transform,renderers,.45f,
                BraxObozPlacement.Axis.Y,filename);
        else if(filename=="food_tray.glb")
            // Camp design width, not a claim that the author's source is metres.
            BraxObozPlacement.RealDimension(model.transform,renderers,.55f,
                BraxObozPlacement.Axis.X,"taca — docelowa szerokość obozu 0,55 m");
        else
            BraxObozPlacement.NativeMetres(model.transform,renderers,filename);
        bounds=BraxObozPlacement.Bounds(renderers);
        var target=parent.TransformPoint(bottomCenter);
        if(filename=="wall-urinal.glb" || filename=="trough-washbasin.glb" || filename=="towel-rail-with-towel.glb")
        {
            if(filename=="wall-urinal.glb")
                BraxObozPlacement.AtHeightAboveGround(model.transform,renderers,
                    parent.TransformPoint(new Vector3(bottomCenter.x,.24f,bottomCenter.z)),.55f,
                    BraxObozPlacement.HeightReference.Bottom,filename);
            else if(filename=="trough-washbasin.glb")
                BraxObozPlacement.AtHeightAboveGround(model.transform,renderers,
                    parent.TransformPoint(new Vector3(bottomCenter.x,.24f,bottomCenter.z)),.85f,
                    BraxObozPlacement.HeightReference.Top,filename,SourceBody(model,"Trough | shared stone basin"));
            else BraxObozPlacement.AtBottom(model.transform,renderers,target);
            Vector3 toward=filename=="wall-urinal.glb"?-parent.right:parent.forward;
            BraxObozPlacement.ToWall(model.transform,renderers,
                BraxObozPlacement.Bounds(renderers).center,toward,filename);
        }
        else if(bottomCenter.y<=.30f*K || (parent.lossyScale.y > 1.10f && bottomCenter.y<=.30f))
            BraxObozPlacement.OnGround(model.transform,renderers,target,filename);
        else
            BraxObozPlacement.AtBottom(model.transform,renderers,target);
        bounds=BraxObozPlacement.Bounds(renderers);
        // Weryfikujemy oryginalna proporcje, nie zmieniamy jej.
        if(bounds.size.x>maxLocalSize.x*K*1.4f || bounds.size.z>maxLocalSize.z*K*1.4f)
            Debug.LogWarning("BRAXHORN_OBOZ_FIXTURE_DIMENSION_REVIEW "+filename+
                " rzeczywisty obrys="+bounds.size+" zalozony limit="+maxLocalSize);
        // Korpus jest nieruchomy: zachowujemy pojedynczy fizyczny hitbox
        // odpowiadający siatce; nie wstawiamy kolizji przez drzwi.
        ContactBox(model,renderers);
        return model;
    }

    static void SanitaryBlock(Transform parent,Dictionary<string,Material> mats)
    {
        const string label="BRAXHORN — WC / ŁAŹNIA / UMYWALNIA";
        var building=Node(label,parent,new Vector3(-66,0,-52)*K);
        building.localScale=Vector3.one*K;
        // Real CC0 Poly Haven surfaces: white ceramic indoors, dark concrete
        // cladding outside. Source textures must pass 2K MD5 verification.
        var tile=BraxObozMaterials.PublicPbr("long_white_tiles",1.3f);
        // Autentyczna cegła Poly Haven CC0 2K, poziome spoiny po obu osiach.
        var facade=BraxObozMaterials.PublicPbr("red_brick_03",1f);
        var porcelain=BraxObozMaterials.Plain("sanitariaty-porcelana",new Color(.94f,.95f,.92f),.70f);
        var dark=BraxObozMaterials.Plain("sanitariaty-spoiny",new Color(.15f,.18f,.20f),.22f);
        var mirror=BraxObozMaterials.Plain("sanitariaty-lustro",new Color(.93f,.94f,.95f),.99f,1f);
        var rubber=mats["rubber"];var metal=mats["metal"];var wood=mats["wood"];
        var darkTile=BraxObozMaterials.OwnerSlabTilesPbr();
        var tileFloor=FixtureBox(building,"Oryginalny ciemny gres 2K — podłoga",
            new Vector3(0,.12f,0),new Vector3(12,.24f,10),darkTile);
        // Skala UV w metrach: kamienne płytki powtarzalne, bez rozciągnięcia
        // jednej mapy 2K na całe 12 x 10 m.
        var tileFilter=tileFloor.GetComponent<MeshFilter>();
        var tileMesh=Object.Instantiate(tileFilter.sharedMesh);
        var tileVertices=tileMesh.vertices;
        var tileNormals=tileMesh.normals;
        var tileUv=new Vector2[tileVertices.Length];
        for(int i=0;i<tileVertices.Length;i++)
        {
            Vector3 p=Vector3.Scale(tileVertices[i],new Vector3(12,.24f,10));
            Vector3 n=tileNormals[i];
            tileUv[i]=Mathf.Abs(n.y)>.9f?new Vector2(p.x,p.z)*.5f:
                Mathf.Abs(n.x)>.9f?new Vector2(p.z,p.y)*.5f:new Vector2(p.x,p.y)*.5f;
        }
        tileMesh.uv=tileUv;tileMesh.RecalculateTangents();
        tileFilter.sharedMesh=BraxObozStableMesh.Save(tileMesh);
        PbrWall(building,"Białe płytki ściany południowej 2K",new Vector3(0,1.70f,-5f),new Vector3(12,3.25f,.20f),tile);
        PbrWall(building,"Białe płytki ściany zachodniej 2K",new Vector3(-6f,1.70f,0),new Vector3(.20f,3.25f,10),tile);
        PbrWall(building,"Białe płytki ściany wschodniej 2K",new Vector3(6f,1.70f,0),new Vector3(.20f,3.25f,10),tile);
        // Prawdziwy otwór wejściowy 2.8m, bez niewidzialnego collidera.
        foreach(float x in new[]{-3.7f,3.7f})
            PbrWall(building,"Białe płytki przy wejściu",new Vector3(x,1.70f,5f),new Vector3(4.6f,3.25f,.20f),tile);
        PbrWall(building,"Białe płytki nad wejściem",new Vector3(0,2.81f,5f),new Vector3(2.85f,.95f,.23f),tile);
        // Thin noncolliding cladding OUTSIDE the existing solid wall. The
        // owner's concrete building remains, door opening remains 2.8m.
        PbrWall(building,"Ceglana elewacja PBR — południe",
            new Vector3(0,1.70f,-5.13f),new Vector3(12.25f,3.25f,.035f),facade,false);
        PbrWall(building,"Ceglana elewacja PBR — zachód",
            new Vector3(-6.13f,1.70f,0),new Vector3(.035f,3.25f,10.25f),facade,false);
        PbrWall(building,"Ceglana elewacja PBR — wschód",
            new Vector3(6.13f,1.70f,0),new Vector3(.035f,3.25f,10.25f),facade,false);
        foreach(float x in new[]{-3.7f,3.7f})
            PbrWall(building,"Ceglana elewacja PBR — front i ościeże",
                new Vector3(x,1.70f,5.13f),new Vector3(4.6f,3.25f,.035f),facade,false);
        PbrWall(building,"Ceglane nadproże elewacji",
            new Vector3(0,2.81f,5.14f),new Vector3(2.85f,.95f,.035f),facade,false);
        FixtureBox(building,"Próg kamienny",new Vector3(0,.06f,5.12f),new Vector3(2.75f,.12f,.34f),tile);
        FixtureBox(building,"Dach hydroizolowany",new Vector3(0,3.42f,0),new Vector3(12.6f,.23f,10.6f),dark);
        foreach(float x in new[]{-5.75f,5.75f})
            FixtureBox(building,"Obróbka dachu",new Vector3(x,3.53f,0),new Vector3(.18f,.25f,10.6f),metal,false);
        // Oddzielna strefa WC (zachód) oraz pryszniców (wschód);
        // korytarz od wejścia pozostaje szeroki i przechodni.
        PbrWall(building,"Białe płytki 2K — podział stref",new Vector3(0,1.45f,-2.0f),
            new Vector3(.12f,2.65f,6.0f),tile);
        PbrWall(building,"Białe płytki 2K — WC",new Vector3(-3.05f,1.30f,-2.65f),
            new Vector3(.10f,2.20f,4.25f),tile);
        PbrWall(building,"Białe płytki 2K — prysznice",new Vector3(3.02f,1.30f,-2.65f),
            new Vector3(.10f,2.20f,4.25f),tile);
        foreach(float x in new[]{-4.5f,-1.58f})
        {
            OptionalBuilding("wyposażenie kabiny WC "+x,building,()=>{
            var cubicle=Node("Kabina WC — "+x,building,new Vector3(x,0,-2.95f));
            var toilet=OwnerFixture(cubicle,"close-coupled-toilet.glb",
                new Vector3(0,.24f,0),new Vector3(.70f,1.10f,1.02f));
            var flushButton=BraxObozPlacement.Bounds(SourceBody(toilet,"WC | dual-flush button"));
            var flush=Node("SPŁUCZ TOALETĘ",cubicle,cubicle.InverseTransformPoint(flushButton.center))
                .gameObject.AddComponent<BraxObozFacility>();
            flush.kind=BraxObozFacility.Kind.Toilet;
            // The 0.90 x 2.10 m door starts at the actual 0.24 m bathroom
            // floor. Jamb colliders stop exactly at the opening, x=+/-0.45.
            PbrWall(cubicle,"Lewa ościeżnica WC",new Vector3(-.92f,1.29f,1.48f),
                new Vector3(.94f,2.10f,.12f),wood);
            PbrWall(cubicle,"Prawa ościeżnica WC",new Vector3(.92f,1.29f,1.48f),
                new Vector3(.94f,2.10f,.12f),wood);
            FixtureBox(cubicle,"Nadproże kabiny WC",new Vector3(0,2.465f,1.48f),
                new Vector3(2.78f,.25f,.12f),tile,false);
            var seat=Node("Usiądź — toaleta",cubicle,new Vector3(0,.69f,.10f)).gameObject.AddComponent<BraxSeat>();
            seat.label="TOALETA";seat.lie=false;seat.surfaceY=seat.transform.position.y;seat.hipsAboveRoot=.95f*K;
            // Drzwi każdej kabiny otwierają się, skrzydło ma własną kolizję.
            var hinge=Node("Zawias drzwi kabiny",cubicle,new Vector3(-.45f,.24f,1.48f));
            for(int plank=0;plank<6;plank++)
            {
                float left=plank*.15f+(plank==0?0:.002f);
                float right=(plank+1)*.15f-(plank==5?0:.002f);
                PbrWall(hinge,"Deska skrzydła WC",new Vector3((left+right)*.5f,1.05f,0),
                    new Vector3(right-left,2.10f,.055f),wood,false);
            }
            var leafCollider=hinge.gameObject.AddComponent<BoxCollider>();
            leafCollider.center=new Vector3(.45f,1.05f,0);leafCollider.size=new Vector3(.9f,2.1f,.055f);
            var door=hinge.gameObject.AddComponent<BraxCityDoor>();
            door.id="oboz-wc-"+(x< -3?"1":"2");door.label="KABINA WC";
            door.leaves=new[]{hinge};door.sides=new[]{1f};
            });
        }
        foreach(float x in new[]{1.5f,4.52f})
        {
            OptionalBuilding("wyposażenie prysznica "+x,building,()=>{
            var shower=Node("Kabina prysznicowa — "+x,building,new Vector3(x,0,-2.95f));
            OwnerFixture(shower,"shower-tray-square.glb",
                new Vector3(0,.24f,0),new Vector3(1.65f,.14f,1.65f));
            // The supplied tray keeps its own drain geometry. No authored
            // shower column/head was supplied: list that asset in H rather
            // than inventing fixtures or emitting water from an empty point.
            BraxBuildQuality.Warn("OBÓZ BRAK MODELU — H: shower-column; bez zastępczego panelu i deszczownicy");
            // Ścianka przeciwbryzgowa, otwarte przejście 1.2 m.
            FixtureBox(shower,"Szkło przy wejściu do prysznica",new Vector3(1.02f,1.28f,1.43f),
                new Vector3(.72f,2.08f,.04f),mats["glass"],false);
            FixtureBox(shower,"Profil szyby",new Vector3(.66f,1.29f,1.43f),
                new Vector3(.055f,2.10f,.07f),metal,false);
            });
        }
        foreach(float x in new[]{-4.1f,4.1f})
        {
            OptionalBuilding("umywalka i lustro "+x,building,()=>{
            // Sink rear must touch the north interior wall (z=4.90).
            // Previous z=3.4 left a 1.2m gap, making basins look airborne.
            var sink=Node("Umywalka z lustrem — przy ścianie",building,new Vector3(x,0,4.6f));
            var basin=OwnerFixture(sink,"trough-washbasin.glb",new Vector3(0,.70f,0),new Vector3(1.20f,.35f,.55f));
            var basinBounds=BraxObozPlacement.BoundsInFrame(sink,
                SourceBody(basin,"Trough | shared stone basin"));
            BraxBuildQuality.Warn("OBÓZ BRAK MODELU — H: basin-waste-trap; oryginalna umywalka bez osobnej rury/syfonu");
            // Two real faucets are already part of the supplied GLB.
            var mirrorSurface=FixtureBox(sink,"Lustro na ścianie — ReflectionProbe",
                new Vector3(0,1.89f,.275f),new Vector3(1.25f,1.25f,.035f),mirror,false);
            var mirrorRenderers=mirrorSurface.GetComponentsInChildren<Renderer>();
            BraxObozPlacement.AtHeightAboveGround(mirrorSurface.transform,mirrorRenderers,
                sink.TransformPoint(new Vector3(0,.24f,.275f)),1.6f,
                BraxObozPlacement.HeightReference.Centre,"lustro nad umywalką");
            BraxObozPlacement.ToWall(mirrorSurface.transform,mirrorRenderers,
                BraxObozPlacement.Bounds(mirrorRenderers).center,sink.forward,"lustro na ścianie");
            BathroomReflectionProbe(building,mirrorSurface.GetComponent<Renderer>(),sink.forward,
                x<0?"01":"02");
            foreach(float bracket in new[]{-.43f,.43f}){
                FixtureBox(sink,"Stalowy wspornik ścienny umywalki",
                    new Vector3(bracket,basinBounds.min.y-.07f,basinBounds.center.z),
                    new Vector3(.07f,.14f,basinBounds.size.z),metal);
                FixtureBox(sink,"Kotwa do ściany pod umywalką",
                    new Vector3(bracket,basinBounds.min.y-.15f,.275f),new Vector3(.14f,.30f,.04f),metal);
            }
            // Oryginalny wieszak i ręcznik GLB, bez proceduralnej atrapy.
            var towel=OwnerFixture(sink,"towel-rail-with-towel.glb",
                new Vector3(.63f,1.19f,.10f),new Vector3(.70f,.55f,.32f));
            var towelCollider=towel.GetComponent<Collider>();
            if(towelCollider)Object.DestroyImmediate(towelCollider);
            var outlet=BraxObozPlacement.Bounds(SourceBody(basin,"Mixer | aerator outlet")).center;
            var tapAt=sink.InverseTransformPoint(outlet);
            var tap=Node("UMYJ RĘCE — KRAN",sink,tapAt)
                .gameObject.AddComponent<BraxObozFacility>();
            tap.kind=BraxObozFacility.Kind.Sink;
            tap.effect=Effect(sink,tapAt,false,true);
            });
        }
        // Prawdziwe pisuary 3D na zachodniej ścianie; nie zastępuj ich bryłami.
        foreach(float z in new[]{.2f,2.0f})
            OptionalBuilding("oryginalny pisuar "+z,building,()=>
                OwnerFixture(building,"wall-urinal.glb",new Vector3(-5.62f,.55f,z),
                    new Vector3(.52f,.95f,.38f)));
        // The owner's independent towel GLB rests on a physically modelled
        // wall shelf; avoid another floating decorative sanitary object.
        var shelf=FixtureBox(building,"Półka na ręczniki — ściana łaźni",
            new Vector3(-5.3f,1.47f,3.78f),new Vector3(.85f,.07f,.50f),metal);
        var shelfRenderers=shelf.GetComponentsInChildren<Renderer>();
        BraxObozPlacement.ToWall(shelf.transform,shelfRenderers,
            BraxObozPlacement.Bounds(shelfRenderers).center,building.forward,"półka na ręczniki");
        var shelfBounds=BraxObozPlacement.Bounds(shelfRenderers);
        var towelAt=new Vector3(shelfBounds.center.x,shelfBounds.max.y,shelfBounds.center.z);
        OptionalBuilding("oryginalny ręcznik na półce",building,()=>{
            var foldedTowel=OwnerFixture(building,"bath-towel.glb",
                building.InverseTransformPoint(towelAt),new Vector3(.72f,.25f,.43f));
            PlaceOrRemove(foldedTowel,shelf.transform,towelAt,"ręcznik na półce");
        });
        BraxBuildQuality.Warn("OBÓZ BRAK MODELU — H: interior-lamps; budynek=łaźnia; bez prowizorycznych opraw");
        // A Baked probe includes only ReflectionProbeStatic contributors.
        // These bathroom meshes are fixed; particles are excluded deliberately.
        foreach(var visible in building.GetComponentsInChildren<MeshRenderer>(true))
            GameObjectUtility.SetStaticEditorFlags(visible.gameObject,
                GameObjectUtility.GetStaticEditorFlags(visible.gameObject)|StaticEditorFlags.ReflectionProbeStatic);
        CaptureConstruction(building);
        Debug.Log("BRAXHORN_OBOZ_SANITARIATY dwa WC, dwie kabiny z prysznicem, umywalki; wymagany test przejścia i pozycji gracza");
    }

    static void BathroomReflectionProbe(Transform building,Renderer mirror,Vector3 towardWall,string suffix)
    {
        // Keep the capture outside the mirror and independent of its very thin
        // nonuniform visual transform. Probe box dimensions below are WORLD units.
        var node=new GameObject("Sonda lustra — "+suffix);
        node.transform.SetParent(building,true);
        node.transform.position=mirror.bounds.center-towardWall*.15f*K;
        node.transform.rotation=Quaternion.identity;
        var probe=node.AddComponent<ReflectionProbe>();
        probe.mode=UnityEngine.Rendering.ReflectionProbeMode.Baked;
        probe.refreshMode=UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
        probe.resolution=256;probe.nearClipPlane=.02f*K;probe.farClipPlane=14f*K;
        probe.size=new Vector3(3.5f,2.9f,4f)*K;
        probe.center=new Vector3(0,-.3f,-1.7f)*K;
        probe.boxProjection=true;probe.importance=10;probe.blendDistance=.1f*K;
        mirror.probeAnchor=node.transform;
        mirror.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Simple;
    }

    // Root calls this AFTER lighting and all geometry, BEFORE saving the scene.
    // It produces real cubemap assets; merely adding a ReflectionProbe is not a bake.
    public static void BakeBathroomReflections(Transform camp)
    {
        var probes=camp.GetComponentsInChildren<ReflectionProbe>(true)
            .Where(p=>p.name.StartsWith("Sonda lustra — ",StringComparison.Ordinal))
            .OrderBy(p=>p.name,StringComparer.Ordinal).ToArray();
        if(probes.Length==0)
        {
            BraxBuildQuality.Warn("OBÓZ LUSTRA: brak sond łazienki do wypalenia");return;
        }
        if(probes.Length!=2)BraxBuildQuality.Warn("OBÓZ LUSTRA: sondy="+probes.Length+"; oczekiwano 2");
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            BraxBuildQuality.Warn("OBÓZ LUSTRA: brak urządzenia graficznego; sondy NIE zostały wypalone");return;
        }
        Directory.CreateDirectory(ReflectionFolder);AssetDatabase.Refresh();
        var allMirrors=camp.GetComponentsInChildren<Renderer>(true)
            .Where(r=>r.probeAnchor && probes.Any(p=>p.transform==r.probeAnchor)).ToArray();
        var mirrorEnabled=allMirrors.Select(r=>r.enabled).ToArray();
        // Interior lamps are normally distance-gated at runtime. Capture their
        // intended light once, then restore each exact enabled state.
        var lamps=probes.SelectMany(p=>p.transform.parent.GetComponentsInChildren<Light>(true))
            .Where(l=>l.type!=LightType.Directional).Distinct().ToArray();
        var lampEnabled=lamps.Select(l=>l.enabled).ToArray();
        try
        {
            foreach(var mirror in allMirrors)mirror.enabled=false;
            foreach(var lamp in lamps)lamp.enabled=true;
            for(int i=0;i<probes.Length;i++)
            {
                var probe=probes[i];string suffix=probe.name.EndsWith("01",StringComparison.Ordinal)?"01":"02";
                string path=ReflectionFolder+"/bathroom-mirror-"+suffix+".exr";
                probe.bakedTexture=null;
                try
                {
                    // Unity can remove an orphan .meta before the first bake.
                    // Recreate the same checked-in identity immediately before import.
                    if(!File.Exists(path+".meta"))
                    {
                        using(var hash=System.Security.Cryptography.SHA256.Create())
                        {
                            var bytes=System.Text.Encoding.UTF8.GetBytes("braxhorn-camp-D-20261106/"+path);
                            string guid=BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant().Substring(0,32);
                            File.WriteAllText(path+".meta","fileFormatVersion: 2\nguid: "+guid+"\n");
                        }
                    }
                    bool baked=Lightmapping.BakeReflectionProbe(probe,path);
                    if(!baked || !File.Exists(path))
                    {
                        BraxBuildQuality.Warn("OBÓZ LUSTRA: BakeReflectionProbe nie zapisał cubemapy "+path);continue;
                    }
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                    var cube=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                    if(!cube)
                    {
                        BraxBuildQuality.Warn("OBÓZ LUSTRA: zapis istnieje, ale nie zaimportowano Cubemap "+path);continue;
                    }
                    probe.bakedTexture=cube;EditorUtility.SetDirty(probe);
                    Debug.Log("BRAXHORN_OBOZ_REFLECTION_BAKE path="+path+" resolution="+cube.width+
                        " bytes="+new FileInfo(path).Length+" bakedTextureAssigned=true");
                }
                catch(Exception ex){BraxBuildQuality.Warn("OBÓZ LUSTRA NIE WYPALONE: "+ex.ToString());}
            }
        }
        finally
        {
            for(int i=0;i<allMirrors.Length;i++)allMirrors[i].enabled=mirrorEnabled[i];
            for(int i=0;i<lamps.Length;i++)lamps[i].enabled=lampEnabled[i];
        }
        AssetDatabase.SaveAssets();
    }

    static ParticleSystem Effect(Transform parent,Vector3 position,bool steam,bool faucet=false)
    {
        var t=Node(steam?"Para przy piecu":faucet?"Woda z kranu":"Strumień prysznica",parent,position);
        t.localRotation=Quaternion.Euler(steam?-90:90,0,0);var particles=t.gameObject.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=particles.main;main.playOnAwake=false;main.loop=true;main.maxParticles=steam?70:120;
        main.startLifetime=steam?2.0f:faucet?.16f:.52f;
        main.startSpeed=steam?.35f:faucet?1.1f:4.1f;
        main.startSize=steam?.22f:faucet?.010f:.018f;
        main.simulationSpace=ParticleSystemSimulationSpace.Local;main.scalingMode=ParticleSystemScalingMode.Hierarchy;
        var emission=particles.emission;emission.rateOverTime=steam?18:faucet?45:180;
        var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;
        shape.angle=steam?20:3;shape.radius=steam?.15f:faucet?.018f:.08f;
        var color=particles.colorOverLifetime;color.enabled=true;var gradient=new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(0,1)});color.color=gradient;
        var shader=Shader.Find("Braxhorn/ObozWaterParticle");if(!shader)throw new InvalidOperationException("Brak shadera wody/pary");
        string path=BraxObozCharacters.Generated+"/Materials/"+(steam?"steam":"shower")+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(shader);material.SetColor("_Color",steam?new Color(.85f,.85f,.80f,.13f):new Color(.6f,.78f,.85f,.38f));AssetDatabase.CreateAsset(material,path);}
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;return particles;
    }
    public static void BindPlayer(BraxObozSceneInfo info)
    {
        foreach(var f in info.GetComponentsInChildren<BraxObozFacility>(true))f.player=info.player;
        foreach(var f in info.GetComponentsInChildren<BraxObozLocalLight>(true))f.player=info.player;
        var hips=info.player.visual.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.EndsWith("Hips",StringComparison.Ordinal));
        if(hips)
        {
            float offset=hips.position.y-info.player.transform.position.y;
            foreach(var seat in info.GetComponentsInChildren<BraxSeat>(true))seat.hipsAboveRoot=offset;
        }
    }
}

