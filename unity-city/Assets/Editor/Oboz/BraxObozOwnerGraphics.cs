using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Właścicielskie grafiki na prawdziwej architekturze, bez regenerowania logo/napisów.
// Źródła JPEG są prywatne. Repozytorium publiczne zawiera TYLKO kod integracji.
// To jest dodatkowy fragment generatora otoczenia. Nie dotyka postaci.
public static partial class BraxObozEnvironment
{
    const string OwnerGraphicsSource =
        "Assets/Source/Prywatne/Oboz/OwnerAssets/MURALE_I_SZYLD/";
    const string OwnerGraphicsOutput =
        "Assets/PlayableGenerated/Oboz/OwnerGraphics/";

    static Texture2D OwnerImage(string filename)
    {
        var path=OwnerGraphicsSource+filename;
        if(!File.Exists(path))
        {
            Debug.LogWarning("BRAXHORN_OWNER_GRAPHICS_PENDING: brak oryginalnego JPEG "+path);
            return null;
        }
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)throw new InvalidOperationException("Nieprawidlowy importer JPEG: "+path);
        var android=importer.GetPlatformTextureSettings("Android");
        bool changed=importer.textureType!=TextureImporterType.Default ||
            !importer.sRGBTexture || !importer.mipmapEnabled || importer.maxTextureSize!=2048 ||
            importer.filterMode!=FilterMode.Trilinear || importer.wrapMode!=TextureWrapMode.Clamp ||
            !android.overridden || android.maxTextureSize!=2048 ||
            android.format!=TextureImporterFormat.ASTC_4x4;
        if(changed)
        {
            importer.textureType=TextureImporterType.Default;
            importer.sRGBTexture=true;
            importer.maxTextureSize=2048; // źródło 1536 px pozostaje źródłem 1536 px
            importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
            importer.anisoLevel=8;importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.compressionQuality=100;
            android.name="Android";android.overridden=true;
            android.maxTextureSize=2048;
            android.format=TextureImporterFormat.ASTC_4x4;
            android.compressionQuality=100;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(!texture)throw new InvalidOperationException("Nie można załadować JPEG: "+path);
        if(texture.width<2048)
            Debug.LogWarning("BRAXHORN_OWNER_GRAPHICS_SOURCE_LT_2K: "+filename+
                " źródło="+texture.width+"x"+texture.height+
                " (nie wolno udawać, że oryginał ma 2K)");
        return texture;
    }

    static Material OwnerGraphicMaterial(string id,Texture2D texture,bool transparent)
    {
        Directory.CreateDirectory(OwnerGraphicsOutput);AssetDatabase.Refresh();
        string path=OwnerGraphicsOutput+id+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material)
        {
            var shader=Shader.Find("Standard");
            if(!shader)throw new InvalidOperationException("Brak shadera Standard PBR");
            material=new Material(shader){name="BRAXHORN — "+id};
            AssetDatabase.CreateAsset(material,path);
        }
        material.SetTexture("_MainTex",texture);
        material.SetColor("_Color",Color.white);
        material.SetFloat("_Metallic",0f);
        material.SetFloat("_Glossiness",.10f);
        if(transparent)
        {
            material.SetFloat("_Mode",1f);
            material.SetInt("_SrcBlend",(int)BlendMode.One);
            material.SetInt("_DstBlend",(int)BlendMode.Zero);
            material.SetFloat("_Cutoff",.5f);
            material.SetInt("_ZWrite",1);
            material.EnableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType","TransparentCutout");
            material.renderQueue=(int)RenderQueue.AlphaTest;
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    static void PrintedPanel(Transform parent,string name,Vector3 center,
        Quaternion rotation,float width,float height,Material material)
    {
        var panel=Group(name,parent,P(center.x,center.y,center.z));
        panel.localRotation=rotation;
        var mesh=new Mesh{name=name+" — 1:1 UV bez kadrowania"};
        mesh.vertices=new[]{
            P(-width*.5f,-height*.5f,0),P(width*.5f,-height*.5f,0),
            P(width*.5f,height*.5f,0),P(-width*.5f,height*.5f,0)};
        // Normalna frontu to +Z; odwrócone U utrzymuje czytelne, nieodwrócone napisy
        // dla kamery oglądającej przednią stronę panelu.
        mesh.uv=new[]{new Vector2(1,0),new Vector2(0,0),new Vector2(0,1),new Vector2(1,1)};
        mesh.triangles=new[]{0,1,2,0,2,3};
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        panel.gameObject.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh);
        var renderer=panel.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;
        renderer.receiveShadows=true;
    }

    static void BuildingMural(Transform building,string filename,string id,
        Vector3 center,Quaternion facing,float width, bool hasWall)
    {
        var texture=OwnerImage(filename);
        if(!texture)return; // nie montujemy pustych tablic udających dostarczony mural
        float ratio=texture.width/(float)texture.height;
        if(Mathf.Abs(ratio-16f/9f)>.05f)
            throw new InvalidOperationException("Nieprawidłowe proporcje muralu: "+filename);
        float height=width/ratio;
        if(hasWall)
        {
            // Dodatkowa realna ściana malarska osadzona w budynku i wsparta konstrukcją.
            // Sam mural jest cienką nadrukowaną warstwą na ścianie.
            var backing=Group("Nośnik muralu — drewniana ściana budynku",building,
                P(center.x,center.y,center.z));
            backing.localRotation=facing;
            Box(backing,"Ściana pod mural",new Vector3(0,0,.08f),
                new Vector3(width+.35f,height+.35f,.16f),wood);
            PrintedPanel(backing,"MURAL — "+id,new Vector3(0,0,.17f),Quaternion.identity,
                width,height,OwnerGraphicMaterial("Mural_"+id,texture,false));
        }
        else
        {
            PrintedPanel(building,"MURAL — "+id,center,facing,width,height,
                OwnerGraphicMaterial("Mural_"+id,texture,false));
        }
    }

    // Usunięcie samego BIAŁEGO TŁA JPEG, nie treści szyldu.
    // Flood fill od brzegów obrazu: białe napisy wewnątrz sylwetki pozostają.
    static Texture2D RemoveOuterWhiteForGate(string originalFilename)
    {
        string source=OwnerGraphicsSource+originalFilename;
        if(!File.Exists(source))return null;
        Directory.CreateDirectory(OwnerGraphicsOutput);
        string output=OwnerGraphicsOutput+"BraxhornGateSign_TransparentOriginal.png";
        var sourceBytes=File.ReadAllBytes(source);
        var decoded=new Texture2D(2,2,TextureFormat.RGBA32,false);
        if(!decoded.LoadImage(sourceBytes,false))
            throw new InvalidOperationException("Nie można odczytać oryginalnego szyldu JPEG");
        int w=decoded.width,h=decoded.height,n=w*h;
        var pixels=decoded.GetPixels32();
        var removed=new bool[n];
        var queue=new int[n];
        int front=0,back=0;
        bool White(int i)=>pixels[i].r>=243&&pixels[i].g>=243&&pixels[i].b>=243;
        void Enqueue(int i)
        {
            if(i<0||i>=n||removed[i]||!White(i))return;
            removed[i]=true;queue[back++]=i;
        }
        for(int x=0;x<w;x++){Enqueue(x);Enqueue((h-1)*w+x);}
        for(int y=0;y<h;y++){Enqueue(y*w);Enqueue(y*w+w-1);}
        while(front<back)
        {
            int i=queue[front++],x=i%w,y=i/w;
            if(x>0)Enqueue(i-1);if(x+1<w)Enqueue(i+1);
            if(y>0)Enqueue(i-w);if(y+1<h)Enqueue(i+w);
        }
        for(int i=0;i<n;i++)if(removed[i])pixels[i].a=0;
        var processed=new Texture2D(w,h,TextureFormat.RGBA32,false);
        processed.SetPixels32(pixels);processed.Apply(false);
        var png=processed.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(processed);
        UnityEngine.Object.DestroyImmediate(decoded);
        bool shouldWrite=!File.Exists(output)||!File.ReadAllBytes(output).SequenceEqual(png);
        if(shouldWrite){File.WriteAllBytes(output,png);AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceUpdate);}
        var importer=AssetImporter.GetAtPath(output) as TextureImporter;
        if(importer)
        {
            var android=importer.GetPlatformTextureSettings("Android");
            if(importer.maxTextureSize!=2048 || !importer.alphaIsTransparency ||
                !android.overridden || android.format!=TextureImporterFormat.ASTC_4x4)
            {
                importer.maxTextureSize=2048;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
                importer.wrapMode=TextureWrapMode.Clamp;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;
                importer.compressionQuality=100;
                android.name="Android";android.overridden=true;
                android.maxTextureSize=2048;android.format=TextureImporterFormat.ASTC_4x4;
                android.compressionQuality=100;
                importer.SetPlatformTextureSettings(android);
                importer.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(output);
    }

    // Prawdziwa bryła nad wjazdem, szerokość równa światłu bramy 8.4 m.
    // Oryginalne logo i litery pochodzą WYŁĄCZNIE z właścicielskiego JPEG.
    static bool GateOwnerSign(Transform gate)
    {
        const string filename="44B8F5D6-7F27-4EF3-A804-9BC8C4F462D0.jpeg";
        if(!OwnerImage(filename))return false;
        var sign=RemoveOuterWhiteForGate(filename);
        if(!sign)throw new InvalidOperationException("Brak przezroczystej kopii szyldu");
        float width=8.4f,height=width*sign.height/sign.width;
        var header=Group("BRAXHORN TRAINING CAMP — bryla szyldu 3D",gate,Vector3.zero);
        var black=BraxObozMaterials.Plain("szyld-metal-3D",new Color(.035f,.039f,.042f),.37f,.72f);
        // JPG ma białe marginesy nad i pod samą rzeźbą szyldu. Nośnik
        // nie może być wielkim czarnym prostokątem poza obrysem grafiki.
        // Masywny korpus ma 24 cm grubości, ale tylko 2.62 m wysokości;
        // przezroczyste fragmenty ornamentów nie są zasłaniane tłem nośnika.
        Box(header,"Metalowy korpus szyldu — grubość 24 cm",
            new Vector3(0,5.57f,.04f),new Vector3(width,2.62f,.24f),black);
        Box(header,"Nadbudowa pod centralny emblemat byka",
            new Vector3(0,6.42f,.04f),new Vector3(2.15f,1.02f,.26f),black);
        Box(header,"Belka przenoszaca obciazenie na slupy bramy",
            new Vector3(0,3.33f,.22f),new Vector3(9.0f,.28f,.42f),campusSteel);
        foreach(float x in new[]{-4f,4f})
            Box(header,"Stalowa podpora szyldu",
                new Vector3(x,5.1f,.3f),new Vector3(.18f,4.4f,.30f),campusSteel);
        PrintedPanel(header,"Oryginalny szyld BRAXHORN — bez białego tła",
            new Vector3(0,5.55f,-.145f),Quaternion.Euler(0,180f,0),
            width,height,OwnerGraphicMaterial("Oryginalny_Szyld_BRAXHORN",sign,true));
        Debug.Log("BRAXHORN_OWNER_GRAPHICS_GATE: oryginalny JPG "+sign.width+"x"+sign.height+
            " na fizycznej bryle 8.4 m, UV i napisy bez zmian; Android wymaga testu");
        return true;
    }

    static void PlaceOwnerMurals()
    {
        var transforms=root.GetComponentsInChildren<Transform>(true);
        Transform Exact(string label)
        {
            var found=transforms.FirstOrDefault(t=>t.name==label);
            if(!found)throw new InvalidOperationException("Nie znaleziono ściany/budynku muralu: "+label);
            return found;
        }
        var cabin1=Exact("Domek 1");
        var cabin3=Exact("Domek 3");
        // Murale na rzeczywistych drewnianych elewacjach, bez wycinania obrazu.
        BuildingMural(cabin1,"87405F34-A95B-4AD6-8637-A19599AADA61.jpeg",
            "OBOZ_WOJOWNIKOW",new Vector3(0,1.55f,4.17f),
            Quaternion.identity,4.6f,false);
        BuildingMural(cabin3,"6C3045DC-051D-4B53-8F6E-FEA579A0DE21.jpeg",
            "DROGA_MISTRZA",new Vector3(0,1.55f,4.17f),
            Quaternion.identity,4.6f,false);
        // Ściana między słupami pawilonu: mural jest na budynku, nie w powietrzu.
        BuildingMural(Exact("Pawilon treningowy — otwarty, bez starej siłowni"),
            "35502A48-F7AB-4650-9874-3266881B2DF6.jpeg","FIGHT_ZONE",
            new Vector3(-15f,2.7f,-7.50f),Quaternion.Euler(0,180,0),8.8f,true);
        // Zachodnia osłona siłowni pełni też funkcję ściany z muralem.
        BuildingMural(Exact("BRAXHORN · SILOWNIA POD DACHEM"),
            "6B6E1CA3-605A-47FB-BF0E-F23B1D2C3301(1).jpeg",
            "TRAINING_CAMP",new Vector3(-12.58f,2.65f,0),
            Quaternion.Euler(0,-90f,0),8.5f,true);
    }
}
