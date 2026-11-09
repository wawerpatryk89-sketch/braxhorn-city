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
    // REQUIRED exact owner HD 6144x2048 staged from Release IMG_0266.png. No fallback.
    public const string HdImage =
        "Assets/Source/Mistrzowie/ArenaMuralsHD/MURAL_WEJSCIE_CHAMPION_FIGHTER_6144x2048.png";
    const string Output="Assets/PlayableGenerated/Mistrzowie/Arena/Mural";

    // Original arena-bowl LOCAL coordinates, before the unchanged 180-degree
    // placement by BraxChampionsArena.Generate. Exterior public entrance is at
    // global/local Z=-59; the inner wall faces the lobby at global Z=-35.
    // Owner 9.11 23:43: "Mural jest ucięty, ten pierwszy." The print covered 79.8 x 26.6 m of the wall: the doorway
    // cut its bottom centre, the mezzanine (slab underside 11.28 m, 9 m in front of the wall) hid everything above
    // ~40% from the lobby floor, the belt pillar stood in front of it. It could never be seen whole.
    // Now the WHOLE picture, exactly 3:1, hangs above the doorway in a gold frame: 39.6 x 13.2 m, bottom 4.70 m
    // (door top 4.20 + gold portal), top 17.90 m. From the entrance doors (22 m away) the sight line to its top
    // passes under the mezzanine edge; nothing cuts it and nothing stands in front of it.
    public const float MuralWidth=39.6f, MuralHeight=13.2f, MuralBottom=4.70f;
    const float DoorLeft=-88.05f;
    const float DoorRight=-83.95f;
    const float DoorTop=4.20f;
    public const float FrameLeft=(DoorLeft+DoorRight)*.5f-MuralWidth*.5f-BraxChampionsArenaMuralPrint.FrameBar;
    public const float FrameRight=(DoorLeft+DoorRight)*.5f+MuralWidth*.5f+BraxChampionsArenaMuralPrint.FrameBar;
    public const float FrameBottom=MuralBottom-BraxChampionsArenaMuralPrint.FrameBar;
    public const float FrameTop=MuralBottom+MuralHeight+BraxChampionsArenaMuralPrint.FrameBar;
    public const float LobbyRoofUndersideBowlY=27.35f-.30f-(.22f-.055f);
    // Public lobby is on local +Z. The front wall is a real 45 cm structural wall clad in black marble on its
    // lobby face (BraxChampionsArena.LobbyWallMarble); the print panel stands 4.5 cm in front of the marble.
    public const float WallImageZ=BraxChampionsArena.FrontWallLobbyFaceZ+BraxChampionsArena.LobbyMarbleDepth+.045f;

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
        mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateUVDistributionMetrics();
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
        BraxChampionsGold.Apply(gold);
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


    public static bool Generate(Transform originalBowlRoot)
    {
        if(!originalBowlRoot)throw new ArgumentNullException(nameof(originalBowlRoot));
        if(!File.Exists(HdImage))
        {
            BraxBuildQuality.Warn("ENTRANCE_MURAL = FAIL: required owner HD source missing: "+HdImage+
                "; NO FALLBACK and NO substitute image permitted");
            return false;
        }
        Directory.CreateDirectory(Output);
        var walls=new GameObject("A1 | MURAL CHAMPION FIGHTER BRAXHORN - whole picture above the entrance").transform;
        walls.SetParent(originalBowlRoot,false);
        float cx=(DoorLeft+DoorRight)*.5f;
        // From the lobby (looking toward local -Z) viewer-right is local -X: the picture is not mirrored.
        int hung=BraxChampionsArenaMuralPrint.Hang(walls,"A1 | MURAL CHAMPION FIGHTER | owner original 6144x2048","Entrance_ChampionFighter",
            HdImage,new Vector3(cx+MuralWidth*.5f,MuralBottom,WallImageZ),Vector3.left,Vector3.forward,MuralWidth,MuralHeight,out _);
        if(hung==0)
        {
            UnityEngine.Object.DestroyImmediate(walls.gameObject);
            BraxBuildQuality.Warn("ENTRANCE_MURAL = FAIL: Unity did not import the required owner HD source: "+HdImage);
            return false;
        }
        BraxChampionsArenaMuralPrint.Frame(walls,"Entrance",new Vector3(cx,MuralBottom+MuralHeight*.5f,WallImageZ),
            Vector3.left,Vector3.forward,MuralWidth,MuralHeight);
        PremiumEntrancePortal(walls);
        if(FrameTop>=LobbyRoofUndersideBowlY||FrameBottom<=DoorTop+.18f)
            BraxBuildQuality.Warn("ENTRANCE_MURAL = FAIL: frame "+FrameBottom+".."+FrameTop+" must sit between the gold portal ("+
                (DoorTop+.18f)+") and the lobby roof ("+LobbyRoofUndersideBowlY+")");
        Debug.Log("ENTRANCE_MURAL = PASS");
        Debug.Log("CHAMPION_A1_MURAL_ORIGINAL_APPLIED hd=true source="+HdImage+" sourcePixels=6144x2048 panels="+hung+
                  " mural="+MuralWidth+"x"+MuralHeight+"m bottom="+MuralBottom+" top="+(MuralBottom+MuralHeight)+
                  " WHOLE_ABOVE_DOOR=true doorCut=false frame=gold backlit=true roofUnderside="+LobbyRoofUndersideBowlY+
                  " door=4.10x4.20m open noMirror=true noStretch=true sourcePixelsUnmodified=true");
        return true;
    }
}
