using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// D2 railway (owner 11.11 13:08-13:09: tracks out of the city, "może górą ten pociąg ... wiadukt", the train disappears):
// * STATION DECK x -130..136 (local), z 38.6..64.4: concrete slab 5.25..5.85 on transverse frames of three round columns
//   every 13.3 m (x +-6.65 + 13.3 n) - the paved arcade under it is the passage from the hall to the platforms.
// * PLATFORMS P1 / P2 (side platforms, 260 m, +7.26 = .76 over the rail head, edges 1.725 m from the track centres):
//   granite coping, white line, yellow tactile strip, light granite paving, glass balustrades on the outer edges, gates
//   at the ends; three accesses (x -95, 0, 95): stairs 2.6 m in two flights + a glass lift each; canopies on navy columns
//   (12 m) with larch soffits, LED lines, the platform numbers and the station name.
// * TRACK: ballast bed (1:1.5 shoulders), B03 concrete sleepers every .6 m, UIC60 rails (foot / web / head), friction
//   buffer stops at the east end; catenary: contact wire 5.3 m over the rail head on cross-span beams / portals.
// * BRIDGE over road X70 (local x -186..-142 = world 49..93): steel tied arch (navy), rise 9 m, hangers every 3.67 m,
//   wind bracing; deck soffit 5.25 over the road - the street is untouched.
// * WEST: box-girder viaduct to world x -60, abutment, embankment, the cutting into the hill (BraxChampionsHorizon.RailHill
//   builds its land) and the TUNNEL PORTAL at world x -170; the tube runs 140 m into the hill, dark, closed at the end.
public static partial class BraxChampionsD2
{
    // world nominal <-> local
    static float LX(float wx)=>wx-CentreX;
    public static readonly float[] Accesses={-95f,0f,95f};
    const float StairW=2.6f,StairRun=.30f,LiftW=2.3f;
    const int StairRisers=20;
    // band of the stair opening on each platform (1 m from the outer edge), z0..z1
    static (float z0,float z1) Band(int p)=>p==0?(P1Z0+1.0f,P1Z0+1.0f+StairW):(P2Z1-1.0f-StairW,P2Z1-1.0f);
    // the opening in the deck / platform for the access at xa: from where the stair is 2.1 m under the soffit to the top,
    // then the lift shaft beyond the top of the stair
    static (float x0,float x1) Opening(float xa)=>(xa-2.4f,xa+7.0f+.6f+LiftW+.3f);
    static float CanopyColumnZ(int p)=>p==0?P1Z0+4.9f:P2Z1-4.9f;

    // ================================================================ the hill (shared with BraxChampionsHorizon.RailHill)
    static float Smooth(float a,float b,float v){float t=Mathf.Clamp01((v-a)/(b-a));return t*t*(3-2*t);}
    // normalised elliptic radius of the hill (a ridge running north-south across the line: 250 x 400 m half-axes)
    public static float HillR2(float wx,float wz){float a=(wx-HillWX)/HillRX,b=(wz-HillWZ)/HillRZ;return Mathf.Sqrt(a*a+b*b);}
    public static float HillNatural(float wx,float wz)
    {
        float r=HillR2(wx,wz),bump=0f;
        if(r<1f){float c=Mathf.Cos(Mathf.PI*.5f*r);bump=HillH*c*c*(.82f+.36f*BraxChampionsHorizon.Fbm(wx/260f+4.1f,wz/260f-1.3f,4));}
        return BraxChampionsHorizon.FarHeight(wx,wz)+bump-1.5f*Smooth(.78f,1f,r);
    }
    // the land of the hill with the railway cutting east of the portal (floor at the formation, 1:1.5 slopes)
    public static float HillGround(float wx,float wz)
    {
        float h=HillNatural(wx,wz);
        if(wx>=PortalWX){float dz=Mathf.Abs(wz-HillWZ);h=Mathf.Min(h,Formation+Mathf.Max(0f,dz-CutHalf)/1.5f);}
        return h;
    }

    // ================================================================ station deck, platforms
    static void StationDeck()
    {
        // slab with holes for the accesses (both platforms' bands), split into rectangles
        var holes=new List<(float x0,float x1,float z0,float z1)>();
        foreach(float xa in Accesses)for(int p=0;p<2;p++){var (o0,o1)=Opening(xa);var (b0,b1)=Band(p);holes.Add((o0,o1,b0,b1));}
        Slab(concrete,PlatX0,DeckEndX,DeckZ0,DeckZ1,DeckSoffit,DeckTop,holes,true);
        // deck edges: fascia with a shadow line, drip
        foreach(float z in new[]{DeckZ0,DeckZ1})
        {
            float s=z<50f?-1f:1f;
            B(concrete,PlatX0,DeckEndX,DeckSoffit-.35f,DeckSoffit,Mathf.Min(z,z-s*.45f),Mathf.Max(z,z-s*.45f));          // edge beam
            B(frame,PlatX0,DeckEndX,DeckSoffit+.18f,DeckSoffit+.24f,Mathf.Min(z,z+s*.012f),Mathf.Max(z,z+s*.012f));    // shadow line (12 mm)
        }
        // the corridor deck between the bridge and the station (x -142..-130), same section as the viaduct west
        BoxGirder(BridgeX1,PlatX0);
        // transverse frames: three round columns (D 1.0) + cap beam, every 13.3 m
        for(int k=0;k<10;k++)foreach(float s in new[]{-1f,1f})Frame(s*(6.65f+13.3f*k));
        Frame(DeckEndX-1.0f);
        // platforms
        for(int p=0;p<2;p++)Platform(p);
    }
    static void Frame(float x)
    {
        foreach(float z in new[]{DeckZ0+2.4f,LineZ,DeckZ1-2.4f})
        {
            m.Cylinder(concrete,new Vector3(x,LawnY-.2f,z),.5f,DeckSoffit-.8f-(LawnY-.2f),24,Quaternion.identity,false);
            m.Cylinder(concrete,new Vector3(x,DeckSoffit-.8f,z),.5f,.10f,24,Quaternion.identity,false,false,.62f);  // flared head
            Col(x-.5f,x+.5f,LawnY,DeckSoffit-.8f,z-.5f,z+.5f,"słup");
        }
        B(concrete,x-.6f,x+.6f,DeckSoffit-.7f,DeckSoffit,DeckZ0+1.0f,DeckZ1-1.0f);
        Piers++;
    }
    // a slab x0..x1, z0..z1, y0..y1 minus rectangular holes (cut into strips along x)
    static void Slab(Material mat,float x0,float x1,float z0,float z1,float y0,float y1,List<(float x0,float x1,float z0,float z1)> holes,bool col)
    {
        var xs=new List<float>{x0,x1};foreach(var h in holes){if(h.x0>x0&&h.x0<x1)xs.Add(h.x0);if(h.x1>x0&&h.x1<x1)xs.Add(h.x1);}
        xs.Sort();
        for(int i=0;i+1<xs.Count;i++)
        {
            float a=xs[i],b=xs[i+1];if(b-a<1e-3f)continue;float xm=(a+b)*.5f;
            var zs=new List<float>{z0,z1};var hz=new List<(float,float)>();
            foreach(var h in holes)if(xm>h.x0&&xm<h.x1){hz.Add((Mathf.Max(z0,h.z0),Mathf.Min(z1,h.z1)));}
            hz.Sort((p,q)=>p.Item1.CompareTo(q.Item1));
            float z=z0;
            foreach(var (h0,h1) in hz){if(h0>z)B(mat,a,b,y0,y1,z,h0,col);z=Mathf.Max(z,h1);}
            if(z<z1)B(mat,a,b,y0,y1,z,z1,col);
        }
    }
    static void Platform(int p)
    {
        float edge=p==0?P1Z1:P2Z0,outer=p==0?P1Z0:P2Z1,s=p==0?1f:-1f;          // s: from the outer side toward the track
        float lo=Mathf.Min(edge,outer),hi=Mathf.Max(edge,outer);
        // the platform body (hollow structure, closed): track-side wall set back .2 under the coping
        float wall=edge-s*.20f;
        B(concrete,PlatX0+.02f,PlatX1-.02f,DeckTop,PlatformTop-.20f,Mathf.Min(wall,wall-s*.25f),Mathf.Max(wall,wall-s*.25f),true);
        // the surface: coping (granite .40, overhanging), white line .10, a .40 gap, tactile .40, then paving to the outer edge
        var holes=new List<(float,float,float,float)>();var (b0,b1)=Band(p);foreach(float xa in Accesses){var (o0,o1)=Opening(xa);holes.Add((o0,o1,b0,b1));}
        float c1=edge-s*.40f,w1=c1-s*.10f,t0=edge-s*.90f,t1=edge-s*1.30f;
        B(granite,PlatX0,PlatX1,PlatformTop-.20f,PlatformTop,Mathf.Min(edge,c1),Mathf.Max(edge,c1),true);
        Flat(paintWhite,PlatX0+.3f,PlatX1-.3f,Mathf.Min(c1,w1),Mathf.Max(c1,w1),PlatformTop+.012f);
        Flat(tactile,PlatX0+.3f,PlatX1-.3f,Mathf.Min(t0,t1),Mathf.Max(t0,t1),PlatformTop+.012f);
        // paving from the coping to the outer edge (top at PlatformTop), body below it
        float pa=c1,pb=outer;
        Slab(granite,PlatX0,PlatX1,Mathf.Min(pa,pb),Mathf.Max(pa,pb),PlatformTop-.25f,PlatformTop,holes,true);
        Slab(concrete,PlatX0+.02f,PlatX1-.02f,Mathf.Min(wall-s*.25f,pb),Mathf.Max(wall-s*.25f,pb),DeckTop,PlatformTop-.25f,holes,false);
        // outer edge: glass balustrade 1.1 m with a stainless rail, navy posts every 2 m
        float bz=outer+s*.05f;
        var g0=new Vector3(PlatX0,PlatformTop,bz);var g1=new Vector3(PlatX1,PlatformTop,bz);var g2=new Vector3(PlatX1,PlatformTop+1.1f,bz);var g3=new Vector3(PlatX0,PlatformTop+1.1f,bz);
        Q(glass,g0,g1,g2,g3,Vector3.forward);Q(glass,g0,g1,g2,g3,Vector3.back);
        B(stainless,PlatX0,PlatX1,PlatformTop+1.1f,PlatformTop+1.16f,bz-.03f,bz+.03f);
        for(float x=PlatX0;x<=PlatX1+.01f;x+=2f)B(steelNavy,x-.03f,x+.03f,PlatformTop,PlatformTop+1.1f,bz-.04f,bz+.04f);
        Col(PlatX0,PlatX1,PlatformTop,PlatformTop+1.2f,bz-.1f,bz+.1f,"balustrada peronu");
        // ends: ramps down for staff are closed by railings with a gate; colliders across the ends (no walking onto the line)
        foreach(float x in new[]{PlatX0,PlatX1})
        {
            B(steelNavy,x-.04f,x+.04f,PlatformTop,PlatformTop+1.1f,lo,hi);
            Col(x-.1f,x+.1f,PlatformTop,PlatformTop+2f,lo,hi,"koniec peronu");
        }
        // invisible wall along the platform edge above the track (no jumping down; the train has its own collider)
        Col(PlatX0,PlatX1,PlatformTop-1.5f,PlatformTop+3.0f,Mathf.Min(edge,edge+s*.15f),Mathf.Max(edge,edge+s*.15f),"krawędź peronu");
        // furniture: benches (back to back) and bins every 24 m between the columns, out of the access openings
        for(float x=-120f;x<=120f;x+=24f)
        {
            bool near=false;foreach(float xa in Accesses){var (o0,o1)=Opening(xa);if(x>o0-3f&&x<o1+3f)near=true;}
            if(near)continue;
            float cz=CanopyColumnZ(p)-s*1.6f;
            PlatformBench(x,cz,s);
        }
    }
    // a platform bench (5 seats, oak on a graphite beam), facing the track; seats are BraxSeats
    static void PlatformBench(float cx,float z,float s)
    {
        float y=PlatformTop;var face=new Vector3(0,0,s);
        B(frame,cx-2.3f,cx+2.3f,y+.38f,y+.43f,z-.05f,z+.05f);
        foreach(float lx in new[]{-1.9f,1.9f})B(frame,cx+lx-.04f,cx+lx+.04f,y,y+.43f,z-.22f,z+.22f);
        for(int i=0;i<5;i++)
        {
            float x0=cx-2.25f+i*.9f;
            RB(benchWood??BraxChampionsPromenadeFurniture.Teak,x0+.03f,x0+.87f,y+.43f,y+.48f,Mathf.Min(z-s*.22f,z+s*.20f),Mathf.Max(z-s*.22f,z+s*.20f),.02f);
            RB(benchWood??BraxChampionsPromenadeFurniture.Teak,x0+.03f,x0+.87f,y+.53f,y+.88f,Mathf.Min(z-s*.25f,z-s*.20f),Mathf.Max(z-s*.25f,z-s*.20f),.02f);
            Seat(x0+.45f,y+.48f,z+s*.02f,face,"ławka peron "+cx.ToString("0")+"/"+i);
        }
        Col(cx-2.3f,cx+2.3f,y,y+.9f,z-.3f,z+.3f,"ławka peronowa");Benches++;
    }

    // ================================================================ canopies + catenary
    static void Canopies()
    {
        for(int p=0;p<2;p++)
        {
            float s=p==0?1f:-1f,cz=CanopyColumnZ(p),r0=p==0?DeckZ0:P2Z0-.37f,r1=p==0?P1Z1+.37f:DeckZ1;
            float soffit=PlatformTop+4.0f,top=soffit+.55f;
            // roof: graphite top, navy fascia, larch slat soffit, LED lines along x
            B(roofGraphite,PlatX0-1.85f,PlatX1+1.85f,top-.08f,top,r0+.15f,r1-.15f);
            B(navy,PlatX0-2f,PlatX1+2f,soffit,top+.06f,r0,r0+.15f);B(navy,PlatX0-2f,PlatX1+2f,soffit,top+.06f,r1-.15f,r1);
            B(navy,PlatX0-2f,PlatX0-1.85f,soffit,top+.06f,r0+.15f,r1-.15f);B(navy,PlatX1+1.85f,PlatX1+2f,soffit,top+.06f,r0+.15f,r1-.15f);
            for(float z=r0+.35f;z<r1-.3f;z+=.22f)B(larchSoffit,PlatX0-1.85f,PlatX1+1.85f,soffit+.06f,soffit+.12f,z,z+.14f);
            B(steelNavy,PlatX0-1.85f,PlatX1+1.85f,soffit+.20f,top-.08f,r0+.15f,r1-.15f);           // roof deck behind the slats (dark)
            foreach(float lz in new[]{cz-s*2.2f,cz+s*2.0f})B(led,PlatX0,PlatX1,soffit+.035f,soffit+.06f,lz-.05f,lz+.05f);
            // columns every 12 m on the row cz, skipping the access openings; arms (navy) across the roof at each column
            for(float x=-126f;x<=126.1f;x+=12f)
            {
                bool skip=false;foreach(float xa in Accesses){var (o0,o1)=Opening(xa);if(x>o0-.6f&&x<o1+.6f)skip=true;}
                if(skip)continue;
                m.Cylinder(steelNavy,new Vector3(x,PlatformTop,cz),.18f,soffit-PlatformTop,20,Quaternion.identity,false);
                m.Cylinder(frame,new Vector3(x,PlatformTop,cz),.26f,.12f,20,Quaternion.identity,true);
                B(steelNavy,x-.12f,x+.12f,soffit-.35f,soffit+.02f,r0+.2f,r1-.2f);
                Col(x-.2f,x+.2f,PlatformTop,soffit,cz-.2f,cz+.2f,"słup wiaty");
            }
            // platform number boards hanging under the soffit every 48 m, the station name every 48 m (offset 24)
            for(float x=-108f;x<=108f;x+=48f)
            {
                PlatformSign(x,cz-s*1.0f,soffit,(p+1).ToString(),true,s);
                PlatformSign(x+24f,cz-s*1.0f,soffit,"MIASTO MISTRZÓW",false,s);
            }
            Col(PlatX0-2f,PlatX1+2f,soffit,top,r0,r1,"wiata");
        }
        // catenary over both tracks: cross-span beams between the two canopies every 24 m with droppers to the wires
        float cw=RailTop+5.3f;
        foreach(float tz in new[]{Track1Z,Track2Z})
        {
            m.Tube(steel,new Vector3(-545f,cw,tz),new Vector3(BufferX-2f,cw,tz),.014f,6,1);
        }
        for(float x=-120f;x<=120.1f;x+=24f)
        {
            B(steelNavy,x-.08f,x+.08f,PlatformTop+4.0f+.1f,PlatformTop+4.0f+.35f,P1Z1+.37f,P2Z0-.37f);
            foreach(float tz in new[]{Track1Z,Track2Z})m.Tube(steel,new Vector3(x,PlatformTop+4.1f,tz),new Vector3(x,cw,tz),.012f,6,1);
        }
    }
    // a hanging double-sided sign (navy, white 3D letters) under the canopy, along x
    static void PlatformSign(float x,float z,float soffit,string text,bool number,float s)
    {
        float w=number?1.2f:6.4f,h=number?1.0f:.75f,y1=soffit-.6f,y0=y1-h;
        foreach(float dx in new[]{-w*.4f,w*.4f})m.Tube(steel,new Vector3(x+dx,y1,z),new Vector3(x+dx,soffit,z),.012f,6,1);
        RB(signBlue,x-w*.5f,x+w*.5f,y0,y1,z-.06f,z+.06f,.05f);
        var lm=new BraxChampionsMeshBatch();
        float cap=number?.70f:.38f;
        BraxChampionsLetters3D.Text(lm,whiteLetters,navy,text,new Vector3(x,y0+(h-cap)*.5f,z-.06f),180f,cap,.012f,.10f,w-.3f);
        BraxChampionsLetters3D.Text(lm,whiteLetters,navy,text,new Vector3(x,y0+(h-cap)*.5f,z+.06f),0f,cap,.012f,.10f,w-.3f);
        BraxChampionsLetters3D.Build(lm,root,"D2 tablica "+text);Signs++;
    }

    // ================================================================ track
    static readonly float[] RailOff={-.7535f,.7535f};
    static void Track()
    {
        float xe=BufferX,xw=LX(TunnelEndWX)+20f;
        // ballast bed: profile across both tracks, extruded along x in 25 m pieces
        var prof=new[]{new Vector2(Track1Z-2.33f,DeckTop),new Vector2(Track1Z-1.7f,6.27f),new Vector2(Track1Z+1.7f,6.27f),new Vector2(LineZ,6.12f),
                       new Vector2(Track2Z-1.7f,6.27f),new Vector2(Track2Z+1.7f,6.27f),new Vector2(Track2Z+2.33f,DeckTop)};
        for(float x0=xw;x0<xe-.01f;x0+=25f)
        {
            float x1=Mathf.Min(xe+1.0f,x0+25f);
            for(int i=0;i+1<prof.Length;i++)
            {
                var a=prof[i];var b=prof[i+1];var n=new Vector3(0,b.x-a.x,-(b.y-a.y));
                Q(ballast,new Vector3(x0,a.y,a.x),new Vector3(x1,a.y,a.x),new Vector3(x1,b.y,b.x),new Vector3(x0,b.y,b.x),n.y<0?-n:n);
            }
        }
        // sleepers (B03: 2.6 x .26 x .22), every .6 m, on both tracks; rails (UIC60) in 25 m lengths
        foreach(float tz in new[]{Track1Z,Track2Z})
        {
            for(float x=xe-.6f;x>xw;x-=.6f){m.Box(sleeper,new Vector3(x,6.208f,tz),new Vector3(.26f,.22f,2.6f),Quaternion.identity,false);Sleepers++;}
            foreach(float ro in RailOff)
                for(float x0=xw;x0<xe-.01f;x0+=25f)
                {
                    float x1=Mathf.Min(xe,x0+25f),z=tz+ro;
                    B(railWeb,x0,x1,6.318f,6.330f,z-.075f,z+.075f);
                    B(railWeb,x0,x1,6.330f,6.448f,z-.0085f,z+.0085f);
                    B(railHead,x0,x1,6.448f,RailTop,z-.036f,z+.036f);
                }
            BufferStop(xe,tz);
        }
        // the trains' own colliders keep you off; the track between the platforms is out of reach (platform edge walls)
    }
    // friction buffer stop: steel frame clamped to the rails, two buffers at 1.06 m, red/white beam, red lamp
    static void BufferStop(float x,float tz)
    {
        float y=RailTop;
        foreach(float ro in RailOff){B(steelNavy,x,x+1.6f,y,y+.10f,tz+ro-.09f,tz+ro+.09f);Beam(steelNavy,new Vector3(x+1.5f,y,tz+ro),new Vector3(x+.15f,y+1.0f,tz+ro),.16f,.16f);}
        B(bufferRed,x+.05f,x+.35f,y+.85f,y+1.25f,tz-1.05f,tz+1.05f);
        for(int i=0;i<4;i++){float z0=tz-1.05f+i*.525f;B(paintWhite,x+.035f,x+.05f,y+.85f,y+1.25f,z0,z0+.26f);}
        foreach(float bz in new[]{-.875f,.875f})
        {
            m.Cylinder(frame,new Vector3(x+.05f,y+1.06f,tz+bz),.19f,.35f,20,Quaternion.Euler(0,0,90),true,true);
            m.Cylinder(stainless,new Vector3(x-.30f,y+1.06f,tz+bz),.22f,.04f,20,Quaternion.Euler(0,0,90),true,true);
        }
        m.Cylinder(frame,new Vector3(x+.2f,y+1.25f,tz),.04f,.6f,10,Quaternion.identity,true);
        m.Cylinder(bufferRed,new Vector3(x+.2f,y+1.85f,tz),.10f,.12f,16,Quaternion.Euler(0,0,90),true,true);
        Col(x,x+1.6f,y,y+1.3f,tz-1.2f,tz+1.2f,"kozioł oporowy");
    }

    // ================================================================ accesses: stairs, lifts, the passage
    static void Access()
    {
        foreach(float xa in Accesses)for(int p=0;p<2;p++)Stair(xa,p);
        // the passage under the tracks at the hall (x -6..6): larch slat ceiling under the deck, LED lines, wayfinding
        foreach(float xa in Accesses)
        {
            float x0=xa-8f,x1=xa+14f;
            for(float z=DeckZ0+.6f;z<DeckZ1-.7f;z+=.25f)
            {
                bool hole=false;for(int p=0;p<2;p++){var (b0,b1)=Band(p);if(z+.15f>b0-.05f&&z<b1+.05f)hole=true;}
                if(hole)continue;
                B(larchSoffit,x0,x1,DeckSoffit-.12f,DeckSoffit-.06f,z,z+.15f);
            }
            for(float z=DeckZ0+3f;z<DeckZ1-2f;z+=6f)B(led,x0,x1,DeckSoffit-.14f,DeckSoffit-.12f,z-.04f,z+.04f);
            // the "PERON 1" / "PERON 2" signs at the stair feet
            for(int p=0;p<2;p++)
            {
                var (b0,b1)=Band(p);float bz=(b0+b1)*.5f;
                var lm=new BraxChampionsMeshBatch();
                RB(signBlue,xa-7.6f,xa-7.5f+.0f,DeckSoffit-1.3f,DeckSoffit-.5f,b0,b1,.03f);
                BraxChampionsLetters3D.Text(lm,whiteLetters,navy,"PERON "+(p+1),new Vector3(xa-7.62f,DeckSoffit-1.08f,bz),-90f,.38f,.012f,.10f,StairW-.2f);
                BraxChampionsLetters3D.Build(lm,root,"D2 znak peron "+(p+1));Signs++;
            }
        }
    }
    // two flights (20 risers each) rising toward +x from xa-7 (passage .30) to xa+7 (platform 7.26), landing at xa-1..xa+1;
    // the lift (glass shaft, navy frame) beyond the top; glass balustrade round the opening on the platform
    static void Stair(float xa,int p)
    {
        var (z0,z1)=Band(p);float rise=(PlatformTop-WalkY)/(2*StairRisers);
        float xb=xa-7f;
        void Flight(float xs,float ys)
        {
            for(int i=0;i<StairRisers;i++)
            {
                float x=xs+i*StairRun,y=ys+(i+1)*rise;
                B(granite,x,x+StairRun+.02f,y-.05f,y,z0+.05f,z1-.05f);
                B(concrete,x,x+StairRun,ys-.25f+i*rise,y-.05f,z0+.07f,z1-.07f);
                Flat(paintYellow,x,x+.05f,z0+.08f,z1-.08f,y+.012f);                 // contrast nosing
            }
            // collider as a ramp
            float len=StairRisers*StairRun,h=StairRisers*rise;
            var o=new GameObject("schody | kolizja");o.transform.SetParent(cols,false);
            float ang=-Mathf.Atan2(h,len)*Mathf.Rad2Deg;
            o.transform.localPosition=new Vector3(xs+len*.5f,ys+h*.5f-.15f,(z0+z1)*.5f)*K;o.transform.localRotation=Quaternion.Euler(0,0,-ang);
            o.AddComponent<BoxCollider>().size=new Vector3(Mathf.Sqrt(len*len+h*h),.3f,z1-z0)*K;Colliders++;
            // stringers (navy steel) and stainless handrails on both sides
            foreach(float zz in new[]{z0+.05f,z1-.05f})
            {
                Beam(steelNavy,new Vector3(xs,ys-.15f,zz),new Vector3(xs+len,ys+h-.15f,zz),.10f,.40f);
                Beam(stainless,new Vector3(xs,ys+.95f,zz+(zz<(z0+z1)*.5f?.05f:-.05f)),new Vector3(xs+len,ys+h+.95f,zz+(zz<(z0+z1)*.5f?.05f:-.05f)),.05f,.05f);
                var g0=new Vector3(xs,ys,zz);var g1=new Vector3(xs+len,ys+h,zz);var g2=new Vector3(xs+len,ys+h+.9f,zz);var g3=new Vector3(xs,ys+.9f,zz);
                Q(glass,g0,g1,g2,g3,Vector3.forward);Q(glass,g0,g1,g2,g3,Vector3.back);
            }
        }
        Flight(xb,WalkY);
        float yl=WalkY+StairRisers*rise;
        B(granite,xa-1f,xa+1f,yl-.25f,yl,z0+.05f,z1-.05f,true);
        Flight(xa+1f,yl);
        // glass balustrade round the opening on the platform (three sides; the stair arrives at xa+7 open toward +x)
        var (o0,o1)=Opening(xa);
        float y=PlatformTop;
        void Rail(Vector3 a,Vector3 b)
        {
            var n=Vector3.Cross(b-a,Vector3.up);
            Q(glass,a,b,b+Vector3.up*1.1f,a+Vector3.up*1.1f,n);Q(glass,a,b,b+Vector3.up*1.1f,a+Vector3.up*1.1f,-n);
            Beam(stainless,a+Vector3.up*1.13f,b+Vector3.up*1.13f,.05f,.05f);
            var c=(a+b)*.5f;var sz=new Vector3(Mathf.Abs(b.x-a.x)+.1f,1.2f,Mathf.Abs(b.z-a.z)+.1f);
            Col(c.x-sz.x*.5f,c.x+sz.x*.5f,y,y+1.2f,c.z-sz.z*.5f,c.z+sz.z*.5f,"balustrada otworu");
        }
        Rail(new Vector3(o0,y,z0),new Vector3(xa+7f,y,z0));Rail(new Vector3(o0,y,z1),new Vector3(xa+7f,y,z1));Rail(new Vector3(o0,y,z0),new Vector3(o0,y,z1));
        // lift: glass shaft from the passage to the platform + 2.6 m, navy corner posts, cabin at the platform
        float lx0=xa+7.6f,lx1=lx0+LiftW;
        Lift(lx0,lx1,z0+.15f,z1-.15f,p);
        // the opening's sides in the deck: concrete reveals
        B(concrete,o0,o1,DeckSoffit,DeckTop,z0-.02f,z0);B(concrete,o0,o1,DeckSoffit,DeckTop,z1,z1+.02f);
    }
    static void Lift(float x0,float x1,float z0,float z1,int p)
    {
        float yb=WalkY,yt=PlatformTop+2.7f;
        foreach(float x in new[]{x0,x1})foreach(float z in new[]{z0,z1})B(steelNavy,x-.06f,x+.06f,yb,yt+.2f,z-.06f,z+.06f);
        B(steelNavy,x0-.06f,x1+.06f,yt,yt+.25f,z0-.06f,z1+.06f);
        // glass on the three closed sides (the doors face -x, toward the stair)
        foreach(float z in new[]{z0,z1})
        {
            var a=new Vector3(x0,yb,z);var b=new Vector3(x1,yb,z);var c=new Vector3(x1,yt,z);var d=new Vector3(x0,yt,z);
            Q(glass,a,b,c,d,Vector3.forward);Q(glass,a,b,c,d,Vector3.back);
        }
        {var a=new Vector3(x1,yb,z0);var b=new Vector3(x1,yb,z1);var c=new Vector3(x1,yt,z1);var d=new Vector3(x1,yt,z0);Q(glass,a,b,c,d,Vector3.right);Q(glass,a,b,c,d,Vector3.left);}
        // door frames (stainless) at both stops on the -x face
        foreach(float y in new[]{yb,PlatformTop})
        {
            B(stainless,x0-.08f,x0+.02f,y,y+2.2f,z0,z0+.35f);B(stainless,x0-.08f,x0+.02f,y,y+2.2f,z1-.35f,z1);B(stainless,x0-.08f,x0+.02f,y+2.1f,y+2.3f,z0,z1);
        }
        Col(x0,x1,yb,yt,z0-.1f,z0+.1f,"szyb windy");Col(x0,x1,yb,yt,z1-.1f,z1+.1f,"szyb windy");Col(x1-.1f,x1+.1f,yb,yt,z0,z1,"szyb windy");
        Lifts++;
        LiftCar(x0,x1,z0,z1,p);
    }

    // ================================================================ bridge over X70: steel tied arch
    static void Bridge()
    {
        float x0=BridgeX0,x1=BridgeX1,span=x1-x0,xc=(x0+x1)*.5f,zA=LineZ-LineDeckHalf+.3f,zB=LineZ+LineDeckHalf-.3f,rise=9f,yT=6.6f;
        // deck slab, cross girders under it, tie girders (navy boxes) at both edges
        B(concrete,x0,x1,DeckSoffit+.25f,DeckTop,zA+.4f,zB-.4f);
        for(float x=x0+1.0f;x<x1-.5f;x+=span/12f)B(steelNavy,x-.18f,x+.18f,DeckSoffit,DeckSoffit+.25f,zA,zB);
        foreach(float z in new[]{zA,zB})
        {
            B(steelNavy,x0-.6f,x1+.6f,DeckSoffit,yT,z-.40f,z+.40f,true);
            B(frame,x0-.6f,x1+.6f,yT,yT+.03f,z-.42f,z+.42f);
        }
        float Arch(float x){float t=(x-xc)/(span*.5f);return yT+rise*(1f-t*t);}
        foreach(float z in new[]{zA,zB})
        {
            int n=24;
            for(int i=0;i<n;i++)
            {
                float a=x0+span*i/n,b=x0+span*(i+1)/n;
                Beam(steelNavy,new Vector3(a,Arch(a),z),new Vector3(b,Arch(b),z),.80f,.70f);
            }
            for(int i=1;i<12;i++)
            {
                float x=x0+span*i/12f;
                m.Tube(steel,new Vector3(x,yT,z),new Vector3(x,Arch(x)-.35f,z),.05f,8,1);Hangers++;
                m.Cylinder(frame,new Vector3(x,yT,z),.09f,.12f,10,Quaternion.identity,true);
            }
        }
        // wind bracing between the arch tops (struts + diagonals) clear of the train (top bracing above 13 m)
        float prev=0f;
        for(int i=3;i<=9;i++)
        {
            float x=x0+span*i/12f,y=Arch(x);
            if(y-RailTop<7.0f)continue;
            Beam(steelNavy,new Vector3(x,y,zA),new Vector3(x,y,zB),.40f,.40f);
            if(prev!=0f)Beam(steelNavy,new Vector3(prev,Arch(prev),zA),new Vector3(x,y,zB),.22f,.22f);
            prev=x;
        }
        // bearings on the piers and the piers (wall piers 9 x 2.2 m) at both ends
        foreach(float x in new[]{x0,x1})
        {
            Pier(x,DeckSoffit-.30f);
            foreach(float z in new[]{zA,zB})B(frame,x-.5f,x+.5f,DeckSoffit-.30f,DeckSoffit,z-.5f,z+.5f);
        }
        // the track walkways / cable troughs along the inside of the ties, railings
        foreach(float z in new[]{zA+.9f,zB-.9f})B(concrete,x0,x1,DeckTop,DeckTop+.25f,z-.35f,z+.35f);
        Col(x0,x1,DeckSoffit,DeckTop,zA,zB,"most");
    }
    static void Pier(float x,float top)
    {
        B(concrete,x-1.1f,x+1.1f,-1.0f,top-.6f,LineZ-4.5f,LineZ+4.5f,true);
        B(concrete,x-1.4f,x+1.4f,top-.6f,top,LineZ-5.5f,LineZ+5.5f,true);       // pier cap
        B(frame,x-1.112f,x+1.112f,.8f,.9f,LineZ-4.512f,LineZ+4.512f);           // dark band (12 mm proud)
        Piers++;
    }
    // concrete box girder for the 2-track line: deck 12 m (z 45.5..57.5), webs inclined to a 6 m bottom slab at 5.0
    static void BoxGirder(float x0,float x1)
    {
        float z0=LineZ-LineDeckHalf,z1=LineZ+LineDeckHalf,yb=DeckSoffit-.25f;
        B(concrete,x0,x1,DeckTop-.30f,DeckTop,z0,z1,true);
        foreach(float s in new[]{-1f,1f})
        {
            var a=new Vector3(x0,DeckTop-.30f,LineZ+s*4.6f);var b=new Vector3(x1,DeckTop-.30f,LineZ+s*4.6f);
            var c=new Vector3(x1,yb,LineZ+s*3.0f);var d=new Vector3(x0,yb,LineZ+s*3.0f);
            Q(concrete,a,b,c,d,new Vector3(0,-.5f,s));
        }
        Q(concrete,new Vector3(x0,yb,LineZ-3f),new Vector3(x1,yb,LineZ-3f),new Vector3(x1,yb,LineZ+3f),new Vector3(x0,yb,LineZ+3f),Vector3.down);
        // cantilever undersides from the webs to the deck edges
        foreach(float s in new[]{-1f,1f})
            Q(concrete,new Vector3(x0,DeckTop-.30f,LineZ+s*4.6f),new Vector3(x1,DeckTop-.30f,LineZ+s*4.6f),new Vector3(x1,DeckTop-.30f,LineZ+s*LineDeckHalf),new Vector3(x0,DeckTop-.30f,LineZ+s*LineDeckHalf),Vector3.down);
        // edge parapets with a steel handrail on top (maintenance walkways)
        foreach(float s in new[]{-1f,1f})
        {
            float zz=LineZ+s*LineDeckHalf;
            B(concrete,x0,x1,DeckTop,DeckTop+1.0f,Mathf.Min(zz,zz-s*.25f),Mathf.Max(zz,zz-s*.25f),true);
            B(frame,x0,x1,DeckTop+1.0f,DeckTop+1.05f,Mathf.Min(zz,zz-s*.25f)-.02f,Mathf.Max(zz,zz-s*.25f)+.02f);
        }
    }

    // ================================================================ west: viaduct, embankment, cutting, tunnel
    static void LineWest()
    {
        float xEdge=RegionEdgeX,xv=ViaductEndX;
        BoxGirder(xv,BridgeX0);
        foreach(float x in new[]{-211f,-233f,-255f,-275f})Pier(x,DeckSoffit-.25f);
        // abutment at the end of the viaduct (world x -60): wall + wings into the embankment
        B(concrete,xv-2.0f,xv,-1.0f,DeckTop+1.0f,LineZ-7.5f,LineZ+7.5f,true);
        // embankment: top at the formation (half-width 6.5), 1:1.5 slopes to below the ground, from the abutment west
        float wPortal=LX(PortalWX),xE=xv-2.0f;
        float xc=xE;
        for(float x=xE;x>wPortal;x-=2f){float h=HillNatural(x+CentreX,CentreZ+LineZ);if(h>Formation+.3f){xc=x;break;}xc=x;}
        for(float x=xE;x>xc+.01f;x-=4f)
        {
            float x1=Mathf.Max(xc,x-4f);
            foreach(float s in new[]{-1f,1f})
            {
                float zt=LineZ+s*CutHalf,zb=LineZ+s*(CutHalf+1.5f*(Formation+2f));
                Q(lawnSlope(),new Vector3(x,Formation,zt),new Vector3(x1,Formation,zt),new Vector3(x1,-2f,zb),new Vector3(x,-2f,zb),new Vector3(0,1,s));
            }
            Flat(lawnSlope(),x1,x,LineZ-CutHalf,LineZ+CutHalf,Formation+.01f);
        }
        // drainage ditches and the cable trough along the line in the cutting are part of the ballast shoulders
        // the portal at world x -170: headwall following the cutting and the hill, the tube into the hill
        Portal(wPortal);
        Tunnel(wPortal,LX(TunnelEndWX));
        // catenary portals (masts on the deck edges, a beam over both tracks) every 45 m west of the station
        for(float x=-150f;x>wPortal+5f;x-=45f)
        {
            if(x>BridgeX0-2f&&x<BridgeX1+2f)continue;
            float y0=x>xv?DeckTop:Formation;
            foreach(float s in new[]{-1f,1f})
            {
                float zz=LineZ+s*(LineDeckHalf-.6f);
                m.Cylinder(steelNavy,new Vector3(x,y0,zz),.14f,RailTop+6.4f-y0,12,Quaternion.identity,true);
            }
            B(steelNavy,x-.12f,x+.12f,RailTop+6.2f,RailTop+6.5f,LineZ-LineDeckHalf+.4f,LineZ+LineDeckHalf-.4f);
            foreach(float tz in new[]{Track1Z,Track2Z})m.Tube(steel,new Vector3(x,RailTop+6.2f,tz),new Vector3(x,RailTop+5.3f,tz),.012f,6,1);
        }
    }
    // no tree models on the hill: drzewo_verdant is ~84 k triangles; the far land's vertex albedo paints its meadow and forest
    static Material slopeGrass;
    static Material lawnSlope(){if(!slopeGrass)slopeGrass=lawn;return slopeGrass;}
    // headwall: a vertical concrete face (1.2 m thick) across the cutting, its top following the hill, the arched opening
    const float TubeHalf=5.5f,TubeSpring=9.0f;
    static float TubeArch(float dz){float a=Mathf.Abs(dz);return a>=TubeHalf?TubeSpring:TubeSpring+Mathf.Sqrt(TubeHalf*TubeHalf-a*a);}
    static void Portal(float xp)
    {
        float xf=xp+.6f,xbk=xp-.6f,half=26f;
        float CutY(float dz)=>Formation+Mathf.Max(0f,Mathf.Abs(dz)-CutHalf)/1.5f;
        float TopY(float dz)=>HillNatural(xp+CentreX,CentreZ+LineZ+dz)+.5f;
        for(float dz=-half;dz<half-.01f;dz+=.5f)
        {
            float a=dz,b=dz+.5f,ma=(a+b)*.5f;
            float ya0=CutY(a)-.4f,yb0=CutY(b)-.4f,ya1=Mathf.Max(TopY(a),TubeSpring+TubeHalf+1.2f),yb1=Mathf.Max(TopY(b),TubeSpring+TubeHalf+1.2f);
            bool open=Mathf.Abs(ma)<TubeHalf;
            if(open){ya0=TubeArch(a);yb0=TubeArch(b);}
            Q(concrete,new Vector3(xf,ya0,LineZ+a),new Vector3(xf,yb0,LineZ+b),new Vector3(xf,yb1,LineZ+b),new Vector3(xf,ya1,LineZ+a),Vector3.right);
            Q(concrete,new Vector3(xbk,ya1,LineZ+a),new Vector3(xbk,yb1,LineZ+b),new Vector3(xf,yb1,LineZ+b),new Vector3(xf,ya1,LineZ+a),Vector3.up);   // top
            if(open)Q(concreteDark,new Vector3(xbk,ya0,LineZ+a),new Vector3(xbk,yb0,LineZ+b),new Vector3(xf,yb0,LineZ+b),new Vector3(xf,ya0,LineZ+a),Vector3.down);   // intrados
        }
        // coping on the top edge, a band round the opening (dark granite), the end faces
        for(float dz=-half;dz<half-.01f;dz+=1f)
        {
            float a=dz,b=dz+1f;
            Beam(graniteDark,new Vector3(xp,Mathf.Max(TopY(a),TubeSpring+TubeHalf+1.2f)+.12f,LineZ+a),new Vector3(xp,Mathf.Max(TopY(b),TubeSpring+TubeHalf+1.2f)+.12f,LineZ+b),1.5f,.25f);
        }
        for(int i=0;i<24;i++)
        {
            float a0=Mathf.PI*i/24f,a1=Mathf.PI*(i+1)/24f;
            Beam(graniteDark,new Vector3(xf+.06f,TubeSpring+Mathf.Sin(a0)*(TubeHalf+.25f),LineZ+Mathf.Cos(a0)*(TubeHalf+.25f)),
                             new Vector3(xf+.06f,TubeSpring+Mathf.Sin(a1)*(TubeHalf+.25f),LineZ+Mathf.Cos(a1)*(TubeHalf+.25f)),.5f,.12f);
        }
        foreach(float s in new[]{-1f,1f})B(graniteDark,xf,xf+.12f,Formation,TubeSpring,LineZ+s*(TubeHalf+.5f)-.25f,LineZ+s*(TubeHalf+.5f)+.25f);
        Col(xbk,xf,Formation,TubeSpring+TubeHalf+1.2f,LineZ-half,LineZ-TubeHalf,"portal");Col(xbk,xf,Formation,TubeSpring+TubeHalf+1.2f,LineZ+TubeHalf,LineZ+half,"portal");
    }
    static void Tunnel(float xp,float xe)
    {
        float a=xp-.6f;
        // walls and vault (dark, inside), floor under the ballast, end wall
        foreach(float s in new[]{-1f,1f})
            Q(tunnelDark,new Vector3(a,Formation,LineZ+s*TubeHalf),new Vector3(xe,Formation,LineZ+s*TubeHalf),new Vector3(xe,TubeSpring,LineZ+s*TubeHalf),new Vector3(a,TubeSpring,LineZ+s*TubeHalf),new Vector3(0,0,-s));
        int n=24;
        for(int i=0;i<n;i++)
        {
            float a0=Mathf.PI*i/n,a1=Mathf.PI*(i+1)/n;
            var p0=new Vector3(0,TubeSpring+Mathf.Sin(a0)*TubeHalf,LineZ+Mathf.Cos(a0)*TubeHalf);var p1=new Vector3(0,TubeSpring+Mathf.Sin(a1)*TubeHalf,LineZ+Mathf.Cos(a1)*TubeHalf);
            var c=new Vector3(0,TubeSpring,LineZ);
            Q(tunnelDark,new Vector3(a,p0.y,p0.z),new Vector3(xe,p0.y,p0.z),new Vector3(xe,p1.y,p1.z),new Vector3(a,p1.y,p1.z),c-(p0+p1)*.5f);
            T(black,new Vector3(xe,TubeSpring,LineZ),new Vector3(xe,p0.y,p0.z),new Vector3(xe,p1.y,p1.z),Vector3.right);
        }
        Q(black,new Vector3(xe,Formation,LineZ-TubeHalf),new Vector3(xe,Formation,LineZ+TubeHalf),new Vector3(xe,TubeSpring,LineZ+TubeHalf),new Vector3(xe,TubeSpring,LineZ-TubeHalf),Vector3.right);
        Flat(tunnelDark,xe,a,LineZ-TubeHalf,LineZ+TubeHalf,Formation-.02f);
        // keep everyone out of the tunnel and off the line beyond the station (the player never needs it)
        Col(a-1f,a,Formation,TubeSpring+TubeHalf,LineZ-TubeHalf,LineZ+TubeHalf,"tunel");
        Col(PlatX0-1.0f,PlatX0,DeckTop,DeckTop+4f,LineZ-LineDeckHalf,LineZ+LineDeckHalf,"tor - zakaz wejścia");
    }
}
