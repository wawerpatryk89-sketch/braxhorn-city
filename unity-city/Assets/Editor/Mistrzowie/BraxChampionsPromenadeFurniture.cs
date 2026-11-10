using System;
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
    public static int Benches,Lamps,Beds,Islands,Chairs,Stools;

    public static void Materials()
    {
        // silver-grey aged teak (oak_wood_planks alone is orange 162,117,84 -> ~158,154,148): no brown on the promenade
        Teak=BraxChampionsFoundation.SourceMaterial("prom_bench_silver_teak_slats","oak_wood_planks",.9f,new Color(.975f,1.32f,1.76f));
        Kerb=BraxChampionsFoundation.SourceMaterial("prom_bed_granite_kerb","granite_tile",1.2f,new Color(.70f,.70f,.69f));
        // the same silver-grey weathered boards as the pontoons and the beach boardwalks
        Deck=BraxChampionsFoundation.LibraryAcg("prom_beach_boardwalk_deck","Planks040",new Color(.94f,1.08f,1.18f),2.0f);
    }

    static void Bench(BraxChampionsMeshBatch m,Transform colliders,float x,float z)
    {
        // faces the sea (-z): seat slats along x, backrest on the land side
        var g=BraxChampionsHarbour.Graphite;
        foreach(float sx in new[]{-.82f,.82f})
        {
            m.BoxMinMax(g,x+sx-.04f,x+sx+.04f,Y,Y+.44f,z-.28f,z-.22f);        // front leg
            m.BoxMinMax(g,x+sx-.04f,x+sx+.04f,Y,Y+.84f,z+.20f,z+.26f);        // rear leg + back support (under the top slat)
            m.BoxMinMax(g,x+sx-.04f,x+sx+.04f,Y+.40f,Y+.44f,z-.30f,z+.26f);   // seat rail
            m.BoxMinMax(g,x+sx-.05f,x+sx+.05f,Y+.62f,Y+.66f,z-.30f,z+.12f);   // armrest
            m.BoxMinMax(g,x+sx-.035f,x+sx+.035f,Y+.44f,Y+.62f,z-.275f,z-.225f);  // 10 mm off the seat slat faces
        }
        for(int i=0;i<6;i++)
        {
            float z0=z-.29f+i*.09f;
            m.BoxMinMax(Teak,x-.95f,x+.95f,Y+.44f,Y+.475f,z0,z0+.075f);
        }
        for(int i=0;i<4;i++)
        {
            float y0=Y+.52f+i*.09f;
            m.BoxMinMax(Teak,x-.95f,x+.95f,y0,y0+.075f,z+.21f,z+.245f);       // 15 mm inside the rear legs' faces
        }
        // stainless litter bin beside the bench
        var bin=new Vector3(x+1.35f,Y,z);
        m.Cylinder(BraxChampionsHarbour.Steel,bin,.22f,.82f,16,Quaternion.identity,false);
        m.Cylinder(g,bin+Vector3.up*.82f,.235f,.06f,16);
        m.Cylinder(g,bin,.235f,.05f,16);
        BraxChampionsMeshBatch.Collider(colliders,"ławka "+x.ToString("0"),new Vector3(x,Y+.45f,z),new Vector3(1.95f,.9f,.62f));
        BraxChampionsMeshBatch.Collider(colliders,"kosz "+x.ToString("0"),new Vector3(bin.x,Y+.42f,z),new Vector3(.46f,.84f,.46f));
        Benches++;
    }

    static void Lamp(BraxChampionsMeshBatch m,Transform colliders,float x,float z)
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
        Benches=Lamps=Beds=Islands=Chairs=Stools=0;
        var root=new GameObject("MIASTO MISTRZÓW | PROMENADA 10.11 | palmy w kwaterach, ławki, latarnie").transform;root.SetParent(foundation,false);
        var colliders=new GameObject("kolizje").transform;colliders.SetParent(root,false);
        var m=new BraxChampionsMeshBatch();
        foreach(var b in beds)Bed(m,b);
        foreach(var f in furniture)
        {
            if(f.kind=="bench")Bench(m,colliders,f.x,f.z);
            else if(f.kind=="lamp")Lamp(m,colliders,f.x,f.z);
        }
        m.Build(root,"Promenada: kwatery, ławki, kosze, latarnie",Output+"/Meshes/Harbour");
        // one batch per dining island (each is a separate place on the 1.1 km promenade: culled on its own)
        foreach(var d in dining)
        {
            var im=new BraxChampionsMeshBatch();Island(im,colliders,d);
            im.Build(root,"Strefa jedzenia x"+d.x.ToString("0"),Output+"/Meshes/Harbour",true);
        }
        Debug.Log("CHAMPIONS_PROMENADE_FURNITURE_10_11 beds="+Beds+" benches="+Benches+" lamps="+Lamps+
                  " diningIslands="+Islands+" chairs="+Chairs+" stools="+Stools);
    }
}
