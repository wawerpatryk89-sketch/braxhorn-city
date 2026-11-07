using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

// Real timber construction at the retained arena centres. All lengths below
// are source metres; the existing Box/Cylinder/P helpers apply K once.
public static partial class BraxObozEnvironment
{
    internal const float RingPlatformHeight=1f;
    internal const float RingMatThickness=.04f;
    internal const float RingDeckHalf=4f;
    internal const float RingRopeRadius=.025f;
    internal const float RingRopeAxis=3f+RingRopeRadius; // clear inner faces: 6 m
    internal const int RingStairCount=6;
    internal const float RingStairRun=.38f;
    internal const float RingStairWidth=1.40f;
    internal const float ArenaEntryHalf=.75f;
    internal static float RingStairTop(int index)=>(index+1)*(RingPlatformHeight+RingMatThickness)/RingStairCount;
    internal static float RingStairCentreX(int index)=>-RingDeckHalf-RingStairCount*RingStairRun+(index+.5f)*RingStairRun;

    static GameObject ExactTimber(Transform parent,string label,Vector3 a,Vector3 b,
        float radius,Material material)
    {
        var timber=Cylinder(parent,label,a,b,radius,material,true);
        // A short, broad stump must not become the taller capsule that Unity
        // substitutes when a CapsuleCollider's height is below its diameter.
        foreach(var collider in timber.GetComponents<Collider>())Object.DestroyImmediate(collider);
        var contact=timber.AddComponent<MeshCollider>();
        contact.sharedMesh=timber.GetComponent<MeshFilter>().sharedMesh;
        contact.convex=true; // the original cylinder is convex; ClosestPoint requires this
        return timber;
    }

    internal static bool GroundBuiltStructure(Transform model,Renderer[] visible,float floorWorldY,string label)
    {
        // A side stair changes the complete assembly's bounding-box centre.
        // AtBottom must preserve its authored X/Z anchor, not recenter the
        // ring on that asymmetric footprint and silently move the camp plan.
        var bounds=BraxObozPlacement.Bounds(visible);
        return BraxObozPlacement.OnGround(model,visible,
            new Vector3(bounds.center.x,floorWorldY,bounds.center.z),label);
    }

    static void BuildTrainingRing(string name,float x,float z)
    {
        var ring=Group(name,root,P(x,0,z));
        var deck=Group("Podest z desek i belek — 1,00 m",ring,Vector3.zero);
        const int boardCount=45;
        float pitch=2f*RingDeckHalf/boardCount;
        for(int i=0;i<boardCount;i++)
            Box(deck,"Deska podestu 17,5 cm",new Vector3(0,.9625f,-RingDeckHalf+(i+.5f)*pitch),
                new Vector3(8f,.075f,pitch-.003f),floor);
        foreach(float bx in new[]{-3.8f,-1.9f,0f,1.9f,3.8f})
        {
            Box(deck,"Belka pod deskami",new Vector3(bx,.84f,0),new Vector3(.16f,.17f,8f),wood);
            foreach(float bz in new[]{-3.8f,0f,3.8f})
                ExactTimber(deck,"Podpora podestu z bala",new Vector3(bx,0,bz),
                    new Vector3(bx,.755f,bz),.15f,bark);
        }
        CombineDecoration(deck);
        var ringMat=Box(ring,"Mata bez cudzych logo",new Vector3(0,1.02f,0),
            new Vector3(7.8f,RingMatThickness,7.8f),burgundy);
        ringMat.AddComponent<BraxObozDeformableMat>().Configure(7.8f,7.8f);

        // One continuous stair at the west side. The old four risers were
        // .25*K=.3235 m, above the actual NavMesh climb limit of .30 m.
        // Six .173333*K=.2243 m rises fit both controller and bake settings.
        for(int i=0;i<RingStairCount;i++)
        {
            float top=RingStairTop(i);
            var tread=Group("Schodek boczny "+(i+1),ring,Vector3.zero);
            float px=RingStairCentreX(i);
            // Each tread has real boards and timber supports down to the
            // floor; the landing board bridges exactly to the mat's edge.
            for(int board=0;board<2;board++)
                Box(tread,"Deska stopnia",new Vector3(px+(board-.5f)*RingStairRun/2,
                    top-.025f,0),new Vector3(RingStairRun/2-.002f,.05f,RingStairWidth),wood);
            foreach(float side in new[]{-1f,1f})
                Box(tread,"Policzek stopnia",new Vector3(px,(top-.05f)/2,side*.55f),
                    new Vector3(RingStairRun,top-.05f,.12f),wood);
            CombineDecoration(tread);
        }
        Box(ring,"Styk schodów i podestu",new Vector3(-RingDeckHalf+.03f,1.015f,0),
            new Vector3(.14f,.05f,RingStairWidth),wood);

        // Position the COMPLETE assembly before creating world-space ropes.
        // Ring 2 therefore rests on the pavilion's actual .11 m floor.
        var visible=ring.GetComponentsInChildren<Renderer>(true);
        GroundBuiltStructure(ring,visible,ring.position.y,name+" — konstrukcja na podłożu");
        var record=BraxObozPlacementRecord.For(ring);
        record.contactPoint=Group("Pomiar stopy podestu",ring,P(0,0,0));
        record.heightAboveFloorMetres=RingPlatformHeight;
        record.heightReference=(int)BraxObozPlacement.HeightReference.Top;
        record.heightRenderers=deck.GetComponentsInChildren<Renderer>(true);

        var corners=new Dictionary<Vector2,Collider>();
        foreach(float dx in new[]{-RingRopeAxis,RingRopeAxis})
        foreach(float dz in new[]{-RingRopeAxis,RingRopeAxis})
        {
            var post=ExactTimber(ring,"Słupek ringu",new Vector3(dx,1f,dz),
                new Vector3(dx,2.55f,dz),.09f,metal);
            corners[new Vector2(dx,dz)]=post.GetComponent<Collider>();
            Cylinder(ring,"Ochraniacz narożnika",new Vector3(dx*.98f,1.23f,dz*.98f),
                new Vector3(dx*.98f,2.50f,dz*.98f),.16f,burgundy);
        }
        // The entry shares the side reached by the stairs. Its real posts
        // support the ends of all four ropes; there are no floating rope ends.
        foreach(float end in new[]{-ArenaEntryHalf,ArenaEntryHalf})
        {
            var post=ExactTimber(ring,"Słupek wejścia przy schodach",new Vector3(-RingRopeAxis,1f,end),
                new Vector3(-RingRopeAxis,2.55f,end),.065f,metal);
            corners[new Vector2(-RingRopeAxis,end)]=post.GetComponent<Collider>();
        }
        void Side(Vector3 a,Vector3 b,Vector3 outward)
        {
            ArenaRopes(ring,a,b,outward,corners[new Vector2(a.x,a.z)]);
            Vector3 mid=(a+b)*.5f;
            var collider=Group("Kolizja za linami",ring,P(mid.x,1.75f,mid.z));
            collider.localRotation=Quaternion.LookRotation(b-a);
            collider.gameObject.AddComponent<BoxCollider>().size=new Vector3(.10f,1.45f,Vector3.Distance(a,b))*K;
        }
        Side(new Vector3(-RingRopeAxis,0,-RingRopeAxis),new Vector3(-RingRopeAxis,0,-ArenaEntryHalf),Vector3.left);
        Side(new Vector3(-RingRopeAxis,0,ArenaEntryHalf),new Vector3(-RingRopeAxis,0,RingRopeAxis),Vector3.left);
        Side(new Vector3(RingRopeAxis,0,-RingRopeAxis),new Vector3(RingRopeAxis,0,RingRopeAxis),Vector3.right);
        Side(new Vector3(-RingRopeAxis,0,RingRopeAxis),new Vector3(RingRopeAxis,0,RingRopeAxis),Vector3.forward);
        Side(new Vector3(-RingRopeAxis,0,-RingRopeAxis),new Vector3(RingRopeAxis,0,-RingRopeAxis),Vector3.back);
        var arena=ring.gameObject.AddComponent<BraxObozArena>();arena.label=name;
        arena.halfSize=new Vector2(2.95f,2.95f)*K;arenas.Add(arena);
        var ropeMeshes=root.GetComponentsInChildren<BraxLina>()
            .Where(r=>r.name==name+" — liny na słupkach").Select(r=>r.GetComponent<MeshFilter>());
        var structure=BraxObozStructureAuthoring.Capture(ring,
            ring.GetComponentsInChildren<MeshFilter>().Concat(ropeMeshes).ToArray(),ring.GetComponentsInChildren<Collider>());
        structure.expectedTopMetres=1f;structure.heightMembers=deck.GetComponentsInChildren<Renderer>();
        Optional(name+" ławy i słupy",()=>{
            foreach(float side in new[]{-1f,1f})
            foreach(float at in new[]{-2.7f,2.7f})
            {
                ArenaLogBench(ring,new Vector3(at,0,side*5.7f),0);
                // The clear west opening remains wider than the whole stair.
                ArenaLogBench(ring,new Vector3(side*5.7f,0,at),90);
            }
            foreach(float side in new[]{-1f,1f})
                ArenaLightPost(ring,new Vector3(side*5.7f,0,5.7f),new Vector3(0,1.1f,0));
        });
    }

    internal static bool TryRopePostSurfaceContact(BraxLina line,Collider post,
        out Vector3 contact,out int triangleIndex)
    {
        contact=default;triangleIndex=-1;
        if(!line || !post || !post.enabled || post.isTrigger || !post.gameObject.activeInHierarchy ||
            line.boki<3 || line.odcinki<1 || line.wysokosci==null || line.wysokosci.Length==0)return false;
        var filter=line.GetComponent<MeshFilter>();
        if(!filter || !filter.sharedMesh || !filter.sharedMesh.isReadable)return false;
        var mesh=filter.sharedMesh;var vertices=mesh.vertices;var indices=mesh.triangles;
        // Zbuduj emits the lowest rope's side triangles first. Read the
        // rendered vertices, including any present deformation; do not
        // reconstruct an attachment from line.a or an assumed post radius.
        int firstRopeIndices=line.odcinki*line.boki*6;
        if(vertices.Length<(line.odcinki+1)*(line.boki+1) || indices.Length<firstRopeIndices)return false;
        var world=new Vector3[(line.odcinki+1)*(line.boki+1)];
        for(int i=0;i<world.Length;i++)world[i]=filter.transform.TransformPoint(vertices[i]);
        Physics.SyncTransforms();
        var endBounds=new Bounds(world[0],Vector3.zero);
        for(int q=1;q<=line.boki;q++)endBounds.Encapsulate(world[q]);
        endBounds.Expand(.06f*K);
        // Broad phase only: the designated post must be near this end, not a
        // different post crossed halfway along the rope. This proves no contact.
        if(!endBounds.Intersects(post.bounds))return false;
        for(int i=0;i<firstRopeIndices;i+=3)
        for(int edge=0;edge<3;edge++)
        {
            int ia=indices[i+edge],ib=indices[i+(edge+1)%3];
            if(ia<0 || ib<0 || ia>=world.Length || ib>=world.Length)return false;
            var a=world[ia];var b=world[ib];float length=Vector3.Distance(a,b);
            if(length<.000001f)continue;
            // Unity rays starting inside a solid do not find its exit. Try
            // both directions so the outside endpoint finds the real surface.
            for(int direction=0;direction<2;direction++)
            {
                var start=direction==0?a:b;var end=direction==0?b:a;
                var ray=new Ray(start,(end-start)/length);
                if(!post.Raycast(ray,out var hit,length+.0001f*K))continue;
                // This interpolation is on an actual edge of the rendered
                // rope triangle. Verify the same point against source support
                // triangles, rather than accepting a cooked hull or AABB.
                var candidate=Vector3.Lerp(start,end,Mathf.Clamp01(hit.distance/length));
                if(!BraxObozSupportSurface.TryMeasure(post,candidate,out var surface) ||
                    surface.distanceWorld>.001f*K)continue;
                contact=candidate;triangleIndex=i/3;return true;
            }
        }
        return false;
    }

    static void ArenaRopes(Transform ring,Vector3 a,Vector3 b,Vector3 outward,Collider post)
    {
        // BraxLina explicitly stores world-space vertices on an identity root.
        // Preserve that API and include the measured platform base in heights.
        var go=Group(ring.name+" — liny na słupkach",root,Vector3.zero).gameObject;
        go.AddComponent<MeshRenderer>().sharedMaterial=rope;
        var line=go.AddComponent<BraxLina>();
        line.a=ring.TransformPoint(a*K);line.a.y=0;
        line.b=ring.TransformPoint(b*K);line.b.y=0;
        line.wysokosci=new[]{1.35f,1.70f,2.05f,2.40f}.Select(y=>ring.position.y+y*K).ToArray();
        line.promien=RingRopeRadius*K;line.naZewnatrz=outward;
        line.Zbuduj();var filter=go.GetComponent<MeshFilter>();
        filter.sharedMesh=SaveMesh(Object.Instantiate(filter.sharedMesh));
        var record=BraxObozPlacementRecord.For(go.transform);
        record.SetMembers(go.GetComponents<Renderer>());
        record.label=ring.name+" — prawdziwy koniec liny na słupku";
        record.support=BraxObozPlacementRecord.SupportKind.Suspended;record.suspensionSupport=post;
        if(TryRopePostSurfaceContact(line,post,out var contact,out int triangle))
            record.suspensionPoint=Group("Zmierzony styk liny i słupka — trójkąt "+triangle,
                go.transform,go.transform.InverseTransformPoint(contact));
        else
        {
            record.suspensionPoint=null;
            BraxBuildQuality.Warn("OBÓZ ROPE_POST_CONTACT_UNMEASURED "+ring.name+
                " — brak zmierzonego przecięcia powierzchni liny i słupka");
        }
    }

    static void BuildTrainingOctagon(float x,float z)
    {
        const float radius=5.3f,deckTop=.31f,matTop=.35f;
        var cage=Group("Oktagon",root,P(x,0,z));
        var polygon=Enumerable.Range(0,8).Select(i=>new Vector2(
            Mathf.Cos(i*Mathf.PI/4)*radius,Mathf.Sin(i*Mathf.PI/4)*radius)).ToList();
        // Individual boards clipped to the actual octagon, above genuine
        // radial joists and grounded timber supports. No floating single face.
        for(float start=-radius;start<radius;start+=.18f)
        {
            var strip=ClipHalfPlane(polygon,new Vector2(-10,start),new Vector2(10,start),true);
            strip=ClipHalfPlane(strip,new Vector2(10,Mathf.Min(radius,start+.177f)),
                new Vector2(-10,Mathf.Min(radius,start+.177f)),true);
            if(strip.Count>=3)PolygonVolume(cage,"Deska oktagonu",strip,.26f,p=>deckTop,wood,true);
        }
        for(int i=0;i<8;i++)
        {
            float angle=i*Mathf.PI/4;
            var end=new Vector3(Mathf.Sin(angle)*(radius-.2f),0,Mathf.Cos(angle)*(radius-.2f));
            var beam=Box(cage,"Belka pod oktagonem",end*.5f+Vector3.up*.20f,
                new Vector3(.16f,.12f,end.magnitude),wood);
            beam.transform.localRotation=Quaternion.LookRotation(end);
            foreach(float fraction in new[]{.30f,.90f})
                ExactTimber(cage,"Podpora podestu oktagonu",end*fraction,
                    end*fraction+Vector3.up*.14f,.15f,bark);
        }
        var mat=PolygonVolume(cage,"Mata oktagonu na deskach",polygon,deckTop,p=>matTop,burgundy,true);
        CombineDecoration(cage);
        // Ground the complete base before cage panels and measured side stairs.
        GroundBuiltStructure(cage,cage.GetComponentsInChildren<Renderer>(true),cage.position.y,"oktagon na podłożu");
        var floorRecord=BraxObozPlacementRecord.For(cage);
        floorRecord.contactPoint=Group("Pomiar podpory oktagonu",cage,P(0,0,(radius-.2f)*.3f));
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;
            var p=new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius);
            var q=new Vector3(Mathf.Sin(b)*radius,0,Mathf.Cos(b)*radius);
            ExactTimber(cage,"Słupek klatki",p+Vector3.up*matTop,p+Vector3.up*2.4f,.065f,metal);
            if(i==3)continue;
            Cylinder(cage,"Górny profil klatki",p+Vector3.up*2.35f,q+Vector3.up*2.35f,.04f,metal);
            Cylinder(cage,"Dolny profil klatki",p+Vector3.up*.4f,q+Vector3.up*.4f,.04f,metal);
            CagePanel(cage,p,q);
            Cylinder(cage,"Miękka osłona słupka",p+Vector3.up*.52f,p+Vector3.up*2.3f,.105f,burgundy);
            var mid=(p+q)*.5f;var collision=Group("Kolizja klatki",cage,(mid+Vector3.up*1.4f)*K);
            collision.localRotation=Quaternion.LookRotation(q-p);
            collision.gameObject.AddComponent<BoxCollider>().size=new Vector3(.08f,2.1f,Vector3.Distance(p,q))*K;
        }
        var entry=Group("Schody wejścia do oktagonu",cage,Vector3.zero);
        entry.localRotation=Quaternion.Euler(0,3.5f*45f,0);
        float edge=radius*Mathf.Cos(Mathf.PI/8);
        for(int i=0;i<3;i++)
        {
            float top=(i+1)*matTop/3f;
            Box(entry,"Drewniany stopień oktagonu",new Vector3(0,top*.5f,edge+.70f-i*.30f),
                new Vector3(1.4f,top,.42f),wood);
        }
        var arena=cage.gameObject.AddComponent<BraxObozArena>();arena.label="OKTAGON";
        arena.halfSize=new Vector2(4.8f,4.8f)*K;arenas.Add(arena);
        BraxObozStructureAuthoring.Capture(cage,cage.GetComponentsInChildren<MeshFilter>(),cage.GetComponentsInChildren<Collider>());
        Optional("oktagon ławy i słupy",()=>{
            for(int i=0;i<8;i++)
            {
                if(i==3)continue; // physical entry and stair corridor
                float angle=(i+.5f)*45f;
                var radial=Quaternion.Euler(0,angle,0)*Vector3.forward;
                ArenaLogBench(cage,radial*7f,angle);
            }
            foreach(float angle in new[]{22.5f,202.5f})
                ArenaLightPost(cage,Quaternion.Euler(0,angle,0)*Vector3.forward*7.5f,new Vector3(0,1f,0));
        });
        BraxBuildQuality.Warn("OBÓZ BRAK MODELU — H: beczki z ogniem, gong; oktagon zachowuje miejsce bez atrap");
    }

    static void ArenaLogBench(Transform parent,Vector3 sourcePoint,float yaw)
    {
        var bench=Group("Ława widowni z bali",parent,sourcePoint*K);
        bench.localRotation=Quaternion.Euler(0,yaw,0);
        ExactTimber(bench,"Siedzisko z okrągłego bala",new Vector3(-1.2f,.32f,0),
            new Vector3(1.2f,.32f,0),.13f,wood);
        foreach(float x in new[]{-.85f,.85f})
            ExactTimber(bench,"Pień pod siedziskiem",new Vector3(x,0,0),new Vector3(x,.19f,0),.18f,bark);
        GroundBuiltStructure(bench,bench.GetComponentsInChildren<Renderer>(true),
            bench.position.y,"ława widowni 0,45 m");
        var record=BraxObozPlacementRecord.For(bench);record.expectedDimensionMetres=.45f;
        record.dimensionAxis=(int)BraxObozPlacement.Axis.Y;record.dimensionFrame=parent;
        record.contactPoint=Group("Pomiar pnia ławy",bench,P(-.85f,0,0));
        var seat=Group("Miejsce na ławie widowni",bench,P(0,.45f,0)).gameObject.AddComponent<BraxSeat>();
        seat.label="WIDOWNIA";seat.surfaceY=seat.transform.position.y;seat.hipsAboveRoot=.95f*K;
        var structure=BraxObozStructureAuthoring.Capture(bench,bench.GetComponentsInChildren<MeshFilter>(),bench.GetComponentsInChildren<Collider>());
        structure.expectedTopMetres=.45f;structure.heightMembers=bench.GetComponentsInChildren<Renderer>();
    }

    static void ArenaLightPost(Transform parent,Vector3 sourcePoint,Vector3 target)
    {
        var group=Group("Drewniany słup oświetlenia areny",parent,sourcePoint*K);
        ExactTimber(group,"Słup z bala",Vector3.zero,new Vector3(0,3.8f,0),.16f,bark);
        GroundBuiltStructure(group,group.GetComponentsInChildren<Renderer>(true),
            group.position.y,"słup oświetlenia areny");
        var lamp=Group("Światło areny 4000 K — oprawa na liście H",group,P(0,3.65f,0));
        lamp.LookAt(parent.TransformPoint(target*K));
        var light=lamp.gameObject.AddComponent<Light>();light.type=LightType.Spot;
        light.useColorTemperature=true;light.colorTemperature=4000f;light.color=Color.white;
        light.intensity=1.5f;light.range=14f*K;light.spotAngle=95f;
        light.shadows=LightShadows.None;light.renderMode=LightRenderMode.ForceVertex;
        lamp.gameObject.AddComponent<BraxObozLocalLight>().source=light;
        BraxObozStructureAuthoring.Capture(group,group.GetComponentsInChildren<MeshFilter>(),group.GetComponentsInChildren<Collider>());
        BraxBuildQuality.Warn("OBÓZ BRAK MODELU — H: oprawa reflektora areny; istnieje drewniany słup i światło4000K, bez zastępczej kostki");
    }
}
