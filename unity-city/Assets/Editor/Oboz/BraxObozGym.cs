using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// The same original geometry/collision source used by BraxGymBuild.
// This copies only into the camp; the first city gym is not changed.
// Latest finishing/mural assets still require reconciliation and visual review.
public static class BraxObozGym
{
    const string Source="Assets/Source/Prywatne/Oboz/environment/gym/";
    const float K=1.294f;
    [Serializable] sealed class World { public BoxData[] boxes; public CircleData[] circles; }
    [Serializable] sealed class BoxData { public float minX,maxX,minZ,maxZ,top,bottom; public string tag; }
    [Serializable] sealed class CircleData { public float x,z,r,top,bottom; public string tag; }
    public sealed class Result { public Transform root; public BraxCityDoor door; public BraxWorek[] bags; public BraxObozArena arena; }
    static Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
    static GameObject Box(Transform parent,string name,Vector3 at,Vector3 size)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=at;g.AddComponent<BoxCollider>().size=size;return g;
    }
    public static Result Copy(Transform parent,Vector3 campPosition,float yaw)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"braxhorn-original-gym__unity.glb");
        if(!prefab)throw new InvalidOperationException("Brak oryginalnego eksportu salki");
        var data=JsonUtility.FromJson<World>(File.ReadAllText(Source+"gym-collisions.json"));
        if(data.boxes==null||data.boxes.Length<40)throw new InvalidOperationException("Niepełne kolizje oryginalnej salki");
        var root=new GameObject("Bunkier główny — źródłowa salka, wykończenie do odbioru").transform;
        var gym=Object.Instantiate(prefab);gym.transform.SetParent(root,false);
        var renderers=gym.GetComponentsInChildren<MeshRenderer>(true);
        if(renderers.Length<100)throw new InvalidOperationException("Niepełna geometria oryginalnej salki");
        var solids=new GameObject("Oryginalne kolizje po konwersji osi X").transform;solids.SetParent(root,false);
        GameObject doorCollider=null;int number=0;
        foreach(var b in data.boxes)
        {
            if(b.maxX<=b.minX||b.maxZ<=b.minZ||b.top<=b.bottom)continue;
            var c=Box(solids,"Kolizja "+(number++)+" "+b.tag,new Vector3(-(b.minX+b.maxX)*.5f,(b.top+b.bottom)*.5f,(b.minZ+b.maxZ)*.5f),new Vector3(b.maxX-b.minX,b.top-b.bottom,b.maxZ-b.minZ));
            if(b.tag=="door")doorCollider=c;
        }
        foreach(var c in data.circles??Array.Empty<CircleData>())
            Box(solids,"Oryginalna przeszkoda okrągła",new Vector3(-c.x,(c.top+c.bottom)*.5f,c.z),new Vector3(c.r*2,c.top-c.bottom,c.r*2));
        Box(solids,"Podłoga hali",new Vector3(0,-.1f,-2.5f),new Vector3(37.2f,.2f,28.2f));
        Box(solids,"Podłoga wejścia",new Vector3(6.75f,-.1f,16.4f),new Vector3(16.7f,.2f,9.6f));
        var pivot=new GameObject("Oryginalne otwierane drzwi salki").transform;pivot.SetParent(root,false);pivot.localPosition=new Vector3(6.8f,0,11.56f);
        for(int i=24;i<=31;i++)
        {
            var part=Find(gym.transform,"Gym_Mesh_"+i);if(!part)throw new InvalidOperationException("Brak elementu drzwi "+i);part.SetParent(pivot,true);
        }
        if(!doorCollider)throw new InvalidOperationException("Brak kolizji drzwi salki");
        doorCollider.transform.SetParent(pivot,true);
        var door=pivot.gameObject.AddComponent<BraxCityDoor>();door.id="oboz-gym";door.label="SALKA BRAXHORN";door.leaves=new[]{pivot};door.sides=new[]{1f};
        var bags=new List<BraxWorek>();
        foreach(float z in new[]{-8.4f,-5.4f,-2.4f,.6f})
        {
            var bag=new GameObject("Worek salki "+bags.Count).transform;bag.SetParent(root,false);bag.localPosition=new Vector3(14,4.6f,z);
            foreach(var r in renderers)
            {
                var b=r.bounds;if(Mathf.Abs(b.center.x-14)<.45f&&Mathf.Abs(b.center.z-z)<.5f&&b.size.x<1.4f&&b.size.z<1.4f&&b.center.y>1f&&b.max.y<4.65f)r.transform.SetParent(bag,true);
            }
            var skin=Find(bag,"Gym_Mesh_"+(81+bags.Count*8));var renderer=skin?skin.GetComponent<MeshRenderer>():null;
            if(!renderer)throw new InvalidOperationException("Brak korpusu worka salki "+bags.Count);
            var bounds=renderer.bounds;var c=bag.gameObject.AddComponent<CapsuleCollider>();
            c.center=bag.InverseTransformPoint(bounds.center);c.radius=Mathf.Max(bounds.extents.x,bounds.extents.z);c.height=bounds.size.y;
            var swing=bag.gameObject.AddComponent<BraxWorek>();swing.gora=(bag.position.y-bounds.max.y)*K;swing.dol=(bag.position.y-bounds.min.y)*K;
            swing.dlugosc=(bag.position.y-bounds.center.y)*K;swing.promien=c.radius*K;bags.Add(swing);
        }
        var arenaObject=new GameObject("Ring 2 — źródłowa salka");arenaObject.transform.SetParent(root,false);arenaObject.transform.localPosition=new Vector3(-9,.95f,-5.5f);
        var arena=arenaObject.AddComponent<BraxObozArena>();arena.label="Ring 2";arena.halfSize=new Vector2(3.4f,3.4f);
        // Arena.Inside uses local coordinates, so do not multiply its local size
        // again when its parent receives the world scale.
        root.SetParent(parent,false);root.localPosition=campPosition*K;root.localRotation=Quaternion.Euler(0,yaw,0);root.localScale=Vector3.one*K;
        return new Result{root=root,door=door,bags=bags.ToArray(),arena=arena};
    }
}
