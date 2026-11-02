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
    // DRZWI SAME SIĘ ZAMYKAJĄ (build 14, właściciel 1.11 20:50: „zawsze same się zamykają — otwierają się przy podejściu
    // (lub AKCJĄ), zamykają same po przejściu / gdy gracz odejdzie ok. 3 m, z dźwiękiem zamknięcia; wszystkie drzwi”):
    //  • otwarcie: gracz idzie w stronę drzwi i jest bliżej niż 1.7 m (albo AKCJA) — skrzydło odchyla się od gracza;
    //  • zamknięcie: nikogo w promieniu 2.6 m przez 3 s → skrzydło wraca, dźwięk zamknięcia (BraxDzwiekiSwiata.Drzwi);
    //  • po ręcznym zamknięciu (AKCJA) drzwi nie otwierają się same, dopóki gracz nie odejdzie (> 3 m) albo przez 4 s.
    // Sprawdzanie co 0.2 s (rozłożone w czasie między drzwiami), obrót skrzydeł tylko w ruchu — bez pracy w spoczynku.
    // Tylko pola prywatne (bez zmiany danych zapisanych w scenach).
    public const float ZamykanieS=3f, PromienPusto=2.6f, PromienOtwarcia=1.7f;
    private float pusto,nastSpr,blokadaDo;
    private bool blokadaOdejscie,ruszaj=true;
    private static Transform gracz;private static float szukajGracza,graczCzas;private static Vector3 graczPoz,graczV;private static int graczKlatka=-1;
    private void Awake()
    {
        rest=new Quaternion[leaves.Length];
        for(int i=0;i<leaves.Length;i++)rest[i]=leaves[i].localRotation;
        nastSpr=Time.time+Random.value*.2f;
    }
    public void Toggle(Vector3 playerPosition)
    {
        BraxDzwiekiSwiata.Drzwi(transform.position,target<=.5f);   // 1.11: dźwięk klamki i skrzydła
        if(target>.5f){target=0;pusto=0;blokadaDo=Time.time+4f;blokadaOdejscie=true;ruszaj=true;return;}   // ręczne zamknięcie
        swing=transform.InverseTransformPoint(playerPosition).z>=0?1:-1;
        target=1;pusto=0;ruszaj=true;
    }
    private void Update()
    {
        if(Time.time>=nastSpr){nastSpr=Time.time+.2f;Samo();}
        if(!ruszaj)return;
        angle=Mathf.MoveTowards(angle,target,Time.deltaTime*2.2f);
        for(int i=0;i<leaves.Length;i++)if(leaves[i])leaves[i].localRotation=rest[i]*Quaternion.Euler(0,sides[i]*angle*90f*swing,0);
        if(angle==target)ruszaj=false;
    }
    // samo otwieranie i zamykanie (co 0.2 s)
    private void Samo()
    {
        var g=Gracz();if(!g)return;
        Vector3 v=g.position-transform.position;float dy=Mathf.Abs(v.y);v.y=0;float d=v.magnitude;
        bool blisko=dy<2.5f&&d<PromienPusto;
        if(blokadaOdejscie&&(d>3f||dy>2.5f))blokadaOdejscie=false;
        if(target>.5f)
        {
            if(blisko){pusto=0;return;}
            pusto+=.2f;
            if(pusto>=ZamykanieS){target=0;pusto=0;ruszaj=true;BraxDzwiekiSwiata.Drzwi(transform.position,false);Debug.Log("BRAXHORN_DRZWI zamknięte same (nikogo przez "+ZamykanieS+" s): "+label+" "+id);}
            return;
        }
        if(d<4.5f&&dy<2.5f)nastSpr=Time.time+.1f;   // gracz blisko: częściej (bieg 5 m/s = 0.5 m na sprawdzenie)
        if(blokadaOdejscie||Time.time<blokadaDo||dy>2f||d>PromienOtwarcia)return;
        // gracz idzie w stronę drzwi (prędkość w poziomie > 0.4 m/s, kierunek najwyżej ~55° od drzwi — przejście chodnikiem
        // wzdłuż elewacji nie otwiera mijanych drzwi)
        var vel=GraczV();
        if(vel.sqrMagnitude<.16f||Vector3.Dot(vel,-v)<.57f*vel.magnitude*Mathf.Max(d,.01f))return;
        Toggle(g.position);
        Debug.Log("BRAXHORN_DRZWI otwarte przy podejściu: "+label+" "+id);
    }
    private static Transform Gracz()
    {
        if(gracz)return gracz;
        if(Time.unscaledTime<szukajGracza)return null;
        szukajGracza=Time.unscaledTime+1f;
        var p=FindAnyObjectByType<BraxGymPlayer>();if(p)gracz=p.transform;
        return gracz;
    }
    // prędkość gracza liczona raz na klatkę (wspólna dla wszystkich drzwi)
    private static Vector3 GraczV()
    {
        if(graczKlatka==Time.frameCount||!gracz)return graczV;
        graczKlatka=Time.frameCount;
        float t=Time.time,dt=t-graczCzas;var p=gracz.position;
        if(dt<=0f)return graczV;
        if(dt<1f){var v=(p-graczPoz)/dt;v.y=0;graczV=v.sqrMagnitude<30f*30f?v:Vector3.zero;}else graczV=Vector3.zero;   // stary pomiar = brak ruchu
        graczPoz=p;graczCzas=t;
        return graczV;
    }
}
