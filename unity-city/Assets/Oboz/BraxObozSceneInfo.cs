using UnityEngine;

// Authored references for verification; not a second controls/menu system.
public sealed class BraxObozSceneInfo : MonoBehaviour
{
    public const float WorldScale = 1.294f;
    public BraxGymPlayer player;
    public BraxObozNpc[] npcs = new BraxObozNpc[0];
    public BraxObozArena[] arenas = new BraxObozArena[0];
    public BraxWorek[] bags = new BraxWorek[0];
    public BraxCityDoor[] cabinDoors = new BraxCityDoor[0];
    public Transform[] coveredBags = new Transform[0];
    public bool ownerAppearanceApproved, mobileRuntimeVerified;
    public string sourcePlan = "Oboz_Misia_plan_terenu_R3.png";

    // Scene-local quality policy; never lower original file resolutions or
    // change controls, colliders, gameplay, lighting or character geometry.
    bool streamingConfigured;
    bool previousStreaming, previousAllCameras;
    int previousReduction, previousGlobalMipLimit;

    void Awake()
    {
        if (!Application.isPlaying) return;
        if (!SystemInfo.supportsMipStreaming)
        {
            Debug.LogWarning("BRAXHORN_OBOZ_MIP supported=false; originals remain loaded");
            return;
        }

        previousStreaming = QualitySettings.streamingMipmapsActive;
        previousAllCameras = QualitySettings.streamingMipmapsAddAllCameras;
        previousReduction = QualitySettings.streamingMipmapsMaxLevelReduction;
        previousGlobalMipLimit = QualitySettings.globalTextureMipmapLimit;

        QualitySettings.globalTextureMipmapLimit = 0; // Full imported 4K near.
        QualitySettings.streamingMipmapsActive = true;
        QualitySettings.streamingMipmapsAddAllCameras = true;
        QualitySettings.streamingMipmapsMaxLevelReduction = 1; // 4K -> 2K maximum.
        streamingConfigured = true;

        Debug.Log("BRAXHORN_OBOZ_MIP active=" + QualitySettings.streamingMipmapsActive +
            " supported=true max_reduction=" + QualitySettings.streamingMipmapsMaxLevelReduction +
            " global_mip_limit=" + QualitySettings.globalTextureMipmapLimit +
            " budget_mb=" + QualitySettings.streamingMipmapsMemoryBudget +
            " runtime_memory_unmeasured=true");
    }

    void OnDestroy()
    {
        if (!streamingConfigured) return;
        QualitySettings.streamingMipmapsActive = previousStreaming;
        QualitySettings.streamingMipmapsAddAllCameras = previousAllCameras;
        QualitySettings.streamingMipmapsMaxLevelReduction = previousReduction;
        QualitySettings.globalTextureMipmapLimit = previousGlobalMipLimit;
    }
}
