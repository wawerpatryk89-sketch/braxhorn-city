using UnityEngine;

public sealed class BraxCityDoor : MonoBehaviour
{
    // Rejestr wszystkich drzwi w załadowanych scenach (1.11, Opus): drzwi w doczytywanych obszarach (port, osiedle B)
    // nie mogą być w tablicy BraxCityWorld.doors (odwołanie między scenami Unity zeruje przy zapisie).
    public static readonly System.Collections.Generic.List<BraxCityDoor> All=new System.Collections.Generic.List<BraxCityDoor>();
    private void OnEnable(){if(!All.Contains(this))All.Add(this);}
    private void OnDisable(){All.Remove(this);}
    public string id,label;
    public Transform[] leaves;
    public float[] sides;
    private Quaternion[] rest;
    private float angle,target,swing=1;
    public bool IsOpen=>target>.5f;
    private void Awake()
    {
        rest=new Quaternion[leaves.Length];
        for(int i=0;i<leaves.Length;i++)rest[i]=leaves[i].localRotation;
    }
    public void Toggle(Vector3 playerPosition)
    {
        BraxDzwiekiSwiata.Drzwi(transform.position,target<=.5f);   // 1.11: dźwięk klamki i skrzydła
        if(target>.5f){target=0;return;}
        swing=transform.InverseTransformPoint(playerPosition).z>=0?1:-1;
        target=1;
    }
    private void Update()
    {
        angle=Mathf.MoveTowards(angle,target,Time.deltaTime*2.2f);
        for(int i=0;i<leaves.Length;i++)leaves[i].localRotation=rest[i]*Quaternion.Euler(0,sides[i]*angle*90f*swing,0);
    }
}
