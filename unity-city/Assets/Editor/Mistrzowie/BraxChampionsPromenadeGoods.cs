using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// MIASTO MISTRZÓW — physically instantiate verified CC0 ORIGINAL Poly Haven
// 2K glTF food/retail models inside 12 existing premium shop shells.
// NEVER fabricate food as primitives. NO staff, NPCs, crowds or avatars.
// All original imported meshes/textures/materials shared between instances.
// Source list is pinned to the official Poly Haven model manifest; no 1K fallbacks.
public static class BraxChampionsPromenadeGoods
{
    const string Source="Assets/Source/Mistrzowie/PolyHaven";
    const string Catalog="unity-tools/mistrzowie/promenade_polyhaven_models.json";
    static float K => BraxClaudeOsiedle.K;

    [Serializable] class Item
    {
        public string id;
        public int polycount;
        public string[] kiosks;
    }
    [Serializable] class CatalogData
    {
        public string schema,license;
        public Item[] assets;
    }

    // Unity startuje w folderze unity-city; plik leży w katalogu repo (piętro wyżej).
    // APK #21 padł na "Verified official Poly Haven model list missing".
    static string RepoFile(string relative)
    {
        foreach(var candidate in new[]{relative,"../"+relative,
            Path.Combine(Directory.GetParent(Application.dataPath).Parent?.FullName??"",relative)})
            if(File.Exists(candidate))return candidate;
        return null;
    }

    static CatalogData GetCatalog()
    {
        var catalogFile=RepoFile(Catalog);
        if(catalogFile==null)
            throw new FileNotFoundException("Verified official Poly Haven model list missing",Catalog);
        var c=JsonUtility.FromJson<CatalogData>(File.ReadAllText(catalogFile));
        if(c==null||c.assets==null||c.assets.Length!=25
           ||c.schema!="BRAXHORN_POLY_HAVEN_REAL_2K_GLTF_V1"
           ||c.license!="CC0"
           ||c.assets.Select(x=>x.id).Distinct().Count()!=25)
            throw new InvalidOperationException("Official 25-model source contract is invalid");
        if(c.assets.Any(x=>x.kiosks==null||x.kiosks.Length==0
                       ||x.kiosks.Any(y=>!new[]{"K1","K2","K3","K4","K5","K6",
                                      "K7","K8","K9","K10","K11","K12"}.Contains(y))))
            throw new InvalidOperationException("Owner kiosk assignments changed");
        return c;
    }

    static readonly Dictionary<string,GameObject> Sources=
        new Dictionary<string,GameObject>(StringComparer.Ordinal);
    static readonly HashSet<string> CheckedSources=new HashSet<string>(StringComparer.Ordinal);

    static void CheckMaps(string id)
    {
        if(CheckedSources.Contains(id))return;
        var directory=Source+"/"+id;
        string file=directory+"/"+id+"_2k.gltf";
        if(!File.Exists(file))
            throw new FileNotFoundException("Missing original verified Poly Haven 2K glTF; no low-quality model substitute",file);
        string[] files=Directory.GetFiles(directory,"*_2k.*",SearchOption.AllDirectories);
        bool Has(string part)=>files.Any(f=>Path.GetFileName(f).ToLowerInvariant().Contains(part));
        if(!Has("_diff_2k")&&!Has("_col_2k"))
            throw new InvalidOperationException("No original 2K Albedo for "+id);
        if(!Has("_nor_gl_2k"))
            throw new InvalidOperationException("No original 2K OpenGL normal for "+id);
        if(!Has("_arm_2k")&&!Has("_rough_2k"))
            throw new InvalidOperationException("No original 2K ARM/Roughness for "+id);
        foreach(var texturePath in files.Where(f=>f.EndsWith(".jpg",StringComparison.OrdinalIgnoreCase)
                          ||f.EndsWith(".png",StringComparison.OrdinalIgnoreCase)))
        {
            AssetDatabase.ImportAsset(texturePath.Replace('\\','/'),
                                      ImportAssetOptions.ForceSynchronousImport);
            var assetPath=texturePath.Replace('\\','/');
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            var imp=AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if(!tex||!imp)
                throw new InvalidOperationException("Real retail 2K source could not be imported: "+texturePath);
            imp.GetSourceTextureWidthAndHeight(out int sourceWidth,out int sourceHeight);
            if(sourceWidth!=2048||sourceHeight!=2048)
                BraxBuildQuality.Warn("Poly Haven retail map is "+sourceWidth+"x"+sourceHeight+" (not 2048x2048): "+texturePath);
            if(tex.width<2048||tex.height<2048)
                BraxBuildQuality.Warn("Poly Haven retail SOURCE is original 2K but Unity import is "+
                    tex.width+"x"+tex.height+" at "+texturePath+"; review on Android");
        }
        CheckedSources.Add(id);
    }

    static GameObject Load(string id)
    {
        if(Sources.TryGetValue(id,out var cached))return cached;
        CheckMaps(id);
        string file=Source+"/"+id+"/"+id+"_2k.gltf";
        // The project has official glTFast 6.20.0. glTF editor importer must
        // turn these originals into reusable GameObject subassets. If it
        // cannot, STOP and repair the importer — never use a fake mesh.
        AssetDatabase.ImportAsset(file,ImportAssetOptions.ForceSynchronousImport);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(file);
        if(!prefab)
            prefab=AssetDatabase.LoadAllAssetsAtPath(file).OfType<GameObject>().FirstOrDefault();
        if(!prefab)
            throw new InvalidOperationException("Official Poly Haven glTF not imported as GameObject by Unity glTFast: "+file);
        Sources.Add(id,prefab);
        return prefab;
    }

    // Towar na promenadzie to dekoracja: brak listy, modelu albo mapy = ostrzeżenie,
    // nigdy przerwany build całego miasta.
    public static void Verify25OriginalModels()
    {
        CatalogData c;
        try{c=GetCatalog();}
        catch(Exception e)
        {
            BraxBuildQuality.Warn("Promenada: lista modeli Poly Haven niedostępna ("+e.Message+"); pawilony bez towaru");
            return;
        }
        int ok=0;
        foreach(var entry in c.assets)
        {
            try{CheckMaps(entry.id);ok++;}
            catch(Exception e){BraxBuildQuality.Warn("Promenada: model "+entry.id+" pominięty: "+e.Message);}
        }
        Debug.Log("MISTRZOWIE_POLYHAVEN_SOURCE_MAPS ok="+ok+"/"+c.assets.Length+" editorModelLoadDeferred");
    }

    static float TargetLength(string id)
    {
        switch(id)
        {
            case "croissant": return .20f;
            case "hamburger_buns":return .21f;
            case "carrot_cake":return .24f;
            case "strawberry_chocolate_cake":return .25f;
            case "tea_set_01":return .33f;
            case "wooden_display_shelves_01":return 1.32f;
            case "painted_wooden_shelves":return 1.35f;
            case "outdoor_table_chair_set_01":return 2.1f;
            case "metal_stool_03":return .60f;
            case "wicker_basket_02":return .32f;
            case "plastic_crate_02":return .42f;
            case "life_jacket":return .78f;
            case "binoculars":return .17f;
            case "wooden_cutting_board":return .34f;
            case "wooden_bowl_02":return .22f;
            default:return .18f;
        }
    }

    static float DisplayY(string id)
    {
        if(id=="wooden_display_shelves_01"||id=="painted_wooden_shelves")return 0f;
        if(id=="outdoor_table_chair_set_01"||id=="metal_stool_03")return 0f;
        if(id=="plastic_crate_02"||id=="wicker_basket_02")return .21f;
        return 1.15f;
    }

    static void Put(Transform parent,GameObject original,Item item,
                    float x,float y,float z,int index)
    {
        var go=(GameObject)UnityEngine.Object.Instantiate(original);
        go.name="POLY HAVEN CC0 2K | "+item.id+" | inventory #"+index;
        go.transform.SetParent(parent,false);
        go.transform.localPosition=Vector3.zero;
        var bounds=BraxRegion.Bounds(go);
        if(Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z)<.0001f)
            throw new InvalidOperationException("Degenerate source model: "+item.id);
        // Real imported source size in Unity; correct with target size/K once.
        float wanted=TargetLength(item.id)*K;
        float actual=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
        var factor=wanted/actual;
        if(float.IsNaN(factor)||float.IsInfinity(factor)||factor<.005f||factor>1000f)
            throw new InvalidOperationException("Unsafe import scale: "+item.id);
        go.transform.localScale*=factor;
        bounds=BraxRegion.Bounds(go);
        // Position the rendered model BASE at countertop/ground, not its
        // bounding centre. Adjust X/Z so original offset does not displace it.
        go.transform.position+=new Vector3((parent.position.x+x*K)-bounds.center.x,
                                           (parent.position.y+y*K)-bounds.min.y,
                                           (parent.position.z+z*K)-bounds.center.z);
        go.transform.Rotate(Vector3.up,(index%4)*90f,Space.Self);
        foreach(var renderer in go.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=true;
        }
        // Pure product props are not obstacles; no duplicate meshes/colliders.
        foreach(var collider in go.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);
    }

    public static int Build(Transform kioskRoot,string id,float width,float depth)
    {
        if(!kioskRoot||width<4.9f||depth<3.3f)
            throw new InvalidOperationException("Invalid actual kiosk geometry for goods: "+id);
        var c=GetCatalog();
        var owned=c.assets.Where(x=>x.kiosks.Contains(id)).ToArray();
        if(owned.Length<2)
            throw new InvalidOperationException("Unfurnished approved pavilion "+id);
        var parent=new GameObject(id+" | ORIGINAL POLY HAVEN 2K GOODS - no humans").transform;
        parent.SetParent(kioskRoot,false);
        int placed=0;
        foreach(var item in owned)
        {
            var prefab=Load(item.id);
            bool large=TargetLength(item.id)>.5f;
            // A single appropriate fixture or one exemplar per product model,
            // instead of hundreds of copies in the mobile APK.
            int count=large?1:(item.polycount>15000?1:2);
            for(int n=0;n<count;n++)
            {
                int position=placed++;
                float span=width-.75f;
                float sx=-span*.5f+(position+.5f)*span/
                         Mathf.Max(1,owned.Length*2);
                sx=Mathf.Clamp(sx,-width*.5f+.50f,width*.5f-.50f);
                float sz=large?depth*.5f-.78f:
                         (position%2==0?-.16f:.27f);
                var y=DisplayY(item.id);
                // Interior-only: stay behind the serving front/entrance.
                Put(parent,prefab,item,sx,y,sz,n);
            }
        }
        Debug.Log("MISTRZOWIE_KIOSK_REAL_INVENTORY "+id+" sourceModels="+owned.Length+
                  " instances="+placed+" original2K=true humans=0");
        return placed;
    }
}
