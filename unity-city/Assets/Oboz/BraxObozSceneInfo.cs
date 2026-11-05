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

    bool streamingConfigured, previousStreaming, previousAllCameras;
    int previousMaxReduction;

    void Awake()
    {
        if (!Application.isPlaying) return;
        if (!SystemInfo.supportsMipStreaming)
        {
            Debug.LogWarning("BRAXHORN_OBOZ_MIP streaming_unavailable=true: retain original mipmaps");
            return;
        }
        previousStreaming = QualitySettings.streamingMipmapsActive;
        previousAllCameras = QualitySettings.streamingMipmapsAddAllCameras;
        previousMaxReduction = QualitySettings.streamingMipmapsMaxLevelReduction;
        QualitySettings.streamingMipmapsActive = true;
        QualitySettings.streamingMipmapsAddAllCameras = true;
        // Exactly one level may be discarded: 4K -> 2K. Imported 2K sources
        // remain non-streamed. Never lower the global texture resolution.
        QualitySettings.streamingMipmapsMaxLevelReduction = 1;
        streamingConfigured = true;
        Debug.Log("BRAXHORN_OBOZ_MIP active=" + QualitySettings.streamingMipmapsActive +
            " max_reduction=" + QualitySettings.streamingMipmapsMaxLevelReduction +
            " budget_mb=" + QualitySettings.streamingMipmapsMemoryBudget +
            " supported=" + SystemInfo.supportsMipStreaming);
    }

    void OnDestroy()
    {
        if (!streamingConfigured) return;
        QualitySettings.streamingMipmapsActive = previousStreaming;
        QualitySettings.streamingMipmapsAddAllCameras = previousAllCameras;
        QualitySettings.streamingMipmapsMaxLevelReduction = previousMaxReduction;
    }
}
