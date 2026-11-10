using System;
using UnityEngine;

// Promenade furniture, owner 10.11 09:01-09:06: "drzewa posadzić ... palmy posadzić ... promenady ładne, ekskluzywne".
// The seaside palm row (foundation: promenade_layout.PALM_BEDS, the owner's thick-trunk palms planted in 3.2 m beds
// flush with the paving) gets granite kerbs and a warm uplight at every trunk; between the beds alternately a teak
// bench on cast frames facing the sea (with a stainless litter bin) or a slim promenade lamp. The city walks no longer
// run into the sand: timber boardwalks lead from them over the beach to the loungers.
// All parts in one combined mesh per material (BraxChampionsMeshBatch).
public static class BraxChampionsPromenadeFurniture
{
    const string Output="Assets/PlayableGenerated/Mistrzowie";
    const float Y=.16f;
    public static Material Teak,Kerb,Deck;
    public static int Benches,Lamps,Beds,Boardwalks;

    public static void Materials()
    {
        Teak=BraxChampionsFoundation.SourceMaterial("prom_bench_pale_oak_slats","oak_wood_planks",.9f,new Color(1.0f,.90f,.78f));
        Kerb=BraxChampionsFoundation.SourceMaterial("prom_bed_granite_kerb","granite_tile",1.2f,new Color(.70f,.70f,.69f));
        Deck=BraxChampionsFoundation.LibraryAcg("prom_beach_boardwalk_deck","Planks040",new Color(.86f,.89f,.94f),2.0f);
    }

    static void Bench(BraxChampionsMeshBatch m,Transform colliders,float x,float z)
    {
        // faces the sea (-z): seat slats along x, backrest on the land side
        var g=BraxChampionsHarbour.Graphite;
        foreach(float sx in new[]{-.82f,.82f})
        {
            m.BoxMinMax(g,x+sx-.04f,x+sx+.04f,Y,Y+.44f,z-.28f,z-.22f);        // front leg
            m.BoxMinMax(g,x+sx-.04f,x+sx+.04f,Y,Y+.86f,z+.20f,z+.26f);        // rear leg + back support
            m.BoxMinMax(g,x+sx-.04f,x+sx+.04f,Y+.40f,Y+.44f,z-.30f,z+.26f);   // seat rail
            m.BoxMinMax(g,x+sx-.05f,x+sx+.05f,Y+.62f,Y+.66f,z-.30f,z+.12f);   // armrest
            m.BoxMinMax(g,x+sx-.035f,x+sx+.035f,Y+.44f,Y+.62f,z-.27f,z-.21f);
        }
        for(int i=0;i<6;i++)
        {
            float z0=z-.29f+i*.09f;
            m.BoxMinMax(Teak,x-.95f,x+.95f,Y+.44f,Y+.475f,z0,z0+.075f);
        }
        for(int i=0;i<4;i++)
        {
            float y0=Y+.52f+i*.09f;
            m.BoxMinMax(Teak,x-.95f,x+.95f,y0,y0+.075f,z+.22f,z+.255f);
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
        m.Cylinder(BraxChampionsHarbour.Steel,b+Vector3.up*2.9f,.062f,.10f,14);
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

    // Timber boardwalk over the sand from the end of a city walk (z -161) to the lounger row (z -199).
    static void Boardwalk(BraxChampionsMeshBatch m,Transform colliders,float x)
    {
        const float half=1.25f,step=2f;
        float z=-160.9f;int i=0;
        while(z>-199f)
        {
            float za=z,zb=Mathf.Max(-199f,z-step);
            float ya=BraxChampionsBeachFurnishings.SandY(x,za)+.07f,yb=BraxChampionsBeachFurnishings.SandY(x,zb)+.07f;
            var a0=new Vector3(x-half,ya,za);var a1=new Vector3(x+half,ya,za);var b0=new Vector3(x-half,yb,zb);var b1=new Vector3(x+half,yb,zb);
            m.Quad(Deck,a0,a1,b1,b0);
            // side boards (fascia) and the sleepers they rest on
            m.Quad(Teak,b0,b0-Vector3.up*.14f,a0-Vector3.up*.14f,a0);
            m.Quad(Teak,a1,a1-Vector3.up*.14f,b1-Vector3.up*.14f,b1);
            m.BoxMinMax(Teak,x-half-.05f,x+half+.05f,yb-.16f,yb-.02f,zb,zb+.12f);
            BraxChampionsMeshBatch.Collider(colliders,"kładka "+x.ToString("0")+" "+i,new Vector3(x,(ya+yb)*.5f-.06f,(za+zb)*.5f),new Vector3(half*2f,.12f,za-zb));
            z=zb;i++;
        }
        Boardwalks++;
    }

    public static void Build(Transform foundation,BraxChampionsFoundation.PalmBed[] beds,BraxChampionsFoundation.Furniture[] furniture)
    {
        if(!foundation||beds==null||furniture==null)throw new InvalidOperationException("Promenade furniture needs the beds and furniture plan");
        if(!BraxChampionsHarbour.Steel)BraxChampionsHarbour.Materials();
        if(!Teak)Materials();
        Benches=Lamps=Beds=Boardwalks=0;
        var root=new GameObject("MIASTO MISTRZÓW | PROMENADA 10.11 | palmy w kwaterach, ławki, latarnie, kładki").transform;root.SetParent(foundation,false);
        var colliders=new GameObject("kolizje").transform;colliders.SetParent(root,false);
        var m=new BraxChampionsMeshBatch();
        foreach(var b in beds)Bed(m,b);
        foreach(var f in furniture)
        {
            if(f.kind=="bench")Bench(m,colliders,f.x,f.z);
            else if(f.kind=="lamp")Lamp(m,colliders,f.x,f.z);
        }
        foreach(float x in new[]{230f,530f})Boardwalk(m,colliders,x);
        m.Build(root,"Promenada: kwatery, ławki, kosze, latarnie, kładki",Output+"/Meshes/Harbour");
        Debug.Log("CHAMPIONS_PROMENADE_FURNITURE_10_11 beds="+Beds+" benches="+Benches+" lamps="+Lamps+" boardwalks="+Boardwalks);
    }
}
