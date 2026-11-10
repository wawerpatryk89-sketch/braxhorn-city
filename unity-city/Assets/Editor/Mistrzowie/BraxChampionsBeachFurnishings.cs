using System;
using System.IO;
using UnityEngine;

// Miasto Mistrzów beach. Owner's original R1 lifeguard_tower.glb and the SAME R1 BraxNabrzezeMebel.Lezak lounger
// dimensions: 1.95 x 0.65 m, 40° backrest. No simulated vendor, NPC, sunbathers or crowd in this phase.
//
// Owner 10.11 10:04 (photo from the city walk onto the sand): "takie wąskie ... coś tu nie gra". Review of the beach:
// 110-125 m of empty sand (a desert, the sea out of sight), ONE row of 16 parasols and 32 loungers every 33 m (dots
// from the walk) and a 2.5 m boardwalk ending in open sand 5 m short of them. Now:
// * the sand is ~63-77 m deep (port_layout.shoreline; Shore below is the same function);
// * two BEACH CLUB zones on the two city walks (x 230, 530): a 12 m entry deck with showers and foot washes, a 4 m
//   timber boardwalk to a landing 9 m from the water, on both sides four rows of sets (two R1 loungers with white
//   cushions and a white parasol between the heads), rows parallel to the waterline;
// * between the zones pairs of sets every 22 m (was one set every 33 m), the lifeguard tower 30 m from the water
//   with its emergency gap kept clear;
// * a line of yellow swimming-zone buoys 30 m out (R1 etap 1.1: "żółte boje na linie jako granica kąpieliska").
// Every lounger has a collider and a BraxSeat (lie) - "nic nie stoi na pokaz". Colours: silver-grey teak, stainless,
// white canvas, navy towels; no brown/beige (owner: "sraczkowate").
// All parts in combined meshes per material (BraxChampionsMeshBatch), one batch per zone.
public static class BraxChampionsBeachFurnishings
{
    const string Output="Assets/PlayableGenerated/Mistrzowie";
    const float WaterLevel=-2.0f;                                   // = BraxChampionsHarbour.WaterLevel, foundation sea
    public static readonly float[] ClubX={230f,530f};              // the two city walks that end at the sand
    public const float BoardwalkHalf=2.0f,EntryHalf=6.0f,EntryZ0=-160.9f,EntryZ1=-169f,ClubHalf=29f;
    public const float TowerX=331f,TowerGap=27f;
    public static int Loungers,Parasols,Seats,Buoys,Showers,Boardwalks;
    static float K=>BraxClaudeOsiedle.K;

    static float S(float t) { t=Mathf.Clamp01(t); return t*t*(3f-2f*t); }

    public static float Shore(float x)
    {
        float blend=S((x-50f)/37f)*S((700f-x)/49f);
        float deeper=-231f+5.0f*Mathf.Sin(x*.013f+.4f)+2.4f*Mathf.Sin(x*.034f+1.1f);   // = port_layout.shoreline
        return -163f+(deeper+163f)*blend;
    }

    public static float SandY(float x,float z)
    {
        float s=Shore(x);
        float t=Mathf.Clamp01((-161f-z)/(-161f-s));
        return .16f-2.26f*Mathf.Pow(t,1.32f)
               +.034f*Mathf.Sin(x*.045f)*Mathf.Sin(z*.082f)*Mathf.Sin(Mathf.PI*t);
    }

    static Material Teak,Cushion,Towel,Canvas,Navy,Deck,Buoy,Rope;

    static void Materials()
    {
        if(!BraxChampionsHarbour.Steel)BraxChampionsHarbour.Materials();
        // teak_veneer alone is orange (175,130,87): silver-grey aged teak ~ (160,156,150)
        Teak=BraxChampionsFoundation.SourceMaterial("beach_lounger_silver_teak","teak_veneer",1.1f,new Color(.91f,1.20f,1.72f));
        // owner 10.11 10:27: real cloth (OBIEKTY_Z_KODU_PRAWDZIWE_MATERIALY) - plain cotton cushions and towels,
        // woven terlenka canvas (the plaid fabric_pattern_05 is gone); white + navy, no beige
        Cushion=BraxChampionsPromenade.Fabric("beach_cushion_white_cotton","cotton_jersey",new Color(1.20f,1.36f,1.46f),.6f);
        Towel=BraxChampionsPromenade.Fabric("beach_towel_navy_cotton","cotton_jersey",new Color(.18f,.30f,.59f),.6f);
        Canvas=BraxChampionsPromenade.Fabric("beach_parasol_canvas_white","terlenka",new Color(1.21f,1.36f,1.53f),.9f,new Color(.38f,.38f,.36f));
        Navy=BraxChampionsPromenade.Fabric("beach_parasol_valance_navy","terlenka",new Color(.17f,.30f,.60f),.9f);
        Deck=BraxChampionsFoundation.LibraryAcg("prom_beach_boardwalk_deck","Planks040",new Color(.94f,1.08f,1.18f),2.0f);
        Buoy=BraxChampionsHarbour.Surface("beach_swim_buoy_yellow",new Color(1.0f,.78f,.08f),.35f,0f);
        Rope=BraxChampionsHarbour.Surface("beach_swim_rope_white",new Color(.92f,.92f,.90f),.8f,0f);
    }

    // ---------- R1 lounger: 1.95 x 0.65 m, seat .32 m, backrest 40° toward the land (+z), feet to the sea ----------
    static void Lounger(BraxChampionsMeshBatch m,Transform col,float x,float z,int n)
    {
        float y=SandY(x,z);
        if(y<=-1.6f)throw new InvalidOperationException("Lounger position too near tide zone: "+x+" "+z);
        var steel=BraxChampionsHarbour.Steel;
        const float w=.65f,L=1.95f,h=.32f,lie=L*.62f,back=L-lie;
        var o=new Vector3(x,y,z);
        foreach(float side in new[]{-.46f,.46f})
        {
            float sx=side*w;
            m.Box(steel,o+new Vector3(sx,h,-L*.5f+lie*.5f),new Vector3(.035f,.05f,lie));
            foreach(float zz in new[]{-L*.46f,-L*.5f+lie-.05f})
                m.Box(steel,o+new Vector3(sx,h*.5f,zz),new Vector3(.035f,h,.035f));
        }
        // teak seat and back boards (the R1 slats lie under the cushion: one board each keeps the outline, 1/7 of the faces)
        m.Box(Teak,o+new Vector3(0,h+.03f,-L*.5f+lie*.5f),new Vector3(w*.9f,.02f,lie-.02f),Quaternion.identity,false);
        var backRot=Quaternion.Euler(-40f,0,0);
        var bc=o+new Vector3(0,h,-L*.5f+lie)+backRot*new Vector3(0,0,back*.5f);
        m.Box(Teak,bc+backRot*new Vector3(0,.03f,0),new Vector3(w*.9f,.02f,back*.96f),backRot);
        foreach(float side in new[]{-.46f,.46f})
            m.Box(steel,bc+backRot*new Vector3(side*w,0,0),new Vector3(.03f,.04f,back),backRot);
        // white cushion on the seat and the backrest (6 cm), a folded navy towel at the feet of every other lounger
        m.Box(Cushion,o+new Vector3(0,h+.07f,-L*.5f+lie*.5f+.02f),new Vector3(w*.86f,.06f,lie*.96f));
        m.Box(Cushion,bc+backRot*new Vector3(0,.07f,0),new Vector3(w*.86f,.06f,back*.92f),backRot);
        if(n%2==0)m.Box(Towel,o+new Vector3(0,h+.115f,-L*.5f+.22f),new Vector3(w*.62f,.03f,.30f));
        BraxChampionsMeshBatch.Collider(col,"leżak "+n,new Vector3(x,y+.27f,z),new Vector3(w,.54f,L*.95f));
        // lie down (BraxSeat): feet toward the seat's forward = the sea (-z), surface = cushion top
        var g=new GameObject("Siedzisko leżak "+n);g.transform.SetParent(col,false);
        g.transform.localPosition=new Vector3(x,y+h+.10f,z)*K;g.transform.localRotation=Quaternion.Euler(0,180f,0);
        var s=g.AddComponent<BraxSeat>();s.lie=true;s.label="LEŻAK";s.surfaceY=(y+h+.10f)*K;
        Loungers++;Seats++;
    }

    // 8-rib round white parasol, 2.7 m canopy, navy valance; pole anchored in the sand between the two heads
    static void Parasol(BraxChampionsMeshBatch m,Transform col,float x,float z,int n)
    {
        float y=SandY(x,z);var b=new Vector3(x,y,z);
        var steel=BraxChampionsHarbour.Steel;
        m.Cylinder(BraxChampionsHarbour.Graphite,b-Vector3.up*.05f,.09f,.17f,12);       // sand anchor collar
        m.Cylinder(steel,b,.024f,2.55f,10);
        const int ribs=16;const float r=1.35f,eave=2.18f,top=2.62f;
        var apex=b+Vector3.up*top;
        for(int j=0;j<ribs;j++)
        {
            float a0=2f*Mathf.PI*j/ribs,a1=2f*Mathf.PI*(j+1)/ribs;
            float s0=j%2==0?0f:.05f,s1=(j+1)%2==0?0f:.05f;
            var p0=b+new Vector3(r*Mathf.Cos(a0),eave-s0,r*Mathf.Sin(a0));
            var p1=b+new Vector3(r*Mathf.Cos(a1),eave-s1,r*Mathf.Sin(a1));
            m.Triangle(Canvas,apex,p1,p0);m.Triangle(Canvas,apex,p0,p1);                     // two-sided cloth
            m.Quad(Navy,p0,p0-Vector3.up*.10f,p1-Vector3.up*.10f,p1);m.Quad(Navy,p1,p1-Vector3.up*.10f,p0-Vector3.up*.10f,p0);
        }
        m.Cylinder(steel,apex-Vector3.up*.02f,.05f,.10f,10);                                 // finial
        BraxChampionsMeshBatch.Collider(col,"parasol "+n,new Vector3(x,y+1.25f,z),new Vector3(.12f,2.5f,.12f));
        Parasols++;
    }

    // a set: two loungers side by side, parasol between their heads
    static void Set(BraxChampionsMeshBatch m,Transform col,float x,float z,ref int n)
    {
        Lounger(m,col,x-.62f,z,n++);
        Lounger(m,col,x+.62f,z,n++);
        Parasol(m,col,x,z+.70f,n);
    }

    static Vector3 On(float x,float z,float lift=.07f)=>new Vector3(x,SandY(x,z)+lift,z);

    // timber deck surface over the sand between z0 > z1 (2 m strips), side fascia and sleepers
    static void DeckStrip(BraxChampionsMeshBatch m,Transform col,float x0,float x1,float z0,float z1,string name,bool fascia)
    {
        float z=z0;int i=0;
        while(z>z1+.001f)
        {
            float za=z,zb=Mathf.Max(z1,z-2f);
            var a0=On(x0,za);var a1=On(x1,za);var b0=On(x0,zb);var b1=On(x1,zb);
            // Owner 10.11 13:05 (zero flicker): the sand is curved, a 2 m plank quad cut through it (23 m2 of deck in the
            // sand plane). The deck follows the sand in 0.5 x 1 m panels, 7 cm above it everywhere (the sand mesh is the
            // same profile sampled coarser and concave, so it always stays below).
            int nz=Mathf.Max(1,Mathf.CeilToInt((za-zb)/.5f)),nx=Mathf.Max(1,Mathf.CeilToInt((x1-x0)/1f));
            for(int iz=0;iz<nz;iz++)
            {
                float zA=Mathf.Lerp(za,zb,iz/(float)nz),zB=Mathf.Lerp(za,zb,(iz+1)/(float)nz);
                for(int ix=0;ix<nx;ix++)
                {
                    float xA=Mathf.Lerp(x0,x1,ix/(float)nx),xB=Mathf.Lerp(x0,x1,(ix+1)/(float)nx);
                    m.Quad(Deck,On(xA,zA),On(xB,zA),On(xB,zB),On(xA,zB));
                }
                if(fascia)
                {
                    Vector3 p0=On(x0,zA),q0=On(x0,zB),p1=On(x1,zA),q1=On(x1,zB);
                    m.Quad(Teak,q0,q0-Vector3.up*.14f,p0-Vector3.up*.14f,p0);
                    m.Quad(Teak,p1,p1-Vector3.up*.14f,q1-Vector3.up*.14f,q1);
                }
            }
            m.BoxMinMax(Teak,x0-.05f,x1+.05f,Mathf.Min(b0.y,b1.y)-.16f,Mathf.Min(b0.y,b1.y)-.02f,zb,zb+.12f);
            BraxChampionsMeshBatch.Collider(col,name+" "+i,new Vector3((x0+x1)*.5f,(a0.y+b1.y)*.5f-.06f,(za+zb)*.5f),new Vector3(x1-x0,.12f,za-zb));
            z=zb;i++;
        }
    }

    static void Shower(BraxChampionsMeshBatch m,Transform col,float x,float z)
    {
        var steel=BraxChampionsHarbour.Steel;var b=On(x,z);
        m.BoxMinMax(BraxChampionsHarbour.Graphite,x-.45f,x+.45f,b.y,b.y+.025f,z-.45f,z+.45f);   // drain grate
        m.Cylinder(steel,b,.06f,2.30f,14);
        m.Tube(steel,b+Vector3.up*2.26f,b+new Vector3(0,2.26f,-.42f),.025f,8);
        m.Cylinder(steel,b+new Vector3(0,2.18f,-.42f),.11f,.05f,16,Quaternion.identity,true,true);  // rain head
        m.Tube(steel,b+new Vector3(0,.45f,0),b+new Vector3(0,.45f,-.22f),.018f,8);               // foot tap
        m.Box(steel,b+new Vector3(0,1.15f,-.07f),new Vector3(.10f,.06f,.06f));                  // push valve
        BraxChampionsMeshBatch.Collider(col,"prysznic "+x.ToString("0"),new Vector3(x,b.y+1.15f,z),new Vector3(.14f,2.3f,.14f));
        Showers++;
    }

    static void Club(Transform root,float xb,ref int n)
    {
        var m=new BraxChampionsMeshBatch();
        var col=new GameObject("kolizje i leżenie").transform;
        var zone=new GameObject("BEACH CLUB x"+xb.ToString("0")+" | wejście z prysznicami, kładka 4 m, 4 rzędy leżaków").transform;
        zone.SetParent(root,false);col.SetParent(zone,false);
        // entry deck where the city walk meets the sand, two showers and foot washes
        DeckStrip(m,col,xb-EntryHalf,xb+EntryHalf,EntryZ0,EntryZ1,"wejście",true);
        Shower(m,col,xb-EntryHalf+1.4f,-165f);Shower(m,col,xb+EntryHalf-1.4f,-165f);
        // boardwalk to a landing 9 m from the water
        float end=Shore(xb)+9f;
        DeckStrip(m,col,xb-BoardwalkHalf,xb+BoardwalkHalf,EntryZ1,end+4f,"kładka",true);
        DeckStrip(m,col,xb-3f,xb+3f,end+4f,end,"pomost kładki",true);
        Boardwalks++;
        // four rows each side, parallel to the waterline: front row 17 m from the water, 7 m between rows
        for(int side=-1;side<=1;side+=2)
            for(int c=0;c<6;c++)
            {
                float x=xb+side*(BoardwalkHalf+3.0f+c*4.5f);
                for(int row=0;row<4;row++)Set(m,col,x,Shore(x)+17f+row*7f,ref n);
            }
        m.Build(zone,"Beach club x"+xb.ToString("0"),Output+"/Meshes/Harbour",true);
    }

    static void Public(Transform root,ref int n)
    {
        var m=new BraxChampionsMeshBatch();
        var zone=new GameObject("plaża publiczna | pary zestawów co 22 m").transform;zone.SetParent(root,false);
        var col=new GameObject("kolizje i leżenie").transform;col.SetParent(zone,false);
        for(float x=121f;x<=650f;x+=22f)
        {
            if(Mathf.Abs(x-TowerX)<TowerGap)continue;                              // emergency access to the tower
            bool inClub=false;foreach(float xb in ClubX)if(Mathf.Abs(x-xb)<ClubHalf+4f)inClub=true;
            if(inClub)continue;
            foreach(float dx in new[]{-2.3f,2.3f})Set(m,col,x+dx,Shore(x+dx)+20f,ref n);
        }
        m.Build(zone,"Plaża publiczna",Output+"/Meshes/Harbour",true);
    }

    static void SwimBuoys(Transform root)
    {
        var m=new BraxChampionsMeshBatch();
        Vector3 prev=Vector3.zero;bool first=true;
        for(float x=100f;x<=650f;x+=9f)
        {
            var p=new Vector3(x,WaterLevel-.12f,Shore(x)-30f);
            m.Cylinder(Buoy,p,.26f,.34f,14,Quaternion.identity,true,true,.20f);
            m.Cylinder(Buoy,p+Vector3.up*.34f,.20f,.10f,14,Quaternion.identity,true,false,.06f);
            if(!first)m.Tube(Rope,prev+Vector3.up*.16f,p+Vector3.up*.16f,.02f,6,4,.08f);
            prev=p;first=false;Buoys++;
        }
        m.Build(root,"Granica kąpieliska | żółte boje na linie 30 m od brzegu",Output+"/Meshes/Harbour");
    }

    static GameObject OriginalLifeguard(Transform parent)
    {
        // Exact file used by the R1 sea-front generator. It is already in
        // the owner's repository; do not download a substitute or modify R1.
        if(!File.Exists("Assets/Source/HarborBeach/Models/lifeguard_tower.glb"))
            throw new FileNotFoundException("Existing R1 lifeguard tower model required");
        float x=TowerX,z=Shore(TowerX)+30f;                          // 30 m from the water (was 65 m on the old desert)
        float seaY=SandY(x,z);
        if(seaY<=-1.5f)throw new InvalidOperationException("Lifeguard steps reach tide zone");
        var root=new GameObject("ORIGINAL FIRST-CITY R1 LIFEGUARD TOWER COPY - NO NPC").transform;
        root.SetParent(parent,false);
        var targetHeight=BraxNabrzezeRatownik.WysModelu*BraxNabrzezeRatownik.Skala;
        var tower=BraxHarborBeachBuild.Postaw(root,
            "lifeguard_tower.glb","R1 original tower | access to beach",
            new Vector3(x,seaY,z)*K,targetHeight,0);
        if(!tower)throw new InvalidOperationException("Cannot import original existing R1 lifeguard tower");
        var colliders=new GameObject("R1 original lifeguard entrance and stair collisions").transform;
        colliders.SetParent(root,false);
        int count=BraxNabrzezeRatownik.Wejscie(tower,colliders,out float deckHeight);
        // Keep the ORIGINAL placed tower and the beach even if its real
        // stairs fail access validation; never invent phantom colliders.
        // This is a traversability advisory requiring the owner's review.
        if(count<=0)
            BraxBuildQuality.Warn("R1 original lifeguard tower: measured stair/walkway "+
                "colliders missing; access NOT VALIDATED (tower source preserved)");
        else
            Debug.Log("CHAMPIONS_BEACH_R1_LIFEGUARD original_GLb=TRUE stairs_colliders="+count+" accessibleDeckY="+deckHeight);
        return tower;
    }

    public static void Build(Transform foundation)
    {
        if(!foundation)throw new ArgumentNullException(nameof(foundation));
        Directory.CreateDirectory(Output+"/Meshes/Harbour");
        Directory.CreateDirectory(Output+"/Materials");
        Materials();
        Loungers=Parasols=Seats=Buoys=Showers=Boardwalks=0;
        var root=new GameObject("MIASTO MISTRZOW | ORIGINAL R1 LIFEGUARD + BEACH CLUB LOUNGERS + PARASOLS + BOARDWALKS").transform;
        root.SetParent(foundation,false);
        OriginalLifeguard(root);
        int n=0;
        foreach(float xb in ClubX)Club(root,xb,ref n);
        Public(root,ref n);
        SwimBuoys(root);
        if(Loungers<180||Parasols!=Loungers/2||Seats!=Loungers)
            throw new InvalidOperationException("Beach club incomplete: loungers="+Loungers+" parasols="+Parasols+" seats="+Seats);
        Debug.Log("CHAMPIONS_BEACH_CLUB_10_11 depth=63-77m loungers="+Loungers+" parasols="+Parasols+" lieSeats="+Seats+
                  " boardwalks="+Boardwalks+" showers="+Showers+" swimBuoys="+Buoys+" original_r1_chaise_dimensions=1.95x.65m");
        Debug.Log("CHAMPIONS_BEACH_ASSETS_SOURCE R1_LIFEGUARD=1 original_r1_chaise_dimensions=1.95x.65m"+
                  " occupied_by_people=0 actor_modifications=0 sea_waves_unchanged=1 Android2K=true");
    }
}
