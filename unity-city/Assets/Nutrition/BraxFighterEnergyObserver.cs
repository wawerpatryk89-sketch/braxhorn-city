using UnityEngine;

// Receives the same existing SendMessage commands as BraxGymPlayer. No new
// movement/camera/input implementation, and no changes to original animations.
public sealed class BraxFighterEnergyObserver : MonoBehaviour
{
    BraxFighterEnergy energy;
    BraxGymPlayer player;
    public bool Paused { get; private set; }
    public void Bind(BraxFighterEnergy owner,BraxGymPlayer actor)
    {
        if(player==actor&&energy==owner)return;
        energy=owner;player=actor;Paused=false;
    }
    public void Command(string key)
    {
        if(!player||!player.ready)return;
        if(key=="pause")Paused=!Paused;
    }
    public void ReviewEnergy(string text){if(energy)energy.ReviewEnergy(text);}
}
