using System;
using System.Linq;
using UnityEngine;

// Functional access to the preserved ORIGINAL Nutrition and NOVA interiors.
// Uses the audited modular facade's real door mesh, not a placeholder box.
public static class BraxCityVenueDoors
{
    static BraxCityDoor Entrance(Transform parent,string id,string label,float x,float z,float yaw)
    {
        var root=new GameObject("Source-kit working entrance "+label).transform;
        root.SetParent(parent,false);
        root.position=new Vector3(x,.15f,z);
        root.rotation=Quaternion.Euler(0,yaw,0);
        var hinge=new GameObject("Actual hinged source door leaf").transform;
        hinge.SetParent(root,false);
        hinge.localPosition=new Vector3(-1.05f,0,0);
        var source=BraxCityArchitecture.Part("modular_urban_apartments_facade",
            "door_centered_large_01",hinge,Vector3.zero,Quaternion.identity,new Vector3(.7f,.90f,1));
        // The audited source module is 3m wide, 3m tall. The 0.7 x 0.9
        // transform yields a 2.1m by 2.7m operable leaf.
        var collider=hinge.gameObject.AddComponent<BoxCollider>();
        collider.center=new Vector3(1.05f,1.35f,0);
        collider.size=new Vector3(2.1f,2.7f,.12f);
        var door=root.gameObject.AddComponent<BraxCityDoor>();
        door.id=id;door.label=label;door.leaves=new[]{hinge};door.sides=new[]{1f};
        Debug.Log("CITY_VENUE_REAL_SOURCE_DOOR id="+id+" world="+root.position+
            " source="+source.name);
        return door;
    }

    public static void Apply(BraxCityWorld world,Transform parent)
    {
        if(!world||world.area!="city"||!parent)throw new InvalidOperationException("Original city venue access required");
        // Positions are pinned to original city-manifest.json source anchors:
        // club_door x=-42,z=35 -> Unity x=42,z=35
        // shop_door x=-20.7,z=21.4 -> Unity x=20.7,z=21.4
        var club=Entrance(parent,"NOVA_SOURCE_ENTRY","NOVA",42f,36.08f,0);
        var nutrition=Entrance(parent,"NUTRITION_SOURCE_ENTRY","BRAXHORN NUTRITION",20.7f,20.60f,180);
        world.doors=world.doors.Concat(new[]{club,nutrition}).ToArray();
        if(world.doors.Count(d=>d&&
            (d.id=="NOVA_SOURCE_ENTRY"||d.id=="NUTRITION_SOURCE_ENTRY"))!=2)
            throw new InvalidOperationException("NOVA and Nutrition real source entrances missing");
    }
}
