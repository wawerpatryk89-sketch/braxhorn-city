using UnityEngine;

// Owner 10.11 08:40: "hamulec ręczny". The touch controls send Command(key) to every component of the player object
// (SendMessage). While the player drives the owner's car, the JUMP button pulls the car's handbrake. Added to the
// player by BraxChampionsCar.Board; does nothing on foot (the player's own Command handles jump then).
[DisallowMultipleComponent]
public sealed class BraxChampionsCarCommands : MonoBehaviour
{
    BraxGymPlayer player;
    void Awake() { player = GetComponent<BraxGymPlayer>(); }
    public void Command(string key)
    {
        if (!player || !player.championsCar) return;
        if (key == "jump") player.championsCar.PullHandbrake();
    }
}
