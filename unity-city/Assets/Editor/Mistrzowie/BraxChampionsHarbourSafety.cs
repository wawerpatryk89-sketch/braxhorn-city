using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Five marina zones share two global low-poly safety railing meshes.
// The handrails are physical original-scale geometry; rope is a separate
// solid twisted-look 3D cable, not an animated ring rope (BraxLina).
// Pier fingers and passenger gangways stay OPEN at actual boarding positions.
// None of these railings touches the existing promenade, buildings, actors,
// arena, casino or other Unity regions.
public static class BraxChampionsHarbourSafety
{
    const string Output="Assets/PlayableGenerated/Mistrzowie";
    static float K=>BraxClaudeOsiedle.K;

    sealed class MeshStream
    {
        public readonly List<Vector3> vertices=new List<Vector3>();
        public readonly List<Vector2> uv=new List<Vector2>();
        public readonly List<int> indices=new List<int>();

        public void Tube(Vector3 start,Vector3 end,float radius,int sides,int divisions,float sag)
        {
            Vector3 dir=(end-start).normalized;
            if(dir.sqrMagnitude<.1f)return;
            var basis=Mathf.Abs(Vector3.Dot(dir,Vector3.up))>.9f?Vector3.forward:Vector3.up;
            Vector3 right=Vector3.Cross(dir,basis).normalized;
            Vector3 up=Vector3.Cross(right,dir).normalized;
            int n=vertices.Count;
            for(int i=0;i<=divisions;i++)
            {
                float t=(float)i/divisions;
                var centre=Vector3.Lerp(start,end,t)-Vector3.up*(sag*Mathf.Sin(Mathf.PI*t));
                for(int j=0;j<=sides;j++)
                {
                    float angle=j*2f*Mathf.PI/sides;
                    vertices.Add(centre+radius*(right*Mathf.Cos(angle)+up*Mathf.Sin(angle)));
                    uv.Add(new Vector2(t*Vector3.Distance(start,end)/(.34f*K),j/(float)sides));
                }
            }
            for(int i=0;i<divisions;i++)
            for(int j=0;j<sides;j++)
            {
                int a=n+i*(sides+1)+j,b=a+sides+1;
                indices.Add(a);indices.Add(b);indices.Add(a+1);
                indices.Add(a+1);indices.Add(b);indices.Add(b+1);
            }
        }

        public Mesh Create(string name)
        {
            if(indices.Count==0)throw new InvalidOperationException("Empty harbour railing mesh: "+name);
            var mesh=new Mesh{name=name,
                indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(vertices);
            mesh.SetUVs(0,uv);
            mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    sealed class Span
    {
        public Vector3 a,b;
        public string name;
    }
    static readonly List<Span> runs=new List<Span>();

    static float Deck(BraxChampionsFoundation.Pier pier,float z)
    {
        switch(pier.role)
        {
            case "quay_jetski":return -1.37f;
            case "quay_cruise":
            case "quay_ferry":return .16f;
            case "quay_ramp":return .16f-1.24f*Mathf.Clamp01((-z-150f)/20f);
            case "quay_ramp_jetski":return .16f-1.53f*Mathf.Clamp01((-z-150f)/20f);
            default:return -1.08f;
        }
    }

    static void Add(BraxChampionsFoundation.Pier pier,string label,
                    float x0,float z0,float x1,float z1)
    {
        if(Vector2.Distance(new Vector2(x0,z0),new Vector2(x1,z1))<.65f)return;
        var a=new Vector3(x0,Deck(pier,z0),z0)*K;
        var b=new Vector3(x1,Deck(pier,z1),z1)*K;
        runs.Add(new Span{name=label,a=a,b=b});
    }

    // Keep actual berthing openings in the main walk: never block their
    // 5m boarding connection or put a collider directly over a gangway.
    static void SplitWithGaps(BraxChampionsFoundation.Pier pier,string label,
                              float x,float from,float to,IEnumerable<float> gaps,float halfGap)
    {
        var holes=gaps.OrderBy(z=>z).ToArray();
        float current=from;
        foreach(float centre in holes)
        {
            float lo=Mathf.Max(from,centre-halfGap),hi=Mathf.Min(to,centre+halfGap);
            if(lo>current+.7f)Add(pier,label,x,current,x,lo);
            current=Mathf.Max(current,hi);
        }
        if(to>current+.7f)Add(pier,label,x,current,x,to);
    }

    static BraxChampionsFoundation.Pier Require(IDictionary<string,BraxChampionsFoundation.Pier> piers,string id)
    {
        if(!piers.TryGetValue(id,out var pier))
            throw new InvalidOperationException("Approved marina dock missing: "+id);
        return pier;
    }

    static T Save<T>(T asset,string path) where T:UnityEngine.Object
    {
        var old=AssetDatabase.LoadAssetAtPath<T>(path);
        if(old)
        {
            EditorUtility.CopySerialized(asset,old);
            UnityEngine.Object.DestroyImmediate(asset);
            EditorUtility.SetDirty(old);
            return old;
        }
        AssetDatabase.CreateAsset(asset,path);
        return asset;
    }

    static Material Material(string name,Color color,float rough,float metal)
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("Harbour safety needs CityStaticPBR shader");
        var m=new Material(shader){name="BRAXHORN "+name,enableInstancing=true};
        m.SetColor("_Color",color);m.SetFloat("_Roughness",rough);
        m.SetFloat("_Metallic",metal);m.SetFloat("_VertexTint",0);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        return Save(m,Output+"/Materials/harbour_"+name+".mat");
    }

    public static void Generate(Transform harbour,BraxChampionsFoundation.Pier[] approvedPiers)
    {
        if(!harbour||approvedPiers==null||approvedPiers.Length!=56)
            throw new InvalidOperationException("Safety railings require the approved 56 pier pieces");
        var p=approvedPiers.ToDictionary(x=>x.id);
        runs.Clear();
        // Main yacht walk: two rows of braided rope with open bays for 9
        // opposite-side fingers. The shared spine connects all 15 berths.
        var y=Require(p,"YACHT_MAIN_SPINE");
        var fingers=Enumerable.Range(0,9).Select(i=>-179.9f-27.2f*i).ToArray();
        SplitWithGaps(y,"Yacht outer west",y.x,y.z,y.z+y.d,fingers,2.9f);
        SplitWithGaps(y,"Yacht outer east",y.x+y.w,y.z,y.z+y.d,fingers,2.9f);
        Add(y,"Yacht seaward head safety",y.x,y.z,y.x+y.w,y.z);
        var yr=Require(p,"YACHT_ACCESS_FROM_SHORE");
        Add(yr,"Yacht access west rope",yr.x,yr.z,yr.x,yr.z+yr.d);
        Add(yr,"Yacht access east rope",yr.x+yr.w,yr.z,yr.x+yr.w,yr.z+yr.d);

        // The 10-motorboat basin is a real U. Guard the OUTER sides, not
        // the finger docks where people step aboard.
        var mw=Require(p,"MOTOR_U_WEST");
        var me=Require(p,"MOTOR_U_EAST");
        Add(mw,"Motorboat U outer west",mw.x,mw.z,mw.x,mw.z+mw.d);
        Add(me,"Motorboat U outer east",me.x+me.w,me.z,me.x+me.w,me.z+me.d);
        var ml=Require(p,"MOTOR_LINK");
        Add(ml,"Motorboat service west",ml.x,ml.z,ml.x,ml.z+ml.d);
        Add(ml,"Motorboat service east",ml.x+ml.w,ml.z,ml.x+ml.w,ml.z+ml.d);
        var mr=Require(p,"MOTOR_ACCESS_FROM_SHORE");
        Add(mr,"Motorboat ramp west",mr.x,mr.z,mr.x,mr.z+mr.d);
        Add(mr,"Motorboat ramp east",mr.x+mr.w,mr.z,mr.x+mr.w,mr.z+mr.d);

        // Low watersports dock: keep all ten jetski boarding fingers open.
        var ski=Require(p,"JETSKI_LOW_MAIN");
        var skiFingers=Enumerable.Range(0,6).Select(i=>-177f-17.7f*i).ToArray();
        SplitWithGaps(ski,"Watersports outer west",ski.x,ski.z,ski.z+ski.d,skiFingers,2.1f);
        SplitWithGaps(ski,"Watersports outer east",ski.x+ski.w,ski.z,ski.z+ski.d,skiFingers,2.1f);
        var sr=Require(p,"JETSKI_ACCESS_FROM_SHORE");
        Add(sr,"Jetski ramp west",sr.x,sr.z,sr.x,sr.z+sr.d);
        Add(sr,"Jetski ramp east",sr.x+sr.w,sr.z,sr.x+sr.w,sr.z+sr.d);

        // Passenger ship quays have sturdy posts + TWO rope runs at 0.53m
        // and 0.98m high. Open only at actual ship-side gangway bridges.
        var ferry=Require(p,"FERRY_BOARDING_QUAY");
        Add(ferry,"Ferry landward safety",ferry.x,ferry.z,ferry.x,ferry.z+ferry.d);
        SplitWithGaps(ferry,"Ferry vessel side",ferry.x+ferry.w,
                      ferry.z,ferry.z+ferry.d,new[]{-312f},5f);
        var cruise=Require(p,"CRUISE_PASSENGER_QUAY");
        Add(cruise,"Cruise landward safety",cruise.x,cruise.z,cruise.x,cruise.z+cruise.d);
        SplitWithGaps(cruise,"Cruise embarkation side",cruise.x+cruise.w,
                      cruise.z,cruise.z+cruise.d,new[]{-273f,-397f},6f);

        Directory.CreateDirectory(Output+"/Meshes");
        Directory.CreateDirectory(Output+"/Materials");
        var owner=new GameObject("BRAXHORN | marina real marine rope and stainless safety railings").transform;
        owner.SetParent(harbour,false);
        var steel=new MeshStream();
        var rope=new MeshStream();
        foreach(var run in runs)
        {
            float length=Vector3.Distance(run.a,run.b);
            int pieces=Mathf.Max(1,Mathf.CeilToInt(length/(5.0f*K)));
            for(int n=0;n<=pieces;n++)
            {
                Vector3 baseAt=Vector3.Lerp(run.a,run.b,(float)n/pieces);
                steel.Tube(baseAt+.04f*Vector3.up*K,
                           baseAt+1.14f*Vector3.up*K,.047f*K,8,1,0f);
            }
            for(int n=0;n<pieces;n++)
            {
                Vector3 a=Vector3.Lerp(run.a,run.b,(float)n/pieces);
                Vector3 b=Vector3.Lerp(run.a,run.b,(float)(n+1)/pieces);
                foreach(float ropeHeight in new[]{.54f,1.04f})
                    rope.Tube(a+Vector3.up*K*ropeHeight,
                              b+Vector3.up*K*ropeHeight,.025f*K,8,4,.042f*K);
            }
            // One slim physical barrier per continuous run, never over
            // a berth opening, gangway or original beachfront promenade.
            var hit=new GameObject("RAIL_COLLIDER | "+run.name);
            hit.transform.SetParent(owner,false);
            hit.transform.position=(run.a+run.b)*.5f+Vector3.up*.60f*K;
            var dir=run.b-run.a;dir.y=0;
            hit.transform.rotation=Quaternion.LookRotation(dir.normalized,Vector3.up);
            var c=hit.AddComponent<BoxCollider>();
            c.size=new Vector3(.12f*K,1.18f*K,dir.magnitude);
        }
        var steelMesh=steel.Create("BRAXHORN marina reusable low-poly steel posts");
        var ropeMesh=rope.Create("BRAXHORN marina braided-look 3D safety ropes");
        var savedSteel=Save(steelMesh,Output+"/Meshes/harbour_safety_steel.asset");
        var savedRope=Save(ropeMesh,Output+"/Meshes/harbour_safety_rope.asset");
        var steelMat=Material("safety_stainless",new Color(.49f,.52f,.55f,1f),.32f,.84f);
        var ropeMat=Material("safety_natural_rope",new Color(.70f,.60f,.43f,1f),.81f,.02f);
        foreach(var item in new[]{(savedSteel,steelMat,"steel posts"),(savedRope,ropeMat,"sagging natural rope")})
        {
            var o=new GameObject("Marina safety | "+item.Item3);
            o.transform.SetParent(owner,false);
            o.AddComponent<MeshFilter>().sharedMesh=item.Item1;
            var renderer=o.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=item.Item2;
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=false;
        }
        Debug.Log("CHAMPIONS_MARINA_SAFETY rail_runs="+runs.Count+
                  " colliders="+runs.Count+" steel_vertices="+steel.vertices.Count+
                  " rope_vertices="+rope.vertices.Count+
                  " boarding_bays_and_ship_gangways=OPEN promenade=UNMODIFIED");
    }
}
