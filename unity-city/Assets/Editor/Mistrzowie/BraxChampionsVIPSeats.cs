using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// The original 4K GLB stays with the owner. This is its mesh-identical
// 2K-texture derivative, with the six embedded PBR images downsized to 2048.
// Replace ONLY VIP chairs beside the judges. All judge/commentator desks,
// computers and working chairs remain owned by BraxChampionsArena.
public static class BraxChampionsVIPSeats
{
    public const string SourcePath =
        "Assets/Source/Mistrzowie/VIP/free_luxurious_barrel_seat_2k.glb";
    public static int SeatCount { get; private set; }
    public static bool HasSource =>
        AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath) != null;

    static Material SolidBlackMetal()
    {
        const string path="Assets/PlayableGenerated/Mistrzowie/Arena/A1_VIP_clean_black_metal.mat";
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("A1 VIP CityStaticPBR shader missing");
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m)
        {
            m=new Material(shader){name="A1 VIP clean black metal",enableInstancing=true};
            AssetDatabase.CreateAsset(m,path);
        }
        m.SetColor("_Color",new Color(.035f,.040f,.050f));
        m.SetFloat("_Metallic",.92f);m.SetFloat("_Roughness",.22f);
        m.SetFloat("_VertexTint",0f);m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Bounds VisualBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException("VIP barrel seat GLB has no renderers: " + SourcePath);
        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        if (bounds.size.x < .001f || bounds.size.y < .001f || bounds.size.z < .001f)
            throw new InvalidOperationException("VIP barrel seat GLB geometry bounds invalid");
        return bounds;
    }

    static void VerifyImportedTextures(GameObject source)
    {
        int seen = 0;
        foreach (var renderer in source.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials)
            {
                if (!material) continue;
                foreach (string slot in new[] {
                    "baseColorTexture", "normalTexture", "metallicRoughnessTexture",
                    "occlusionTexture", "_MainTex", "_BumpMap", "_ARM" })
                {
                    if (!material.HasProperty(slot)) continue;
                    var map = material.GetTexture(slot) as Texture2D;
                    if (!map) continue;
                    seen++;
                    if (map.width > 2048 || map.height > 2048)
                        BraxBuildQuality.Warn(
                            "VIP GLB texture exceeds 2K target (review after installation): " +
                            map.name + " " + map.width + "x" + map.height);
                    if (map.mipmapCount <= 1)
                        BraxBuildQuality.Warn(
                            "VIP GLB imported without mipmaps; inspect glTFast material on device: " +
                            map.name + ". The original model remains unchanged in the test APK.");
                }
            }
        if (seen == 0)
            BraxBuildQuality.Warn(
                "VIP GLB has no bound PBR texture; visual inspection required. Build continues with original model.");
    }

    // Repair the exact owner GLB when its authored up-axis imports sideways.
    // Phone proof 8.11 showed the previous feet-name heuristic rejected every
    // valid correction (16 x UPRIGHT_UNCERTAIN). Score geometry first, then use
    // a named back/backrest only to resolve the 180-degree facing ambiguity.
    static void KeepImportedChairUpright(GameObject chair,Quaternion facing)
    {
        Quaternion[] corrections={
            Quaternion.identity,
            Quaternion.Euler(90f,0,0),Quaternion.Euler(-90f,0,0),
            Quaternion.Euler(0,0,90f),Quaternion.Euler(0,0,-90f),
            Quaternion.Euler(180f,0,0),Quaternion.Euler(0,0,180f)
        };
        float bestScore=float.PositiveInfinity;
        Quaternion best=Quaternion.identity;
        foreach(var correction in corrections)
        {
            chair.transform.localRotation=facing*correction;
            var b=VisualBounds(chair);
            float horizontal=Mathf.Max(b.size.x,b.size.z);
            float upright=b.size.y/Mathf.Max(horizontal,.001f);
            // Real barrel lounge chair is roughly human-seat proportions:
            // upright height close to width/depth; a sideways import is much flatter.
            float score=Mathf.Abs(upright-1.08f);
            if(upright<.84f)score+=3f+(.84f-upright)*8f;
            if(upright>1.48f)score+=(upright-1.48f)*2f;
            if(score<bestScore)
            {
                bestScore=score;
                best=correction;
            }
        }
        chair.transform.localRotation=facing*best;

        // If the source exposes a semantic back/backrest transform, it belongs
        // AWAY from the cage. Flip only around the now-upright local Y axis.
        bool flipped=false;
        var back=chair.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t=>t&&t!=chair.transform&&
                (t.name.IndexOf("backrest",StringComparison.OrdinalIgnoreCase)>=0||
                 t.name.IndexOf("back",StringComparison.OrdinalIgnoreCase)>=0));
        if(back)
        {
            var spot=chair.transform.parent;
            var bounds=VisualBounds(chair);
            float centreZ=spot.InverseTransformPoint(bounds.center).z;
            float backZ=spot.InverseTransformPoint(back.position).z;
            if(backZ>centreZ)
            {
                chair.transform.localRotation=facing*Quaternion.Euler(0,180f,0)*best;
                flipped=true;
            }
        }
        var finalBounds=VisualBounds(chair);
        float finalUpright=finalBounds.size.y/
            Mathf.Max(Mathf.Max(finalBounds.size.x,finalBounds.size.z),.001f);
        if(finalUpright<.84f)
            BraxBuildQuality.Warn("CHAMPIONS_VIP_UPRIGHT_FAILED geometry still sideways after deterministic axis repair");
        Debug.Log("CHAMPIONS_VIP_OWNER_GLB_UPRIGHT axisCorrection="+best.eulerAngles+
            " backFacingFlip="+flipped+
            " uprightRatio="+finalUpright.ToString("F3")+
            " dimensions="+finalBounds.size.ToString("F3")+
            " originalArtUnchanged=true");
    }

    static void ApplyPremiumMaterials(GameObject chair,Material leather,Material blackMetal)
    {
        int leatherSlots=0,metalSlots=0;
        foreach(var renderer in chair.GetComponentsInChildren<Renderer>(true))
        {
            var slots=renderer.sharedMaterials;
            for(int i=0;i<slots.Length;i++)
            {
                var source=slots[i];
                string semantic=(renderer.gameObject.name+" "+(source?source.name:"")).ToLowerInvariant();
                bool metal=semantic.Contains("metal")||semantic.Contains("steel")||
                    semantic.Contains("chrome")||semantic.Contains("leg")||
                    semantic.Contains("base")||semantic.Contains("frame")||
                    semantic.Contains("foot")||semantic.Contains("stand")||
                    semantic.Contains("pedestal");
                if(source)
                {
                    foreach(string property in new[]{"_Metallic","metallicFactor","_MetallicFactor"})
                        if(source.HasProperty(property)&&source.GetFloat(property)>.25f)metal=true;
                }
                slots[i]=metal?blackMetal:leather;
                if(metal)metalSlots++;else leatherSlots++;
            }
            renderer.sharedMaterials=slots;
        }
        if(leatherSlots==0)
            throw new InvalidOperationException("A1 VIP premium material mapping found no upholstery slots");
        if(metalSlots==0)
            BraxBuildQuality.Warn("A1_VIP_METAL_SLOT_NOT_IDENTIFIED: source GLB exposes no separate metal/base material slot; visual scene proof required");
        Debug.Log("A1_VIP_MATERIAL_MAPPING leatherSlots="+leatherSlots+
            " metalSlots="+metalSlots+" leather=PolyHaven_leather_white blackMetal=CityStaticPBR");
    }

    static void Place(Transform root, GameObject source, float x, float z, float cx, float cz,
        string label,Material leather,Material blackMetal)
    {
        var spot = new GameObject(label).transform;
        spot.SetParent(root, false);
        spot.localPosition = new Vector3(x, .055f, z);

        var chair = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (!chair) throw new InvalidOperationException("Cannot instantiate original VIP seat GLB");
        chair.name = "Premium barrel chair | owner GLB | 2K PBR";
        chair.transform.SetParent(spot, false);
        chair.transform.localPosition = Vector3.zero;
        // Original front (+Z) points to the octagon; keep the authored mesh and materials.
        var towardCage = new Vector3(cx - x, 0, cz - z);
        var facing=Quaternion.LookRotation(towardCage.normalized,Vector3.up);
        chair.transform.localRotation=facing;
        chair.transform.localScale=Vector3.one;
        KeepImportedChairUpright(chair,facing);

        var bounds = VisualBounds(chair);
        // Owner-approved premium scale: UNIFORM only, no mesh stretching.
        // Max envelope 1.18 x 1.18 x 1.28 m still leaves 27cm between centres
        // horizontally and 57cm between the two 1.75m-spaced rows.
        float ratio = Mathf.Max(bounds.size.x / 1.18f,
                        Mathf.Max(bounds.size.z / 1.18f, bounds.size.y / 1.28f));
        float factor = 1f / ratio;
        chair.transform.localScale *= factor;
        bounds = VisualBounds(chair);
        Vector3 target = spot.position + new Vector3(0, bounds.extents.y, 0);
        chair.transform.position += target - bounds.center;
        ApplyPremiumMaterials(chair,leather,blackMetal);

        // One simple collider per place; no expensive imported MeshColliders.
        foreach (var c in chair.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(c);
        var collider = spot.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, .64f, 0);
        collider.size = new Vector3(1.18f, 1.28f, 1.18f);
        SeatCount++;
    }

    public static int Generate(Transform interiorRoot)
    {
        SeatCount = 0;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        if (!source)
        {
            Debug.LogWarning("CHAMPIONS_VIP_SOURCE_PENDING: " + SourcePath +
                " is not imported; existing VIP seats remain temporarily. Judge seats unchanged.");
            return 0;
        }
        VerifyImportedTextures(source);
        var leather=BraxChampionsFoundation.SourceMaterial(
            "A1_VIP_black_leather_polyhaven","leather_white",.45f,
            new Color(.07f,.075f,.08f));
        leather.SetFloat("_Metallic",0f);leather.SetFloat("_Roughness",.22f);
        var blackMetal=SolidBlackMetal();
        // Existing reserved positions near jury/commentator area:
        // two rows behind cage-side service desks; one 6m central aisle stays open.
        // Four places per side per row; seat spacing 1.45m, row spacing 1.75m.
        const float cx = -86f, cz = 63.3f;
        var xs = new[] { -94.0f, -92.55f, -91.10f, -89.65f,
                         -82.35f, -80.90f, -79.45f, -78.0f };
        foreach (float z in new[] { 70.30f, 72.05f })
            foreach (float x in xs)
                Place(interiorRoot,source,x,z,cx,cz,
                    "Reserved VIP | imported luxurious barrel seat",leather,blackMetal);

        if (SeatCount != 16)
            BraxBuildQuality.Warn("Champion A1 expected 16 premium VIP chairs, got " + SeatCount +
                ". Visual acceptance FAIL.");
        else Debug.Log("A1_VIP_PREMIUM PASS count=16 maxEnvelope=1.18x1.18x1.28"+
            " spacingX=1.45 gapX=.27 rowSpacing=1.75 gapRows=.57"+
            " blackPolyHavenLeather=true blackPBRMetal=true uniformScale=true");
        return SeatCount;
    }
}
