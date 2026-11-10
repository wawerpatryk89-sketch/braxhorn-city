using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Owner 10.11 18:31: "Koło mariny też jakiś parking by się przydał, nie? Mariny, koło mariny, czy plaży". Two public car
// parks on the lawn between the city road Z-70 and the promenade's palm strip (promenade_layout.PARKINGS: PARKING_PLAZA
// by the beach and the fountain plazas, PARKING_MARINA by the marina), everything flush at the sidewalk level .16:
//   - the drive leaves the road as a real street mouth (BraxChampionsDriveMouth: road -> sidewalk over R, R 3 m granite
//     kerb arcs, city kerb open over it, layout.PARKING_DRIVE_MOUTHS) and crosses the lawn between flush granite edging,
//   - the lot: asphalt with a flush granite border, 2 rows of 2.5 x 5.0 m bays on a 7.0 m two-way aisle, white bay lines
//     cut INTO the asphalt (separate surfaces edge to edge, never paint lying on the asphalt: zero migotania), two blue
//     accessible bays ("koperty") next to the footpath, a precast wheel stop in every bay, the promenade lamps round the lot,
//     a pay station at the footpath,
//   - a footpath of promenade stone between granite edging to the promenade, between two palms.
public static class BraxChampionsPromenadeParking
{
    const string Output="Assets/PlayableGenerated/Mistrzowie";
    const float Y=.16f,EdgeW=.25f,BayW=2.5f,BayD=5.0f,Aisle=7.0f,Line=.12f,RoadEdgeZ=-80f,MouthR=3f;
    static Material asphalt,paint,bayBlue,walk,cityWalk,kerbStone;
    public static int Lots,Bays,Accessible,WheelStops,Lamps;

    static Material Kerb=>BraxChampionsPromenadeFurniture.Kerb;

    static void Materials()
    {
        if(!BraxChampionsHarbour.Steel)BraxChampionsHarbour.Materials();
        if(!BraxChampionsPromenadeFurniture.Teak)BraxChampionsPromenadeFurniture.Materials();
        asphalt=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/asphalt.mat");
        if(!asphalt)asphalt=BraxChampionsFoundation.SourceMaterial("prom_parking_asphalt","clean_asphalt",3f,Color.white);
        walk=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/prom_paving.mat");
        if(!walk)walk=BraxChampionsFoundation.LibraryAcg("prom_zone_path","PavingStones126A",Color.white,4.5f);
        cityWalk=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/paving.mat");
        if(!cityWalk)cityWalk=walk;
        kerbStone=BraxChampionsFoundation.SourceMaterial("infrastructure_kerb","granite_tile",.9f,new Color(.78f,.79f,.80f));
        paint=BraxChampionsHarbour.Surface("prom_parking_line_white",new Color(.93f,.93f,.91f),.55f,0f);
        bayBlue=BraxChampionsHarbour.Surface("prom_parking_accessible_blue",new Color(.05f,.27f,.60f),.60f,0f);
    }

    public static void Build(Transform foundation,BraxChampionsFoundation.Parking[] lots)
    {
        if(!foundation||lots==null||lots.Length!=2)throw new InvalidOperationException("Promenade needs its 2 public car parks (promenade_layout.PARKINGS)");
        Materials();
        Lots=Bays=Accessible=WheelStops=Lamps=0;
        var root=new GameObject("MIASTO MISTRZÓW | PROMENADA 10.11 | parkingi przy marinie i plaży").transform;root.SetParent(foundation,false);
        foreach(var p in lots)
        {
            var g=new GameObject(p.id).transform;g.SetParent(root,false);
            var colliders=new GameObject("kolizje").transform;colliders.SetParent(g,false);
            var m=new BraxChampionsMeshBatch();
            Lot(m,colliders,p);
            m.Build(g,p.id,Output+"/Meshes/Harbour",true);
            BraxChampionsDriveMouth.Build(g,new BraxChampionsDriveMouth.Spec{id=p.id+" street mouth",alongX=true,edge=RoadEdgeZ,dir=-1f,
                u0=p.driveX0,u1=p.driveX1,R=MouthR,depth=RoadEdgeZ-p.sidewalkZ,topEnd=Y,unit=BraxClaudeOsiedle.K,assetDir=Output+"/Meshes/Harbour"},
                asphalt,cityWalk,kerbStone);
            Lots++;
        }
        Debug.Log("CHAMPIONS_PROMENADE_PARKING_10_11 lots="+Lots+" bays="+Bays+" accessible="+Accessible+" wheelStops="+WheelStops+
                  " lamps="+Lamps+" bay="+BayW+"x"+BayD+"m aisle="+Aisle+"m streetMouths=R"+MouthR);
    }

    static void Flat(BraxChampionsMeshBatch m,Material mat,float x0,float x1,float z0,float z1)
    {
        if(x1-x0<1e-4f||z1-z0<1e-4f)return;
        m.Quad(mat,new Vector3(x0,Y,z1),new Vector3(x1,Y,z1),new Vector3(x1,Y,z0),new Vector3(x0,Y,z0));   // faces up (as the box top)
    }
    static void Ground(Transform c,string name,float x0,float x1,float z0,float z1)
        =>BraxChampionsMeshBatch.Collider(c,name,new Vector3((x0+x1)*.5f,Y-.15f,(z0+z1)*.5f),new Vector3(x1-x0,.30f,z1-z0));

    static void Lot(BraxChampionsMeshBatch m,Transform c,BraxChampionsFoundation.Parking p)
    {
        float e=EdgeW,ix0=p.x0+e,ix1=p.x1-e,iz0=p.z0+e,iz1=p.z1-e;
        float dx0=p.driveX0,dx1=p.driveX1,pathMid=(p.pathX0+p.pathX1)*.5f;
        // granite border; open over the drive on the north side
        Flat(m,Kerb,p.x0,p.x1,p.z0,iz0);
        Flat(m,Kerb,p.x0,dx0,iz1,p.z1);Flat(m,Kerb,dx1,p.x1,iz1,p.z1);
        Flat(m,Kerb,p.x0,ix0,iz0,iz1);Flat(m,Kerb,ix1,p.x1,iz0,iz1);
        // the drive across the lawn to the sidewalk: asphalt between flush granite edging (the mouth's edging continues it)
        Flat(m,asphalt,dx0+e,dx1-e,iz1,p.sidewalkZ);
        Flat(m,kerbStone,dx0,dx0+e,iz1,p.sidewalkZ);Flat(m,kerbStone,dx1-e,dx1,iz1,p.sidewalkZ);
        Ground(c,"parking "+p.id,p.x0,p.x1,p.z0,p.z1);
        Ground(c,"wjazd "+p.id,dx0,dx1,p.z1,p.sidewalkZ);
        // rows: north (heads at the north border), aisle, south (heads at the south border)
        float nz0=iz1-BayD,sz1=iz0+BayD;
        Flat(m,asphalt,ix0,ix1,sz1,nz0);
        Row(m,c,p,ix0,ix1,nz0,iz1,true,dx0-.3f,dx1+.3f,pathMid);
        Row(m,c,p,ix0,ix1,iz0,sz1,false,float.NaN,float.NaN,pathMid);
        // lamps on the lawn round the lot (north strip between the lot and the sidewalk, south strip before the palms)
        int n=Mathf.Max(2,Mathf.RoundToInt((p.x1-p.x0)/20f));
        for(int i=0;i<=n;i++)
        {
            float x=Mathf.Lerp(p.x0+1.5f,p.x1-1.5f,(float)i/n);
            if(x<dx0-1f||x>dx1+1f){BraxChampionsPromenadeFurniture.Lamp(m,c,x,(p.z1+p.sidewalkZ)*.5f);Lamps++;}
            if(x<p.pathX0-1f||x>p.pathX1+1f){BraxChampionsPromenadeFurniture.Lamp(m,c,x,p.z0-1.4f);Lamps++;}
        }
        // the footpath to the promenade
        float fz0=-120f,fz1=p.z0;
        Flat(m,walk,p.pathX0+e,p.pathX1-e,fz0,fz1);
        Flat(m,Kerb,p.pathX0,p.pathX0+e,fz0,fz1);Flat(m,Kerb,p.pathX1-e,p.pathX1,fz0,fz1);
        Ground(c,"ścieżka "+p.id,p.pathX0,p.pathX1,fz0,fz1);
        PayStation(m,c,p.pathX1-.55f,p.z0-1.6f);                         // on the footpath, display to the walkers
    }

    // one row of bays between z0 and z1 (north: heads at z1). Bay lines are their own surfaces cut into the asphalt.
    static void Row(BraxChampionsMeshBatch m,Transform c,BraxChampionsFoundation.Parking p,float x0,float x1,float z0,float z1,bool north,float gap0,float gap1,float pathMid)
    {
        int n=Mathf.FloorToInt((x1-x0-Line)/BayW);
        float margin=(x1-x0-n*BayW-Line)*.5f;
        bool Kept(int k){if(k<0||k>=n)return false;float a=x0+margin+k*BayW,b=a+BayW+Line;return float.IsNaN(gap0)||b<=gap0||a>=gap1;}
        // the two bays nearest the footpath in the south row are accessible
        var blue=new HashSet<int>();
        if(!north)
        {
            var order=new List<int>();for(int k=0;k<n;k++)order.Add(k);
            order.Sort((a,b)=>Mathf.Abs(x0+margin+(a+.5f)*BayW+Line*.5f-pathMid).CompareTo(Mathf.Abs(x0+margin+(b+.5f)*BayW+Line*.5f-pathMid)));
            blue.Add(order[0]);blue.Add(order[1]);
        }
        float cur=x0;
        void Asphalt(float to){Flat(m,asphalt,cur,to,z0,z1);cur=to;}
        for(int k=0;k<=n;k++)
        {
            float lx=x0+margin+k*BayW;
            if(Kept(k-1)||Kept(k)){Asphalt(lx);Flat(m,paint,lx,lx+Line,z0,z1);cur=lx+Line;}
            if(k==n)break;
            float bx0=lx+Line,bx1=lx+BayW;
            if(Kept(k)&&blue.Contains(k)){Asphalt(Mathf.Max(cur,bx0));Flat(m,bayBlue,bx0,bx1,z0,z1);cur=bx1;Accessible++;}
            if(Kept(k))
            {
                Bays++;
                // precast wheel stop .75 m from the head of the bay
                float cx=(bx0+bx1)*.5f,cz=north?z1-.75f:z0+.75f;
                m.BoxMinMax(Kerb,cx-.8f,cx+.8f,Y,Y+.10f,cz-.075f,cz+.075f);
                BraxChampionsMeshBatch.Collider(c,"odbojnik",new Vector3(cx,Y+.05f,cz),new Vector3(1.6f,.10f,.15f));
                WheelStops++;
            }
        }
        Asphalt(x1);
    }

    // pay station: graphite cabinet, stainless face, warm LED display on its -x side (towards the middle of the path)
    static void PayStation(BraxChampionsMeshBatch m,Transform c,float x,float z)
    {
        var g=BraxChampionsHarbour.Graphite;
        m.BoxMinMax(g,x-.25f,x+.25f,Y,Y+1.65f,z-.18f,z+.18f);
        m.BoxMinMax(BraxChampionsHarbour.Steel,x-.27f,x-.25f,Y+.55f,Y+1.45f,z-.15f,z+.15f);
        m.BoxMinMax(BraxChampionsHarbour.Led,x-.285f,x-.27f,Y+1.20f,Y+1.38f,z-.10f,z+.10f);
        m.BoxMinMax(g,x-.30f,x+.30f,Y+1.65f,Y+1.71f,z-.23f,z+.23f);
        BraxChampionsMeshBatch.Collider(c,"parkomat",new Vector3(x,Y+.85f,z),new Vector3(.6f,1.7f,.46f));
    }
}
