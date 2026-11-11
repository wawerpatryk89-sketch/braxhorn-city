using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// G1 KINO MULTIPLEX (board: "KINO 8 SAL (S1-S8)", the west wing x -85..-47, z -55..55, double height to the roof 11.6) -
// functional (owner 11.11 16:33): the lobby (z 25..55) entered by the north doors (x -70), from the north mall's west end
// (a portal with KINO MULTIPLEX) and seen through the west glazing; the reception: a film kiosk and the tills (AKCJA:
// choose a film, buy a ticket), the snack bar (popcorn, drinks), three live repertoire boards, eight poster light boxes,
// lounge seating by the windows; the ticket check at the halls' corridor (x -68.5..-64.5) with the hall doors, the door
// screens and posters; eight halls S1..S8 (west row x -84.6..-68.65, east row -64.35..-47.15, 20 m deep): a stage under a
// 16:9 screen in a black masking frame, stadium rows (10 x 24 / 26 red velvet seats - every seat a BraxSeat), aisle stairs
// with step lights, acoustic fabric walls, surround speakers, a black ceiling with star lights, the projection window, the
// exit signs. One BraxChampionsKino plays it (the owner's films from StreamingAssets/Mistrzowie/Kino/repertuar.json).
public static partial class BraxChampionsD2
{
    const float KX0=-G1HW+.3f,KCorr0=-68.5f,KCorr1=-64.5f,KLobbyZ=25f,KHallTop=10.6f,KRowD=1.15f,KRise=.45f,KFront=6.0f,KAisle=1.1f,KMidAisle=1.2f,KPitch=.56f;
    const int KRows=10;
    static readonly float[] KHallZ={KLobbyZ,5f,-15f,-35f,-G1HD+.3f};
    static Material kCarpet,kVelvet,kFabric,kExit,kWood;
    static void KinoMaterials()
    {
        // Poly Haven: poly_wool_herringbone (carpet, acoustic fabric), velour_velvet (seats), smoked_walnut_veneer (lobby)
        kCarpet=Inside(Tex("g1_kino_carpet_wine","poly_wool_herringbone",.8f,new Color(.46f,.10f,.13f)),.6f);
        kVelvet=Inside(Tex("g1_kino_seat_red_velvet","velour_velvet",.5f,new Color(.82f,.10f,.12f)),.6f);
        kFabric=Inside(Tex("g1_kino_acoustic_fabric","poly_wool_herringbone",1.2f,new Color(.22f,.22f,.26f)),.55f);
        kExit=S("g1_kino_exit_green",new Color(.10f,.80f,.30f),.3f,0f,new Color(.20f,1.0f,.40f));
        kWood=gWood;
    }
    // hall h: S1 west / S2 east from the lobby southward ... S7 / S8 at the south façade
    static void KHall(int h,out float xa,out float xb,out float zc,out float zd)
    {
        int row=h/2;bool west=h%2==0;
        xa=west?KX0+.1f:KCorr1+.15f;xb=west?KCorr0-.15f:G1CinemaX1-.25f;
        zd=KHallZ[row]-.15f;zc=row==3?-G1HD+.4f:KHallZ[row+1]+.15f;
    }
    static int KBlock(int h){KHall(h,out float xa,out float xb,out _,out _);return Mathf.FloorToInt((xb-xa-2f*KAisle-KMidAisle)*.5f/KPitch);}
    static int KSeats(int h)=>2*KBlock(h);

    static void G1Cinema()
    {
        KinoMaterials();
        float y=G1F0,top=G1Roof-.9f;
        var kinoRoot=Group(root,"G1 | KINO MULTIPLEX (BraxChampionsKino: repertuar, recepcja, 8 sal)");
        // ---- the wing's walls: the corridor's two walls (hall side fabric, corridor side walnut) with the hall doors, the
        // dividers between the halls, the lobby's south wall (hall side fabric, lobby side walnut), the east wall to the mall
        foreach(int side in new[]{0,1})
        {
            float xw0=side==0?KCorr0-.15f:KCorr1,xw1=side==0?KCorr0:KCorr1+.15f,xm=(xw0+xw1)*.5f;
            var gaps=new List<(float,float)>();
            for(int r=0;r<4;r++){KHall(r*2+side,out _,out _,out float zc,out _);gaps.Add((zc+2.1f,zc+3.9f));}
            gaps.Sort((p,q)=>p.Item1.CompareTo(q.Item1));
            float z=-G1HD+.3f;
            // west wall: the hall side (x lower) fabric, the corridor side walnut; the east wall the other way round
            var m0=side==0?kFabric:kWood;var m1=side==0?kWood:kFabric;
            foreach(var (a,b) in gaps){Seg(z,a);B(m0,xw0,xm,y+2.3f,top,a,b);B(m1,xm,xw1,y+2.3f,top,a,b);z=b;}
            Seg(z,KLobbyZ-.15f);
            void Seg(float a,float b){if(b-a<.01f)return;B(m0,xw0,xm,y,top,a,b,true);B(m1,xm,xw1,y,top,a,b,true);}
        }
        foreach(float zw in new[]{5f,-15f,-35f})
        {
            B(kFabric,KX0,KCorr0-.15f,y,top,zw-.15f,zw+.15f,true);
            B(kFabric,KCorr1+.15f,G1CinemaX1-.15f,y,top,zw-.15f,zw+.15f,true);
        }
        foreach(var (a,b) in new[]{(KX0,KCorr0),(KCorr1,G1CinemaX1-.15f)})
        {
            B(kFabric,a,b,y,top,KLobbyZ-.15f,KLobbyZ,true);B(gVelvetBlack,a,b,y,top,KLobbyZ,KLobbyZ+.15f,true);
            // the lobby side: vertical walnut slats (4 x 4 cm, every 12 cm) on the black backing, to 9 m
            for(float x=a+.06f;x<b-.06f;x+=.12f)B(walnut,x,x+.04f,y+.10f,9.0f,KLobbyZ+.15f,KLobbyZ+.19f);
            B(walnut,a,b,9.0f,9.06f,KLobbyZ+.15f,KLobbyZ+.21f);B(ledWarm,a+.1f,b-.1f,8.98f,9.0f,KLobbyZ+.16f,KLobbyZ+.20f);
        }
        // the façade linings of the halls (west and south), the east wall to the mall (with the portal and the P1 overlook)
        B(kFabric,KX0,KX0+.1f,y,top,-G1HD+.3f,KLobbyZ-.15f,true);B(kFabric,G1CinemaX1-.25f,G1CinemaX1-.15f,y,top,-G1HD+.3f,KLobbyZ-.15f,true);
        B(kFabric,KX0,KCorr0-.15f,y,top,-G1HD+.3f,-G1HD+.4f,true);B(kFabric,KCorr1+.15f,G1CinemaX1-.15f,y,top,-G1HD+.3f,-G1HD+.4f,true);
        B(gWhitePanel,G1CinemaX1-.15f,G1CinemaX1,y,top,-G1HD+.3f,G1NorthMallZ0,true);
        B(gWhitePanel,G1CinemaX1-.15f,G1CinemaX1,y,top,G1NorthMallZ1,G1HD-.26f,true);
        B(gDark,G1CinemaX1-.15f,G1CinemaX1+.02f,4.2f,G1F1-.12f,G1NorthMallZ0,G1NorthMallZ1);                  // the portal's head
        B(gWhitePanel,G1CinemaX1-.15f,G1CinemaX1,G1Ceil1,top,G1NorthMallZ0,G1NorthMallZ1);              // over the P1 overlook
        GlassRail(new Vector3(G1CinemaX1+.04f,G1F1,G1NorthMallZ0+.04f),new Vector3(G1CinemaX1+.04f,G1F1,G1NorthMallZ1-.04f),1.08f,1.4f);
        Col(G1CinemaX1-.02f,G1CinemaX1+.1f,G1F1,G1F1+1.1f,G1NorthMallZ0,G1NorthMallZ1,"balustrada nad kinem");
        PortalSign("KINO MULTIPLEX (pasaż)",new Vector3(G1CinemaX1+.02f,3.92f,(G1NorthMallZ0+G1NorthMallZ1)*.5f),Vector3.right,11.4f,"KINO MULTIPLEX","BRAXHORN • 8 SAL • BILETY I BAR",1.0f);
        Flush("G1 kino | walls",kinoRoot);
        // ---- the lobby, the corridor
        var boards=new List<Renderer>();var posters=new List<Renderer>();var posterSlot=new List<int>();var doorScreens=new Renderer[8];
        KinoLobby(kinoRoot,boards,posters,posterSlot);Flush("G1 kino | lobby, reception, bar",kinoRoot);
        KinoCorridor(kinoRoot,posters,posterSlot,doorScreens);Flush("G1 kino | corridor",kinoRoot);
        // ---- the halls
        var screens=new Renderer[8];var sMin=new Vector3[8];var sMax=new Vector3[8];var spk=new Vector3[8];var rows=new int[8];var seats=new int[8];
        var surf=new List<Renderer>();var surfHall=new List<int>();kSeatR.Clear();kSeatH.Clear();
        for(int h=0;h<8;h++)
        {
            var hg=Group(kinoRoot,"G1 KINO SALA "+(h+1));
            screens[h]=KinoHall(h,hg,out sMin[h],out sMax[h],out spk[h]);rows[h]=KRows;seats[h]=KSeats(h);
            Flush("G1 kino | sala "+(h+1),hg);
            foreach(var r in hg.GetComponentsInChildren<MeshRenderer>(true))if(r!=screens[h]){surf.Add(r);surfHall.Add(h);}
            G1Halls++;
        }
        // ---- the runtime
        var k=kinoRoot.gameObject.AddComponent<BraxChampionsKino>();
        k.ekrany=screens;k.salaMin=sMin;k.salaMax=sMax;k.glosnik=spk;k.rzedy=rows;k.miejsca=seats;
        k.widok=surf.ToArray();k.widokSala=surfHall.ToArray();k.fotele=kSeatR.ToArray();k.foteleSala=kSeatH.ToArray();
        k.tablice=boards.ToArray();k.plakaty=posters.ToArray();k.plakatFilm=posterSlot.ToArray();k.drzwiEkrany=doorScreens;
        k.kiosk=root.TransformPoint(new Vector3(-79.6f,y,30.0f)*K);k.kasa=root.TransformPoint(new Vector3(-74.9f,y,30.0f)*K);k.zasieg=1.8f*K;
        Debug.Log("G1_KINO sale=8 miejsca="+G1CinemaSeats+" tablice="+boards.Count+" plakaty="+posters.Count+" repertuar=StreamingAssets/Mistrzowie/Kino/repertuar.json");
    }
    static readonly List<Renderer> kSeatR=new List<Renderer>();static readonly List<int> kSeatH=new List<int>();
    static Material kScreenBoard,kScreenCard;static Material[] kMenu;
    static Material[] kScreenHall,kScreenDoor;
    static void KinoScreens()
    {
        var px=new Color32[BraxChampionsKinoEkrany.TW*BraxChampionsKinoEkrany.TH];BraxChampionsKinoEkrany.TablicaWkrotce(px,"");
        kScreenBoard=ScreenMaterial("g1_kino_repertuar",BraxChampionsKinoEkrany.TW,BraxChampionsKinoEkrany.TH,px);
        px=new Color32[BraxChampionsKinoEkrany.PW*BraxChampionsKinoEkrany.PH];BraxChampionsKinoEkrany.KartaWkrotce(px);
        kScreenCard=ScreenMaterial("g1_kino_plakat",BraxChampionsKinoEkrany.PW,BraxChampionsKinoEkrany.PH,px);
        kMenu=new Material[3];
        for(int i=0;i<3;i++){px=new Color32[BraxChampionsKinoEkrany.EW*BraxChampionsKinoEkrany.EH];BraxChampionsKinoEkrany.MenuBaru(px,i);kMenu[i]=ScreenMaterial("g1_kino_menu_baru_"+(i+1),BraxChampionsKinoEkrany.EW,BraxChampionsKinoEkrany.EH,px);}
        kScreenHall=new Material[8];kScreenDoor=new Material[8];
        for(int h=0;h<8;h++)
        {
            px=new Color32[BraxChampionsKinoEkrany.EW*BraxChampionsKinoEkrany.EH];BraxChampionsKinoEkrany.EkranWkrotce(px,h);
            kScreenHall[h]=ScreenMaterial("g1_kino_ekran_sala_"+(h+1),BraxChampionsKinoEkrany.EW,BraxChampionsKinoEkrany.EH,px);
            px=new Color32[BraxChampionsKinoEkrany.DW*BraxChampionsKinoEkrany.DH];BraxChampionsKinoEkrany.DrzwiWkrotce(px,h);
            kScreenDoor[h]=ScreenMaterial("g1_kino_drzwi_sala_"+(h+1),BraxChampionsKinoEkrany.DW,BraxChampionsKinoEkrany.DH,px);
        }
    }

    // ---- the lobby (x -84.7..-47.15, z 25.15..54.74): dark granite floor with a wine carpet runner from the doors to the
    // halls, the black ceiling with a field of warm downlights; along the south wall the reception (x -82..-70: the film
    // kiosk and three tills) and the bar (x -63..-53) - front counters (black, white marble tops, a red LED line), back
    // counters with popcorn machines, drinks fountains, coffee, menu screens; the three repertoire boards over the reception,
    // KASY / BAR / SALE 1-8 in 3D; the ticket check at the corridor; eight poster light boxes on the east wall; the lounge
    static void KinoLobby(Transform kr,List<Renderer> boards,List<Renderer> posters,List<int> slots)
    {
        KinoScreens();
        float y=G1F0,x0=KX0,x1=G1CinemaX1-.15f,z0=KLobbyZ+.15f,z1=G1HD-.26f,yc=G1Roof-.92f;
        B(gMallBand,x0,x1,WalkY,y,z0,z1,true);
        B(kCarpet,-71.2f,-66.0f,y,y+.012f,z0+1.0f,G1HD-3.9f);                                  // the runner (on the stone)
        Q(gCinemaDark,new Vector3(x0,yc,z0),new Vector3(x1,yc,z0),new Vector3(x1,yc,z1),new Vector3(x0,yc,z1),Vector3.down);
        for(float x=x0+1.5f;x<x1-1f;x+=2.5f)for(float z=z0+1.5f;z<z1-1f;z+=2.5f)m.Cylinder(ledWarm,new Vector3(x,yc-.002f,z),.07f,.012f,12,Quaternion.Euler(180,0,0),true,false);
        foreach(float z in new[]{z0+.6f,z1-.6f})B(signBlue,x0+.3f,x1-.3f,yc-.03f,yc,z-.04f,z+.04f);
        // the reception and the bar: front counters (z 28.6..29.4), back counters (z 25.15..25.85), the staff zone between
        foreach(var (a,b,name) in new[]{(-82.0f,-71.5f,"KASY • BILETY"),(-63.0f,-53.0f,"BAR • POPCORN")})
        {
            RB(gCinemaDark,a+.02f,b-.02f,y,y+.12f,28.68f,29.32f,.02f,false,1);                    // the black plinth
            RB(walnut,a,b,y+.10f,y+1.02f,28.6f,29.4f,.035f,true,2);
            RB(StoneTop(),a-.05f,b+.05f,y+1.02f,y+1.07f,28.55f,29.45f,.015f,false,2);
            B(ledWarm,a+.1f,b-.1f,y+.98f,y+1.0f,29.4f,29.415f);                                   // the warm line under the top
            RB(walnut,a,b,y,y+.95f,z0,z0+.7f,.03f,true,2);RB(stainless,a+.012f,b-.012f,y+.95f,y+.99f,z0,z0+.72f,.01f,false,1);   // 12 mm in from the ends (the wall battens)
            RB(walnut,a-.4f,a,y,y+1.02f,z0,29.4f,.03f,true,2);                                  // the counter's end (closes the staff zone)
            var lmc=new BraxChampionsMeshBatch();
            BraxChampionsLetters3D.Text(lmc,gLetterFace,gLetterSide,name,new Vector3((a+b)*.5f,y+2.35f,KLobbyZ+.192f),0f,.34f,.05f,.12f,b-a-1f);
            BraxChampionsLetters3D.Build(lmc,kr,"G1 kino napis "+name);Signs++;
        }
        // the reception: the film kiosk (a black totem with a screen: AKCJA = next film), three tills (POS, card readers)
        RB(gCinemaDark,-80.1f,-79.1f,y,y+1.55f,29.5f,29.9f,.04f,true);
        boards.Add(ScreenQuad(kr,"kiosk wyboru filmu",kScreenBoard,new Vector3(-79.6f,y+1.1f,29.902f),.86f,.39f,180f));
        foreach(float x in new[]{-77.0f,-74.9f,-72.8f})
        {
            var tp=new Vector3(x,y+1.07f,28.95f);Goods("G1 kino kasa "+x,()=>Monitor(tp,Vector3.forward));
            RB(gCinemaDark,x+.25f,x+.38f,y+1.07f,y+1.20f,29.12f,29.30f,.012f,false,1);
        }
        // the bar's back: popcorn machines (glass cases in black frames, lit, full of popcorn), drinks fountains, coffee
        for(int i=0;i<3;i++)
        {
            float x=-62.2f+i*1.4f;
            RB(gRed,x,x+.9f,y+.99f,y+1.10f,z0+.08f,z0+.62f,.015f,false,1);
            B(Inside(S("g1_kino_popcorn",new Color(.98f,.86f,.52f),.85f,0f),.9f),x+.05f,x+.85f,y+1.10f,y+1.42f,z0+.13f,z0+.57f);
            foreach(float fx in new[]{x,x+.9f})B(frame,fx-.015f,fx+.015f,y+1.10f,y+1.75f,z0+.08f,z0+.62f);
            B(frame,x,x+.9f,y+1.75f,y+1.80f,z0+.08f,z0+.62f);B(ledWarm,x+.05f,x+.85f,y+1.72f,y+1.75f,z0+.13f,z0+.57f);
            var gp=new Vector3(x+.015f,y+1.10f,z0+.62f);var gq=new Vector3(x+.885f,y+1.10f,z0+.62f);
            Q(glass,gp,gq,gq+Vector3.up*.62f,gp+Vector3.up*.62f,Vector3.forward);Q(glass,gp,gq,gq+Vector3.up*.62f,gp+Vector3.up*.62f,Vector3.back);
        }
        for(int i=0;i<2;i++){float x=-57.6f+i*1.5f;RB(stainless,x,x+1.1f,y+.99f,y+1.75f,z0+.1f,z0+.6f,.03f,false,2);for(int t=0;t<4;t++){RB(gCinemaDark,x+.1f+t*.25f,x+.2f+t*.25f,y+1.25f,y+1.35f,z0+.6f,z0+.68f,.012f,false,1);m.Cylinder(stainless,new Vector3(x+.15f+t*.25f,y+1.18f,z0+.64f),.008f,.07f,8,Quaternion.identity,true);}}
        RB(gCinemaDark,-54.6f,-53.6f,y+.99f,y+1.55f,z0+.1f,z0+.6f,.04f,false,2);m.Cylinder(stainless,new Vector3(-54.1f,y+1.25f,z0+.62f),.02f,.08f,10,Quaternion.Euler(90,0,0),true,true);   // the coffee machine
        for(int i=0;i<3;i++){float x=-62.4f+i*3.2f;RB(gCinemaDark,x,x+2.8f,y+2.7f,y+4.3f,KLobbyZ+.19f,KLobbyZ+.25f,.02f,false,1);ScreenQuad(kr,"menu baru "+(i+1),kMenu[i],new Vector3(x+1.4f,y+3.5f,KLobbyZ+.252f),2.68f,1.48f,180f);}   // the bar's menu: POPCORN / NAPOJE / ZESTAWY
        // over the reception (owner 11.11 19:09 "tam gdzie się kupuje bilety ... pod oba filmy"): the repertoire board (4.0 x
        // 1.8 m) between the two films' posters (16:9 screens 3.2 x 1.8, the owner's key art: slot 100 + film)
        RB(gCinemaDark,-76.0f-2.06f,-76.0f+2.06f,y+2.9f,y+4.82f,KLobbyZ+.19f,KLobbyZ+.25f,.02f,false,1);
        boards.Add(ScreenQuad(kr,"tablica repertuaru",kScreenBoard,new Vector3(-76.0f,y+3.86f,KLobbyZ+.252f),4.0f,1.8f,180f));
        for(int i=0;i<2;i++)
        {
            float x=i==0?-80.5f:-71.5f;
            RB(gCinemaDark,x-1.66f,x+1.66f,y+2.9f,y+4.82f,KLobbyZ+.19f,KLobbyZ+.25f,.02f,false,1);
            posters.Add(ScreenQuad(kr,"plakat filmu "+(i+1)+" nad kasami",kScreenCard,new Vector3(x,y+3.86f,KLobbyZ+.252f),3.2f,1.8f,180f));slots.Add(100+i);
        }
        // the ticket check at the corridor: a podium with a scanner, the posts; SALE 1-8 over the corridor
        RB(walnut,-67.4f,-66.8f,y,y+1.05f,26.6f,27.2f,.04f,true,2);RB(gCinemaDark,-67.35f,-66.85f,y+1.05f,y+1.08f,26.65f,27.15f,.01f,false,1);B(screenLit(),-67.3f,-66.9f,y+1.08f,y+1.092f,26.7f,27.1f);
        var lms=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(lms,gLetterFace,gLetterSide,"SALE 1-8",new Vector3(-66.5f,y+4.6f,KLobbyZ+.152f),0f,.46f,.07f,.12f,3.6f);
        B(ledWarm,KCorr0+.2f,KCorr1-.2f,y+4.40f,y+4.415f,KLobbyZ+.15f,KLobbyZ+.165f);
        BraxChampionsLetters3D.Build(lms,kr,"G1 kino napis SALE");Signs++;
        B(gCinemaDark,KCorr0,KCorr1,y+3.7f,top(),KLobbyZ-.15f,KLobbyZ+.15f);                          // the lintel over the corridor (down to its ceiling)
        // eight poster light boxes on the east wall (x -47.15, facing west), z 41.3..53.7: black frames, lit faces
        for(int i=0;i<8;i++)
        {
            float zc=42.05f+i*1.55f,xw=G1CinemaX1-.15f;
            RB(frame,xw-.10f,xw,y+1.05f,y+2.95f,zc-.66f,zc+.66f,.018f,false,2);
            posters.Add(ScreenQuad(kr,"plakat "+(i+1),kScreenCard,new Vector3(xw-.102f,y+2.0f,zc),1.2f,1.75f,90f));slots.Add(i);
        }
        // the lounge by the west windows: upholstered benches back to back round low tables, planters
        for(int i=0;i<3;i++)
        {
            float z=33f+i*6f;
            Sofa(new Vector3(-82.6f,y,z),Vector3.right,3,gLeatherBlack,"sofa kino");
            Sofa(new Vector3(-78.6f,y,z),Vector3.left,3,gLeatherBlack,"sofa kino");
            RB(walnut,-81.1f,-80.1f,y+.30f,y+.36f,z-.6f,z+.6f,.02f,true,2);foreach(var (dx,dz) in new[]{(-.4f,-.5f),(.4f,-.5f),(-.4f,.5f),(.4f,.5f)})m.Cylinder(walnut,new Vector3(-80.6f+dx,y,z+dz),.025f,.30f,10,Quaternion.identity,false);
            Prop(root,"ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf",new Vector3(-80.6f,y,z+3f),i*40f,.3f);
        }
        foreach(var (x,z) in new[]{(-60f,36f),(-56f,36f),(-60f,42f),(-56f,42f)})
        {
            m.Cylinder(stainless,new Vector3(x,y,z),.26f,.02f,24);m.Cylinder(stainless,new Vector3(x,y+.02f,z),.04f,1.0f,12);
            m.Cylinder(StoneTop(),new Vector3(x,y+1.02f,z),.40f,.04f,32);
            Col(x-.4f,x+.4f,y,y+1.06f,z-.4f,z+.4f,"stolik barowy");
        }
        Bin(-52.5f,y,30.2f);Bin(-83.6f,y,30.2f);
    }
    static float top()=>G1Roof-.9f;

    // ---- the corridor to the halls (x -68.5..-64.5): wine carpet, black ceiling at 4.0 with downlights and a red LED
    // line, the hall doors (black leaves) with the screens over them and a poster light box beside each
    static void KinoCorridor(Transform kr,List<Renderer> posters,List<int> slots,Renderer[] doorScreens)
    {
        float y=G1F0,z0=-G1HD+.3f,z1=KLobbyZ+.15f,yc=y+3.7f;
        B(kCarpet,KCorr0,KCorr1,WalkY,y,z0,z1,true);
        Q(gCinemaDark,new Vector3(KCorr0,yc,z0),new Vector3(KCorr1,yc,z0),new Vector3(KCorr1,yc,z1),new Vector3(KCorr0,yc,z1),Vector3.down);
        for(float z=z0+1.5f;z<z1-1f;z+=3f)m.Cylinder(ledWarm,new Vector3(-66.5f,yc-.002f,z),.08f,.012f,12,Quaternion.Euler(180,0,0),true,false);
        foreach(float x in new[]{KCorr0+.3f,KCorr1-.3f})B(gRed,x-.03f,x+.03f,yc-.03f,yc,z0+.5f,z1-.5f);
        B(gCinemaDark,KCorr0,KCorr0+.06f,y,y+.12f,z0,z1);B(gCinemaDark,KCorr1-.06f,KCorr1,y,y+.12f,z0,z1);  // skirtings
        var keep=plaster;plaster=gCinemaDark;
        for(int h=0;h<8;h++)
        {
            KHall(h,out _,out _,out float zc,out _);bool west=h%2==0;
            float xw=west?KCorr0-.075f:KCorr1+.075f,xf=west?KCorr0+.002f:KCorr1-.002f,yaw=west?-90f:90f,zd=zc+3.0f;
            Door("G1_kino_sala_"+(h+1),"G1 kino sala "+(h+1),xw,y,zd,90f,1.8f,2.3f,false);
            // the door frame (black), the screen over it, the poster box beside it
            foreach(float s in new[]{-1f,1f})B(frame,Mathf.Min(xf,xf+(west?.03f:-.03f)),Mathf.Max(xf,xf+(west?.03f:-.03f)),y,y+2.35f,zd+s*.9f-.03f,zd+s*.9f+.03f);
            B(frame,Mathf.Min(xf,xf+(west?.03f:-.03f)),Mathf.Max(xf,xf+(west?.03f:-.03f)),y+2.30f,y+2.38f,zd-.93f,zd+.93f);
            RB(gCinemaDark,Mathf.Min(xf,xf+(west?.08f:-.08f)),Mathf.Max(xf,xf+(west?.08f:-.08f)),y+2.55f,y+2.95f,zd-.74f,zd+.74f,.018f,false,2);
            doorScreens[h]=ScreenQuad(kr,"sala "+(h+1)+" nad drzwiami",kScreenDoor[h],new Vector3(xf+(west?.082f:-.082f),y+2.75f,zd),1.4f,.35f,west?-90f:90f);
            float zp=zd+2.2f;
            RB(frame,Mathf.Min(xf,xf+(west?.10f:-.10f)),Mathf.Max(xf,xf+(west?.10f:-.10f)),y+.95f,y+2.85f,zp-.66f,zp+.66f,.018f,false,2);
            posters.Add(ScreenQuad(kr,"plakat sala "+(h+1),kScreenCard,new Vector3(xf+(west?.102f:-.102f),y+1.9f,zp),1.2f,1.75f,west?-90f:90f));slots.Add(-(h+1));
            var lm=new BraxChampionsMeshBatch();
            BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"SALA "+(h+1),new Vector3(xf+(west?.004f:-.004f),y+3.15f,zd),west?90f:-90f,.24f,.02f,.10f,1.6f);   // letters face the corridor
            BraxChampionsLetters3D.Build(lm,kr,"G1 kino napis sala "+(h+1));Signs++;
        }
        plaster=keep;
    }

    // ---- a hall (returns its screen; bounds and the speaker point for the runtime). The stadium: 10 rows (rise 45, depth
    // 115 - a clear passage in front of every row), two blocks of seats between three aisles (the sides along the walls, one
    // in the middle) with two steps per row, step lights and handrails; the front zone with the stage under the screen,
    // pleated velvet curtains at the screen's sides, black velvet masking; the walls lined with fabric acoustic panels
    // (4 cm proud, 3 cm reveals) with warm sconces between them; a black ceiling with star lights and an LED cove; the
    // projection window with the projector behind it; surround speakers; the exit sign
    static Renderer KinoHall(int h,Transform hg,out Vector3 wMin,out Vector3 wMax,out Vector3 wSpk)
    {
        KHall(h,out float xa,out float xb,out float zc,out float zd);
        bool west=h%2==0;float y=G1F0,xm=(xa+xb)*.5f;
        int nb=KBlock(h);float bw=nb*KPitch;
        float bl0=xa+KAisle,bl1=bl0+bw,br1=xb-KAisle,br0=br1-bw;                         // the two seat blocks
        var aisles=new[]{(xa,bl0),(bl1,br0),(br1,xb)};
        B(kCarpet,xa,xb,WalkY,y,zc,zc+KFront,true);
        RB(gVelvetBlack,xa+.6f,xb-.6f,y,y+.30f,zc,zc+1.2f,.03f,true,2);                   // the stage
        B(ledWarm,xa+.7f,xb-.7f,y+.20f,y+.215f,zc+1.2f,zc+1.215f);
        for(int k=1;k<=KRows;k++)
        {
            float z0=zc+KFront+(k-1)*KRowD,z1=k==KRows?zd:z0+KRowD,yk=y+k*KRise,zm=z0+KRowD*.5f;
            B(kCarpet,bl0,bl1,y,yk,z0,z1,true);B(kCarpet,br0,br1,y,yk,z0,z1,true);
            foreach(var (a,b) in aisles)
            {
                B(kCarpet,a,b,y,yk-KRise*.5f,z0,zm,true);B(kCarpet,a,b,y,yk,zm,z1,true);
                B(led,a+.12f,b-.12f,yk-KRise*.5f-.06f,yk-KRise*.5f-.035f,z0-.012f,z0);B(led,a+.12f,b-.12f,yk-.06f,yk-.035f,zm-.012f,zm);
                GameObjectRamp("G1 kino sala "+(h+1)+" schody "+k,new Vector3((a+b)*.5f,yk-KRise,z0),new Vector3((a+b)*.5f,yk,z0+KRowD),b-a);
            }
            foreach(var (s0,s1) in new[]{(bl0,bl1),(br0,br1)})Col(s0,s1,yk,yk+1.10f,z0+.92f,z0+1.10f,"rząd foteli");
        }
        // handrails on the side walls along the aisles (stainless on brackets, 0.9 over the nosings)
        foreach(float s in new[]{-1f,1f})
        {
            float xr=s<0?xa+.06f:xb-.06f;
            var p0=new Vector3(xr,y+.9f,zc+KFront-.3f);var p1=new Vector3(xr,y+KRows*KRise+.9f,zc+KFront+(KRows-.5f)*KRowD);
            m.Tube(stainless,p0,p1,.022f,12);
            for(int k=0;k<=KRows;k+=2){var q=Vector3.Lerp(p0,p1,k/(float)KRows);m.Tube(stainless,q,q+(s<0?Vector3.left:Vector3.right)*.05f,.012f,8);}
        }
        // the screen (16:9) with black velvet masking, pleated red velvet curtains at its sides, the speakers behind it
        float sw=Mathf.Min(xb-xa-3.4f,13.6f),sh=sw*9f/16f,sy=y+1.45f,zs=zc+.6f;
        var scr=ScreenQuad(hg,"ekran sali "+(h+1),kScreenHall[h],new Vector3(xm,sy+sh*.5f,zs),sw,sh,180f);
        float mw=.35f;
        B(gVelvetBlack,xm-sw*.5f-mw,xm-sw*.5f,y+.30f,sy+sh+mw,zs-.15f,zs+.02f);B(gVelvetBlack,xm+sw*.5f,xm+sw*.5f+mw,y+.30f,sy+sh+mw,zs-.15f,zs+.02f);
        B(gVelvetBlack,xm-sw*.5f,xm+sw*.5f,sy+sh,sy+sh+mw,zs-.15f,zs+.02f);B(gVelvetBlack,xm-sw*.5f,xm+sw*.5f,y+.30f,sy,zs-.15f,zs+.02f);
        B(gVelvetBlack,xa,xb,sy+sh+mw,KHallTop,zs-.15f,zs+.02f);                            // the valance over the screen
        foreach(float s in new[]{-1f,1f})
        {
            float c0=s<0?xa:xm+sw*.5f+mw,c1=s<0?xm-sw*.5f-mw:xb;
            Curtain(c0,c1,y+.30f,sy+sh+mw,zs+.03f);
        }
        // the walls: fabric acoustic panels on the side walls, warm sconces between them; the back wall panelled too
        foreach(float s in new[]{-1f,1f})
        {
            float xw=s<0?xa:xb;
            bool doorWall=(s>0)==west;
            for(float z=zs+.4f;z<zd-.6f;z+=1.25f)
            {
                float zb=Mathf.Min(z+1.22f,zd-.3f),yb=y+Mathf.Clamp((z-zc-KFront)/KRowD+1f,0f,KRows)*KRise+1.2f;
                if(doorWall&&z<zc+4.1f&&zb>zc+1.9f)continue;
                RB(kFabric,Mathf.Min(xw,xw-s*.04f),Mathf.Max(xw,xw-s*.04f),yb,KHallTop-.5f,z,zb,.012f,false,1);
                var sc=new Vector3(xw-s*.07f,yb+2.6f,zb+.015f);RB(ledWarm,sc.x-.03f,sc.x+.03f,sc.y-.18f,sc.y+.18f,sc.z-.06f,sc.z+.06f,.02f,false,1);
            }
            // surround speakers (black, rounded) over the panels
            for(float z=zc+5f;z<zd-1f;z+=4f)
            {
                float yy=y+Mathf.Clamp((z-zc-KFront)/KRowD,0,KRows)*KRise+4.6f;
                RB(gCinemaDark,Mathf.Min(xw-s*.04f,xw-s*.26f),Mathf.Max(xw-s*.04f,xw-s*.26f),yy,yy+.55f,z-.18f,z+.18f,.025f,false,2);
            }
        }
        for(float x=xa+.4f;x<xb-.6f;x+=1.25f)RB(kFabric,x,Mathf.Min(x+1.22f,xb-.3f),y+KRows*KRise+1.2f,KHallTop-.5f,zd-.04f,zd,.012f,false,1);
        // the ceiling: black with star lights, an LED cove along the side walls
        Q(gVelvetBlack,new Vector3(xa,KHallTop,zc),new Vector3(xb,KHallTop,zc),new Vector3(xb,KHallTop,zd),new Vector3(xa,KHallTop,zd),Vector3.down);
        for(float x=xa+1f;x<xb-.5f;x+=1.6f)for(float z=zc+1.2f;z<zd-.5f;z+=1.6f)m.Cylinder(ledWarm,new Vector3(x+((int)(z*7)%3)*.3f,KHallTop-.002f,z),.025f,.008f,8,Quaternion.Euler(180,0,0),true,false);
        foreach(float s in new[]{-1f,1f}){float xw=s<0?xa:xb;RB(gCinemaDark,Mathf.Min(xw,xw-s*.25f),Mathf.Max(xw,xw-s*.25f),KHallTop-.32f,KHallTop,zc,zd,.02f,false,1);B(ledWarm,Mathf.Min(xw-s*.25f,xw-s*.262f),Mathf.Max(xw-s*.25f,xw-s*.262f),KHallTop-.30f,KHallTop-.28f,zc+.3f,zd-.3f);}
        // the projection window and the projector behind it
        float yp=y+KRows*KRise+3.3f;
        RB(gCinemaDark,xm-.8f,xm+.8f,yp-.42f,yp+.42f,zd-.06f,zd,.02f,false,1);
        B(glassDark,xm-.6f,xm+.6f,yp-.3f,yp+.3f,zd-.075f,zd-.06f);
        // the exit sign over the door (green, lit)
        var lm=new BraxChampionsMeshBatch();
        float xe=west?xb-.002f:xa+.002f;
        RB(gCinemaDark,Mathf.Min(xe,xe+(west?-.05f:.05f)),Mathf.Max(xe,xe+(west?-.05f:.05f)),y+2.5f,y+2.86f,zc+2.15f,zc+3.85f,.015f,false,1);
        BraxChampionsLetters3D.Text(lm,kExit,gLetterSide,"WYJŚCIE",new Vector3(xe+(west?-.052f:.052f),y+2.58f,zc+3.0f),west?-90f:90f,.20f,.02f,.10f,1.4f);
        BraxChampionsLetters3D.Build(lm,hg,"G1 kino sala "+(h+1)+" WYJŚCIE");Signs++;
        // the seats: their own group, drawn up close only (a hall seen from the lobby or another hall is behind walls)
        var sg=Group(hg,"fotele sali "+(h+1));
        var keep=m;m=new BraxChampionsMeshBatch();
        for(int k=1;k<=KRows;k++)
        {
            float z0=zc+KFront+(k-1)*KRowD,yk=y+k*KRise;
            foreach(var (s0,_) in new[]{(bl0,bl1),(br0,br1)})
            {
                for(int i=0;i<=nb;i++)CinemaArm(s0+i*KPitch,yk,z0);
                for(int i=0;i<nb;i++)
                {
                    float x=s0+(i+.5f)*KPitch;CinemaSeat(x,yk,z0);
                    int num=(s0==bl0?i:nb+i)+1;
                    Seat(x,yk+.55f,z0+.66f,Vector3.back,"FOTEL · SALA "+(h+1)+" · RZĄD "+k+" · M "+num);G1CinemaSeats++;
                }
            }
        }
        m.Build(sg,"fotele",Folder,false);m=keep;
        foreach(var r in sg.GetComponentsInChildren<MeshRenderer>(true)){kSeatR.Add(r);kSeatH.Add(h);}
        wMin=root.TransformPoint(new Vector3(xa,y,zc)*K);wMax=root.TransformPoint(new Vector3(xb,KHallTop,zd)*K);
        wSpk=root.TransformPoint(new Vector3(xm,sy+sh*.5f,zc+.3f)*K);
        return scr;
    }
    // a cinema seat facing -z at x on row level yk, row front z0: a black seat pan under a padded velvet cushion (6 deg),
    // a padded velvet back (14 deg) with a rounded head bolster, the black moulded back shell
    static void CinemaSeat(float x,float yk,float z0)
    {
        var tiltS=Quaternion.Euler(-6f,0,0);var tiltB=Quaternion.Euler(-14f,0,0);
        m.RoundedBox(kVelvet,new Vector3(x,yk+.48f,z0+.66f),new Vector3(.49f,.13f,.48f),.05f,tiltS,2);
        m.RoundedBox(kVelvet,new Vector3(x,yk+.86f,z0+.97f),new Vector3(.49f,.66f,.13f),.055f,tiltB,2);
        m.RoundedBox(kVelvet,new Vector3(x,yk+1.25f,z0+1.06f),new Vector3(.44f,.16f,.15f),.065f,tiltB,1);
        m.RoundedBox(gCinemaDark,new Vector3(x,yk+.93f,z0+1.05f),new Vector3(.51f,.86f,.045f),.02f,tiltB,1);
    }
    // the standard between two seats: a black moulded side panel (the leg) under a padded black leather armrest with a cup
    // holder (stainless ring, black well)
    static void CinemaArm(float x,float yk,float z0)
    {
        m.RoundedBox(gCinemaDark,new Vector3(x,yk+.31f,z0+.72f),new Vector3(.05f,.62f,.58f),.02f,Quaternion.identity,1);
        m.RoundedBox(gLeatherBlack,new Vector3(x,yk+.655f,z0+.68f),new Vector3(.08f,.055f,.56f),.025f,Quaternion.identity,1);
        m.Cylinder(stainless,new Vector3(x,yk+.672f,z0+.44f),.043f,.016f,14,Quaternion.identity,false,false);
        m.Cylinder(gCinemaDark,new Vector3(x,yk+.676f,z0+.44f),.038f,.012f,14,Quaternion.identity,true,false);
    }
    // a pleated curtain (red velvet) from x0 to x1 at z, y0..y1: folds every 18 cm, 7 cm deep, facing +z
    static void Curtain(float x0,float x1,float y0,float y1,float z)
    {
        int n=Mathf.Max(2,Mathf.RoundToInt((x1-x0)/.09f));
        for(int i=0;i<n;i++)
        {
            float xa=x0+(x1-x0)*i/n,xb=x0+(x1-x0)*(i+1)/n,za=z+(i%2==0?0f:.07f),zb=z+(i%2==0?.07f:0f);
            var a=new Vector3(xa,y0,za);var b=new Vector3(xb,y0,zb);var nrm=Vector3.Cross(Vector3.up,b-a).normalized;if(nrm.z<0)nrm=-nrm;
            Q(kVelvet,a,b,b+Vector3.up*(y1-y0),a+Vector3.up*(y1-y0),nrm);
        }
        RB(gCinemaDark,x0,x1,y1-.06f,y1+.04f,z-.02f,z+.12f,.015f,false,1);              // the track cover
    }

}
