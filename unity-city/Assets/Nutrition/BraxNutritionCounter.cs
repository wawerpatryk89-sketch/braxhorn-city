using UnityEngine;

public sealed class BraxNutritionCounter : MonoBehaviour
{
    public BraxGymPlayer player;
    public Bounds shopBounds;
    public Vector3 servicePoint;
    public float interactionRadius=1.45f;
    public TextMesh readout;
    float nextDisplay;
    public bool InShop=>player&&shopBounds.Contains(player.transform.position+Vector3.up*.8f);
    public bool InRange
    {
        get
        {
            if(!InShop||!player.ready)return false;
            var d=player.transform.position-servicePoint;
            return Mathf.Abs(d.y)<1.3f&&new Vector2(d.x,d.z).sqrMagnitude<=interactionRadius*interactionRadius;
        }
    }
    public string Context
    {
        get
        {
            var energy=BraxFighterEnergy.Instance;
            if(!InRange||!energy)return "";
            return energy.Value>=energy.Maximum-.001f?"ENERGIA PEŁNA · NUTRITION":"UZUPEŁNIJ ENERGIĘ · NUTRITION";
        }
    }
    public bool TryInteract()
    {
        var energy=BraxFighterEnergy.Instance;
        if(!InRange||!energy||energy.IsPaused)return false;
        energy.Refill();UpdateReadout();return true;
    }
    void Update()
    {
        if(Time.unscaledTime<nextDisplay)return;
        nextDisplay=Time.unscaledTime+.25f;UpdateReadout();
    }
    void UpdateReadout()
    {
        var energy=BraxFighterEnergy.Instance;
        if(readout&&energy)readout.text="REGENERACJA\n"+Mathf.CeilToInt(energy.Value)+" / "+Mathf.RoundToInt(energy.Maximum);
    }
}
