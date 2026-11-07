using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Reopened-scene geometry measurement. No name/parent exemptions and no
// generated proxy collider can stand in for a measured construction joint.
public static class BraxObozStructureAudit
{
    const float K=1.294f, Tolerance=.03f*K, Weld=.00001f*K;
    const int MaximumTrianglesPerBatch=250000, MaximumTrianglesPerIsland=16000, MaximumTrianglePairs=2000000;

    [Serializable] public sealed class Piece
    {
        public string owner,path,label,status,groundPath,budgetExceeded;
        public int part,island,vertices,triangles,parentPiece=-1,connectionDepth=-1;
        public bool measured,groundMeasured,directGroundContact,connected,embedded,verified;
        public float minimumGroundGapMetres,maximumGroundGapMetres;
    }
    [Serializable] public sealed class Connection
    {
        public int a,b;
        // The first proven point/edge/triangle distance, not a claim that an
        // early-out search found the global minimum of every pair of meshes.
        public float separationUpperBoundMetres;
    }
    [Serializable] public sealed class RendererEvidence
    {
        public string path,status;
        public int pieces,measured,connected,embedded,unmeasured;
        public bool completeTriangleCoverage,verified;
        [NonSerialized] public Renderer renderer;
    }
    [Serializable] public sealed class SurfaceHeight
    {
        public string owner,status;
        public bool measured,supportVerified;
        public float expectedMetres,actualMetres,errorMetres,baseYWorld,topYWorld;
    }
    [Serializable] public sealed class Result
    {
        public bool sceneGeometryMeasured,phoneVerified;
        public int maximumTrianglesPerBatch=MaximumTrianglesPerBatch,
            maximumTrianglesPerIsland=MaximumTrianglesPerIsland,maximumTrianglePairs=MaximumTrianglePairs;
        public int batchesOverBudget,islandsOverBudget;
        public int records,registeredRenderers,measuredPieces,unmeasuredPieces,
            unsupportedPieces,embeddedPieces,invalidGrounds,unmeasuredPairChecks,heightWarnings,unmeasuredRenderers;
        public Piece[] pieces;
        public Connection[] connections;
        public RendererEvidence[] renderers;
        public SurfaceHeight[] heights;
        public string[] findings;
        public bool TryGet(Renderer renderer,out RendererEvidence evidence)
        {
            foreach(var entry in renderers)
                if(entry.renderer==renderer){evidence=entry;return true;}
            evidence=null;return false;
        }
    }
    struct Triangle
    {
        public Vector3 a,b,c;
        public Bounds bounds;
        public Vector3 Point(int i)=>i==0?a:i==1?b:c;
        public Triangle(Vector3 a,Vector3 b,Vector3 c)
        {
            this.a=a;this.b=b;this.c=c;
            bounds=new Bounds(a,Vector3.zero);bounds.Encapsulate(b);bounds.Encapsulate(c);
        }
    }
    sealed class Geometry
    {
        public BraxObozStructureRecord owner;
        public Piece entry;
        public Renderer renderer;
        public Bounds bounds;
        public Vector3[] vertices;
        public Triangle[] triangles;
    }
    sealed class MeshData
    {
        public Vector3[] vertices;
        public int[] indices;
    }
    readonly struct VertexKey : IEquatable<VertexKey>
    {
        readonly long x,y,z;
        public VertexKey(Vector3 p)
        {x=(long)Math.Round((double)p.x/Weld);y=(long)Math.Round((double)p.y/Weld);z=(long)Math.Round((double)p.z/Weld);}
        public bool Equals(VertexKey b)=>x==b.x && y==b.y && z==b.z;
        public override bool Equals(object other)=>other is VertexKey k && Equals(k);
        public override int GetHashCode()=>unchecked((x.GetHashCode()*397^y.GetHashCode())*397^z.GetHashCode());
    }
    static string Path(Transform t)
    {
        var names=new List<string>();for(;t;t=t.parent)names.Add(t.name);
        names.Reverse();return string.Join("/",names);
    }
    static int Root(int[] parents,int i)
    {
        while(parents[i]!=i){parents[i]=parents[parents[i]];i=parents[i];}return i;
    }
    static void Join(int[] parents,int a,int b)
    {a=Root(parents,a);b=Root(parents,b);if(a!=b)parents[b]=a;}

    static void Read(BraxObozStructureRecord owner,BraxObozStructureRecord.Part part,int number,
        Dictionary<Mesh,MeshData> meshes,List<Geometry> output,List<string> findings)
    {
        var entry=new Piece {owner=Path(owner.transform),label=part?.label,part=number,
            path=part!=null && part.filter?Path(part.filter.transform):Path(owner.transform),
            status="niezmierzone: brak geometrii lub zakresów"};
        Renderer renderer=null;
        if(part!=null && part.filter)renderer=part.filter.GetComponent<Renderer>();
        try
        {
            if(part==null || !part.filter || !part.filter.sharedMesh || !renderer ||
                !renderer.enabled || !renderer.gameObject.activeInHierarchy || !part.rangesVerified)
                throw new InvalidOperationException("Brak aktywnej, jawnie zarejestrowanej geometrii");
            if(!owner.Contains(renderer))throw new InvalidOperationException("Renderer nie jest jawnym członkiem rekordu");
            var mesh=part.filter.sharedMesh;
            if(!meshes.TryGetValue(mesh,out var data))
            {
                for(int i=0;i<mesh.subMeshCount;i++)
                    if(mesh.GetTopology(i)!=MeshTopology.Triangles)
                        throw new InvalidOperationException("Topologia inna niż trójkąty");
                data=new MeshData {vertices=mesh.vertices,indices=mesh.triangles};meshes.Add(mesh,data);
            }
            if(part.firstVertex<0 || part.vertexCount<=0 ||
                part.firstVertex+part.vertexCount>data.vertices.Length ||
                part.firstIndex<0 || part.indexCount<=0 || part.indexCount%3!=0 ||
                part.firstIndex+part.indexCount>data.indices.Length)
                throw new InvalidOperationException("Nieprawidłowe zakresy końcowej siatki");
            int count=part.indexCount/3;entry.triangles=count;
            // A service-building batch contains many small disconnected boards.
            // Separate the bounded batch FIRST; the detailed joint budget is
            // applied to each actual island below, never to their combined sum.
            if(count>MaximumTrianglesPerBatch)
            {
                entry.budgetExceeded="batch";
                throw new InvalidOperationException("Batch przekracza jawny budżet podziału "+MaximumTrianglesPerBatch+" trójkątów");
            }
            var world=new Vector3[part.vertexCount];
            for(int i=0;i<world.Length;i++)world[i]=part.filter.transform.TransformPoint(data.vertices[part.firstVertex+i]);
            var triangles=new Triangle[count];var parents=new int[count];
            var firstAtVertex=new Dictionary<VertexKey,int>();
            for(int t=0;t<count;t++)
            {
                parents[t]=t;var p=new Vector3[3];
                for(int corner=0;corner<3;corner++)
                {
                    int v=data.indices[part.firstIndex+t*3+corner]-part.firstVertex;
                    if(v<0 || v>=world.Length)throw new InvalidOperationException("Indeks wychodzi poza zapisaną część");
                    p[corner]=world[v];var key=new VertexKey(p[corner]);
                    if(firstAtVertex.TryGetValue(key,out int first))Join(parents,t,first);
                    else firstAtVertex.Add(key,t);
                }
                triangles[t]=new Triangle(p[0],p[1],p[2]);
            }
            // A batched renderer can contain disconnected boards. Even one
            // source mesh is split into geometric islands before support tests.
            var islands=new Dictionary<int,List<Triangle>>();
            for(int i=0;i<count;i++)
            {
                int key=Root(parents,i);
                if(!islands.TryGetValue(key,out var list)){list=new List<Triangle>();islands.Add(key,list);}
                list.Add(triangles[i]);
            }
            int island=0;
            foreach(var list in islands.Values)
            {
                if(list.Count>MaximumTrianglesPerIsland)
                {
                    var limited=new Piece {owner=entry.owner,label=entry.label,path=entry.path,part=number,
                        island=island++,triangles=list.Count,budgetExceeded="island",
                        status="niezmierzone: wyspa przekracza budżet "+MaximumTrianglesPerIsland+" trójkątów"};
                    output.Add(new Geometry {owner=owner,entry=limited,renderer=renderer});
                    findings.Add(limited.path+" / "+limited.label+": "+limited.status);
                    continue;
                }
                var unique=new Dictionary<VertexKey,Vector3>();
                foreach(var triangle in list)
                    foreach(var p in new[]{triangle.a,triangle.b,triangle.c})unique[new VertexKey(p)]=p;
                var points=unique.Values.ToArray();var bounds=new Bounds(points[0],Vector3.zero);
                foreach(var p in points)bounds.Encapsulate(p);
                var measured=new Piece {owner=entry.owner,label=entry.label,path=entry.path,part=number,
                    island=island++,measured=true,vertices=points.Length,triangles=list.Count,
                    status="zmierzona geometria; podparcie jeszcze niepotwierdzone"};
                output.Add(new Geometry {owner=owner,entry=measured,renderer=renderer,bounds=bounds,
                    vertices=points,triangles=list.ToArray()});
            }
        }
        catch(Exception ex)
        {
            entry.status="niezmierzone: "+ex.Message;
            output.Add(new Geometry {owner=owner,entry=entry,renderer=renderer});
            findings.Add(entry.path+" / "+entry.label+": "+entry.status);
        }
    }

    static bool ValidGround(BraxObozStructureGround ground)
    {
        return ground && ground.surface && ground.geometry && ground.visibleSurface &&
            ground.surface.enabled && !ground.surface.isTrigger && ground.visibleSurface.enabled &&
            ground.gameObject.activeInHierarchy && ground.surface.gameObject.scene==ground.gameObject.scene &&
            ground.surface.sharedMesh && ground.surface.sharedMesh==ground.geometry.sharedMesh &&
            ground.surface.transform==ground.geometry.transform &&
            ground.visibleSurface.transform==ground.geometry.transform;
    }
    static void Ground(Geometry geometry,BraxObozStructureGround[] grounds)
    {
        var e=geometry.entry;if(!e.measured)return;
        float min=float.PositiveInfinity,max=float.NegativeInfinity;int hits=0;
        foreach(var vertex in geometry.vertices)
        {
            float best=float.NegativeInfinity;string path=null;
            foreach(var ground in grounds)
            {
                if(ground.gameObject.scene!=geometry.renderer.gameObject.scene)continue;
                float originY=Mathf.Max(ground.surface.bounds.max.y,geometry.bounds.max.y)+K;
                var ray=new Ray(new Vector3(vertex.x,originY,vertex.z),Vector3.down);
                float distance=originY-Mathf.Min(vertex.y,ground.surface.bounds.min.y)+K;
                if(ground.surface.Raycast(ray,out var hit,distance) && hit.normal.y>.65f && hit.point.y>best)
                {best=hit.point.y;path=Path(ground.surface.transform);}
            }
            if(float.IsNegativeInfinity(best))continue;
            hits++;float gap=(vertex.y-best)/K;
            if(gap<min){min=gap;e.groundPath=path;}max=Mathf.Max(max,gap);
        }
        // Missing ground rays remain explicit. Partial terrain coverage cannot
        // certify an entire construction foot as supported or not embedded.
        e.groundMeasured=hits==geometry.vertices.Length && hits>0;
        if(hits==0)return;
        e.minimumGroundGapMetres=min;e.maximumGroundGapMetres=max;
        e.embedded=min<-.03f;
        e.directGroundContact=e.groundMeasured && min<=.03f && max>=-.03f;
    }
    static float BoundsDistanceSquared(Bounds a,Bounds b)
    {
        float x=Mathf.Max(0,Mathf.Max(a.min.x-b.max.x,b.min.x-a.max.x));
        float y=Mathf.Max(0,Mathf.Max(a.min.y-b.max.y,b.min.y-a.max.y));
        float z=Mathf.Max(0,Mathf.Max(a.min.z-b.max.z,b.min.z-a.max.z));
        return x*x+y*y+z*z;
    }
    static Vector3 ClosestTriangle(Vector3 p,Triangle t)
    {
        var ab=t.b-t.a;var ac=t.c-t.a;var ap=p-t.a;
        float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);
        if(d1<=0 && d2<=0)return t.a;
        var bp=p-t.b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);
        if(d3>=0 && d4<=d3)return t.b;
        float vc=d1*d4-d3*d2;
        if(vc<=0 && d1>=0 && d3<=0)return t.a+ab*(d1/(d1-d3));
        var cp=p-t.c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);
        if(d6>=0 && d5<=d6)return t.c;
        float vb=d5*d2-d1*d6;
        if(vb<=0 && d2>=0 && d6<=0)return t.a+ac*(d2/(d2-d6));
        float va=d3*d6-d5*d4;
        if(va<=0 && d4-d3>=0 && d5-d6>=0)return t.b+(t.c-t.b)*((d4-d3)/((d4-d3)+(d5-d6)));
        float sum=va+vb+vc;
        if(Mathf.Abs(sum)<1e-18f)return t.a;
        return t.a+ab*(vb/sum)+ac*(vc/sum);
    }
    static float SegmentDistanceSquared(Vector3 p1,Vector3 q1,Vector3 p2,Vector3 q2)
    {
        var d1=q1-p1;var d2=q2-p2;var r=p1-p2;
        float a=Vector3.Dot(d1,d1),e=Vector3.Dot(d2,d2),f=Vector3.Dot(d2,r),s,t;
        if(a<=1e-15f && e<=1e-15f)return r.sqrMagnitude;
        if(a<=1e-15f){s=0;t=Mathf.Clamp01(f/e);}
        else
        {
            float c=Vector3.Dot(d1,r);
            if(e<=1e-15f){t=0;s=Mathf.Clamp01(-c/a);}
            else
            {
                float b=Vector3.Dot(d1,d2),den=a*e-b*b;
                s=Mathf.Abs(den)>1e-15f?Mathf.Clamp01((b*f-c*e)/den):0;
                t=(b*s+f)/e;
                if(t<0){t=0;s=Mathf.Clamp01(-c/a);}
                else if(t>1){t=1;s=Mathf.Clamp01((b-c)/a);}
            }
        }
        return (p1+d1*s-p2-d2*t).sqrMagnitude;
    }
    static bool SegmentHitsTriangle(Vector3 p,Vector3 q,Triangle t)
    {
        var direction=q-p;var edge1=t.b-t.a;var edge2=t.c-t.a;
        var cross=Vector3.Cross(direction,edge2);float det=Vector3.Dot(edge1,cross);
        if(Mathf.Abs(det)<1e-12f)return false;
        float inverse=1f/det;var start=p-t.a;
        float u=Vector3.Dot(start,cross)*inverse;if(u<0 || u>1)return false;
        var other=Vector3.Cross(start,edge1);float v=Vector3.Dot(direction,other)*inverse;
        if(v<0 || u+v>1)return false;
        float along=Vector3.Dot(edge2,other)*inverse;return along>=0 && along<=1;
    }
    static float TriangleDistanceSquared(Triangle a,Triangle b)
    {
        float best=float.PositiveInfinity;
        for(int i=0;i<3;i++)
        {
            if(SegmentHitsTriangle(a.Point(i),a.Point((i+1)%3),b) ||
                SegmentHitsTriangle(b.Point(i),b.Point((i+1)%3),a))return 0;
            best=Mathf.Min(best,(a.Point(i)-ClosestTriangle(a.Point(i),b)).sqrMagnitude);
            best=Mathf.Min(best,(b.Point(i)-ClosestTriangle(b.Point(i),a)).sqrMagnitude);
            for(int j=0;j<3;j++)best=Mathf.Min(best,
                SegmentDistanceSquared(a.Point(i),a.Point((i+1)%3),b.Point(j),b.Point((j+1)%3)));
        }
        return best;
    }
    static bool Touches(Geometry a,Geometry b,out float upperBound,out bool measured)
    {
        upperBound=float.PositiveInfinity;measured=true;int comparisons=0;
        float limit=Tolerance*Tolerance;
        foreach(var ta in a.triangles)foreach(var tb in b.triangles)
        {
            if(++comparisons>MaximumTrianglePairs){measured=false;return false;}
            if(BoundsDistanceSquared(ta.bounds,tb.bounds)>limit)continue;
            float distance=TriangleDistanceSquared(ta,tb);
            if(distance<=limit){upperBound=Mathf.Sqrt(distance)/K;return true;}
        }
        return false;
    }

    static bool CompleteCoverage(Renderer renderer,BraxObozStructureRecord[] records)
    {
        var filter=renderer.GetComponent<MeshFilter>();
        if(!filter || !filter.sharedMesh)return false;
        var ranges=records.SelectMany(r=>r.parts ?? Array.Empty<BraxObozStructureRecord.Part>())
            .Where(p=>p!=null && p.filter==filter).OrderBy(p=>p.firstIndex).ToArray();
        int next=0;
        foreach(var range in ranges)
        {
            if(!range.rangesVerified || range.firstIndex!=next || range.indexCount<=0)return false;
            next+=range.indexCount;
        }
        try{return next==filter.sharedMesh.triangles.Length && next>0;}
        catch{return false;}
    }
    static SurfaceHeight Height(BraxObozStructureRecord record,List<Geometry> geometry)
    {
        var result=new SurfaceHeight {owner=Path(record.transform),expectedMetres=record.expectedTopMetres,
            status="niezmierzona wysokość powierzchni"};
        // Use only the pieces captured by THIS exact record, including their
        // saved sparse mesh ranges. A later child and a sibling floor cannot
        // silently change the measured construction base.
        var own=geometry.Where(g=>g.owner==record).ToArray();
        var members=new HashSet<Renderer>(record.heightMembers ?? Array.Empty<Renderer>());
        if(own.Length==0 || own.Any(g=>!g.entry.measured) || members.Count==0 ||
            members.Any(r=>!r || !record.Contains(r)))return result;
        var surfaces=own.Where(g=>g.renderer && members.Contains(g.renderer)).ToArray();
        if(surfaces.Length==0 || members.Any(r=>!surfaces.Any(g=>g.renderer==r)))return result;
        result.baseYWorld=own.Min(g=>g.vertices.Min(p=>p.y));
        result.topYWorld=surfaces.Max(g=>g.vertices.Max(p=>p.y));
        result.measured=true;result.actualMetres=(result.topYWorld-result.baseYWorld)/K;
        result.errorMetres=result.actualMetres-result.expectedMetres;
        // Dimension and support are separate evidence. A correctly sized
        // but floating deck must still fail the independently measured graph.
        result.supportVerified=own.All(g=>g.entry.verified);
        result.status=!result.supportVerified?"wymiar zmierzony; podparcie konstrukcji niepotwierdzone":
            Mathf.Abs(result.errorMetres)<=.03f?"wysokość nad własną podstawą zmierzona w tolerancji":
            "WYSOKOŚĆ POZA TOLERANCJĄ 3 cm";
        return result;
    }

    public static Result Inspect(Scene scene)
    {
        var roots=scene.GetRootGameObjects();
        return Inspect(roots.SelectMany(g=>g.GetComponentsInChildren<BraxObozStructureRecord>(true))
                .Where(r=>r && r.gameObject.activeInHierarchy).ToArray(),
            roots.SelectMany(g=>g.GetComponentsInChildren<BraxObozStructureGround>(true)).ToArray());
    }
    public static Result Inspect(BraxObozStructureRecord[] records,BraxObozStructureGround[] grounds)
    {
        Physics.SyncTransforms();
        var result=new Result {sceneGeometryMeasured=true,phoneVerified=false,records=records.Length};
        var findings=new List<string>();var geometry=new List<Geometry>();var meshes=new Dictionary<Mesh,MeshData>();
        var validGrounds=grounds.Where(ValidGround).ToArray();result.invalidGrounds=grounds.Length-validGrounds.Length;
        if(validGrounds.Length==0)findings.Add("Brak zweryfikowanego odnośnika do rzeczywistej siatki gruntu; żadna konstrukcja nie zostanie uznana za podpartą.");
        foreach(var record in records)
            for(int i=0;i<record.parts.Length;i++)Read(record,record.parts[i],i,meshes,geometry,findings);
        foreach(var part in geometry)Ground(part,validGrounds);
        var edges=new List<Connection>();var adjacency=new List<int>[geometry.Count];
        for(int i=0;i<adjacency.Length;i++)adjacency[i]=new List<int>();
        var ordered=Enumerable.Range(0,geometry.Count).Where(i=>geometry[i].entry.measured)
            .OrderBy(i=>geometry[i].bounds.min.x).ToArray();
        for(int left=0;left<ordered.Length;left++)
        {
            int a=ordered[left];var ga=geometry[a];
            for(int right=left+1;right<ordered.Length;right++)
            {
                int b=ordered[right];var gb=geometry[b];
                if(gb.bounds.min.x>ga.bounds.max.x+Tolerance)break;
                if(ga.renderer.gameObject.scene!=gb.renderer.gameObject.scene ||
                    BoundsDistanceSquared(ga.bounds,gb.bounds)>Tolerance*Tolerance)continue;
                if(Touches(ga,gb,out float gap,out bool measured))
                {
                    edges.Add(new Connection {a=a,b=b,separationUpperBoundMetres=gap});
                    adjacency[a].Add(b);adjacency[b].Add(a);
                }
                else if(!measured)result.unmeasuredPairChecks++;
            }
        }
        var queue=new Queue<int>();
        for(int i=0;i<geometry.Count;i++)
            if(geometry[i].entry.directGroundContact)
            {geometry[i].entry.connected=true;geometry[i].entry.connectionDepth=0;queue.Enqueue(i);}
        while(queue.Count>0)
        {
            int current=queue.Dequeue();
            foreach(int next in adjacency[current])
                if(!geometry[next].entry.connected)
                {
                    var e=geometry[next].entry;e.connected=true;e.parentPiece=current;
                    e.connectionDepth=geometry[current].entry.connectionDepth+1;queue.Enqueue(next);
                }
        }
        foreach(var g in geometry)
        {
            var e=g.entry;e.verified=e.measured && e.groundMeasured && e.connected && !e.embedded;
            if(!e.measured)result.unmeasuredPieces++;
            else
            {
                result.measuredPieces++;
                if(!e.groundMeasured)result.unmeasuredPieces++;
                if(!e.connected)result.unsupportedPieces++;
                if(e.embedded)result.embeddedPieces++;
                e.status=e.embedded?"GEOMETRIA PONIŻEJ GRUNTU > 3 cm":
                    !e.groundMeasured?"NIEZMIERZONE: niepełne promienie do gruntu":
                    e.connected?"podparcie zmierzone przez rzeczywiste styki części":
                    "BRAK ZMIERZONEJ DROGI PODPARCIA — kandydat pozostaje";
            }
        }
        var membership=records.SelectMany(r=>r.members ?? Array.Empty<Renderer>()).Where(r=>r).Distinct().ToArray();
        var renderers=new List<RendererEvidence>();
        foreach(var renderer in membership)
        {
            var parts=geometry.Where(g=>g.renderer==renderer).Select(g=>g.entry).ToArray();
            var entry=new RendererEvidence {renderer=renderer,path=Path(renderer.transform),pieces=parts.Length,
                measured=parts.Count(p=>p.measured),connected=parts.Count(p=>p.connected),
                embedded=parts.Count(p=>p.embedded),unmeasured=parts.Count(p=>!p.measured || !p.groundMeasured)};
            if(parts.Length==0)entry.unmeasured=1;
            entry.completeTriangleCoverage=CompleteCoverage(renderer,records);
            if(!entry.completeTriangleCoverage)entry.unmeasured++;
            entry.verified=entry.completeTriangleCoverage && parts.Length>0 && parts.All(p=>p.verified);
            entry.status=entry.verified?"zweryfikowane podparcie wszystkich jawnych części":
                "nieweryfikowane — zachowaj surowe kandydaty renderera";
            renderers.Add(entry);
        }
        result.registeredRenderers=renderers.Count;result.renderers=renderers.ToArray();
        result.unmeasuredRenderers=renderers.Count(r=>r.unmeasured>0);
        result.heights=records.Where(r=>r.expectedTopMetres>=0).Select(r=>Height(r,geometry)).ToArray();
        result.heightWarnings=result.heights.Count(h=>!h.measured || !h.supportVerified || Mathf.Abs(h.errorMetres)>.03f);
        result.batchesOverBudget=geometry.Count(g=>g.entry.budgetExceeded=="batch");
        result.islandsOverBudget=geometry.Count(g=>g.entry.budgetExceeded=="island");
        result.pieces=geometry.Select(g=>g.entry).ToArray();result.connections=edges.ToArray();
        result.findings=findings.ToArray();return result;
    }
}
