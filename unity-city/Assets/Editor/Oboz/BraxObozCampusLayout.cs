using System;
using System.IO;
using UnityEngine;

public static class BraxObozCampusLayout
{
    public const string Path="Assets/Editor/Oboz/CampusLayout.json";
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
        // Keep foliage outside the fence and moving gate pockets.
        return Mathf.Abs(Mathf.Abs(x)-150)<margin+1||Mathf.Abs(Mathf.Abs(z)-150)<margin+1;
    }
}
