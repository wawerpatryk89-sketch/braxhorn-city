using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// D2 station building to the board (front picture, ground floor plan, section 4.5 / 8 / 12 m):
// * MAIN HALL x -18..18, z 0..36: barrel vault (segmental, R 38.25, springing 8.0, crown 12.5) in dark graphite
//   standing-seam metal, larch slat ceiling on navy arched ribs every 6 m, glazed arch fronts to the forecourt and to the
//   passage under the tracks, the 3 m station CLOCK in the front arch (live: BraxChampionsZegar), 3 automatic double doors.
// * FOUR TRAVERTINE TOWERS at the hall's corners (x +-18..22.6, 10.2 m) with navy caps.
// * WINGS x +-22.6..40 (and behind the towers to x +-18): ground floor 4.5 + first floor to 8.0, travertine pilasters,
//   glazing in graphite frames, navy spandrel band and fascia, larch slat panels with 3D KOLEJ / AUTOBUSY + pictograms,
//   dark hip roofs (15 deg) with larch eave soffits.
// * ENTRANCE CANOPY x -20..20, z -6..0 at 4.7-5.2 m: navy fascia, larch soffit with downlights, tension rods; 3D letters
//   "DWORZEC" (1.7 m) and "KOLEJOWO-AUTOBUSOWY" (.62 m), white, backlit, on the glazing above it.
public static partial class BraxChampionsD2
{
    const float VaultR=38.25f,VaultCY=Crown-VaultR,Roof=.9f,FrontEave=-1.2f,RearEave=BD+1.2f,TowerTop=10.2f;
    static float VaultY(float x)=>VaultCY+Mathf.Sqrt(Mathf.Max(0f,VaultR*VaultR-x*x));     // outer roof line over the hall
    static float VaultIn(float x)=>VaultY(x)-Roof;                                        // the slat ceiling under it
    static Material spandrel,windowFrame,doorGlass,slatsInside;
    static void BuildingMaterials()
    {
        spandrel=navy;windowFrame=frame;doorGlass=glass;
        slatsInside=Inside(Tex("larch_ceiling_slats","teak_veneer",1.4f,new Color(1.10f,.92f,.78f)),.9f);
    }

    // ================================================================ shell
    static void Building()
    {
        Hall();
        foreach(float s in new[]{-1f,1f}){Towers(s);Wing(s);}
        Canopy();
        // the building's floor slab (.32) under everything, walkable
        Col(-BW,BW,FFL-.5f,FFL,0f,BD,"podłoga dworca");
    }
    static void Hall()
    {
        // ---- vault: outer standing-seam skin, fascia bands front/back, larch soffit over the eaves, navy ribs and slats inside
        int n=36;
        for(int i=0;i<n;i++)
        {
            float x0=-HallW+2*HallW*i/n,x1=-HallW+2*HallW*(i+1)/n,y0=VaultY(x0),y1=VaultY(x1);
            var up=new Vector3(-(y1-y0),x1-x0,0);
            Q(roofGraphite,new Vector3(x0,y0,FrontEave),new Vector3(x1,y1,FrontEave),new Vector3(x1,y1,RearEave),new Vector3(x0,y0,RearEave),up);
            // the underside of the eaves (larch) front and back, and the ceiling inside (larch slats are separate boards)
            Q(larchSoffit,new Vector3(x0,y0-Roof,FrontEave),new Vector3(x1,y1-Roof,FrontEave),new Vector3(x1,y1-Roof,0f),new Vector3(x0,y0-Roof,0f),-up);
            Q(larchSoffit,new Vector3(x0,y0-Roof,BD),new Vector3(x1,y1-Roof,BD),new Vector3(x1,y1-Roof,RearEave),new Vector3(x0,y0-Roof,RearEave),-up);
            Q(ceilWhite,new Vector3(x0,y0-Roof-.30f,0f),new Vector3(x1,y1-Roof-.30f,0f),new Vector3(x1,y1-Roof-.30f,BD),new Vector3(x0,y0-Roof-.30f,BD),-up);
            // fascia (navy) round the arch at both ends
            foreach(float z in new[]{FrontEave,RearEave})
                Q(navy,new Vector3(x0,y0-Roof,z),new Vector3(x1,y1-Roof,z),new Vector3(x1,y1+.05f,z),new Vector3(x0,y0+.05f,z),new Vector3(0,0,z<0?-1:1));
        }
        // standing seams every .6 m along z (ribs following the arch), 5 cm proud
        for(float z=FrontEave+.3f;z<RearEave-.1f;z+=.6f)
            for(int i=0;i<n;i++)
            {
                float x0=-HallW+2*HallW*i/n,x1=-HallW+2*HallW*(i+1)/n;
                Beam(roofGraphite,new Vector3(x0,VaultY(x0)+.025f,z),new Vector3(x1,VaultY(x1)+.025f,z),.03f,.05f);
            }
        // ridge skylight band along the crown (glass, navy frame), 3 m wide
        for(int i=0;i<12;i++)
        {
            float z0=1.5f+i*2.75f,z1=z0+2.2f;
            Q(glassDark,new Vector3(-1.5f,VaultY(1.5f)+.06f,z0),new Vector3(1.5f,VaultY(1.5f)+.06f,z0),new Vector3(1.5f,VaultY(1.5f)+.06f,z1),new Vector3(-1.5f,VaultY(1.5f)+.06f,z1),Vector3.up);
        }
        // inside: navy arched ribs every 6 m under the ceiling, larch slats between them (boards along z, 12 cm gaps)
        for(int r=0;r<=6;r++)
        {
            float z=r*6f;if(r==0)z=.35f;if(r==6)z=BD-.35f;
            for(int i=0;i<n;i++)
            {
                float x0=-HallW+2*HallW*i/n,x1=-HallW+2*HallW*(i+1)/n;
                Beam(steelNavy,new Vector3(x0,VaultIn(x0)-.72f,z),new Vector3(x1,VaultIn(x1)-.72f,z),.30f,.60f);
            }
        }
        for(float x=-HallW+.4f;x<HallW-.3f;x+=.30f)
        {
            float y=VaultIn(x)-.36f;var tilt=Quaternion.Euler(0,0,-Mathf.Atan2(x,VaultR*Mathf.Cos((float)Math.Asin(Mathf.Clamp(x/VaultR,-1,1))))*Mathf.Rad2Deg);
            m.Box(slatsInside,new Vector3(x,y,BD*.5f),new Vector3(.16f,.05f,BD-.2f),tilt);
        }
        // ---- the glazed arch fronts (front z 0, back z 36): navy mullions every 3 m, transoms, glass up to the soffit
        foreach(float z in new[]{0f,BD})ArchFront(z,z<1f);
    }
    static void ArchFront(float z,bool front)
    {
        float face=front?-1f:1f,gz=z;
        for(int i=0;i<12;i++)
        {
            float x0=-HallW+i*3f,x1=x0+3f,t0=VaultIn(x0)-.30f,t1=VaultIn(x1)-.30f;
            // glass panes between transoms (4.5 / 7.0 / 9.5 / 11.0), each clipped by the arch
            float[] ys={FFL,4.5f,7.0f,9.5f,11.0f,99f};
            for(int k=0;k+1<ys.Length;k++)
            {
                float ya=ys[k],yb=ys[k+1];
                if(ya>=Mathf.Max(t0,t1))break;
                bool door=front&&k==0&&x0>=-6.01f&&x1<=6.01f;          // the 4 middle bays at the doors: frames + leaves below
                bool rearDoor=!front&&k==0&&x0>=-6.01f&&x1<=6.01f;
                if(door||rearDoor){if(k==0)B(windowFrame,x0,x1,3.0f,3.15f,gz-.06f,gz+.06f);ya=3.15f;}
                var a=new Vector3(x0+.06f,ya+.06f,gz);var b=new Vector3(x1-.06f,ya+.06f,gz);
                var c=new Vector3(x1-.06f,Mathf.Min(yb,t1)-.06f,gz);var d=new Vector3(x0+.06f,Mathf.Min(yb,t0)-.06f,gz);
                Q(glassDark,a,b,c,d,new Vector3(0,0,face));Q(glassDark,a,b,c,d,new Vector3(0,0,-face));
                if(yb<Mathf.Min(t0,t1))B(windowFrame,x0,x1,yb-.06f,yb+.06f,gz-.10f,gz+.10f);                  // transom
            }
            bool doorZone=i>=5&&i<=7;
            B(windowFrame,x0-.07f,x0+.07f,doorZone?3.0f:FFL,(t0+VaultIn(x0))*.5f,gz-.14f,gz+.14f);                 // mullion
        }
        B(windowFrame,HallW-.07f,HallW+.07f,FFL,VaultIn(HallW)-.3f,gz-.14f,gz+.14f);
        // sill and the arch frame (navy) along the soffit line
        B(windowFrame,-HallW,-6f,FFL,FFL+.12f,gz-.12f,gz+.12f);B(windowFrame,6f,HallW,FFL,FFL+.12f,gz-.12f,gz+.12f);
        // the door zone x -6..6 below 3.0: three automatic double doors (x -4, 0, 4; 2.4 m) between jambs and fixed panes
        foreach(float jx in new[]{-6f,-5.2f,-2.8f,-1.2f,1.2f,2.8f,5.2f,6f})B(windowFrame,jx-.05f,jx+.05f,FFL,3.0f,gz-.10f,gz+.10f);
        foreach(var (a0,a1) in new[]{(-6f,-5.2f),(-2.8f,-1.2f),(1.2f,2.8f),(5.2f,6f)})
        {
            var p0=new Vector3(a0+.05f,FFL+.05f,gz);var p1=new Vector3(a1-.05f,FFL+.05f,gz);var p2=new Vector3(a1-.05f,2.95f,gz);var p3=new Vector3(a0+.05f,2.95f,gz);
            Q(glassDark,p0,p1,p2,p3,new Vector3(0,0,face));Q(glassDark,p0,p1,p2,p3,new Vector3(0,0,-face));
        }
        for(int i=0;i<36;i++)
        {
            float x0=-HallW+i,x1=x0+1;
            Beam(navy,new Vector3(x0,VaultIn(x0)-.18f,gz),new Vector3(x1,VaultIn(x1)-.18f,gz),.45f,.30f);
        }
        // ground-floor wall pieces are glass: colliders along the front except the doors
        if(front)
        {
            Col(-HallW,-6f,FFL,8f,gz-.15f,gz+.15f,"szyba hali");Col(6f,HallW,FFL,8f,gz-.15f,gz+.15f,"szyba hali");Col(-6f,6f,3.0f,8f,gz-.15f,gz+.15f,"nadświetle");
            for(int i=0;i<3;i++)Door("D2_main_"+(i+1),"D2 wejście główne "+(i+1),-4f+i*4f,FFL,gz,0f,2.4f,2.7f,true);
            // fixed glass beside the doors
            Col(-6f,-5.2f,FFL,3f,gz-.1f,gz+.1f,"szyba");Col(-2.8f,-1.2f,FFL,3f,gz-.1f,gz+.1f,"szyba");Col(1.2f,2.8f,FFL,3f,gz-.1f,gz+.1f,"szyba");Col(5.2f,6f,FFL,3f,gz-.1f,gz+.1f,"szyba");
        }
        else
        {
            Col(-HallW,-6f,FFL,8f,gz-.15f,gz+.15f,"szyba hali tył");Col(6f,HallW,FFL,8f,gz-.15f,gz+.15f,"szyba hali tył");Col(-6f,6f,3.0f,8f,gz-.15f,gz+.15f,"nadświetle");
            for(int i=0;i<3;i++)Door("D2_rear_"+(i+1),"D2 wyjście na perony "+(i+1),-4f+i*4f,FFL,gz,180f,2.4f,2.7f,true);
            Col(-6f,-5.2f,FFL,3f,gz-.1f,gz+.1f,"szyba");Col(-2.8f,-1.2f,FFL,3f,gz-.1f,gz+.1f,"szyba");Col(1.2f,2.8f,FFL,3f,gz-.1f,gz+.1f,"szyba");Col(5.2f,6f,FFL,3f,gz-.1f,gz+.1f,"szyba");
        }
    }
    static void Towers(float s)
    {
        foreach(var (z0,z1) in new[]{(-.8f,6.0f),(32.5f,BD+.8f)})
        {
            float x0=s<0?-Tower:HallW,x1=s<0?-HallW:Tower;
            B(travertine,x0,x1,0f,TowerTop,z0,z1,true);
            // navy cap with a drip edge, and vertical shadow grooves (dark inlays 2 cm proud... recessed: graphite strips)
            B(navy,x0-.12f,x1+.12f,TowerTop,TowerTop+.35f,z0-.12f,z1+.12f);
            // two vertical graphite inlay strips on the outer face (12 mm proud), as the board's pilaster joints
            float zfa=z0<0?z0-.012f:z1,zfb=z0<0?z0:z1+.012f;
            foreach(float f in new[]{.33f,.66f}){float xg=x0+(x1-x0)*f;B(frame,xg-.03f,xg+.03f,.6f,TowerTop-.4f,zfa,zfb);}
            // plinth band of dark granite
            B(graniteDark,x0-.03f,x1+.03f,.05f,.6f,z0-.03f,z1+.03f);
        }
    }
    // ---- wing: ground floor 4.5 + first floor to 8, pilasters, glazing, the slat panel with its sign, hip roof
    static void Wing(float s)
    {
        float xi=HallW,xo=BW;                      // inner (hall side) and outer x (magnitudes)
        float X(float v)=>s*v;
        // walls: front (z 0) from the tower to the outer corner, side (x 40), back (z 36)
        // front: slat panel x 22.6..30.6 over the ground-floor shopfront, then three glazed bays between pilasters
        float pz=0f;
        // pilasters (travertine) on the front: at 30.6, 35.3, 40 (the corner one wraps)
        foreach(float px in new[]{30.6f,35.3f,39.2f})
        {
            float a=X(px),b=X(px+.8f);B(travertine,Mathf.Min(a,b),Mathf.Max(a,b),0f,Eave,pz-.45f,pz+.2f,true);
            B(graniteDark,Mathf.Min(a,b)-.02f,Mathf.Max(a,b)+.02f,.05f,.6f,pz-.47f,pz+.18f);
        }
        // glazing in the bays (two floors) with the navy spandrel band 4.2-4.9 and the fascia 7.3-8.0
        foreach(var (g0,g1) in new[]{(22.6f,30.6f),(31.4f,35.3f),(36.1f,39.2f)})
        {
            bool panel=g0<23f;
            float a=X(g0),b=X(g1),lo=Mathf.Min(a,b),hi=Mathf.Max(a,b);
            Glazing(lo,hi,FFL,4.2f,pz,-1f,true);
            B(spandrel,lo,hi,4.2f,4.9f,pz-.15f,pz+.05f,true);
            if(panel)SlatPanel(lo,hi,4.9f,7.3f,pz-.15f);
            else Glazing(lo,hi,4.9f,7.3f,pz,-1f,false);
            B(spandrel,lo,hi,7.3f,Eave,pz-.15f,pz+.05f,true);
        }
        // the corner and the side (x = +-40): pilasters every 6 m, two floors of windows
        float sx=X(xo);
        for(int k=0;k<=6;k++)
        {
            float z=k*6f;float z0=k==0?.2f:z-.4f,z1=Mathf.Min(BD,z+.4f);
            B(travertine,Mathf.Min(sx,sx+s*.45f),Mathf.Max(sx,sx+s*.45f),0f,Eave,z0,z1,true);
            B(graniteDark,Mathf.Min(sx+s*.02f,sx+s*.47f),Mathf.Max(sx+s*.02f,sx+s*.47f),.05f,.6f,z0-.02f,z1+.02f);
            if(k==6)break;
            float a=z+.4f,b=z+6f-.4f;
            bool wc=k==0;                                           // WC in the front corner: frosted ribbon window high up
            if(wc){B(travertine,Mathf.Min(sx,sx-s*.25f),Mathf.Max(sx,sx-s*.25f),FFL,2.6f,a,b,true);GlazingZ(sx,a,b,2.6f,4.2f,s);}
            else if(k==3)
            {
                // the side door (z 20..22) with glazing either side and a transom
                GlazingZ(sx,a,20f,FFL,4.2f,s);GlazingZ(sx,22f,b,FFL,4.2f,s);GlazingZ(sx,20f,22f,2.75f,4.2f,s);
                B(windowFrame,sx-.08f,sx+.08f,2.62f,2.75f,20f,22f);
            }
            else GlazingZ(sx,a,b,FFL,4.2f,s);
            B(spandrel,Mathf.Min(sx,sx+s*.12f),Mathf.Max(sx,sx+s*.12f),4.2f,4.9f,a,b,true);
            GlazingZ(sx,a,b,4.9f,7.3f,s);
            B(spandrel,Mathf.Min(sx,sx+s*.12f),Mathf.Max(sx,sx+s*.12f),7.3f,Eave,a,b,true);
        }
        // back (z 36): to the arcade - glazing with doors in the middle bay, pilasters
        foreach(float px in new[]{22.6f,30.6f,35.3f,39.2f})
        {
            float a=X(px),b=X(px+.8f);B(travertine,Mathf.Min(a,b),Mathf.Max(a,b),0f,Eave,BD-.2f,BD+.45f,true);
        }
        foreach(var (g0,g1) in new[]{(23.4f,30.6f),(31.4f,35.3f),(36.1f,39.2f)})
        {
            float a=X(g0),b=X(g1),lo=Mathf.Min(a,b),hi=Mathf.Max(a,b);
            if(g0<24f)
            {
                // the back door (x 26..28) in the first bay: glazing either side, a transom over it
                float d0=Mathf.Min(X(26f),X(28f)),d1=Mathf.Max(X(26f),X(28f));
                Glazing(lo,d0,FFL,4.2f,BD,1f,true);Glazing(d1,hi,FFL,4.2f,BD,1f,true);
                var t0=new Vector3(d0+.05f,2.75f,BD);var t1=new Vector3(d1-.05f,2.75f,BD);var t2=new Vector3(d1-.05f,4.15f,BD);var t3=new Vector3(d0+.05f,4.15f,BD);
                Q(glassDark,t0,t1,t2,t3,Vector3.forward);Q(glassDark,t0,t1,t2,t3,Vector3.back);
                B(windowFrame,d0,d1,2.62f,2.75f,BD-.08f,BD+.08f);Col(d0,d1,2.62f,4.2f,BD-.1f,BD+.1f,"nadświetle");
            }
            else Glazing(lo,hi,FFL,4.2f,BD,1f,true);
            B(spandrel,lo,hi,4.2f,4.9f,BD-.05f,BD+.15f,true);
            Glazing(lo,hi,4.9f,7.3f,BD,1f,false);
            B(spandrel,lo,hi,7.3f,Eave,BD-.05f,BD+.15f,true);
        }
        Door(s<0?"D2_wing_w_rear":"D2_wing_e_rear",s<0?"D2 skrzydło zachodnie tył":"D2 skrzydło wschodnie tył",X(27f),FFL,BD,180f,2.0f,2.6f,true);
        Door(s<0?"D2_wing_w_side":"D2_wing_e_side",s<0?"D2 skrzydło zachodnie bok":"D2 skrzydło wschodnie bok",sx,FFL,21f,s<0?-90f:90f,2.0f,2.6f,true);
        // the wing between the tower and the hall wall behind it (x 18..22.6, z 6..30): the hall's side wall up to the roof
        // hip roof: x 18 (inner eave at 8.0) .. 40.8 (outer eave overhang .8), z -0.8 .. 36.8, 15 deg
        HipRoof(s,HallW,BW+.8f,-.8f,BD+.8f,Eave);
    }
    // a glazed band between x lo..hi along a facade at z (n = outward z sign), graphite mullions every ~1.6 m
    static void Glazing(float lo,float hi,float y0,float y1,float z,float n,bool ground)
    {
        int bays=Mathf.Max(1,Mathf.RoundToInt((hi-lo)/1.6f));float w=(hi-lo)/bays;
        for(int i=0;i<bays;i++)
        {
            float a=lo+i*w,b=a+w;
            var p0=new Vector3(a+.05f,y0+.05f,z);var p1=new Vector3(b-.05f,y0+.05f,z);var p2=new Vector3(b-.05f,y1-.05f,z);var p3=new Vector3(a+.05f,y1-.05f,z);
            Q(glassDark,p0,p1,p2,p3,new Vector3(0,0,n));Q(glassDark,p0,p1,p2,p3,new Vector3(0,0,-n));
            B(windowFrame,a-.04f,a+.04f,y0,y1,z-.08f,z+.08f);
        }
        B(windowFrame,hi-.04f,hi+.04f,y0,y1,z-.08f,z+.08f);
        B(windowFrame,lo,hi,y0,y0+.06f,z-.10f,z+.10f);B(windowFrame,lo,hi,y1-.06f,y1,z-.10f,z+.10f);
        if(ground)B(windowFrame,lo,hi,2.75f,2.85f,z-.08f,z+.08f);                                 // transom over the doors' height
        Col(lo,hi,y0,y1,z-.1f,z+.1f,"szyba");
    }
    static void GlazingZ(float x,float a,float b,float y0,float y1,float s)
    {
        int bays=Mathf.Max(1,Mathf.RoundToInt((b-a)/1.7f));float w=(b-a)/bays;
        for(int i=0;i<bays;i++)
        {
            float z0=a+i*w,z1=z0+w;
            var p0=new Vector3(x,y0+.05f,z0+.05f);var p1=new Vector3(x,y0+.05f,z1-.05f);var p2=new Vector3(x,y1-.05f,z1-.05f);var p3=new Vector3(x,y1-.05f,z0+.05f);
            Q(glassDark,p0,p1,p2,p3,new Vector3(s,0,0));Q(glassDark,p0,p1,p2,p3,new Vector3(-s,0,0));
            B(windowFrame,x-.08f,x+.08f,y0,y1,z0-.04f,z0+.04f);
        }
        B(windowFrame,x-.08f,x+.08f,y0,y1,b-.04f,b+.04f);
        B(windowFrame,x-.10f,x+.10f,y0,y0+.06f,a,b);B(windowFrame,x-.10f,x+.10f,y1-.06f,y1,a,b);
        Col(x-.1f,x+.1f,y0,y1,a,b,"szyba");
    }
    // vertical larch slats (6 cm wide, 9 cm deep, 4 cm gaps) on a navy backing, framed
    static void SlatPanel(float lo,float hi,float y0,float y1,float z)
    {
        B(navy,lo,hi,y0,y1,z+.02f,z+.12f,true);
        for(float x=lo+.08f;x<hi-.07f;x+=.10f)B(larch,x,x+.06f,y0+.08f,y1-.08f,z-.09f,z+.02f);
        B(navy,lo,hi,y0,y0+.08f,z-.12f,z+.12f);B(navy,lo,hi,y1-.08f,y1,z-.12f,z+.12f);
    }
    // hip roof over x a..b (signed by s), z c..d; eave height e, 15 deg; standing seams, larch soffit under the overhang
    static void HipRoof(float s,float a,float b,float c,float d,float e)
    {
        float w=b-a,h=w*.5f*Mathf.Tan(15f*Mathf.Deg2Rad),xr=(a+b)*.5f,zr0=c+w*.5f,zr1=d-w*.5f,top=e+h;
        Vector3 P(float x,float y,float z)=>new Vector3(s*x,y,z);
        var e00=P(a,e,c);var e10=P(b,e,c);var e11=P(b,e,d);var e01=P(a,e,d);var r0=P(xr,top,zr0);var r1=P(xr,top,zr1);
        // outer slope (x = b side), inner slope (x = a side), front hip triangle, back hip triangle
        Q(roofGraphite,e10,e11,r1,r0,P(1,1,0)-P(0,0,0));
        Q(roofGraphite,e00,r0,r1,e01,P(-1,1,0)-P(0,0,0));
        T(roofGraphite,e00,e10,r0,new Vector3(0,1,-1));
        T(roofGraphite,e01,r1,e11,new Vector3(0,1,1));
        // ridge and hip cappings (graphite)
        Beam(frame,r0+Vector3.up*.04f,r1+Vector3.up*.04f,.18f,.08f);
        foreach(var (p,q) in new[]{(e00,r0),(e10,r0),(e01,r1),(e11,r1)})Beam(frame,p+Vector3.up*.04f,q+Vector3.up*.04f,.16f,.07f);
        // standing seams on the long slopes every .6 m (down the slope, x-direction)
        for(float z=zr0;z<=zr1+.01f;z+=.6f)
        {
            Beam(roofGraphite,P(b,e+.03f,z),P(xr,top+.03f,z),.03f,.05f);
            if(s*1f!=0f)Beam(roofGraphite,P(a,e+.03f,z),P(xr,top+.03f,z),.03f,.05f);
        }
        // eave fascia (navy) and larch soffit under the overhangs: outer (x 40..40.8), front (z -.8..0), back (z 36..36.8)
        float lo=Mathf.Min(s*a,s*b),hi=Mathf.Max(s*a,s*b),wl=Mathf.Min(s*(b-.8f),s*b),wh=Mathf.Max(s*(b-.8f),s*b);
        B(navy,Mathf.Min(s*(b-.12f),s*b),Mathf.Max(s*(b-.12f),s*b),e-.35f,e,c,d);
        float to=Mathf.Min(s*Tower,s*b),th=Mathf.Max(s*Tower,s*b);                                   // front / back edges from the towers out
        B(navy,to,th,e-.35f,e,c,c+.12f);B(navy,to,th,e-.35f,e,d-.12f,d);
        float sl=Mathf.Min(s*(b-.12f),s*(b-.8f)),sh=Mathf.Max(s*(b-.12f),s*(b-.8f));
        Flip(larchSoffit,sl,sh,c+.12f,d-.12f,e-.35f);
        float fl=Mathf.Min(s*Tower,s*(b-.8f)),fh=Mathf.Max(s*Tower,s*(b-.8f));
        Flip(larchSoffit,fl,fh,c+.12f,0f,e-.35f);Flip(larchSoffit,fl,fh,BD,d-.12f,e-.35f);
        // the attic: the roof is closed above the first-floor ceiling (8.0) - the wing's ceiling slab
        B(concrete,Mathf.Min(s*(a+.06f),s*(b-.8f)),Mathf.Max(s*(a+.06f),s*(b-.8f)),e-.35f,e-.05f,.06f,BD-.06f);
    }
    static void Flip(Material mat,float x0,float x1,float z0,float z1,float y)
    {
        if(x1-x0<1e-4f||z1-z0<1e-4f)return;
        Q(mat,new Vector3(x0,y,z0),new Vector3(x1,y,z0),new Vector3(x1,y,z1),new Vector3(x0,y,z1),Vector3.down);
    }
    // ---- entrance canopy (cantilever from the hall front, tension rods to the arch frame)
    static void Canopy()
    {
        float x0=-20f,x1=20f,z0=-6f,y0=4.7f,y1=5.2f;
        B(navy,x0,x1,y0,y1,z0,z0+.18f,true);B(navy,x0,x0+.18f,y0,y1,z0,0f);B(navy,x1-.18f,x1,y0,y1,z0,0f);    // fascia
        B(roofGraphite,x0+.18f,x1-.18f,y1-.10f,y1-.02f,z0+.18f,-.02f);                                       // membrane top
        Q(larchSoffit,new Vector3(x0+.18f,y0+.02f,z0+.18f),new Vector3(x1-.18f,y0+.02f,z0+.18f),new Vector3(x1-.18f,y0+.02f,0f),new Vector3(x0+.18f,y0+.02f,0f),Vector3.down);
        for(float x=x0+1.5f;x<x1-1f;x+=3f)for(float z=z0+1.2f;z<-.5f;z+=2.0f)m.Cylinder(ledWarm,new Vector3(x,y0+.008f,z),.09f,.012f,12,Quaternion.identity,false,true);
        Col(x0,x1,y0,y1,z0,0f,"zadaszenie");
        foreach(float x in new[]{-15f,-6f,6f,15f})Beam(steel,new Vector3(x,y1,z0+.3f),new Vector3(x,8.2f,0f),.05f,.05f);
    }

    // ================================================================ 3D letters and pictograms
    static void BuildingSigns()
    {
        var lm=new BraxChampionsMeshBatch();
        // DWORZEC (1.7 m) and KOLEJOWO-AUTOBUSOWY (.62 m) on a slim navy rail in front of the glazing, above the canopy
        BraxChampionsLetters3D.Text(lm,whiteLetters,navy,"DWORZEC",new Vector3(0,6.20f,-.38f),180f,1.5f,.22f,.10f,26f);
        BraxChampionsLetters3D.Text(lm,whiteLetters,navy,"KOLEJOWO-AUTOBUSOWY",new Vector3(0,5.42f,-.38f),180f,.62f,.14f,.10f,26f);
        B(frame,-14f,14f,6.05f,6.15f,-.30f,-.06f);B(frame,-14f,14f,5.30f,5.38f,-.30f,-.06f);Signs+=2;
        // KOLEJ / AUTOBUSY on the wing slat panels (x 22.6..30.6), pictograms above the words
        foreach(float s in new[]{-1f,1f})
        {
            float cx=s*26.6f;
            BraxChampionsLetters3D.Text(lm,whiteLetters,navy,s<0?"KOLEJ":"AUTOBUSY",new Vector3(cx,5.25f,-.26f),180f,.62f,.10f,.10f,7.2f);
            if(s<0)TrainPictogram(lm,cx,6.35f,-.26f);else BusPictogram(lm,cx,6.35f,-.26f);
            Signs+=2;
        }
        BraxChampionsLetters3D.Build(lm,root,"D2 napisy 3D");
    }
    // front view of a train: rounded body, dark windscreen, two lamps, buffer beam, rails - white, 1.0 m tall
    static void TrainPictogram(BraxChampionsMeshBatch lm,float cx,float y,float z)
    {
        float d=.10f;
        lm.RoundedBoxMinMax(whiteLetters,cx-.42f,cx+.42f,y,y+.92f,z-d,z,.16f,4);
        lm.RoundedBoxMinMax(navy,cx-.32f,cx+.32f,y+.50f,y+.80f,z-d-.012f,z-d+.02f,.06f,3);
        foreach(float s in new[]{-1f,1f})lm.Cylinder(navy,new Vector3(cx+s*.24f,y+.22f,z-d-.012f),.07f,.02f,16,Quaternion.Euler(-90,0,0),true,true);
        lm.BoxMinMax(navy,cx-.30f,cx+.30f,y+.06f,y+.10f,z-d-.012f,z-d+.02f);
        foreach(float s in new[]{-1f,1f})lm.BoxMinMax(whiteLetters,cx+s*.30f-.04f,cx+s*.30f+.04f,y-.22f,y,z-d,z);
        lm.BoxMinMax(whiteLetters,cx-.5f,cx+.5f,y-.26f,y-.20f,z-d,z);
    }
    static void BusPictogram(BraxChampionsMeshBatch lm,float cx,float y,float z)
    {
        float d=.10f;
        lm.RoundedBoxMinMax(whiteLetters,cx-.45f,cx+.45f,y-.05f,y+.90f,z-d,z,.10f,4);
        lm.RoundedBoxMinMax(navy,cx-.36f,cx+.36f,y+.40f,y+.76f,z-d-.012f,z-d+.02f,.05f,3);
        foreach(float s in new[]{-1f,1f}){lm.Cylinder(navy,new Vector3(cx+s*.27f,y+.13f,z-d-.012f),.06f,.02f,16,Quaternion.Euler(-90,0,0),true,true);
            lm.BoxMinMax(whiteLetters,cx+s*.33f-.08f,cx+s*.33f+.08f,y-.22f,y-.05f,z-d,z);
            lm.BoxMinMax(whiteLetters,cx+s*.52f-.03f,cx+s*.52f+.03f,y+.55f,y+.80f,z-d,z);}                      // mirrors
        lm.BoxMinMax(navy,cx-.2f,cx+.2f,y+.80f,y+.86f,z-d-.012f,z-d+.02f);                                     // route display
    }

    // ================================================================ the clock (live: BraxChampionsZegar)
    static void Clock()
    {
        float cy=9.6f,z=-.42f,r=1.5f;
        var g=Group(root,"D2 zegar dworcowy (BraxChampionsZegar, czas z telefonu)");g.localPosition=new Vector3(0,cy,z)*K;
        var cm=new BraxChampionsMeshBatch();
        var rot=Quaternion.Euler(-90,0,0);
        cm.Cylinder(navy,new Vector3(0,0,.10f),r+.16f,.22f,64,rot,true,true);                 // case (navy), 22 cm deep
        cm.Cylinder(clockFace,new Vector3(0,0,-.12f),r,.012f,64,rot,true,false);           // face 12 mm proud of the case
        cm.Cylinder(stainless,new Vector3(0,0,-.124f),r+.16f,.03f,64,rot,false,false,r+.16f);
        for(int i=0;i<12;i++)
        {
            float a=i*30f*Mathf.Deg2Rad,l=i%3==0?.30f:.18f,w=i%3==0?.09f:.05f;
            var c=new Vector3(Mathf.Sin(a)*(r-.14f-l*.5f),Mathf.Cos(a)*(r-.14f-l*.5f),-.142f);
            cm.Box(black,c,new Vector3(w,l,.02f),Quaternion.Euler(0,0,-i*30f));
        }
        for(int i=0;i<60;i++){if(i%5==0)continue;float a=i*6f*Mathf.Deg2Rad;cm.Box(black,new Vector3(Mathf.Sin(a)*(r-.17f),Mathf.Cos(a)*(r-.17f),-.138f),new Vector3(.025f,.07f,.012f),Quaternion.Euler(0,0,-i*6f));}
        cm.Build(g,"D2 zegar tarcza",Folder,false);
        Transform Hand(string name,float len,float w,float zz,Material mat)
        {
            var h=Group(g,name);var hm=new BraxChampionsMeshBatch();
            hm.Box(mat,new Vector3(0,len*.5f-.12f,zz),new Vector3(w,len,.015f),Quaternion.identity);
            hm.Build(h,name,Folder,false);
            foreach(var t in h.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
            return h;
        }
        var hh=Hand("wskazówka godzinowa",.95f,.10f,-.175f,black);var mm=Hand("wskazówka minutowa",1.32f,.07f,-.20f,black);var ss=Hand("wskazówka sekundowa",1.30f,.025f,-.225f,bufferRed);
        var zc=g.gameObject.AddComponent<BraxChampionsZegar>();zc.godzinowa=hh;zc.minutowa=mm;zc.sekundowa=ss;
        var cap=new BraxChampionsMeshBatch();cap.Cylinder(black,new Vector3(0,0,-.2325f),.07f,.02f,24,rot,true,false);cap.Build(g,"D2 zegar oś",Folder,false);
    }

    // ================================================================ doors (BraxCityDoor, glass leaves, open on approach)
    static void Door(string id,string label,float x,float y,float z,float yaw,float width,float height,bool glassLeaf)
    {
        var group=Group(root,label+" | door (opens on approach)");
        group.localPosition=new Vector3(x,y,z)*K;group.localRotation=Quaternion.Euler(0,yaw,0);
        int n=width>1.5f?2:1;var leaves=new Transform[n];var sides=new float[n];
        for(int i=0;i<n;i++)
        {
            float side=n==1?-1f:(i==0?-1f:1f),lw=width/n;
            var hinge=Group(group,"door leaf "+(i+1));hinge.localPosition=new Vector3(side*width*.5f,0,0)*K;
            float a0=Mathf.Min(0f,-side*lw)+.01f,a1=Mathf.Max(0f,-side*lw)-.01f;
            var lm=new BraxChampionsMeshBatch();
            if(glassLeaf)
            {
                lm.BoxMinMax(frame,a0,a1,height-.12f,height-.02f,-.04f,.04f);lm.BoxMinMax(frame,a0,a1,.02f,.20f,-.04f,.04f);
                lm.BoxMinMax(frame,a0,a0+.08f,.20f,height-.12f,-.04f,.04f);lm.BoxMinMax(frame,a1-.08f,a1,.20f,height-.12f,-.04f,.04f);
                lm.BoxMinMax(glass,a0+.08f,a1-.08f,.20f,height-.12f,-.01f,.01f);
                float px=side<0?a1-.25f:a0+.25f;
                foreach(float f in new[]{-1f,1f})lm.BoxMinMax(stainless,px-.025f,px+.025f,.80f,1.90f,f>0?.04f:-.10f,f>0?.10f:-.04f);
            }
            else
            {
                lm.BoxMinMax(plaster,a0,a1,.02f,height-.02f,-.025f,.025f);
                float px=side<0?a1-.12f:a0+.12f;
                foreach(float f in new[]{-1f,1f})lm.BoxMinMax(stainless,px-.08f,px+.08f,1.02f,1.06f,f>0?.025f:-.075f,f>0?.075f:-.025f);
            }
            lm.Build(hinge,label+" leaf "+(i+1),Folder,false);
            BraxChampionsMeshBatch.Collider(hinge,"door leaf collider",new Vector3((a0+a1)*.5f,height*.5f,0),new Vector3(a1-a0,height,.08f));
            foreach(var t in hinge.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
            leaves[i]=hinge;sides[i]=n==1?1f:(i==0?1f:-1f);
        }
        var d=group.gameObject.AddComponent<BraxCityDoor>();d.id=id;d.label=label;d.leaves=leaves;d.sides=sides;
        Doors++;
    }
    // ================================================================ interior (board ground floor plan)
    // north row (z 24..36): TECH x -40..-33 | KASY KOLEJOWE x -33..-6 | passage to the platforms x -6..6 | KASY AUTOBUSOWE
    // x 6..33 | MAGAZYN x 33..40; west / east: POCZEKALNIA KOLEJ / AUTOBUS x +-18..40, z 10..24; front corners: WC M/K/♿
    // x +-30..40, z 0..10; kiosks x +-22.6..30, z 0..10; the MAIN HALL x -18..18, z 0..24 with the info desk at the entrance.
    // +1 (4.9) in the wings: galleries over the hall (food court, shops, offices), glass balustrades.
    static Material counterTop,wallPanel,benchWood;
    static void Interior()
    {
        counterTop=Inside(BraxChampionsFoundation.LibraryMarbleWhite("d2_counter_white_marble",new Color(.96f,.96f,.97f),2.0f),.85f);
        wallPanel=Inside(Tex("hall_wall_larch","teak_veneer",1.4f,new Color(1.05f,.88f,.74f)),.85f);
        benchWood=Inside(Tex("bench_oak","oak_wood_planks",1.0f,new Color(.92f,.86f,.82f)),.85f);
        // floor: granite tiles everywhere, a darker granite band at the entrance mats and round the info desk
        Flat(hallFloor,-BW,BW,0f,BD,FFL);
        Flat(graniteDark,-6f,6f,.3f,3.0f,FFL+.012f);Flat(graniteDark,-6f,6f,BD-3.0f,BD-.3f,FFL+.012f);
        // first floor slabs in the wings (4.5..4.9), ceilings under them (white), gallery edge (navy band) + glass balustrade
        foreach(float s in new[]{-1f,1f})
        {
            float a=s*HallW,b=s*BW,lo=Mathf.Min(a,b),hi=Mathf.Max(a,b);
            float li=s<0?lo:lo+.06f,hj=s<0?hi-.06f:hi;
            B(concrete,li,hj,Floor1,Floor1+.35f,.06f,BD-.06f,true);
            Flat(hallFloor,li,hj,.06f,BD-.06f,Floor1+.40f);
            Q(ceilWhite,new Vector3(lo,Floor1-.01f,0f),new Vector3(hi,Floor1-.01f,0f),new Vector3(hi,Floor1-.01f,BD),new Vector3(lo,Floor1-.01f,BD),Vector3.down);
            B(navy,Mathf.Min(a,a-s*.06f),Mathf.Max(a,a-s*.06f),Floor1-.10f,Floor1+.40f,6.0f,32.5f);
            Balustrade(a,6.0f,32.5f,Floor1+.40f,s);
            // hall side wall below the gallery: larch panels with the shopfronts of the waiting room and the ticket hall
            float wx=a+s*.10f;
            B(wallPanel,Mathf.Min(a,wx),Mathf.Max(a,wx),FFL,Floor1-.1f,6.0f,10.0f,true);
            ShopFront(a,10f,24f,s,s<0?"POCZEKALNIA KOLEJ":"POCZEKALNIA AUTOBUS");
            B(wallPanel,Mathf.Min(a,wx),Mathf.Max(a,wx),FFL,Floor1-.1f,24.0f,26.0f,true);
            Rooms(s);
        }
        InfoDesk();
        HallBenches();
        TicketCounters();
        DepartureBoards();
    }
    static void Balustrade(float x,float z0,float z1,float y,float s)
    {
        var p0=new Vector3(x,y,z0);var p1=new Vector3(x,y,z1);var p2=new Vector3(x,y+1.05f,z1);var p3=new Vector3(x,y+1.05f,z0);
        Q(glass,p0,p1,p2,p3,new Vector3(-s,0,0));Q(glass,p0,p1,p2,p3,new Vector3(s,0,0));
        B(stainless,x-.03f,x+.03f,y+1.05f,y+1.11f,z0,z1);
        Col(x-.05f,x+.05f,y,y+1.1f,z0,z1,"balustrada");
    }
    // glass shopfront in the hall's side wall (x = a) between z0..z1, a double door in the middle, the room's name above
    static void ShopFront(float a,float z0,float z1,float s,string name)
    {
        float dz=(z0+z1)*.5f;
        GlazingZ(a,z0,dz-1.2f,FFL,3.2f,s);GlazingZ(a,dz+1.2f,z1,FFL,3.2f,s);
        B(wallPanel,Mathf.Min(a,a+s*.10f),Mathf.Max(a,a+s*.10f),3.2f,Floor1-.1f,z0,z1,true);
        B(windowFrame,a-.08f,a+.08f,2.62f,2.75f,dz-1.2f,dz+1.2f);
        GlazingZ(a,dz-1.2f,dz+1.2f,2.75f,3.2f,s);
        Door("D2_"+name.Replace(" ","_"),"D2 "+name,a,FFL,dz,s<0?-90f:90f,2.4f,2.6f,true);
        var lm=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(lm,whiteLetters,navy,name,new Vector3(a-s*.02f,3.55f,dz),s<0?90f:-90f,.34f,.05f,.10f,(z1-z0)-1f);
        BraxChampionsLetters3D.Build(lm,root,"D2 napis "+name);Signs++;
    }
    // partitions of the wing (plastered, 12 cm), doors, WC fit-out, waiting-room benches
    static void Rooms(float s)
    {
        float X(float v)=>s*v;
        void WallX(float z,float xa,float xb,params float[] doors)
        {
            float lo=Mathf.Min(X(xa),X(xb)),hi=Mathf.Max(X(xa),X(xb)),x=lo;
            var ds=new List<float>(doors);ds.Sort((p,q)=>X(p).CompareTo(X(q)));
            foreach(float dc in ds){float c=X(dc);B(plaster,x,c-.5f,FFL,Floor1,z-.06f,z+.06f,true);B(plaster,c-.5f,c+.5f,2.2f,Floor1,z-.06f,z+.06f,true);x=c+.5f;}
            B(plaster,x,hi,FFL,Floor1,z-.06f,z+.06f,true);
        }
        void WallZ(float x,float za,float zb,params float[] doors)
        {
            float xx=X(x),z=za;
            foreach(float dc in doors){B(plaster,xx-.06f,xx+.06f,FFL,Floor1,z,dc-.5f,true);B(plaster,xx-.06f,xx+.06f,2.2f,Floor1,dc-.5f,dc+.5f,true);z=dc+.5f;}
            B(plaster,xx-.06f,xx+.06f,FFL,Floor1,z,zb,true);
        }
        // the waiting room: z 10 (to the WC / kiosk) and z 24 (to the ticket hall / tech room) walls
        WallX(10f,HallW+.1f,BW-.1f,26f,35f);
        WallX(24f,33f,BW-.1f,36.5f);
        // the WC block x 30..40, z 0..10: men / women / accessible, the kiosk x 22.6..30 in front of the waiting room
        WallZ(30f,.2f,10f,5f);
        WallX(5.5f,30f,BW-.1f);
        WallZ(36.5f,5.5f,10f,7.8f);
        // tech room / store x 33..40, z 24..36 (closed, a door from the ticket hall)
        WallZ(33f,24f,BD-.1f,28f);
        // WC fittings: basins on a marble counter, cubicles (doors), the accessible WC with grab rails
        float cx=X(35f);
        B(counterTop,Mathf.Min(X(30.3f),X(34.5f)),Mathf.Max(X(30.3f),X(34.5f)),.82f,.88f,.25f,.85f,true);
        for(int i=0;i<3;i++){float bx=X(31.2f+i*1.3f);m.Cylinder(porcelainMat(),new Vector3(bx,.70f,.55f),.20f,.18f,20,Quaternion.identity,false,false,.24f);}
        for(int i=0;i<3;i++)
        {
            float z0=1.2f+i*1.45f;
            B(plaster,Mathf.Min(X(37.5f),X(37.55f)),Mathf.Max(X(37.5f),X(37.55f)),FFL+.15f,2.2f,z0,z0+1.4f);
            B(plaster,Mathf.Min(X(37.5f),X(39.9f)),Mathf.Max(X(37.5f),X(39.9f)),FFL+.15f,2.2f,z0+1.40f,z0+1.45f);
            m.Cylinder(porcelainMat(),new Vector3(X(39.4f),FFL,z0+.7f),.20f,.42f,18,Quaternion.identity,true);
        }
        // waiting room: 4 rows of benches facing the departure board on the z 24 wall
        for(int r=0;r<4;r++)for(int k=0;k<3;k++)
        {
            float bz=13.0f+r*2.6f,bx0=X(21.5f+k*5.5f);
            WaitingBench(bx0,bz,s);
        }
        // the departure board for this waiting room (live screen added by DepartureBoards)
    }
    static Material porcelain;
    static Material porcelainMat(){if(!porcelain)porcelain=Inside(S("porcelain_white",new Color(.95f,.95f,.94f),.12f,0f),.85f);return porcelain;}
    // a 4-seat station bench: oak seats and backs on a graphite steel beam, armrests between seats; every seat is a BraxSeat
    static void WaitingBench(float cx,float z,float s)
    {
        float y=FFL;
        B(frame,cx-1.95f,cx+1.95f,y+.36f,y+.42f,z-.05f,z+.05f);
        foreach(float lx in new[]{-1.6f,1.6f}){B(frame,cx+lx-.04f,cx+lx+.04f,y,y+.42f,z-.25f,z+.25f);B(frame,cx+lx-.15f,cx+lx+.15f,y,y+.02f,z-.30f,z+.30f);}
        for(int i=0;i<4;i++)
        {
            float x0=cx-1.9f+i*.95f;
            RB(benchWood,x0+.03f,x0+.92f,y+.42f,y+.47f,z-.25f,z+.20f,.02f);
            RB(benchWood,x0+.03f,x0+.92f,y+.52f,y+.90f,z+.20f,z+.25f,.02f);
            Seat(x0+.475f,y+.47f,z-.02f,Vector3.back,"ławka poczekalnia "+cx.ToString("0")+"/"+z.ToString("0")+"/"+i);
            if(i>0)B(frame,x0-.02f,x0+.02f,y+.47f,y+.68f,z-.20f,z+.15f);
        }
        Col(cx-1.95f,cx+1.95f,y,y+.9f,z-.3f,z+.3f,"ławka");Benches++;
    }
    // round info desk (D 5 m) with a white marble top, larch front, "i" in 3D above it
    static void InfoDesk()
    {
        var c=new Vector3(0,FFL,6.5f);
        m.Lathe(wallPanel,c,new[]{new Vector2(2.5f,0f),new Vector2(2.5f,1.02f),new Vector2(1.7f,1.02f),new Vector2(1.7f,0f)},48,Quaternion.identity,false,false);
        m.Lathe(counterTop,c+Vector3.up*1.02f,new[]{new Vector2(2.62f,0f),new Vector2(2.62f,.06f),new Vector2(1.6f,.06f),new Vector2(1.6f,0f)},48,Quaternion.identity,false,false);
        Col(-2.5f,2.5f,FFL,FFL+1.08f,4.0f,9.0f,"informacja");
        // the "i" pylon on the desk: navy disc with a white i, 2.5 m up
        m.Cylinder(steel,c+Vector3.up*1.08f,.05f,1.9f,12,Quaternion.identity,true);
        m.Cylinder(signBlue,c+new Vector3(0,3.2f,0),.55f,.08f,40,Quaternion.Euler(-90,0,0),true,true);
        var lm=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(lm,whiteLetters,navy,"I",c+new Vector3(0,2.95f,-.09f),180f,.50f,.03f);
        BraxChampionsLetters3D.Text(lm,whiteLetters,navy,"I",c+new Vector3(0,2.95f,.09f),0f,.50f,.03f);
        BraxChampionsLetters3D.Text(lm,whiteLetters,navy,"INFORMACJA",c+new Vector3(0,1.25f,-2.65f),180f,.18f,.02f);
        BraxChampionsLetters3D.Build(lm,root,"D2 informacja");Signs++;
    }
    // benches in the hall: two blocks either side of the central aisle (board plan), facing the entrance
    static void HallBenches()
    {
        foreach(float s in new[]{-1f,1f})for(int r=0;r<4;r++){float z=11.5f+r*2.8f;WaitingBench(s*7.5f,z,s);WaitingBench(s*12.5f,z,s);}
    }
    // ticket counters along the north wall of the ticket halls: white marble, glass screens, a 3D name over them
    static void TicketCounters()
    {
        foreach(float s in new[]{-1f,1f})
        {
            float xa=s*8f,xb=s*31f,lo=Mathf.Min(xa,xb),hi=Mathf.Max(xa,xb);
            B(wallPanel,lo,hi,FFL,1.05f,33.4f,34.2f,true);
            B(counterTop,lo,hi,1.05f,1.11f,33.3f,34.3f);
            int n=Mathf.FloorToInt((hi-lo)/2.3f);
            for(int i=0;i<=n;i++){float x=lo+i*(hi-lo)/n;B(frame,x-.03f,x+.03f,1.11f,2.4f,33.7f,33.8f);}
            var p0=new Vector3(lo,1.11f,33.75f);var p1=new Vector3(hi,1.11f,33.75f);var p2=new Vector3(hi,2.4f,33.75f);var p3=new Vector3(lo,2.4f,33.75f);
            Q(glass,p0,p1,p2,p3,Vector3.back);Q(glass,p0,p1,p2,p3,Vector3.forward);
            B(navy,lo,hi,2.4f,3.3f,33.6f,33.9f);
            B(plaster,lo,hi,FFL,Floor1,BD-.3f,BD-.18f);
            var lm=new BraxChampionsMeshBatch();
            BraxChampionsLetters3D.Text(lm,whiteLetters,navy,s<0?"KASY KOLEJOWE":"KASY AUTOBUSOWE",new Vector3((lo+hi)*.5f,2.62f,33.58f),180f,.42f,.05f,.10f,hi-lo-1f);
            BraxChampionsLetters3D.Build(lm,root,"D2 napis kasy");Signs++;
            for(int i=0;i<n;i++){float x=lo+(i+.5f)*(hi-lo)/n;BraxChampionsLetters3D.Text(m,whiteLetters,navy,(i+1).ToString(),new Vector3(x,2.05f,33.70f),180f,.16f,.01f);}
        }
    }
    static void DepartureBoards(){}
}
