using System;
using UnityEditor;
using UnityEngine;

// Promenade furniture, owner 10.11 09:01-09:06: "drzewa posadzić ... palmy posadzić ... promenady ładne, ekskluzywne".
// The seaside palm row (foundation: promenade_layout.PALM_BEDS, the owner's thick-trunk palms planted in 3.2 m beds
// flush with the paving) gets granite kerbs and a warm uplight at every trunk; between the beds alternately a teak
// bench on cast frames facing the sea (with a stainless litter bin) or a slim promenade lamp. The timber boardwalks over
// the beach are part of the beach club (BraxChampionsBeachFurnishings).
// All parts in one combined mesh per material (BraxChampionsMeshBatch).
public static class BraxChampionsPromenadeFurniture
{
    const string Output="Assets/PlayableGenerated/Mistrzowie";
    const float Y=.16f;
    public static Material Teak,Kerb,Deck;
    public static int Benches,Lamps,Beds,Islands,Chairs,Stools,Planters,Flowers,BikeHoops,Chargers,RepairStands,Drinking,Telescopes,Bins;

    public static void Materials()
    {
        // silver-grey aged teak (oak_wood_planks alone is orange 162,117,84 -> ~158,154,148): no brown on the promenade
        Teak=BraxChampionsFoundation.SourceMaterial("prom_bench_silver_teak_slats","oak_wood_planks",.9f,new Color(.975f,1.32f,1.76f));
        Kerb=BraxChampionsFoundation.SourceMaterial("prom_bed_granite_kerb","granite_tile",1.2f,new Color(.70f,.70f,.69f));
        // the same silver-grey weathered boards as the pontoons and the beach boardwalks
        Deck=BraxChampionsFoundation.LibraryAcg("prom_beach_boardwalk_deck","Planks040",new Color(.94f,1.08f,1.18f),2.0f);
    }

    // Owner 10.11 15:53 (render of a quay bench from the walk: "coś z niej wystaje ... jakby jakiś słupek z drugiej strony
    // od morza"): the rear legs ran up THROUGH the backrest (8 cm posts crossing the slats, standing proud of them on the
    // back). Every bench of the promenade and the quays is now one design: two cast graphite END FRAMES outside the slats
    // (front post with the armrest, rear post up to the top of the backrest, seat rail, armrest), teak seat and back slats
    // spanning between the frames (nothing crosses them), two bearers under the seat. f = the way the bench faces.
    public static void BenchFramed(BraxChampionsMeshBatch m,Transform colliders,float x,float z,Vector2 f,string label)
    {
        f=f.normalized;var u=new Vector2(-f.y,f.x);                     // along the seat
        // local a (along), d (depth, front d<0 toward f), y
        void B(Material mat,float a0,float a1,float y0,float y1,float d0,float d1)
        {
            Vector2 p0=new Vector2(x,z)+u*a0-f*d0,p1=new Vector2(x,z)+u*a1-f*d1;
            m.BoxMinMax(mat,Mathf.Min(p0.x,p1.x),Mathf.Max(p0.x,p1.x),Y+y0,Y+y1,Mathf.Min(p0.y,p1.y),Mathf.Max(p0.y,p1.y));
        }
        var g=BraxChampionsHarbour.Graphite;
        const float half=.86f,fw=.04f;                                   // slats end at +-.86, frames .86..90
        foreach(float s in new[]{-1f,1f})
        {
            float a0=s<0?-half-fw:half,a1=s<0?-half:half+fw;
            B(g,a0,a1,0f,.66f,-.27f,-.21f);                               // front post, carries the armrest
            B(g,a0,a1,0f,.88f,.19f,.25f);                                 // rear post to the top of the backrest
            B(g,a0,a1,.40f,.44f,-.21f,.19f);                              // seat rail between the posts
            B(g,a0,a1,.62f,.66f,-.21f,.19f);                              // armrest between the posts
            B(g,a0-.005f*s,a1+.005f*s,.62f,.66f,-.31f,-.27f);             // armrest nose in front of the post
        }
        foreach(float s in new[]{-1f,1f})B(g,s*.60f-.02f,s*.60f+.02f,.40f,.44f,-.27f,.19f);   // bearers under the seat
        for(int i=0;i<6;i++){float d0=-.29f+i*.083f;B(Teak,-half,half,.44f,.475f,d0,d0+.07f);}
        for(int i=0;i<4;i++){float y0=.52f+i*.09f;B(Teak,-half,half,y0,y0+.075f,.155f,.19f);}   // back slats on the posts' faces
        // stainless litter bin beside the bench
        var bp=new Vector2(x,z)+u*1.35f;var bin=new Vector3(bp.x,Y,bp.y);
        m.Cylinder(BraxChampionsHarbour.Steel,bin,.22f,.82f,16,Quaternion.identity,false);
        m.Cylinder(g,bin+Vector3.up*.82f,.235f,.06f,16);
        m.Cylinder(g,bin,.235f,.05f,16);
        bool alongX=Mathf.Abs(u.x)>.5f;
        BraxChampionsMeshBatch.Collider(colliders,"ławka "+label,new Vector3(x,Y+.45f,z),alongX?new Vector3(1.85f,.9f,.6f):new Vector3(.6f,.9f,1.85f));
        BraxChampionsMeshBatch.Collider(colliders,"kosz "+label,new Vector3(bin.x,Y+.42f,bin.z),new Vector3(.46f,.84f,.46f));
        Benches++;
    }
    // faces the sea (-z): seat along x, backrest on the land side
    static void Bench(BraxChampionsMeshBatch m,Transform colliders,float x,float z)=>BenchFramed(m,colliders,x,z,new Vector2(0,-1),x.ToString("0"));
    // Owner 10.11 14:56-14:57: the benches of the passenger quays face the ship (face +1 = east, -1 = west), seat along z
    static void QuayBench(BraxChampionsMeshBatch m,Transform colliders,float x,float z,float face)=>BenchFramed(m,colliders,x,z,new Vector2(face,0),"nabrzeża "+z.ToString("0"));

    public static void Lamp(BraxChampionsMeshBatch m,Transform colliders,float x,float z)
    {
        var g=BraxChampionsHarbour.Graphite;var b=new Vector3(x,Y,z);
        m.Cylinder(g,b,.16f,.30f,16,Quaternion.identity,true,false,.11f);
        m.Cylinder(g,b+Vector3.up*.30f,.075f,5.1f,14,Quaternion.identity,true,false,.055f);
        m.Cylinder(BraxChampionsHarbour.Steel,b+Vector3.up*2.9f,.078f,.10f,14);   // ring 1 cm proud of the tapered pole
        // flat round luminaire with a warm LED disc underneath
        m.Cylinder(g,b+Vector3.up*5.36f,.42f,.10f,24,Quaternion.identity,true,true,.40f);
        m.Cylinder(BraxChampionsHarbour.Led,b+Vector3.up*5.345f,.34f,.016f,24,Quaternion.identity,false,true);
        BraxChampionsMeshBatch.Collider(colliders,"latarnia "+x.ToString("0"),new Vector3(x,Y+2.7f,z),new Vector3(.22f,5.4f,.22f));
        Lamps++;
    }

    // ---- owner 10.11 18:12, the promenade board ("donice / zieleń", "kosze", "stojaki rowerowe", "stacje naprawy rowerów",
    // "źródełka z wodą", "ładowarki e-bike", "punkty widokowe"): promenade_layout.LINE_FURNITURE ----

    // granite planter 2.0 x 0.9 x 0.55 m, lawn inside, the owner board's flowers (Poly Haven flower_empodium) on it
    public static void Planter(BraxChampionsMeshBatch m,Transform colliders,Transform models,float x,float z,string flower="flower_empodium")
    {
        float x0=x-1.0f,x1=x+1.0f,z0=z-.45f,z1=z+.45f,t=.08f,top=Y+.55f,soil=Y+.47f;
        m.BoxMinMax(Kerb,x0,x1,Y,top,z0,z0+t);m.BoxMinMax(Kerb,x0,x1,Y,top,z1-t,z1);
        m.BoxMinMax(Kerb,x0,x0+t,Y,top,z0+t,z1-t);m.BoxMinMax(Kerb,x1-t,x1,Y,top,z0+t,z1-t);
        m.BoxMinMax(Lawn(),x0+t,x1-t,Y,soil,z0+t,z1-t);
        BraxChampionsMeshBatch.Collider(colliders,"donica "+x.ToString("0"),new Vector3(x,Y+.275f,z),new Vector3(2.0f,.55f,.9f));
        // two staggered rows of the original flower set (5 plants each), the second turned round
        Flower(models,flower,new Vector3(x-.08f,soil,z-.17f),1.62f);
        Flower(models,flower,new Vector3(x+.08f,soil,z+.17f),1.62f,180f);
        Planters++;
    }
    static Material lawn;
    public static Material Lawn()
    {
        if(lawn)return lawn;
        lawn=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/grass.mat");
        if(!lawn)lawn=BraxChampionsHarbour.Surface("prom_planter_lawn",new Color(.30f,.46f,.20f),.85f,0f);
        return lawn;
    }
    // one flower model scaled so its longest side is `length` (nominal), longest side along x, standing on `at`
    public static void Flower(Transform models,string id,Vector3 at,float length,float yaw=0f)
    {
        var src=BraxChampionsPromenadeStock.Flower(id);if(!src)return;
        var o=(GameObject)UnityEngine.Object.Instantiate(src);o.name="kwiaty "+id;
        o.transform.SetParent(models,false);
        var b=BraxRegion.Bounds(o);float longest=Mathf.Max(b.size.x,b.size.z);
        if(longest<1e-4f){UnityEngine.Object.DestroyImmediate(o);return;}
        if(b.size.z>b.size.x)yaw+=90f;
        o.transform.localRotation=Quaternion.Euler(0,yaw,0);
        o.transform.localScale=o.transform.localScale*(length*K/longest);
        b=BraxRegion.Bounds(o);
        o.transform.position+=models.TransformPoint(at*K)-new Vector3(b.center.x,b.min.y,b.center.z);
        foreach(var c in o.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        // small plants: drawn up close only (about 25 m for a 40 cm plant), never a distant cost on the phone
        var lod=o.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.015f,o.GetComponentsInChildren<Renderer>(true))});lod.RecalculateBounds();
        Flowers++;
    }
    static float K=>BraxClaudeOsiedle.K;

    // stainless litter bin on its own
    static void Bin(BraxChampionsMeshBatch m,Transform colliders,float x,float z)
    {
        var g=BraxChampionsHarbour.Graphite;var bin=new Vector3(x,Y,z);
        m.Cylinder(BraxChampionsHarbour.Steel,bin,.22f,.82f,16,Quaternion.identity,false);
        m.Cylinder(g,bin+Vector3.up*.82f,.235f,.06f,16);m.Cylinder(g,bin,.235f,.05f,16);
        BraxChampionsMeshBatch.Collider(colliders,"kosz "+x.ToString("0"),new Vector3(x,Y+.42f,z),new Vector3(.46f,.84f,.46f));
        Bins++;
    }

    // five stainless Sheffield hoops (bikes park across the line) and an e-bike charging post at the east end
    static void BikeRack(BraxChampionsMeshBatch m,Transform colliders,float x,float z)
    {
        var st=BraxChampionsHarbour.Steel;const float r=.024f,h=.80f,half=.34f,bend=.10f;
        for(int i=0;i<5;i++)
        {
            float hx=x-1.8f+i*.9f;
            Vector3 a=new Vector3(hx,Y-.02f,z-half),b=new Vector3(hx,Y+h-bend,z-half),c=new Vector3(hx,Y+h,z-half+bend);
            Vector3 d=new Vector3(hx,Y+h,z+half-bend),e=new Vector3(hx,Y+h-bend,z+half),f=new Vector3(hx,Y-.02f,z+half);
            m.Tube(st,a,b,r,10);m.Tube(st,b,c,r,10);m.Tube(st,c,d,r,10);m.Tube(st,d,e,r,10);m.Tube(st,e,f,r,10);
            BraxChampionsMeshBatch.Collider(colliders,"stojak rowerowy "+hx.ToString("0.0"),new Vector3(hx,Y+.40f,z),new Vector3(.06f,.80f,.72f));
            BikeHoops++;
        }
        // e-bike charger: graphite post, warm LED ring, two stainless socket covers facing the bikes
        var g=BraxChampionsHarbour.Graphite;float cx=x+2.75f;
        m.BoxMinMax(g,cx-.13f,cx+.13f,Y,Y+1.10f,z-.13f,z+.13f);
        m.BoxMinMax(BraxChampionsHarbour.Led,cx-.13f,cx+.13f,Y+1.10f,Y+1.14f,z-.13f,z+.13f);     // LED band flush with the post
        m.BoxMinMax(g,cx-.13f,cx+.13f,Y+1.14f,Y+1.25f,z-.13f,z+.13f);
        foreach(float s in new[]{-1f,1f})m.BoxMinMax(st,cx-.09f,cx+.09f,Y+.78f,Y+.96f,z+s*.13f-(s<0?.02f:0f),z+s*.13f+(s>0?.02f:0f));
        BraxChampionsMeshBatch.Collider(colliders,"ładowarka e-bike "+cx.ToString("0"),new Vector3(cx,Y+.625f,z),new Vector3(.26f,1.25f,.26f));
        Chargers++;
    }

    // bike repair stand: post with a hanger arm, steel tool cables, a floor pump with a gauge
    static void BikeRepair(BraxChampionsMeshBatch m,Transform colliders,float x,float z)
    {
        var g=BraxChampionsHarbour.Graphite;var st=BraxChampionsHarbour.Steel;
        m.BoxMinMax(g,x-.09f,x+.09f,Y,Y+1.55f,z-.09f,z+.09f);
        m.BoxMinMax(g,x-.09f,x+.09f,Y+1.47f,Y+1.55f,z-.55f,z-.09f);                 // hanger arm toward the walk
        foreach(float dx in new[]{-.07f,.07f})m.Tube(st,new Vector3(x+dx,Y+1.47f,z-.50f),new Vector3(x+dx,Y+1.38f,z-.50f),.012f,8);
        for(int i=0;i<4;i++)m.Tube(st,new Vector3(x-.06f+i*.04f,Y+1.20f,z-.09f),new Vector3(x-.06f+i*.04f,Y+.95f,z-.12f),.004f,6);
        var pump=new Vector3(x+.45f,Y,z);
        m.Cylinder(g,pump,.10f,.04f,12);m.Cylinder(st,pump+Vector3.up*.04f,.035f,.95f,10);
        m.Cylinder(BraxChampionsPromenade.Marble,pump+new Vector3(0,.60f,-.03f),.045f,.025f,16,Quaternion.Euler(-90,0,0));  // gauge toward the walk
        BraxChampionsMeshBatch.Collider(colliders,"stacja naprawy rowerów "+x.ToString("0"),new Vector3(x+.2f,Y+.78f,z-.2f),new Vector3(.7f,1.55f,.6f));
        RepairStands++;
    }

    // drinking fountain: granite plinth, graphite column, stainless bowl with a spout, a low dog bowl
    static void DrinkingFountain(BraxChampionsMeshBatch m,Transform colliders,float x,float z)
    {
        var g=BraxChampionsHarbour.Graphite;var st=BraxChampionsHarbour.Steel;var b=new Vector3(x,Y,z);
        m.Cylinder(Kerb,b,.32f,.06f,20);
        m.Cylinder(g,b+Vector3.up*.06f,.13f,.84f,16,Quaternion.identity,false);
        m.Cylinder(st,b+Vector3.up*.90f,.24f,.07f,24,Quaternion.identity,true,true,.27f);
        m.Tube(st,b+new Vector3(0,.97f,.06f),b+new Vector3(0,1.03f,-.06f),.012f,8);
        m.Cylinder(st,b+new Vector3(0,0f,-.52f),.13f,.12f,16);                           // dog bowl on the paving
        BraxChampionsMeshBatch.Collider(colliders,"źródełko "+x.ToString("0"),new Vector3(x,Y+.5f,z-.1f),new Vector3(.64f,1.0f,.84f));
        Drinking++;
    }

    // coin telescope on the sea edge, looking out to sea (-z)
    static void Telescope(BraxChampionsMeshBatch m,Transform colliders,float x,float z)
    {
        var g=BraxChampionsHarbour.Graphite;var st=BraxChampionsHarbour.Steel;var b=new Vector3(x,Y,z);
        m.Cylinder(g,b,.24f,.05f,20);m.Cylinder(g,b+Vector3.up*.05f,.07f,1.02f,14);
        m.BoxMinMax(g,x-.20f,x+.20f,Y+1.07f,Y+1.12f,z-.06f,z+.06f);                     // yoke
        foreach(float s in new[]{-1f,1f})m.BoxMinMax(g,x+s*.20f-.02f,x+s*.20f+.02f,Y+1.12f,Y+1.36f,z-.05f,z+.05f);
        m.Box(st,new Vector3(x,Y+1.30f,z),new Vector3(.34f,.24f,.42f));
        foreach(float s in new[]{-1f,1f})
            m.Cylinder(g,new Vector3(x+s*.07f,Y+1.33f,z+.21f),.035f,.09f,12,Quaternion.Euler(90,0,0));    // eyepieces to the land side
        m.Cylinder(g,new Vector3(x,Y+1.30f,z-.21f),.11f,.05f,16,Quaternion.Euler(-90,0,0));            // objective hood to the sea
        BraxChampionsMeshBatch.Collider(colliders,"luneta "+x.ToString("0"),new Vector3(x,Y+.7f,z),new Vector3(.48f,1.4f,.48f));
        Telescopes++;
    }

    static void Bed(BraxChampionsMeshBatch m,BraxChampionsFoundation.PalmBed b)
    {
        float x0=b.x-b.w*.5f,x1=b.x+b.w*.5f,z0=b.z-b.d*.5f,z1=b.z+b.d*.5f,k=.14f,h=.12f;
        m.BoxMinMax(Kerb,x0-k,x1+k,Y,Y+h,z0-k,z0);
        m.BoxMinMax(Kerb,x0-k,x1+k,Y,Y+h,z1,z1+k);
        m.BoxMinMax(Kerb,x0-k,x0,Y,Y+h,z0,z1);
        m.BoxMinMax(Kerb,x1,x1+k,Y,Y+h,z0,z1);
        // warm uplight at the seaward edge of the bed, aimed at the trunk
        m.BoxMinMax(BraxChampionsHarbour.Graphite,b.x-.16f,b.x+.16f,Y,Y+.05f,z0+.18f,z0+.42f);
        m.BoxMinMax(BraxChampionsHarbour.Led,b.x-.11f,b.x+.11f,Y+.05f,Y+.058f,z0+.22f,z0+.38f);
        Beds++;
    }

    // Owner 10.11 10:28: "między budkami ... krzesełka obok, stanowiska do jedzenia, palmy posadzić". A dining island
    // in every gap between two kiosks (promenade_layout.DINING): a silver-teak deck 12 cm high with a granite edge,
    // three marble bistro tables with four chairs and a parasol each, two standing counters with leather stools at
    // the deck ends, and a flush bed with the owner's thick palm (planted by the foundation) beyond each end.
    static void Island(BraxChampionsMeshBatch m,Transform colliders,BraxChampionsFoundation.Dining d)
    {
        float x0=d.x-d.w*.5f,x1=d.x+d.w*.5f,z0=d.z-d.d*.5f,z1=d.z+d.d*.5f,e=.16f,top=Y+.12f;
        m.BoxMinMax(Kerb,x0,x1,Y,top,z0,z0+e);m.BoxMinMax(Kerb,x0,x1,Y,top,z1-e,z1);
        m.BoxMinMax(Kerb,x0,x0+e,Y,top,z0+e,z1-e);m.BoxMinMax(Kerb,x1-e,x1,Y,top,z0+e,z1-e);
        m.BoxMinMax(Deck,x0+e,x1-e,Y,top,z0+e,z1-e);
        BraxChampionsMeshBatch.Collider(colliders,"strefa jedzenia "+d.x.ToString("0"),new Vector3(d.x,Y+.06f,d.z),new Vector3(d.w,.12f,d.d));
        foreach(float dx in new[]{-4.0f,0f,4.0f})
        {
            var f=new Vector3(d.x+dx,top,d.z);
            BraxChampionsPromenade.BistroTable(m,f,.45f);
            BraxChampionsPromenade.Chair(m,f+new Vector3(.74f,0,0),-90f);BraxChampionsPromenade.Chair(m,f+new Vector3(-.74f,0,0),90f);
            BraxChampionsPromenade.Chair(m,f+new Vector3(0,0,.74f),180f);BraxChampionsPromenade.Chair(m,f+new Vector3(0,0,-.74f),0f);
            BraxChampionsPromenade.Parasol(m,f+Vector3.up*.76f,1.45f);
            BraxChampionsMeshBatch.Collider(colliders,"stolik "+(d.x+dx).ToString("0"),new Vector3(f.x,top+.38f,f.z),new Vector3(1.9f,.76f,1.9f));
            Chairs+=4;
        }
        // standing counters ("stanowiska do jedzenia"): marble top at 1.08 m, two padded leather stools each
        foreach(float s in new[]{-1f,1f})
        {
            var f=new Vector3(d.x+s*6.15f,top,d.z);
            var g=BraxChampionsHarbour.Graphite;
            m.Cylinder(g,f,.30f,.03f,16);m.Cylinder(g,f+Vector3.up*.03f,.045f,1.02f,10);
            m.Cylinder(BraxChampionsPromenade.Marble,f+Vector3.up*1.05f,.36f,.035f,24,Quaternion.identity,true,true);
            foreach(float sz in new[]{-1f,1f})
            {
                var st=f+new Vector3(0,0,sz*.62f);
                m.Cylinder(g,st,.19f,.025f,16);m.Cylinder(BraxChampionsHarbour.Steel,st+Vector3.up*.025f,.028f,.70f,10);
                m.Cylinder(g,st+Vector3.up*.28f,.16f,.018f,16);                                         // foot ring
                m.Cylinder(BraxChampionsHarbour.Steel,st+Vector3.up*.72f,.19f,.015f,20);
                m.Cylinder(BraxChampionsPromenade.Leather,st+Vector3.up*.735f,.185f,.065f,20,Quaternion.identity,true,false,.16f);
                Stools++;
            }
            BraxChampionsMeshBatch.Collider(colliders,"lada stojąca "+f.x.ToString("0"),new Vector3(f.x,top+.54f,f.z),new Vector3(.8f,1.08f,1.9f));
        }
        // palm beds beyond both ends (the palms themselves: foundation plants, band dining_bed)
        foreach(float s in new[]{-1f,1f})
            Bed(m,new BraxChampionsFoundation.PalmBed{x=d.x+s*(d.w*.5f+1.6f+.4f),z=d.z,w=3.2f,d=3.2f});
        Islands++;
    }

    public static void Build(Transform foundation,BraxChampionsFoundation.PalmBed[] beds,BraxChampionsFoundation.Furniture[] furniture,BraxChampionsFoundation.Dining[] dining)
    {
        if(!foundation||beds==null||furniture==null||dining==null)throw new InvalidOperationException("Promenade furniture needs the beds, furniture and dining plan");
        if(!BraxChampionsHarbour.Steel)BraxChampionsHarbour.Materials();
        if(!Teak)Materials();
        if(!BraxChampionsPromenade.Marble||!BraxChampionsPromenade.Cushion)BraxChampionsPromenade.Materials();
        Benches=Lamps=Beds=Islands=Chairs=Stools=Planters=Flowers=BikeHoops=Chargers=RepairStands=Drinking=Telescopes=Bins=0;
        var root=new GameObject("MIASTO MISTRZÓW | PROMENADA 10.11 | palmy w kwaterach, ławki, latarnie").transform;root.SetParent(foundation,false);
        var colliders=new GameObject("kolizje").transform;colliders.SetParent(root,false);
        var models=new GameObject("kwiaty w donicach (Poly Haven, oryginały 2K)").transform;models.SetParent(root,false);
        var m=new BraxChampionsMeshBatch();
        foreach(var b in beds)Bed(m,b);
        foreach(var f in furniture)
        {
            if(f.kind=="bench")Bench(m,colliders,f.x,f.z);
            else if(f.kind=="lamp")Lamp(m,colliders,f.x,f.z);
            else if(f.kind=="quay_bench_east")QuayBench(m,colliders,f.x,f.z,1f);
            else if(f.kind=="quay_bench_west")QuayBench(m,colliders,f.x,f.z,-1f);
            else if(f.kind=="planter")Planter(m,colliders,models,f.x,f.z);
            else if(f.kind=="bin")Bin(m,colliders,f.x,f.z);
            else if(f.kind=="bike_rack")BikeRack(m,colliders,f.x,f.z);
            else if(f.kind=="bike_repair")BikeRepair(m,colliders,f.x,f.z);
            else if(f.kind=="water_fountain")DrinkingFountain(m,colliders,f.x,f.z);
            else if(f.kind=="binoculars")Telescope(m,colliders,f.x,f.z);
            else BraxBuildQuality.Warn("DO POPRAWKI promenada: nieznany element "+f.kind+" x"+f.x.ToString("0"));
        }
        m.Build(root,"Promenada: kwatery, ławki, kosze, latarnie",Output+"/Meshes/Harbour");
        // one batch per dining island (each is a separate place on the 1.1 km promenade: culled on its own)
        foreach(var d in dining)
        {
            var im=new BraxChampionsMeshBatch();Island(im,colliders,d);
            im.Build(root,"Strefa jedzenia x"+d.x.ToString("0"),Output+"/Meshes/Harbour",true);
        }
        Debug.Log("CHAMPIONS_PROMENADE_FURNITURE_10_11 beds="+Beds+" benches="+Benches+" lamps="+Lamps+
                  " diningIslands="+Islands+" chairs="+Chairs+" stools="+Stools+" planters="+Planters+" flowers="+Flowers+
                  " bikeHoops="+BikeHoops+" ebikeChargers="+Chargers+" repairStands="+RepairStands+" drinkingFountains="+Drinking+
                  " telescopes="+Telescopes+" bins="+Bins);
    }
}
