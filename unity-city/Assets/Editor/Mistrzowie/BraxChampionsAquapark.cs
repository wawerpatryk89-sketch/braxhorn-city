using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Owner 12.11 02:47-02:56 ("masz strefę spa, masz zjeżdżalnię, masz wszystko ... budujesz basen przez całą noc", "zrób kurwa
// taki basen, jak wstanę rano powiem, o kurwa, ty to zrobiłeś", "wykończenia w detale, wszystko najlepsze") and his aquapark
// picture of 12.11 00:04 (a tower with the bull on a stone column, a red and a yellow tube spiralling round it, three blue
// racers in the middle, a rock waterfall, palms, a curved pool). AQUAPARK BRAXHORN on the free lot of the old P1 reservation
// between road X400 and the park (world x 426..632, z 87..313; the region's ground is cut out there: legacy_foundation
// AQUAPARK_SITE, this generator lays every surface inside, flush with the sidewalks at .16):
//
// * the ENTRANCE PAVILION on road Z70 (reception, changing rooms with showers, SPA with the owner's four hot tubs, two
//   saunas and two massage rooms with his tables: BraxChampionsAquaparkBudynek),
// * the LAGOON (freeform, 1.30 m, stairs and ladders, underwater lights, swimming: BraxSwimmingZone strips),
// * the SLIDE TOWER (16 m, stone column with the bull mark) with the red tube, the yellow tube and three blue racers, all
//   RIDDEN (BraxChampionsZjezdzalnia: BraxChampionsAquaparkZjezdzalnie), the rock waterfall into the lagoon,
// * the KIDS' ZONE (rubber floor, a 35 cm splash pool with the owner's two playground slides, mushroom fountain),
// * two outdoor JACUZZI (bubbling), sun terraces with loungers you lie on and parasols, the POOL BAR, palms, flowers, lamps,
//   a glass fence round the lot.
// Nominal metres = real metres (x K once), world frame (the promenade furniture helpers work in it). Nothing coplanar, every
// floor / stair / pool shell has a collider, water never does.
public static partial class BraxChampionsAquapark
{
    // ---- the lot (world nominal) - legacy_foundation.AQUAPARK_SITE must match
    public const float CX=529f,CZ=200f,X0=426f,X1=632f,Z0=87f,Z1=313f,DY=.16f;
    const string Out="Assets/PlayableGenerated/Mistrzowie",Folder=Out+"/Meshes/Aquapark",Paczka="basen_01";
    static float K=>BraxClaudeOsiedle.K;
    public static int Colliders,Loungers,Parasols,Seats,SwimZones,Slides,Palms,Lamps,OwnerModels,OwnerMissing,Pools,Walkers;

    static Transform root,cols,seats,models;
    static BraxChampionsMeshBatch m,walk;
    static Material deck,plaza,coping,poolTile,poolFloor,waterline,teak,lawn,rubber,rubberYellow,rock,steel,graphite,glass,led,
        cushion,towel,canvas,canvasRed,white,black,blackGloss,stone,water,waterKids,waterSpa,uplight,bark,leaf,fountainWater;

    static Material S(string id,Color c,float r,float met,Color? e=null,bool t=false)=>BraxChampionsHarbour.Surface("aq_"+id,c,r,met,e,t);
    static Material Tex(string id,string slug,float metres,Color tint)=>BraxChampionsFoundation.SourceMaterial("aq_"+id,slug,metres,tint);

    static void Materials()
    {
        if(!BraxChampionsHarbour.Steel)BraxChampionsHarbour.Materials();
        steel=BraxChampionsHarbour.Steel;graphite=BraxChampionsHarbour.Graphite;led=BraxChampionsHarbour.Led;
        // pool deck: light anti-slip granite tiles (wet feet), the forecourt in the darker granite, coping in white marble
        // pool deck: large-format white porcelain (the warm long_white_tiles map neutralised to a cool white), the forecourt in
        // grey concrete pavers, the coping in white marble (marble_01 neutralised: no beige - the region's rule)
        deck=Tex("deck_white_porcelain","long_white_tiles",2.4f,new Color(.98f,1.03f,1.13f));
        plaza=Tex("plaza_grey_pavers","concrete_pavement_02",1.8f,new Color(.96f,1.05f,1.18f));
        coping=Tex("coping_white_marble","marble_01",1.2f,new Color(1.02f,1.15f,1.47f));
        // the shell: long white ceramic tiles with an aqua cast (the water reads turquoise over them), a navy mosaic waterline
        poolTile=Tex("pool_wall_tiles","long_white_tiles",1.0f,new Color(.80f,1.02f,1.10f));
        poolFloor=Tex("pool_floor_tiles","long_white_tiles",1.0f,new Color(.74f,.98f,1.08f));
        waterline=Tex("pool_waterline_mosaic","tiled_floor_001",.6f,new Color(.22f,.42f,.86f));
        teak=Tex("deck_silver_teak","teak_veneer",1.1f,new Color(.91f,1.20f,1.72f));
        lawn=BraxChampionsPromenadeFurniture.Lawn();
        // coloured EPDM safety floor of the kids' zone (the black rubber_tiles map stays black under any tint)
        rubber=S("kids_epdm_teal",new Color(.07f,.56f,.64f),.92f,0f);
        rubberYellow=S("kids_epdm_yellow",new Color(.98f,.76f,.10f),.92f,0f);
        rock=Tex("waterfall_rock","concrete_wall_008",2.4f,new Color(.86f,.80f,.72f));
        stone=Tex("tower_column_stone","sandstone_blocks_08",2.2f,new Color(.30f,.34f,.40f));
        cushion=BraxChampionsPromenade.Fabric("aq_lounger_cushion_white","cotton_jersey",new Color(1.20f,1.36f,1.46f),.6f);
        towel=BraxChampionsPromenade.Fabric("aq_towel_red","cotton_jersey",new Color(.92f,.10f,.12f),.6f);
        canvas=BraxChampionsPromenade.Fabric("aq_parasol_white","terlenka",new Color(1.21f,1.36f,1.53f),.9f,new Color(.38f,.38f,.36f));
        canvasRed=BraxChampionsPromenade.Fabric("aq_parasol_valance_red","terlenka",new Color(.80f,.08f,.10f),.9f);
        white=S("white_composite",new Color(.94f,.95f,.95f),.30f,.02f);
        black=S("black_steel",new Color(.035f,.037f,.04f),.36f,.65f);
        blackGloss=S("black_gloss_frp",new Color(.025f,.027f,.03f),.12f,.05f);
        glass=S("balustrade_glass",new Color(.80f,.90f,.94f,.26f),.04f,0f,null,true);
        uplight=S("underwater_led",new Color(.80f,.95f,1f),.2f,0f,new Color(.75f,1.15f,1.45f));
        bark=Tex("palm_planter_bark","brown_mud_leaves_01",1.0f,new Color(.62f,.58f,.52f));
        leaf=S("rockery_fern",new Color(.20f,.40f,.14f),.7f,0f);
        water=Water("aq_pool_water",new Color(.03f,.26f,.47f),new Color(.12f,.66f,.86f),.70f,.8f,0f);
        waterKids=Water("aq_kids_water",new Color(.07f,.40f,.58f),new Color(.20f,.78f,.92f),.55f,.6f,0f);
        waterSpa=Water("aq_jacuzzi_water",new Color(.05f,.30f,.50f),new Color(.16f,.70f,.88f),.66f,1.4f,1f);
        fountainWater=S("fountain_water",new Color(.90f,.95f,1f,.62f),.1f,0f,new Color(.30f,.32f,.34f),true);
        BuildingMaterials();
        SlideMaterials();
    }

    // the pools' moving water: the region's pool shader (Braxhorn/ChampionsP1PoolWater, analytic waves, no collider)
    static Material Water(string id,Color deep,Color shallow,float opacity,float ripple,float bubbles)
    {
        var sh=Shader.Find("Braxhorn/ChampionsP1PoolWater");
        if(!sh){BraxBuildQuality.Warn("AQUAPARK_WATER_SHADER_MISSING Braxhorn/ChampionsP1PoolWater - CityStaticPBR glass instead");return S(id,new Color(shallow.r,shallow.g,shallow.b,.55f),.05f,0f,null,true);}
        var w=new Material(sh){name="Aquapark "+id};
        w.SetColor("_DeepColor",deep);w.SetColor("_ShallowColor",shallow);w.SetFloat("_Opacity",opacity);w.SetFloat("_RippleStrength",ripple);w.SetFloat("_Bubbles",bubbles);
        w.renderQueue=3000;
        string path=Out+"/Materials/"+id+".mat";
        var old=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(old){EditorUtility.CopySerialized(w,old);UnityEngine.Object.DestroyImmediate(w);return old;}
        BraxZapisAssetu.Utworz(w,path);return w;
    }

    // ================================================================ helpers (world nominal)
    static Vector3 V(float lx,float y,float lz)=>new Vector3(CX+lx,y,CZ+lz);
    static Vector2 P(float lx,float lz)=>new Vector2(CX+lx,CZ+lz);
    static void B(Material mat,float x0,float x1,float y0,float y1,float z0,float z1,bool col=false)
    {
        if(x1-x0<1e-4f||y1-y0<1e-4f||z1-z0<1e-4f)return;
        m.BoxMinMax(mat,x0,x1,y0,y1,z0,z1);
        if(col)Col(x0,x1,y0,y1,z0,z1);
    }
    static void Col(float x0,float x1,float y0,float y1,float z0,float z1,string name="kolizja")
    {
        BraxChampionsMeshBatch.Collider(cols,name,new Vector3((x0+x1)*.5f,(y0+y1)*.5f,(z0+z1)*.5f),new Vector3(x1-x0,y1-y0,z1-z0));
        Colliders++;
    }
    static void ColRot(Vector3 c,Vector3 size,Quaternion r,string name="kolizja")
    {
        var o=new GameObject(name);o.transform.SetParent(cols,false);o.transform.localPosition=c*K;o.transform.localRotation=r;
        o.AddComponent<BoxCollider>().size=size*K;Colliders++;
    }
    static void Q(BraxChampionsMeshBatch b,Material mat,Vector3 a,Vector3 c1,Vector3 c2,Vector3 d,Vector3 n)
    {
        if(Vector3.Dot(Vector3.Cross(c1-a,c2-a),n)<0)b.Quad(mat,d,c2,c1,a);else b.Quad(mat,a,c1,c2,d);
    }
    static void T(BraxChampionsMeshBatch b,Material mat,Vector3 a,Vector3 c1,Vector3 c2,Vector3 n)
    {
        if(Vector3.Dot(Vector3.Cross(c1-a,c2-a),n)<0)b.Triangle(mat,a,c2,c1);else b.Triangle(mat,a,c1,c2);
    }
    static Transform Group(Transform parent,string name){var g=new GameObject(name).transform;g.SetParent(parent,false);return g;}
    static void Flush(string name,Transform parent=null){m.Build(parent??root,name,Folder,false);m=new BraxChampionsMeshBatch();}
    // walkable surfaces (deck, pool shells, stairs, tower floors): the same meshes are their colliders
    static void FlushWalk(string name,Transform parent=null)
    {
        var g=walk.Build(parent??root,name,Folder,false);walk=new BraxChampionsMeshBatch();
        foreach(var mf in g.GetComponentsInChildren<MeshFilter>(true))
            if(mf.sharedMesh&&mf.GetComponent<MeshRenderer>().sharedMaterial.renderQueue<3000){mf.gameObject.AddComponent<MeshCollider>().sharedMesh=mf.sharedMesh;Colliders++;}
    }

    // ================================================================ 2D geometry
    static float Area(IList<Vector2> p){float a=0;for(int i=0;i<p.Count;i++){var u=p[i];var w=p[(i+1)%p.Count];a+=u.x*w.y-w.x*u.y;}return a*.5f;}
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static bool InTri(Vector2 q,Vector2 a,Vector2 b,Vector2 c)=>Cross(b-a,q-a)>=-1e-7f&&Cross(c-b,q-b)>=-1e-7f&&Cross(a-c,q-c)>=-1e-7f;
    public static bool Inside(IList<Vector2> poly,Vector2 q)
    {
        bool r=false;for(int i=0,j=poly.Count-1;i<poly.Count;j=i++){var a=poly[i];var b=poly[j];if((a.y>q.y)!=(b.y>q.y)&&q.x<(b.x-a.x)*(q.y-a.y)/(b.y-a.y)+a.x)r=!r;}return r;
    }
    static List<Vector2> Ccw(IEnumerable<Vector2> src){var p=src.ToList();if(Area(p)<0)p.Reverse();return p;}

    /// <summary>Closed smooth outline through the control points (centripetal Catmull-Rom), sampled every ds m; CCW.</summary>
    public static List<Vector2> Blob(Vector2[] c,float ds)
    {
        var o=new List<Vector2>();int n=c.Length;
        for(int i=0;i<n;i++)
        {
            Vector2 p0=c[(i-1+n)%n],p1=c[i],p2=c[(i+1)%n],p3=c[(i+2)%n];
            float t0=0,t1=t0+Mathf.Sqrt((p1-p0).magnitude),t2=t1+Mathf.Sqrt((p2-p1).magnitude),t3=t2+Mathf.Sqrt((p3-p2).magnitude);
            int steps=Mathf.Max(2,Mathf.CeilToInt((p2-p1).magnitude/ds));
            for(int k=0;k<steps;k++)
            {
                float t=Mathf.Lerp(t1,t2,k/(float)steps);
                Vector2 A1=(t1-t)/(t1-t0)*p0+(t-t0)/(t1-t0)*p1,A2=(t2-t)/(t2-t1)*p1+(t-t1)/(t2-t1)*p2,A3=(t3-t)/(t3-t2)*p2+(t-t2)/(t3-t2)*p3;
                Vector2 B1=(t2-t)/(t2-t0)*A1+(t-t0)/(t2-t0)*A2,B2=(t3-t)/(t3-t1)*A2+(t-t1)/(t3-t1)*A3;
                o.Add((t2-t)/(t2-t1)*B1+(t-t1)/(t2-t1)*B2);
            }
        }
        return Ccw(o);
    }
    public static List<Vector2> RoundRect(float x0,float x1,float z0,float z1,float r,float ds=.6f)
    {
        var o=new List<Vector2>();
        var cs=new[]{new Vector2(x1-r,z0+r),new Vector2(x1-r,z1-r),new Vector2(x0+r,z1-r),new Vector2(x0+r,z0+r)};
        float[] a0={-90f,0f,90f,180f};
        for(int i=0;i<4;i++){int seg=Mathf.Max(3,Mathf.CeilToInt(r*Mathf.PI*.5f/ds));for(int k=0;k<seg;k++){float a=(a0[i]+90f*k/seg)*Mathf.Deg2Rad;o.Add(cs[i]+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r);}}
        return Ccw(o);
    }
    public static List<Vector2> Circle(Vector2 c,float r,int seg)
    {
        var o=new List<Vector2>();for(int k=0;k<seg;k++){float a=2f*Mathf.PI*k/seg;o.Add(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r);}return o;
    }
    /// <summary>Outline moved outward by d (CCW, smooth: per-vertex normals).</summary>
    public static List<Vector2> Offset(IList<Vector2> p,float d)
    {
        var o=new List<Vector2>(p.Count);int n=p.Count;
        for(int i=0;i<n;i++)
        {
            var e0=(p[i]-p[(i-1+n)%n]).normalized;var e1=(p[(i+1)%n]-p[i]).normalized;
            var n0=new Vector2(e0.y,-e0.x);var n1=new Vector2(e1.y,-e1.x);var nn=(n0+n1).normalized;
            float c=Mathf.Max(.5f,Vector2.Dot(nn,n0));o.Add(p[i]+nn*(d/c));
        }
        return o;
    }

    // several bridges can end at one outline vertex (two lawns bridged to the lot's corner): of its copies take the one whose
    // inside wedge holds the new bridge, else the bridges cross and the ears spill over the holes
    static float Wrap(float a){while(a<0f)a+=2f*Mathf.PI;while(a>=2f*Mathf.PI)a-=2f*Mathf.PI;return a;}
    static int PickCopy(List<Vector2> poly,int idx,Vector2 M)
    {
        var P=poly[idx];int n=poly.Count;
        for(int j=0;j<n;j++)
        {
            if(poly[j]!=P)continue;
            var pr=poly[(j-1+n)%n];var nx=poly[(j+1)%n];
            float aOut=Mathf.Atan2(nx.y-P.y,nx.x-P.x);
            float w=Wrap(Mathf.Atan2(pr.y-P.y,pr.x-P.x)-aOut),t=Wrap(Mathf.Atan2(M.y-P.y,M.x-P.x)-aOut);
            if(t>0f&&t<w)return j;
        }
        return idx;
    }

    /// <summary>Polygon with holes (outer CCW, holes inside, disjoint) -> one simple polygon (bridges), then ear clipping.</summary>
    public static List<int> Triangulate(List<Vector2> outer,List<List<Vector2>> holes,out List<Vector2> pts)
    {
        var poly=Ccw(outer);
        var hs=(holes??new List<List<Vector2>>()).Where(h=>h!=null&&h.Count>=3).Select(h=>{var c=h.ToList();if(Area(c)>0)c.Reverse();return c;})
            .OrderByDescending(h=>h.Max(q=>q.x)).ToList();
        foreach(var h in hs)
        {
            int mi=0;for(int i=1;i<h.Count;i++)if(h[i].x>h[mi].x)mi=i;
            var M=h[mi];float bx=float.MaxValue;int bi=-1;
            for(int i=0;i<poly.Count;i++)
            {
                var a=poly[i];var b=poly[(i+1)%poly.Count];
                if((a.y>M.y)==(b.y>M.y))continue;
                float x=a.x+(M.y-a.y)*(b.x-a.x)/(b.y-a.y);
                if(x<M.x-1e-6f||x>=bx)continue;
                bx=x;bi=a.x>b.x?i:(i+1)%poly.Count;
            }
            if(bi<0){BraxBuildQuality.Warn("AQUAPARK_TRIANGULATE hole outside the outline - skipped");continue;}
            var I=new Vector2(bx,M.y);var Pb=poly[bi];int pi=bi;float best=float.MaxValue;
            for(int j=0;j<poly.Count;j++)
            {
                var q=poly[j];if(j==bi||q.x<M.x||q==Pb)continue;
                var pr=poly[(j-1+poly.Count)%poly.Count];var nx=poly[(j+1)%poly.Count];
                if(Cross(q-pr,nx-q)>0)continue;                                          // convex: cannot block
                if(!InTri(q,M,I,Pb)&&!InTri(q,M,Pb,I))continue;
                float ang=Mathf.Abs(Mathf.Atan2(q.y-M.y,q.x-M.x));
                if(ang<best){best=ang;pi=j;}
            }
            pi=PickCopy(poly,pi,M);
            var np=new List<Vector2>(poly.Count+h.Count+2);
            for(int k=0;k<=pi;k++)np.Add(poly[k]);
            for(int k=0;k<=h.Count;k++)np.Add(h[(mi+k)%h.Count]);
            np.Add(poly[pi]);
            for(int k=pi+1;k<poly.Count;k++)np.Add(poly[k]);
            poly=np;
        }
        pts=poly;
        var idx=Enumerable.Range(0,poly.Count).ToList();var tris=new List<int>(poly.Count*3);
        int cur=0,guard=0;
        while(idx.Count>3&&guard++<poly.Count*poly.Count+100)
        {
            bool found=false;
            for(int s=0;s<idx.Count;s++)
            {
                int i=(cur+s)%idx.Count;
                int a=idx[(i-1+idx.Count)%idx.Count],b=idx[i],c=idx[(i+1)%idx.Count];
                Vector2 A=poly[a],Bv=poly[b],C=poly[c];
                if(Cross(Bv-A,C-Bv)<=1e-9f)continue;
                bool blocked=false;
                for(int j=0;j<idx.Count&&!blocked;j++)
                {
                    int q=idx[j];if(q==a||q==b||q==c)continue;var Qv=poly[q];
                    if((Qv-A).sqrMagnitude<1e-10f||(Qv-Bv).sqrMagnitude<1e-10f||(Qv-C).sqrMagnitude<1e-10f)continue;
                    if(InTri(Qv,A,Bv,C))blocked=true;
                }
                if(blocked)continue;
                tris.Add(a);tris.Add(b);tris.Add(c);idx.RemoveAt(i);cur=i%Mathf.Max(1,idx.Count);found=true;break;
            }
            if(!found)
            {
                // degenerate (collinear run / touching bridge): drop the flattest vertex and go on
                int w=0;float wa=float.MaxValue;
                for(int i=0;i<idx.Count;i++){var A=poly[idx[(i-1+idx.Count)%idx.Count]];var Bv=poly[idx[i]];var C=poly[idx[(i+1)%idx.Count]];float cr=Mathf.Abs(Cross(Bv-A,C-Bv));if(cr<wa){wa=cr;w=i;}}
                idx.RemoveAt(w);
            }
        }
        if(idx.Count==3){tris.Add(idx[0]);tris.Add(idx[1]);tris.Add(idx[2]);}
        return tris;
    }

    /// <summary>A flat surface (polygon with holes) at height y, facing up (or down: a ceiling).</summary>
    public static void Fill(BraxChampionsMeshBatch b,Material mat,List<Vector2> outer,List<List<Vector2>> holes,float y,bool down=false)
    {
        var tris=Triangulate(outer,holes,out var pts);
        var n=down?Vector3.down:Vector3.up;
        // area check: the triangles must cover exactly the outline minus the holes (no gaps, nothing over a hole)
        float want=Mathf.Abs(Area(outer))-(holes??new List<List<Vector2>>()).Where(h=>h!=null&&h.Count>=3).Sum(h=>Mathf.Abs(Area(h))),got=0f;
        for(int i=0;i+2<tris.Count;i+=3)got+=Mathf.Abs(Cross(pts[tris[i+1]]-pts[tris[i]],pts[tris[i+2]]-pts[tris[i]]))*.5f;
        if(Mathf.Abs(got-want)>Mathf.Max(.05f,want*.002f)&&System.Environment.GetEnvironmentVariable("AQUAPARK_DEBUG_FILL")=="1")
            System.IO.File.WriteAllText("fill_debug.json","{\"outer\":["+string.Join(",",outer.Select(q=>"["+q.x.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+q.y.ToString(System.Globalization.CultureInfo.InvariantCulture)+"]"))+"],\"holes\":["+
                string.Join(",",(holes??new List<List<Vector2>>()).Select(h=>"["+string.Join(",",h.Select(q=>"["+q.x.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+q.y.ToString(System.Globalization.CultureInfo.InvariantCulture)+"]"))+"]"))+"]}");
        if(Mathf.Abs(got-want)>Mathf.Max(.05f,want*.002f))BraxBuildQuality.Warn("AQUAPARK_TRIANGULATE_AREA "+mat.name+" want="+want.ToString("0.0")+" got="+got.ToString("0.0")+" holes="+(holes?.Count??0));
        for(int i=0;i+2<tris.Count;i+=3)
        {
            var a=pts[tris[i]];var c1=pts[tris[i+1]];var c2=pts[tris[i+2]];
            if(Mathf.Abs(Cross(c1-a,c2-a))<1e-8f)continue;
            T(b,mat,new Vector3(a.x,y,a.y),new Vector3(c1.x,y,c1.y),new Vector3(c2.x,y,c2.y),n);
        }
    }
    /// <summary>Band between two outlines of the same vertex count at heights yi (inner) / yo (outer), facing up.</summary>
    static void Band(BraxChampionsMeshBatch b,Material mat,IList<Vector2> inner,IList<Vector2> outer,float yi,float yo)
    {
        int n=inner.Count;
        for(int i=0;i<n;i++)
        {
            int j=(i+1)%n;
            Q(b,mat,new Vector3(inner[i].x,yi,inner[i].y),new Vector3(inner[j].x,yi,inner[j].y),new Vector3(outer[j].x,yo,outer[j].y),new Vector3(outer[i].x,yo,outer[i].y),Vector3.up);
        }
    }
    /// <summary>Vertical skin along a closed outline from y0 to y1, facing inward (inside=true: a pool wall) or outward.</summary>
    static void Skin(BraxChampionsMeshBatch b,Material mat,IList<Vector2> p,float y0,float y1,bool inward)
    {
        int n=p.Count;
        for(int i=0;i<n;i++)
        {
            int j=(i+1)%n;var e=(p[j]-p[i]);var no=new Vector3(e.y,0,-e.x).normalized;if(inward)no=-no;   // CCW: (e.y,-e.x) points out
            Q(b,mat,new Vector3(p[i].x,y0,p[i].y),new Vector3(p[j].x,y0,p[j].y),new Vector3(p[j].x,y1,p[j].y),new Vector3(p[i].x,y1,p[i].y),no);
        }
    }

    // ================================================================ GENERATE
    public static int Generate()
    {
        System.IO.Directory.CreateDirectory(Folder);
        Colliders=Loungers=Parasols=Seats=SwimZones=Slides=Palms=Lamps=OwnerModels=OwnerMissing=Pools=Walkers=0;
        Materials();
        var go=new GameObject("AQUAPARK BRAXHORN | laguna, wieża ze zjeżdżalniami, strefa dzieci, SPA, jacuzzi (owner 12.11 02:47)");
        root=go.transform;root.position=Vector3.zero;
        cols=Group(root,"kolizje");seats=Group(root,"siedziska (BraxSeat)");models=Group(root,"modele właściciela i rośliny");
        m=new BraxChampionsMeshBatch();walk=new BraxChampionsMeshBatch();
        Layout();
        Pools3();FlushWalk("aquapark | niecki: laguna, brodzik, jacuzzi (kolizje)");Flush("aquapark | niecki: woda, światła, drabinki");
        Ground();FlushWalk("aquapark | nawierzchnie: posadzka, tek, guma, plac (kolizje)");Flush("aquapark | trawniki, obrzeża, donice");
        Waterfall();FlushWalk("aquapark | wodospad: skały (kolizje)");Flush("aquapark | wodospad: woda, paprocie");
        Tower();FlushWalk("aquapark | wieża zjeżdżalni: podesty, schody (kolizje)");Flush("aquapark | wieża zjeżdżalni: konstrukcja, szkło, dach, byk");
        SlidesAll();Flush("aquapark | zjeżdżalnie: rury, tor wyścigowy, podpory");
        Kids();Flush("aquapark | strefa dzieci: zjeżdżalnie, grzybek, ogrodzenie");
        Furniture();Flush("aquapark | leżaki, parasole, bar, ratownicy, prysznice, latarnie");
        Fence();Flush("aquapark | ogrodzenie szklane, bramy");
        Building();Flush("aquapark | pawilon wejściowy: bryła");
        // life: the owner's girls in bikinis stroll the deck round the lagoon's south half (there and back)
        BraxChampionsFoundation.Optional("aquapark: spacerowiczki", () => { Walkers = BraxChampionsPostacie.Aquapark(Group(root, "spacerowiczki (BraxPieszy)"), WalkRoutes()); });
        Debug.Log("MISTRZOWIE_AQUAPARK walkers="+Walkers+" pools="+Pools+" swimZones="+SwimZones+" slides="+Slides+" loungers="+Loungers+" parasols="+Parasols+
                  " seats="+Seats+" palms="+Palms+" lamps="+Lamps+" ownerModels="+OwnerModels+" ownerMissing="+OwnerMissing+" colliders="+Colliders+
                  " site=x"+X0+".."+X1+" z"+Z0+".."+Z1);
        return 1;
    }
}
