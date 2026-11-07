using System;
using UnityEngine;

// Converts the measured instance's LOD reference size into perspective-screen
// thresholds. 20/35 m are reference-camera distances, not a phone FPS promise.
public static class BraxObozTreeLodDistances
{
    public static float Threshold(float worldReferenceHeight,float distanceMetres,
        float verticalFieldOfView=60f,float lodBias=1f)
    {
        if(worldReferenceHeight<=0 || distanceMetres<=0 || verticalFieldOfView<=1 ||
            verticalFieldOfView>=179 || lodBias<=0)throw new ArgumentOutOfRangeException();
        float worldDistance=distanceMetres*BraxObozPlacement.K;
        return Mathf.Clamp(worldReferenceHeight*lodBias /
            (2f*worldDistance*Mathf.Tan(verticalFieldOfView*Mathf.Deg2Rad*.5f)),.00001f,.99f);
    }
    public static void Configure(LODGroup group,float[] distancesMetres,
        float verticalFieldOfView=60f,float lodBias=1f)
    {
        var levels=group.GetLODs();
        if(distancesMetres==null || distancesMetres.Length!=levels.Length)
            throw new InvalidOperationException("Nieprawidłowa liczba rzeczywistych poziomów LOD");
        group.RecalculateBounds();
        var scale=group.transform.lossyScale;
        float height=group.size*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
        float previous=1f;
        for(int i=0;i<levels.Length;i++)
        {
            if(i>0 && distancesMetres[i]<=distancesMetres[i-1])
                throw new InvalidOperationException("Odległości LOD nie są rosnące");
            float threshold=Threshold(height,distancesMetres[i],verticalFieldOfView,lodBias);
            if(threshold>=previous)throw new InvalidOperationException("Progi LOD nie są malejące");
            levels[i].screenRelativeTransitionHeight=threshold;previous=threshold;
        }
        group.SetLODs(levels);group.fadeMode=LODFadeMode.None;
    }
    public static void ConfigureNamed(Transform tree)
    {
        var group=tree.GetComponent<LODGroup>();if(!group)return;
        if(tree.name=="pine_tree_01_b" || tree.name.StartsWith("pine_tree_01_b —",StringComparison.Ordinal))
            Configure(group,new[]{20f,35f,300f});
        else if(tree.name=="tree_small_02" || tree.name.StartsWith("tree_small_02 —",StringComparison.Ordinal))
            Configure(group,new[]{20f,300f});
    }
}
