using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// D2 first floor (board: "Piętro (+1): strefa gastronomiczna, sklepy, biura, zaplecze"; "pełna dostępność: windy"):
// * in each wing's front corner by the hall (x +-23..30, z .9..8.8, open to the hall through the walnut portal) a U stair
//   (2 x 13 risers of 17.6 cm, 30 cm going, granite treads with contrast nosings, glass balustrades with stainless rails)
//   and a working lift (BraxWinda) HOL <-> PIĘTRO;
// * WEST +1: the food court - three food counters along the outer wall with 3D names on navy bands and lit menu boards,
//   twelve round marble tables with four upholstered chairs each, the gallery over the hall;
// * EAST +1: shops and offices behind glass fronts (PRASA · KSIĄŻKI, PAMIĄTKI, BIURO PODRÓŻY) with shelves and desks;
// * the +1 ceiling white with a grid of downlights.
public static partial class BraxChampionsD2
{
    const float UpY=Floor1+.40f;
    static void StairCore(float s)
    {
        float rise=(UpY-FFL)/26f,run=.30f;
        float X(float v)=>s*v;
        // flight 1: x 23.0..25.4, from z 6.4 (hall floor) south to z 2.5; landing z .9..2.5; flight 2: x 25.8..28.2 north to z 6.4
        void Flight(float xa,float xb,float zStart,float dir,float yStart)
        {
            float lo=Mathf.Min(X(xa),X(xb)),hi=Mathf.Max(X(xa),X(xb));
            for(int i=0;i<13;i++)
            {
                float z0=zStart+dir*i*run,z1=z0+dir*(run+(i<12?.02f:0f)),y=yStart+(i+1)*rise;
                B(granite,lo,hi,y-.05f,y,Mathf.Min(z0,z1),Mathf.Max(z0,z1));
                B(concrete,lo+.02f,hi-.02f,yStart-.25f+i*rise,y-.05f,Mathf.Min(z0,z0+dir*run),Mathf.Max(z0,z0+dir*run));
                float zn=z0+dir*.0f;Flat(paintYellow,lo+.05f,hi-.05f,Mathf.Min(zn,zn+dir*.05f),Mathf.Max(zn,zn+dir*.05f),y+.012f);
            }
            float len=13*run,h=13*rise,zc=zStart+dir*len*.5f;
            var o=new GameObject("schody piętro | kolizja");o.transform.SetParent(cols,false);
            o.transform.localPosition=new Vector3((lo+hi)*.5f,yStart+h*.5f-.15f,zc)*K;
            o.transform.localRotation=Quaternion.Euler(dir*Mathf.Atan2(h,len)*Mathf.Rad2Deg,0,0);
            o.AddComponent<BoxCollider>().size=new Vector3(hi-lo,.3f,Mathf.Sqrt(len*len+h*h))*K;Colliders++;
            foreach(float x in new[]{lo,hi})
            {
                var a=new Vector3(x,yStart,zStart);var b=new Vector3(x,yStart+h,zStart+dir*len);
                Q(glass,a,b,b+Vector3.up*.95f,a+Vector3.up*.95f,Vector3.right);Q(glass,a,b,b+Vector3.up*.95f,a+Vector3.up*.95f,Vector3.left);
                Beam(stainless,a+Vector3.up*.97f,b+Vector3.up*.97f,.045f,.045f);
                Beam(steelNavy,a+new Vector3(0,-.12f,0),b+new Vector3(0,-.12f,0),.08f,.30f);
            }
        }
        float y1=FFL+13*rise;
        Flight(23.0f,25.4f,6.4f,-1f,FFL);
        B(granite,Mathf.Min(X(23f),X(28.2f)),Mathf.Max(X(23f),X(28.2f)),y1-.25f,y1,.9f,2.5f,true);
        Flight(25.8f,28.2f,2.5f,1f,y1);
        // the hole's balustrades at +1: along the front (z .9) and the inner side (x 23) and the north edge over flight 1
        float lo=Mathf.Min(X(23f),X(28.2f)),hi=Mathf.Max(X(23f),X(28.2f));
        void Rail(Vector3 a,Vector3 b)
        {
            var n=Vector3.Cross(b-a,Vector3.up);
            Q(glass,a,b,b+Vector3.up*1.05f,a+Vector3.up*1.05f,n);Q(glass,a,b,b+Vector3.up*1.05f,a+Vector3.up*1.05f,-n);
            Beam(stainless,a+Vector3.up*1.08f,b+Vector3.up*1.08f,.05f,.05f);
            var c=(a+b)*.5f;Col(c.x-Mathf.Abs(b.x-a.x)*.5f-.05f,c.x+Mathf.Abs(b.x-a.x)*.5f+.05f,UpY,UpY+1.1f,c.z-Mathf.Abs(b.z-a.z)*.5f-.05f,c.z+Mathf.Abs(b.z-a.z)*.5f+.05f,"balustrada");
        }
        Rail(new Vector3(lo,UpY,.9f),new Vector3(hi,UpY,.9f));
        Rail(new Vector3(X(23f),UpY,.9f),new Vector3(X(23f),UpY,6.4f));
        Rail(new Vector3(X(23f),UpY,6.4f),new Vector3(X(25.6f),UpY,6.4f));
        // the lift: shaft x 28.4..29.94, z 6.6..8.8, doors toward the stair (-s x)
        float lx0=Mathf.Min(X(28.4f),X(29.94f)),lx1=Mathf.Max(X(28.4f),X(29.94f));
        foreach(float y in new[]{FFL,UpY})
        {
            float dx=s<0?lx1:lx0;
            B(stainless,dx-.04f,dx+.04f,y,y+2.2f,6.68f,7.25f,true);B(stainless,dx-.04f,dx+.04f,y,y+2.2f,8.15f,8.72f,true);
            B(stainless,dx-.04f,dx+.04f,y+2.2f,y==FFL?Floor1:y+2.58f,6.68f,8.72f,true);
        }
        B(plaster,lx0,lx1,FFL,UpY+2.6f,6.6f,6.68f,true);B(plaster,lx0,lx1,FFL,UpY+2.6f,8.72f,8.8f,true);
        BuildingLift("D2 winda piętro "+(s<0?"zachód":"wschód"),(lx0+lx1)*.5f,7.7f,.62f,.98f,s*90f,new[]{FFL+.012f,UpY+.012f},new[]{"HOL","PIĘTRO"},.45f);
    }
    // a lift in the building (BraxWinda): car hw (along the door) x hd (depth), the door face at the car's local -z
    static void BuildingLift(string id,float xc,float zc,float hw,float hd,float yaw,float[] stops,string[] names,float doorHalf)
    {
        var landL=new Transform[stops.Length];var landR=new Transform[stops.Length];
        for(int i=0;i<stops.Length;i++)
        {
            var land=Group(root,id+" | przystanek "+names[i]);land.localPosition=new Vector3(xc,stops[i],zc)*K;land.localRotation=Quaternion.Euler(0,yaw,0);
            foreach(float side in new[]{-1f,1f})
            {
                var t=Group(land,"drzwi przystanku "+(side<0?"L":"P"));t.localPosition=new Vector3(side*doorHalf*.5f,0,-hd-.16f)*K;
                var lm=new BraxChampionsMeshBatch();lm.BoxMinMax(stainless,-doorHalf*.5f,doorHalf*.5f,0f,2.15f,-.015f,.015f);lm.Build(t,"drzwi przystanku",Folder,false);
                BraxChampionsMeshBatch.Collider(t,"drzwi przystanku",new Vector3(0,1.07f,0),new Vector3(doorHalf,2.15f,.05f));
                foreach(var c in t.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(c.gameObject,0);
                if(side<0)landL[i]=t;else landR[i]=t;
            }
        }
        var cab=Group(root,id+" | kabina");cab.localPosition=new Vector3(xc,stops[0],zc)*K;cab.localRotation=Quaternion.Euler(0,yaw,0);
        var cm=new BraxChampionsMeshBatch();
        cm.BoxMinMax(granite,-hw,hw,-.12f,0f,-hd,hd);
        cm.BoxMinMax(stainless,-hw,hw,0f,2.4f,hd,hd+.04f);
        foreach(float sd in new[]{-1f,1f})cm.BoxMinMax(stainless,sd>0?hw:-hw-.04f,sd>0?hw+.04f:-hw,0f,2.4f,-hd,hd);
        cm.BoxMinMax(stainless,-hw-.04f,hw+.04f,2.4f,2.48f,-hd,hd+.04f);
        cm.BoxMinMax(led,-.3f,.3f,2.37f,2.4f,-hd+.3f,hd-.3f);
        foreach(float sd in new[]{-1f,1f})cm.BoxMinMax(stainless,sd>0?doorHalf:-hw,sd>0?hw:-doorHalf,0f,2.4f,-hd-.03f,-hd);
        cm.BoxMinMax(stainless,-hw,hw,2.15f,2.4f,-hd-.03f,-hd);
        cm.BoxMinMax(stainless,-hw+.05f,hw-.05f,.9f,.94f,hd-.03f,hd);
        cm.Build(cab,"kabina",Folder,false);
        BraxChampionsMeshBatch.Collider(cab,"podłoga kabiny",new Vector3(0,-.06f,0),new Vector3(2*hw,.12f,2*hd));
        foreach(float sd in new[]{-1f,1f})BraxChampionsMeshBatch.Collider(cab,"ściana kabiny",new Vector3(sd*hw,1.2f,0),new Vector3(.05f,2.4f,2*hd));
        BraxChampionsMeshBatch.Collider(cab,"tył kabiny",new Vector3(0,1.2f,hd+.02f),new Vector3(2*hw,2.4f,.05f));
        var cabL=Group(cab,"drzwi kabiny L");var cabR=Group(cab,"drzwi kabiny P");
        var dm=new BraxChampionsMeshBatch();dm.BoxMinMax(stainless,-doorHalf,0f,0f,2.15f,-hd-.07f,-hd-.04f);dm.Build(cabL,"drzwi kabiny L",Folder,false);
        dm=new BraxChampionsMeshBatch();dm.BoxMinMax(stainless,0f,doorHalf,0f,2.15f,-hd-.07f,-hd-.04f);dm.Build(cabR,"drzwi kabiny P",Folder,false);
        foreach(var t in cab.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        var w=cab.gameObject.AddComponent<BraxWinda>();
        w.nazwa=id.ToUpperInvariant();w.kabina=cab;w.kabinaL=cabL;w.kabinaP=cabR;
        var py=new float[stops.Length];var call=new Vector3[stops.Length];
        var front=Quaternion.Euler(0,yaw,0)*Vector3.back;
        for(int i=0;i<stops.Length;i++){py[i]=root.TransformPoint(new Vector3(0,stops[i],0)*K).y;call[i]=root.TransformPoint((new Vector3(xc,stops[i]+1.2f,zc)+front*(hd+1.0f))*K);}
        w.pietraY=py;w.pietraNazwy=names;var u=new bool[stops.Length];for(int i=0;i<u.Length;i++)u[i]=true;w.uzytkowe=u;w.wezwanie=call;
        w.przystanekL=landL;w.przystanekP=landR;
        var shaft=root.TransformPoint(new Vector3(xc,0,zc)*K);
        var ext=Quaternion.Euler(0,yaw,0)*new Vector3(hw,0,hd);
        w.szyb=new Vector2(shaft.x,shaft.z);w.wnetrze=new Vector2(Mathf.Abs(ext.x)*K,Mathf.Abs(ext.z)*K);
        w.przesuw=doorHalf*.95f*K;w.start=0;Lifts++;
    }

    // ================================================================ +1: food court (west), shops and offices (east)
    static void UpperFloor(float s)
    {
        float X(float v)=>s*v;float y=UpY;
        // ceiling: white under the attic slab with a grid of downlights
        float lo=Mathf.Min(X(HallW+.06f),X(BW-.06f)),hi=Mathf.Max(X(HallW+.06f),X(BW-.06f));
        Flip(ceilWhite,lo,hi,.06f,BD-.06f,Eave-.42f);
        for(float x=21f;x<39f;x+=3f)for(float z=2f;z<35f;z+=3f){var p=new Vector3(X(x),Eave-.43f,z);m.Cylinder(ledWarm,p+Vector3.down*.0f,.09f,.012f,12,Quaternion.Euler(180,0,0),true,false);}
        // the walls of the +1 rooms toward the outside are the facade glazing; inner partitions here
        // the hall's vault springs at 6.8 inside, the wing's ceiling is at 7.58: a navy edge beam closes the gap over the gallery
        float ex=s*HallW;
        B(navy,Mathf.Min(ex,ex+s*.25f),Mathf.Max(ex,ex+s*.25f),VaultIn(HallW)-.15f,Eave-.40f,6.0f,32.5f);
        if(s<0)FoodCourt(y);else Shops(y);
    }
    static void FoodCourt(float y)
    {
        string[] names={"PIZZERIA","KAWIARNIA","BISTRO"};
        for(int i=0;i<3;i++)
        {
            float z0=10f+i*8.2f,z1=z0+7.6f,xb=-39.6f,xf=-36.4f;
            // back wall with the walnut panelling and the navy name band; the counter in front (marble top, walnut front, LED)
            B(wallPanel,xb-.1f,xb,y,y+2.9f,z0,z1,true);
            B(navy,xb,xb+.25f,y+2.25f,y+2.95f,z0+.2f,z1-.2f);
            var lm=new BraxChampionsMeshBatch();
            BraxChampionsLetters3D.Text(lm,stainless,frame,names[i],new Vector3(xb+.252f,y+2.40f,(z0+z1)*.5f),90f,.36f,.04f,.10f,z1-z0-1f);
            BraxChampionsLetters3D.Build(lm,root,"D2 napis "+names[i]);Signs++;
            for(int k=0;k<3;k++){float zc=z0+1.4f+k*2.4f;B(black,xb,xb+.05f,y+1.45f,y+2.1f,zc-.55f,zc+.55f);B(screenLit(),xb+.05f,xb+.056f,y+1.5f,y+2.05f,zc-.5f,zc+.5f);}
            B(walnut,xf-.6f,xf,y,y+1.02f,z0+.4f,z1-.4f,true);
            B(counterTop,xf-.66f,xf+.04f,y+1.02f,y+1.07f,z0+.35f,z1-.35f);
            B(black,xf-.55f,xf-.05f,y,y+.10f,z0+.42f,z1-.42f);
            B(led,xf+.0f,xf+.012f,y+.10f,y+.12f,z0+.5f,z1-.5f);
            // the back counter with appliances (stainless)
            B(stainless,xb,xb+.65f,y,y+.92f,z0+.3f,z1-.3f,true);
        }
        // seating: 12 round tables, 4 upholstered chairs each
        for(int r=0;r<4;r++)for(int c=0;c<3;c++)
        {
            float x=-32.5f+c*4.4f,z=8.0f+r*6.6f;
            CafeTable(x,y,z);
            for(int k=0;k<4;k++){float a=k*90f+45f;var d=new Vector3(Mathf.Sin(a*Mathf.Deg2Rad),0,Mathf.Cos(a*Mathf.Deg2Rad));Chair(new Vector3(x,y,z)+d*.78f,-d);}
        }
        Prop(root,"ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf",new Vector3(-20.0f,y,33.5f),30f,.3f);
        Prop(root,"ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf",new Vector3(-20.0f,y,10.6f),80f,.3f);
    }
    // round table: white marble top D .80 on a stainless column and a cast base
    static void CafeTable(float x,float y,float z)
    {
        var c=new Vector3(x,y,z);
        m.Lathe(benchFrame,c,new[]{new Vector2(.30f,0f),new Vector2(.30f,.02f),new Vector2(.06f,.06f),new Vector2(.035f,.08f)},24,Quaternion.identity,false,false);
        m.Cylinder(stainless,c+Vector3.up*.08f,.035f,.62f,12,Quaternion.identity,false);
        m.Lathe(counterTop,c+Vector3.up*.70f,new[]{new Vector2(.06f,0f),new Vector2(.40f,.0f),new Vector2(.41f,.015f),new Vector2(.41f,.035f),new Vector2(.40f,.05f),new Vector2(.0f,.05f)},40,Quaternion.identity,false,false);
        Col(x-.4f,x+.4f,y,y+.75f,z-.4f,z+.4f,"stolik");
    }
    // a chair: bent steel frame, upholstered navy leather seat and back (rounded); a BraxSeat
    static void Chair(Vector3 p,Vector3 face)
    {
        face.y=0;face.Normalize();var u=Vector3.Cross(Vector3.up,face).normalized;var rot=Quaternion.LookRotation(-face,Vector3.up);
        Vector3 P(float a,float h,float d)=>p+u*a+Vector3.up*h-face*d;
        foreach(float a in new[]{-.2f,.2f})
        {
            m.Tube(benchFrame,P(a,0f,-.19f),P(a,.44f,-.17f),.012f,6);
            m.Tube(benchFrame,P(a,0f,.20f),P(a,.44f,.17f),.012f,6);
            m.Tube(benchFrame,P(a,.44f,.17f),P(a,.86f,.22f),.012f,6);
        }
        m.RoundedBox(leatherNavy,P(0,.475f,0f),new Vector3(.46f,.07f,.44f),.025f,rot,3);
        m.RoundedBox(leatherNavy,P(0,.70f,.20f),new Vector3(.44f,.28f,.05f),.02f,rot*Quaternion.Euler(8f,0,0),3);
        Seat(P(0,.51f,0).x,P(0,.51f,0).y,P(0,.51f,0).z,face,"krzesło");
        var cc=P(0,.45f,0);Col(cc.x-.25f,cc.x+.25f,p.y,p.y+.9f,cc.z-.25f,cc.z+.25f,"krzesło");
    }
    static void Shops(float y)
    {
        // the corridor along the gallery: benches facing the hall, planters
        for(int i=0;i<3;i++)StationBench(new Vector3(24.5f,y,12f+i*8f),Vector3.left,4,"ławka piętro "+i,2,true);
        foreach(float z in new[]{8f,16f,24f,32f})Prop(root,"ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf",new Vector3(29.5f,y,z),z*13f,.3f);
        string[] names={"PRASA · KSIĄŻKI","PAMIĄTKI","BIURO PODRÓŻY"};
        for(int i=0;i<3;i++)
        {
            float z0=8.5f+i*9.0f,z1=z0+8.4f,xf=33f;
            // glass front with a door, the name on a navy band over it
            GlazingZ(xf,z0,z0+3.0f,y,y+2.9f,1f);GlazingZ(xf,z0+4.6f,z1,y,y+2.9f,1f);GlazingZ(xf,z0+3.0f,z0+4.6f,y+2.4f,y+2.9f,1f);
            Door("D2_up_"+i,"D2 piętro "+names[i],xf,y,z0+3.8f,90f,1.6f,2.35f,true);
            B(navy,xf-.25f,xf,y+2.9f,y+3.35f,z0,z1,true);
            var lm=new BraxChampionsMeshBatch();
            BraxChampionsLetters3D.Text(lm,stainless,frame,names[i],new Vector3(xf-.252f,y+2.98f,(z0+z1)*.5f),-90f,.28f,.03f,.10f,z1-z0-1f);
            BraxChampionsLetters3D.Build(lm,root,"D2 napis "+names[i]);Signs++;
            // side walls between the units (walnut lining inside)
            B(plaster,xf,BW-.1f,y,Eave-.42f,z1,z1+.12f,true);
            if(i==0)B(plaster,xf,BW-.1f,y,Eave-.42f,z0-.12f,z0,true);
            // inside: shelves (walnut, white shelves) along the side walls, a counter, or office desks
            if(i<2)
            {
                foreach(float zz in new[]{z0+.35f,z1-.35f})
                {
                    B(walnut,34f,39.4f,y,y+2.2f,zz-.2f,zz+.2f,true);
                    for(int k=1;k<5;k++)B(counterTop,34.05f,39.35f,y+k*.45f,y+k*.45f+.03f,zz-.24f,zz+.24f);
                }
                B(walnut,35f,37.2f,y,y+1.0f,(z0+z1)*.5f-.4f,(z0+z1)*.5f+.4f,true);B(counterTop,34.95f,37.25f,y+1.0f,y+1.05f,(z0+z1)*.5f-.45f,(z0+z1)*.5f+.45f);
            }
            else
            {
                for(int k=0;k<2;k++)
                {
                    float zc=z0+2.4f+k*3.6f;
                    B(walnut,35f,37.4f,y+.70f,y+.74f,zc-.75f,zc+.75f);B(benchFrame,35.1f,35.15f,y,y+.70f,zc-.7f,zc+.7f);B(benchFrame,37.25f,37.3f,y,y+.70f,zc-.7f,zc+.7f);
                    B(black,36.6f,36.63f,y+.74f,y+1.12f,zc-.3f,zc+.3f);B(screenLit(),36.57f,36.6f,y+.78f,y+1.08f,zc-.27f,zc+.27f);
                    Chair(new Vector3(37.9f,y,zc),Vector3.left);Chair(new Vector3(34.3f,y,zc),Vector3.right);
                    Col(35f,37.4f,y,y+.74f,zc-.75f,zc+.75f,"biurko");
                }
            }
        }
    }
}
