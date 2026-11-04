using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// A parked copy of the same original GLB used in the estate. No driving controller.
public static class BraxObozParkedBus
{
    const float K = BraxObozSceneInfo.WorldScale;
    const string Source = "Assets/Source/Prywatne/Oboz/environment/vehicles/autobus.glb";
    const string Materials = BraxObozCharacters.Generated + "/BusMaterials";
    public const string Name = "Zaparkowany autobus — oryginał osiedla";
    public static void Add(Transform parent)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Source);
        if(!asset)throw new InvalidOperationException("Brak oryginalnego autobusu osiedla");
        var root=new GameObject(Name).transform;root.SetParent(parent,false);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(asset);model.transform.SetParent(root,false);
        var renderers=model.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0)throw new InvalidOperationException("Autobus bez renderera");
        Bounds bounds=BoundsOf(renderers);
        if(bounds.size.x>bounds.size.z*1.5f)root.rotation=Quaternion.Euler(0,90,0);
        bounds=BoundsOf(renderers);
        root.localScale*=12*K/Mathf.Max(.1f,bounds.size.z);bounds=BoundsOf(renderers);
        // Door side faces the existing passenger pavement at x=26.
        root.position+=new Vector3(21*K-bounds.center.x,.115f*K-bounds.min.y,-113*K-bounds.center.z);
        foreach(var collider in model.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
        var solid=root.gameObject.AddComponent<BoxCollider>();bounds=BoundsOf(renderers);
        // Root may be rotated/scaled: convert the eight world bounds corners to local coordinates.
        Bounds local=new Bounds(root.InverseTransformPoint(bounds.center),Vector3.zero);
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            local.Encapsulate(root.InverseTransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z))));
        solid.center=local.center;solid.size=local.size;
        Directory.CreateDirectory(Materials);AssetDatabase.Refresh();
        foreach(var renderer in renderers)
        {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(CampMaterial).ToArray();
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;renderer.receiveShadows=true;
        }
        if(bounds.size.x>4*K||Mathf.Abs(bounds.size.z-12*K)>.02f)
            throw new InvalidOperationException("Oryginalny autobus nie mieści się w zatoce");
        AssetDatabase.SaveAssets();
        Debug.Log("BRAXHORN_CAMP_BUS parked=true length="+bounds.size.z+" width="+bounds.size.x+" source="+Source);
    }
    static Bounds BoundsOf(Renderer[] renderers)
    {var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;}
    static Material CampMaterial(Material original)
    {
        // Preserve the estate's PBR conversion; store its dependencies in the camp checkpoint.
        var converted=BraxCityMaterials.Imported(original);
        string path=Materials+"/"+Hash128.Compute(AssetDatabase.GetAssetPath(converted))+".mat";
        var copy=new Material(converted);
        foreach(string property in copy.GetTexturePropertyNames())
        {
            var texture=copy.GetTexture(property);if(!texture)continue;
            string source=AssetDatabase.GetAssetPath(texture);
            if(!source.StartsWith(BraxCityMaterials.Generated+"/",StringComparison.Ordinal))continue;
            string target=Materials+"/"+Hash128.Compute(source)+Path.GetExtension(source);
            if(!File.Exists(target)&&!AssetDatabase.CopyAsset(source,target))throw new IOException("Nie skopiowano mapy autobusu");
            copy.SetTexture(property,AssetDatabase.LoadAssetAtPath<Texture>(target));
        }
        var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(existing){EditorUtility.CopySerialized(copy,existing);Object.DestroyImmediate(copy);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(copy,path);return copy;
    }
}
