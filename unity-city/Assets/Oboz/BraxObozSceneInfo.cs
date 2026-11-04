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
}
