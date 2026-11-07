using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Real owner meshes and actual Unity raycasts/strike method, in disposable
// EditMode fixtures. This is not a claim that PlayMode/mobile motion was tested.
public static class BraxObozTrainingRegression
{
    const float K=1.294f;
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    const string BagPath="Assets/Source/Prywatne/Oboz/OwnerAssets/leather_punching_bag.glb";

    public static string[] Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string label)=>checks.Add((ok?"PASS ":"FAIL ")+label);
        GameObject frame=null;
        try
        {
            foreach(string staticSource in new[]{BraxObozGymEquipment.SourcePath,
                "Assets/Source/Prywatne/Oboz/OwnerAssets/Przystanek/Meshy_AI_The_object_is_a_recta_1104160815_texture.fbx"})
            {
                var importer=AssetImporter.GetAtPath(staticSource) as ModelImporter;
                Check(importer && importer.animationType==ModelImporterAnimationType.None &&
                    importer.avatarSetup==ModelImporterAvatarSetup.NoAvatar && !importer.importAnimation,
                    "actual static FBX importer has no human rig or animation: "+staticSource);
                Check(importer && !AssetDatabase.LoadAllAssetsAtPath(staticSource).OfType<Avatar>().Any(),
                    "actual static FBX contains no manufactured Avatar subasset: "+staticSource);
            }
            frame=new GameObject("temporary original training regression");
            frame.transform.position=new Vector3(9400,0,9400);
            var floorObject=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorObject.name="temporary measured test floor";floorObject.transform.SetParent(frame.transform,false);
            floorObject.transform.localPosition=Vector3.up*1.5f;
            floorObject.transform.localScale=new Vector3(80,.2f,80);
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(BagPath);
            var prototype=BraxObozBagAssembly.Prepare(original);
            bool found=BraxObozBagAssembly.TryFloor(frame.transform.position+Vector3.up*1.6f,null,out var floor);
            if(!found)throw new InvalidOperationException("Temporary floor ray missed");
            var beamObject=GameObject.CreatePrimitive(PrimitiveType.Cube);
            beamObject.name="temporary attachment test beam";beamObject.transform.SetParent(frame.transform,false);
            beamObject.transform.position=floor.point+Vector3.up*(prototype.MountAboveFloorMetres+.17f)*K;
            beamObject.transform.localScale=new Vector3(30*K,.36f*K,.36f*K);
            var beam=beamObject.GetComponent<Collider>();
            var bag=BraxObozBagAssembly.Create(frame.transform,"temporary original bag A",prototype,floor,beam);
            var geometry=bag.GetComponent<BraxObozBagGeometry>();
            Check(prototype.mesh.vertexCount==16130 && prototype.mesh.GetIndexCount(0)==26950*3,
                "original bag retains all 16,130 vertices and 26,950 triangles");
            bool measured=geometry.TryMeasureBodyBounds(out var body);
            Check(measured && Mathf.Abs(body.size.y/K-1.2f)<.001f,
                "rendered leather body measures 1.20 metres before K");
            Check(measured && Mathf.Abs((body.min.y-floor.point.y)/K-.6f)<.001f,
                "rendered body bottom is 0.60 metres above the raycast floor");
            Check(geometry.TryMeasureAttachment(out var attachment) &&
                BraxObozSupportSurface.TryMeasure(beam,attachment,out var mounting) && mounting.distanceWorld<.03f*K,
                "actual original suspension vertex touches its beam");
            var capsule=bag.GetComponent<CapsuleCollider>();
            Check(Mathf.Abs(capsule.height-1.2f)<.0001f &&
                Vector3.Distance(bag.transform.TransformPoint(capsule.center),body.center)<.001f,
                "bag capsule follows the measured body centre and height");
            var nested=new GameObject("temporary K-scaled bag parent").transform;nested.SetParent(frame.transform,false);
            nested.localScale=Vector3.one*K;
            bool nextFloor=BraxObozBagAssembly.TryFloor(floor.point+Vector3.right*4f*K,null,out var secondFloor);
            if(!nextFloor)throw new InvalidOperationException("Second temporary floor ray missed");
            var second=BraxObozBagAssembly.Create(nested,"temporary original bag B",prototype,secondFloor,beam);
            var secondGeometry=second.GetComponent<BraxObozBagGeometry>();
            Check(secondGeometry.SharedMesh==geometry.SharedMesh &&
                second.GetComponent<Renderer>().sharedMaterial==bag.GetComponent<Renderer>().sharedMaterial &&
                second.GetComponent<Renderer>().sharedMaterial.enableInstancing &&
                secondGeometry.TryMeasureBodyBounds(out var otherBody) && Mathf.Abs(otherBody.size.y/K-1.2f)<.001f,
                "instances share one mesh/material and K is not applied twice under a scaled parent");
            Check(Mathf.Abs((bag.transform.position.y-bag.dol)-body.min.y)<.001f &&
                Mathf.Abs((bag.transform.position.y-bag.gora)-body.max.y)<.001f,
                "swing hit-height parameters come from the rendered body vertices");
            // Explicit method-level EditMode check of existing shared logic.
            // No new animation, source modification or lifecycle SendMessage.
            if(bag.isActiveAndEnabled)
            {
                typeof(BraxWorek).GetField("spoczynek",Hidden).SetValue(bag,bag.transform.rotation);
                Vector3 from=body.center-Vector3.forward*(bag.promien+.8f);
                bool struck=bag.StrikeFromNpc(from,Vector3.forward);
                Vector2 impulse=(Vector2)typeof(BraxWorek).GetField("v",Hidden).GetValue(bag);
                Check(struck && impulse.sqrMagnitude>.01f,"actual existing NPC strike method adds a bag impulse");
                bool far=bag.StrikeFromNpc(body.center-Vector3.forward*10f,Vector3.forward);
                bool backwards=bag.StrikeFromNpc(from,Vector3.back);
                Vector2 after=(Vector2)typeof(BraxWorek).GetField("v",Hidden).GetValue(bag);
                Check(!far && !backwards && (after-impulse).sqrMagnitude<1e-12f,
                    "out-of-range and backwards strikes do not add an impulse");
            }
            else BraxBuildQuality.Warn("OBÓZ TEST CIOSU POMINIĘTY: edytor nie aktywował BraxWorek. "+
                "Wymagany test PlayMode; nie oznaczono niewykonanego ciosu jako PASS.");

            // A raised fixture makes a fake ground-at-zero pass impossible.
            float localFloor=1.6f/K;
            var yoga=BraxObozGymEquipment.Place(frame.transform,"yoga_mat_1",new Vector3(-10,localFloor,8));
            var yogaGeometry=yoga.GetComponent<BraxObozEquipmentGeometry>();
            Check(yogaGeometry.TryMeasureBounds(out var yogaBounds) &&
                Mathf.Abs(yogaBounds.min.y-1.6f)<.001f && Mathf.Abs(yogaBounds.size.y/K-.02f)<.001f,
                "source yoga mat authored at -1 m is independently grounded at the real raised floor");
            var press=BraxObozGymEquipment.Place(frame.transform,"bench_press",new Vector3(-6,localFloor,8));
            var rack=BraxObozGymEquipment.Place(frame.transform,"loaded_weight_rack",new Vector3(0,localFloor,8));
            Check(press.GetComponent<BraxObozEquipmentGeometry>().geometry.Length==12 &&
                rack.GetComponent<BraxObozEquipmentGeometry>().geometry.Length==11,
                "bench press and loaded rack retain every original supported part");
            var pullup=BraxObozGymEquipment.Place(frame.transform,"pull_up_rack",new Vector3(6,localFloor,8));
            var pullupGeometry=pullup.GetComponent<BraxObozEquipmentGeometry>();
            var filter=pullupGeometry.geometry.Single();var collision=filter.GetComponent<MeshCollider>();
            Check(collision && !collision.convex && collision.sharedMesh==filter.sharedMesh &&
                pullup.GetComponentsInChildren<BoxCollider>().Length==0,
                "pull-up frame collision uses original triangles, not a solid bounds box");
            Physics.SyncTransforms();var bounds=filter.GetComponent<Renderer>().bounds;int holes=0;
            for(int x=1;x<=3;x++)for(int y=1;y<=3;y++)
            {
                var point=new Vector3(Mathf.Lerp(bounds.min.x,bounds.max.x,x*.25f),
                    Mathf.Lerp(bounds.min.y,bounds.max.y,y*.25f),bounds.min.z-.2f);
                if(!collision.Raycast(new Ray(point,Vector3.forward),out _,bounds.size.z+.4f))holes++;
            }
            Check(holes>0,"real raycasts pass through empty space inside the original pull-up frame");
        }
        catch(Exception ex){checks.Add("FAIL original training regression: "+ex);}
        finally{if(frame)Object.DestroyImmediate(frame);}
        return checks.ToArray();
    }
}
