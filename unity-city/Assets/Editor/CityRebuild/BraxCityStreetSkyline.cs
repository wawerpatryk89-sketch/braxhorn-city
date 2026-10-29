using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Street-level density plan derived ONLY from the existing eighteen authored
// source footprints. No live city transform, mission anchor or protected room is
// moved by this plan. Actual camera, collision and phone QA remain mandatory.
public static class BraxCityStreetSkyline
{
    public const string AccessibleTowerId = "quay_corner";
    public const int AccessibleExtraFloors = 26;
    public const float FloorHeight = 3.35f;

    public sealed class TowerPlan
    {
        public readonly string id;
        public readonly int additionalFloors;
        public readonly float xInset, zInset;
        public TowerPlan(string id,int additionalFloors,float xInset=.45f,float zInset=.45f)
        {this.id=id;this.additionalFloors=additionalFloors;this.xInset=xInset;this.zInset=zInset;}
    }

    // Alternating 90–135 m street-adjacent high-rises on BOTH frontages.
    // Distinct original masonry ground floors remain visible and in scale.
    // No unverified "floating" skyline blocks are added to protected interiors.
    public static readonly TowerPlan[] HighRises = {
        new TowerPlan("apothecary",28,.55f,.7f),
        new TowerPlan("corner_store",22,.5f,.6f),
        new TowerPlan("restaurant",23,.45f,.4f),
        new TowerPlan("hotel",29,.4f,.3f),
        new TowerPlan("estate_corner",22,.6f,.45f),
        new TowerPlan(AccessibleTowerId,AccessibleExtraFloors,.4f,.45f),
        new TowerPlan("gallery",27,.5f,.55f),
        new TowerPlan("apartments_1",30,.4f,.35f),
        new TowerPlan("estate_apartments_2",22,.55f,.4f)
    };

    public static void ValidateStreetFootprints()
    {
        var buildings=BraxCityArchitecture.StreetBuildings;
        if(buildings.Length!=18||buildings.Count(b=>b.far)!=9||buildings.Count(b=>!b.far)!=9)
            throw new InvalidOperationException("City street source changed: required 18 buildings / 9 per side");
        if(buildings.Select(b=>b.id).Distinct().Count()!=18)
            throw new InvalidOperationException("Duplicate authored building footprint identifier");
        foreach(var b in buildings)
        {
            if(b.x1<=b.x0||b.z1<=b.z0||b.floors<2||b.Height>54.9f)
                throw new InvalidOperationException("Invalid existing source footprint: "+b.id);
            if((b.far&&b.z0<36.05f)||(!b.far&&b.z1>20.65f))
                throw new InvalidOperationException("A source footprint intrudes into the protected main road: "+b.id);
        }
        for(int i=0;i<buildings.Length;i++)
        for(int j=i+1;j<buildings.Length;j++)
        {
            var a=buildings[i];var b=buildings[j];
            if(Mathf.Min(a.x1,b.x1)-Mathf.Max(a.x0,b.x0)>.01f &&
               Mathf.Min(a.z1,b.z1)-Mathf.Max(a.z0,b.z0)>.01f)
                throw new InvalidOperationException("Overlapping source footprints: "+a.id+" / "+b.id);
        }
        if(HighRises.Select(p=>p.id).Distinct().Count()!=HighRises.Length)
            throw new InvalidOperationException("Duplicated high-rise work order");
        foreach(var plan in HighRises)
        {
            var b=buildings.SingleOrDefault(x=>x.id==plan.id);
            if(b==null||plan.additionalFloors<2||plan.additionalFloors>48 ||
               b.x1-b.x0-2*plan.xInset<6||b.z1-b.z0-2*plan.zInset<6)
                throw new InvalidOperationException("Invalid tower over source building "+plan.id);
        }
        Debug.Log("CITY_STREET_SOURCE_INVENTORY 18 planned footprints / near=9 far=9 / tower=9; actual scene walk and material QA pending");
    }

    public static float RoofHeight(BraxCityArchitecture.Building building,int additionalFloors)
        =>building.Height+additionalFloors*FloorHeight;

    public static Rect Footprint(BraxCityArchitecture.Building b,TowerPlan p)
        =>new Rect(b.x0+p.xInset,b.z0+p.zInset,
            b.x1-b.x0-2*p.xInset,b.z1-b.z0-2*p.zInset);

    public static void Build(Transform streetRoot)
    {
        ValidateStreetFootprints();
        foreach(var plan in HighRises)
        {
            var b=BraxCityArchitecture.StreetBuildings.Single(x=>x.id==plan.id);
            var footprint=Footprint(b,plan);
            var title=plan.id==AccessibleTowerId?"quay observation tower":plan.id+" skyline";
            BraxCityArchitecture.Tower(streetRoot,title,footprint,b.Height,plan.additionalFloors,FloorHeight);
            Debug.Log("CITY_PREMIUM_HIGHRISE "+plan.id+" base="+b.Height+" roof="+RoofHeight(b,plan.additionalFloors)
                +" footprint="+footprint+" side="+(b.far?"FAR":"NEAR"));
        }
    }
}
