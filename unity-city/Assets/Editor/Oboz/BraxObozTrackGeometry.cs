using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Shared physical boundary for tartan, road and paving. Clipping against this
// same 128-edge polygon removes the old deliberately omitted boundary tiles.
public static partial class BraxObozEnvironment
{
    internal const int TrackSegments=128;
    internal const float TrackSurfaceY=.09f; // same as the retained approach road
    internal const float TrackWearThickness=.01f;
    internal const float TrackPaintOffset=.001f;
    internal const float WalkwaySurfaceY=.016f;
    internal const float TrackApproachRun=1f;
    internal const float TrackFineGrid=.25f;
    // Owner's global rule: real metres BEFORE K, including the sprint track.
    // The previous 100/K silently shortened it to 77.27975 source metres.
    internal const float actualLengthMetres=100f;
    internal const float actualLaneWidthMetres=1.5f;

    static List<Vector2> TrackEllipse(bool outer)
    {
        float extra=outer?4f*1.2f:0f;
        float rx=BraxObozCampusLayout.PlotRadiusX-8.8f+extra;
        float rz=BraxObozCampusLayout.PlotRadiusZ-8.8f+extra;
        return Enumerable.Range(0,TrackSegments).Select(i=>new Vector2(
            Mathf.Cos(i*Mathf.PI*2f/TrackSegments)*rx,
            BraxObozCampusLayout.PlotCenterZ+Mathf.Sin(i*Mathf.PI*2f/TrackSegments)*rz)).ToList();
    }
    static readonly List<Vector2> innerTrack=TrackEllipse(false),outerTrack=TrackEllipse(true);
    static readonly List<Vector2> straightTrack=RectanglePolygon(68f-3f,68f+3f,-24f-50f,-24f+50f);
    static readonly List<Vector2> straightInOval=straightTrack
        .Select(p=>new Vector2(p.x,p.y-BraxObozCampusLayout.PlotCenterZ)).ToList();

    static List<Vector2> RectanglePolygon(float x0,float x1,float z0,float z1)
    {
        return new List<Vector2>{new Vector2(x0,z0),new Vector2(x1,z0),
            new Vector2(x1,z1),new Vector2(x0,z1)};
    }
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;

    // Retain the inside (left) or outside (right) of an oriented edge. Every
    // shape is convex and counterclockwise in the source X/Z plane.
    internal static List<Vector2> ClipHalfPlane(IReadOnlyList<Vector2> input,
        Vector2 a,Vector2 b,bool inside)
    {
        var output=new List<Vector2>();if(input.Count==0)return output;
        float sign=inside?1f:-1f;Vector2 previous=input[input.Count-1];
        float previousDistance=sign*Cross(b-a,previous-a);
        bool previousIn=previousDistance>=-1e-6f;
        foreach(var current in input)
        {
            float distance=sign*Cross(b-a,current-a);bool currentIn=distance>=-1e-6f;
            if(currentIn!=previousIn)
            {
                float t=previousDistance/(previousDistance-distance);
                output.Add(Vector2.LerpUnclamped(previous,current,t));
            }
            if(currentIn)output.Add(current);
            previous=current;previousDistance=distance;previousIn=currentIn;
        }
        // Repeated intersection vertices otherwise produce degenerate faces.
        for(int i=output.Count-1;i>=0 && output.Count>1;i--)
            if((output[i]-output[(i+1)%output.Count]).sqrMagnitude<1e-12f)output.RemoveAt(i);
        return output;
    }
    static List<Vector2> Intersection(List<Vector2> source,IReadOnlyList<Vector2> clip)
    {
        var result=source;
        for(int i=0;i<clip.Count && result.Count>0;i++)
            result=ClipHalfPlane(result,clip[i],clip[(i+1)%clip.Count],true);
        return result;
    }
    static List<List<Vector2>> SubtractConvex(List<Vector2> source,IReadOnlyList<Vector2> clip)
    {
        var pieces=new List<List<Vector2>>();var remainder=source;
        for(int i=0;i<clip.Count && remainder.Count>=3;i++)
        {
            var outside=ClipHalfPlane(remainder,clip[i],clip[(i+1)%clip.Count],false);
            if(PolygonArea(outside)>1e-7f)pieces.Add(outside);
            remainder=ClipHalfPlane(remainder,clip[i],clip[(i+1)%clip.Count],true);
        }
        return pieces;
    }
    internal static float PolygonArea(IReadOnlyList<Vector2> polygon)
    {
        if(polygon.Count<3)return 0f;
        float sum=0;var origin=polygon[0];
        for(int i=0;i<polygon.Count;i++)sum+=Cross(polygon[i]-origin,polygon[(i+1)%polygon.Count]-origin);
        return Mathf.Abs(sum)*.5f;
    }
    internal static List<List<Vector2>> OutsideRunningTracks(List<Vector2> polygon)
    {
        // Rectangle minus an annulus = its inner intersection plus the pieces
        // outside the outer loop. Then remove the independent sprint track.
        var pieces=SubtractConvex(polygon,outerTrack);
        var inner=Intersection(polygon,innerTrack);
        if(PolygonArea(inner)>1e-7f)pieces.Add(inner);
        return pieces.SelectMany(p=>SubtractConvex(p,straightTrack)).ToList();
    }
    internal static List<List<Vector2>> ClipOvalAtSprint(List<Vector2> polygon)
    {
        return SubtractConvex(polygon,straightInOval);
    }
    static float EdgeDistance(Vector2 point,IReadOnlyList<Vector2> polygon)
    {
        float closest=float.PositiveInfinity;
        for(int i=0;i<polygon.Count;i++)
        {
            var a=polygon[i];var ab=polygon[(i+1)%polygon.Count]-a;
            float t=Mathf.Clamp01(Vector2.Dot(point-a,ab)/Mathf.Max(1e-10f,ab.sqrMagnitude));
            closest=Mathf.Min(closest,(point-(a+ab*t)).sqrMagnitude);
        }
        return Mathf.Sqrt(closest);
    }
    internal static float WalkwayHeightAt(Vector2 point)
    {
        float distance=Mathf.Min(EdgeDistance(point,innerTrack),
            Mathf.Min(EdgeDistance(point,outerTrack),EdgeDistance(point,straightTrack)));
        float blend=Mathf.Clamp01(1f-distance/TrackApproachRun);
        // A linear grade has a maximum slope of only 7.4%. Heights on a shared
        // edge are identical, including every clipped track boundary vertex.
        return Mathf.Lerp(WalkwaySurfaceY,TrackSurfaceY,blend);
    }

    // Closed physical volume: explicit underside, top and each side. It is
    // also used for clipped timber boards and the supported octagonal mat.
    internal static GameObject PolygonVolume(Transform parent,string label,List<Vector2> polygon,
        float bottom,Func<Vector2,float> height,Material material,bool solid)
    {
        if(polygon.Count<3 || PolygonArea(polygon)<1e-7f)return null;
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
        int n=polygon.Count;
        foreach(var p in polygon){vertices.Add(P(p.x,height(p),p.y));uv.Add(p*K);}
        foreach(var p in polygon){vertices.Add(P(p.x,bottom,p.y));uv.Add(p*K);}
        for(int i=1;i<n-1;i++)
        {
            indices.Add(0);indices.Add(i+1);indices.Add(i); // top: +Y
            indices.Add(n);indices.Add(n+i);indices.Add(n+i+1); // bottom: -Y
        }
        for(int i=0;i<n;i++)
        {
            int j=(i+1)%n,k=vertices.Count;
            var a=polygon[i];var b=polygon[j];float length=Vector2.Distance(a,b)*K;
            vertices.Add(P(a.x,bottom,a.y));vertices.Add(P(a.x,height(a),a.y));
            vertices.Add(P(b.x,height(b),b.y));vertices.Add(P(b.x,bottom,b.y));
            uv.Add(Vector2.zero);uv.Add(new Vector2(0,(height(a)-bottom)*K));
            uv.Add(new Vector2(length,(height(b)-bottom)*K));uv.Add(new Vector2(length,0));
            indices.Add(k);indices.Add(k+1);indices.Add(k+2);
            indices.Add(k);indices.Add(k+2);indices.Add(k+3);
        }
        var mesh=new Mesh{name=label};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        var go=Group(label,parent,Vector3.zero).gameObject;
        go.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh);
        go.AddComponent<MeshRenderer>().sharedMaterial=material;
        if(solid)go.AddComponent<MeshCollider>().sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;
        return go;
    }

    static IEnumerable<float> GridEdges(float min,float max,float step)
    {
        yield return min;
        for(float value=(Mathf.Floor(min/step)+1)*step;value<max-1e-5f;value+=step)yield return value;
        yield return max;
    }
    static int TrackAwarePaving(Transform parent,float x0,float x1,float z0,float z1,
        Material material,bool curb)
    {
        int created=0;
        void Piece(float ax,float bx,float az,float bz)
        {
            foreach(var polygon in OutsideRunningTracks(RectanglePolygon(ax,bx,az,bz)))
            {
                PolygonVolume(parent,curb?"Drewniane obrzeże ścieżki":"Oryginalne płyty slab_tiles 2K",polygon,0,p=>{
                    float y=WalkwayHeightAt(p);
                    float remaining=1f-(y-WalkwaySurfaceY)/(TrackSurfaceY-WalkwaySurfaceY);
                    return y+(curb?.04f*Mathf.Clamp01(remaining):0f);
                },material,false);
                created++;
            }
        }
        // All fine cells use a WORLD-aligned grid. Neighbouring path rectangles
        // share the same edge samples, with no height cracks or arbitrary gaps.
        var xs=GridEdges(x0,x1,4f).ToArray();var zs=GridEdges(z0,z1,4f).ToArray();
        for(int x=0;x<xs.Length-1;x++)for(int z=0;z<zs.Length-1;z++)
        {
            if(!IntersectsRunningTrack(xs[x],xs[x+1],zs[z],zs[z+1],TrackApproachRun+.05f))
            {Piece(xs[x],xs[x+1],zs[z],zs[z+1]);continue;}
            var fineX=GridEdges(xs[x],xs[x+1],TrackFineGrid).ToArray();
            var fineZ=GridEdges(zs[z],zs[z+1],TrackFineGrid).ToArray();
            for(int a=0;a<fineX.Length-1;a++)for(int b=0;b<fineZ.Length-1;b++)
                Piece(fineX[a],fineX[a+1],fineZ[b],fineZ[b+1]);
        }
        return created;
    }

    static void TrackClippedRoad(Transform parent)
    {
        // Retain the approach road's exact footprint and .09 m top. Tartan
        // owns the crossing, so two coplanar materials never flicker there.
        foreach(var piece in OutsideRunningTracks(RectanglePolygon(-3.5f,3.5f,-139f,-109f)))
            PolygonVolume(parent,"Droga dojazdowa — ciągły styk z tartanem",piece,0,p=>TrackSurfaceY,campusAsphalt,true);
    }

    static void SolidTrackBand(Transform parent,string label,float ax0,float az0,float ax1,float az1,
        float bottom,float top,Material material)
    {
        for(int i=0;i<TrackSegments;i++)
        {
            float a=i*Mathf.PI*2f/TrackSegments,b=(i+1)*Mathf.PI*2f/TrackSegments;
            // Inner(a), outer(a), outer(b), inner(b) is CCW in X/Z.
            var polygon=new List<Vector2>{
                new Vector2(Mathf.Cos(a)*ax0,Mathf.Sin(a)*az0),
                new Vector2(Mathf.Cos(a)*ax1,Mathf.Sin(a)*az1),
                new Vector2(Mathf.Cos(b)*ax1,Mathf.Sin(b)*az1),
                new Vector2(Mathf.Cos(b)*ax0,Mathf.Sin(b)*az0)};
            // The corrected 100 m source track meets the oval at its southern
            // end. The sprint owns that common area at every physical layer,
            // including paint; no coplanar red/red or white/white duplicates.
            foreach(var piece in ClipOvalAtSprint(polygon))
                PolygonVolume(parent,label,piece,bottom,p=>top,material,false);
        }
    }

    internal static GameObject CreateSprintSurface(Transform parent,Material red)
    {
        return Box(parent,"Tartan — 100 m przed K, 129,4 jednostki świata",
            new Vector3(0,TrackSurfaceY-TrackWearThickness*.5f,0),
            new Vector3(4f*actualLaneWidthMetres,TrackWearThickness,actualLengthMetres),red);
    }

    static void BuildRunningTracks()
    {
        var red=BraxObozMaterials.OwnerTrackPbr();
        var white=BraxObozMaterials.Plain("bieznia-biale-linie",new Color(.95f,.95f,.91f),.18f);
        var oval=Group("BIEZNIA · pelna czerwona petla przy nowym plocie",root,P(0,0,BraxObozCampusLayout.PlotCenterZ));
        const float laneWidth=1.2f;
        float innerX=BraxObozCampusLayout.PlotRadiusX-8.8f;
        float innerZ=BraxObozCampusLayout.PlotRadiusZ-8.8f;
        for(int lane=0;lane<4;lane++)
        {
            float x=innerX+lane*laneWidth,z=innerZ+lane*laneWidth;
            SolidTrackBand(oval,"Podbudowa toru "+(lane+1),x,z,x+laneWidth,z+laneWidth,
                0,TrackSurfaceY-TrackWearThickness,gravel);
            SolidTrackBand(oval,"Tartan toru "+(lane+1),x,z,x+laneWidth,z+laneWidth,
                TrackSurfaceY-TrackWearThickness,TrackSurfaceY,red);
        }
        for(int lane=0;lane<=4;lane++)
        {
            float x=innerX+lane*laneWidth,z=innerZ+lane*laneWidth;
            float lo=lane==0?0f:lane==4?-.05f:-.025f;
            float hi=lane==0?.05f:lane==4?0f:.025f;
            SolidTrackBand(oval,"Biała linia pętli "+lane,x+lo,z+lo,x+hi,z+hi,
                TrackSurfaceY+TrackPaintOffset*.5f,TrackSurfaceY+TrackPaintOffset,white);
        }
        CombineDecoration(oval);
        foreach(var filter in oval.GetComponentsInChildren<MeshFilter>())
            if(filter.GetComponent<MeshRenderer>().sharedMaterial==red)
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
        var ovalRecord=BraxObozPlacementRecord.For(oval);
        ovalRecord.SetMembers(oval.GetComponentsInChildren<Renderer>(true));
        ovalRecord.support=BraxObozPlacementRecord.SupportKind.Ground;ovalRecord.label="bieżnia owalna — fizyczna podbudowa od gruntu";
        ovalRecord.contactPoint=Group("Pomiar podbudowy toru",oval,P(innerX+.6f,0,0));
        ovalRecord.heightAboveFloorMetres=TrackSurfaceY;
        ovalRecord.heightReference=(int)BraxObozPlacement.HeightReference.Top;
        ovalRecord.heightRenderers=oval.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterial==red).Cast<Renderer>().ToArray();

        var straight=Group("BIEZNIA · 100 metrow · cztery tory",root,P(68,0,-24));
        float length=actualLengthMetres,half=length*.5f,sprintLane=actualLaneWidthMetres,width=4f*sprintLane;
        Box(straight,"Podbudowa bieżni 100 m",new Vector3(0,(TrackSurfaceY-TrackWearThickness)*.5f,0),
            new Vector3(width,TrackSurfaceY-TrackWearThickness,length),gravel,false);
        var sprintSurface=CreateSprintSurface(straight,red);
        float lineWidth=.055f,lineDepth=.12f;
        for(int i=0;i<=4;i++)
        {
            float x=-width*.5f+sprintLane*i+(i==0?lineWidth*.5f:i==4?-lineWidth*.5f:0);
            Box(straight,"Linia oddzielająca tor "+i,new Vector3(x,TrackSurfaceY+.0015f,0),
                new Vector3(lineWidth,.001f,length),white,false);
        }
        foreach(float end in new[]{-1f,1f})
            Box(straight,end<0?"START 100 m":"META 100 m",
                new Vector3(0,TrackSurfaceY+.0015f,end*(half-lineDepth*.5f)),
                new Vector3(width,.001f,lineDepth),white,false);
        for(int metres=10;metres<100;metres+=10)
            Box(straight,"Znacznik "+metres+" m",new Vector3(width*.5f-.20f,
                TrackSurfaceY+.0015f,-half+metres),new Vector3(.23f,.001f,.08f),white,false);
        var straightRecord=BraxObozPlacementRecord.For(straight);
        straightRecord.SetMembers(straight.GetComponentsInChildren<Renderer>(true));
        straightRecord.support=BraxObozPlacementRecord.SupportKind.Ground;
        straightRecord.label="bieżnia 100 m — podbudowa i znaczniki na tartanie";
        straightRecord.heightAboveFloorMetres=TrackSurfaceY;
        straightRecord.heightReference=(int)BraxObozPlacement.HeightReference.Top;
        straightRecord.heightRenderers=new[]{sprintSurface.GetComponent<Renderer>()};
        straightRecord.expectedDimensionMetres=actualLengthMetres;
        straightRecord.dimensionAxis=(int)BraxObozPlacement.Axis.Z;
        straightRecord.dimensionFrame=straight;
        // Sign is separate so its feet and wall-mount records are not confused
        // with the measured physical length of the track's surface.
        // Extending the physical sprint to 100 source metres would put its
        // former sign on the oval. Keep both feet outside either track.
        Sign(root,"BRAXHORN · 100 M",new Vector3(61,2f,-70),width);
        Debug.Log("BRAXHORN_OBOZ_BIEZNIE podbudowaOdGruntu=true poziom=.09 podejście=1m"+
            " prostaPrzedK="+length+" prostaJednostkiŚwiata="+(length*K)+
            " granicaWspólna=128 odcinków; przejście NavMesh/telefon do weryfikacji");
    }
}
