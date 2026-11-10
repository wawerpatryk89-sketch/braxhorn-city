using System;
using UnityEngine;

// Pedestrian-scale infrastructure belongs to the city, not the gym or the
// fight systems. Every part is a PBR-authored static mesh in a local segment.
// There are deliberately NO vehicle/NPC spawners and NO live mission triggers.
public static class BraxCityStreetLighting
{
    static Material steel,graphite,glass,concrete,bolts;
    static readonly float[] Posts={-98f,-82f,-66f,-50f,-34f,-2f,14f,30f,46f,62f};

    static Transform Group(string name,Transform parent,Vector3 pos)
    {
        var g=new GameObject(name).transform;
        g.SetParent(parent,false);g.localPosition=pos;return g;
    }

    static void Post(Transform street,float x,bool far)
    {
        // Both posts stand on the existing pavements with a recessed foundation.
        // The metro entrance and marked pedestrian crossing remain free.
        // Source Unity no-teleport walking test 40914321114 proved that
        // the far-side lamp at (46,34.30) physically blocked Brax at
        // (46.188,34.770), exactly its 0.145 m post collider plus his
        // capsule radius. Recess the entire far line close to the *road-side*
        // kerb, leaving the pedestrian corridor z>=34.15 completely free.
        // Preserve the actual posts, shading, materials and their colliders.
        float z=far?33.32f:22.90f;
        var root=Group("Premium graphite curved street lantern "+x+" "+(far?"far":"near"),
            street,new Vector3(x,.15f,z));
        // 10.11 (właściciel 09:21: „latarnie mają być dobrze ustawione w stronę ulicy, a nie w stronę chodnika”): wysięgnik
        // szedł wzdłuż osi x — równolegle do krawężnika, oprawa wisiała nad chodnikiem. Teraz wysięgnik i oprawa nad JEZDNIĄ:
        // strona bliższa (z 22.90) → +z, dalsza (z 33.32) → −z (oś jezdni z ≈ 28.5).
        float facing=far?-1f:1f;
        Vector3 Z(float along,float y)=>new Vector3(0,y,along*facing);
        var frame=new BraxCitySystemMesh();
        var fixtures=new BraxCitySystemMesh();
        var screws=new BraxCitySystemMesh();
        frame.Tube(new Vector3(0,0,0),new Vector3(0,.26f,0),.21f,20,.17f);
        frame.Tube(new Vector3(0,.26f,0),new Vector3(0,5.72f,0),.13f,16,.085f);
        frame.Tube(Z(0,5.72f),Z(.18f,6.04f),.085f,16,.080f);
        frame.Tube(Z(.18f,6.04f),Z(1.22f,6.04f),.078f,16,.06f);
        frame.Tube(Z(1.22f,6.04f),Z(1.46f,5.85f),.06f,16,.055f);
        // Recessed luminaire, casting housing and optical lower plate (long side along the arm).
        fixtures.Box(Z(1.54f,5.88f),new Vector3(.4f,.22f,.7f));
        fixtures.Box(Z(1.54f,5.73f),new Vector3(.32f,.07f,.57f));
        frame.Box(new Vector3(0,5.40f,0),new Vector3(.18f,.045f,.18f));
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4;
            screws.Tube(new Vector3(Mathf.Cos(a)*.14f,.17f,Mathf.Sin(a)*.14f),
                new Vector3(Mathf.Cos(a)*.14f,.19f,Mathf.Sin(a)*.14f),.012f,6);
        }
        BraxCityArchitecture.MeshObject("Galvanized steel tapered column and curved cantilever",root,frame,graphite);
        BraxCityArchitecture.MeshObject("Diecast aluminium luminaire with closed optical glazing",root,fixtures,steel);
        BraxCityArchitecture.MeshObject("Real flange fixings",root,screws,bolts);
        BraxCityArchitecture.Box("Dark inset LED diffuser (unlit daylight)",root,
            Z(1.54f,5.682f),new Vector3(.28f,.012f,.51f),glass);
        var c=root.gameObject.AddComponent<CapsuleCollider>();
        c.center=new Vector3(0,2.92f,0);c.radius=.145f;c.height=5.84f;
        // No emission and no point lights in daylight; this reduces GPU cost.
    }

    public static void Build(Transform street)
    {
        if(!street)throw new ArgumentNullException("street");
        graphite=BraxCityMaterials.Surface("Graphite cast urban streetlight / matte enamel",
            new Color(.075f,.086f,.092f),.72f,.35f);
        steel=BraxCityMaterials.Surface("Streetlamp powder-coated steel head",
            new Color(.16f,.18f,.19f),.66f,.45f);
        glass=BraxCityMaterials.Surface("Streetlamp diffuse milky safety lens unlit",
            new Color(.58f,.59f,.56f),.82f,.03f);
        bolts=BraxCityMaterials.Surface("Stainless steel street fixture bolts",
            new Color(.42f,.45f,.45f),.40f,.76f);
        var root=Group("Main street both-side professional lanterns / no traffic",street,Vector3.zero);
        int pominiete=0;
        foreach(float x in Posts)
        {
            // 10.11 (właściciel 09:21: „wychodzisz z siłowni, w lewo, potem ulica w lewo — na środku drogi stoi słup”): w miejscu
            // dawnego wiaduktu jest wylot ul. Wiaduktowej (jezdnia x −69.2…−63.2 aż do jezdni głównej; chodnik i krawężnik
            // ulicy głównej w wylocie wycięte w BraxRegionMiastoB). Słup bliższej strony x −66 (z 22.90) stał na jezdni.
            // Skrzyżowanie oświetlają sygnalizatory na wysięgnikach i latarnie Wiaduktowej.
            if(Mathf.Abs(x-(-66f))<.01f)pominiete++;
            else Post(root,x,false);
            // The eastern metro entrance sits at X=78.9. The farther-side
            // posts here remain behind the curb and outside the excavated
            // metro rectangle; crosswalk X=50..54 is kept unobstructed.
            Post(root,x,true);
        }
        Debug.Log("CITY_PREMIUM_STREET_FURNITURE built="+(Posts.Length*2-pominiete)+" (wysięgniki nad jezdnią; pominięty słup w wylocie ul. Wiaduktowej: "+pominiete+")"+
            " unlit physical pedestrian lanterns; traffic, NPCs, police and missions absent");
    }
}
