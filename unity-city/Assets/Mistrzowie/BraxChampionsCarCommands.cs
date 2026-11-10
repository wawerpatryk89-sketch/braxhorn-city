using UnityEngine;

// Owner 10.11 08:40: "hamulec ręczny". The touch controls send Command(key) to every component of the player object
// (SendMessage). While the player drives the owner's car, the JUMP button pulls the car's handbrake. Added to the
// player by BraxChampionsCar.Board; does nothing on foot (the player's own Command handles jump then).
[DisallowMultipleComponent]
public sealed class BraxChampionsCarCommands : MonoBehaviour
{
    BraxGymPlayer player;
    // the menu's "pause" toggles the game (BraxTouchControls.Pauza sends it to the player): the car HUD hides with it
    public static bool Paused { get; private set; }
    void Awake() { player = GetComponent<BraxGymPlayer>(); Paused = false; }
    public void Command(string key)
    {
        if (key == "pause") { Paused = !Paused; return; }
        if (!player || !player.championsCar) return;
        if (key == "jump") player.championsCar.PullHandbrake();
    }
}
