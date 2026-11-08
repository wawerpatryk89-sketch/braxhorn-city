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
    // HD 6144x2048 (oryginał właściciela powiększony 4x, 6.11) — używany, gdy jest.
    public const string HdImage =
        "Assets/Source/Mistrzowie/ArenaMuralsHD/MURAL_WEJSCIE_CHAMPION_FIGHTER_6144x2048.png";
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
    // Public lobby is on local +Z. The existing wall/cladding ends at Z=98.3.
    // Keep the printed surface on its LOBBY side, not hidden behind the cladding.
    const float WallImageZ=98.33f; // 3cm nominal clearance; existing walls/colliders unchanged
    // World scene image aspect stays 1536:512 (3:1) at 82:27.333 m.
    // The 19m peripheral sections extend the existing artwork's EDGE COLOUR
    // rather than stretching any fighter, emblem or lettering.
    // From the lobby (looking toward local -Z), viewer-right is local -X.
    // Reverse only the UV direction: the owner image must not read as a mirror.
    static float U(float x) => Mathf.Clamp01((ImageRight-x)/(ImageRight-ImageLeft));
    static float V(float y) => Mathf.Clamp01((y-MuralBase)/(MuralTop-MuralBase));

    static GameObject RoundedPanel(Transform parent,string name,Material material,
        Vector3 center,Vector2 size,float depth,float radius)
    {
        const int cornerSegments=5;
        float hx=size.x*.5f,hy=size.y*.5f;
        float r=Mathf.Clamp(radius,.001f,Mathf.Min(hx,hy)-.001f);
        var outline=new System.Collections.Generic.List<Vector2>();
        void Arc(float cx,float cy,float a0,float a1)
        {
            for(int i=0;i<=cornerSegments;i++)
            {
                float a=Mathf.Lerp(a0,a1,i/(float)cornerSegments)*Mathf.Deg2Rad;
                outline.Add(new Vector2(cx+r*Mathf.Cos(a),cy+r*Mathf.Sin(a)));
            }
        }
        Arc(hx-r,hy-r,0,90);Arc(-hx+r,hy-r,90,180);
        Arc(-hx+r,-hy+r,180,270);Arc(hx-r,-hy+r,270,360);
        var verts=new System.Collections.Generic.List<Vector3>();
        var tris=new System.Collections.Generic.List<int>();
        float zf=depth*.5f,zb=-zf;
        for(int i=0;i<outline.Count;i++)
        {
            int n=(i+1)%outline.Count;
            int b=verts.Count;
            verts.Add(center+new Vector3(outline[i].x,outline[i].y,zb));
            verts.Add(center+new Vector3(outline[n].x,outline[n].y,zb));
            verts.Add(center+new Vector3(outline[n].x,outline[n].y,zf));
            verts.Add(center+new Vector3(outline[i].x,outline[i].y,zf));
            tris.Add(b);tris.Add(b+2);tris.Add(b+1);
            tris.Add(b);tris.Add(b+3);tris.Add(b+2);
        }
        // front/back caps
        for(int i=0;i<outline.Count;i++)
        {
            int n=(i+1)%outline.Count,b=verts.Count;
            verts.Add(center+new Vector3(0,0,zf));
            verts.Add(center+new Vector3(outline[i].x,outline[i].y,zf));
            verts.Add(center+new Vector3(outline[n].x,outline[n].y,zf));
            tris.Add(b);tris.Add(b+1);tris.Add(b+2);
            b=verts.Count;
            verts.Add(center+new Vector3(0,0,zb));
            verts.Add(center+new Vector3(outline[n].x,outline[n].y,zb));
            verts.Add(center+new Vector3(outline[i].x,outline[i].y,zb));
            tris.Add(b);tris.Add(b+1);tris.Add(b+2);
        }
        var mesh=new Mesh{name=name+" rounded R="+radius};
        mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string safe=name.Replace(' ','_').Replace('|','_');
        AssetDatabase.CreateAsset(mesh,Output+"/"+safe+"_rounded.asset");
        var go=new GameObject(name);go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=material;
        GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.OccludeeStatic);
        return go;
    }

    static void PremiumEntrancePortal(Transform parent)
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("A1 portal CityStaticPBR shader missing");
        var gold=new Material(shader){name="A1 entrance portal clean premium gold",enableInstancing=true};
        gold.SetColor("_Color",new Color(.835f,.639f,.231f));
        gold.SetFloat("_Metallic",.98f);gold.SetFloat("_Roughness",.19f);
        gold.SetFloat("_VertexTint",0f);gold.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(gold,Output+"/A1_entrance_portal_clean_gold.mat");
        var warm=AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/PlayableGenerated/Mistrzowie/Buildings/Materials/light.mat");
        if(!warm)throw new InvalidOperationException("A1 portal warm emissive material missing");
        float cx=(DoorLeft+DoorRight)*.5f;
        float z=WallImageZ+.055f; // in front of mural, no coplanar overlap
        RoundedPanel(parent,"A1 PREMIUM PORTAL | GOLD left",gold,
            new Vector3(DoorLeft-.09f,2.10f,z),new Vector2(.18f,4.20f),.08f,.030f);
        RoundedPanel(parent,"A1 PREMIUM PORTAL | GOLD right",gold,
            new Vector3(DoorRight+.09f,2.10f,z),new Vector2(.18f,4.20f),.08f,.030f);
        RoundedPanel(parent,"A1 PREMIUM PORTAL | GOLD top",gold,
            new Vector3(cx,4.29f,z),new Vector2(4.46f,.18f),.08f,.030f);

        // 25 mm warm LED sits on the gold face, outside the clear 4.10m doorway.
        RoundedPanel(parent,"A1 PREMIUM PORTAL | 3000K LED left",warm,
            new Vector3(DoorLeft-.013f,2.10f,z+.046f),new Vector2(.025f,4.05f),.018f,.010f);
        RoundedPanel(parent,"A1 PREMIUM PORTAL | 3000K LED right",warm,
            new Vector3(DoorRight+.013f,2.10f,z+.046f),new Vector2(.025f,4.05f),.018f,.010f);
        RoundedPanel(parent,"A1 PREMIUM PORTAL | 3000K LED top",warm,
            new Vector3(cx,4.213f,z+.046f),new Vector2(4.10f,.025f),.018f,.010f);
        Debug.Log("A1_PREMIUM_ENTRANCE_PORTAL clearOpening=4.10x4.20 outer=4.46x4.38"+
            " goldProfile=.18x.08 bevelR=.03 LED=.025 noThreshold=true");
    }


    static void AddSegment(Transform parent,Material mural,
        string name,float x0,float x1,float y0,float y1,bool extension)
    {
        if(x1<=x0||y1<=y0)throw new ArgumentOutOfRangeException(name);
        float u0=U(x0),u1=U(x1);
        // Sample exactly one edge column over the monochrome side extensions.
        // The original 82-metre artwork is not cropped, stretched, or recoloured.
        if(extension)
        {
            if(x1<=ImageLeft)u0=u1=.998f;
            else if(x0>=ImageRight)u0=u1=.002f;
        }
        var mesh=new Mesh{name=name+" | original panoramic UV panel"};
        mesh.vertices=new[]{
            new Vector3(x0,y0,WallImageZ),
            new Vector3(x0,y1,WallImageZ),
            new Vector3(x1,y1,WallImageZ),
            new Vector3(x1,y0,WallImageZ)
        };
        // Normal must face local +Z, toward the public lobby, before bowl rotation.
        mesh.uv=new[]{
            new Vector2(u0,V(y0)),new Vector2(u0,V(y1)),
            new Vector2(u1,V(y1)),new Vector2(u1,V(y0))
        };
        mesh.triangles=new[]{0,2,1,0,3,2};
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
        const bool hd=true;
        string source=HdImage;
        if(!File.Exists(source))
        {
            BraxBuildQuality.Warn("ENTRANCE_MURAL = FAIL: required owner HD source missing: "+source+
                "; NO FALLBACK and NO substitute image permitted");
            return false;
        }
        AssetDatabase.ImportAsset(source,ImportAssetOptions.ForceSynchronousImport);
        var image=AssetDatabase.LoadAssetAtPath<Texture2D>(source);
        if(!image)
        {
            BraxBuildQuality.Warn("ENTRANCE_MURAL = FAIL: Unity did not import required owner HD source: "+source);
            return false;
        }
        var importer=AssetImporter.GetAtPath(source) as TextureImporter;
        if(!importer)throw new InvalidDataException("A1 original mural image has no texture importer");
        importer.GetSourceTextureWidthAndHeight(out int sourceWidth,out int sourceHeight);
        if(sourceWidth!=6144||sourceHeight!=2048||sourceWidth!=sourceHeight*3)
        {
            BraxBuildQuality.Warn("ENTRANCE_MURAL = FAIL: required source must remain exact 6144x2048 3:1, got "+
                sourceWidth+"x"+sourceHeight);
            return false;
        }
        bool importerDirty=false;
        if(!importer.mipmapEnabled){importer.mipmapEnabled=true;importerDirty=true;}
        if(importer.wrapMode!=TextureWrapMode.Clamp){importer.wrapMode=TextureWrapMode.Clamp;importerDirty=true;}
        var android=importer.GetPlatformTextureSettings("Android");
        if(!android.overridden||android.maxTextureSize!=4096||
           android.format!=TextureImporterFormat.ASTC_4x4||android.compressionQuality!=100)
        {
            android.overridden=true;android.maxTextureSize=4096;
            android.format=TextureImporterFormat.ASTC_4x4;android.compressionQuality=100;
            importer.SetPlatformTextureSettings(android);importerDirty=true;
        }
        if(importerDirty){importer.SaveAndReimport();image=AssetDatabase.LoadAssetAtPath<Texture2D>(source);}
        if(!image)return false;

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
        PremiumEntrancePortal(walls);
        Debug.Log("ENTRANCE_MURAL = PASS");
        Debug.Log("CHAMPION_A1_MURAL_ORIGINAL_APPLIED hd="+hd+" source="+source+
                  " sourcePixels="+sourceWidth+"x"+sourceHeight+
                  " wall=120m mural=82x27.333m visibleArea=2224.09m2"+
                  " door=4.10x4.20m open noMirror=true noStretch=true sourcePixelsUnmodified=true");
        return true;
    }
}
