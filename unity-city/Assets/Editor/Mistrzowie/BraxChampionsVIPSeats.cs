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

    // Repair a GLB authored with its long chair axis horizontal. Do not
    // reorient an already-upright source and never replace the original PBR.
    static void KeepImportedChairUpright(GameObject chair,Quaternion facing)
    {
        var initial=VisualBounds(chair);
        float maxHorizontal=Mathf.Max(initial.size.x,initial.size.z);
        if(initial.size.y>=maxHorizontal*.83f)return;

        Quaternion[] corrections={
            Quaternion.Euler(90f,0,0),Quaternion.Euler(-90f,0,0),
            Quaternion.Euler(0,0,90f),Quaternion.Euler(0,0,-90f)
        };
        var feet=chair.GetComponentsInChildren<Transform>(true)
            .Where(t=>t&&t!=chair.transform&&
                (t.name.IndexOf("leg",StringComparison.OrdinalIgnoreCase)>=0||
                 t.name.IndexOf("foot",StringComparison.OrdinalIgnoreCase)>=0||
                 t.name.IndexOf("base",StringComparison.OrdinalIgnoreCase)>=0))
            .ToArray();
        float bestScore=Mathf.Abs(initial.size.y/maxHorizontal-1.12f);
        Quaternion best=Quaternion.identity;
        foreach(var correction in corrections)
        {
            chair.transform.localRotation=facing*correction;
            var b=VisualBounds(chair);
            float h=b.size.y/Mathf.Max(b.size.x,b.size.z);
            float score=Mathf.Abs(h-1.12f);
            if(feet.Length>0)
            {
                float footY=feet.Average(t=>t.position.y);
                score+=Mathf.Clamp01((footY-b.min.y)/Mathf.Max(b.size.y,.001f));
            }
            if(score<bestScore-.06f) {bestScore=score;best=correction;}
        }
        chair.transform.localRotation=facing*best;
        if(best!=Quaternion.identity)
            Debug.Log("CHAMPIONS_VIP_OWNER_GLB_UPRIGHT axisCorrection="+best.eulerAngles+
                " originalArtUnchanged=true");
        else
            BraxBuildQuality.Warn("CHAMPIONS_VIP_UPRIGHT_UNCERTAIN original GLB chair proportions require phone review");
    }

    static void Place(Transform root, GameObject source, float x, float z, float cx, float cz, string label)
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
        // Constrain real footprint and height without changing proportions.
        // The source model was authored at ~85 nominal units wide, not human size.
        float ratio = Mathf.Max(bounds.size.x / .96f,
                        Mathf.Max(bounds.size.z / .96f, bounds.size.y / 1.08f));
        float factor = 1f / ratio;
        chair.transform.localScale *= factor;
        bounds = VisualBounds(chair);
        // Sit on the actual floor, centred in the reserved VIP place.
        Vector3 target = spot.position + new Vector3(0, bounds.extents.y, 0);
        chair.transform.position += target - bounds.center;

        // One cheap collider for a complete upholstered chair, never five MeshColliders.
        foreach (var c in chair.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(c);
        var collider = spot.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, .54f, 0);
        collider.size = new Vector3(.96f, 1.08f, .96f);
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
        // Existing reserved positions near jury/commentator area:
        // two rows behind cage-side service desks; one 6m central aisle stays open.
        // Four places per side per row; seat spacing 1.45m, row spacing 1.75m.
        const float cx = -86f, cz = 63.3f;
        var xs = new[] { -94.0f, -92.55f, -91.10f, -89.65f,
                         -82.35f, -80.90f, -79.45f, -78.0f };
        foreach (float z in new[] { 70.30f, 72.05f })
            foreach (float x in xs)
                Place(interiorRoot, source, x, z, cx, cz,
                    "Reserved VIP | imported luxurious barrel seat");

        if (SeatCount != 16)
            BraxBuildQuality.Warn("Champion A1 expected 16 premium VIP chairs, got " + SeatCount +
                ". Test APK requires visual inspection.");
        return SeatCount;
    }
}
