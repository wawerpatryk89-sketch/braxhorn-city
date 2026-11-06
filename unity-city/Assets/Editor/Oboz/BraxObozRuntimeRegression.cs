using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Executes real Unity serialization, materials, meshes and clip samples in the
// existing temporary regression scene. No test object enters the saved camp.
public static class BraxObozRuntimeRegression
{
    const float K=1.294f;
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Read(object target,string field)=>target.GetType().GetField(field,Hidden).GetValue(target);
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,Hidden).SetValue(target,value);
    static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Hidden).Invoke(target,args);

    static GameObject SkinFixture(out Mesh mesh,out BraxObozIdleFootContact contact)
    {
        var root=new GameObject("temporary two-foot runtime fixture");
        var body=root.AddComponent<CharacterController>();body.enabled=false;
        var visual=new GameObject("temporary visual").transform;visual.SetParent(root.transform,false);
        var left=new GameObject("mixamorig:LeftFoot").transform;left.SetParent(visual,false);
        var right=new GameObject("mixamorig:RightFoot").transform;right.SetParent(visual,false);
        left.localPosition=new Vector3(-.15f,.1f,0);right.localPosition=new Vector3(.15f,.1f,0);
        mesh=new Mesh{name="temporary weighted soles"};
        mesh.vertices=new[]{new Vector3(-.25f,0,-.1f),new Vector3(-.05f,0,-.1f),new Vector3(-.15f,0,.2f),
            new Vector3(.05f,0,-.1f),new Vector3(.25f,0,-.1f),new Vector3(.15f,0,.2f)};
        mesh.triangles=new[]{0,2,1,3,5,4};
        mesh.bindposes=new[]{left.worldToLocalMatrix*visual.localToWorldMatrix,right.worldToLocalMatrix*visual.localToWorldMatrix};
        var weights=new BoneWeight[6];
        for(int i=0;i<6;i++)weights[i]=new BoneWeight{boneIndex0=i<3?0:1,weight0=1f};
        mesh.boneWeights=weights;mesh.RecalculateBounds();
        var skin=visual.gameObject.AddComponent<SkinnedMeshRenderer>();
        skin.sharedMesh=mesh;skin.bones=new[]{left,right};skin.rootBone=visual;
        contact=root.AddComponent<BraxObozIdleFootContact>();
        contact.Configure(body,visual,visual.gameObject.AddComponent<Animation>());
        return root;
    }

    public static string[] Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string label)=>checks.Add((ok?"PASS ":"FAIL ")+label);
        GameObject source=null,loaded=null;BraxObozFootprintTrail trail=null;
        try
        {
            source=new GameObject("temporary footprint serialization");
            var playerObject=new GameObject("temporary player");playerObject.transform.SetParent(source.transform,false);
            var npcObject=new GameObject("temporary NPC");npcObject.transform.SetParent(source.transform,false);
            var player=playerObject.AddComponent<BraxGymPlayer>();var npc=npcObject.AddComponent<BraxObozNpc>();
            var configured=source.AddComponent<BraxObozFootprintTrail>();configured.Configure(player,new[]{npc});
            // Same engine session: InstanceID references must survive. This
            // does not claim persistence across reopening a scene or Unity.
            string json=JsonUtility.ToJson(configured);
            loaded=new GameObject("temporary reloaded footprint effect");
            trail=loaded.AddComponent<BraxObozFootprintTrail>();JsonUtility.FromJsonOverwrite(json,trail);
            var actors=(Transform[])Read(trail,"actors");
            Check(actors.Length==2 && actors[0]==player.transform && actors[1]==npc.transform,
                "footprint actor references survive same-session JsonUtility InstanceID roundtrip");
            Call(trail,"Start");
            var pool=(Array)Read(trail,"pool");
            Check(pool!=null && pool.Length==40 && ((Vector3[])Read(trail,"previous")).Length==2,
                "reloaded footprint effect rebuilds tracking and exactly 40 slots");
            if(pool==null)throw new InvalidOperationException("Footprint shader/pool unavailable");
            Mesh sharedMesh=null;Material sharedMaterial=null;bool shared=true;
            foreach(var renderer in loaded.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                if(!sharedMesh){sharedMesh=mesh;sharedMaterial=renderer.sharedMaterial;}
                shared&=mesh==sharedMesh && renderer.sharedMaterial==sharedMaterial;
            }
            Check(shared && loaded.GetComponentsInChildren<Collider>(true).Length==0,
                "all footprint slots share one mesh and material and have no colliders");
            Check(sharedMaterial && sharedMaterial.HasProperty("_Color") &&
                !ShaderUtil.ShaderHasError(sharedMaterial.shader),
                "footprint Resources shader imports without error and exposes fade tint");
            for(int i=0;i<55;i++)Call(trail,"Spawn",Vector3.zero,Vector3.up,Vector3.forward);
            Check(loaded.GetComponentsInChildren<MeshRenderer>(true).Length==40,
                "55 stamps reuse the same 40 GameObjects");
            foreach(var mark in pool)mark.GetType().GetField("born").SetValue(mark,Time.time-10f);
            Call(trail,"LateUpdate");
            var first=loaded.GetComponentInChildren<MeshRenderer>(true);
            var properties=new MaterialPropertyBlock();first.GetPropertyBlock(properties);
            Check(Mathf.Abs(properties.GetColor("_Color").a-.38f)<.01f,
                "footprint alpha is one half after 10 seconds");
            foreach(var mark in pool)mark.GetType().GetField("born").SetValue(mark,Time.time-21f);
            Call(trail,"LateUpdate");
            int active=0;foreach(var renderer in loaded.GetComponentsInChildren<MeshRenderer>(true))
                if(renderer.gameObject.activeSelf)active++;
            Check(active==0,"all footprint stamps expire after 20 seconds");
        }
        catch(Exception ex){checks.Add("FAIL footprint runtime regression: "+ex);}
        finally
        {
            // Start was invoked explicitly in EditMode. Clean temporary runtime
            // allocations here so MonoBehaviour.Destroy is never used in EditMode.
            if(trail)
            {
                Set(trail,"pool",null);
                foreach(string field in new[]{"stampMaterial","stampMesh","footprint"})
                {var value=Read(trail,field) as Object;Set(trail,field,null);if(value)Object.DestroyImmediate(value);}
            }
            if(loaded)Object.DestroyImmediate(loaded);if(source)Object.DestroyImmediate(source);
        }

        GameObject actor=null,matObject=null,rayObject=null;Mesh skinMesh=null,liveMesh=null;
        BraxObozDeformableMat mat=null;
        try
        {
            actor=SkinFixture(out skinMesh,out var contact);
            actor.transform.position=new Vector3(9400,.08f*K,9400);
            matObject=GameObject.CreatePrimitive(PrimitiveType.Cube);matObject.name="temporary deformable mat";
            matObject.layer=25;matObject.transform.position=new Vector3(9400,.06f*K,9400);
            matObject.transform.rotation=Quaternion.Euler(0,37f,0);
            matObject.transform.localScale=new Vector3(8*K,.04f*K,8*K);
            mat=matObject.AddComponent<BraxObozDeformableMat>();mat.Configure(8,8);Call(mat,"Start");
            liveMesh=matObject.GetComponent<MeshFilter>().sharedMesh;
            Check(Mathf.Abs(liveMesh.bounds.size.x*matObject.transform.lossyScale.x-8*K)<.0001f,
                "runtime mat mesh preserves real width times K");
            Set(mat,"activeFootCount",0);
            Call(mat,"AddContact",contact,true,8*K,.04f*K,8*K);
            Call(mat,"AddContact",contact,false,8*K,.04f*K,8*K);
            Check((int)Read(mat,"activeFootCount")==2,"mat records both measured soles, not an actor root");
            contact.TryGetSolePosition(true,out var sole);
            Vector3 local=matObject.transform.InverseTransformPoint(sole);
            float depth=(float)Call(mat,"DepthAt",local,8*K,8*K);
            Check(Mathf.Abs(depth-.03f*K)<.0001f,"mat depression is 3 cm times K at a planted sole");
            var positions=(Vector2[])Read(mat,"activeFeet");
            Vector2 away=(positions[0]-positions[1]).normalized;
            Vector3 outside=local+new Vector3(away.x,0,away.y)*(.401f/8f);
            Check(Mathf.Abs((float)Call(mat,"DepthAt",outside,8*K,8*K))<.00001f,
                "mat has no depression just beyond the 0.4 metre contact radius");
            Call(mat,"ApplyDeformation",8*K,.04f*K,8*K);
            float deepest=0;foreach(var vertex in liveMesh.vertices)
                if(vertex.y>-.5f)deepest=Mathf.Max(deepest,(.5f-vertex.y)*.04f*K);
            Check(deepest>.015f*K && deepest<=.03f*K+.0001f,
                "actual mat vertices deform within the 3 cm limit");
            rayObject=new GameObject("temporary rendered mat ray");rayObject.layer=26;
            rayObject.transform.SetPositionAndRotation(matObject.transform.position,matObject.transform.rotation);
            rayObject.transform.localScale=matObject.transform.localScale;
            rayObject.AddComponent<MeshCollider>().sharedMesh=liveMesh;
            Physics.SyncTransforms();
            bool measured=mat.TryGetSurfaceHeight(sole,out float surface);
            bool hit=Physics.Raycast(sole+Vector3.up*.3f,Vector3.down,out var renderedHit,1f,1<<26);
            Check(measured && hit && Mathf.Abs(surface-renderedHit.point.y)<.001f,
                "mat surface API equals a raycast onto its actual deformed triangles");
            bool fixedHit=Physics.Raycast(sole+Vector3.up*.3f,Vector3.down,out var colliderHit,1f,1<<25);
            float contactHeight=fixedHit?(float)Call(contact,"RenderedGroundHeight",colliderHit):float.NaN;
            bool follows=false;
            if(fixedHit && !float.IsNaN(contactHeight))
            {
                contact.PlaceSolesOnSurface(contactHeight);
                follows=contact.TryGetSoleHeight(out float soleY) &&
                    Mathf.Abs(soleY-contactHeight-.006f*K)<.0001f;
            }
            Check(fixedHit && follows && contactHeight<colliderHit.point.y,
                "sole follows depressed visual mat while physical collider height stays fixed");
        }
        catch(Exception ex){checks.Add("FAIL mat runtime regression: "+ex);}
        finally
        {
            if(mat)Set(mat,"dynamicMesh",null);
            if(rayObject)Object.DestroyImmediate(rayObject);if(matObject)Object.DestroyImmediate(matObject);
            if(actor)Object.DestroyImmediate(actor);if(liveMesh)Object.DestroyImmediate(liveMesh);
            if(skinMesh)Object.DestroyImmediate(skinMesh);
        }

        GameObject gait=null;Mesh gaitMesh=null;AnimationClip clip=null;
        try
        {
            gait=SkinFixture(out gaitMesh,out var contact);
            clip=new AnimationClip{name="temporary measured Walk",legacy=true,wrapMode=WrapMode.ClampForever};
            foreach(string side in new[]{"Left","Right"})
                clip.SetCurve("mixamorig:"+side+"Foot",typeof(Transform),"localPosition.z",AnimationCurve.Linear(0,0,1,-2f));
            var measured=BraxObozLocomotionCalibration.Measure(contact.visual,Vector3.one*K,clip,"regression");
            contact.visual.localRotation=Quaternion.Euler(0,90f,0);
            var sideways=BraxObozLocomotionCalibration.Measure(contact.visual,Vector3.one*K,clip,"sideways regression");
            contact.visual.localRotation=Quaternion.identity;
            Check(measured.measured && Mathf.Abs(measured.worldUnitsPerSecond-2f*K)<.001f && !sideways.measured,
                "2 m/s stance yields 2*K world m/s; sideways source rotation is rejected");
            Check(BraxObozStrideMatch.TryPlaybackRate(3f*K,measured.worldUnitsPerSecond,true,out float rate) &&
                Mathf.Abs(rate-1.5f)<.001f,"measured stride matches actual root speed without a target speed change");
            Check(BraxObozStrideMatch.TryPlaybackRate(0f,measured.worldUnitsPerSecond,true,out float stopped) && stopped==0,
                "blocked movement produces zero walking playback rate");
            Check(!BraxObozStrideMatch.TryPlaybackRate(3f,0f,true,out _) &&
                !BraxObozStrideMatch.TryPlaybackRate(3f,2f,false,out _),
                "unmeasured clips and paused/jumping/action/blended eligibility do not receive a rate");
        }
        catch(Exception ex){checks.Add("FAIL stride calibration regression: "+ex);}
        finally
        {if(gait)Object.DestroyImmediate(gait);if(gaitMesh)Object.DestroyImmediate(gaitMesh);if(clip)Object.DestroyImmediate(clip);}
        return checks.ToArray();
    }
}
