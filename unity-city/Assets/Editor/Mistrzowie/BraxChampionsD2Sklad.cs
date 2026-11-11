using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// D2 train built in code (owner 11.11 15:28-15:31: "pociąg możesz też stworzyć? żeby był lżejszy, a funkcjonalny, ze
// środkiem, ze wszystkim, z lokomotywą ... zrób normalnie wagony, żeby stał na razie i środek tak samo ... żeby można było
// wejść do pociągu"; 15:47 "wszystko, co stworzymy, ma być funkcjonalne w życiu"). Replaces the metro model (4 x 95k tris,
// 25 MB): one modern electric locomotive + 3 low-floor coaches, ~100 m, standing at platform 1 (BraxChampionsD2Pociag.staly).
//
// * LOCOMOTIVE (18.9 m over buffers, 3.0 m wide): two cabs (sloped windscreens in black frames, desk with screens, two
//   seats, linings - the cab is seen through the glass), navy body with a white band and a red nose / red solebar band,
//   "BRAXHORN" in 3D, louvre panels, cab doors with handrails and steps, two bogies (4 axles, wheels 1.25 m), buffers,
//   coupling, snow plough, head / tail lamps, roof: two single-arm pantographs on ceramic insulators (the front one raised
//   to the contact wire at 5.3 m), the roof line.
// * COACHES (26.4 m over buffers, 2.88 m wide, floor 12 mm over the platform - level boarding): livery bands (navy, red
//   line, black window band, white top; coach 1 = 1st class with a yellow line), windows with tinted glass and black
//   reveals, plug doors at both ends of both sides (the platform side WORKS: BraxChampionsD2DrzwiPociagu - leaves out and
//   apart, the step slides out over the gap; the platform edge's invisible wall opens at the open doorway), vestibules
//   with rubber floors and grab poles, glass sliding doors to the saloon and through the gangways (rubber bellows,
//   walkable from coach to coach), saloon: oak-look vinyl floor, warm-white linings, a curved white ceiling with LED
//   lines, glass luggage racks on black brackets, 2+2 seats in navy moquette (1st class 2+1 in navy leather) with white
//   headrest covers, armrests, a vis-a-vis bay with tables, amber destination displays; every seat is a BraxSeat;
//   colliders for floors, walls, ends, partitions, seats and tables. Bogies with small wheels (0.70 m) under the ends.
// Everything at real size x K; nothing grey at any glass (black frames, owner 11.11 14:57); no coplanar faces (zero flicker).
public static partial class BraxChampionsD2
{
    const float TLocoLoB=18.9f,TCoachLoB=26.4f,TBuf=.45f,THW=1.44f,TWall=.08f,TLocoHW=1.5f,TFloor=.772f,TCeilEdge=2.922f,TCeilTop=3.22f,TBodyTop=3.30f,TRoofRise=.65f;
    public const int TrainCoaches=3;
    static Material tPaintNavy,tPaintWhite,tPaintRed,tPaintYellow,tRoof,tBogie,tWheel,tGlass,tLining,tCeiling,tFloor,tVestFloor,tMoquette,tHeadrest,tStep,tAmber,tGreen,tInsulator,tHeadLamp,tTailLamp,tPanto,tCarbon;
    static Transform tg;
    static List<Collider> trainGates=new List<Collider>();          // platform-edge gates at the doorways (made by Platform 1)

    static Vector3 TP(float x,float h,float w)=>new Vector3(x,RailTop+h,Track1Z+w);
    static void TB(Material mat,float x0,float x1,float h0,float h1,float w0,float w1)
    {
        if(x1-x0<1e-4f||h1-h0<1e-4f||w1-w0<1e-4f)return;
        m.BoxMinMax(mat,x0,x1,RailTop+h0,RailTop+h1,Track1Z+w0,Track1Z+w1);
    }
    static void TCol(string name,float x0,float x1,float h0,float h1,float w0,float w1)
    {
        if(x1-x0<1e-3f||h1-h0<1e-3f||w1-w0<1e-3f)return;
        BraxChampionsMeshBatch.Collider(tg,name,new Vector3((x0+x1)*.5f,RailTop+(h0+h1)*.5f,Track1Z+(w0+w1)*.5f),new Vector3(x1-x0,h1-h0,w1-w0));Colliders++;
    }
    // a vertical quad on a train side (w = const) facing `face` (+-1 along z)
    static void TSide(Material mat,float x0,float x1,float h0,float h1,float w,float face)
    {
        if(x1-x0<1e-4f||h1-h0<1e-4f)return;
        Q(mat,TP(x0,h0,w),TP(x1,h0,w),TP(x1,h1,w),TP(x0,h1,w),new Vector3(0,0,face));
    }
    static List<(float,float)> TCut(float a,float b,List<(float a,float b)> holes)
    {
        var r=new List<(float,float)>();float x=a;var hs=new List<(float a,float b)>(holes);hs.Sort((p,q)=>p.a.CompareTo(q.a));
        foreach(var h in hs){if(h.b<=x||h.a>=b)continue;if(h.a>x)r.Add((x,h.a));x=Mathf.Max(x,h.b);}
        if(b>x)r.Add((x,b));
        return r;
    }

    // the doors of the coaches (centre x): two per coach, 1.45 m from the body ends - shared with the platform edge
    public static List<float> TrainDoorXs()
    {
        var r=new List<float>();
        for(int k=0;k<TrainCoaches;k++){float x1=TrainStopX-TLocoLoB-k*TCoachLoB,b0=x1-TCoachLoB+TBuf,b1=x1-TBuf;r.Add(b0+1.45f);r.Add(b1-1.45f);}
        return r;
    }

    static void TrainMaterials()
    {
        if(!oakSlat)FurnitureMaterials();
        tPaintNavy=S("train_paint_navy",new Color(.045f,.10f,.25f),.20f,.35f);
        tPaintWhite=S("train_paint_white",new Color(.92f,.93f,.94f),.22f,.15f);
        tPaintRed=S("train_paint_red",new Color(.72f,.06f,.07f),.24f,.25f);
        tPaintYellow=S("train_paint_first_class_yellow",new Color(.96f,.76f,.08f),.25f,.15f);
        tRoof=S("train_roof_graphite",new Color(.17f,.18f,.20f),.55f,.45f);
        tBogie=S("train_bogie_black",new Color(.05f,.05f,.055f),.6f,.5f);
        tWheel=S("train_wheel_steel",new Color(.55f,.56f,.58f),.35f,.9f);
        tGlass=S("train_window_glass",new Color(.16f,.21f,.25f,.36f),.04f,.15f,null,true);
        tLining=Inside(S("train_lining_warm_white",new Color(.90f,.89f,.86f),.45f,0f),.9f);
        tCeiling=Inside(S("train_ceiling_white",new Color(.94f,.94f,.93f),.5f,0f),.9f);
        tFloor=Inside(Tex("train_floor_vinyl_oak","laminate_floor_02",1.2f,new Color(.78f,.72f,.66f)),.85f);
        tVestFloor=Inside(Tex("train_vestibule_rubber","rubber_tiles",.6f,new Color(.30f,.31f,.34f)),.85f);
        tMoquette=Inside(Tex("train_moquette_navy","fabric_pattern_05",.5f,new Color(.32f,.40f,.66f)),.85f);
        tHeadrest=Inside(S("train_headrest_cloth",new Color(.95f,.95f,.93f),.95f,0f),.9f);
        tStep=S("train_step_tread",new Color(.10f,.10f,.11f),.5f,.6f);
        tAmber=S("train_display_amber",new Color(1f,.62f,.12f),.3f,0f,new Color(1.2f,.66f,.12f));
        tGreen=S("train_button_green",new Color(.2f,.85f,.4f),.3f,0f,new Color(.25f,1.0f,.45f));
        tInsulator=S("train_insulator_ceramic",new Color(.80f,.78f,.72f),.25f,0f);
        tHeadLamp=S("train_headlamp",Color.white,.1f,0f,new Color(1.6f,1.6f,1.5f));
        tTailLamp=S("train_taillamp",new Color(.9f,.05f,.05f),.2f,0f,new Color(1.2f,.05f,.05f));
        tPanto=S("train_pantograph_steel",new Color(.30f,.31f,.33f),.4f,.8f);
        tCarbon=S("train_pantograph_carbon",new Color(.08f,.08f,.08f),.7f,0f);
    }

    // ================================================================ the train
    static void TrainSet()
    {
        TrainMaterials();
        trainSeats=0;
        Flush("D2 zegary i reszta przed pociągiem");                             // nothing of the station may end up in the moving group
        tg=Group(root,"D2 pociąg | lokomotywa + "+TrainCoaches+" wagony (z kodu) - BraxChampionsD2Pociag");
        float front=TrainStopX;
        Locomotive(front-TLocoLoB,front);
        Flush("D2 pociąg | lokomotywa",tg);
        for(int k=0;k<TrainCoaches;k++)
        {
            float x1=front-TLocoLoB-k*TCoachLoB,x0=x1-TCoachLoB;
            Coach(k,x0+TBuf,x1-TBuf,k==0,k==TrainCoaches-1);
            Flush("D2 pociąg | wagon "+(k+1),tg);
        }
        // gangways between the coaches (bellows, walkable)
        for(int k=0;k+1<TrainCoaches;k++){float xe=front-TLocoLoB-(k+1)*TCoachLoB;Gangway(xe-TBuf,xe+TBuf);}
        Flush("D2 pociąg | przejścia",tg);
        foreach(var t in tg.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        float len=TLocoLoB+TrainCoaches*TCoachLoB;
        var run=tg.gameObject.AddComponent<BraxChampionsD2Pociag>();trainRun=run;
        run.wjazdX=(LX(PortalWX)-60f-TrainStopX)*K;run.wyjazdX=(LX(TunnelEndWX)+10f+len-TrainStopX)*K;run.drugiTorZ=(Track2Z-Track1Z)*K;
        run.predkosc=TrainSpeed*K;run.hamowanie=.9f*K;run.postoj=240f;run.przerwa=35f;run.staly=true;
        Debug.Log("D2_POCIAG z kodu: lokomotywa + "+TrainCoaches+" wagony, długość "+len.ToString("0.0")+" m, drzwi peronowe="+(TrainCoaches*2)+" (działają), miejsca="+trainSeats+", stopX="+TrainStopX);
    }
    static int trainSeats;
    static void TSeat(float x,float h,float w,float f,string label)
    {
        var g=new GameObject("Siedzisko "+label);g.transform.SetParent(tg,false);
        g.transform.localPosition=TP(x,h,w)*K;g.transform.localRotation=Quaternion.LookRotation(new Vector3(f,0,0));
        var s=g.AddComponent<BraxSeat>();s.label=label;s.surfaceY=root.TransformPoint(TP(x,h,w)*K).y;
        Seats++;trainSeats++;
    }

    // ---------------------------------------------------------------- coach (body b0..b1)
    static void Coach(int k,float b0,float b1,bool first,bool tail)
    {
        float s0=b0+2.68f,s1=b1-2.68f;                                    // the saloon between the vestibule partitions
        var doors=new List<(float a,float b)>{(b0+.80f,b0+2.10f),(b1-2.10f,b1-.80f)};
        var wells=new List<(float a,float b)>{(b0+2.45f,b0+5.95f),(b1-5.95f,b1-2.45f)};
        // windows: pitch 1.8, 1.50 wide, centred on the saloon
        var wins=new List<(float a,float b)>();
        {int n=Mathf.FloorToInt((s1-s0-.2f)/1.8f);float st=s0+(s1-s0-n*1.8f)*.5f;for(int i=0;i<n;i++){float a=st+i*1.8f+.15f;wins.Add((a,a+1.5f));}}
        foreach(float s in new[]{-1f,1f})
        {
            float w=s*THW,wi=s*(THW-TWall);
            // outer skin: livery bands cut by the openings
            void Band(Material mat,float h0,float h1,List<(float a,float b)> holes){foreach(var (a,b) in TCut(b0,b1,holes))TSide(mat,a,b,h0,h1,w,s);}
            var dw=new List<(float a,float b)>(doors);dw.AddRange(wins);
            Band(tPaintNavy,.30f,.74f,wells);
            Band(tPaintNavy,.74f,TFloor,new List<(float,float)>());
            Band(tPaintNavy,TFloor,1.45f,doors);
            Band(tPaintRed,1.45f,1.50f,doors);
            Band(frame,1.50f,1.55f,doors);
            Band(frame,1.55f,2.55f,dw);
            Band(frame,2.55f,2.62f,doors);
            Band(tPaintWhite,2.62f,2.772f,doors);
            Band(tPaintWhite,2.772f,3.08f,new List<(float,float)>());
            Band(first?tPaintYellow:tPaintWhite,3.08f,3.14f,new List<(float,float)>());
            Band(tPaintWhite,3.14f,TBodyTop,new List<(float,float)>());
            // inner lining (inside the end walls), cut by doors and windows
            void Lin(float h0,float h1,List<(float a,float b)> holes){foreach(var (a,b) in TCut(b0+.11f,b1-.11f,holes))TSide(tLining,a,b,h0,h1,wi,-s);}
            Lin(TFloor,1.55f,doors);Lin(1.55f,2.55f,dw);Lin(2.55f,2.772f,doors);Lin(2.772f,TCeilEdge,new List<(float,float)>());
            // windows: tinted glass between skin and lining, black reveals
            foreach(var (a,b) in wins)
            {
                float wg=s*(THW-.04f);
                var g0=TP(a,1.55f,wg);var g1=TP(b,1.55f,wg);var g2=TP(b,2.55f,wg);var g3=TP(a,2.55f,wg);
                Q(tGlass,g0,g1,g2,g3,Vector3.forward);Q(tGlass,g0,g1,g2,g3,Vector3.back);
                Reveal(a,b,1.55f,2.55f,w,wi);
            }
            // door reveals (black) and the threshold
            foreach(var (a,b) in doors)
            {
                Reveal(a,b,TFloor,2.772f,w,wi,false);
                TB(frame,a,b,.70f,TFloor,Mathf.Min(s*(THW-.01f),wi),Mathf.Max(s*(THW-.01f),wi));
            }
            // track side: the doors stay shut (leaves in the openings, as built)
            if(s>0)foreach(var (a,b) in doors)StaticLeaves(m,(a+b)*.5f,s);
            // brand and coach numbers (3D, 15 mm proud)
            var lm=new BraxChampionsMeshBatch();
            float yaw=s<0?180f:0f;
            BraxChampionsLetters3D.Text(lm,tPaintNavy,tPaintNavy,"BRAXHORN EXPRESS",TP((b0+b1)*.5f,2.80f,s*(THW+.002f)),yaw,.20f,.015f,.10f,7.5f);
            foreach(var (a,b) in doors)
            {
                float xc=(a+b)*.5f<(b0+b1)*.5f?b+.55f:a-.55f;
                BraxChampionsLetters3D.Text(lm,whiteLetters,tPaintNavy,(k+1).ToString(),TP(xc,.98f,s*(THW+.002f)),yaw,.30f,.015f,.10f,.6f);
            }
            BraxChampionsLetters3D.Build(lm,tg,"D2 pociąg napisy wagon "+(k+1)+(s<0?" peron":" tor"));Signs++;
        }
        // floors: vestibules rubber, saloon oak-look vinyl; under the gangway openings a black threshold
        TB(tVestFloor,b0+.11f,b0+2.64f,.62f,TFloor,-(THW-TWall),THW-TWall);
        TB(tFloor,b0+2.64f,b1-2.64f,.62f,TFloor,-(THW-TWall),THW-TWall);
        TB(tVestFloor,b1-2.64f,b1-.11f,.62f,TFloor,-(THW-TWall),THW-TWall);
        // underframe between and beyond the bogies (black), behind the skirts
        TB(tBogie,b0+6.1f,b1-6.1f,.32f,.62f,-(THW-.10f),THW-.10f);
        TB(tBogie,b0+.15f,b0+2.3f,.32f,.62f,-(THW-.10f),THW-.10f);TB(tBogie,b1-2.3f,b1-.15f,.32f,.62f,-(THW-.10f),THW-.10f);
        // roof (white cant rails, graphite roof), end caps, AC units
        TrainRoof(b0,b1,THW,TBodyTop,TRoofRise,true);
        foreach(float xa in new[]{b0+5.5f,b1-8.5f})TB(tRoof,xa,xa+3.0f,3.75f,TBodyTop+TRoofRise+.22f,-.85f,.85f);
        // end walls with the gangway openings
        EndWall(b1,1f,!(k==0));         // east end: to the loco on coach 1 (shut), else the gangway
        EndWall(b0,-1f,!tail);          // west end: the tail of the train on the last coach (shut)
        if(tail)foreach(float w in new[]{-1.0f,1.0f})m.Cylinder(tTailLamp,TP(b0-.002f,1.30f,w),.07f,.03f,16,Quaternion.Euler(0,0,90),true,false);
        // buffers both ends (the gangway bellows sit between them)
        foreach(float e in new[]{b0,b1})Buffers(e,e<(b0+b1)*.5f?-1f:1f,.95f,1.06f);
        // bogies under the ends
        foreach(float bc in new[]{b0+4.2f,b1-4.2f})Bogie(bc,1.25f,.35f,.86f,true);
        // vestibule / saloon partitions with the glass sliding doors
        Partition(s0-.08f,s0,1f,"wagon "+(k+1)+" zachód");
        Partition(s1,s1+.08f,-1f,"wagon "+(k+1)+" wschód");
        // the platform doors (working) with the edge gates
        int di=0;
        foreach(var (a,b) in doors){PlatformDoor((a+b)*.5f,"wagon "+(k+1)+" drzwi "+(di+1),k*2+di);di++;}
        // vestibules: grab poles by the doors, both sides
        foreach(var (a,b) in doors)foreach(float s in new[]{-1f,1f})foreach(float xx in new[]{a-.06f,b+.06f})
            m.Cylinder(frame,TP(xx,TFloor,s*(THW-TWall-.14f)),.02f,1.95f,10,Quaternion.identity,true);
        // ceiling: curved white panels with two LED lines, the whole length inside the end walls
        Ceiling(b0+.11f,b1-.11f);
        // saloon: seats, tables, luggage racks, displays
        Saloon(k,s0,s1,first);
        // colliders: floor, sides (platform side open at the doors), roof, ends
        TCol("pociąg podłoga",b0,b1,.40f,TFloor,-THW,THW);
        foreach(var (a,b) in TCut(b0,b1,doors))TCol("pociąg ściana",a,b,TFloor,2.95f,-THW,-(THW-TWall));
        TCol("pociąg ściana",b0,b1,TFloor,2.95f,THW-TWall,THW);
        TCol("pociąg sufit",b0,b1,2.95f,3.10f,-THW,THW);
        foreach(var (a,b) in doors)TCol("pociąg nad drzwiami",a,b,2.772f,2.95f,-THW,-(THW-TWall));
    }
    // black reveals of an opening a..b, h0..h1 through the wall (w outer, wi inner)
    static void Reveal(float a,float b,float h0,float h1,float w,float wi,bool sill=true)
    {
        float lo=Mathf.Min(w,wi),hi=Mathf.Max(w,wi);
        Q(frame,TP(a,h0,lo),TP(a,h0,hi),TP(a,h1,hi),TP(a,h1,lo),Vector3.right);
        Q(frame,TP(b,h0,lo),TP(b,h0,hi),TP(b,h1,hi),TP(b,h1,lo),Vector3.left);
        Q(frame,TP(a,h1,lo),TP(b,h1,lo),TP(b,h1,hi),TP(a,h1,hi),Vector3.down);
        if(sill)Q(frame,TP(a,h0,lo),TP(b,h0,lo),TP(b,h0,hi),TP(a,h0,hi),Vector3.up);
    }
    // roof: an arc (w = hw cos t, h = top + rise sin t) from b0 to b1, the two lowest segments each side white (cant rail)
    static void TrainRoof(float b0,float b1,float hw,float top,float rise,bool caps)
    {
        const int N=12;
        for(int i=0;i<N;i++)
        {
            float t0=Mathf.PI*i/N,t1=Mathf.PI*(i+1)/N;
            Vector2 p0=new Vector2(hw*Mathf.Cos(t0),top+rise*Mathf.Sin(t0)),p1=new Vector2(hw*Mathf.Cos(t1),top+rise*Mathf.Sin(t1));
            var mat=(i<2||i>=N-2)?tPaintWhite:tRoof;
            var nrm=new Vector3(0,(p0.y+p1.y)*.5f-top+.001f,(p0.x+p1.x)*.5f);
            Q(mat,TP(b0,p0.y,p0.x),TP(b1,p0.y,p0.x),TP(b1,p1.y,p1.x),TP(b0,p1.y,p1.x),nrm);
            if(caps)foreach(float e in new[]{b0,b1})T(tRoof,TP(e,top,0f),TP(e,p0.y,p0.x),TP(e,p1.y,p1.x),new Vector3(e<(b0+b1)*.5f?-1f:1f,0,0));
        }
    }
    // end wall at x = e (dir: +1 east end, -1 west end), the gangway opening w +-.50 x h .772..2.772; open = a gangway
    // with sliding glass doors, else a shut end door (black frame, glass)
    static void EndWall(float e,float dir,bool open)
    {
        float xo0=Mathf.Min(e,e-dir*.03f),xo1=Mathf.Max(e,e-dir*.03f);              // exterior plate (navy)
        float xl0=Mathf.Min(e-dir*.03f,e-dir*.11f),xl1=Mathf.Max(e-dir*.03f,e-dir*.11f);   // lining
        float hw=THW-.01f;
        TB(tPaintNavy,xo0,xo1,.30f,TFloor,-hw,hw);
        TB(tPaintNavy,xo0,xo1,TFloor,TBodyTop,-hw,-.50f);TB(tPaintNavy,xo0,xo1,TFloor,TBodyTop,.50f,hw);TB(tPaintNavy,xo0,xo1,2.772f,TBodyTop,-.50f,.50f);
        float hl=THW-TWall;
        TB(tLining,xl0,xl1,TFloor,TBodyTop,-hl,-.50f);TB(tLining,xl0,xl1,TFloor,TBodyTop,.50f,hl);TB(tLining,xl0,xl1,2.772f,TBodyTop,-.50f,.50f);
        TB(frame,xl0,xl1,.70f,TFloor,-.50f,.50f);                                  // threshold
        TCol("pociąg ściana czołowa",Mathf.Min(xo0,xl0),Mathf.Max(xo1,xl1),TFloor,2.95f,-THW,-.50f);
        TCol("pociąg ściana czołowa",Mathf.Min(xo0,xl0),Mathf.Max(xo1,xl1),TFloor,2.95f,.50f,THW);
        TCol("pociąg ściana czołowa",Mathf.Min(xo0,xl0),Mathf.Max(xo1,xl1),2.772f,2.95f,-.50f,.50f);
        float xd=e-dir*.15f;                                                       // the door leaves' plane, inside
        if(open)GlassSlidingDoor(xd,"przejście "+e.ToString("0.0"));
        else
        {
            // shut end door: black frame + glass, a collider
            m.BoxMinMax(frame,xd-.02f,xd+.02f,RailTop+TFloor,RailTop+TFloor+.10f,Track1Z-.52f,Track1Z+.52f);
            m.BoxMinMax(frame,xd-.02f,xd+.02f,RailTop+2.70f,RailTop+2.772f,Track1Z-.52f,Track1Z+.52f);
            m.BoxMinMax(frame,xd-.02f,xd+.02f,RailTop+TFloor+.10f,RailTop+2.70f,Track1Z-.52f,Track1Z-.46f);
            m.BoxMinMax(frame,xd-.02f,xd+.02f,RailTop+TFloor+.10f,RailTop+2.70f,Track1Z+.46f,Track1Z+.52f);
            var g0=TP(xd,TFloor+.09f,-.47f);var g1=TP(xd,TFloor+.09f,.47f);var g2=TP(xd,2.71f,.47f);var g3=TP(xd,2.71f,-.47f);
            Q(tGlass,g0,g1,g2,g3,Vector3.right);Q(tGlass,g0,g1,g2,g3,Vector3.left);
            TCol("pociąg drzwi czołowe zamknięte",xd-.03f,xd+.03f,TFloor,2.772f,-.52f,.52f);
        }
    }
    // the gangway between two coaches: x a..b (the gap between the end plates), ridged rubber bellows, a floor plate
    static void Gangway(float a,float b)
    {
        int n=Mathf.Max(3,Mathf.RoundToInt((b-a)/.1f));float d=(b-a)/n;
        for(int j=0;j<n;j++)
        {
            float x0=a+j*d,x1=x0+d,wo=j%2==0?.72f:.69f,ht=j%2==0?2.98f:2.95f;
            foreach(float s in new[]{-1f,1f})TB(rubber,x0,x1,.74f,2.80f,Mathf.Min(s*.56f,s*wo),Mathf.Max(s*.56f,s*wo));
            TB(rubber,x0,x1,2.80f,ht,-wo,wo);
        }
        TB(tStep,a,b,.70f,TFloor,-.56f,.56f);
        TCol("przejście podłoga",a,b,.40f,TFloor,-.56f,.56f);
        TCol("przejście ściana",a,b,TFloor,2.8f,-.72f,-.56f);TCol("przejście ściana",a,b,TFloor,2.8f,.56f,.72f);
    }
    // buffers at the body end e (dir outward), at +-wb, height hb; heads .45 out (they touch the neighbour's)
    static void Buffers(float e,float dir,float wb,float hb)
    {
        foreach(float w in new[]{-wb,wb})
        {
            var rot=Quaternion.Euler(0,0,-dir*90f);                                 // cylinder axis along +-x
            m.Cylinder(tBogie,TP(e,hb,w),.11f,.40f,16,rot,true,false);
            m.Cylinder(tWheel,TP(e+dir*.40f,hb,w),.20f,.05f-.002f,24,rot,true,true);
        }
    }
    // a bogie at xc: axles +-ax, wheel radius r, wheel outer face at +-wo (inner = wo - .14)
    static void Bogie(float xc,float ax,float r,float wo,bool lowFloor=false)
    {
        // low-floor coach: the frames stay under the floor (.62); the loco's ride higher
        float f0=lowFloor?r+.09f:r+.29f,f1=lowFloor?r+.25f:r+.45f;
        foreach(float x in new[]{xc-ax,xc+ax})
        {
            foreach(float s in new[]{-1f,1f})
            {
                float wa=s>0?wo-.14f:-wo;
                m.Cylinder(tWheel,TP(x,r,wa),r,.14f,24,Quaternion.Euler(90,0,0),true,true);
                TB(tBogie,x-.16f,x+.16f,r-.13f,r+.13f,Mathf.Min(s*wo,s*(wo+.14f)),Mathf.Max(s*wo,s*(wo+.14f)));   // axle box
                if(!lowFloor)m.Cylinder(tBogie,TP(x,r+.13f,s*(wo+.07f)),.07f,.16f,12,Quaternion.identity,true,false);   // spring
            }
            m.Tube(tBogie,TP(x,r,-(wo-.14f)),TP(x,r,wo-.14f),.065f,10);
        }
        foreach(float s in new[]{-1f,1f})TB(tBogie,xc-ax-.45f,xc+ax+.45f,f0,f1,Mathf.Min(s*(wo+.02f),s*(wo+.16f)),Mathf.Max(s*(wo+.02f),s*(wo+.16f)));   // side frames
        TB(tBogie,xc-.25f,xc+.25f,f0+.02f,f1-.02f,-(wo+.02f),wo+.02f);           // bolster
    }
    // the track-side (shut) door leaves, built into the coach: two .66 m leaves in the livery, glass in black frames
    static void StaticLeaves(BraxChampionsMeshBatch b,float xc,float s)
    {
        float wc=s*(THW-.035f);
        foreach(float side in new[]{-1f,1f})LeafMesh(b,new Vector3(xc+side*.33f,RailTop+TFloor,Track1Z+wc),s,side);
    }
    // one plug-door leaf (.66 x 2.0 x .04) centred at c (floor level), outside toward s; meetSide: where the other leaf is
    static void LeafMesh(BraxChampionsMeshBatch b,Vector3 c,float s,float side)
    {
        void L(Material mat,float x0,float x1,float y0,float y1){b.BoxMinMax(mat,c.x+x0,c.x+x1,c.y+y0,c.y+y1,c.z-.02f,c.z+.02f);}
        L(tPaintNavy,-.33f,.33f,0f,.678f);L(tPaintRed,-.33f,.33f,.678f,.728f);L(frame,-.33f,.33f,.728f,.778f);
        L(frame,-.33f,-.27f,.778f,1.778f);L(frame,.27f,.33f,.778f,1.778f);L(frame,-.33f,.33f,1.778f,1.848f);L(tPaintWhite,-.33f,.33f,1.848f,2.0f);
        var g0=new Vector3(c.x-.27f,c.y+.778f,c.z);var g1=new Vector3(c.x+.27f,c.y+.778f,c.z);var g2=new Vector3(c.x+.27f,c.y+1.778f,c.z);var g3=new Vector3(c.x-.27f,c.y+1.778f,c.z);
        Quad2(b,tGlass,g0,g1,g2,g3);
        // the open button by the meeting edge (green ring), outside and inside
        foreach(float f in new[]{-1f,1f})b.Cylinder(tGreen,new Vector3(c.x-side*.24f,c.y+1.05f,c.z+f*.02f),.035f,.015f,16,Quaternion.Euler(f*90f,0,0),true,false);
    }
    static void Quad2(BraxChampionsMeshBatch b,Material mat,Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3){b.Quad(mat,p0,p1,p2,p3);b.Quad(mat,p3,p2,p1,p0);}

    // a working platform door (platform side, w < 0): leaves out .085 then apart .66, the step out .30 over the gap
    static void PlatformDoor(float xc,string label,int gate)
    {
        float s=-1f,wc=s*(THW-.035f);
        var d=Group(tg,"D2 pociąg | "+label+" (otwierają się przy podejściu)");
        d.localPosition=TP(xc,TFloor,wc)*K;
        var leaves=new Transform[2];var outs=new Vector3[2];var slides=new Vector3[2];
        for(int i=0;i<2;i++)
        {
            float side=i==0?-1f:1f;
            var t=Group(d,"skrzydło "+(i+1));t.localPosition=new Vector3(side*.33f,0,0)*K;
            var lm=new BraxChampionsMeshBatch();LeafMesh(lm,Vector3.zero,s,side);lm.Build(t,"skrzydło drzwi pociągu",Folder,false);
            BraxChampionsMeshBatch.Collider(t,"skrzydło",new Vector3(0,1.0f,0),new Vector3(.66f,2.0f,.05f));
            leaves[i]=t;outs[i]=new Vector3(0,0,s*.085f)*K;slides[i]=new Vector3(side*.66f,0,0)*K;
        }
        // the sliding step: retracted under the floor (w -1.42..-1.12), out to -1.72 (the platform edge -1.725)
        var st=Group(d,"stopień");st.localPosition=Vector3.zero;
        var sm=new BraxChampionsMeshBatch();
        float z0=(-1.42f)-wc,z1=(-1.12f)-wc;                                      // local z (relative to the doorway)
        sm.BoxMinMax(tStep,-.64f,.64f,-.037f,-.008f,Mathf.Min(z0+.05f,z1),Mathf.Max(z0+.05f,z1));
        sm.BoxMinMax(tPaintYellow,-.64f,.64f,-.037f,-.008f,Mathf.Min(z0,z0+.05f),Mathf.Max(z0,z0+.05f));   // the yellow edge
        sm.Build(st,"stopień",Folder,false);
        BraxChampionsMeshBatch.Collider(st,"stopień",new Vector3(0,-.10f,(z0+z1)*.5f),new Vector3(1.28f,.20f,.30f));
        var c=d.gameObject.AddComponent<BraxChampionsD2DrzwiPociagu>();
        c.skrzydla=leaves;c.wysuw=outs;c.przesuw=slides;c.stopien=st;c.stopienWysuw=new Vector3(0,0,s*.30f)*K;c.promien=2.6f*K;c.czas=1.8f;
        if(gate<trainGates.Count&&trainGates[gate])c.blokady=new[]{trainGates[gate]};
        Doors++;
    }
    // a pair of glass sliding leaves in the plane x = xd across the opening w +-.50 (h .772..2.772), apart +-.50
    static void GlassSlidingDoor(float xd,string label)
    {
        var d=Group(tg,"D2 pociąg | "+label+" (szklane drzwi przesuwne)");
        d.localPosition=TP(xd,TFloor,0f)*K;
        var leaves=new Transform[2];var outs=new Vector3[2];var slides=new Vector3[2];
        for(int i=0;i<2;i++)
        {
            float side=i==0?-1f:1f;
            var t=Group(d,"skrzydło "+(i+1));t.localPosition=new Vector3(0,0,side*.255f)*K;
            var lm=new BraxChampionsMeshBatch();
            lm.BoxMinMax(frame,-.02f,.02f,0f,.10f,-.255f,.255f);lm.BoxMinMax(frame,-.02f,.02f,1.92f,2.0f,-.255f,.255f);
            lm.BoxMinMax(frame,-.02f,.02f,.10f,1.92f,-.255f,-.205f);lm.BoxMinMax(frame,-.02f,.02f,.10f,1.92f,.205f,.255f);
            var g0=new Vector3(0,.09f,-.215f);var g1=new Vector3(0,.09f,.215f);var g2=new Vector3(0,1.93f,.215f);var g3=new Vector3(0,1.93f,-.215f);
            Quad2(lm,glass,g0,g1,g2,g3);
            lm.Build(t,"skrzydło szklane",Folder,false);
            BraxChampionsMeshBatch.Collider(t,"skrzydło",new Vector3(0,1.0f,0),new Vector3(.05f,2.0f,.51f));
            leaves[i]=t;outs[i]=Vector3.zero;slides[i]=new Vector3(0,0,side*.50f)*K;
        }
        var c=d.gameObject.AddComponent<BraxChampionsD2DrzwiPociagu>();
        c.skrzydla=leaves;c.wysuw=outs;c.przesuw=slides;c.promien=1.7f*K;c.czas=1.2f;
        Doors++;
    }
    // vestibule / saloon partition x0..x1 (white), the opening w +-.50 with glass sliding doors on the saloon side
    // (toward: +1 = the saloon is east of it), an amber destination display over the doors on the saloon face
    static void Partition(float x0,float x1,float toward,string label)
    {
        float hl=THW-TWall;
        TB(tLining,x0,x1,TFloor,3.25f,-hl,-.50f);TB(tLining,x0,x1,TFloor,3.25f,.50f,hl);TB(tLining,x0,x1,2.772f,3.25f,-.50f,.50f);
        TCol("pociąg ścianka",x0,x1,TFloor,2.95f,-hl,-.50f);TCol("pociąg ścianka",x0,x1,TFloor,2.95f,.50f,hl);
        float face=toward>0?x1:x0,xd=face+toward*.05f;
        GlassSlidingDoor(xd,"drzwi przedziału "+label);
        // display over the doors (saloon side), clear of the door leaves (they run in the plane xd)
        float dx0=Mathf.Min(face,face+toward*.035f),dx1=Mathf.Max(face,face+toward*.035f);
        TB(black,dx0,dx1,2.80f,2.95f,-.45f,.45f);
        var lm=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(lm,tAmber,black,"KRAKÓW GŁÓWNY",TP(face+toward*.037f,2.835f,0f),toward>0?90f:-90f,.065f,.012f,.10f,.80f);
        BraxChampionsLetters3D.Build(lm,tg,"D2 pociąg wyświetlacz "+label);Signs++;
    }
    // the ceiling: a curved band w +-1.36 from 2.922 up to 3.22 (12 segments), LED lines at w +-.62
    static void Ceiling(float x0,float x1)
    {
        const int N=10;float hw=THW-TWall,rise=TCeilTop-TCeilEdge;
        for(int i=0;i<N;i++)
        {
            float t0=Mathf.PI*i/N,t1=Mathf.PI*(i+1)/N;
            Vector2 p0=new Vector2(hw*Mathf.Cos(t0),TCeilEdge+rise*Mathf.Sin(t0)),p1=new Vector2(hw*Mathf.Cos(t1),TCeilEdge+rise*Mathf.Sin(t1));
            var nrm=-new Vector3(0,(p0.y+p1.y)*.5f-TCeilEdge+.001f,(p0.x+p1.x)*.5f);
            Q(tCeiling,TP(x0,p0.y,p0.x),TP(x1,p0.y,p0.x),TP(x1,p1.y,p1.x),TP(x0,p1.y,p1.x),nrm);
        }
        foreach(float w in new[]{-.62f,.62f})
        {
            float hc=TCeilEdge+rise*Mathf.Sqrt(1f-(w/hw)*(w/hw));
            TB(ledWarm,x0+.05f,x1-.05f,hc-.025f,hc+.02f,w-.025f,w+.025f);
        }
    }
    // the saloon s0..s1: seats, the vis-a-vis bay with tables, luggage racks
    static void Saloon(int k,float s0,float s1,bool first)
    {
        float pitch=first?1.0f:.90f;int westRows=first?10:11,eastRows=first?9:10;
        // lateral layout: 2+2 (2nd) / 2+1 (1st, the pair on the platform side), aisle .70 / .98
        var cols2=first?new[]{(-1.33f,-.21f,2),(.77f,1.33f,1)}:new[]{(-1.33f,-.35f,2),(.35f,1.33f,2)};
        var mat=first?leatherNavy:tMoquette;
        void Row(float x,float f)
        {
            foreach(var (w0,w1,n) in cols2)
            {
                float sw=(w1-w0)/n;
                for(int i=0;i<n;i++){float wc=w0+sw*(i+.5f);SeatMesh(x,wc,f,sw,mat);TSeat(x+f*.02f,TFloor+.475f,wc,f,first?"fotel 1 klasa":"fotel");}
                // armrests: at both ends of the unit and between the seats (black)
                for(int i=0;i<=n;i++){float wa=w0+sw*i;if((i==0&&w0<-1.3f)||(i==n&&w1>1.3f))continue;TB(tBogie,Mathf.Min(x-f*.25f,x+f*.20f),Mathf.Max(x-f*.25f,x+f*.20f),TFloor+.60f,TFloor+.65f,wa-.025f,wa+.025f);}
                // the leg at the aisle end of the unit, the collider
                float wl=w0<0?w1-.12f:w0+.12f;
                TB(tBogie,x-.12f,x+.12f,TFloor,TFloor+.37f,wl-.03f,wl+.03f);
                TCol("fotele",Mathf.Min(x-f*.36f,x+f*.26f),Mathf.Max(x-f*.36f,x+f*.26f),TFloor,TFloor+1.25f,w0,w1);
            }
        }
        for(int i=0;i<westRows;i++)Row(s0+.50f+pitch*i,1f);
        for(int i=0;i<eastRows;i++)Row(s1-.50f-pitch*i,-1f);
        // the vis-a-vis bay: tables by the windows between the two facing rows
        float xw=s0+.50f+pitch*(westRows-1),xe=s1-.50f-pitch*(eastRows-1),xt=(xw+xe)*.5f;
        foreach(var (w0,w1,n) in cols2)
        {
            float a=Mathf.Max(w0,-1.34f),b=Mathf.Min(w1,1.34f),wall=w0<0?-1.36f:1.36f;
            TB(walnut,xt-.30f,xt+.30f,TFloor+.72f,TFloor+.75f,a,b);
            TB(black,xt-.31f,xt+.31f,TFloor+.70f,TFloor+.72f,a-.005f,b+.005f);
            float wb=wall<0?a+.05f:b-.05f;TB(frame,xt-.05f,xt+.05f,TFloor+.25f,TFloor+.70f,Mathf.Min(wall,wb),Mathf.Max(wall,wb));   // wall bracket
            TCol("stolik",xt-.30f,xt+.30f,TFloor,TFloor+.75f,a,b);
        }
        // luggage racks over the windows: glass shelves on black brackets with a black front rail
        foreach(float s in new[]{-1f,1f})
        {
            float wi=s*(THW-TWall-.005f),wf=s*1.02f;
            TB(glass,s0+.10f,s1-.10f,2.70f,2.712f,Mathf.Min(wi,wf),Mathf.Max(wi,wf));
            m.Tube(frame,TP(s0+.10f,2.735f,s*1.03f),TP(s1-.10f,2.735f,s*1.03f),.015f,8);
            for(float x=s0+.4f;x<s1-.2f;x+=1.8f)TB(frame,x-.008f,x+.008f,2.64f,2.75f,Mathf.Min(wi,s*1.03f),Mathf.Max(wi,s*1.03f));
        }
    }
    // one seat at (x, w) facing f (+-1 along x), width sw: cushion and back in moquette/leather, a white headrest cover
    static void SeatMesh(float x,float w,float f,float sw,Material mat)
    {
        m.RoundedBox(mat,TP(x+f*.02f,TFloor+.42f,w),new Vector3(.48f,.11f,sw-.03f),.035f,Quaternion.identity,1);
        var rot=Quaternion.Euler(0,0,f*12f);
        var bse=TP(x-f*.22f,TFloor+.47f,w);
        m.RoundedBox(mat,bse+rot*new Vector3(0,.40f,0),new Vector3(.09f,.80f,sw-.03f),.035f,rot,1);
        m.Box(tHeadrest,bse+rot*new Vector3(0,.745f,0),new Vector3(.12f,.13f,sw-.13f),rot);
    }

    // ---------------------------------------------------------------- locomotive (over buffers x0..x1, front at x1)
    // The cabs are smooth (owner 11.11 16:06-16:09: "ten przód zrób ładny, zaokrąglony, żeby nie wyglądał jak piksel ...
    // i prawdziwe światła ... ta szyba wygląda sztucznie"): each nose is one surface - at every height h its plan section is
    // a superellipse (rounded corners, a slightly convex front) whose depth and centre follow a side profile: an undercut
    // lower nose, the inclined panoramic windscreen wrapping round the corners, the rounded hood into the roof arc. The
    // body sides and the roof end exactly on it. Inside the cab: dark dashboard, seats, a dark lining - the glass reads as
    // real tinted glass (dark, reflective) with wipers on it. Head / marker / tail lamps in black housings set into the
    // nose, and real spot lights (Light) at the head lamps of the leading cab.
    static readonly float[] NoseH={1.05f,1.12f,1.22f,1.35f,1.50f,1.66f,1.82f,1.96f,2.06f,2.16f,2.24f,2.45f,2.68f,2.92f,3.12f,3.20f,3.29f,3.40f,3.52f,3.64f,3.74f,3.80f};
    const float NoseN=3.2f,LocoTop=3.80f,LocoRise=.45f;
    static float NoseLerp(float[] hs,float[] vs,float h)
    {
        if(h<=hs[0])return vs[0];
        for(int i=0;i+1<hs.Length;i++)if(h<=hs[i+1]){float t=(h-hs[i])/(hs[i+1]-hs[i]);t=t*t*(3f-2f*t)*.35f+t*.65f;return Mathf.Lerp(vs[i],vs[i+1],t);}
        return vs[vs.Length-1];
    }
    // the nose centre's distance behind the end face at height h
    static float NoseD(float h)=>NoseLerp(new[]{1.05f,1.25f,1.50f,1.75f,1.98f,2.16f,3.20f,3.40f,3.60f,3.80f,4.02f,4.25f},new[]{.16f,.05f,.005f,0f,.03f,.10f,.68f,.84f,1.02f,1.26f,1.62f,2.05f},h);
    // the depth of the rounded front in plan (from the side's end to the tip)
    static float NoseL(float h)=>NoseLerp(new[]{1.05f,2.16f,3.20f,3.80f,4.25f},new[]{.52f,.62f,.78f,.86f,.92f},h);
    static float NoseW(float h)=>h<=LocoTop?TLocoHW:TLocoHW*Mathf.Sqrt(Mathf.Max(0f,1f-((h-LocoTop)/LocoRise)*((h-LocoTop)/LocoRise)));
    static float NoseSide(float h)=>NoseD(h)+NoseL(h);
    // a point of the nose at height h, angle t (-pi/2 = the -w side, 0 = the tip, +pi/2 = the +w side); e: end face, dir +-1
    static Vector3 NoseP(float e,float dir,float h,float t)
    {
        float st=Mathf.Sin(t),ct=Mathf.Abs(Mathf.Cos(t));
        float w=NoseW(h)*Mathf.Sign(st)*Mathf.Pow(Mathf.Abs(st),2f/NoseN);
        float d=NoseD(h)+NoseL(h)*(1f-Mathf.Pow(ct,2f/NoseN));
        return TP(e-dir*d,h,w);
    }
    static float NoseT(float h,float w){float r=Mathf.Clamp01(Mathf.Abs(w)/Mathf.Max(NoseW(h),1e-3f));return Mathf.Sign(w)*(float)Math.Asin(Mathf.Pow(r,NoseN*.5f));}
    static Vector3 NoseNormal(float e,float dir,float h,float t)
    {
        var a=NoseP(e,dir,h,t);var dt=NoseP(e,dir,h,t+.01f)-NoseP(e,dir,h,t-.01f);var dh=NoseP(e,dir,h+.02f,t)-NoseP(e,dir,h-.02f,t);
        var n=Vector3.Cross(dt,dh).normalized;var inside=TP(e-dir*2.2f,2.6f,0f);
        return Vector3.Dot(n,a-inside)<0?-n:n;
    }
    static Material tWindscreen,tCabIn,tCabDesk,tCabCeil,tCabSeat,tBlind;
    // the windscreen is real glass (owner 11.11 16:14: "ta szyba ... nie może być czarna ... ma widać środek lokomotywy za
    // szybą, tam gdzie prowadzi maszynista"): clear with a slight green-grey tint, glossy; the cab behind it is lit and light
    static void CabMaterials()
    {
        if(tWindscreen)return;
        tWindscreen=S("train_windscreen_glass",new Color(.60f,.70f,.70f,.16f),.02f,.08f,null,true);
        // a real cab: anthracite plastic linings, a black matte desk, a mid-grey ceiling panel, black leather seats,
        // dark translucent sun blinds
        tCabIn=Inside(S("train_cab_lining_anthracite",new Color(.29f,.30f,.32f),.58f,0f),.95f);
        tCabDesk=Inside(S("train_cab_desk_black",new Color(.085f,.09f,.095f),.62f,.05f),.95f);
        tCabCeil=Inside(S("train_cab_ceiling_grey",new Color(.50f,.51f,.53f),.6f,0f),.95f);
        tCabSeat=Inside(BraxChampionsFoundation.LibraryLeather("d2_train_cab_seat_leather",new Color(.20f,.20f,.22f)),.9f);
        tBlind=S("train_cab_sun_blind",new Color(.10f,.10f,.11f,.72f),.7f,0f,null,true);
    }
    static void Nose(float e,float dir)
    {
        CabMaterials();
        // levels: the body part, then the roof arc's own heights (so the nose meets the roof's vertices exactly)
        var hs=new List<float>(NoseH);
        const int N=12;for(int i=1;i<=N/2;i++)hs.Add(LocoTop+LocoRise*Mathf.Sin(Mathf.PI*i/N));
        const int M=24;const float tgl=1.08f,tf=.14f;
        var inside=TP(e-dir*2.2f,2.6f,0f);
        for(int j=0;j+1<hs.Count;j++)
        {
            float h0=hs[j],h1=hs[j+1],hm=(h0+h1)*.5f;
            for(int i=0;i<M;i++)
            {
                float t0=-Mathf.PI*.5f+Mathf.PI*i/M,t1=t0+Mathf.PI/M,tm=Mathf.Abs((t0+t1)*.5f);
                Material mat;bool glassQ=false;
                if(hm<2.06f)mat=tPaintRed;
                else if(hm<2.24f)mat=tm<tgl+tf?frame:tPaintNavy;
                else if(hm<3.20f){if(tm<tgl){mat=tWindscreen;glassQ=true;}else mat=tm<tgl+tf?frame:tPaintNavy;}
                else if(hm<3.29f)mat=tm<tgl+tf?frame:tPaintNavy;
                else if(hm<LocoTop)mat=tPaintWhite;
                else{int ai=j-(NoseH.Length-1);mat=ai<2?tPaintNavy:tRoof;}
                var a=NoseP(e,dir,h0,t0);var b=NoseP(e,dir,h0,t1);var c=NoseP(e,dir,h1,t1);var d=NoseP(e,dir,h1,t0);
                var n=(a+b+c+d)*.25f-inside;
                Q(mat,a,b,c,d,n);
                Q(glassQ?tWindscreen:tCabIn,a,b,c,d,-n);                             // inside: the cab's light lining
            }
        }
        // the bottom of the nose (h 1.05): a fan from inside to the outline
        {
            var cen=TP(e-dir*(NoseD(1.05f)+NoseL(1.05f)*.5f),1.05f,0f);
            for(int i=0;i<M;i++){float t0=-Mathf.PI*.5f+Mathf.PI*i/M,t1=t0+Mathf.PI/M;T(tBogie,cen,NoseP(e,dir,1.05f,t0),NoseP(e,dir,1.05f,t1),Vector3.down);}
            T(tBogie,cen,NoseP(e,dir,1.05f,Mathf.PI*.5f),NoseP(e,dir,1.05f,-Mathf.PI*.5f),Vector3.down);
        }
        // wipers parked on the glass (black, 2 cm over it)
        foreach(float w in new[]{-.55f,.55f})
        {
            float t=NoseT(2.30f,w);var p=NoseP(e,dir,2.30f,t);var n=NoseNormal(e,dir,2.30f,t);
            var q=NoseP(e,dir,2.95f,NoseT(2.95f,w-.10f));var nq=NoseNormal(e,dir,2.95f,NoseT(2.95f,w-.10f));
            m.Tube(frame,p+n*.02f,q+nq*.02f,.011f,6);
            m.Cylinder(frame,p,.035f,.03f,10,Quaternion.FromToRotation(Vector3.up,n),true,false);
        }
        // lamps: black housings set into the nose; head lamps (white, LED rings), the red tail lamp beside each, the top lamp
        bool lead=dir>0;
        foreach(float s in new[]{-1f,1f})
        {
            Lamp(e,dir,1.56f,s*.86f,.40f,.20f,lead?tHeadLamp:tBogie,.075f,lead);
            Lamp(e,dir,1.56f,s*1.12f,.16f,.16f,lead?tBogie:tTailLamp,.05f,false);
        }
        Lamp(e,dir,3.50f,0f,.36f,.16f,lead?tHeadLamp:tBogie,.07f,lead);
        // the number on the nose
        var nm=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(nm,whiteLetters,tPaintRed,"160-01",TP(e-dir*NoseD(1.86f)+dir*.004f,1.86f,0f),dir>0?90f:-90f,.11f,.012f,.10f,1.0f);
        BraxChampionsLetters3D.Build(nm,tg,"D2 lokomotywa numer czoło");Signs++;
    }
    // a lamp at (h, w) on the nose: a rounded black housing (size sx x sy, 6 cm deep, sunk 2 cm), a lit lens with an LED
    // ring; light: a real spot light along the nose normal (the leading cab's head lamps)
    static void Lamp(float e,float dir,float h,float w,float sx,float sy,Material lens,float r,bool light)
    {
        float t=NoseT(h,w);var p=NoseP(e,dir,h,t);var n=NoseNormal(e,dir,h,t);
        var rot=Quaternion.LookRotation(n,Vector3.up);
        m.RoundedBox(frame,p+n*.01f,new Vector3(sx,sy,.06f),.025f,rot,2);
        var lr=Quaternion.FromToRotation(Vector3.up,n);
        m.Cylinder(lens,p+n*.04f,r,.012f,24,lr,true,false);
        if(lens==tHeadLamp)m.Lathe(tHeadLamp,p+n*.041f,new[]{new Vector2(r+.012f,0f),new Vector2(r+.028f,0f),new Vector2(r+.028f,.006f),new Vector2(r+.012f,.006f)},32,lr,false,false);
        if(!light)return;
        var go=new GameObject("D2 lokomotywa reflektor");go.transform.SetParent(tg,false);
        go.transform.localPosition=(p+n*.06f)*K;go.transform.localRotation=Quaternion.LookRotation((n+Vector3.down*.12f).normalized,Vector3.up);
        var L=go.AddComponent<Light>();L.type=LightType.Spot;L.range=55f*K;L.spotAngle=34f;L.intensity=3.2f;L.color=new Color(1f,.97f,.90f);L.shadows=LightShadows.None;
    }

    static void Locomotive(float x0,float x1)
    {
        float xr=x0+.55f,xf=x1-.55f,hw=TLocoHW;
        float XF(float h)=>xf-NoseSide(h);float XR(float h)=>xr+NoseSide(h);
        // body sides: strips between the noses at the nose levels; double-sided inside the cabs (dark), so nothing shows
        // through from the cab windows
        CabMaterials();
        const float WinH0=2.45f,WinH1=3.12f;
        float fw0=xf-2.32f,fw1=xf-1.62f,rw0=xr+1.62f,rw1=xr+2.32f;                // the cab side windows (x)
        void Strip(Material mat,float ax0,float ax1,float bx0,float bx1,float h0,float h1,float w,float face)
        {
            if(Mathf.Abs(ax1-ax0)<1e-4f&&Mathf.Abs(bx1-bx0)<1e-4f)return;
            Q(mat,TP(ax0,h0,w),TP(ax1,h0,w),TP(bx1,h1,w),TP(bx0,h1,w),new Vector3(0,0,face));
        }
        for(int j=0;j+1<NoseH.Length;j++)
        {
            float h0=NoseH[j],h1=NoseH[j+1];bool win=h0>WinH0-.001f&&h1<WinH1+.001f;
            foreach(float s in new[]{-1f,1f})
            {
                float w=s*hw;
                if(!win)Strip(tPaintNavy,XR(h0),XF(h0),XR(h1),XF(h1),h0,h1,w,s);
                else
                {
                    Strip(tPaintNavy,XR(h0),rw0,XR(h1),rw0,h0,h1,w,s);Strip(tPaintNavy,rw1,fw0,rw1,fw0,h0,h1,w,s);Strip(tPaintNavy,fw1,XF(h0),fw1,XF(h1),h0,h1,w,s);
                }
                foreach(float dir in new[]{1f,-1f})
                {
                    float e=dir>0?xf:xr,c0=dir>0?XF(h0):XR(h0),c1=dir>0?XF(h1):XR(h1),k=e-dir*2.45f;
                    if(!win)Strip(tCabIn,c0,k,c1,k,h0,h1,w,-s);
                    else
                    {
                        float wn=dir>0?fw1:rw0,wf=dir>0?fw0:rw1;                      // window edge nearer the nose / farther
                        Strip(tCabIn,c0,wn,c1,wn,h0,h1,w,-s);Strip(tCabIn,wf,k,wf,k,h0,h1,w,-s);
                    }
                }
            }
        }
        // the cab side windows: openings with black reveals, clear glass, a black frame ring 30 mm proud
        foreach(float s in new[]{-1f,1f})foreach(var (a,b) in new[]{(fw0,fw1),(rw0,rw1)})
        {
            Reveal(a,b,WinH0,WinH1,s*hw,s*(hw-.06f));
            var g0=TP(a,WinH0,s*(hw-.02f));var g1=TP(b,WinH0,s*(hw-.02f));var g2=TP(b,WinH1,s*(hw-.02f));var g3=TP(a,WinH1,s*(hw-.02f));
            Q(tWindscreen,g0,g1,g2,g3,Vector3.forward);Q(tWindscreen,g0,g1,g2,g3,Vector3.back);
            float v0=Mathf.Min(s*hw,s*(hw+.03f)),v1=Mathf.Max(s*hw,s*(hw+.03f));
            TB(frame,a-.05f,b+.05f,WinH1,WinH1+.05f,v0,v1);TB(frame,a-.05f,b+.05f,WinH0-.05f,WinH0,v0,v1);
            TB(frame,a-.05f,a,WinH0,WinH1,v0,v1);TB(frame,b,b+.05f,WinH0,WinH1,v0,v1);
        }
        // the roof arc between the noses (each vertex ends on its nose level), dark inside over the cabs
        {
            const int N=12;
            for(int i=0;i<N;i++)
            {
                float t0=Mathf.PI*i/N,t1=Mathf.PI*(i+1)/N;
                Vector2 p0=new Vector2(hw*Mathf.Cos(t0),LocoTop+LocoRise*Mathf.Sin(t0)),p1=new Vector2(hw*Mathf.Cos(t1),LocoTop+LocoRise*Mathf.Sin(t1));
                var nrm=new Vector3(0,(p0.y+p1.y)*.5f-LocoTop+.001f,(p0.x+p1.x)*.5f);
                Q(i<2||i>=N-2?tPaintNavy:tRoof,TP(XR(p0.y),p0.y,p0.x),TP(XF(p0.y),p0.y,p0.x),TP(XF(p1.y),p1.y,p1.x),TP(XR(p1.y),p1.y,p1.x),nrm);
                foreach(float dir in new[]{1f,-1f})
                {
                    float e=dir>0?xf:xr,k=e-dir*2.45f;
                    // only over the cab (where the roof vertex's nose edge is still in front of the back wall)
                    float a0=dir>0?Mathf.Max(XF(p0.y),k):Mathf.Min(XR(p0.y),k),a1=dir>0?Mathf.Max(XF(p1.y),k):Mathf.Min(XR(p1.y),k);
                    if(Mathf.Abs(a0-k)<.01f&&Mathf.Abs(a1-k)<.01f)continue;
                    if(Mathf.Abs(a0-k)<.01f)T(tCabIn,TP(k,p0.y,p0.x),TP(k,p1.y,p1.x),TP(a1,p1.y,p1.x),-nrm);
                    else if(Mathf.Abs(a1-k)<.01f)T(tCabIn,TP(a0,p0.y,p0.x),TP(k,p0.y,p0.x),TP(k,p1.y,p1.x),-nrm);
                    else Q(tCabIn,TP(a0,p0.y,p0.x),TP(k,p0.y,p0.x),TP(k,p1.y,p1.x),TP(a1,p1.y,p1.x),-nrm);
                }
            }
        }
        // bottom of the body between the noses
        Q(tBogie,TP(XR(1.05f),1.05f,-hw),TP(XF(1.05f),1.05f,-hw),TP(XF(1.05f),1.05f,hw),TP(XR(1.05f),1.05f,hw),Vector3.down);
        foreach(float dir in new[]{1f,-1f}){Nose(dir>0?xf:xr,dir);Cab(dir>0?xf:xr,dir);}
        // livery overlays (15 mm proud): white band, red solebar band (both end before the rounded noses)
        foreach(float s in new[]{-1f,1f})
        {
            float w0=Mathf.Min(s*hw,s*(hw+.015f)),w1=Mathf.Max(s*hw,s*(hw+.015f));
            TB(tPaintWhite,xr+2.42f,xf-2.42f,2.30f,2.55f,w0,w1);
            TB(tPaintRed,XR(1.25f)+.02f,XF(1.25f)-.02f,1.05f,1.25f,w0,w1);
            // louvre panels (black frames, slats 25 mm proud)
            foreach(float lx in new[]{xr+3.6f,xr+6.0f,xf-7.8f,xf-5.4f})
            {
                float lw0=Mathf.Min(s*hw,s*(hw+.025f)),lw1=Mathf.Max(s*hw,s*(hw+.025f));
                TB(frame,lx,lx+1.8f,2.66f,2.70f,w0,w1);TB(frame,lx,lx+1.8f,3.51f,3.55f,w0,w1);TB(frame,lx,lx+.04f,2.70f,3.51f,w0,w1);TB(frame,lx+1.76f,lx+1.8f,2.70f,3.51f,w0,w1);
                for(float y=2.74f;y<3.48f;y+=.07f)TB(frame,lx+.06f,lx+1.74f,y,y+.035f,lw0,lw1);
            }
            foreach(float dir in new[]{1f,-1f})
            {
                // cab doors behind the cabs: outline 30 mm proud (over the band), window, handrails, steps
                float dx0=dir>0?xf-3.10f:xr+2.45f,dx1=dx0+.65f;
                float v0=Mathf.Min(s*hw,s*(hw+.03f)),v1=Mathf.Max(s*hw,s*(hw+.03f));
                TB(frame,dx0,dx1,1.25f,1.28f,v0,v1);TB(frame,dx0,dx1,3.22f,3.25f,v0,v1);TB(frame,dx0,dx0+.03f,1.28f,3.22f,v0,v1);TB(frame,dx1-.03f,dx1,1.28f,3.22f,v0,v1);
                TB(frame,dx0+.10f,dx1-.10f,2.55f,2.58f,v0,v1);TB(frame,dx0+.10f,dx1-.10f,3.07f,3.10f,v0,v1);
                var gw=s*(hw+.027f);var g0=TP(dx0+.10f,2.58f,gw);var g1=TP(dx1-.10f,2.58f,gw);var g2=TP(dx1-.10f,3.07f,gw);var g3=TP(dx0+.10f,3.07f,gw);
                Q(glassDark,g0,g1,g2,g3,new Vector3(0,0,s));                          // the door window (machine room behind)
                foreach(float hx in new[]{dx0-.10f,dx1+.10f})
                {
                    m.Tube(frame,TP(hx,1.30f,s*(hw+.10f)),TP(hx,2.90f,s*(hw+.10f)),.018f,8);
                    foreach(float hy in new[]{1.35f,2.85f})m.Tube(frame,TP(hx,hy,s*(hw+.10f)),TP(hx,hy,s*hw),.012f,6);
                }
                foreach(float sy in new[]{.55f,.85f})TB(frame,dx0,dx1,sy,sy+.05f,Mathf.Min(s*(hw-.05f),s*(hw+.22f)),Mathf.Max(s*(hw-.05f),s*(hw+.22f)));
                TB(frame,dx0-.02f,dx0+.02f,.55f,1.05f,Mathf.Min(s*(hw+.18f),s*(hw+.22f)),Mathf.Max(s*(hw+.18f),s*(hw+.22f)));
                TB(frame,dx1-.02f,dx1+.02f,.55f,1.05f,Mathf.Min(s*(hw+.18f),s*(hw+.22f)),Mathf.Max(s*(hw+.18f),s*(hw+.22f)));
            }
            // name and number in 3D
            var lm=new BraxChampionsMeshBatch();float yaw=s<0?180f:0f;
            BraxChampionsLetters3D.Text(lm,whiteLetters,tPaintNavy,"BRAXHORN",TP((xr+xf)*.5f,1.42f,s*(hw+.002f)),yaw,.42f,.02f,.12f,7.0f);
            foreach(float dir in new[]{1f,-1f})BraxChampionsLetters3D.Text(lm,whiteLetters,tPaintNavy,"BX 160-01",TP(dir>0?xf-1.75f:xr+1.75f,1.66f,s*(hw+.002f)),yaw,.12f,.012f,.10f,.9f);
            BraxChampionsLetters3D.Build(lm,tg,"D2 lokomotywa napisy "+(s<0?"peron":"tor"));Signs++;
        }
        // underframe, equipment, bogies, buffers, couplings, aprons and ploughs
        TB(tBogie,xr+.70f,xf-.70f,.95f,1.05f,-1.42f,1.42f);
        float xc=(xr+xf)*.5f;
        TB(tBogie,xc-2.2f,xc+2.2f,.45f,.95f,-1.15f,1.15f);                       // transformer tank
        foreach(float bc in new[]{xc-4.95f,xc+4.95f})
        {
            Bogie(bc,1.35f,.625f,.86f);
            TB(tBogie,bc-.55f,bc+.55f,.35f,.80f,-.50f,.50f);                       // traction motor
        }
        foreach(float dir in new[]{1f,-1f})
        {
            float e=dir>0?xf:xr;
            m.RoundedBox(tPaintRed,TP(e-dir*.07f,1.05f,0f),new Vector3(.26f,.34f,2.80f),.06f,Quaternion.identity,3);   // buffer beam (rounded)
            m.RoundedBox(tBogie,TP(e-dir*.22f,.72f,0f),new Vector3(.42f,.34f,2.50f),.07f,Quaternion.identity,3);       // the apron under the nose
            m.RoundedBox(tBogie,TP(e-dir*.05f,.37f,0f),new Vector3(.36f,.36f,2.60f),.05f,Quaternion.Euler(0,0,dir*8f),2);   // snow plough
            foreach(float w in new[]{-.875f,.875f})
            {
                var rot=Quaternion.Euler(0,0,-dir*90f);
                m.Cylinder(tBogie,TP(e+dir*.06f,1.06f,w),.12f,.42f,16,rot,true,false);
                m.Cylinder(tWheel,TP(e+dir*.48f,1.06f,w),.21f,.068f,24,rot,true,true);
            }
            TB(tBogie,Mathf.Min(e+dir*.06f,e+dir*.32f),Mathf.Max(e+dir*.06f,e+dir*.32f),.98f,1.14f,-.07f,.07f);   // hook
        }
        // roof: pantographs (front raised to the wire 5.3 m, rear lowered), the roof line on insulators, the breaker
        Pantograph(xf-3.6f,true);Pantograph(xr+3.6f,false);
        m.Tube(tPanto,TP(xr+3.6f,4.52f,0f),TP(xf-3.6f,4.52f,0f),.03f,8);
        for(float x=xr+5.5f;x<xf-5f;x+=3.2f)Insulator(x,0f,4.20f,.30f);
        Insulator(xc,.45f,4.20f,.25f);TB(tBogie,xc-.35f,xc+.35f,4.45f,4.70f,.20f,.70f);   // main breaker
        // the loco's collider (solid)
        TCol("lokomotywa",xr-.55f,xf+.55f,.30f,4.30f,-hw,hw);
    }
    static void Insulator(float x,float w,float h,float len)
    {
        var prof=new List<Vector2>{new Vector2(.05f,0f)};
        int n=Mathf.Max(3,Mathf.RoundToInt(len/.04f));
        for(int i=1;i<n;i++)prof.Add(new Vector2(i%2==1?.085f:.05f,len*i/n));
        prof.Add(new Vector2(.05f,len));
        m.Lathe(tInsulator,TP(x,h,w),prof.ToArray(),12,Quaternion.identity,false,true);
    }
    // a single-arm pantograph at x: base on four insulators, lower arm, upper arm, the head with carbon strips and horns
    static void Pantograph(float x,bool raised)
    {
        foreach(float dx in new[]{-.55f,.55f})foreach(float w in new[]{-.45f,.45f})Insulator(x+dx,w,4.20f,.25f);
        TB(tBogie,x-.62f,x+.62f,4.45f,4.52f,-.55f,.55f);
        Vector3 hinge=TP(x-.50f,4.56f,0f),knee=raised?TP(x+.85f,4.95f,0f):TP(x+.95f,4.62f,0f),head=raised?TP(x-.05f,5.20f,0f):TP(x-.20f,4.66f,0f);
        foreach(float w in new[]{-.30f,.30f})m.Tube(tPanto,hinge+new Vector3(0,0,w),knee+new Vector3(0,0,w*.15f),.035f,8);
        foreach(float w in new[]{-.35f,.35f})m.Tube(tPanto,knee,head+new Vector3(0,0,w),.022f,8);
        m.Tube(tPanto,TP(x-.25f,4.52f,0f),Vector3.Lerp(hinge,knee,.45f),.02f,6);   // the lifting spring / damper
        float hy=head.y-RailTop;
        TB(tPanto,x-.08f-(raised?.05f:.20f),x+.08f-(raised?.05f:.20f),hy,hy+.05f,-.80f,.80f);
        TB(tCarbon,x-.035f-(raised?.05f:.20f),x+.035f-(raised?.05f:.20f),hy+.05f,hy+.09f,-.70f,.70f);   // top 5.29 when raised
        foreach(float s in new[]{-1f,1f})m.Tube(tPanto,TP(x-(raised?.05f:.20f),hy+.03f,s*.80f),TP(x-(raised?.05f:.20f),hy-.10f,s*1.00f),.015f,6);
    }
    // a cab at the end e (dir +1 front / -1 rear), as real as the windscreen shows it (owner 11.11 16:14-16:18): anthracite
    // linings, a mid-grey ceiling panel with its light, a dark rubber floor; the black desk across the nose with its wings
    // to the side windows, four lit screens and a round speed display, button rows, the power handle and the red brake
    // handle at the driver's place, a radio handset; two black leather seats on columns (the driver on the left in the
    // direction of travel), the sun blind half down in front of the driver, the door to the machine room, an extinguisher
    static void Cab(float e,float dir)
    {
        CabMaterials();
        float x(float d)=>e-dir*d;
        float lo(float a,float b)=>Mathf.Min(x(a),x(b));float hi(float a,float b)=>Mathf.Max(x(a),x(b));
        TB(tCabIn,lo(2.45f,2.40f),hi(2.45f,2.40f),1.25f,3.75f,-1.45f,1.45f);     // back wall
        TB(frame,lo(2.40f,2.385f),hi(2.40f,2.385f),1.30f,3.25f,-.40f,.40f);     // the door to the machine room
        TB(tCabDesk,lo(2.385f,2.37f),hi(2.385f,2.37f),1.36f,3.19f,-.34f,.34f);
        TB(tVestFloor,lo(2.40f,.55f),hi(2.40f,.55f),1.20f,1.30f,-1.25f,1.25f);   // floor
        TB(tCabCeil,lo(2.40f,1.10f),hi(2.40f,1.10f),3.30f,3.36f,-1.20f,1.20f);   // ceiling panel
        TB(ledWarm,lo(1.95f,1.55f),hi(1.95f,1.55f),3.285f,3.30f,-.30f,.30f);     // its light
        m.Cylinder(tPaintRed,TP(x(2.25f),1.30f,-1.28f),.08f,.48f,14,Quaternion.identity,true);   // extinguisher
        // desk: body and wings, the sloped top
        TB(tCabDesk,lo(1.0f,.34f),hi(1.0f,.34f),1.30f,2.0f,-1.15f,1.15f);
        foreach(float s in new[]{-1f,1f})TB(tCabDesk,lo(1.40f,.45f),hi(1.40f,.45f),1.30f,1.95f,Mathf.Min(s*1.15f,s*1.36f),Mathf.Max(s*1.15f,s*1.36f));
        var rot=Quaternion.Euler(0,0,dir*18f);var dc=TP(x(.66f),2.08f,0f);
        m.Box(tCabDesk,dc,new Vector3(.56f,.05f,2.30f),rot);
        Vector3 OnDesk(float d,float w,float up)=>dc+rot*new Vector3(dir*(.66f-d),up,w);   // a point on the sloped top (d from the end)
        foreach(float w in new[]{1.0f,.52f,-.05f,-.55f})m.Box(screenLit(),OnDesk(.70f,w,.033f),new Vector3(.22f,.012f,.38f),rot);
        m.Cylinder(screenLit(),OnDesk(.50f,.76f,.025f),.07f,.012f,24,rot,true,false);                        // speed display
        m.Lathe(frame,OnDesk(.50f,.76f,.025f),new[]{new Vector2(.073f,0f),new Vector2(.085f,0f),new Vector2(.085f,.02f),new Vector2(.073f,.02f)},24,rot,false,false);
        for(int r=0;r<2;r++)for(int i=0;i<12;i++)
        {
            float w=-1.05f+i*.085f;var col=i%4==0?tGreen:i%4==1?tAmber:i%4==2?tHeadLamp:tTailLamp;
            m.Box(col,OnDesk(.40f+r*.045f,w,.033f),new Vector3(.032f,.012f,.032f),rot);
        }
        var hr=rot*Quaternion.Euler(0,0,dir*15f);
        m.Cylinder(frame,OnDesk(.86f,.98f,.025f),.018f,.15f,10,hr,true);                                       // power handle
        m.RoundedBox(frame,OnDesk(.86f,.98f,.025f)+hr*new Vector3(0,.16f,0),new Vector3(.06f,.045f,.09f),.02f,hr,2);
        m.Cylinder(tPaintRed,OnDesk(.86f,.64f,.025f),.016f,.12f,10,hr,true);                                   // brake handle
        m.RoundedBox(tCabDesk,TP(x(.55f),1.97f,-.80f),new Vector3(.08f,.20f,.06f),.02f,Quaternion.identity,2);   // radio handset
        // the sun blind: a housing over the windscreen, the blind half down in front of the driver (dark, translucent)
        TB(frame,lo(1.00f,.94f),hi(1.00f,.94f),3.22f,3.30f,-1.0f,1.0f);
        {
            float w0=.08f,w1=.98f,ha=3.16f,hb=2.86f;
            Vector3 BlindP(float h,float w){float t=NoseT(h,w);return NoseP(e,dir,h,t)-NoseNormal(e,dir,h,t)*.07f;}
            var b0=BlindP(ha,w0);var b1=BlindP(ha,w1);var b2=BlindP(hb,w1);var b3=BlindP(hb,w0);
            Q(tBlind,b0,b1,b2,b3,Vector3.right);Q(tBlind,b0,b1,b2,b3,Vector3.left);
            m.Tube(frame,b3,b2,.008f,6);
        }
        // seats: black leather on a black column with a base plate, armrests, headrests
        foreach(float w in new[]{.55f,-.55f})
        {
            float sx=x(1.45f);
            m.RoundedBox(tCabSeat,TP(sx,1.78f,w),new Vector3(.50f,.10f,.50f),.04f,Quaternion.identity,2);
            m.RoundedBox(tCabSeat,TP(sx+dir*-.27f,2.17f,w),new Vector3(.11f,.72f,.48f),.04f,Quaternion.Euler(0,0,dir*10f),2);
            m.RoundedBox(tCabSeat,TP(sx+dir*-.33f,2.62f,w),new Vector3(.10f,.20f,.32f),.04f,Quaternion.Euler(0,0,dir*10f),2);
            foreach(float a in new[]{-.27f,.27f})TB(frame,sx-.15f,sx+.12f,1.98f,2.02f,w+a-.02f,w+a+.02f);
            TB(frame,sx-.05f,sx+.05f,1.30f,1.73f,w-.05f,w+.05f);
            TB(frame,sx-.22f,sx+.22f,1.30f,1.33f,w-.22f,w+.22f);
        }
    }
}
