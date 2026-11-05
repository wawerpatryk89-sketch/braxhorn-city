using System;
using System.IO;
using UnityEngine;

public static class BraxObozCampusLayout
{
    public const string Path="Assets/Editor/Oboz/CampusLayout.json";
    // Fizyczna skala świata pozostaje K=1.294 w generatorze.
    // Nowy obóz ma obrys eliptyczny, bramę zachowaną przy Z=-125.
    // Pole: PI*90*103 = 29 123 jednostki kw. vs stare 252*240=60 480 (-51.8%).
    public const float PlotRadiusX=90f;
    public const float PlotRadiusZ=103f;
    public const float PlotCenterZ=-22f;
    public const float GateZ=-125f;
    public static bool InsidePlot(float x,float z,float margin=0f)
    {
        float rx=PlotRadiusX-margin,rz=PlotRadiusZ-margin;
        if(rx<=0f||rz<=0f)return false;
        float a=x/rx,b=(z-PlotCenterZ)/rz;
        return a*a+b*b<=1f;
    }
    public static float FenceDistance(float x,float z)
    {
        float a=x/PlotRadiusX,b=(z-PlotCenterZ)/PlotRadiusZ;
        return Mathf.Abs(Mathf.Sqrt(a*a+b*b)-1f)*Mathf.Min(PlotRadiusX,PlotRadiusZ);
    }
    [Serializable] public sealed class Zone { public string id; public float x,z,width,depth; }
    [Serializable] public sealed class Plan { public int schema; public Zone[] clearances,lawnClearances; }
    static Plan plan;
    public static void Reload()
    {
        plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Path));
        if(plan==null||plan.schema!=1||plan.clearances==null)throw new InvalidOperationException("Nieprawidłowy plan obozu");
        foreach(var r in plan.clearances)
            if(r==null||string.IsNullOrEmpty(r.id)||!Finite(r.x)||!Finite(r.z)||!Finite(r.width)||!Finite(r.depth)||r.width<=0||r.depth<=0)
                throw new InvalidOperationException("Nieprawidłowa strefa wolna od roślinności");
        if(plan.lawnClearances==null)throw new InvalidOperationException("Brak obrysów zabudowy dla trawnika");
        foreach(var r in plan.lawnClearances)
            if(r==null||string.IsNullOrEmpty(r.id)||!Finite(r.x)||!Finite(r.z)||!Finite(r.width)||!Finite(r.depth)||r.width<=0||r.depth<=0)
                throw new InvalidOperationException("Nieprawidłowy obrys zabudowy dla trawnika");
    }
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    public static bool ReservedLawn(float x,float z,float margin)
    {
        if(Reserved(x,z,margin))return true;
        foreach(var r in plan.lawnClearances)
            if(Mathf.Abs(x-r.x)<=r.width*.5f+margin&&Mathf.Abs(z-r.z)<=r.depth*.5f+margin)return true;
        return false;
    }
    public static bool Reserved(float x,float z,float margin)
    {
        if(plan==null)Reload();
        foreach(var r in plan.clearances)
            if(Mathf.Abs(x-r.x)<=r.width*.5f+margin&&Mathf.Abs(z-r.z)<=r.depth*.5f+margin)return true;
        // Chroń pas przy nowym, eliptycznym ogrodzeniu; las poza ogrodzeniem
        // pozostaje możliwy jako naturalne tło HDR, bez kasowania prototypów.
        return FenceDistance(x,z)<margin+1.5f ||
            (Mathf.Abs(z-GateZ)<margin+2f&&Mathf.Abs(x)<12.5f+margin);
    }
}
