using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BraxObjetoscRak
{
    // Intermediate rotations retain the wrist and elbow cross-section on Unity's
    // four-weight linear skinning path without changing scan vertices or the animation clips.
    public static void Przygotuj(SkinnedMeshRenderer skora, string folder)
    {
        var source = skora.sharedMesh;
        var kosci = skora.bones.ToList();
        var bind = source.bindposes.ToList();
        var pary = new Dictionary<(int, int), int>();
        var stawy = new List<BraxStawyOsiedla.Staw>();
        int ile = kosci.Count;
        for (int i = 0; i < ile; i++)
        {
            string name = kosci[i].name.Split(':').Last();
            if (!(name.EndsWith("ForeArm", StringComparison.Ordinal) || name.EndsWith("Hand", StringComparison.Ordinal))) continue;
            int p = kosci.IndexOf(kosci[i].parent);
            if (p < 0) continue;
            int pierwszy = kosci.Count;
            Matrix4x4 rest = bind[p] * bind[i].inverse;
            var staw = new BraxStawyOsiedla.Staw { rodzic = kosci[p], kosc = kosci[i],
                punkt = rest.MultiplyPoint3x4(Vector3.zero), obrot = rest.rotation, posrednie = new Transform[3] };
            for (int j = 0; j < 3; j++)
            {
                var helper = new GameObject(name + "_objetosc_" + (j + 1)).transform;
                helper.SetParent(kosci[i], false);
                kosci.Add(helper); bind.Add(bind[i]); staw.posrednie[j] = helper;
            }
            stawy.Add(staw); pary[(p,i)] = pierwszy;
        }
        if (stawy.Count != 4) throw new InvalidOperationException(folder + " brak kości łokci lub nadgarstków.");
        var wagi = source.boneWeights;
        int[] ix = new int[4]; float[] w = new float[4];
        for (int v = 0; v < wagi.Length; v++)
        {
            var old = wagi[v];
            ix[0]=old.boneIndex0; ix[1]=old.boneIndex1; ix[2]=old.boneIndex2; ix[3]=old.boneIndex3;
            w[0]=old.weight0; w[1]=old.weight1; w[2]=old.weight2; w[3]=old.weight3;
            int a=-1,b=-1,first=-1;
            float best=0;
            for (int x=0;x<4;x++) for (int y=0;y<4;y++)
            {
                float score = Mathf.Min(w[x],w[y]);
                if (score > best && pary.TryGetValue((ix[x],ix[y]),out int found))
                { a=x; b=y; first=found; best=score; }
            }
            if (a<0) continue;
            float sum=w[a]+w[b], part=w[b]/sum*4f;
            int low=Mathf.Min(3,Mathf.FloorToInt(part)), parent=ix[a], child=ix[b];
            ix[a]=low==0 ? parent : first+low-1; ix[b]=low==3 ? child : first+low;
            w[b]=sum*(part-low); w[a]=sum-w[b];
            for (int x=0;x<3;x++) for (int y=x+1;y<4;y++)
                if(w[y]>w[x]) { (w[x],w[y])=(w[y],w[x]); (ix[x],ix[y])=(ix[y],ix[x]); }
            wagi[v]=new BoneWeight { boneIndex0=ix[0],boneIndex1=ix[1],boneIndex2=ix[2],boneIndex3=ix[3],
                weight0=w[0],weight1=w[1],weight2=w[2],weight3=w[3] };
        }
        var path=folder+"SkoraStawow.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!mesh) { mesh=UnityEngine.Object.Instantiate(source); AssetDatabase.CreateAsset(mesh,path); }
        else EditorUtility.CopySerialized(source,mesh);
        mesh.name=source.name; mesh.bindposes=bind.ToArray(); mesh.boneWeights=wagi;
        EditorUtility.SetDirty(mesh);
        skora.bones=kosci.ToArray(); skora.sharedMesh=mesh;
        skora.gameObject.AddComponent<BraxStawyOsiedla>().stawy=stawy.ToArray();
    }
}
