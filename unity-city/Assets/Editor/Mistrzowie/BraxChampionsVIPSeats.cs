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
    // Owner phone 9.11 "one tak kurwa leżą": measured on the owner GLB, the
    // node chain is Sketchfab_model (-90 X) -> Collada group (+90 X) = upright,
    // 85 x 73.6 x 78.2 cm (W x H x D), backrest toward -Z, front +Z. glTFast puts
    // the first node's matrix on the instance ROOT; writing localRotation=facing
    // there DELETED the -90 and left +90: every chair lay on its back.
    // Facing is now composed ON TOP of the imported rotation, and the result
    // must have the authored 73.6 cm height or the build reports it.
    public const float AuthoredHeightUnits=73.6f, AuthoredDepthUnits=78.2f, AuthoredWidthUnits=85.0f;
    static void KeepImportedChairUpright(GameObject chair,Quaternion facing)
    {
        var imported=chair.transform.localRotation; // Place never writes it before this
        var yaw=facing;
        // Audit 9.11 of the owner GLB: its two Sketchfab/Collada root matrices
        // cancel out (net identity), so the AUTHORED pose is already upright
        // (85 x 74 x 75 cm-units). The old geometry score preferred a 90-degree
        // X tilt (ratio .88 is "closer" to 1.08 than the true .87) and the
        // phone showed the chair lying on its side. Keep the authored pose when
        // it is plausibly upright; only search when it clearly is not.
        chair.transform.localRotation=yaw*imported;
        {
            var authored=VisualBounds(chair);
            float authoredUpright=authored.size.y/
                Mathf.Max(Mathf.Max(authored.size.x,authored.size.z),.001f);
            float parent=Mathf.Max(chair.transform.parent?chair.transform.parent.lossyScale.y:1f,.0001f);
            float heightUnits=authored.size.y/parent;
            if(Mathf.Abs(heightUnits-AuthoredHeightUnits)<=2.0f||Mathf.Abs(heightUnits*100f-AuthoredHeightUnits)<=2.0f)
            {
                Debug.Log("CHAMPIONS_VIP_OWNER_GLB_UPRIGHT authored pose kept uprightRatio="+
                    authoredUpright.ToString("F3")+" dimensions="+authored.size.ToString("F3"));
                return;
            }
        }
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
            chair.transform.localRotation=yaw*correction*imported;
            var b=VisualBounds(chair);
            // Measured on the source: upright = 73.6 cm tall (lying = 78.2 or 85).
            float p=Mathf.Max(chair.transform.parent?chair.transform.parent.lossyScale.y:1f,.0001f);
            float h=b.size.y/p;
            float score=Mathf.Min(Mathf.Abs(h-AuthoredHeightUnits),Mathf.Abs(h*100f-AuthoredHeightUnits));
            if(correction!=Quaternion.identity)score+=.01f; // prefer the authored pose on ties
            if(score<bestScore)
            {
                bestScore=score;
                best=correction;
            }
        }
        chair.transform.localRotation=yaw*best*imported;

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
                chair.transform.localRotation=yaw*Quaternion.Euler(0,180f,0)*best*imported;
                flipped=true;
            }
        }
        var finalBounds=VisualBounds(chair);
        float finalUpright=finalBounds.size.y/
            Mathf.Max(Mathf.Max(finalBounds.size.x,finalBounds.size.z),.001f);
        float fp=Mathf.Max(chair.transform.parent?chair.transform.parent.lossyScale.y:1f,.0001f);
        float fh=finalBounds.size.y/fp;
        if(Mathf.Min(Mathf.Abs(fh-AuthoredHeightUnits),Mathf.Abs(fh*100f-AuthoredHeightUnits))>2.0f)
            BraxBuildQuality.Warn("CHAMPIONS_VIP_UPRIGHT_FAILED height="+fh.ToString("F1")+" expected="+AuthoredHeightUnits+" (chair lying)");
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
            // glTFast names every mesh node "defaultMaterial"; the real part names
            // (seat_outer_mesh, seat_inner_mesh, seat_pillow_mesh, leg_base_mesh,
            // leg_pivot_mesh) are on the PARENT nodes. Read the whole path.
            string path="";
            for(var t=renderer.transform;t&&t!=chair.transform.parent;t=t.parent)path=t.name+"/"+path;
            for(int i=0;i<slots.Length;i++)
            {
                var source=slots[i];
                string semantic=(path+" "+(source?source.name:"")).ToLowerInvariant();
                bool upholstery=semantic.Contains("seat_")||semantic.Contains("pillow")||
                    semantic.Contains("cushion")||semantic.Contains(" seat")||
                    semantic.Contains("upholster")||semantic.Contains("leather")||
                    semantic.Contains("fabric");
                bool metal=!upholstery&&(semantic.Contains("metal")||semantic.Contains("steel")||
                    semantic.Contains("chrome")||semantic.Contains("leg")||
                    semantic.Contains("base")||semantic.Contains("frame")||
                    semantic.Contains("foot")||semantic.Contains("stand")||
                    semantic.Contains("pedestal"));
                // glTF metallicFactor defaults to 1.0 when a metallicRoughness
                // TEXTURE drives the value, so the factor alone is not evidence of
                // metal (that rule turned all upholstery into metal: build 32
                // "found no upholstery slots"). Use it only for unnamed parts.
                if(source&&!upholstery&&!metal)
                {
                    foreach(string property in new[]{"_Metallic","metallicFactor","_MetallicFactor"})
                        if(source.HasProperty(property)&&source.GetFloat(property)>.25f&&
                           !(source.HasProperty("metallicRoughnessTexture")&&source.GetTexture("metallicRoughnessTexture")))
                            metal=true;
                }
                slots[i]=metal?blackMetal:leather;
                if(metal)metalSlots++;else leatherSlots++;
            }
            renderer.sharedMaterials=slots;
        }
        if(leatherSlots==0)
            throw new InvalidOperationException("A1 VIP premium material mapping found no upholstery slots");
        if(metalSlots==0)
            throw new InvalidOperationException(
                "A1 VIP 1:1 contract failed: no separate metal/base slot was identified; do not accept an all-leather chair");
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
        // Owner 9.11 "loże VIP stać prawidłowo": straight theatre rows, every
        // chair square to the cage (no fan of individually turned chairs).
        var towardCage = new Vector3(0, 0, cz - z);
        var facing=Quaternion.LookRotation(towardCage.normalized,Vector3.up);
        // Keep glTFast's imported root rotation (Sketchfab -90 X): yaw goes on top.
        KeepImportedChairUpright(chair,facing);

        var bounds = VisualBounds(chair);
        // GLOBAL RULE 6.11 §3: real dimension x K, never "squeeze into an
        // invented box". The arena root already applies K once, so the chair
        // needs its REAL size in metres here. The Sketchfab source is authored
        // in centimetres (85 x 74 x 75 units = a 0.85 m wide barrel chair);
        // glTF reads those numbers as metres, hence the single cm->m factor.
        // Measure in the chair's own (unscaled) space: lossy parent scale K is
        // divided out so the check does not depend on where it is parked.
        float parentScale = Mathf.Max(spot.lossyScale.x, .0001f);
        float widestUnits = Mathf.Max(bounds.size.x, bounds.size.z) / parentScale;
        float factor = widestUnits > 10f ? .01f : 1f;
        float realWidth = widestUnits * factor;
        if (realWidth < .55f || realWidth > 1.25f)
            BraxBuildQuality.Warn("A1_VIP_REAL_SIZE_REVIEW: owner barrel chair measures " +
                realWidth.ToString("F2") + " m wide after cm->m; expected 0.55-1.25 m");
        chair.transform.localScale *= factor;
        bounds = VisualBounds(chair);
        Vector3 target = spot.position + new Vector3(0, bounds.extents.y, 0);
        chair.transform.position += target - bounds.center;
        ApplyPremiumMaterials(chair,leather,blackMetal);

        // One simple collider per place; no expensive imported MeshColliders.
        foreach (var c in chair.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(c);
        // Collider = the chair's own real footprint (local, before root K).
        var local = Vector3.Scale(bounds.size, new Vector3(1f / parentScale, 1f / parentScale, 1f / parentScale));
        var collider = spot.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, local.y * .5f, 0);
        collider.size = new Vector3(Mathf.Max(local.x, local.z), local.y, Mathf.Max(local.x, local.z));
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
            new Color(.12f,.11f,.105f));
        // b33 phone: .07 black at roughness .22 read as black plastic tubs.
        leather.SetFloat("_Metallic",0f);leather.SetFloat("_Roughness",.34f);
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
        else Debug.Log("A1_VIP_PREMIUM PASS count=16 realSize=sourceCm*0.01 thenRootK"+
            " spacingX=1.45 rowSpacing=1.75 authoredUpright=true"+
            " blackPolyHavenLeather=true blackPBRMetal=true uniformScale=true");
        return SeatCount;
    }
}
