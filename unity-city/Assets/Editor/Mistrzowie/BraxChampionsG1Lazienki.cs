using System;
using System.Collections.Generic;
using UnityEngine;

// G1 toilets, done again (owner 11.11 19:49 "niewykończone"; 19:54-19:57 the R1 toilets as the bar: "drewno, pisuary,
// ceramiki", "fugi, wszystko ... każdy detal", "pisuary, ceramika, krany - wszystko ma być realne"; the urinal's drain at
// the bottom of the bowl, not in its middle). The SW block x -47..-30 on both levels: the lobby from the mall (oak battens,
// warm plaster, the WC sign), the accessible WC and the baby care room off it, the men's and the women's rooms.
// Finishes: slim ceramic wall tiles with their grout (Poly Haven long_white_tiles) in warm sand, the vanity walls in
// graphite; a dark granite tile floor; a warm plaster ceiling with downlights. Sanitary ware built as real objects:
// wall-hung urinals (an egg-shaped porcelain shell with a real opening, the lip, the bowl inside, the chrome drain at the
// bottom, the chrome flush valve with its lever), oak dividers on chrome brackets; wall-hung pans (seat, lid, the chrome
// flush plate with two buttons), oak cubicles on chrome legs with a chrome head rail; vessel basins on a stone counter
// over an oak cabinet, tall chrome sensor taps that RUN WATER when you come near (BraxChampionsFontanna with a sensor),
// soap dispensers, live mirrors in black frames, hand dryers, roll holders, hooks, bins.
public static partial class BraxChampionsD2
{
    static Material wcTile,wcTileDark,wcFloor,chrome,blackMatt;
    static Transform g1Mirrors;
    static void WcMaterials()
    {
        wcTile=Inside(Tex("g1_wc_wall_tiles_sand","long_white_tiles",1.0f,new Color(1.0f,.90f,.77f)),.9f);
        wcTileDark=Inside(Tex("g1_wc_wall_tiles_graphite","long_white_tiles",1.0f,new Color(.30f,.30f,.32f)),.9f);
        wcFloor=Inside(Tex("g1_wc_floor_granite","granite_tile_03",1.2f,new Color(.62f,.63f,.66f)),.85f);
        chrome=Inside(S("g1_chrome_polished",new Color(.88f,.89f,.91f),.06f,1f),.9f);
        blackMatt=Inside(S("g1_black_matt_metal",new Color(.06f,.06f,.065f),.55f,.3f),.9f);
    }
    // a wall of two halves (its two faces in their own finish): along x (thin in z) or along z (thin in x)
    static void Wall2X(float x0,float x1,float y0,float y1,float z0,float z1,Material south,Material north,bool col=true)
    {float zm=(z0+z1)*.5f;B(south,x0,x1,y0,y1,z0,zm,false);B(north,x0,x1,y0,y1,zm,z1,false);if(col)Col(x0,x1,y0,y1,z0,z1,"ściana");}
    static void Wall2Z(float x0,float x1,float y0,float y1,float z0,float z1,Material west,Material east,bool col=true)
    {float xm=(x0+x1)*.5f;B(west,x0,xm,y0,y1,z0,z1,false);B(east,xm,x1,y0,y1,z0,z1,false);if(col)Col(x0,x1,y0,y1,z0,z1,"ściana");}

    // ---- the wall-hung urinal, ported from R1 (BraxCeramika.Pisuar on claude/miasto-opus-20261031, commit 17cc5a0; owner
    // 20:38-20:39: "pisuary są na gałęzi u niego, więc możesz wziąć") into this region's batch (nominal metres, built x K):
    // one smooth porcelain sheet - the outline at the wall (a superellipse) -> the sides straight out of the wall -> the
    // rounded lip (r 1.8 cm) -> the bowl (a Bezier from the lip deep toward the wall, then down) funnelling to the drain at
    // the LOWEST point, 7 cm under the lower lip (owner 19:52-19:59: "odpływ na dole") -> a 2 cm sump -> a level grate
    // (chrome / graphite rings in one plane); over it the chrome flush valve: the pipe out of the hood, the body, the lever
    // with its knob, the connection into the wall with a rosette. 36 cm wide, 66 high (underside 46, lip 58), 33.5 deep.
    // o: the tile face under the urinal's centre at floor level; f: out of the wall.
    static void UrinalReal(Vector3 o,Vector3 f)
    {
        f.y=0;f.Normalize();var en=f;var ex=Vector3.Cross(Vector3.up,f).normalized;
        const float pO=2.4f,pR=2.6f,rl=.018f,rd=.024f,yD=.525f,zD=.175f;
        var Bc=new Vector2(0f,.77f);
        float Se(float c,float pw)=>Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),2f/pw);
        Vector2 Obrys(float th){float c=Mathf.Cos(th),s=Mathf.Sin(th);return new Vector2(.18f*(1f-.06f*(1f-s))*Se(c,pO),.79f+.33f*Se(s,pO));}
        Vector2 Warga(float th){float c=Mathf.Cos(th),s=Mathf.Sin(th);return new Vector2(.14f*(1f-.08f*(1f-s))*Se(c,pR),.77f+.19f*Se(s,pR));}
        float Zr(float th)=>.25f-.085f*Mathf.Sin(th);
        Vector3 W(Vector2 q,float z)=>o+ex*q.x+Vector3.up*q.y+en*z;
        Vector3 P(int seg,float th,float t)
        {
            Vector2 O=Obrys(th),R=Warga(th);float zr=Zr(th);var dIn=(Bc-R).normalized;var Rin=R+dIn*rl;
            switch(seg)
            {
                case 0:{float a=t*Mathf.PI*.5f;return W(O+(R-O)*(1f-Mathf.Cos(a)),-.004f+(zr+.004f)*Mathf.Sin(a));}
                case 1:{float a=t*Mathf.PI*.5f;return W(R+dIn*(rl*Mathf.Sin(a)),zr-rl+rl*Mathf.Cos(a));}
                case 2:
                {
                    float sn=Mathf.Sin(th),z0=zr-rl;
                    var B0=new Vector3(Rin.x,Rin.y,z0);var B1=new Vector3(Rin.x,Rin.y,Mathf.Lerp(z0,.06f,.55f+.25f*sn));
                    var B3=new Vector3(rd*Mathf.Cos(th),yD,zD-rd*sn);var B2=B3+Vector3.up*((Rin.y-yD)*.6f);
                    float u=1f-t;var q=u*u*u*B0+3f*u*u*t*B1+3f*u*t*t*B2+t*t*t*B3;
                    return W(new Vector2(q.x,q.y),q.z);
                }
                default:{float sn=Mathf.Sin(th);return W(new Vector2(rd*Mathf.Cos(th),Mathf.Lerp(yD,yD-.02f,t)),zD-rd*sn);}
            }
        }
        // the sheet faces away from the wall at the lip's lowest point (R1: the sign of dP/dth x dP/dt along en)
        float Zn(int seg,float th,float t){const float e=1e-3f;var a=(P(seg,th+e,t)-P(seg,th-e,t))/e;var b=(P(seg,th,Mathf.Min(1f,t+e))-P(seg,th,Mathf.Max(0f,t-e)))/e;return Vector3.Dot(Vector3.Cross(a,b),en);}
        bool flip=Zn(0,-Mathf.PI*.5f,1f)<0f;
        int[] podzial={10,5,16,1};
        for(int seg=0;seg<4;seg++){int sg=seg;m.Surface(porcelainMat(),(u,v)=>P(sg,-Mathf.PI*.5f+u*Mathf.PI*2f,v),72,podzial[seg],flip);}
        // the level drain grate in the sump: chrome / graphite rings in one plane
        var cS=W(new Vector2(0f,yD-.02f),zD);float[] pas={0f,.22f,.34f,.54f,.66f,1f};
        for(int b=0;b<pas.Length-1;b++)m.Lathe(b%2==0?chrome:blackMatt,cS,new[]{new Vector2(rd*pas[b+1],0f),new Vector2(Mathf.Max(.0005f,rd*pas[b]),0f)},36,Quaternion.identity,false,false);
        // the flush valve over it
        const float zz=.07f;
        m.Tube(chrome,W(new Vector2(0f,1.08f),zz),W(new Vector2(0f,1.33f),zz),.016f,14);
        m.Tube(chrome,W(new Vector2(0f,1.30f),zz),W(new Vector2(0f,1.40f),zz),.028f,18);
        m.Cylinder(chrome,W(new Vector2(0f,1.40f),zz),.028f,.012f,18,Quaternion.identity,true,false,.020f);
        m.Tube(chrome,W(new Vector2(.01f,1.345f),zz),W(new Vector2(.10f,1.345f),zz),.007f,10);
        m.Tube(chrome,W(new Vector2(.095f,1.345f),zz),W(new Vector2(.125f,1.345f),zz),.012f,12);
        m.Tube(chrome,W(new Vector2(0f,1.37f),zz),W(new Vector2(0f,1.37f),-.02f),.016f,14);
        m.Tube(chrome,W(new Vector2(0f,1.37f),-.025f),W(new Vector2(0f,1.37f),.016f),.034f,20);
        var c0=o-ex*.18f+Vector3.up*.46f;var c1=o+ex*.18f+Vector3.up*1.12f+en*.335f;
        Col(Mathf.Min(c0.x,c1.x),Mathf.Max(c0.x,c1.x),c0.y,c1.y,Mathf.Min(c0.z,c1.z),Mathf.Max(c0.z,c1.z),"pisuar");
    }
    // an oak divider between urinals (45 x 90 cm, 25 mm, rounded) on two chrome brackets
    static void UrinalDivider(Vector3 wall,Vector3 f)
    {
        f.y=0;f.Normalize();var rot=Quaternion.LookRotation(f,Vector3.up);
        m.RoundedBox(gOak,wall+f*.255f+Vector3.up*1.0f,new Vector3(.025f,.90f,.45f),.010f,rot,2);
        foreach(float y in new[]{.70f,1.30f})m.Cylinder(chrome,wall+f*.012f+Vector3.up*y,.016f,.022f,12,Quaternion.FromToRotation(Vector3.up,f),true);
    }
    // a wall-hung pan: wall = tile face on its axis at floor level, f = out of the wall
    static void WcPanReal(Vector3 wall,Vector3 f)
    {
        f.y=0;f.Normalize();var rot=Quaternion.LookRotation(f,Vector3.up);var r=Vector3.Cross(Vector3.up,f).normalized;
        m.RoundedBox(porcelainMat(),wall+f*.28f+Vector3.up*.27f,new Vector3(.36f,.28f,.54f),.13f,rot,3);   // the bowl, 41 cm top
        m.RoundedBox(porcelainMat(),wall+f*.06f+Vector3.up*.27f,new Vector3(.40f,.34f,.10f),.04f,rot,2);  // the back to the wall
        // the seat ring and the closed lid (white, a hair over the rim)
        var ring=new List<Vector3>();
        for(int k=0;k<=36;k++){float t=k*Mathf.PI*2f/36f;ring.Add(wall+f*(.31f+.22f*Mathf.Sin(t))+r*(.165f*Mathf.Cos(t))+Vector3.up*.428f);}
        for(int k=0;k+1<ring.Count;k++)m.Tube(porcelainMat(),ring[k],ring[k+1],.016f,8);
        m.RoundedBox(porcelainMat(),wall+f*.31f+Vector3.up*.452f,new Vector3(.35f,.022f,.46f),.010f,rot,2);
        m.Cylinder(chrome,wall+f*.105f+Vector3.up*.44f+r*.12f,.012f,.03f,10,Quaternion.FromToRotation(Vector3.up,r),true);   // hinges
        m.Cylinder(chrome,wall+f*.105f+Vector3.up*.44f-r*.15f,.012f,.03f,10,Quaternion.FromToRotation(Vector3.up,r),true);
        // the flush plate (chrome) with its two buttons, the roll holder with a roll, a hook on the door side
        m.RoundedBox(chrome,wall+f*.008f+Vector3.up*1.02f,new Vector3(.25f,.16f,.016f),.008f,rot,2);
        m.RoundedBox(chrome,wall+f*.022f+Vector3.up*1.02f-r*.035f,new Vector3(.10f,.12f,.014f),.006f,rot,2);
        m.RoundedBox(chrome,wall+f*.022f+Vector3.up*1.02f+r*.06f,new Vector3(.06f,.12f,.014f),.006f,rot,2);
        var rh=wall+r*.42f+Vector3.up*.70f;
        m.Tube(chrome,rh+f*.012f,rh+f*.10f,.008f,8);m.Tube(chrome,rh+f*.10f,rh+f*.10f-r*.13f,.008f,8);
        m.Cylinder(porcelainMat(),rh+f*.10f-r*.005f,.055f,.11f,18,Quaternion.FromToRotation(Vector3.up,-r),true,true);
        var c0=wall+f*.02f-r*.2f;var c1=wall+f*.58f+r*.2f;Col(Mathf.Min(c0.x,c1.x),Mathf.Max(c0.x,c1.x),wall.y,wall.y+.46f,Mathf.Min(c0.z,c1.z),Mathf.Max(c0.z,c1.z),"WC");
        Seat(wall.x+f.x*.31f,wall.y+.46f,wall.z+f.z*.31f,f,"WC");
    }
    // a vessel basin on the counter top (Ø 42, 14 cm) with its chrome drain, a tall chrome sensor tap behind it with the
    // water (runs while you stand at it), a soap dispenser; top = the counter top under the basin's centre, back = toward
    // the wall
    static void BasinSet(Vector3 top,Vector3 back,string label)
    {
        back.y=0;back.Normalize();var r=Vector3.Cross(Vector3.up,back).normalized;
        m.Lathe(porcelainMat(),top,new[]{new Vector2(0f,0f),new Vector2(.11f,0f),new Vector2(.17f,.03f),new Vector2(.205f,.10f),new Vector2(.212f,.14f),new Vector2(.200f,.143f),new Vector2(.192f,.10f),new Vector2(.155f,.045f),new Vector2(.06f,.022f),new Vector2(0f,.02f)},36,Quaternion.identity,false,false);
        m.Cylinder(chrome,top+Vector3.up*.014f,.032f,.012f,18,Quaternion.identity,true);              // the drain (6 mm proud)
        // the tap: flange, column to 32 cm, an arc spout over the bowl's centre, the aerator
        var tb=top+back*.27f;
        m.Cylinder(chrome,tb,.032f,.012f,18,Quaternion.identity,true);
        m.Cylinder(chrome,tb,.017f,.30f,16,Quaternion.identity,false);
        Vector3 prev=tb+Vector3.up*.30f;
        for(int k=1;k<=10;k++){float t=k/10f*Mathf.PI*.5f;var q=tb+Vector3.up*(.30f+.07f*Mathf.Sin(t))-back*(.07f*(1f-Mathf.Cos(t)));if(k==10)q=tb+Vector3.up*.37f-back*.07f;m.Tube(chrome,prev,q,.016f,12);prev=q;}
        var sp=tb+Vector3.up*.37f-back*.07f;
        m.Tube(chrome,sp,tb+Vector3.up*.37f-back*.24f,.016f,12);
        var aer=tb+Vector3.up*.37f-back*.255f;
        m.Cylinder(chrome,aer-Vector3.up*.035f,.019f,.05f,16,Quaternion.identity,true,true);
        m.Cylinder(blackMatt,tb+Vector3.up*.20f-back*.016f,.006f,.004f,10,Quaternion.FromToRotation(Vector3.up,-back),true);   // the sensor's eye
        // the water: straight down from the aerator into the bowl, ripples on its bottom
        var aerOut=aer-Vector3.up*.036f;
        BraxChampionsMeshBatch.FountainWater(root,"G1 "+label+" | kran z czujnikiem — woda",aerOut,K,
            new[]{Vector3.zero},new[]{new Vector3(.002f,top.y+.025f-aerOut.y,0f)},new[]{.004f},new[]{.016f},new[]{12},new[]{true},
            new[]{new Vector3(top.x-aerOut.x,top.y+.03f-aerOut.y,top.z-aerOut.z)},new[]{.10f},waterJet,1.6f);
        // the soap dispenser beside it
        var sd=top+back*.27f+r*.24f;
        m.Cylinder(chrome,sd,.022f,.012f,14,Quaternion.identity,true);m.Cylinder(chrome,sd,.012f,.17f,12,Quaternion.identity,true);
        m.Tube(chrome,sd+Vector3.up*.16f,sd+Vector3.up*.17f-back*.08f,.008f,8);
    }
    // a hand dryer on the wall (brushed steel, rounded), its black outlet at the bottom
    static void HandDryer(Vector3 wall,Vector3 f,float y)
    {
        f.y=0;f.Normalize();var rot=Quaternion.LookRotation(f,Vector3.up);
        m.RoundedBox(stainless,wall+f*.075f+Vector3.up*y,new Vector3(.30f,.40f,.13f),.05f,rot,3);
        m.RoundedBox(blackMatt,wall+f*.10f+Vector3.up*(y-.215f),new Vector3(.20f,.05f,.07f),.02f,rot,2);
    }
    // a live mirror in a black frame on a wall: c = the tile face's centre, n into the room
    static void G1Mirror(string label,Vector3 c,Vector3 n,float w,float h)
    {
        n.y=0;n.Normalize();var u=Vector3.Cross(Vector3.up,n).normalized;
        if(!g1Mirrors)g1Mirrors=Group(root,"G1 lustra | żywe odbicie (BraxChampionsMirror)");
        var g0=BraxWnetrzaKit.Grupa;bool i0=BraxWnetrzaKit.Wnetrze;
        BraxWnetrzaKit.Grupa=g1Mirrors;BraxWnetrzaKit.Wnetrze=true;
        try{BraxChampionsMirror.Wall("G1 "+label,root.TransformPoint((c+n*.030f)*K),root.rotation*n,w*K,h*K,null,8f*K,12f*K,768,384);}
        catch(Exception e){Debug.LogError("G1 lustro "+label+" DO POPRAWKI: "+e.Message);}
        finally{BraxWnetrzaKit.Grupa=g0;BraxWnetrzaKit.Wnetrze=i0;}
        // the frame: 35 mm black bars round the glass, 50 mm deep from the tiles
        var rot=Quaternion.LookRotation(n,Vector3.up);
        foreach(float s in new[]{-1f,1f})
        {
            m.RoundedBox(frame,c+n*.025f+Vector3.up*(s*(h*.5f+.0175f)),new Vector3(w+.07f,.035f,.05f),.008f,rot,1);
            m.RoundedBox(frame,c+n*.025f+u*(s*(w*.5f+.0175f)),new Vector3(.035f,h,.05f),.008f,rot,1);
        }
        m.RoundedBox(ledWarm,c+n*.04f+Vector3.up*(h*.5f+.06f),new Vector3(w-.2f,.012f,.02f),.004f,rot,1);   // the light over it
    }

    static void G1Toilets(int l)
    {
        if(!wcTile)WcMaterials();
        float y=LevelY(l),top=l==0?G1Slab:G1Roof-.9f,yc=y+3.0f,y0=l==0?WalkY:G1F1-.12f,ym=MallCeil(l);
        const float X0=-47f,X1=-30f,Z0=-54.7f,ZL=-32f,ZM=-25f;
        string lv=l==0?" P0":" P1";
        // floors: dark granite tiles; ceilings: warm plaster with downlights (no white, owner 18:28)
        B(wcFloor,X0+.15f,X1-.15f,y0,y,Z0,ZM-.15f,true);
        B(wcTile,X0+.15f,X1-.15f,y,yc,Z0-.03f,Z0);                                  // the south wall's tiles on the façade backing
        Q(gWallWarm,new Vector3(X0+.15f,yc,Z0),new Vector3(X1-.15f,yc,Z0),new Vector3(X1-.15f,yc,ZM-.15f),new Vector3(X0+.15f,yc,ZM-.15f),Vector3.down);
        for(float x=X0+1.4f;x<X1-1f;x+=1.8f)for(float z=Z0+1.2f;z<ZM-1f;z+=1.8f){m.Cylinder(frame,new Vector3(x,yc-.014f,z),.075f,.014f,16,Quaternion.identity,false,true);m.Cylinder(ledWarm,new Vector3(x,yc-.026f,z),.055f,.012f,16,Quaternion.identity,false,true);}
        // the outer walls: west (to the cinema wing), east (the food court: walnut on its side), the mall wall (black backing
        // for the oak battens) with the opening x -39.2..-36.2
        Wall2Z(X0,X0+.15f,y,yc,Z0,ZM,gCinemaDark,wcTile);B(gCinemaDark,X0,X0+.15f,yc,top,Z0,ZM);
        Wall2Z(X1-.15f,X1,y,yc,Z0,ZM,wcTile,gWood);B(gWood,X1-.15f,X1,yc,top,Z0,ZM);
        foreach(var (a,b) in new[]{(X0+.15f,-39.2f),(-36.2f,X1-.15f)})Wall2X(a,b,y,top,ZM-.15f,ZM,gWallWarm,gCinemaDark);
        B(gCinemaDark,-39.2f,-36.2f,y+2.6f,top,ZM-.15f,ZM);
        RB(frame,-39.27f,-39.17f,y+.004f,y+2.6f,ZM-.17f,ZM+.02f,.01f,false,1);RB(frame,-36.23f,-36.13f,y+.004f,y+2.6f,ZM-.17f,ZM+.02f,.01f,false,1);RB(frame,-39.27f,-36.13f,y+2.57f,y+2.66f,ZM-.17f,ZM+.02f,.01f,false,1);
        // the mall side: vertical oak battens (40 mm every 100 mm, 30 mm proud) on the black backing, an LED line at their foot
        for(float x=X0+.20f;x<X1-.05f;x+=.10f)
        {
            if(x>-39.35f&&x<-36.05f)continue;
            RB(gOak,x,x+.04f,y+.12f,ym-.04f,ZM+.004f,ZM+.034f,.006f,false,1);
        }
        B(ledWarm,X0+.2f,-39.35f,y+.05f,y+.065f,ZM+.004f,ZM+.02f);B(ledWarm,-36.05f,X1-.05f,y+.05f,y+.065f,ZM+.004f,ZM+.02f);
        var lm=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"WC",new Vector3(-37.7f,y+2.85f,ZM+.036f),0f,.46f,.04f,.10f,2.6f);
        BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"PANOWIE • PANIE • DOSTĘPNA • MATKA I DZIECKO",new Vector3(-37.7f,y+2.70f,ZM+.036f),0f,.07f,.015f,.14f,4.6f);
        // inside the lobby: the rooms' doors and the side rooms
        BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"PANOWIE",new Vector3(-42.0f,y+2.35f,ZL+.002f),0f,.14f,.015f,.10f,1.6f);
        BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"PANIE",new Vector3(-36.0f,y+2.35f,ZL+.002f),0f,.14f,.015f,.10f,1.6f);
        BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"WC DOSTĘPNE",new Vector3(-42.848f,y+2.45f,-30.6f),90f,.12f,.015f,.10f,2.4f);
        BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"MATKA I DZIECKO",new Vector3(-34.152f,y+2.45f,-30.6f),-90f,.12f,.015f,.10f,2.6f);
        BraxChampionsLetters3D.Build(lm,root,"G1 napisy WC"+lv);Signs+=6;
        // the rooms' north wall (z -32: tiles in the rooms, warm plaster in the lobby) with the doors; the M / K partition
        // (graphite tiles both sides: the vanities' walls)
        foreach(var (a,b) in new[]{(X0+.15f,-42.5f),(-41.5f,-36.5f),(-35.5f,X1-.15f)})Wall2X(a,b,y,yc,ZL-.15f,ZL,wcTile,gWallWarm);
        B(gWallWarm,X0+.15f,X1-.15f,yc,top,ZL-.15f,ZL);
        foreach(float a in new[]{-42.5f,-36.5f})Wall2X(a,a+1f,y+2.2f,yc,ZL-.15f,ZL,wcTile,gWallWarm,false);
        var keep=plaster;plaster=gOak;
        Door("G1_wc_m"+l,"G1 WC panowie"+lv,-42f,y,ZL-.075f,0f,1.0f,2.2f,false);Door("G1_wc_k"+l,"G1 WC panie"+lv,-36f,y,ZL-.075f,0f,1.0f,2.2f,false);
        Wall2Z(-38.6f,-38.45f,y,yc,Z0,ZL-.15f,wcTileDark,wcTileDark);B(gWallWarm,-38.6f,-38.45f,yc,top,Z0,ZL-.15f);
        // the side rooms off the lobby: the accessible WC (x -46.85..-43) and the baby care room (x -33.85..-30.15)
        Wall2Z(-43f,-42.85f,y,yc,ZL,-29f,wcTile,gWallWarm);Wall2Z(-43f,-42.85f,y,yc,-28f,ZM-.15f,wcTile,gWallWarm);Wall2Z(-43f,-42.85f,y+2.2f,yc,-29f,-28f,wcTile,gWallWarm,false);
        Wall2Z(-34f,-33.85f,y,yc,ZL,-29f,gWallWarm,wcTile);Wall2Z(-34f,-33.85f,y,yc,-28f,ZM-.15f,gWallWarm,wcTile);Wall2Z(-34f,-33.85f,y+2.2f,yc,-29f,-28f,gWallWarm,wcTile,false);
        foreach(float x in new[]{-43f,-34f})B(gWallWarm,x,x+.15f,yc,top,ZL,ZM-.15f);
        Door("G1_wc_acc"+l,"G1 WC dostępne"+lv,-42.925f,y,-28.5f,90f,1.0f,2.2f,false);Door("G1_wc_baby"+l,"G1 pokój matki i dziecka"+lv,-33.925f,y,-28.5f,90f,1.0f,2.2f,false);
        // the lobby: an oak bench, a plant, a bin
        StationBench(new Vector3(-38.5f,y,-30.9f),Vector3.forward,3,"ławka przed WC G1"+lv,2,true);
        Prop(root,"ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf",new Vector3(-41.9f,y,-26.0f),20f,.3f);
        Bin(-35.6f,y,-26.0f);
        // cubicles along the south wall: M 4 (x -46.85..-40.85), K 5 (x -38.45..-30.95), 1.5 x 2.0 m: oak partitions and
        // fronts 15 cm over the floor on chrome legs, a chrome head rail, oak doors; in each a pan, a roll, a hook, a bin
        void Cubicles(float xa,int n,string who)
        {
            float zf=Z0+2.0f;
            for(int i=0;i<=n;i++){float b=xa+i*1.5f;RB(gOak,b-.012f,b+.012f,y+.15f,y+2.05f,Z0+.004f,zf,.008f,true,1);m.Cylinder(chrome,new Vector3(b,y,zf-.03f),.02f,.15f,12,Quaternion.identity,true);}
            for(int i=0;i<n;i++)
            {
                float a=xa+i*1.5f,b=a+1.5f;
                RB(gOak,a+.012f,a+.35f,y+.15f,y+2.05f,zf-.012f,zf+.012f,.008f,true,1);RB(gOak,a+1.15f,b-.012f,y+.15f,y+2.05f,zf-.012f,zf+.012f,.008f,true,1);
                Door("G1_wc_"+who+l+"_"+i,"G1 kabina WC"+lv,a+.75f,y+.15f,zf,0f,.8f,1.90f,false);
                WcPanReal(new Vector3(a+.75f,y,Z0+.004f),Vector3.forward);
                m.Cylinder(chrome,new Vector3(a+1.25f,y+1.75f,zf-.04f),.012f,.06f,10,Quaternion.Euler(90,0,0),true);   // hook
                m.Cylinder(stainless,new Vector3(a+1.25f,y,Z0+.25f),.10f,.32f,20,Quaternion.identity,true);            // bin
            }
            m.Tube(chrome,new Vector3(xa-.012f,y+2.07f,zf),new Vector3(xa+n*1.5f+.012f,y+2.07f,zf),.02f,12);
        }
        Cubicles(X0+.15f,4,"m");Cubicles(-38.45f,5,"k");
        plaster=keep;
        // urinals on the men's west wall (graphite tiles behind them) with oak dividers: 4 at 80 cm
        B(wcTileDark,X0+.15f,X0+.164f,y,yc,-51.2f,-46.6f);                         // a 14 mm tile panel over the sand tiles
        for(int i=0;i<4;i++)
        {
            float z=-50.6f+i*.8f;UrinalReal(new Vector3(X0+.164f,y,z),Vector3.right);
            if(i>0)UrinalDivider(new Vector3(X0+.164f,y,z-.4f),Vector3.right);
        }
        // the vanities on the partition (both rooms): an oak cabinet, a stone top, three vessel basins with sensor taps, the
        // live mirror in a black frame over them, two hand dryers beside
        foreach(float s in new[]{-1f,1f})
        {
            float xw=s<0?-38.6f:-38.45f,xo=xw+s*.58f;
            RB(gOak,Mathf.Min(xw,xo)+(s<0?.0f:.0f),Mathf.Max(xw,xo),y+.30f,y+.80f,-40f,-35f,.012f,true,2);
            RB(gStoneTop,Mathf.Min(xw,xo+s*.03f),Mathf.Max(xw,xo+s*.03f),y+.80f,y+.84f,-40.03f,-34.97f,.008f,false,2);
            for(int i=0;i<3;i++)BasinSet(new Vector3(xw+s*.36f,y+.84f,-39.15f+i*1.65f),Vector3.right*(-s),(s<0?"WC panowie":"WC panie")+lv+" umywalka "+(i+1));
            G1Mirror((s<0?"WC panowie":"WC panie")+lv,new Vector3(xw,y+1.75f,-37.5f),Vector3.right*s,4.6f,1.0f);
            HandDryer(new Vector3(xw,y,-34.3f),Vector3.right*s,1.25f);HandDryer(new Vector3(xw,y,-33.6f),Vector3.right*s,1.25f);
        }
        // the accessible WC: pan, fold-up grab bars, basin with a sensor tap, mirror; the baby room: changing counter with a
        // padded top, basin, a chair
        WcPanReal(new Vector3(-46.85f,y,-30.6f),Vector3.right);
        foreach(float z in new[]{-31.05f,-30.15f})
        {
            m.Tube(stainless,new Vector3(-46.85f,y+.78f,z),new Vector3(-46.0f,y+.78f,z),.017f,12);
            m.Cylinder(stainless,new Vector3(-46.85f,y+.78f,z),.035f,.012f,14,Quaternion.FromToRotation(Vector3.up,Vector3.right),true);
        }
        RB(gStoneTop,-46.85f,-46.30f,y+.80f,y+.84f,-26.75f,-25.45f,.008f,true,2);BasinSet(new Vector3(-46.52f,y+.84f,-26.1f),Vector3.left,"WC dostępne"+lv);
        G1Mirror("WC dostępne"+lv,new Vector3(-46.85f,y+1.55f,-26.1f),Vector3.right,1.0f,.9f);
        RB(gOak,-31.0f,-30.15f,y,y+.90f,-31.6f,-29.6f,.012f,true,2);RB(gStoneTop,-31.04f,-30.15f,y+.90f,y+.94f,-31.64f,-29.56f,.015f,false,2);
        RB(leatherNavy,-30.95f,-30.20f,y+.94f,y+1.0f,-31.4f,-29.8f,.025f,false,3);       // the changing pad
        RB(gStoneTop,-30.80f,-30.15f,y+.82f,y+.86f,-27.2f,-26.0f,.008f,true,2);BasinSet(new Vector3(-30.50f,y+.86f,-26.6f),Vector3.right,"pokój matki i dziecka"+lv);
        G1Chair(new Vector3(-32.4f,y,-26.2f),Vector3.left);
    }
}
