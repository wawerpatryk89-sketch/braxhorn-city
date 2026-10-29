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
    public int originalMeshes,originalTriangles,sourceColliders;
    public bool loading;
    public string activePoint;
    public static string Arrival;
    private BraxCityDoor nearby;
    private float nextScan;
    public int OpenDoors { get { int n=0;foreach(var d in doors)if(d&&d.IsOpen)n++;return n;} }
    public string Context
    {
        get
        {
            if(loading)return "WCZYTYWANIE DZIELNICY…";
            var p=player.transform.position;
            if(area=="city"&&Vector3.Distance(p,new Vector3(-100,.15f,28.5f))<7f)
                // Preserve the existing estate gateway and map marker. The
                // iPhone street review intentionally does not ship the very
                // large estate scene, so don't offer a misleading action.
                return Application.CanStreamedLevelBeLoaded("BraxhornEstate")
                    ?"WEJDŹ NA OSIEDLE":"PRZEJŚCIE DO OSIEDLA — KOLEJNY ETAP";
            if(area=="estate"&&Vector3.Distance(p,new Vector3(0,.08f,92))<6f)return "WRÓĆ DO MIASTA";
            if(elevator)
            {
                var liftContext=elevator.Context;
                if(!string.IsNullOrEmpty(liftContext))return liftContext;
            }
            if(nearby)return (nearby.IsOpen?"ZAMKNIJ DRZWI · ":"OTWÓRZ DRZWI · ")+nearby.label;
            return "";
        }
    }
    private void Start()
    {
        if(!player)throw new InvalidOperationException("City player is not assigned");
        if(Arrival=="city-return"&&area=="city")player.TravelTo(new Vector3(-99,.22f,28.5f));
        Arrival=null;
    }
    private void Update()
    {
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
        if(area=="city"&&Vector3.Distance(p,new Vector3(-100,.15f,28.5f))<7f)
        {
            if(!Application.CanStreamedLevelBeLoaded("BraxhornEstate"))
            {
                Debug.Log("CITY_REVIEW_ESTATE_GATE_PRESERVED_ONLY: no unloaded estate scene in the stage-1 iPhone build");
                return true;
            }
            StartCoroutine(ChangeScene("BraxhornEstate","estate-entry"));
            return true;
        }
        if(area=="estate"&&Vector3.Distance(p,new Vector3(0,.08f,92))<6f){StartCoroutine(ChangeScene("BraxhornCity","city-return"));return true;}
        if(elevator&&elevator.TryInteract(p))return true;
        if(nearby){nearby.Toggle(player.transform.position);return true;}
        return false;
    }
    public void GoTo(string id)
    {
        if(loading)return;
        foreach(var p in points)if(p.id==id){activePoint=id;player.TravelTo(p.position+Vector3.up*.08f);return;}
        Debug.LogWarning("Unknown original city point: "+id);
    }
    private IEnumerator ChangeScene(string scene,string arrival)
    {
        loading=true;Arrival=arrival;player.MoveInput("0,0");
        var operation=SceneManager.LoadSceneAsync(scene,LoadSceneMode.Single);
        if(operation==null){loading=false;throw new InvalidOperationException("Missing original scene "+scene);}
        yield return operation;
        yield return Resources.UnloadUnusedAssets();
    }
}
