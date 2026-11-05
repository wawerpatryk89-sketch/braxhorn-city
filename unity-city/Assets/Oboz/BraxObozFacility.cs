using UnityEngine;

// Actual camp controls on the existing AKCJA register. No invented health or
// economy effect: taps emit water, the sauna emits steam, drinking uses the
// same Mixamo-compatible pose already used by the player elsewhere.
public sealed class BraxObozFacility : MonoBehaviour, IBraxInterakcja
{
    public enum Kind { Drink, Shower, Sauna, Toilet, Sink }
    public Kind kind;
    public BraxGymPlayer player;
    public ParticleSystem effect;
    public float range = 1.4f * 1.294f;
    bool active, paused;
    float remaining, nextFlushTime;
    void OnEnable() => BraxInterakcje.Dodaj(this);
    void OnDisable() { BraxInterakcje.Usun(this); Stop(); }
    void Stop() { active=false;remaining=0;if(effect)effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); }
    bool Near(Vector3 p)
    {
        Vector3 delta=p-transform.position;
        return Mathf.Abs(delta.y)<2*1.294f && new Vector2(delta.x,delta.z).sqrMagnitude<range*range &&
            !Physics.Linecast(p+Vector3.up*1.294f,transform.position+Vector3.up*1.294f,~(1<<8),QueryTriggerInteraction.Ignore);
    }
    public string Kontekst(Vector3 p)
    {
        if(!player || !player.ready || player.Paused || !Near(p))return "";
        if(kind==Kind.Drink)
        {
            var pose=BraxPose.Get(player);
            return player.seat || player.vessel || pose.mode!=BraxPose.Mode.None ? "" : "NAPIJ SIĘ WODY";
        }
        if(kind==Kind.Toilet)return Time.time>=nextFlushTime?"SPŁUCZ TOALETĘ":"";
        if(!effect)return "";
        if(kind==Kind.Sink)return active?"ZAKRĘĆ KRAN":"ODKRĘĆ KRAN";
        return kind==Kind.Shower ? active?"WYŁĄCZ PRYSZNIC":"WŁĄCZ PRYSZNIC" : active?"WYŁĄCZ PARĘ":"WŁĄCZ PARĘ";
    }
    public bool Wykonaj(Vector3 p)
    {
        if(string.IsNullOrEmpty(Kontekst(p)))return false;
        if(kind==Kind.Drink){BraxPose.Get(player).Drink();return true;}
        // Spłukiwanie jest akcją przy przycisku; bez imitowania fizjologii gracza.
        if(kind==Kind.Toilet)
        {
            nextFlushTime=Time.time+3f;
            BraxSport.Pokaz("WC · SPŁUKANO TOALETĘ",2f);
            return true;
        }
        if(active)Stop();
        else
        {
            active=true;
            remaining=kind==Kind.Shower?30:kind==Kind.Sink?12:20;
            effect.Play(true);
        }
        return true;
    }
    void Update()
    {
        if(!player)return;
        if(active && paused!=player.Paused)
        {
            paused=player.Paused;if(effect){if(paused)effect.Pause(true);else effect.Play(true);}
        }
        if(player.Paused)return;
        if(active)
        {
            remaining-=Time.deltaTime;
            if(remaining<=0 || (player.transform.position-transform.position).sqrMagnitude>36*1.294f*1.294f)Stop();
        }
    }
}
