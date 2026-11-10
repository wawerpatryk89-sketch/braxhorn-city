using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

// Solid 3D lettering composed from the region letter kit (unity-tools/mistrzowie/build_letter_kit.py -> LetterKit.bytes,
// Montserrat SemiBold, SIL OFL 1.1). Owner 10.11 10:17-10:23: "Globalnie zakaz tanich napisów" - no TextMesh or bitmap
// font anywhere in Miasto Mistrzów (docs/zasady/NAPISY_PREMIUM_ZAKAZ_TANICH_20261110.md). Every short text the
// generators compose (door and room numbers, stop names, arena wayfinding, the vitrine plaque) is built here: tracked
// capitals, real depth, face + return materials chosen by the caller (stainless / graphite / brass / gold), mounted on
// the carrier the caller already builds (plate, stele, sign band), 2 mm proud of it.
public static class BraxChampionsLetters3D
{
    const string KitPath="Assets/Source/Mistrzowie/LetterKit.bytes";
    [Serializable] sealed class KitMesh{public string role;public float[] positions;public int[] triangles;}
    [Serializable] sealed class Glyph{public string c;public float adv;public KitMesh[] meshes;}
    [Serializable] sealed class Kit{public string font;public float space;public Glyph[] glyphs;}
    static Dictionary<char,Glyph> glyphs;
    static float space=.28f;
    public static int Texts,Missing;

    static bool Load()
    {
        if(glyphs!=null)return glyphs.Count>0;
        glyphs=new Dictionary<char,Glyph>();
        if(!File.Exists(KitPath)){BraxBuildQuality.Warn("DO POPRAWKI LETTER_KIT_MISSING "+KitPath+" (build_letter_kit.py)");return false;}
        Kit kit;
        using(var f=File.OpenRead(KitPath))using(var zip=new GZipStream(f,CompressionMode.Decompress))using(var r=new StreamReader(zip))
            kit=JsonUtility.FromJson<Kit>(r.ReadToEnd());
        space=kit.space>0f?kit.space:space;
        foreach(var g in kit.glyphs)if(!string.IsNullOrEmpty(g.c))glyphs[g.c[0]]=g;
        return glyphs.Count>0;
    }

    /// <summary>Width of text (nominal metres) at cap height cap with tracking (fraction of the cap height).</summary>
    public static float Width(string text,float cap,float tracking=.12f)
    {
        if(!Load()||string.IsNullOrEmpty(text))return 0f;
        float pen=0f;int n=0;
        foreach(char raw in text.ToUpperInvariant())
        {
            if(raw==' '){pen+=space;continue;}
            if(!glyphs.TryGetValue(raw,out var g))continue;
            if(n++>0)pen+=tracking;
            pen+=g.adv;
        }
        return pen*cap;
    }

    /// <summary>
    /// Adds text to the batch. baselineCentre = middle of the baseline (nominal); the letters face the direction
    /// (sin yaw, 0, cos yaw) and stand out of the carrier by depth from baselineCentre toward that direction.
    /// maxWidth > 0 shrinks the cap height so the text fits. Returns the final width.
    /// </summary>
    public static float Text(BraxChampionsMeshBatch m,Material face,Material dark,string text,Vector3 baselineCentre,
                             float yaw,float cap,float depth,float tracking=.12f,float maxWidth=0f)
    {
        if(m==null||!face||!dark)throw new ArgumentNullException(nameof(m),"letters need a batch, a face and a return material");
        if(!Load()||string.IsNullOrEmpty(text)){Missing++;return 0f;}
        string s=text.ToUpperInvariant();
        float w=Width(s,cap,tracking);
        if(maxWidth>0f&&w>maxWidth){cap*=maxWidth/w;w=maxWidth;}
        var rot=Quaternion.Euler(0,yaw,0);
        float pen=0f;int n=0;
        foreach(char c in s)
        {
            if(c==' '){pen+=space;continue;}
            if(!glyphs.TryGetValue(c,out var g)){Missing++;BraxBuildQuality.Warn("DO POPRAWKI LETTER_KIT_GLYPH "+c+" in "+text);continue;}
            if(n++>0)pen+=tracking;
            // Unity is left-handed: seen from the front (+Z) the reader's right is -x, so the text runs toward -x;
            // kit glyphs are mirrored (x in -adv..0), the first one sits at the +x end.
            float x0=w*.5f/cap-pen;
            foreach(var km in g.meshes)
            {
                if(km.positions==null||km.triangles==null)continue;
                var mat=km.role=="dark"?dark:face;
                var p=km.positions;
                Vector3 V(int i)=>baselineCentre+rot*new Vector3((p[3*i]+x0)*cap,p[3*i+1]*cap,p[3*i+2]*depth);
                for(int t=0;t<km.triangles.Length;t+=3)
                    m.Triangle(mat,V(km.triangles[t]),V(km.triangles[t+1]),V(km.triangles[t+2]));
            }
            pen+=g.adv;
        }
        Texts++;
        return w;
    }

    public const string MeshFolder="Assets/PlayableGenerated/Mistrzowie/Meshes/Letters";
    /// <summary>Writes the lettering batch under parent (unit: K under an unscaled parent, 1 under a K-scaled one).</summary>
    public static Transform Build(BraxChampionsMeshBatch m,Transform parent,string name,float unit=0f)
    {
        if(m==null||m.Empty)return null;
        Directory.CreateDirectory(MeshFolder);
        return m.Build(parent,name+" | 3D letters",MeshFolder,false,unit);
    }

    /// <summary>Yaw that turns the letters' face toward the given horizontal normal.</summary>
    public static float YawFacing(Vector3 normal)=>Mathf.Atan2(normal.x,normal.z)*Mathf.Rad2Deg;
}
