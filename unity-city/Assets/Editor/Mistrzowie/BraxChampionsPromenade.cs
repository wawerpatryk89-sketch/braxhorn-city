using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// PROMENADE KIOSKS K1-K12, rebuilt 10.11 for the owner ("te domki, te budki z jedzeniem, porobić lepsze, premium, bo są
// zjebane ... budki wyposażyć w kebaby, w pizzę ... kebaba musisz stworzyć, tego co się kręci"). The old shells were
// pastel boxes (pink, green, blue walls), a flat roof, a small sticker and a blank wall to the street.
// One premium architecture for all twelve (same footprints as promenade_layout.py, front to the sea = -z):
//   travertine body on a dark granite plinth, graphite steel portal with a lit sign fascia and SOLID 3D letters
//   (HarbourSigns.bytes, ZASADA-SZYLDY-3D), a cantilevered canopy with teak soffit, downlights and an LED edge;
//   food kiosks: an open serving hatch over a marble counter with a teak-slat front and LED plinth;
//   walk-in shops: glass shop front with the door opening; the back (street side) clad in vertical teak slats;
//   inside: marble back counter, steel/teak shelving, menu boards and fridges from the owner's shop pack.
// Equipment per kiosk (BraxChampionsPromenadeEquipment): kebab rotisserie made here (rotating meat cone), the owner's
// pack food (kebab, pizza, burgers, fries, chicken, salmon, fruit, bottles - miasto_sklepy) and Poly Haven props.
// Outdoor seating (marble bistro tables, teak chairs, parasols) in front of every food kiosk.
public static class BraxChampionsPromenade
{
    const string Output="Assets/PlayableGenerated/Mistrzowie";
    static float K=>BraxClaudeOsiedle.K;
    // interior anchors shared with the stock / equipment placement (local nominal metres, origin = floor centre)
    public const float FloorTop=.16f,BackCounterTop=.92f,ShelfDepth=.34f;
    public static readonly float[] ShelfTops={1.42f,1.82f,2.22f};
    public static float BackCounterZ(float d)=>d*.5f-.18f-.32f;
    public static float ShelfZ(float d)=>d*.5f-.18f-ShelfDepth*.5f;
    public static float FrontCounterZ(float d)=>-d*.5f+.40f;
    public const float FrontCounterTop=1.02f;
    public static Material Trav,Marble,Slat,Plinth,Glass,Canvas,Floor,Soffit,Wood,Cushion,Leather;
    public static int Built,Letters3D;

    [Serializable] public sealed class KioskRecord
    {
        public string id,name,sign;
        public float x,z,w,d,h;
        public string style;
        public bool walkIn;
    }

    /// <summary>Real woven fabric (Poly Haven maps, tinted), optional glow for translucent canvas.</summary>
    public static Material Fabric(string id,string slug,Color tint,float metres,Color? glow=null)
    {
        var m=BraxChampionsFoundation.SourceMaterial(id,slug,metres,tint);
        if(glow.HasValue){m.SetColor("_EmissionColor",glow.Value);EditorUtility.SetDirty(m);}
        return m;
    }

    public static void Materials()
    {
        if(!BraxChampionsHarbour.Steel)BraxChampionsHarbour.Materials();
        // white marble body (the owner liked the marble walls of the arena lobby; no brown / beige - "sraczkowate"),
        // graphite fluted fins instead of brown timber, white stucco soffits; pale natural oak only on seats
        Trav=BraxChampionsFoundation.LibraryMarbleWhite("prom_kiosk_white_marble_body",new Color(1.02f,1.02f,1.02f),1.8f);
        Marble=BraxChampionsFoundation.LibraryMarbleWhite("prom_kiosk_marble_tops",Color.white,1.4f);
        Floor=BraxChampionsFoundation.LibraryMarbleWhite("prom_kiosk_floor_marble",new Color(.86f,.86f,.86f),1.0f);
        Slat=BraxChampionsHarbour.Graphite;
        Soffit=BraxChampionsFoundation.SourceMaterial("prom_kiosk_white_stucco_soffit","white_stucco_02",1.2f,new Color(1.02f,1.0f,.97f));
        Wood=BraxChampionsFoundation.SourceMaterial("prom_silver_teak_seats","oak_wood_planks",.7f,new Color(.975f,1.32f,1.76f));
        Plinth=BraxChampionsFoundation.SourceMaterial("prom_kiosk_granite_plinth","granite_tile",1.2f,new Color(.42f,.43f,.44f));
        // owner 10.11 10:27 (OBIEKTY_Z_KODU_PRAWDZIWE_MATERIALY): cloth is real cloth, seats are wood or upholstery.
        // Parasol canvas = Poly Haven terlenka woven fabric (white; a faint glow = sun through the cloth, so the
        // underside does not read brown-grey); seat cushions = cotton_jersey; bar stools = black Leather037.
        Canvas=Fabric("prom_parasol_canvas_white","terlenka",new Color(1.21f,1.36f,1.53f),.9f,new Color(.42f,.42f,.40f));
        Cushion=Fabric("prom_seat_cushion_white","cotton_jersey",new Color(1.20f,1.36f,1.46f),.6f);
        Leather=BraxChampionsFoundation.LibraryLeather("prom_bar_stool_black_leather",new Color(.33f,.55f,.70f));
        Glass=BraxChampionsHarbour.Surface("kiosk_clear_glass",new Color(.84f,.92f,.96f,.22f),.05f,0f,null,true);
    }

    /// <summary>Back-wall shelf runs, interrupted above tall equipment on the back counter (kebab, rotisserie, espresso, fryers).</summary>
    public static List<(float,float)> ShelfSpans(KioskRecord p)
    {
        float lo=-p.w*.5f+.18f+.04f,hi=p.w*.5f-.18f-.04f;
        var gaps=BraxChampionsPromenadeEquipment.TallOnBackCounter(p).OrderBy(g=>g.Item1).ToList();
        var spans=new List<(float,float)>();float x=lo;
        foreach(var (g0,g1) in gaps){if(g0-.05f>x+.2f)spans.Add((x,g0-.05f));x=Mathf.Max(x,g1+.05f);}
        if(hi>x+.2f)spans.Add((x,hi));
        return spans;
    }

    static void Shell(BraxChampionsMeshBatch m,Transform col,KioskRecord p)
    {
        float w=p.w,d=p.d,h=p.h,hw=w*.5f,hd=d*.5f,t=.18f,y0=FloorTop;
        var g=BraxChampionsHarbour.Graphite;var led=BraxChampionsHarbour.Led;var steel=BraxChampionsHarbour.Steel;
        float fascia0=h-.78f;                                   // sign fascia band from here to the roof
        // granite plinth band outside, travertine floor inside
        m.BoxMinMax(Plinth,-hw-.04f,hw+.04f,0f,y0,-hd-.04f,hd+.04f);
        m.BoxMinMax(Floor,-hw+t,hw-t,y0,y0+.012f,-hd+.05f,hd-t);
        // side and back walls: travertine
        m.BoxMinMax(Trav,-hw,-hw+t,y0,h-.20f,-hd,hd);
        m.BoxMinMax(Trav,hw-t,hw,y0,h-.20f,-hd,hd);
        m.BoxMinMax(Trav,-hw+t,hw-t,y0,h-.20f,hd-t,hd);
        BraxChampionsMeshBatch.Collider(col,p.id+" ściana lewa",new Vector3(-hw+t*.5f,(y0+h)*.5f,0),new Vector3(t,h,d));
        BraxChampionsMeshBatch.Collider(col,p.id+" ściana prawa",new Vector3(hw-t*.5f,(y0+h)*.5f,0),new Vector3(t,h,d));
        BraxChampionsMeshBatch.Collider(col,p.id+" ściana tylna",new Vector3(0,(y0+h)*.5f,hd-t*.5f),new Vector3(w,h,t));
        // street side: vertical teak slats over the travertine, framed by graphite, a slim LED line under the roof
        for(float x=-hw+.30f;x<=hw-.30f;x+=.14f)m.BoxMinMax(Slat,x-.035f,x+.035f,y0+.45f,h-.55f,hd,hd+.045f);
        m.BoxMinMax(g,-hw,hw,y0+.40f,y0+.45f,hd,hd+.06f);
        m.BoxMinMax(g,-hw,hw,h-.55f,h-.50f,hd,hd+.06f);
        // roof slab with a graphite edge, a little overhang
        m.BoxMinMax(Trav,-hw-.10f,hw+.10f,h-.20f,h-.06f,-hd-.10f,hd+.10f);
        m.BoxMinMax(g,-hw-.12f,hw+.12f,h-.06f,h,-hd-.12f,hd+.12f);
        // front portal: graphite jambs and the sign fascia
        m.BoxMinMax(g,-hw-.06f,-hw+.22f,y0,fascia0,-hd-.06f,-hd+.10f);
        m.BoxMinMax(g,hw-.22f,hw+.06f,y0,fascia0,-hd-.06f,-hd+.10f);
        m.BoxMinMax(g,-hw-.06f,hw+.06f,fascia0,h-.20f,-hd-.10f,-hd+.10f);
        m.BoxMinMax(steel,-hw-.06f,hw+.06f,fascia0-.04f,fascia0,-hd-.10f,-hd+.10f);
        // cantilevered canopy over the counter (1.6 m to the sea) with a teak soffit, downlights and a warm LED edge
        float c0=fascia0-.18f,c1=fascia0-.04f,cz0=-hd-1.60f;
        m.BoxMinMax(g,-hw-.15f,hw+.15f,c0,c1,cz0,-hd-.10f);
        m.BoxMinMax(Soffit,-hw-.10f,hw+.10f,c0-.012f,c0,cz0+.05f,-hd-.12f);
        m.BoxMinMax(led,-hw-.12f,hw+.12f,c0+.02f,c0+.05f,cz0-.006f,cz0);
        for(float x=-hw+.6f;x<=hw-.5f;x+=1.1f)m.Cylinder(led,new Vector3(x,c0-.022f,cz0+.75f),.07f,.01f,12,Quaternion.identity,false,true);
        // interior: marble back counter with teak base, three steel/teak shelves above it, marble splash back
        float bz=BackCounterZ(d);
        m.BoxMinMax(Slat,-hw+t+.05f,hw-t-.05f,y0,BackCounterTop-.04f,bz-.30f,bz+.30f);
        m.BoxMinMax(BraxChampionsHarbour.Led,-hw+t+.08f,hw-t-.08f,y0+.05f,y0+.065f,bz-.315f,bz-.30f);
        m.BoxMinMax(Marble,-hw+t+.02f,hw-t-.02f,BackCounterTop-.04f,BackCounterTop,bz-.32f,bz+.32f);
        m.BoxMinMax(Marble,-hw+t,hw-t,BackCounterTop,ShelfTops[0]-.10f,hd-t-.02f,hd-t);
        float sz=ShelfZ(d);
        foreach(var (sx0,sx1) in ShelfSpans(p))
            foreach(float top in ShelfTops)
            {
                m.BoxMinMax(Marble,sx0,sx1,top-.035f,top,sz-ShelfDepth*.5f,sz+ShelfDepth*.5f);
                m.BoxMinMax(steel,sx0,sx1,top-.05f,top-.035f,sz-ShelfDepth*.5f,sz-ShelfDepth*.5f+.02f);
                if(sx1-sx0>.3f)m.BoxMinMax(led,sx0+.05f,sx1-.05f,top-.052f,top-.05f,sz-.05f,sz+.05f);
            }
        // ceiling inside
        m.BoxMinMax(Trav,-hw+t,hw-t,h-.24f,h-.20f,-hd,hd-t);
        if(p.walkIn)
        {
            // glass shop front with a 1.5 m door opening in the middle
            float door=1.5f,each=(w-.28f*2f-door)*.5f;
            foreach(float s in new[]{-1f,1f})
            {
                float xc=s*(door*.5f+each*.5f);
                m.BoxMinMax(Glass,xc-each*.5f,xc+each*.5f,y0+.30f,fascia0-.06f,-hd+.02f,-hd+.04f);
                m.BoxMinMax(g,xc-each*.5f,xc+each*.5f,y0,y0+.30f,-hd,-hd+.08f);
                m.BoxMinMax(g,s*door*.5f-.04f,s*door*.5f+.04f,y0,fascia0,-hd,-hd+.08f);
                BraxChampionsMeshBatch.Collider(col,p.id+" witryna "+s,new Vector3(xc,(y0+fascia0)*.5f,-hd+.04f),new Vector3(each,fascia0-y0,.08f));
            }
        }
        else
        {
            // open serving hatch over the counter: teak-slat counter front, marble top, LED plinth; roller shutter box
            float fz=FrontCounterZ(d);
            m.BoxMinMax(Trav,-hw+t,hw-t,y0,FrontCounterTop-.05f,fz-.24f,fz+.24f);
            for(float x=-hw+.30f;x<=hw-.30f;x+=.12f)m.BoxMinMax(Slat,x-.03f,x+.03f,y0+.10f,FrontCounterTop-.07f,fz-.27f,fz-.24f);
            m.BoxMinMax(led,-hw+.25f,hw-.25f,y0+.06f,y0+.08f,fz-.27f,fz-.25f);
            m.BoxMinMax(Marble,-hw+t-.02f,hw-t+.02f,FrontCounterTop-.05f,FrontCounterTop,fz-.34f,fz+.26f);
            m.BoxMinMax(g,-hw+.22f,hw-.22f,fascia0-.22f,fascia0,-hd+.02f,-hd+.20f);
            BraxChampionsMeshBatch.Collider(col,p.id+" lada",new Vector3(0,(y0+FrontCounterTop)*.5f,fz),new Vector3(w-.36f,FrontCounterTop-y0,.6f));
        }
        Built++;
    }

    // ---------------- outdoor dining furniture (kiosk terraces and the dining islands between the kiosks) ----------------
    // Owner 10.11 10:27: "co ma być drewnem, ma być drewnem ... obite ... materiałem": graphite powder-coated frames,
    // silver-teak slats, white cotton cushions, white marble table tops, woven white parasol canvas.

    /// <summary>Box given by local min/max in a frame at origin o turned by yaw (nominal metres).</summary>
    static void Lbox(BraxChampionsMeshBatch m,Material mat,Vector3 o,Quaternion r,float x0,float x1,float y0,float y1,float z0,float z1)
        =>m.Box(mat,o+r*new Vector3((x0+x1)*.5f,(y0+y1)*.5f,(z0+z1)*.5f),new Vector3(x1-x0,y1-y0,z1-z0),r);

    /// <summary>Dining chair standing at floor point f; the sitter looks toward (sin yaw, 0, cos yaw).</summary>
    public static void Chair(BraxChampionsMeshBatch m,Vector3 f,float yaw)
    {
        var g=BraxChampionsHarbour.Graphite;var r=Quaternion.Euler(0,yaw,0);
        foreach(float lx in new[]{-.2f,.2f})foreach(float lz in new[]{-.2f,.2f})Lbox(m,g,f,r,lx-.015f,lx+.015f,0,.42f,lz-.015f,lz+.015f);
        Lbox(m,g,f,r,-.22f,.22f,.39f,.42f,-.22f,.22f);                                   // seat frame
        for(int k=0;k<4;k++){float z0=-.22f+k*.112f;Lbox(m,Wood,f,r,-.225f,.225f,.42f,.445f,z0,z0+.10f);}
        Lbox(m,Cushion,f,r,-.205f,.205f,.445f,.495f,-.205f,.205f);
        foreach(float lx in new[]{-.2f,.2f})Lbox(m,g,f,r,lx-.015f,lx+.015f,.42f,.88f,-.23f,-.20f);   // back posts
        foreach(float by in new[]{.60f,.74f})Lbox(m,Wood,f,r,-.22f,.22f,by,by+.10f,-.229f,-.201f);  // back slats
    }

    /// <summary>Round bistro table: graphite foot and column, white marble top at .76 m.</summary>
    public static void BistroTable(BraxChampionsMeshBatch m,Vector3 f,float topRadius)
    {
        var g=BraxChampionsHarbour.Graphite;
        m.Cylinder(g,f,.26f,.03f,16);
        m.Cylinder(g,f+Vector3.up*.03f,.04f,.70f,10);
        m.Cylinder(Marble,f+Vector3.up*.73f,topRadius,.03f,24,Quaternion.identity,true,true);
    }

    /// <summary>Round 16-rib parasol through a table centre (pole from the table top), woven white canvas.</summary>
    public static void Parasol(BraxChampionsMeshBatch m,Vector3 tableTop,float radius)
    {
        m.Cylinder(BraxChampionsHarbour.Steel,tableTop,.022f,1.70f,10);
        const int ribs=16;float eave=1.52f,top=1.84f;var apex=tableTop+Vector3.up*top;
        for(int j=0;j<ribs;j++)
        {
            float a0=2f*Mathf.PI*j/ribs,a1=2f*Mathf.PI*(j+1)/ribs,s0=j%2==0?0f:.04f,s1=(j+1)%2==0?0f:.04f;
            var p0=tableTop+new Vector3(radius*Mathf.Cos(a0),eave-s0,radius*Mathf.Sin(a0));
            var p1=tableTop+new Vector3(radius*Mathf.Cos(a1),eave-s1,radius*Mathf.Sin(a1));
            m.Triangle(Canvas,apex,p1,p0);m.Triangle(Canvas,apex,p0,p1);                    // two-sided cloth
            m.Quad(Canvas,p0,p0-Vector3.up*.08f,p1-Vector3.up*.08f,p1);m.Quad(Canvas,p1,p1-Vector3.up*.08f,p0-Vector3.up*.08f,p0);
        }
        m.Cylinder(BraxChampionsHarbour.Steel,apex-Vector3.up*.02f,.04f,.08f,10);
    }

    // two bistro tables with two chairs each and a parasol, in front of every food kiosk
    static void Terrace(BraxChampionsMeshBatch m,Transform col,KioskRecord p)
    {
        float zf=-p.d*.5f-3.6f,y=.16f;                          // in front of the canopy, in the 5 m service strip
        foreach(float x in new[]{-p.w*.5f+.9f,p.w*.5f-.9f})
        {
            BistroTable(m,new Vector3(x,y,zf),.38f);
            Chair(m,new Vector3(x+.66f,y,zf),-90f);Chair(m,new Vector3(x-.66f,y,zf),90f);
            Parasol(m,new Vector3(x,y+.76f,zf),1.1f);
            BraxChampionsMeshBatch.Collider(col,p.id+" stolik "+x.ToString("0.0"),new Vector3(x,y+.38f,zf),new Vector3(1.8f,.76f,.8f));
        }
    }

    static bool Food(string style)=>style!="beach_shop"&&style!="souvenirs"&&style!="rental"&&style!="info";

    public static void Build(Transform foundation,BraxChampionsFoundation.Pavilion[] approved)
    {
        if(!foundation||approved==null||approved.Length!=12)
            throw new InvalidOperationException("12 exactly reference-approved promenade pavilions required");
        string[] names={"K1","K2","K3","K4","K5","K6","K7","K8","K9","K10","K11","K12"};
        if(!approved.Select(x=>x.id).SequenceEqual(names))
            throw new InvalidOperationException("Promenade K1..K12 order is inconsistent with owner's reference");
        Directory.CreateDirectory(Output+"/Meshes/Harbour");Directory.CreateDirectory(Output+"/Materials");
        Materials();Built=Letters3D=0;
        var baseObj=new GameObject("MIASTO MISTRZÓW | PROMENADA 12 SHOPS K1..K12 | premium 10.11").transform;baseObj.SetParent(foundation,false);
        foreach(var a in approved)
        {
            var p=new KioskRecord{id=a.id,name=a.name,sign=a.sign,x=a.x,z=a.z,w=a.w,d=a.d,h=a.h,style=a.style,walkIn=a.walkIn};
            if(p.w<4.9f||p.h<3.2f)throw new InvalidOperationException("Invalid promenade shop dimensions: "+p.id);
            var root=new GameObject(p.id+" | "+p.name+" | full 2K PBR shop shell").transform;root.SetParent(baseObj,false);
            root.localPosition=new Vector3(p.x,0f,p.z)*K;
            var col=new GameObject("kolizje").transform;col.SetParent(root,false);
            var m=new BraxChampionsMeshBatch();
            Shell(m,col,p);
            if(Food(p.style))Terrace(m,col,p);
            m.Build(root,p.id+" architektura",Output+"/Meshes/Harbour");
            // solid 3D name letters on the fascia, lit warm white
            float fascia0=p.h-.78f;
            if(BraxChampionsHarbour.Letters(root,"KIOSK_"+p.id,new Vector3(0,fascia0+.17f,-p.d*.5f-.105f),180f,1f))Letters3D++;
            // interior lamp (vertex light, no shadows)
            var lamp=new GameObject(p.id+" interior soft LED").AddComponent<Light>();lamp.transform.SetParent(root,false);
            lamp.transform.localPosition=new Vector3(0,(p.h-.5f)*K,0);lamp.type=LightType.Point;lamp.range=5.5f*K;lamp.intensity=.6f;
            lamp.shadows=LightShadows.None;lamp.renderMode=LightRenderMode.ForceVertex;lamp.color=new Color(1f,.88f,.74f);
            BraxChampionsFoundation.Optional("wyposażenie "+p.id,()=>BraxChampionsPromenadeEquipment.Fit(root,p));
        }
        int products=0;
        BraxChampionsFoundation.Optional("towar Poly Haven w budkach",()=>products=BraxChampionsPromenadeStock.Populate(baseObj,approved));
        Debug.Log("CHAMPIONS_PROMENADE_K1_K12_PREMIUM_10_11 built="+Built+" letters3D="+Letters3D+" polyHavenStock="+products
            +" equipment="+BraxChampionsPromenadeEquipment.Summary());
    }
}
