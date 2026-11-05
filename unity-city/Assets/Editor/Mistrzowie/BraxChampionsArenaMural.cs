using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// The owner's original CHAMPION FIGHTER BRAXHORN wall artwork, unchanged.
// Installed INSIDE A1 on the solid front wall visible through the public glass.
// The existing 4.10m-wide central doorway remains physically free.
public static class BraxChampionsArenaMural
{
    public const string OriginalImage =
        "Assets/Source/Mistrzowie/ArenaMurals/BRAXHORN_CHAMPION_FIGHTER_ORIGINAL.jpg";

    const string Output="Assets/PlayableGenerated/Mistrzowie/Arena/Mural";

    // Original arena-bowl LOCAL coordinates, before the unchanged 180-degree
    // placement by BraxChampionsArena.Generate. Exterior public entrance is at
    // global/local Z=-59; the inner wall faces the lobby at global Z=-35.
    const float WallNearLeft=-146f;
    const float WallNearRight=-26f;
    const float ImageLeft=-127f;
    const float ImageRight=-45f;
    const float DoorLeft=-88.05f;
    const float DoorRight=-83.95f;
    const float DoorTop=4.20f;
    const float MuralBase=.20f;
    const float MuralTop=27.53333333f;
    const float WallImageZ=98.215f; // inside the wall: no Z-fighting
    const int SourceWidth=1536;
    const int SourceHeight=512;

    // World scene image aspect stays 1536:512 (3:1) at 82:27.333 m.
    // The 19m peripheral sections extend the existing artwork's EDGE COLOUR
    // rather than stretching any fighter, emblem or lettering.
    static float U(float x) => Mathf.Clamp01((x-ImageLeft)/(ImageRight-ImageLeft));
    static float V(float y) => Mathf.Clamp01((y-MuralBase)/(MuralTop-MuralBase));

    static void AddSegment(Transform parent,Material mural,
        string name,float x0,float x1,float y0,float y1,bool extension)
    {
        if(x1<=x0||y1<=y0)throw new ArgumentOutOfRangeException(name);
        float u0=U(x0),u1=U(x1);
        // Sample exactly one edge column over the monochrome side extensions.
        // The original 82-metre artwork is not cropped, stretched, or recoloured.
        if(extension)
        {
            if(x1<=ImageLeft)u0=u1=.002f;
            else if(x0>=ImageRight)u0=u1=.998f;
        }
        var mesh=new Mesh{name=name+" | original panoramic UV panel"};
        mesh.vertices=new[]{
            new Vector3(x0,y0,WallImageZ),
            new Vector3(x0,y1,WallImageZ),
            new Vector3(x1,y1,WallImageZ),
            new Vector3(x1,y0,WallImageZ)
        };
        // Winding faces the viewer from the public lobby toward the hall (-Z).
        mesh.uv=new[]{
            new Vector2(u0,V(y0)),new Vector2(u0,V(y1)),
            new Vector2(u1,V(y1)),new Vector2(u1,V(y0))
        };
        mesh.triangles=new[]{0,1,2,0,2,3};
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        string safe=name.Replace(' ','_').Replace('|','_');
        AssetDatabase.CreateAsset(mesh,Output+"/"+safe+".asset");
        var go=new GameObject(name);
        go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial=mural;
        renderer.shadowCastingMode=ShadowCastingMode.Off;
        renderer.receiveShadows=false;
        // Murals are PRINTED SURFACES, not replacement walls/colliders.
        // The real doorway and structural collision stay exactly unchanged.
        GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.OccludeeStatic);
    }

    public static bool Generate(Transform originalBowlRoot)
    {
        if(!originalBowlRoot)throw new ArgumentNullException(nameof(originalBowlRoot));
        var image=AssetDatabase.LoadAssetAtPath<Texture2D>(OriginalImage);
        if(!image)
        {
            Debug.LogWarning("CHAMPION_A1_MURAL_SOURCE_PENDING: owner original panoramic JPEG missing: "+
                             OriginalImage+"; NEVER substitute generated people");
            return false;
        }
        if(image.width!=SourceWidth||image.height!=SourceHeight)
            throw new InvalidDataException("Owner's CHAMPION FIGHTER wall artwork must remain original 1536x512");
        var importer=AssetImporter.GetAtPath(OriginalImage) as TextureImporter;
        if(!importer||!importer.mipmapEnabled)
            throw new InvalidDataException("A1 mural requires compressed, mipmapped original artwork");

        Directory.CreateDirectory(Output);
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("Original CityStaticPBR shader unavailable");
        var m=new Material(shader){name="A1 | owner's ORIGINAL CHAMPION FIGHTER mural",enableInstancing=true};
        m.SetTexture("_MainTex",image);
        m.SetTexture("_EmissionMap",image);
        m.SetColor("_Color",Color.white);
        m.SetColor("_EmissionColor",new Color(.27f,.27f,.27f));
        m.SetFloat("_Roughness",.89f);
        m.SetFloat("_Metallic",0);
        m.SetFloat("_VertexTint",0);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(m,Output+"/OwnerOriginalChampionFighter.mat");

        var walls=new GameObject("A1 | MURAL CHAMPION FIGHTER BRAXHORN - entire wall, door cutout").transform;
        walls.SetParent(originalBowlRoot,false);
        // Cover the complete 120m width of the original wall. Keep the central
        // art at its exact 3:1 ratio and use nonfigural image-edge colour outside.
        AddSegment(walls,m,"Far left atmospheric edge",WallNearLeft,ImageLeft,MuralBase,MuralTop,true);
        AddSegment(walls,m,"Original mural left of entry",ImageLeft,DoorLeft,MuralBase,MuralTop,false);
        AddSegment(walls,m,"Original mural ABOVE doorway",DoorLeft,DoorRight,DoorTop,MuralTop,false);
        AddSegment(walls,m,"Original mural right of entry",DoorRight,ImageRight,MuralBase,MuralTop,false);
        AddSegment(walls,m,"Far right atmospheric edge",ImageRight,WallNearRight,MuralBase,MuralTop,true);
        Debug.Log("CHAMPION_A1_MURAL_ORIGINAL_APPLIED source="+OriginalImage+
                  " wall=120m mural=82m door=4.10m x 4.20m open; sourcePixelsUnmodified=true");
        return true;
    }
}
