using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// D2 furniture, round and finished (owner 11.11 14:16: "zobacz te ławki, to jak to wygląda ... ta informacja, co to jest
// za napis ... wykończenie"): every bench of the station is one design of a real station bench - bent steel end frames
// (graphite powder coat, 44 mm tube, feet pads), separate rounded oak slats on the seat (5, 1.5 cm gaps) and the backrest
// (3, leaning back 14 deg), armrests at the ends (and between every two seats on the long hall rows), a BraxSeat per seat.
// The info desk is a curved walnut counter with a white marble top and a raised ledge, an LED line under the top, a staff
// work surface with two monitors and two chairs, and a navy totem behind it with the lit "i" pictogram and INFORMACJA.
public static partial class BraxChampionsD2
{
    static Material oakSlat,walnut,benchFrame,leatherNavy;
    static void FurnitureMaterials()
    {
        oakSlat=Inside(Tex("bench_oak_slats","oak_wood_planks",1.0f,new Color(1.0f,.93f,.86f)),.9f);
        walnut=Inside(Tex("info_walnut","smoked_walnut_veneer",1.2f,new Color(1.0f,.93f,.88f)),.85f);
        benchFrame=S("bench_frame_graphite_powder",new Color(.14f,.15f,.17f),.42f,.6f);
        leatherNavy=Inside(BraxChampionsFoundation.LibraryLeather("d2_bench_leather_navy",new Color(.34f,.42f,.62f)),.85f);   // navy hide, the board's colour
    }
    // a station bench: centre c (floor level), facing f (axis-aligned), n seats of .62 m, arms between seats every `armsEvery`
    static void StationBench(Vector3 c,Vector3 f,int n,string label,int armsEvery=0,bool upholstered=false)
    {
        if(!oakSlat)FurnitureMaterials();
        f=f.normalized;var up=Vector3.up;var u=Vector3.Cross(up,f).normalized;           // along the seat
        var rot=Quaternion.LookRotation(-f,up);                                           // local z = back, x = along
        float L=n*.62f+.10f,half=L*.5f;
        Vector3 P(float a,float h,float d)=>c+u*a+up*h-f*d;                              // d > 0: toward the back
        // supports: at both ends and every two seats
        var sup=new List<float>{-half+.03f,half-.03f};
        for(int i=2;i<n;i+=2)sup.Add(-half+.05f+i*.62f);
        foreach(float a in sup)
        {
            float r=.022f;
            m.Tube(benchFrame,P(a,0f,-.22f),P(a,.40f,-.19f),r,8);                         // front leg
            m.Tube(benchFrame,P(a,.40f,-.19f),P(a,.425f,.17f),r,8);                       // seat rail (slight fall back)
            m.Tube(benchFrame,P(a,0f,.20f),P(a,.425f,.17f),r,8);                          // rear leg
            m.Tube(benchFrame,P(a,.425f,.17f),P(a,.88f,.29f),r,8);                        // back post, leaning back
            foreach(float d in new[]{-.22f,.20f})m.Cylinder(rubber,P(a,0f,d),.04f,.02f,10,Quaternion.identity,true);
        }
        // a stiffening tube along the bench under the seat (between the rear legs)
        m.Tube(benchFrame,P(-half+.03f,.18f,.19f),P(half-.03f,.18f,.19f),.018f,8);
        // seat slats (5) and back slats (3), rounded oak
        for(int i=0;i<5;i++)
        {
            float d=-.20f+i*.083f,h=.445f+(d+.20f)*.06f;
            m.RoundedBox(oakSlat,P(0f,h,d+.034f),new Vector3(L,.032f,.068f),.012f,rot*Quaternion.Euler(-3f,0,0),2);
        }
        for(int i=0;i<3;i++)
        {
            float h=.56f+i*.11f,d=.17f+(h-.425f)*.26f;
            m.RoundedBox(oakSlat,P(0f,h,d+.035f),new Vector3(L,.085f,.026f),.010f,rot*Quaternion.Euler(14f,0,0),2);
        }
        // upholstered (hall, waiting rooms): stitched navy leather cushions on the oak, one per seat, rounded
        if(upholstered)
            for(int i=0;i<n;i++)
            {
                float a=-half+.05f+(i+.5f)*.62f;
                m.RoundedBox(leatherNavy,P(a,.505f,-.01f),new Vector3(.56f,.07f,.40f),.03f,rot*Quaternion.Euler(-3f,0,0),3);
                float h=.70f,d=.17f+(h-.425f)*.26f;
                m.RoundedBox(leatherNavy,P(a,h,d+.0f),new Vector3(.54f,.30f,.06f),.025f,rot*Quaternion.Euler(14f,0,0),3);
            }
        // armrests: ends always, between seats when asked (long hall rows: no lying down)
        var arms=new List<float>{-half+.03f,half-.03f};
        if(armsEvery>0)for(int i=armsEvery;i<n;i+=armsEvery)arms.Add(-half+.05f+i*.62f);
        foreach(float a in arms)
        {
            m.Tube(benchFrame,P(a,.47f,-.12f),P(a,.66f,-.12f),.016f,8);
            m.RoundedBox(oakSlat,P(a,.675f,-.02f),new Vector3(.06f,.035f,.34f),.012f,rot,2);
        }
        for(int i=0;i<n;i++)
        {
            float a=-half+.05f+(i+.5f)*.62f;var s=P(a,upholstered?.54f:.462f,-.02f);
            Seat(s.x,s.y,s.z,f,label+" "+(i+1));
        }
        var cc=P(0f,.45f,0f);
        bool alongX=Mathf.Abs(u.x)>.5f;
        Col(cc.x-(alongX?half:.3f),cc.x+(alongX?half:.3f),c.y,c.y+.9f,cc.z-(alongX?.3f:half),cc.z+(alongX?.3f:half),"ławka");
        Benches++;
    }
    // a litter bin: stainless drum with a graphite lid and an ashtray-free top opening
    static void Bin(float x,float y,float z)
    {
        var p=new Vector3(x,y,z);
        m.Cylinder(stainless,p,.22f,.80f,20,Quaternion.identity,false);
        m.Cylinder(benchFrame,p+Vector3.up*.80f,.235f,.07f,20,Quaternion.identity,true,true);
        m.Cylinder(benchFrame,p,.235f,.04f,20,Quaternion.identity,true,false);
        m.Cylinder(black,p+Vector3.up*.87f,.12f,.012f,16,Quaternion.identity,true,false);
        Col(x-.24f,x+.24f,y,y+.87f,z-.24f,z+.24f,"kosz");
    }

    // ================================================================ the info desk
    static void InfoDesk()
    {
        if(!oakSlat)FurnitureMaterials();
        var c=new Vector3(0,FFL,7.0f);
        // the counter: an arc 220 deg facing the entrance (south), R 2.55 outside / 1.95 inside, open at the back for the staff
        float r0=1.95f,r1=2.55f,a0=-200f,a1=20f;int n=44;
        Vector3 Pt(float deg,float r,float y)=>c+new Vector3(Mathf.Cos(deg*Mathf.Deg2Rad)*r,y,Mathf.Sin(deg*Mathf.Deg2Rad)*r);
        for(int i=0;i<n;i++)
        {
            float d0=a0+(a1-a0)*i/n,d1=a0+(a1-a0)*(i+1)/n,dm=(d0+d1)*.5f;
            var no=new Vector3(Mathf.Cos(dm*Mathf.Deg2Rad),0,Mathf.Sin(dm*Mathf.Deg2Rad));
            // recessed black plinth with a warm LED line under the walnut (the counter floats)
            Q(black,Pt(d0,r1-.08f,0f),Pt(d1,r1-.08f,0f),Pt(d1,r1-.08f,.12f),Pt(d0,r1-.08f,.12f),no);
            Q(ledWarm,Pt(d0,r1-.06f,.12f),Pt(d1,r1-.06f,.12f),Pt(d1,r1-.06f,.14f),Pt(d0,r1-.06f,.14f),no);
            // walnut front (vertical boards: graphite reveal every 4th segment), the inside face (walnut), the ledge
            Q(walnut,Pt(d0,r1,.14f),Pt(d1,r1,.14f),Pt(d1,r1,1.02f),Pt(d0,r1,1.02f),no);
            if(i%4==0)Q(benchFrame,Pt(d0,r1+.004f,.16f),Pt(d0+.6f,r1+.004f,.16f),Pt(d0+.6f,r1+.004f,1.0f),Pt(d0,r1+.004f,1.0f),no);
            Q(walnut,Pt(d0,r0,0f),Pt(d1,r0,0f),Pt(d1,r0,.74f),Pt(d0,r0,.74f),-no);
            // raised marble ledge (1.02..1.08, 22 cm) and the staff work top (.74..76) inside it
            Q(counterTop,Pt(d0,r1-.22f,1.08f),Pt(d1,r1-.22f,1.08f),Pt(d1,r1+.04f,1.08f),Pt(d0,r1+.04f,1.08f),Vector3.up);
            Q(counterTop,Pt(d0,r1+.04f,1.02f),Pt(d1,r1+.04f,1.02f),Pt(d1,r1+.04f,1.08f),Pt(d0,r1+.04f,1.08f),no);
            Q(counterTop,Pt(d0,r1-.22f,1.02f),Pt(d1,r1-.22f,1.02f),Pt(d1,r1-.22f,1.08f),Pt(d0,r1-.22f,1.08f),-no);
            Q(walnut,Pt(d0,r1-.22f,.76f),Pt(d1,r1-.22f,.76f),Pt(d1,r1-.22f,1.02f),Pt(d0,r1-.22f,1.02f),-no);
            Q(counterTop,Pt(d0,r0-.30f,.76f),Pt(d1,r0-.30f,.76f),Pt(d1,r1-.22f,.76f),Pt(d0,r1-.22f,.76f),Vector3.up);
            Q(counterTop,Pt(d0,r0-.30f,.72f),Pt(d1,r0-.30f,.72f),Pt(d1,r0-.30f,.76f),Pt(d0,r0-.30f,.76f),-no);
        }
        // end caps of the arc
        foreach(float d in new[]{a0,a1})
        {
            var t=new Vector3(-Mathf.Sin(d*Mathf.Deg2Rad),0,Mathf.Cos(d*Mathf.Deg2Rad))*(d==a0?-1f:1f);
            Q(walnut,Pt(d,r0,0f),Pt(d,r1,0f),Pt(d,r1,1.02f),Pt(d,r0,1.02f),t);
        }
        // two monitors and two chairs (Poly Haven dining_chair_02) for the staff
        foreach(float d in new[]{-125f,-55f})
        {
            var p=Pt(d,r0+.05f,.76f);var face=(p-c);face.y=0;face.Normalize();
            float yaw=Mathf.Atan2(face.x,face.z)*Mathf.Rad2Deg;
            m.Cylinder(benchFrame,p,.09f,.012f,16,Quaternion.identity,true);
            m.Box(benchFrame,p+Vector3.up*.18f-face*.02f,new Vector3(.03f,.34f,.03f),Quaternion.Euler(0,yaw,0));
            m.Box(black,p+Vector3.up*.38f,new Vector3(.56f,.34f,.03f),Quaternion.Euler(0,yaw,0));
            m.Box(screenLit(),p+Vector3.up*.38f-face*.0165f,new Vector3(.52f,.30f,.004f),Quaternion.Euler(0,yaw,0));
            var chair=Pt(d,r0-.85f,0f);
            Prop(root,"HotelSobczak/dining_chair_02/dining_chair_02_2k.gltf",chair,yaw+180f);
        }
        Col(c.x-r1,c.x+r1,FFL,FFL+1.08f,c.z-r1,c.z+.3f,"informacja");
        // the totem behind the desk: navy, 2.4 x 3.4 m, both faces: a lit white disc with the navy "i", INFORMACJA / INFORMATION
        float tz=c.z+2.95f,tw=2.4f,th=3.4f;
        RB(signBlue,-tw*.5f,tw*.5f,FFL,FFL+th,tz-.16f,tz+.16f,.05f,true);
        B(benchFrame,-tw*.5f-.02f,tw*.5f+.02f,FFL,FFL+.12f,tz-.18f,tz+.18f);
        var lm=new BraxChampionsMeshBatch();
        foreach(float s in new[]{-1f,1f})
        {
            float fz=tz+s*.16f;var rotD=Quaternion.Euler(s*90f,0,0);
            var dc=new Vector3(0,FFL+2.45f,fz);
            lm.Cylinder(whiteLetters,dc-new Vector3(0,0,s*.0f),.50f,.03f,48,Quaternion.Euler(s<0?-90f:90f,0,0),true,false);
            // the "i": a navy dot and stem on the disc (12 mm proud)
            lm.Cylinder(navy,dc+new Vector3(0,.24f,s*.03f),.075f,.012f,24,Quaternion.Euler(s<0?-90f:90f,0,0),true,false);
            lm.Box(navy,dc+new Vector3(0,-.07f,s*.036f),new Vector3(.13f,.40f,.012f),Quaternion.identity);
            lm.Box(navy,dc+new Vector3(-.035f,.10f,s*.036f),new Vector3(.20f,.06f,.012f),Quaternion.identity);
            lm.Box(navy,dc+new Vector3(0,-.25f,s*.036f),new Vector3(.30f,.05f,.012f),Quaternion.identity);
            BraxChampionsLetters3D.Text(lm,whiteLetters,navy,"INFORMACJA",new Vector3(0,FFL+1.38f,fz),s<0?180f:0f,.25f,.02f,.10f,tw-.3f);
            BraxChampionsLetters3D.Text(lm,whiteLetters,navy,"INFORMATION",new Vector3(0,FFL+1.06f,fz),s<0?180f:0f,.14f,.015f,.10f,tw-.6f);
            lm.Box(whiteLetters,new Vector3(0,FFL+1.82f,fz+s*.006f),new Vector3(tw-.5f,.025f,.012f),Quaternion.identity);
        }
        BraxChampionsLetters3D.Build(lm,root,"D2 informacja totem");Signs++;
        // a ring pendant over the desk (LED halo D 4.2 m on three cables)
        Halo(c.x,FFL+4.6f,c.z,2.1f,4);
    }
    static Material screenMat;
    static Material screenLit(){if(!screenMat)screenMat=S("monitor_screen",new Color(.10f,.18f,.30f),.2f,0f,new Color(.25f,.45f,.75f));return screenMat;}
    // an LED halo ring pendant: navy aluminium ring with a warm LED band underneath, on k cables to the ceiling above
    static void Halo(float x,float y,float z,float r,int k)
    {
        int n=48;var c=new Vector3(x,y,z);
        for(int i=0;i<n;i++)
        {
            float a0=2*Mathf.PI*i/n,a1=2*Mathf.PI*(i+1)/n;
            Vector3 P(float a,float rr,float yy)=>c+new Vector3(Mathf.Cos(a)*rr,yy,Mathf.Sin(a)*rr);
            Q(navy,P(a0,r+.06f,0f),P(a1,r+.06f,0f),P(a1,r+.06f,.10f),P(a0,r+.06f,.10f),new Vector3(Mathf.Cos(a0),0,Mathf.Sin(a0)));
            Q(navy,P(a0,r-.06f,0f),P(a1,r-.06f,0f),P(a1,r-.06f,.10f),P(a0,r-.06f,.10f),-new Vector3(Mathf.Cos(a0),0,Mathf.Sin(a0)));
            Q(navy,P(a0,r-.06f,.10f),P(a1,r-.06f,.10f),P(a1,r+.06f,.10f),P(a0,r+.06f,.10f),Vector3.up);
            Q(ledWarm,P(a0,r-.06f,0f),P(a1,r-.06f,0f),P(a1,r+.06f,0f),P(a0,r+.06f,0f),Vector3.down);
        }
        float top=x*x<HallW*HallW?VaultIn(x)-.3f:Floor1;
        for(int i=0;i<k;i++){float a=2*Mathf.PI*i/k+.3f;var p=c+new Vector3(Mathf.Cos(a)*r,.10f,Mathf.Sin(a)*r);m.Tube(steel,p,new Vector3(p.x,VaultIn(p.x)-.3f,p.z),.006f,6,1);}
    }
    // a ticket machine (biletomat): navy body with rounded edges, a lit touch screen, card reader, ticket slot, "BILETY" head
    static void TicketMachine(float x,float z,float yaw)
    {
        var rot=Quaternion.Euler(0,yaw,0);var p=new Vector3(x,FFL,z);
        Vector3 L(float a,float h,float d)=>p+rot*new Vector3(a,h,d);
        m.RoundedBox(navy,L(0,.95f,0),new Vector3(.70f,1.90f,.45f),.05f,rot,3);
        m.Box(black,L(0,1.30f,-.226f),new Vector3(.50f,.44f,.012f),rot);
        m.Box(screenLit(),L(0,1.30f,-.233f),new Vector3(.46f,.40f,.004f),rot);
        m.Box(stainless,L(-.12f,.95f,-.232f),new Vector3(.14f,.10f,.02f),rot);
        m.Box(black,L(.14f,.80f,-.232f),new Vector3(.20f,.03f,.02f),rot);
        m.RoundedBox(whiteLetters,L(0,2.02f,0),new Vector3(.70f,.22f,.46f),.04f,rot,3);
        var lm=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(lm,navy,navy,"BILETY",L(0,1.96f,-.232f),yaw+180f,.11f,.006f,.10f,.6f);
        BraxChampionsLetters3D.Build(lm,root,"D2 biletomat");
        var c=L(0,.95f,0);Col(c.x-.4f,c.x+.4f,FFL,FFL+2.1f,c.z-.4f,c.z+.4f,"biletomat");
    }
}
