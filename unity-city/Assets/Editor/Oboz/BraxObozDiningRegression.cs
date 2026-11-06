using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Called by the isolated scene regression, not added to the playable world.
// Tests actual imported GLBs and real Unity physics, not source-code strings.
public static class BraxObozDiningRegression
{
    const string Owner="Assets/Source/Prywatne/Oboz/OwnerAssets/";
    const string Generated="Assets/PlayableGenerated/Oboz/ServiceModels/";

    public static string[] Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string text){checks.Add((ok?"PASS ":"FAIL ")+text);}
        var frame=new GameObject("Dining regression — disposable").transform;
        frame.position=new Vector3(12000,0,12000);frame.localScale=Vector3.one*BraxObozPlacement.K;
        try
        {
            const float k=BraxObozPlacement.K;
            foreach(string id in new[]{"canteen","sauna","medical","storage"})
            {
                var metadata=JsonUtility.FromJson<BraxObozServices.Model>(File.ReadAllText(Generated+id+".json"));
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Generated+id+".glb");
                if(!asset)throw new InvalidOperationException("Regression model missing: "+id);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(asset);model.transform.SetParent(frame,false);
                foreach(var piece in metadata.furniture ?? Array.Empty<BraxObozServices.Furniture>())
                {
                    var part=model.GetComponentsInChildren<Transform>(true).Single(t=>t.name==piece.node);
                    var bounds=Bounds(part);
                    Check(Mathf.Abs(bounds.min.y-.16f*k)<.001f &&
                          Mathf.Abs(bounds.max.y-(.16f+piece.height)*k)<.001f,
                        id+" "+piece.node+" floor="+bounds.min.y+" top="+bounds.max.y);
                }
                var door=model.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="DoorPivot");
                var leaf=Bounds(door);
                Check(Mathf.Abs(leaf.size.x-.9f*k)<.001f && Mathf.Abs(leaf.size.y-2.1f*k)<.001f &&
                      Mathf.Abs(leaf.min.y-.16f*k)<.001f,id+" real imported door .90 x 2.10 above .16 floor");
                if(id=="canteen")
                    Check(metadata.furniture.Count(f=>f.kind=="Table")==4 &&
                          metadata.furniture.Count(f=>f.kind=="Bench")==8 && metadata.seats.Length==24,
                        "one authored canteen furniture set: 4 tables, 8 benches, 24 seats");
                if(id=="sauna")
                    Check(metadata.missingModels.Contains("sauna-heater") &&
                          metadata.missingModels.Contains("cold-plunge-tub") &&
                          !metadata.facilities.Any(f=>f.kind=="Sauna"),
                        "missing sauna models reported; no unsupported steam action");
                Object.DestroyImmediate(model);
            }

            // A flat support is a disposable test fixture, never camp geometry.
            var table=GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name="temporary contact surface";table.transform.SetParent(frame,false);
            table.transform.localPosition=new Vector3(0,.725f,0);
            table.transform.localScale=new Vector3(2,.05f,2);
            var tray=BraxObozServices.OwnerFixture(frame,"food_tray.glb",new Vector3(0,.75f,0),
                new Vector3(.55f,.021875f,.425f));
            BraxObozServices.UseOriginalContactSurface(tray);
            bool onTable=BraxObozServices.PlaceOnOwnerSurface(tray,table.transform,
                frame.TransformPoint(new Vector3(0,.75f,0)),"regression original tray");
            var trayBounds=Bounds(tray.transform);
            Check(onTable && Mathf.Abs(trayBounds.size.x-.55f*k)<.0001f &&
                  Mathf.Abs(trayBounds.size.z-.425f*k)<.0001f &&
                  Mathf.Abs(trayBounds.min.y-.75f*k)<.0001f,
                "original tray has .55 m design width, preserved proportions and actual table contact");

            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Owner+"24-piece-cutlery-set.glb");
            if(!source)throw new InvalidOperationException("Regression cutlery source missing");
            var fork=(GameObject)PrefabUtility.InstantiatePrefab(source);fork.transform.SetParent(frame,false);
            foreach(var renderer in fork.GetComponentsInChildren<Renderer>(true))
            {
                bool selected=false;
                for(var t=renderer.transform;t && t!=fork.transform;t=t.parent)
                    if(t.name.StartsWith("Dinner fork 1,",StringComparison.Ordinal))selected=true;
                renderer.enabled=selected;
            }
            BraxObozPlacement.NativeMetres(fork.transform,fork.GetComponentsInChildren<Renderer>(true),"regression fork");
            bool inTray=BraxObozServices.PlaceOnOwnerSurface(fork,tray.transform,
                new Vector3(trayBounds.center.x,trayBounds.max.y,trayBounds.center.z),"regression fork in tray");
            var forkBounds=Bounds(fork.transform);
            // The delivered tray has a floor at source y=.0125 and width=2.2.
            // A box proxy would put the fork at the rim (.0875), which must fail.
            float floor=trayBounds.min.y+(.0125f/2.2f)*(.55f*k);
            Check(inTray && Mathf.Abs(forkBounds.min.y-floor)<.0002f &&
                  Mathf.Abs(forkBounds.size.z-.19996f*k)<.0002f,
                "original fork rests on tray floor, not on empty air at its rim");
            Check(tray.GetComponentsInChildren<BoxCollider>(true).Length==0 &&
                  tray.GetComponentsInChildren<MeshCollider>(true).Length>0,
                "tray uses original concave mesh contact");

            var kitchenAsset=AssetDatabase.LoadAssetAtPath<GameObject>(Owner+"kitchen_-_assets.glb");
            if(!kitchenAsset)throw new InvalidOperationException("Regression kitchen source missing");
            var kitchen=(GameObject)PrefabUtility.InstantiatePrefab(kitchenAsset);kitchen.transform.SetParent(frame,false);
            var grate=kitchen.GetComponentsInChildren<Transform>(true)
                .Single(t=>t.name.StartsWith("PortableStove_Grate_L_LP.001_",StringComparison.Ordinal));
            var grateBounds=Bounds(grate);
            var pot=BraxObozServices.OwnerFixture(frame,"stockpot.glb",frame.InverseTransformPoint(grateBounds.center),
                new Vector3(.438f,.2805f,.29f));
            var bottom=pot.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Layered pan base")
                .GetComponentsInChildren<Renderer>(true);
            bool onRidge=BraxObozServices.PlaceOnOwnerSurface(pot,grate,
                new Vector3(grateBounds.center.x,grateBounds.max.y,grateBounds.center.z),
                "regression stockpot on thin grate ridges",bottom);
            Check(onRidge && Mathf.Abs(Bounds(pot.transform).min.y-grateBounds.max.y)<.0002f,
                "stockpot rests on highest original grate ridges; centre-only ray would sink by 8.7 mm");
        }
        catch(Exception ex){checks.Add("FAIL dining regression exception: "+ex);}
        finally {Object.DestroyImmediate(frame.gameObject);}
        return checks.ToArray();
    }

    static Bounds Bounds(Transform root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(renderers.Length==0)throw new InvalidOperationException("Regression has no active geometry: "+root.name);
        var bounds=renderers[0].bounds;
        foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}
