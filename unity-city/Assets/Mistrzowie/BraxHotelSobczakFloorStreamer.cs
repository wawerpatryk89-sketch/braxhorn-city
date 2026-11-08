using UnityEngine;

// Mobile-first interior streaming for HOTEL SOBCZAK.
// Exterior/public floors remain present; only the 30 repeated guest-floor
// interiors are toggled according to the player/camera location.
public sealed class BraxHotelSobczakFloorStreamer : MonoBehaviour
{
    public GameObject[] guestInteriors = new GameObject[0];
    public GameObject[] basementInteriors = new GameObject[0];
    public float metreScale = 1f;
    public float guestBaseY = 9.6f;
    public float floorHeight = 3.6f;
    public float halfWidth = 32f;
    public float halfDepth = 21f;
    public int neighbourFloors = 1;
    public float basementHalfWidth = 43f;
    public float basementHalfDepth = 38f;

    BraxGymPlayer player;
    float nextScan;
    int last = int.MinValue;
    bool basementLast;

    void Awake()
    {
        if(metreScale<=0f) metreScale=1f;
        Apply(-999);
        ApplyBasements(false);
    }

    void Update()
    {
        if(Time.unscaledTime<nextScan)return;
        nextScan=Time.unscaledTime+.20f;
        if(!player)player=FindFirstObjectByType<BraxGymPlayer>();
        Vector3 world=player?player.transform.position:(Camera.main?Camera.main.transform.position:Vector3.positiveInfinity);
        Vector3 local=transform.InverseTransformPoint(world)/metreScale;
        bool inside=Mathf.Abs(local.x)<=halfWidth+2f && Mathf.Abs(local.z)<=halfDepth+2f &&
                    local.y>=guestBaseY-1.0f && local.y<=guestBaseY+guestInteriors.Length*floorHeight+1.0f;
        int index=inside?Mathf.Clamp(Mathf.FloorToInt((local.y-guestBaseY)/floorHeight),0,guestInteriors.Length-1):-999;
        if(index!=last)Apply(index);

        bool nearBasement=Mathf.Abs(local.x)<=basementHalfWidth && Mathf.Abs(local.z)<=basementHalfDepth &&
                          local.y<=1.50f && local.y>=-9.00f;
        if(nearBasement!=basementLast)ApplyBasements(nearBasement);
    }

    void Apply(int centre)
    {
        last=centre;
        for(int i=0;i<guestInteriors.Length;i++)
        {
            bool on=centre>=0 && Mathf.Abs(i-centre)<=Mathf.Max(0,neighbourFloors);
            if(guestInteriors[i] && guestInteriors[i].activeSelf!=on)guestInteriors[i].SetActive(on);
        }
    }

    void ApplyBasements(bool on)
    {
        basementLast=on;
        for(int i=0;i<basementInteriors.Length;i++)
            if(basementInteriors[i] && basementInteriors[i].activeSelf!=on)basementInteriors[i].SetActive(on);
    }
}
