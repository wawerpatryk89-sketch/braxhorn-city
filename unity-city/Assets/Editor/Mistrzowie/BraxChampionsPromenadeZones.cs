using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Owner 10.11 18:12, board "PROMENADA NADMORSKA - PROJEKT ROZBUDOWY": "strefy rekreacyjne co 200-300 m" (strefa relaksu
// z pergolami, siłownia plenerowa, plac zabaw), "2 okrągłe placyki z fontanną". The zones lie on the lawn behind the
// promenade (promenade_layout.ZONES / ZONE_PATHS; the foundation leaves their rectangles and the paths to them open),
// every surface flush with the promenade at .16, built here from code with real materials:
//   - paths: promenade stone between flush granite edging, from the promenade to the zone,
//   - PLAC_FONTANNA_*: a round granite plaza (R 9 m) with a dark granite band, lawn corners, a two-tier granite fountain
//     with water and a jet, benches on the diagonals facing the fountain, lamps, round planters with flowers,
//   - STREFA_RELAKSU: silver-teak deck with a granite border, three black aluminium pergolas with teak slat roofs over
//     lounge sets (sofa, two armchairs with white cushions, teak coffee table), planters, lamps,
//   - SILOWNIA_PLENEROWA: rubber safety tiles, pull-up station, monkey bars, dip bars, wall bars, sit-up bench, push-up
//     bars, benches, lamps,
//   - PLAC_ZABAW: rubber surface inside a low picket fence with a gate, slide tower with a roof, ladder and stainless
//     slide, swings, two spring riders, a sandpit (real sand: footprints, BraxChampionsSandSteps), benches for parents.
// No coplanar faces of different materials, nothing 1-5 mm apart (zero migotania); colliders for walking and for every
// piece of equipment. Everything in nominal metres in one batch per zone (x K once, BraxChampionsMeshBatch).
public static class BraxChampionsPromenadeZones
{
    const string Output="Assets/PlayableGenerated/Mistrzowie";
    const float Y=.16f,EdgeW=.25f;
    public const float PlazaR=9.0f,PlazaBand=.4f,EntryHalf=3.0f;
    static Material walk,lawn,plazaStone,plazaBand,water,curtain,jet,gymRubber,playRubber,sand,hdpeBlue,hdpeRed,hdpeYellow,hdpeGreen;
    public static int Zones,Plazas,Fountains,Pergolas,Sofas,Armchairs,Tables,GymStations,Slides,Swings,Riders,Sandpits,Pickets,Benches,Lamps,Planters;

    static Material Kerb=>BraxChampionsPromenadeFurniture.Kerb;
    static Material Teak=>BraxChampionsPromenadeFurniture.Teak;
    static Material G=>BraxChampionsHarbour.Graphite;
    static Material St=>BraxChampionsHarbour.Steel;

    static void Materials()
    {
        if(!BraxChampionsHarbour.Steel)BraxChampionsHarbour.Materials();
        if(!BraxChampionsPromenadeFurniture.Teak)BraxChampionsPromenadeFurniture.Materials();
        if(!BraxChampionsPromenade.Cushion)BraxChampionsPromenade.Materials();
        walk=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/prom_paving.mat");
        if(!walk)walk=BraxChampionsFoundation.LibraryAcg("prom_zone_path","PavingStones126A",Color.white,4.5f);   // = the promenade stone
        lawn=BraxChampionsPromenadeFurniture.Lawn();
        // the plaza is laid in the promenade's own light vein-cut stone (no warm granite: granite_tile_02/03 read brown)
        plazaStone=walk;
        plazaBand=BraxChampionsFoundation.SourceMaterial("prom_plaza_dark_band","granite_tile",.8f,new Color(.86f,.87f,.89f));
        water=BraxChampionsHarbour.Surface("prom_fountain_water",new Color(.09f,.33f,.42f),.04f,.10f);
        curtain=BraxChampionsHarbour.Surface("prom_fountain_falling_water",new Color(.74f,.90f,.97f,.42f),.03f,0f,new Color(.05f,.08f,.10f),true);
        jet=BraxChampionsHarbour.Surface("prom_fountain_jet",new Color(.86f,.95f,1f,.62f),.03f,0f,new Color(.10f,.14f,.16f),true);
        gymRubber=BraxChampionsFoundation.SourceMaterial("prom_gym_rubber_tiles","rubber_tiles",1.0f,new Color(1.25f,1.25f,1.30f));
        // poured EPDM safety surface, blue granules (rubber_tiles is black interlocking tiles: kept for the gym)
        playRubber=BraxChampionsHarbour.Surface("prom_playground_epdm_blue",new Color(.15f,.36f,.60f),.93f,0f);
        sand=BraxChampionsFoundation.SourceMaterial("prom_playground_sandpit_sand","coast_sand_02",1.5f,new Color(1.02f,1.0f,.96f));
        hdpeBlue=BraxChampionsHarbour.Surface("prom_play_hdpe_blue",new Color(.08f,.34f,.68f),.45f,0f);
        hdpeRed=BraxChampionsHarbour.Surface("prom_play_hdpe_red",new Color(.72f,.10f,.09f),.45f,0f);
        hdpeYellow=BraxChampionsHarbour.Surface("prom_play_hdpe_yellow",new Color(.95f,.74f,.08f),.45f,0f);
        hdpeGreen=BraxChampionsHarbour.Surface("prom_play_hdpe_green",new Color(.16f,.52f,.28f),.45f,0f);
    }

    public static void Build(Transform foundation,BraxChampionsFoundation.Zone[] zones)
    {
        if(!foundation||zones==null||zones.Length!=5)throw new InvalidOperationException("Promenade needs its 5 recreation zones (promenade_layout.ZONES)");
        Materials();
        Zones=Plazas=Fountains=Pergolas=Sofas=Armchairs=Tables=GymStations=Slides=Swings=Riders=Sandpits=Pickets=Benches=Lamps=Planters=0;
        int flowers0=BraxChampionsPromenadeFurniture.Flowers;
        var root=new GameObject("MIASTO MISTRZÓW | PROMENADA 10.11 | strefy rekreacji i place z fontannami").transform;root.SetParent(foundation,false);
        foreach(var z in zones)
        {
            var g=new GameObject(z.id+" | "+z.kind).transform;g.SetParent(root,false);
            var colliders=new GameObject("kolizje").transform;colliders.SetParent(g,false);
            var models=new GameObject("kwiaty (Poly Haven, oryginały 2K)").transform;models.SetParent(g,false);
            var m=new BraxChampionsMeshBatch();
            Path(m,colliders,z);
            if(z.kind=="plaza")Plaza(m,colliders,models,z);
            else if(z.kind=="relax")Relax(m,colliders,models,z);
            else if(z.kind=="gym")Gym(m,colliders,models,z);
            else if(z.kind=="playground")Playground(m,colliders,models,g,z);
            else throw new InvalidOperationException("Unknown promenade zone "+z.kind);
            m.Build(g,z.id,Output+"/Meshes/Harbour",true);
            Zones++;
        }
        Debug.Log("CHAMPIONS_PROMENADE_ZONES_10_11 zones="+Zones+" plazas="+Plazas+" fountains="+Fountains+" pergolas="+Pergolas+
                  " sofas="+Sofas+" armchairs="+Armchairs+" tables="+Tables+" gymStations="+GymStations+" slides="+Slides+
                  " swings="+Swings+" springRiders="+Riders+" sandpits="+Sandpits+" fencePickets="+Pickets+" benches="+Benches+
                  " lamps="+Lamps+" planters="+Planters+" flowers="+(BraxChampionsPromenadeFurniture.Flowers-flowers0));
    }

    // ---------------------------------------------------------------- helpers

    // a local frame: a across (right), d forward (the way the piece faces), y up from the zone surface
    struct F
    {
        public float x,z,yaw;
        public F(float x,float z,float yaw){this.x=x;this.z=z;this.yaw=yaw;}
        public Vector3 P(float a,float y,float d)
        {
            float s=Mathf.Sin(yaw*Mathf.Deg2Rad),c=Mathf.Cos(yaw*Mathf.Deg2Rad);
            return new Vector3(x+c*a+s*d,Y+y,z-s*a+c*d);
        }
        public Quaternion R=>Quaternion.Euler(0,yaw,0);
        public void Box(BraxChampionsMeshBatch m,Material mat,float a0,float a1,float y0,float y1,float d0,float d1)
            =>m.Box(mat,P((a0+a1)*.5f,(y0+y1)*.5f,(d0+d1)*.5f),new Vector3(Mathf.Abs(a1-a0),Mathf.Abs(y1-y0),Mathf.Abs(d1-d0)),R);
        public void Collider(Transform c,string name,float a0,float a1,float y0,float y1,float d0,float d1)
            =>BraxChampionsMeshBatch.Collider(c,name,P((a0+a1)*.5f,(y0+y1)*.5f,(d0+d1)*.5f),new Vector3(Mathf.Abs(a1-a0),Mathf.Abs(y1-y0),Mathf.Abs(d1-d0)),yaw);
    }
    // yaw that makes a piece at p face the point c
    static float Facing(Vector2 p,Vector2 c){var d=c-p;return Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg;}

    // quad / triangle wound to face `n`
    static void Q(BraxChampionsMeshBatch m,Material mat,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 n)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),n)<0f)m.Quad(mat,a,d,c,b);else m.Quad(mat,a,b,c,d);
    }
    static void T(BraxChampionsMeshBatch m,Material mat,Vector3 a,Vector3 b,Vector3 c,Vector3 n)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),n)<0f)m.Triangle(mat,a,c,b);else m.Triangle(mat,a,b,c);
    }
    // flat walkable rectangle at y
    static void Flat(BraxChampionsMeshBatch m,Material mat,float x0,float x1,float z0,float z1,float y=Y)
        =>Q(m,mat,new Vector3(x0,y,z0),new Vector3(x1,y,z0),new Vector3(x1,y,z1),new Vector3(x0,y,z1),Vector3.up);
    // the ground of a rectangle: a walkable collider whose top is the surface
    static void Ground(Transform c,string name,float x0,float x1,float z0,float z1)
        =>BraxChampionsMeshBatch.Collider(c,name,new Vector3((x0+x1)*.5f,Y-.15f,(z0+z1)*.5f),new Vector3(x1-x0,.30f,z1-z0));
    // a surface with a flush granite border of EdgeW
    static void Bordered(BraxChampionsMeshBatch m,Material inner,float x0,float x1,float z0,float z1)
    {
        float e=EdgeW;
        Flat(m,inner,x0+e,x1-e,z0+e,z1-e);
        Flat(m,Kerb,x0,x1,z0,z0+e);Flat(m,Kerb,x0,x1,z1-e,z1);
        Flat(m,Kerb,x0,x0+e,z0+e,z1-e);Flat(m,Kerb,x1-e,x1,z0+e,z1-e);
    }

    // a round wall (basin, round planter, bowl rim): outer face from y0, inner face from yIn, flat top at y1
    static void RoundWall(BraxChampionsMeshBatch m,Material mat,Vector3 c,float rIn,float rOut,float y0,float yIn,float y1,int sides,bool outer=true)
    {
        for(int i=0;i<sides;i++)
        {
            float a0=i*Mathf.PI*2f/sides,a1=(i+1)*Mathf.PI*2f/sides;
            Vector3 d0=new Vector3(Mathf.Cos(a0),0,Mathf.Sin(a0)),d1=new Vector3(Mathf.Cos(a1),0,Mathf.Sin(a1)),mid=(d0+d1).normalized;
            Vector3 O(Vector3 d,float y)=>c+d*rOut+Vector3.up*y;Vector3 I(Vector3 d,float y)=>c+d*rIn+Vector3.up*y;
            if(outer)Q(m,mat,O(d0,y0),O(d1,y0),O(d1,y1),O(d0,y1),mid);
            Q(m,mat,I(d0,yIn),I(d1,yIn),I(d1,y1),I(d0,y1),-mid);
            Q(m,mat,I(d0,y1),O(d0,y1),O(d1,y1),I(d1,y1),Vector3.up);
        }
    }
    static void Disc(BraxChampionsMeshBatch m,Material mat,Vector3 c,float r,int sides)
    {
        for(int i=0;i<sides;i++)
        {
            float a0=i*Mathf.PI*2f/sides,a1=(i+1)*Mathf.PI*2f/sides;
            T(m,mat,c,c+new Vector3(Mathf.Cos(a0),0,Mathf.Sin(a0))*r,c+new Vector3(Mathf.Cos(a1),0,Mathf.Sin(a1))*r,Vector3.up);
        }
    }

    // the path from the promenade to the zone: promenade stone between flush granite edging
    static void Path(BraxChampionsMeshBatch m,Transform c,BraxChampionsFoundation.Zone z)
    {
        float x0=z.pathX,x1=z.pathX+z.pathW,z0=z.pathZ,z1=z.pathZ+z.pathD;
        Flat(m,walk,x0+EdgeW,x1-EdgeW,z0,z1);
        Flat(m,Kerb,x0,x0+EdgeW,z0,z1);Flat(m,Kerb,x1-EdgeW,x1,z0,z1);
        Ground(c,"ścieżka "+z.id,x0,x1,z0,z1);
    }

    // the same teak bench as the promenade's (BraxChampionsPromenadeFurniture.BenchFramed), in any direction:
    // cast graphite end frames outside the slats, teak seat and back, stainless bin at its right
    static void Bench(BraxChampionsMeshBatch m,Transform c,F f,string label)
    {
        const float half=.86f,fw=.04f;
        foreach(float s in new[]{-1f,1f})
        {
            float a0=s<0?-half-fw:half,a1=s<0?-half:half+fw;
            f.Box(m,G,a0,a1,0f,.66f,.21f,.27f);                            // front post with the armrest
            f.Box(m,G,a0,a1,0f,.88f,-.25f,-.19f);                          // rear post to the top of the backrest
            f.Box(m,G,a0,a1,.40f,.44f,-.19f,.21f);                         // seat rail
            f.Box(m,G,a0,a1,.62f,.66f,-.19f,.21f);                         // armrest
            f.Box(m,G,a0-.005f*s,a1+.005f*s,.62f,.66f,.27f,.31f);          // armrest nose
        }
        foreach(float s in new[]{-1f,1f})f.Box(m,G,s*.60f-.02f,s*.60f+.02f,.40f,.44f,-.19f,.27f);
        for(int i=0;i<6;i++){float d0=-.29f+i*.083f;f.Box(m,Teak,-half,half,.44f,.475f,-(d0+.07f),-d0);}
        for(int i=0;i<4;i++){float y0=.52f+i*.09f;f.Box(m,Teak,-half,half,y0,y0+.075f,-.19f,-.155f);}
        var bin=f.P(1.35f,0f,0f);
        m.Cylinder(St,bin,.22f,.82f,16,Quaternion.identity,false);
        m.Cylinder(G,bin+Vector3.up*.82f,.235f,.06f,16);m.Cylinder(G,bin,.235f,.05f,16);
        f.Collider(c,"ławka "+label,-.92f,.92f,0f,.9f,-.3f,.32f);
        BraxChampionsMeshBatch.Collider(c,"kosz "+label,bin+Vector3.up*.42f,new Vector3(.46f,.84f,.46f));
        Benches++;
    }
    static void Lamp(BraxChampionsMeshBatch m,Transform c,float x,float z){BraxChampionsPromenadeFurniture.Lamp(m,c,x,z);Lamps++;}

    // ---------------------------------------------------------------- the round fountain plaza

    static void Plaza(BraxChampionsMeshBatch m,Transform c,Transform models,BraxChampionsFoundation.Zone z)
    {
        float hw=z.w*.5f,hd=z.d*.5f,R=PlazaR,Ri=PlazaR-PlazaBand;
        var C=new Vector3(z.x,Y,z.z);
        Vector3 At(float r,float deg)=>C+new Vector3(Mathf.Cos(deg*Mathf.Deg2Rad),0,Mathf.Sin(deg*Mathf.Deg2Rad))*r;
        // the entry from the path (front, -z) meets the circle at x = +-EntryHalf
        float th=(float)Math.Asin(EntryHalf/R)*Mathf.Rad2Deg;                  // half-angle of the entry seen from the centre
        float s0=-90f+th,s1=270f-th;                                     // the lawn runs counter-clockwise s0 -> s1
        // the lawn's outer boundary (rectangle without the entry) and the circle angle each vertex maps to
        var outer=new List<Vector2>{new Vector2(EntryHalf,-hd),new Vector2(hw,-hd),new Vector2(hw,hd),new Vector2(-hw,hd),new Vector2(-hw,-hd),new Vector2(-EntryHalf,-hd)};
        var ang=new List<float>{s0};
        for(int i=1;i<outer.Count-1;i++){float a=Mathf.Atan2(outer[i].y,outer[i].x)*Mathf.Rad2Deg;if(a<s0)a+=360f;ang.Add(a);}
        ang.Add(s1);
        var rim=new List<float>();                                       // every angle of the circle, counter-clockwise from s0
        for(int e=0;e<outer.Count-1;e++)
        {
            int n=Mathf.Max(3,Mathf.CeilToInt((ang[e+1]-ang[e])/7.5f));
            for(int k=0;k<n;k++)
            {
                float t0=(float)k/n,t1=(float)(k+1)/n,a0=Mathf.Lerp(ang[e],ang[e+1],t0),a1=Mathf.Lerp(ang[e],ang[e+1],t1);
                Vector2 b0=Vector2.Lerp(outer[e],outer[e+1],t0),b1=Vector2.Lerp(outer[e],outer[e+1],t1);
                Q(m,lawn,At(R,a0),At(R,a1),C+new Vector3(b1.x,0,b1.y),C+new Vector3(b0.x,0,b0.y),Vector3.up);
                rim.Add(a0);
            }
        }
        // the entry walk between the circle and the plaza's front edge
        int ne=5;
        for(int k=0;k<ne;k++)
        {
            float a0=Mathf.Lerp(s1,s0+360f,(float)k/ne),a1=Mathf.Lerp(s1,s0+360f,(float)(k+1)/ne);
            Vector3 p0=At(R,a0),p1=At(R,a1);
            Q(m,plazaStone,p0,p1,new Vector3(p1.x,Y,C.z-hd),new Vector3(p0.x,Y,C.z-hd),Vector3.up);
            rim.Add(a0);
        }
        rim.Add(s0+360f);
        // dark granite band R-.4..R and the light granite disc inside it
        for(int i=0;i<rim.Count-1;i++)
        {
            Q(m,plazaBand,At(Ri,rim[i]),At(R,rim[i]),At(R,rim[i+1]),At(Ri,rim[i+1]),Vector3.up);
            T(m,plazaStone,C,At(Ri,rim[i]),At(Ri,rim[i+1]),Vector3.up);
        }
        Ground(c,"plac "+z.id,z.x-hw,z.x+hw,z.z-hd,z.z+hd);
        Fountain(m,c,C);
        // benches on the diagonals facing the fountain, round planters with flowers between them, lamps at the gate and the back
        foreach(float a in new[]{45f,135f,225f,315f})
        {
            var p=At(6.4f,a);
            Bench(m,c,new F(p.x,p.z,Facing(new Vector2(p.x,p.z),new Vector2(C.x,C.z))),z.id+" "+a.ToString("0"));
        }
        foreach(float a in new[]{0f,90f,180f})RoundPlanter(m,c,models,At(6.4f,a));
        foreach(float a in new[]{67.5f,112.5f,247.5f,292.5f}){var p=At(8.15f,a);Lamp(m,c,p.x,p.z);}
        Plazas++;
    }

    // two-tier granite fountain: basin R 3.4 with water, pedestal, lower bowl with a falling-water curtain, upper bowl, jet
    static void Fountain(BraxChampionsMeshBatch m,Transform c,Vector3 C)
    {
        const int n=48;var stone=BraxChampionsPromenade.Marble;                // white marble fountain on the grey granite plaza
        RoundWall(m,stone,C,3.0f,3.4f,0f,.36f,.50f,n);                     // basin wall, coping at .50, water at .36
        Disc(m,water,C+Vector3.up*.36f,3.0f,n);
        m.Cylinder(stone,C+Vector3.up*.36f,.45f,.69f,24,Quaternion.identity,false);   // pedestal to 1.05
        m.Cylinder(stone,C+Vector3.up*1.05f,.52f,.20f,n,Quaternion.identity,false,true,1.35f);   // lower bowl
        RoundWall(m,stone,C+Vector3.up*1.25f,1.22f,1.35f,0f,-.07f,0f,n,false);   // its rim
        Disc(m,water,C+Vector3.up*1.18f,1.22f,n);
        m.Cylinder(curtain,C+Vector3.up*.36f,1.37f,.89f,n,Quaternion.identity,false);   // water falling from the rim's edge
        m.Cylinder(stone,C+Vector3.up*1.18f,.17f,.42f,16,Quaternion.identity,false);   // upper column to 1.60
        m.Cylinder(stone,C+Vector3.up*1.60f,.19f,.12f,32,Quaternion.identity,false,true,.62f);   // upper bowl
        RoundWall(m,stone,C+Vector3.up*1.72f,.54f,.62f,0f,-.04f,0f,32,false);
        Disc(m,water,C+Vector3.up*1.68f,.54f,32);
        m.Cylinder(curtain,C+Vector3.up*1.18f,.64f,.54f,32,Quaternion.identity,false);
        m.Cylinder(jet,C+Vector3.up*1.68f,.055f,.95f,12,Quaternion.identity,true,false,.018f);
        BraxChampionsMeshBatch.Collider(c,"fontanna",C+Vector3.up*.25f,new Vector3(6.8f,.5f,6.8f));
        BraxChampionsMeshBatch.Collider(c,"fontanna środek",C+Vector3.up*.9f,new Vector3(2.7f,1.0f,2.7f));
        Fountains++;
    }

    static void RoundPlanter(BraxChampionsMeshBatch m,Transform c,Transform models,Vector3 p)
    {
        RoundWall(m,Kerb,p,.62f,.72f,0f,.42f,.50f,32);
        Disc(m,lawn,p+Vector3.up*.42f,.62f,32);
        BraxChampionsMeshBatch.Collider(c,"donica okrągła",p+Vector3.up*.25f,new Vector3(1.4f,.5f,1.4f));
        BraxChampionsPromenadeFurniture.Flower(models,"flower_gazania",p+Vector3.up*.42f,1.18f);
        BraxChampionsPromenadeFurniture.Flower(models,"flower_gazania",p+Vector3.up*.42f,1.05f,45f);
        Planters++;
    }

    // ---------------------------------------------------------------- relax zone with pergolas

    static void Relax(BraxChampionsMeshBatch m,Transform c,Transform models,BraxChampionsFoundation.Zone z)
    {
        float hw=z.w*.5f,hd=z.d*.5f;
        Bordered(m,BraxChampionsPromenadeFurniture.Deck,z.x-hw,z.x+hw,z.z-hd,z.z+hd);
        Ground(c,"deck "+z.id,z.x-hw,z.x+hw,z.z-hd,z.z+hd);
        foreach(float px in new[]{-8.6f,0f,8.6f})
        {
            float x=z.x+px,zc=z.z+1.6f;
            Pergola(m,c,x,zc,6.0f,4.6f);
            // the lounge set looks out to the sea (-z): sofa at the back, coffee table, two armchairs turned to it
            Sofa(m,c,new F(x,zc+1.35f,180f),2.2f,z.id);
            Table(m,c,x,zc+.05f);
            Sofa(m,c,new F(x-1.45f,zc-1.05f,30f),1.0f,z.id);
            Sofa(m,c,new F(x+1.45f,zc-1.05f,-30f),1.0f,z.id);
        }
        // two teak benches at the front of the deck looking out to the sea, beside the entry
        foreach(float px in new[]{-5.6f,5.6f})Bench(m,c,new F(z.x+px,z.z-hd+1.2f,180f),z.id+" "+px.ToString("0"));
        foreach(float px in new[]{-4.3f,4.3f})BraxChampionsPromenadeFurniture.Planter(m,c,models,z.x+px,z.z+hd-.95f);
        foreach(float px in new[]{-10.6f,10.6f})BraxChampionsPromenadeFurniture.Planter(m,c,models,z.x+px,z.z-hd+.95f);
        Planters+=4;
        foreach(float px in new[]{-12.2f,12.2f})Lamp(m,c,z.x+px,z.z-hd+2.2f);
        Lamp(m,c,z.x-12.2f,z.z+hd-.6f);Lamp(m,c,z.x+12.2f,z.z+hd-.6f);
    }

    // black aluminium pergola: 4 posts, beams, silver-teak slat roof
    static void Pergola(BraxChampionsMeshBatch m,Transform c,float x,float z,float W,float D)
    {
        float hw=W*.5f,hd=D*.5f;const float p=.12f,top=2.62f,beam=.20f;
        foreach(float sx in new[]{-1f,1f})foreach(float sz in new[]{-1f,1f})
        {
            float px=x+sx*(hw-p*.5f),pz=z+sz*(hd-p*.5f);
            m.BoxMinMax(G,px-p*.5f,px+p*.5f,Y,Y+top-beam,pz-p*.5f,pz+p*.5f);
            BraxChampionsMeshBatch.Collider(c,"pergola słup",new Vector3(px,Y+top*.5f,pz),new Vector3(p,top,p));
        }
        m.BoxMinMax(G,x-hw,x+hw,Y+top-beam,Y+top,z-hd,z-hd+p);              // front and back beams over the posts
        m.BoxMinMax(G,x-hw,x+hw,Y+top-beam,Y+top,z+hd-p,z+hd);
        m.BoxMinMax(G,x-hw,x-hw+p,Y+top-beam,Y+top,z-hd+p,z+hd-p);          // side beams between them
        m.BoxMinMax(G,x+hw-p,x+hw,Y+top-beam,Y+top,z-hd+p,z+hd-p);
        int n=Mathf.FloorToInt((W-.3f)/.25f)+1;
        for(int i=0;i<n;i++)
        {
            float sx=x-hw+.15f+i*.25f;
            m.BoxMinMax(Teak,sx-.025f,sx+.025f,Y+top,Y+top+.09f,z-hd-.15f,z+hd+.15f);
        }
        Pergolas++;
    }

    // outdoor lounge sofa (length L) or armchair (L 1.0): graphite aluminium frame, white cushions, faces +d
    static void Sofa(BraxChampionsMeshBatch m,Transform c,F f,float L,string label)
    {
        float h=L*.5f,arm=.12f,ins=h-arm;
        var cu=BraxChampionsPromenade.Cushion;
        foreach(float sa in new[]{-1f,1f})foreach(float sd in new[]{-1f,1f})
            f.Box(m,G,sa*(h-.06f)-.03f,sa*(h-.06f)+.03f,0f,.08f,sd*.39f-.03f,sd*.39f+.03f);   // feet
        f.Box(m,G,-h,h,.08f,.30f,-.45f,.45f);                              // base frame
        f.Box(m,G,-h,-ins,.30f,.62f,-.45f,.45f);f.Box(m,G,ins,h,.30f,.62f,-.45f,.45f);   // arms
        f.Box(m,G,-ins,ins,.30f,.72f,-.45f,-.33f);                         // back panel
        f.Box(m,cu,-ins,ins,.30f,.44f,-.33f,.45f);                         // seat cushion
        int k=L>1.5f?2:1;float w=(2f*ins-.02f*(k-1))/k;
        for(int i=0;i<k;i++){float a0=-ins+i*(w+.02f);f.Box(m,cu,a0+.01f,a0+w-.01f,.44f,.82f,-.33f,-.15f);}
        f.Collider(c,(L>1.5f?"sofa ":"fotel ")+label,-h,h,0f,.8f,-.45f,.45f);
        if(L>1.5f)Sofas++;else Armchairs++;
    }

    static void Table(BraxChampionsMeshBatch m,Transform c,float x,float z)
    {
        m.BoxMinMax(Teak,x-.6f,x+.6f,Y+.36f,Y+.40f,z-.35f,z+.35f);
        foreach(float sx in new[]{-1f,1f})foreach(float sz in new[]{-1f,1f})
            m.BoxMinMax(G,x+sx*.52f-.025f,x+sx*.52f+.025f,Y,Y+.36f,z+sz*.27f-.025f,z+sz*.27f+.025f);
        BraxChampionsMeshBatch.Collider(c,"stolik",new Vector3(x,Y+.2f,z),new Vector3(1.2f,.4f,.7f));
        Tables++;
    }

    // ---------------------------------------------------------------- outdoor gym

    static void Gym(BraxChampionsMeshBatch m,Transform c,Transform models,BraxChampionsFoundation.Zone z)
    {
        float hw=z.w*.5f,hd=z.d*.5f,x=z.x,zc=z.z;
        Bordered(m,gymRubber,x-hw,x+hw,zc-hd,zc+hd);
        Ground(c,"siłownia plenerowa "+z.id,x-hw,x+hw,zc-hd,zc+hd);
        // pull-up station: three posts, bars at 2.0 and 2.3
        float[] px={x-9f,x-7.2f,x-5.4f};float pz=zc+3.5f;
        foreach(float p in px)Post(m,c,p,pz,2.5f);
        m.Tube(St,new Vector3(px[0],Y+2.0f,pz),new Vector3(px[1],Y+2.0f,pz),.016f,10);
        m.Tube(St,new Vector3(px[1],Y+2.3f,pz),new Vector3(px[2],Y+2.3f,pz),.016f,10);
        GymStations++;
        // monkey bars 4 m, rungs every .4
        float mz0=zc+3.2f,mz1=zc+3.8f,my=Y+2.2f;
        foreach(float p in new[]{x-2f,x+2f})foreach(float q in new[]{mz0,mz1})Post(m,c,p,q,2.3f);
        foreach(float q in new[]{mz0,mz1})m.Tube(St,new Vector3(x-2f,my,q),new Vector3(x+2f,my,q),.025f,10);
        for(int i=1;i<10;i++){float rx=x-2f+i*.4f;m.Tube(St,new Vector3(rx,my,mz0),new Vector3(rx,my,mz1),.016f,10);}
        GymStations++;
        // dip bars
        foreach(float q in new[]{zc+3.25f,zc+3.75f})
        {
            foreach(float p in new[]{x+5.2f,x+7.6f})Post(m,c,p,q,1.12f,.05f);
            m.Tube(St,new Vector3(x+5.0f,Y+1.15f,q),new Vector3(x+7.8f,Y+1.15f,q),.022f,10);
        }
        GymStations++;
        // wall bars: two posts, nine rungs
        foreach(float p in new[]{x+6.5f,x+7.5f})Post(m,c,p,zc-3f,2.4f);
        for(int i=0;i<9;i++){float ry=Y+.3f+i*.25f;m.Tube(St,new Vector3(x+6.5f,ry,zc-3f),new Vector3(x+7.5f,ry,zc-3f),.018f,10);}
        GymStations++;
        // sit-up bench: padded board inclined 20 degrees on a graphite frame, foot bar at the high end
        {
            var f=new F(x-7f,zc-3f,90f);float a=20f*Mathf.Deg2Rad;
            var lo=f.P(0,.45f,-.75f);var hi=f.P(0,.45f+1.5f*Mathf.Sin(a),-.75f+1.5f*Mathf.Cos(a));
            m.Box(BraxChampionsHarbour.Rubber,(lo+hi)*.5f+Vector3.up*.03f,new Vector3(.42f,.06f,1.5f),Quaternion.Euler(-20f,f.yaw,0));
            foreach(var e in new[]{lo,hi})foreach(float s in new[]{-.16f,.16f})
            {
                var leg=e+f.R*new Vector3(s,0,0);
                m.BoxMinMax(G,leg.x-.03f,leg.x+.03f,Y,e.y-.005f,leg.z-.03f,leg.z+.03f);
            }
            var fb=f.P(0,.45f+1.5f*Mathf.Sin(a)+.25f,-.75f+1.5f*Mathf.Cos(a)+.12f);
            m.Tube(St,fb+f.R*new Vector3(-.25f,0,0),fb+f.R*new Vector3(.25f,0,0),.03f,10);
            m.Tube(G,fb,new Vector3(fb.x,Y,fb.z),.03f,10);
            f.Collider(c,"ławka do brzuszków",-.25f,.25f,0f,1.1f,-.8f,.9f);
            GymStations++;
        }
        // push-up bars: three heights
        for(int i=0;i<3;i++)
        {
            float bx=x-1.5f+i*1.5f,h=.30f+i*.15f;
            foreach(float s in new[]{-.5f,.5f})Post(m,c,bx,zc-3f+s,h-.02f,.04f);
            m.Tube(St,new Vector3(bx,Y+h,zc-3.6f),new Vector3(bx,Y+h,zc-2.4f),.02f,10);
        }
        GymStations++;
        foreach(float s in new[]{-1f,1f})Bench(m,c,new F(x+s*9f,zc+hd-.75f,180f),z.id+" "+s);
        foreach(float s in new[]{-1f,1f})Lamp(m,c,x+s*(hw-.8f),zc-hd+.8f);
    }
    // graphite round post with a cap
    static void Post(BraxChampionsMeshBatch m,Transform c,float x,float z,float h,float r=.06f)
    {
        m.Cylinder(G,new Vector3(x,Y,z),r,h,14,Quaternion.identity,true);
        BraxChampionsMeshBatch.Collider(c,"słupek",new Vector3(x,Y+h*.5f,z),new Vector3(r*2f,h,r*2f));
    }

    // ---------------------------------------------------------------- playground

    static void Playground(BraxChampionsMeshBatch m,Transform c,Transform models,Transform zoneRoot,BraxChampionsFoundation.Zone z)
    {
        float hw=z.w*.5f,hd=z.d*.5f,x=z.x,zc=z.z;
        Bordered(m,playRubber,x-hw,x+hw,zc-hd,zc+hd);
        Ground(c,"plac zabaw "+z.id,x-hw,x+hw,zc-hd,zc+hd);
        Fence(m,c,x,zc,hw-.2f,hd-.2f,1.1f);
        SlideTower(m,c,x-7f,zc+1.5f);
        SwingSet(m,c,x+4f,zc+3.4f);
        SpringRider(m,c,x+9.6f,zc-3.2f,hdpeGreen);
        SpringRider(m,c,x+7.4f,zc-4.4f,hdpeYellow);
        Sandpit(m,c,zoneRoot,x+.5f,zc-2.6f);
        foreach(float px in new[]{-10.2f,.2f,10.2f})Bench(m,c,new F(x+px,zc+hd-.95f,180f),z.id+" "+px.ToString("0"));
        foreach(float s in new[]{-1f,1f})BraxChampionsPromenadeFurniture.Planter(m,c,models,x+s*10.8f,zc-hd+1.0f);
        Planters+=2;
        foreach(float s in new[]{-1f,1f})Lamp(m,c,x+s*2.0f,zc-hd+.75f);
        foreach(float s in new[]{-1f,1f})Lamp(m,c,x+s*(hw-.75f),zc+hd-.75f);
    }

    // low picket fence round the playground, gate opening at the front (towards the path)
    static void Fence(BraxChampionsMeshBatch m,Transform c,float x,float z,float ax,float az,float gate)
    {
        const float h=.95f;
        void Run(Vector3 a,Vector3 b)
        {
            var d=b-a;float len=d.magnitude;if(len<.2f)return;var u=d/len;bool alongX=Mathf.Abs(u.x)>.5f;
            int posts=Mathf.Max(1,Mathf.CeilToInt(len/2f));
            for(int i=0;i<=posts;i++)
            {
                var p=a+d*i/posts;
                m.BoxMinMax(G,p.x-.04f,p.x+.04f,Y,Y+h+.05f,p.z-.04f,p.z+.04f);
            }
            foreach(float ry in new[]{.12f,.82f})                           // rails between the posts' faces
                if(alongX)m.BoxMinMax(G,Mathf.Min(a.x,b.x),Mathf.Max(a.x,b.x),Y+ry,Y+ry+.05f,a.z-.025f,a.z+.025f);
                else m.BoxMinMax(G,a.x-.025f,a.x+.025f,Y+ry,Y+ry+.05f,Mathf.Min(a.z,b.z),Mathf.Max(a.z,b.z));
            int n=Mathf.FloorToInt(len/.14f);
            for(int i=1;i<n;i++)
            {
                var p=a+u*(i*.14f);
                if(Mathf.Abs(((i*.14f)/(len/posts))-Mathf.Round((i*.14f)/(len/posts)))*(len/posts)<.08f)continue;   // not through a post
                m.BoxMinMax(Teak,p.x-.015f,p.x+.015f,Y+.07f,Y+h,p.z-.015f,p.z+.015f);
                Pickets++;
            }
            var mid=(a+b)*.5f;
            BraxChampionsMeshBatch.Collider(c,"płotek",new Vector3(mid.x,Y+h*.5f,mid.z),alongX?new Vector3(len,h,.1f):new Vector3(.1f,h,len));
        }
        var A=new Vector3(x-ax,0,z-az);var B=new Vector3(x+ax,0,z-az);var Cc=new Vector3(x+ax,0,z+az);var D=new Vector3(x-ax,0,z+az);
        Run(new Vector3(x+gate,0,z-az),B);Run(B,Cc);Run(Cc,D);Run(D,A);Run(A,new Vector3(x-gate,0,z-az));
        // gate posts, taller, with a stainless arch bar over the opening
        foreach(float s in new[]{-1f,1f})
        {
            m.BoxMinMax(G,x+s*gate-.06f,x+s*gate+.06f,Y,Y+2.2f,z-az-.06f,z-az+.06f);
            BraxChampionsMeshBatch.Collider(c,"brama słupek",new Vector3(x+s*gate,Y+1.1f,z-az),new Vector3(.12f,2.2f,.12f));
        }
        m.BoxMinMax(G,x-gate-.06f,x+gate+.06f,Y+2.2f,Y+2.32f,z-az-.06f,z-az+.06f);
    }

    // slide tower: 1.6 m platform at 1.2 on four posts, HDPE guard panels, red pyramid roof, ladder (front), slide (+x)
    static void SlideTower(BraxChampionsMeshBatch m,Transform c,float x,float z)
    {
        const float h=.8f,deck=1.2f,top=2.6f,apex=3.3f;
        foreach(float sx in new[]{-1f,1f})foreach(float sz in new[]{-1f,1f})
        {
            float px=x+sx*(h-.05f),pz=z+sz*(h-.05f);
            m.BoxMinMax(G,px-.05f,px+.05f,Y,Y+top,pz-.05f,pz+.05f);
        }
        m.BoxMinMax(BraxChampionsPromenadeFurniture.Deck,x-h+.1f,x+h-.1f,Y+deck-.06f,Y+deck,z-h+.1f,z+h-.1f);   // deck between the posts
        m.BoxMinMax(BraxChampionsPromenadeFurniture.Deck,x-h+.1f,x+h-.1f,Y+deck-.06f,Y+deck,z-h,z-h+.1f);       // front edge board
        m.BoxMinMax(BraxChampionsPromenadeFurniture.Deck,x-h+.1f,x+h-.1f,Y+deck-.06f,Y+deck,z+h-.1f,z+h);
        m.BoxMinMax(BraxChampionsPromenadeFurniture.Deck,x-h,x-h+.1f,Y+deck-.06f,Y+deck,z-h+.1f,z+h-.1f);
        m.BoxMinMax(BraxChampionsPromenadeFurniture.Deck,x+h-.1f,x+h,Y+deck-.06f,Y+deck,z-h+.1f,z+h-.1f);
        // guard panels: back (+z) and west (-x) between the posts, front and east open for the ladder and the slide
        m.BoxMinMax(hdpeBlue,x-h+.1f,x+h-.1f,Y+deck+.05f,Y+deck+.75f,z+h-.07f,z+h-.03f);
        m.BoxMinMax(hdpeBlue,x-h+.03f,x-h+.07f,Y+deck+.05f,Y+deck+.75f,z-h+.1f,z+h-.1f);
        // the roof: four red panels to the apex, 15 cm over the posts
        float r=h+.15f;var ap=new Vector3(x,Y+apex,z);
        Vector3 K(float sx,float sz)=>new Vector3(x+sx*r,Y+top,z+sz*r);
        var corners=new[]{K(-1,-1),K(1,-1),K(1,1),K(-1,1)};
        for(int i=0;i<4;i++)
        {
            var a=corners[i];var b=corners[(i+1)%4];var mid=(a+b)*.5f;var nrm=Vector3.Cross(b-a,ap-a);
            if(Vector3.Dot(nrm,mid-new Vector3(x,Y+top,z))<0)nrm=-nrm;
            T(m,hdpeRed,a,b,ap,nrm);
            T(m,hdpeRed,a,b,ap,-nrm);                                      // underside seen from the deck
        }
        // ladder at the front: two stiles, rungs every .28
        for(int s=-1;s<=1;s+=2)
            m.Tube(G,new Vector3(x+s*.28f,Y,z-h-.55f),new Vector3(x+s*.28f,Y+deck+.6f,z-h+.05f),.03f,10);
        for(int i=1;i<=4;i++)
        {
            float t=i*.28f/(deck+.6f);var p=Vector3.Lerp(new Vector3(x,Y,z-h-.55f),new Vector3(x,Y+deck+.6f,z-h+.05f),t);
            m.Tube(St,p+Vector3.left*.28f,p+Vector3.right*.28f,.018f,10);
        }
        // stainless slide to the east: 2.6 m run down to .30, a 0.5 m flat exit, yellow sides
        float x0=x+h,x1=x0+2.6f,yTop=Y+deck-.02f,yEnd=Y+.30f;
        var lo=new Vector3(x1,yEnd,z);var hi=new Vector3(x0,yTop,z);
        float len=(hi-lo).magnitude,ang=Mathf.Atan2(yTop-yEnd,x1-x0)*Mathf.Rad2Deg;
        var rot=Quaternion.Euler(0,0,-ang);var mid2=(lo+hi)*.5f;var up=rot*Vector3.up;
        m.Box(St,mid2,new Vector3(len,.04f,.50f),rot);
        foreach(float s in new[]{-1f,1f})m.Box(hdpeYellow,mid2+up*.06f+Vector3.forward*s*.27f,new Vector3(len,.20f,.04f),rot);
        m.BoxMinMax(St,x1-.01f,x1+.5f,yEnd-.04f,yEnd,z-.25f,z+.25f);
        foreach(float s in new[]{-1f,1f})m.BoxMinMax(hdpeYellow,x1-.01f,x1+.5f,yEnd-.04f,yEnd+.12f,z+s*.27f-.02f,z+s*.27f+.02f);
        foreach(float s in new[]{-1f,1f})m.BoxMinMax(G,x1+.20f-.03f,x1+.20f+.03f,Y,yEnd-.04f,z+s*.20f-.03f,z+s*.20f+.03f);
        BraxChampionsMeshBatch.Collider(c,"wieża zjeżdżalni",new Vector3(x,Y+deck*.5f,z),new Vector3(2*h,deck,2*h));
        BraxChampionsMeshBatch.Collider(c,"zjeżdżalnia",new Vector3((x0+x1+.5f)*.5f,Y+.5f,z),new Vector3(x1+.5f-x0,.9f,.6f));
        Slides++;
    }

    // swing set: A-frame legs, beam at 2.4, two seats on chains
    static void SwingSet(BraxChampionsMeshBatch m,Transform c,float x,float z)
    {
        const float beamY=2.4f,half=2.1f;
        foreach(float s in new[]{-1f,1f})
        {
            var top=new Vector3(x+s*2.0f,Y+beamY,z);
            m.Tube(G,top,new Vector3(x+s*2.0f,Y-.02f,z-.95f),.045f,12);
            m.Tube(G,top,new Vector3(x+s*2.0f,Y-.02f,z+.95f),.045f,12);
            BraxChampionsMeshBatch.Collider(c,"huśtawka nogi",new Vector3(x+s*2.0f,Y+1.2f,z),new Vector3(.12f,2.4f,1.9f));
        }
        m.Tube(G,new Vector3(x-half,Y+beamY,z),new Vector3(x+half,Y+beamY,z),.055f,14);
        foreach(float sx in new[]{-.8f,.8f})
        {
            foreach(float e in new[]{-.2f,.2f})
                m.Tube(St,new Vector3(x+sx+e,Y+beamY-.05f,z),new Vector3(x+sx+e,Y+.47f,z),.006f,6);
            m.BoxMinMax(BraxChampionsHarbour.Rubber,x+sx-.24f,x+sx+.24f,Y+.43f,Y+.47f,z-.09f,z+.09f);
            Swings++;
        }
    }

    // spring rider: steel coil on a base plate, HDPE body with a seat, handle and footrests
    static void SpringRider(BraxChampionsMeshBatch m,Transform c,float x,float z,Material body)
    {
        var b=new Vector3(x,Y,z);
        m.Cylinder(G,b,.26f,.03f,24);
        const int turns=6,seg=12;Vector3 prev=b+new Vector3(.09f,.03f,0);
        for(int i=1;i<=turns*seg;i++)
        {
            float t=(float)i/(turns*seg),a=t*turns*Mathf.PI*2f;
            var p=b+new Vector3(Mathf.Cos(a)*.09f,.03f+t*.42f,Mathf.Sin(a)*.09f);
            m.Tube(hdpeYellow,prev,p,.016f,6);prev=p;
        }
        m.BoxMinMax(body,x-.38f,x+.38f,Y+.45f,Y+.72f,z-.13f,z+.13f);
        m.BoxMinMax(body,x+.22f,x+.42f,Y+.72f,Y+1.02f,z-.11f,z+.11f);      // head
        m.BoxMinMax(BraxChampionsHarbour.Rubber,x-.30f,x+.05f,Y+.72f,Y+.76f,z-.12f,z+.12f);   // seat pad
        m.Tube(St,new Vector3(x+.18f,Y+.90f,z-.20f),new Vector3(x+.18f,Y+.90f,z+.20f),.018f,8);
        m.Tube(St,new Vector3(x+.0f,Y+.55f,z-.24f),new Vector3(x+.0f,Y+.55f,z+.24f),.018f,8);
        BraxChampionsMeshBatch.Collider(c,"bujak",new Vector3(x,Y+.5f,z),new Vector3(.9f,1.0f,.5f));
        Riders++;
    }

    // sandpit 4 x 3 with a teak sitting edge; the sand is its own object with a collider so it takes footprints
    static void Sandpit(BraxChampionsMeshBatch m,Transform c,Transform zoneRoot,float x,float z)
    {
        const float hx=2.0f,hz=1.5f,e=.25f,top=.30f,fill=.22f;
        m.BoxMinMax(Teak,x-hx,x+hx,Y,Y+top,z-hz,z-hz+e);m.BoxMinMax(Teak,x-hx,x+hx,Y,Y+top,z+hz-e,z+hz);
        m.BoxMinMax(Teak,x-hx,x-hx+e,Y,Y+top,z-hz+e,z+hz-e);m.BoxMinMax(Teak,x+hx-e,x+hx,Y,Y+top,z-hz+e,z+hz-e);
        BraxChampionsMeshBatch.Collider(c,"piaskownica brzeg",new Vector3(x,Y+top*.5f,z-hz+e*.5f),new Vector3(2*hx,top,e));
        BraxChampionsMeshBatch.Collider(c,"piaskownica brzeg",new Vector3(x,Y+top*.5f,z+hz-e*.5f),new Vector3(2*hx,top,e));
        BraxChampionsMeshBatch.Collider(c,"piaskownica brzeg",new Vector3(x-hx+e*.5f,Y+top*.5f,z),new Vector3(e,top,2*hz-2*e));
        BraxChampionsMeshBatch.Collider(c,"piaskownica brzeg",new Vector3(x+hx-e*.5f,Y+top*.5f,z),new Vector3(e,top,2*hz-2*e));
        var s=new BraxChampionsMeshBatch();
        s.BoxMinMax(sand,x-hx+e,x+hx-e,Y,Y+fill,z-hz+e,z+hz-e);
        var g=s.Build(zoneRoot,"piaskownica | piasek (ślady stóp)",Output+"/Meshes/Harbour");
        foreach(var mf in g.GetComponentsInChildren<MeshFilter>(true))
        {
            mf.gameObject.name="piaskownica piasek | "+mf.gameObject.name;
            mf.gameObject.AddComponent<MeshCollider>().sharedMesh=mf.sharedMesh;
        }
        Sandpits++;
    }
}
