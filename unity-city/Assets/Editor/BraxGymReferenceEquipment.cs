using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Scene assembly only. The existing player's directional pendulum, attacks,
// camera, character, city navigation and input implementations remain intact.
public static class BraxGymReferenceEquipment
{
    const string Models = "Assets/Kickboxing/Models/";

    public static void Apply(BraxGymPlayer player, string generated)
    {
        if (!player || player.bags == null || player.bags.Length != 4)
            throw new InvalidOperationException("Four original bag pivots are required.");
        string folder = generated + "/KickboxingEquipment";
        Directory.CreateDirectory(folder); AssetDatabase.Refresh();
        var room = GameObject.Find("BRAXHORN");
        if (!room) throw new InvalidOperationException("Reference room assembly is missing.");
        var steel = AssetDatabase.LoadAssetAtPath<Material>(generated + "/ReferenceRebuild/Black_powder_coated_steel.mat");
        if (!steel) throw new InvalidOperationException("Reviewed black steel material is missing.");
        var frame = new GameObject("Bag suspension beam"); frame.transform.SetParent(room.transform, false);
        // The existing floor-fixed columns terminate at this longitudinal beam.
        // The lowest flange and each reviewed clamp have the same contact plane.
        Beam(frame.transform, "Lower flange", new Vector3(14,4.827f,-3), new Vector3(.34f,.05f,14.3f), steel);
        Beam(frame.transform, "Beam web", new Vector3(14,4.985f,-3), new Vector3(.085f,.266f,14.3f), steel);
        Beam(frame.transform, "Upper flange", new Vector3(14,5.143f,-3), new Vector3(.34f,.05f,14.3f), steel);
        var swingSource = RequiredModel("BagSwing.glb");
        var mountSource = RequiredModel("BagMount.glb");
        for (int i = 0; i < player.bags.Length; i++)
        {
            Transform pivot = player.bags[i];
            if (Mathf.Abs(pivot.position.y-4.6f)>.001f)
                throw new InvalidOperationException("Original bag pivot height changed.");
            // Remove all visual and collision children from the obsolete bags,
            // including the earlier stitched-cylinder finishing pass.
            foreach (Transform old in pivot.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(old.gameObject);
            pivot.name = "BRAXHORN suspended leather bag " + (i+1);
            var bag = UnityEngine.Object.Instantiate(swingSource,pivot,false);
            bag.name = "Reviewed leather body, straps and live suspension";
            LayerAndShadows(bag);
            var mount = UnityEngine.Object.Instantiate(mountSource,frame.transform,false);
            mount.name = "Fixed clamp and swivel " + (i+1);
            mount.transform.position = pivot.position;
            LayerAndShadows(mount);
            var body = new GameObject("Local padded bag collision"); body.transform.SetParent(pivot,false);
            body.layer = 10;
            var collider = body.AddComponent<CapsuleCollider>();
            collider.direction=1; collider.center=new Vector3(0,-3.25f,0); collider.height=1.8f; collider.radius=.275f;
            var bounds = bag.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).ToArray();
            if (bounds.Length==0 || bounds.Min(b=>b.min.y)<.40f || bounds.Max(b=>b.max.y)>4.72f)
                throw new InvalidOperationException("Reviewed bag is not freely suspended above the floor: "+i);
        }
        MakeMirror("BRAXHORN/MainMirrorAnchor",player.view,folder,"Main",768,384,24,42);
        MakeMirror("BRAXHORN/Annex/WCMirrorAnchor",player.view,folder,"WC",384,384,12,9,true);
        var diagnostics=player.gameObject.AddComponent<BraxGymRoomDiagnostics>();
        diagnostics.mirror=GameObject.Find("BRAXHORN/MainMirrorAnchor").GetComponent<BraxGymPlanarMirror>();
        diagnostics.wcFixture=GameObject.Find("BRAXHORN/Annex/WCFixtureAnchor").transform;
        diagnostics.roomDoors=player.cityWorld.doors.Where(d=>d.id.StartsWith("gym-")).ToArray();
        diagnostics.hallBounds=new Bounds(new Vector3(0,4.25f,-1),new Vector3(36,8.5f,24));
        diagnostics.annexBounds=new Bounds(new Vector3(-21.75f,1.55f,2.5f),new Vector3(7.5f,3.1f,11));
        AssetDatabase.SaveAssets();
        Debug.Log("BRAXHORN_REFERENCE_EQUIPMENT four reviewed bags, two visible-only live mirrors, original player unchanged");
    }

    static GameObject RequiredModel(string file)
    {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Models+file)
            ??AssetDatabase.LoadAllAssetsAtPath(Models+file).OfType<GameObject>().FirstOrDefault();
        if (!model) throw new InvalidOperationException("Reviewed source model missing: "+file);
        return model;
    }

    static void LayerAndShadows(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=10;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode=ShadowCastingMode.Off;
        foreach (var c in root.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
    }

    static void Beam(Transform root,string name,Vector3 position,Vector3 size,Material material)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;
        o.transform.SetParent(root,false);o.transform.position=position;o.transform.localScale=size;o.layer=10;
        UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        var renderer=o.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
    }

    static void MakeMirror(string path,Camera camera,string folder,string label,int width,int height,float rate,float range,bool westWall=false)
    {
        var anchor=GameObject.Find(path);
        if (!anchor) throw new InvalidOperationException("Mirror mounting frame is missing: "+path);
        if (westWall)
        {
            anchor.transform.rotation=Quaternion.LookRotation(Vector3.right,Vector3.up);
            // Reflective coating sits in front of the solid backing plate.
            anchor.transform.position+=Vector3.right*.03f;
        }
        var mesh=new Mesh{name=label+" reflective glass"};
        mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
        mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};
        mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,folder+"/"+label+"Mirror.asset");
        anchor.AddComponent<MeshFilter>().sharedMesh=mesh;
        var shader=Shader.Find("Braxhorn/GymPlanarMirror");if(!shader)throw new InvalidOperationException("Live mirror shader missing.");
        var material=new Material(shader){name=label+" live reflective glass"};
        Vector3 n=anchor.transform.forward;material.SetVector("_MirrorPlane",new Vector4(n.x,n.y,n.z,-Vector3.Dot(n,anchor.transform.position)));
        AssetDatabase.CreateAsset(material,folder+"/"+label+"Mirror.mat");
        var renderer=anchor.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        anchor.layer=9;camera.cullingMask|=1<<9;
        var mirror=anchor.AddComponent<BraxGymPlanarMirror>();mirror.sourceCamera=camera;mirror.mirrorRenderer=renderer;
        mirror.textureWidth=width;mirror.textureHeight=height;mirror.refreshRate=rate;mirror.maximumViewDistance=range;
    }
}
