using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// D2 bus terminal (board: "TERMINAL AUTOBUSOWY - 8 STANOWISK, zadaszony, oddzielny terminal"), the forecourt furniture
// (fountain, beds, lamps, benches, bollards, the wayfinding pylon "DWORZEC: Kolej / Autobusy / Parking / Taxi / Kiss&Ride")
// and the train (the region's own two-car unit model, coupled twice: a 4-car train, BraxChampionsD2Pociag runs it).
public static partial class BraxChampionsD2
{
    // ================================================================ bus terminal
    static void BusTerminal()
    {
        // island canopy: navy columns on the island's back line, roof over the island and the buses' noses, larch soffit
        float cz=12.4f,r0=4.4f,r1=IslandZ1+.6f,soffit=WalkY+4.3f,top=soffit+.5f,x0=BusX0+.5f,x1=IslandX1;
        B(roofGraphite,x0+.15f,x1,top-.08f,top,r0+.15f,r1-.15f);
        B(navy,x0,x1,soffit,top+.06f,r0,r0+.15f);B(navy,x0,x1,soffit,top+.06f,r1-.15f,r1);B(navy,x0,x0+.15f,soffit,top+.06f,r0+.15f,r1-.15f);
        for(float z=r0+.3f;z<r1-.25f;z+=.22f)B(larchSoffit,x0+.15f,x1,soffit+.06f,soffit+.12f,z,z+.14f);
        B(steelNavy,x0+.15f,x1,soffit+.2f,top-.08f,r0+.15f,r1-.15f);
        B(led,x0+.3f,x1-.3f,soffit+.035f,soffit+.06f,r0+.6f,r0+.7f);B(led,x0+.3f,x1-.3f,soffit+.035f,soffit+.06f,cz-.05f,cz+.05f);
        for(float x=x0+2f;x<x1-1f;x+=11f)
        {
            m.Cylinder(steelNavy,new Vector3(x,WalkY,cz),.16f,soffit-WalkY,20,Quaternion.identity,false);
            B(steelNavy,x-.1f,x+.1f,soffit-.3f,soffit+.02f,r0+.2f,r1-.2f);
            Col(x-.2f,x+.2f,WalkY,soffit,cz-.2f,cz+.2f,"słup wiaty");
        }
        Col(x0,x1,soffit,top,r0,r1,"wiata autobusowa");
        // stand numbers: navy boxes with white 3D digits on the canopy's front edge, one per stand (board picture)
        var lm=new BraxChampionsMeshBatch();
        for(int i=0;i<Stands;i++)
        {
            float x=StandX0+(i+.5f)*StandPitch;
            RB(signBlue,x-.45f,x+.45f,soffit-.95f,soffit-.05f,r0+.02f,r0+.4f,.05f);
            BraxChampionsLetters3D.Text(lm,whiteLetters,navy,(i+1).ToString(),new Vector3(x,soffit-.82f,r0+.02f),180f,.62f,.02f);
            Signs++;
        }
        BraxChampionsLetters3D.Build(lm,root,"D2 numery stanowisk");
        // benches and departure screens on the island
        for(int i=0;i<4;i++)OutdoorBench(70f+i*11f,13.0f,new Vector3(0,0,-1),WalkY);
        // the terminal building x 112..138, z 8..34: glass to the island and the street, larch slat panels, navy fascia,
        // flat roof with a 1.2 m overhang, "TERMINAL AUTOBUSOWY" in 3D on the west and south fascias
        float tx0=TermX0,tx1=TermX1,tz0=TermZ0,tz1=TermZ1,h=5.2f;
        Flat(hallFloor,tx0,tx1,tz0,tz1,FFL);Col(tx0,tx1,FFL-.5f,FFL,tz0,tz1,"podłoga terminalu");
        // west (to the island) and south (to the apron) glazed; east and north travertine with slat panels
        Glazing(tx0+.6f,tx1-.6f,FFL,3.8f,tz0,-1f,true);
        GlazingZ(tx0,tz0+.6f,tz1-.6f,FFL,3.8f,-1f);
        foreach(float x in new[]{tx0,tx1})foreach(float z in new[]{tz0,tz1})B(travertine,x-.3f,x+.3f,0f,h,z-.3f,z+.3f,true);
        B(travertine,tx1-.25f,tx1+.05f,0f,h,tz0+.3f,tz1-.3f,true);B(travertine,tx0+.3f,tx1-.3f,0f,h,tz1-.05f,tz1+.25f,true);
        SlatPanel(tx1-12f,tx1-1f,1.2f,3.6f,tz1+.37f);
        B(navy,tx0-1.2f,tx1+1.2f,3.8f,h,tz0-1.2f,tz0-1.0f,true);B(navy,tx0-1.2f,tx0-1.0f,3.8f,h,tz0-1.2f,tz1+1.2f,true);
        B(navy,tx0-1.2f,tx1+1.2f,3.8f,h,tz1+1.0f,tz1+1.2f);B(navy,tx1+1.0f,tx1+1.2f,3.8f,h,tz0-1.2f,tz1+1.2f);
        B(roofGraphite,tx0-1.0f,tx1+1.0f,h-.12f,h-.02f,tz0-1.0f,tz1+1.0f);
        Flip(larchSoffit,tx0-1.0f,tx1+1.0f,tz0-1.0f,tz1+1.0f,3.82f);
        B(concrete,tx0,tx1,3.8f,h-.12f,tz0,tz1);
        Door("D2_term_w","D2 terminal autobusowy wejście",tx0,FFL,(tz0+tz1)*.5f,-90f,2.4f,2.6f,true);
        Door("D2_term_s","D2 terminal autobusowy wejście od placu",(tx0+tx1)*.5f,FFL,tz0,0f,2.4f,2.6f,true);
        var tl=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(tl,whiteLetters,navy,"TERMINAL AUTOBUSOWY",new Vector3((tx0+tx1)*.5f,4.15f,tz0-1.2f),180f,.55f,.08f,.10f,tx1-tx0);
        BraxChampionsLetters3D.Text(tl,whiteLetters,navy,"TERMINAL AUTOBUSOWY",new Vector3(tx0-1.2f,4.15f,(tz0+tz1)*.5f),-90f,.55f,.08f,.10f,tz1-tz0);
        BraxChampionsLetters3D.Build(tl,root,"D2 napis terminal");Signs+=2;
        // inside: waiting benches, the ticket counter on the east wall
        for(int r=0;r<3;r++)WaitingBench(121f,14f+r*3.0f,1f);
        B(counterTop,tx1-3.0f,tx1-.6f,1.05f,1.11f,20f,30f,true);B(wallPanel,tx1-2.9f,tx1-.7f,FFL,1.05f,20.1f,29.9f);
        Buses();
    }
    // the owner's bus (miasto_pojazdy "autobus", MAN Lion's City 12 m, CC BY KarpWorkz) at five stands, nose to the island
    static void Buses()
    {
        var src=BraxMiastoAssety.Jest("miasto_pojazdy")?BraxMiastoAssety.Model("miasto_pojazdy","autobus"):null;
        if(!src){Debug.Log("D2 autobusy: pack miasto_pojazdy MISSING - stands without buses");return;}
        var g=Group(root,"D2 autobusy (model miasto_pojazdy/autobus, CC BY KarpWorkz)");
        foreach(int i in new[]{0,1,3,5,6})
        {
            float x=StandX0+(i+.5f)*StandPitch,zc=BusZ1-.4f-6.0f;
            var o=BraxMiastoAssety.Postaw(g,src,"D2 autobus stanowisko "+(i+1),root.TransformPoint(new Vector3(x,LaneY,zc)*K),0f,1f,false,true);
            if(o)BraxMiastoAssety.Kolizja(g,o,"autobus kolizja",.1f);
        }
    }
    // outdoor bench at level y (StationBench, 4 seats, oak slats)
    static void OutdoorBench(float cx,float z,Vector3 face,float y)=>StationBench(new Vector3(cx,y,z),face,4,"ławka "+cx.ToString("0")+"/"+z.ToString("0"));

    // ================================================================ forecourt furniture
    static void SiteFurniture()
    {
        Fountain(new Vector3(0,WalkY,-31f));
        // the plaza: granite ring round the basin (12 mm inlay), four raised lawn beds with trees and flowers
        Ring(granite,new Vector3(0,WalkY+.012f,-31f),7.6f,10.5f,WalkY+.012f,WalkY+.012f,64);
        // eight dark granite spokes from the ring toward the plaza's edge (12 mm inlays), round granite planters with the
        // board's flowers between them on R 12.6, two benches facing the fountain
        for(int i=0;i<8;i++)
        {
            float a=2*Mathf.PI*(i+.5f)/8f;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var nn=new Vector2(-d.y,d.x);
            float r1=d.y<0?Mathf.Min(13.5f,(-31f-(SiteZ0+3.5f))/-d.y):Mathf.Min(13.5f,((FrontZ-LaneHalf-.6f)+31f)/d.y);
            Vector3 V(float r,float o)=>new Vector3(d.x*r+nn.x*o,WalkY+.012f,-31f+d.y*r+nn.y*o);
            Q(graniteDark,V(10.5f,-.2f),V(r1,-.2f),V(r1,.2f),V(10.5f,.2f),Vector3.up);
        }
        var fl=Group(root,"D2 kwiaty przy fontannie (Poly Haven)");
        for(int i=0;i<8;i++)
        {
            float a=2*Mathf.PI*i/8f;var p=new Vector3(Mathf.Cos(a)*12.6f,WalkY,-31f+Mathf.Sin(a)*12.6f);
            if(p.z>FrontZ-LaneHalf-1.6f||p.z<SiteZ0+4f)continue;
            m.Lathe(granite,p,new[]{new Vector2(.95f,0f),new Vector2(1.02f,.48f),new Vector2(.92f,.48f),new Vector2(.86f,.06f)},40,Quaternion.identity,false,false);
            m.Cylinder(lawn,p+Vector3.up*.44f,.92f,.01f,32,Quaternion.identity,true,false);
            BraxChampionsPromenadeFurniture.Flower(fl,i%2==0?"flower_empodium":"flower_gazania",p+Vector3.up*.45f,1.62f,i*45f);
            Col(p.x-1f,p.x+1f,WalkY,WalkY+.48f,p.z-1f,p.z+1f,"donica");
        }
        foreach(float a in new[]{200f,340f})
        {
            var d=new Vector3(Mathf.Cos(a*Mathf.Deg2Rad),0,Mathf.Sin(a*Mathf.Deg2Rad));
            var p=new Vector3(0,WalkY,-31f)+d*9.4f;
            StationBench(p,-d,4,"ławka przy fontannie "+a.ToString("0"));
        }
        var flowers=Group(root,"D2 kwiaty (Poly Haven)");
        foreach(float s in new[]{-1f,1f})
        {
            float bx0=s<0?-42f:24f,bx1=s<0?-24f:42f;
            Bed(bx0,bx1,-42f,-22f);
            Tree((bx0+bx1)*.5f-4f,-35f,WalkY+.10f);Tree((bx0+bx1)*.5f+4f,-28f,WalkY+.10f);
            foreach(float x in new[]{bx0+3f,bx1-3f})BraxChampionsPromenadeFurniture.Planter(m,cols,flowers,x,-19.2f,s<0?"flower_empodium":"flower_gazania");
            OutdoorBench((bx0+bx1)*.5f,-19.4f,new Vector3(0,0,-1),WalkY);
        }
        // flowers along the front walk either side of the canopy (board: purple / pink beds by the entrance)
        foreach(float x in new[]{-30f,-24f,24f,30f})BraxChampionsPromenadeFurniture.Planter(m,cols,flowers,x,-1.0f,"flower_empodium");
        // lamps: the loop's outer edge, the plaza, the car park, the bus apron, the front walk
        foreach(var (x,z) in new[]{(-64.5f,-40f),(-64.5f,-26f),(64.0f,-40f),(64.0f,-26f),(-30f,-8.2f),(-10f,-8.2f),(10f,-8.2f),(30f,-8.2f),
                                   (-50f,-46f),(50f,-46f),(-18f,-46f),(18f,-46f),(-52f,-19f),(52f,-19f)})
        {BraxChampionsPromenadeFurniture.Lamp(m,cols,x,z);Lamps++;}
        for(int i=0;i<4;i++){BraxChampionsPromenadeFurniture.Lamp(m,cols,ParkX0+.5f,ParkZ0+8f+i*20f);BraxChampionsPromenadeFurniture.Lamp(m,cols,(ParkWalkX+ParkX1)*.5f,ParkZ0+12f+i*14f);Lamps+=2;}
        for(int i=0;i<3;i++){BraxChampionsPromenadeFurniture.Lamp(m,cols,BusX1-.6f,BusZ0+8f+i*18f);Lamps++;}
        // bollards on the zebra table's ends (stainless, 90 cm), the plaza side and the walk side
        for(float x=-5.25f;x<=5.3f;x+=1.5f)foreach(float z in new[]{FrontZ-LaneHalf-.45f,FrontZ+LaneHalf+.45f})
        {m.Cylinder(stainless,new Vector3(x,WalkY,z),.10f,.90f,16,Quaternion.identity,true);B(frame,x-.105f,x+.105f,WalkY+.70f,WalkY+.76f,z-.105f,z+.105f);Col(x-.11f,x+.11f,WalkY,WalkY+.9f,z-.11f,z+.11f,"słupek");}
        Pylon(-44f,-44.5f);
        TaxiSign(TaxiX0+1.5f,-5.6f,"TAXI");TaxiSign(KissX0+1.5f,-5.6f,"K+R");
    }
    static void Ring(Material mat,Vector3 c,float rIn,float rOut,float y0,float y1,int n)
    {
        for(int i=0;i<n;i++)
        {
            float a0=2*Mathf.PI*i/n,a1=2*Mathf.PI*(i+1)/n;
            Vector3 P(float a,float r,float y)=>new Vector3(c.x+Mathf.Cos(a)*r,y,c.z+Mathf.Sin(a)*r);
            Q(mat,P(a0,rIn,y1),P(a1,rIn,y1),P(a1,rOut,y1),P(a0,rOut,y1),Vector3.up);
            if(y1-y0>1e-3f)
            {
                Q(mat,P(a0,rOut,y0),P(a1,rOut,y0),P(a1,rOut,y1),P(a0,rOut,y1),new Vector3(Mathf.Cos((a0+a1)*.5f),0,Mathf.Sin((a0+a1)*.5f)));
                Q(mat,P(a0,rIn,y0),P(a1,rIn,y0),P(a1,rIn,y1),P(a0,rIn,y1),-new Vector3(Mathf.Cos((a0+a1)*.5f),0,Mathf.Sin((a0+a1)*.5f)));
            }
        }
    }
    static void Disc(Material mat,Vector3 c,float r,int n)
    {
        for(int i=0;i<n;i++){float a0=2*Mathf.PI*i/n,a1=2*Mathf.PI*(i+1)/n;
            T(mat,c,c+new Vector3(Mathf.Cos(a0)*r,0,Mathf.Sin(a0)*r),c+new Vector3(Mathf.Cos(a1)*r,0,Mathf.Sin(a1)*r),Vector3.up);}
    }
    // the forecourt fountain (board: a fountain on the roundabout), low so it never hides the station: a granite basin R 7
    // (coping .45), dark granite floor under the water, a central rose of jets (2.4 m) and an inner ring (1.4 m) rising from
    // foam, sixteen arcs from stainless nozzles on the rim falling toward the centre. Owner 11.11 19:49 ("woda ma lecieć ...
    // tryska ... realistycznie"): the water is no longer static tubes - BraxChampionsFontanna runs it every frame (real
    // ballistic arcs, spray, ripple rings and splashes, a moving surface); here only the stone, the nozzles and the foam
    static void Fountain(Vector3 c)
    {
        const int n=72;
        Ring(granite,c,7.0f,7.6f,c.y,c.y+.45f,n);
        Ring(graniteDark,c,6.85f,7.0f,c.y+.06f,c.y+.30f,n);
        Disc(graniteDark,c+Vector3.up*.06f,6.85f,n);
        Disc(water,c+Vector3.up*.30f,6.85f,n);
        Disc(foam(),c+Vector3.up*.32f,1.25f,32);                                         // 2 cm over the water (outside)
        m.Cylinder(stainless,c+Vector3.up*.20f,.10f,.18f,16,Quaternion.identity,true);   // the central rose, 8 cm proud
        var A=new List<Vector3>();var Bp=new List<Vector3>();var H=new List<float>();var W=new List<float>();var N=new List<int>();var Pl=new List<bool>();
        void J(Vector3 a,Vector3 b,float h,float w,int k){A.Add(a);Bp.Add(b);H.Add(h);W.Add(w);N.Add(k);Pl.Add(true);}
        // the central column: six streams up 2.4 m, opening as they fall onto the foam
        for(int i=0;i<6;i++){float a=2*Mathf.PI*i/6f;J(new Vector3(0,.38f,0),new Vector3(Mathf.Cos(a)*.55f,.30f,Mathf.Sin(a)*.55f),2.02f,.075f,18);}
        // the inner ring: eight jets 1.4 m
        for(int i=0;i<8;i++)
        {
            float a=2*Mathf.PI*(i+.5f)/8f;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            m.Cylinder(stainless,c+d*.75f+Vector3.up*.22f,.035f,.12f,10,Quaternion.identity,true);
            J(d*.75f+Vector3.up*.34f,d*1.15f+Vector3.up*.30f,1.06f,.05f,14);
        }
        // sixteen arcs: from the rim nozzles (R 6.5, .45) to the water at R 3.2, peak 1.7 m
        for(int i=0;i<16;i++)
        {
            float a=2*Mathf.PI*i/16f;var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            J(dir*6.5f+Vector3.up*.45f,dir*3.2f+Vector3.up*.30f,1.25f,.045f,16);
            Disc(foam(),c+dir*3.2f+Vector3.up*.32f,.28f,12);
            m.Cylinder(stainless,c+dir*6.62f+Vector3.up*.40f,.05f,.08f,10,Quaternion.identity,true);
        }
        BraxChampionsMeshBatch.FountainWater(root,"fontanna — płynąca woda (BraxChampionsFontanna)",c,K,A.ToArray(),Bp.ToArray(),H.ToArray(),W.ToArray(),N.ToArray(),Pl.ToArray(),
            new[]{Vector3.up*.30f},new[]{6.85f},waterJet);
        Col(c.x-7.6f,c.x+7.6f,c.y,c.y+.45f,c.z-7.6f,c.z+7.6f,"fontanna");
    }
    static Material foamMat;
    static Material foam(){if(!foamMat)foamMat=S("fountain_foam",new Color(.92f,.96f,.98f,.55f),.3f,0f,new Color(.25f,.27f,.28f),true);return foamMat;}
    // a raised lawn bed with a granite edge (15 cm), grass at +.10
    static void Bed(float x0,float x1,float z0,float z1)
    {
        float y=WalkY,e=.15f;
        B(granite,x0,x1,y,y+.15f,z0,z0+e,true);B(granite,x0,x1,y,y+.15f,z1-e,z1,true);
        B(granite,x0,x0+e,y,y+.15f,z0+e,z1-e,true);B(granite,x1-e,x1,y,y+.15f,z0+e,z1-e,true);
        Flat(lawn,x0+e,x1-e,z0+e,z1-e,y+.10f);
        Col(x0,x1,y,y+.15f,z0,z1,"rabata");
    }
    // the wayfinding pylon (board picture): navy totem 1.5 x 4.4 x .35, "DWORZEC" title, rows with arrows
    static void Pylon(float x,float z)
    {
        float w=1.5f,h=4.4f,d=.35f,y=WalkY;
        RB(signBlue,x-w*.5f,x+w*.5f,y+.3f,y+h,z-d*.5f,z+d*.5f,.06f,true);
        B(frame,x-w*.5f-.04f,x+w*.5f+.04f,y,y+.3f,z-d*.5f-.04f,z+d*.5f+.04f);
        var lm=new BraxChampionsMeshBatch();float zf=z-d*.5f,ty=y+h-.62f;
        BraxChampionsLetters3D.Text(lm,whiteLetters,navy,"DWORZEC",new Vector3(x,ty,zf),180f,.26f,.015f,.10f,w-.25f);
        B(paintWhite,x-w*.5f+.12f,x+w*.5f-.12f,ty-.16f,ty-.13f,zf-.012f,zf);
        string[] rows={"KOLEJ","AUTOBUSY","PARKING","TAXI","KISS&RIDE"};
        Vector2[] dirs={new Vector2(0,1),new Vector2(1,0),new Vector2(-1,0),new Vector2(1,0),new Vector2(1,0)};
        for(int i=0;i<rows.Length;i++)
        {
            float ry=ty-.62f-i*.6f;
            BraxChampionsLetters3D.Text(lm,whiteLetters,navy,rows[i],new Vector3(x-.12f,ry,zf),180f,.17f,.012f,.08f,w-.6f);
            // arrow at the left edge (seen from the front the left is +x)
            var ac=new Vector3(x+w*.5f-.22f,ry+.08f,zf-.012f);var d2=dirs[i];
            Vector3 D=new Vector3(-d2.x,d2.y,0);Vector3 N=new Vector3(-D.y,D.x,0);
            Q(whiteLetters,ac-D*.10f-N*.025f,ac+D*.03f-N*.025f,ac+D*.03f+N*.025f,ac-D*.10f+N*.025f,Vector3.back);
            T(whiteLetters,ac+D*.03f-N*.07f,ac+D*.12f,ac+D*.03f+N*.07f,Vector3.back);
        }
        BraxChampionsLetters3D.Build(lm,root,"D2 pylon");Signs++;
    }
    static void TaxiSign(float x,float z,string text)
    {
        m.Cylinder(steel,new Vector3(x,WalkY,z),.05f,2.7f,12,Quaternion.identity,true);
        RB(signBlue,x-.55f,x+.55f,WalkY+2.1f,WalkY+2.7f,z-.04f,z+.04f,.05f);
        var lm=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(lm,whiteLetters,navy,text,new Vector3(x,WalkY+2.24f,z-.045f),180f,.32f,.012f,.10f,.95f);
        BraxChampionsLetters3D.Build(lm,root,"D2 znak "+text);
        Col(x-.1f,x+.1f,WalkY,WalkY+2.7f,z-.1f,z+.1f,"znak");Signs++;
    }

    // ================================================================ the lift cars (BraxWinda: the city's lift; doors slide on the
    // cabin's local x, so the cabin and the landing doors are turned to put their door face on the track side)
    const float LiftDoorHalf=.55f;
    static void LiftCar(float x0,float x1,float z0,float z1,int p,float xa)
    {
        float xc=(x0+x1)*.5f,zc=(z0+z1)*.5f,yaw=p==0?180f:0f,hw=(x1-x0)*.5f-.12f,hd=(z1-z0)*.5f-.12f;   // local door face at -z
        float[] stops={WalkY+.012f,PlatformTop+.012f};string[] names={"PASAŻ","PERON "+(p+1)};   // cabin floor 12 mm over the landing
        string id="D2 winda "+(xa<-1f?"zachód":xa>1f?"wschód":"środek")+" peron "+(p+1);
        var landL=new Transform[2];var landR=new Transform[2];
        // a glass door leaf in a black frame (owner 11.11: nothing grey at glass), x xa..xb, height h, depth z0..z1
        void GlassLeaf(BraxChampionsMeshBatch b,float xa,float xb,float h,float z0,float z1)
        {
            b.BoxMinMax(frame,xa,xb,0f,.12f,z0,z1);b.BoxMinMax(frame,xa,xb,h-.08f,h,z0,z1);
            b.BoxMinMax(frame,xa,xa+.06f,.12f,h-.08f,z0,z1);b.BoxMinMax(frame,xb-.06f,xb,.12f,h-.08f,z0,z1);
            float zc=(z0+z1)*.5f;b.BoxMinMax(glass,xa+.05f,xb-.05f,.11f,h-.07f,zc-.006f,zc+.006f);
        }
        for(int s=0;s<2;s++)
        {
            var land=Group(root,id+" | przystanek "+names[s]);land.localPosition=new Vector3(xc,stops[s],zc)*K;land.localRotation=Quaternion.Euler(0,yaw,0);
            foreach(float side in new[]{-1f,1f})
            {
                var t=Group(land,"drzwi przystanku "+(side<0?"L":"P"));t.localPosition=new Vector3(side*LiftDoorHalf*.5f,0,-hd-.18f)*K;
                var lm=new BraxChampionsMeshBatch();
                GlassLeaf(lm,-LiftDoorHalf*.5f,LiftDoorHalf*.5f,2.2f,-.015f,.015f);
                lm.Build(t,"drzwi przystanku",Folder,false);
                BraxChampionsMeshBatch.Collider(t,"drzwi przystanku",new Vector3(0,1.1f,0),new Vector3(LiftDoorHalf,2.2f,.05f));
                foreach(var c in t.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(c.gameObject,0);
                if(side<0)landL[s]=t;else landR[s]=t;
            }
        }
        var cab=Group(root,id+" | kabina");cab.localPosition=new Vector3(xc,stops[0],zc)*K;cab.localRotation=Quaternion.Euler(0,yaw,0);
        var cm=new BraxChampionsMeshBatch();
        cm.BoxMinMax(granite,-hw,hw,-.12f,0f,-hd,hd);
        // the car: glass on three sides in a black frame (back corner posts, top and bottom rails), black ceiling plate
        cm.BoxMinMax(glass,-hw,hw,.10f,2.35f,hd+.014f,hd+.026f);
        foreach(float s in new[]{-1f,1f})cm.BoxMinMax(glass,s>0?hw+.014f:-hw-.026f,s>0?hw+.026f:-hw-.014f,.10f,2.35f,-hd,hd);
        foreach(float s in new[]{-1f,1f})
        {
            float a=s>0?hw-.02f:-hw-.04f,b=s>0?hw+.04f:-hw+.02f;
            cm.BoxMinMax(frame,a,b,0f,2.45f,hd-.02f,hd+.04f);                                            // back corner post
            cm.BoxMinMax(frame,a,b,0f,.12f,-hd,hd-.02f);cm.BoxMinMax(frame,a,b,2.33f,2.45f,-hd,hd-.02f);  // side rails
            cm.BoxMinMax(frame,s>0?LiftDoorHalf:-hw-.04f,s>0?hw+.04f:-LiftDoorHalf,0f,2.2f,-hd-.03f,-hd);  // front beside the doors
            cm.BoxMinMax(frame,s*(hw-.32f)-.02f,s*(hw-.32f)+.02f,.905f,.935f,hd-.02f,hd+.014f);          // handrail bracket
        }
        cm.BoxMinMax(frame,-hw+.02f,hw-.02f,0f,.12f,hd-.02f,hd+.04f);cm.BoxMinMax(frame,-hw+.02f,hw-.02f,2.33f,2.45f,hd-.02f,hd+.04f);
        cm.BoxMinMax(frame,-hw-.04f,hw+.04f,2.45f,2.53f,-hd-.03f,hd+.04f);
        cm.BoxMinMax(led,-.5f,.5f,2.42f,2.45f,-hd+.4f,hd-.4f);
        cm.BoxMinMax(frame,-hw-.04f,hw+.04f,2.2f,2.45f,-hd-.03f,-hd);
        cm.BoxMinMax(frame,-hw+.05f,hw-.05f,.9f,.94f,hd-.06f,hd-.02f);                                   // handrail (black)
        cm.BoxMinMax(signBlue,hw-.30f,hw-.05f,1.0f,1.5f,-hd+.02f,-hd+.06f);                            // button panel
        cm.Build(cab,"kabina",Folder,false);
        BraxChampionsMeshBatch.Collider(cab,"podłoga kabiny",new Vector3(0,-.06f,0),new Vector3(2*hw,.12f,2*hd));
        foreach(float s in new[]{-1f,1f})BraxChampionsMeshBatch.Collider(cab,"ściana kabiny",new Vector3(s*hw,1.25f,0),new Vector3(.05f,2.5f,2*hd));
        BraxChampionsMeshBatch.Collider(cab,"tył kabiny",new Vector3(0,1.25f,hd+.02f),new Vector3(2*hw,2.5f,.05f));
        var cabL=Group(cab,"drzwi kabiny L");var cabR=Group(cab,"drzwi kabiny P");
        var dm=new BraxChampionsMeshBatch();GlassLeaf(dm,-LiftDoorHalf,0f,2.2f,-hd-.07f,-hd-.04f);dm.Build(cabL,"drzwi kabiny L",Folder,false);
        dm=new BraxChampionsMeshBatch();GlassLeaf(dm,0f,LiftDoorHalf,2.2f,-hd-.07f,-hd-.04f);dm.Build(cabR,"drzwi kabiny P",Folder,false);
        foreach(var t in cab.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        var w=cab.gameObject.AddComponent<BraxWinda>();
        w.nazwa="D2 • WINDA PERON "+(p+1);w.kabina=cab;w.kabinaL=cabL;w.kabinaP=cabR;
        w.pietraY=new[]{root.TransformPoint(new Vector3(0,stops[0],0)*K).y,root.TransformPoint(new Vector3(0,stops[1],0)*K).y};
        w.pietraNazwy=names;w.uzytkowe=new[]{true,true};
        float callZ=p==0?z1+1.0f:z0-1.0f;
        w.wezwanie=new[]{root.TransformPoint(new Vector3(xc,stops[0]+1.2f,callZ)*K),root.TransformPoint(new Vector3(xc,stops[1]+1.2f,callZ)*K)};
        w.przystanekL=landL;w.przystanekP=landR;
        var shaft=root.TransformPoint(new Vector3(xc,0,zc)*K);
        w.szyb=new Vector2(shaft.x,shaft.z);w.wnetrze=new Vector2(hw*K,hd*K);
        w.przesuw=LiftDoorHalf*.95f*K;w.start=0;
    }

    // ================================================================ the train (BraxChampionsD2Pociag runs it)
    public const float TrainBodyWidth=2.85f,TrainStopX=PlatX1-6f,TrainSpeed=22f;   // nominal m, m/s (80 km/h on the line)
    static BraxChampionsD2Pociag trainRun;
    // the train is built in code since 11.11 (BraxChampionsD2Sklad: loco + 3 coaches, walkable, doors working); the metro
    // model is no longer used here
    static void Train(){TrainSet();}
}
