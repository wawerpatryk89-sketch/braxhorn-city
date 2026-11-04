using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;

// Extends the original city's builder: no second implementation of platforms or stairs.
public static partial class BraxClaudeMetro
{
    public static GameObject ChampionsStationTemplate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<BraxCityWorld>())
            throw new InvalidOperationException("Generate the region in its isolated CI scene, not inside an existing city world.");
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(ModelPociagu))
            throw new FileNotFoundException("The existing metro_train.glb is required. No procedural replacement.",ModelPociagu);
        if (!BraxMiastoAssety.Model("miasto_silownia_metro","metro_bramka"))
            throw new FileNotFoundException("Existing metro ticket-gate model is required; no replacement gate.");
        W.Wyczysc(); rep = new Raport(); Materialy();
        // Preserve original city design, using full source PBR maps for this region only.
        mPosadzka = Premium("floor","granite_tile_02",1.2f,new Color(.8f,.8f,.8f));
        mPosadzkaJasna = Premium("edge","granite_tile",.9f,Color.white);
        mSciana = Premium("wall","rectangular_facade_tiles",2.4f,new Color(.95f,.95f,.93f));
        mLamperia = Premium("skirting","granite_tile_02",1.2f,new Color(.30f,.31f,.33f));
        mBeton = Premium("concrete","concrete_floor_02",2.4f,new Color(.58f,.58f,.56f));
        mBetonCiemny = Premium("tunnel","concrete_wall_008",2.4f,new Color(.22f,.22f,.21f));
        mBetonNoc = Premium("tunnel_dark","concrete_wall_008",2.4f,new Color(.06f,.06f,.06f));
        mStopien = Premium("stairs","granite_tile_02",.9f,new Color(.72f,.72f,.72f));
        mTapicerka = Premium("fabric","fabric_pattern_05",.45f,Color.white);
        mDrewno = Premium("wood","teak_veneer",.8f,new Color(.80f,.62f,.44f));
        // All Tex() fallbacks above are replaced by required full PBR assets before geometry.
        W.Braki.RemoveAll(s=>s.StartsWith("brak tekstury:"));
        if (!atlas) throw new FileNotFoundException("Existing city metro signage atlas is missing");
        var root=new GameObject("Metro template — existing city station");
        zewnatrz=new GameObject("Existing entrance").transform;zewnatrz.SetParent(root.transform,false);
        podziemie=new GameObject("Existing station and open tunnels").transform;podziemie.SetParent(root.transform,false);
        W.Start(zewnatrz);W.Wnetrze=false;W.Komorka=12f;
        foreach(float z0 in WEJSCIA) Wejscie(z0,z0>28f);
        W.Wnetrze=true;
        foreach(float z0 in WEJSCIA) ZejscieWnetrze(z0,z0>28f);
        W.Zapisz("mistrzowie_metro_entrance");
        W.Start(podziemie);W.Komorka=14f;
        Antresola();SchodyNaPeron(true);Hala();Tory();Tunele(true);Wyposazenie();
        W.Zapisz("mistrzowie_metro_station");
        var gates=root.GetComponentsInChildren<BraxDrzwiPrzesuwne>(true);
        if(gates.Length!=7||gates.Any(g=>g.skrzydla==null||g.skrzydla.Length!=4))
            throw new InvalidOperationException("Existing metro gate model did not produce seven complete original gates");
        if(W.Braki.Count>0)throw new InvalidOperationException(string.Join("; ",W.Braki));
        // Source interactions have city-specific mission IDs and promises of departures.
        // Keep physical ticket gates; defer passengers, services and moving trains.
        foreach(var a in root.GetComponentsInChildren<BraxPunktAkcji>(true))UnityEngine.Object.DestroyImmediate(a);
        foreach(var a in root.GetComponentsInChildren<BraxSchodyRuchome>(true))a.enabled=true;
        foreach(var a in root.GetComponentsInChildren<BraxSchodyRuchomeJazda>(true))a.enabled=true;
        return root;
    }
    public static void ChampionsParkedTrain(Transform station)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPociagu);
        if(!source)throw new FileNotFoundException("Original city train missing",ModelPociagu);
        var underground=station.Find("Existing station and open tunnels");
        if(!underground)throw new InvalidOperationException("Station template structure changed");
        var train=new GameObject("Parked metro train | original two-car city model").transform;
        train.SetParent(underground,false);W.Start(train);
        if(!PociagModel(source,true))throw new InvalidOperationException("Existing train could not be placed; no procedural substitute");
    }

    static Material Premium(string id,string slug,float tile,Color tint)
        => BraxChampionsFoundation.SourceMaterial("metro_"+id,slug,tile,tint);

    // Exact city-space route, transformed with each station (positions already world scale).
    public static Vector3[] ChampionsWalkingRoute()
    {
        float zc=ZcSchodow(EZ[1]);
        float xEnd=EX0+(12-1+11-1)*.30f*K+1.5f*K;
        return new[]{new Vector3(EX0-3f,yS,zc),new Vector3(EX0-.3f,yS,zc),
            // The north wall has a bin at (71.8,32) and ticket machines at
            // x=68.4/70.0. Use their public aisle, not their collision boxes.
            new Vector3(xEnd+1f,yM,zc),new Vector3(xEnd+1f,yM,EZ[1]-1.7f),
            new Vector3(GX+3f,yM,EZ[1]-1.7f),new Vector3(GX+1.5f,yM,28.45f),
            new Vector3(GX-1.5f,yM,28.45f),new Vector3(WX1+1f,yM,27.15f),
            // Keep the platform route south of its columns, benches and bins.
            new Vector3(WX0-1.5f,yP,27.15f),new Vector3(20f,yP,26.4f)};
    }
}
