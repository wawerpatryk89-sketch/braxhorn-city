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
        float z=far?34.30f:22.90f;
        var root=Group("Premium graphite curved street lantern "+x+" "+(far?"far":"near"),
            street,new Vector3(x,.15f,z));
        float facing=far?-1f:1f;
        var frame=new BraxCitySystemMesh();
        var fixtures=new BraxCitySystemMesh();
        var screws=new BraxCitySystemMesh();
        frame.Tube(new Vector3(0,0,0),new Vector3(0,.26f,0),.21f,20,.17f);
        frame.Tube(new Vector3(0,.26f,0),new Vector3(0,5.72f,0),.13f,16,.085f);
        frame.Tube(new Vector3(0,5.72f,0),new Vector3(.18f*facing,6.04f,0),.085f,16,.080f);
        frame.Tube(new Vector3(.18f*facing,6.04f,0),new Vector3(1.22f*facing,6.04f,0),.078f,16,.06f);
        frame.Tube(new Vector3(1.22f*facing,6.04f,0),new Vector3(1.46f*facing,5.85f,0),.06f,16,.055f);
        // Recessed luminaire, casting housing and optical lower plate.
        fixtures.Box(new Vector3(1.54f*facing,5.88f,0),new Vector3(.7f,.22f,.4f));
        fixtures.Box(new Vector3(1.54f*facing,5.73f,0),new Vector3(.57f,.07f,.32f));
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
            new Vector3(1.54f*facing,5.682f,0),new Vector3(.51f,.012f,.28f),glass);
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
        foreach(float x in Posts)
        {
            Post(root,x,false);
            // The eastern metro entrance sits at X=78.9. The farther-side
            // posts here remain behind the curb and outside the excavated
            // metro rectangle; crosswalk X=50..54 is kept unobstructed.
            Post(root,x,true);
        }
        Debug.Log("CITY_PREMIUM_STREET_FURNITURE built="+(Posts.Length*2)+
            " unlit physical pedestrian lanterns; traffic, NPCs, police and missions absent");
    }
}
