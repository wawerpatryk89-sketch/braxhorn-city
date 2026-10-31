using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable] public sealed class BraxCityPoint
{
    public string id,label;
    public Vector3 position;
}

public sealed class BraxCityWorld : MonoBehaviour
{
    public string area;
    public BraxGymPlayer player;
    public BraxCityPoint[] points=Array.Empty<BraxCityPoint>();
    public BraxCityDoor[] doors=Array.Empty<BraxCityDoor>();
    public BraxCityElevator elevator;
    public BraxNutritionCounter nutritionCounter;
    // The original quay showcase lift remains separate and unchanged.
    // Eight further on-street tower lifts register independently so every
    // high-rise has its own door/cabin/roof and there is no "teleport" fake.
    public BraxCityElevator[] additionalTowerElevators=Array.Empty<BraxCityElevator>();
    public int originalMeshes,originalTriangles,sourceColliders;
    public bool loading;
    public bool waterfrontPreview;
    public string activePoint;
    public static string Arrival;
    // PRZEJŚCIE MIASTO ↔ OSIEDLE (31.10, Opus): tunel na końcu przedłużonego wiaduktu w mieście (portal z=−74, deska 8.16 m,
    // wnętrze do z=−88) i wylot tunelu na północnym końcu osiedla A (portal z=99.5, wnętrze do z=113). Wejście w głąb
    // tunelu (8 m za portalem) samo wczytuje drugi rejon; przy portalu przycisk AKCJA mówi, dokąd prowadzi tunel.
    // Przyjazd: wylot tunelu po drugiej stronie, kamera za plecami, przodem do wyjścia.
    static readonly Vector3 CityPortal=new Vector3(-66.2f,8.16f,-74f), EstatePortal=new Vector3(0f,.08f,99.5f);
    bool InCityTunnel(Vector3 p,float depth)=>area=="city"&&!waterfrontPreview&&Mathf.Abs(p.x-CityPortal.x)<4.8f&&p.y>CityPortal.y-1.2f&&p.z<CityPortal.z+2f-depth&&p.z>CityPortal.z-16f;
    bool InEstateTunnel(Vector3 p,float depth)=>area=="estate"&&Mathf.Abs(p.x-EstatePortal.x)<4.8f&&p.y<3f&&p.z>EstatePortal.z-2f+depth&&p.z<EstatePortal.z+16f;
    private BraxCityDoor nearby;
    private float nextScan;
    public int OpenDoors { get { int n=0;foreach(var d in doors)if(d&&d.IsOpen)n++;return n;} }
    public string Context
    {
        get
        {
            if(loading)return "WCZYTYWANIE DZIELNICY…";
            var p=player.transform.position;
            if(player.vessel)return "ZEJDŹ · "+player.vessel.label;
            if(player.seat)return "WSTAŃ";
            var vessel=BraxVessel.Nearest(p,3.2f);
            if(vessel)return "WSIĄDŹ · "+vessel.label;
            var seat=BraxSeat.Nearest(p,1.6f);
            if(seat)return (seat.lie?"POŁÓŻ SIĘ · ":"USIĄDŹ · ")+seat.label;
            if(InCityTunnel(p,0f))return "TUNEL · TUR POD ZAMOŚCIEM";
            if(InEstateTunnel(p,0f))return "TUNEL · MIASTO";
            if(!waterfrontPreview&&area=="city"&&Vector3.Distance(p,new Vector3(-100,.15f,28.5f))<7f)return "WEJDŹ · TUR POD ZAMOŚCIEM";
            if(area=="estate"&&Vector3.Distance(p,new Vector3(0,.08f,92))<6f)return "WRÓĆ DO MIASTA";
            // Do not hide elevator calls behind ordinary door prompts.
            // Player proximity is evaluated inside each real lift controller.
            foreach(var lift in additionalTowerElevators)
                if(lift)
                {
                    var prompt=lift.Context;
                    if(!string.IsNullOrEmpty(prompt))return prompt;
                }
            if(elevator)
            {
                var liftContext=elevator.Context;
                if(!string.IsNullOrEmpty(liftContext))return liftContext;
            }
            if(nutritionCounter&&!string.IsNullOrEmpty(nutritionCounter.Context))return nutritionCounter.Context;
            if(nearby)return (nearby.IsOpen?"ZAMKNIJ DRZWI · ":"OTWÓRZ DRZWI · ")+nearby.label;
            return "";
        }
    }
    private void Start()
    {
        if(!player)throw new InvalidOperationException("City player is not assigned");
        if(Arrival=="city-return"&&area=="city")player.TravelTo(CityPortal+new Vector3(0,.14f,3.5f),180f);
        if(Arrival=="estate-entry"&&area=="estate")player.TravelTo(EstatePortal+new Vector3(0,.06f,-4f),0f);
        Arrival=null;
    }
    private void Update()
    {
        if(!loading&&player)
        {
            var q=player.transform.position;
            if(InCityTunnel(q,8f)){StartCoroutine(ChangeScene("BraxhornEstate","estate-entry"));return;}
            if(InEstateTunnel(q,8f)){StartCoroutine(ChangeScene("BraxhornCity","city-return"));return;}
        }
        if(loading||Time.unscaledTime<nextScan)return;
        nextScan=Time.unscaledTime+.12f;nearby=null;float best=3.3f;
        foreach(var d in doors)
        {
            if(!d)continue;var v=player.transform.position-d.transform.position;
            if(Mathf.Abs(v.y)>2.3f)continue;v.y=0;
            if(v.magnitude<best){best=v.magnitude;nearby=d;}
        }
    }
    public bool Interact()
    {
        if(loading)return true;
        var p=player.transform.position;
        if(player.vessel){player.vessel.Dismount();return true;}
        if(player.seat){player.seat.Leave();return true;}
        var vessel=BraxVessel.Nearest(p,3.2f);
        if(vessel){vessel.Board(player);return true;}
        var seat2=BraxSeat.Nearest(p,1.6f);
        if(seat2){seat2.Use(player);return true;}
        if(InCityTunnel(p,0f)||(!waterfrontPreview&&area=="city"&&Vector3.Distance(p,new Vector3(-100,.15f,28.5f))<7f)){StartCoroutine(ChangeScene("BraxhornEstate","estate-entry"));return true;}
        if(InEstateTunnel(p,0f)){StartCoroutine(ChangeScene("BraxhornCity","city-return"));return true;}
        if(area=="estate"&&Vector3.Distance(p,new Vector3(0,.08f,92))<6f){StartCoroutine(ChangeScene("BraxhornCity","city-return"));return true;}
        foreach(var lift in additionalTowerElevators)
            if(lift&&lift.TryInteract(p))return true;
        if(elevator&&elevator.TryInteract(p))return true;
        if(nutritionCounter&&nutritionCounter.TryInteract()){BraxPose.Get(player).Drink();return true;}
        if(nearby){nearby.Toggle(player.transform.position);return true;}
        return false;
    }
    public void GoTo(string id)
    {
        if(loading)return;
        foreach(var p in points)if(p.id==id){activePoint=id;player.TravelTo(p.position+Vector3.up*.08f);return;}
        Debug.LogWarning("Unknown original city point: "+id);
    }
    public void ReviewScene(string target){if(loading||area==target)return;StartCoroutine(ChangeScene(target=="estate"?"BraxhornEstate":"BraxhornCity",target=="estate"?"estate-entry":"city-return"));}
    private IEnumerator ChangeScene(string scene,string arrival)
    {
        loading=true;Arrival=arrival;player.MoveInput("0,0");
        var operation=SceneManager.LoadSceneAsync(scene,LoadSceneMode.Single);
        if(operation==null){loading=false;throw new InvalidOperationException("Missing original scene "+scene);}
        yield return operation;
        yield return Resources.UnloadUnusedAssets();
    }
}
