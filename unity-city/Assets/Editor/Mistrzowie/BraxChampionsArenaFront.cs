using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// OWNER RENDER f2faacc9 (9.11): "Ma być jak na zdjęciu łącznie z szyldem",
// "Zadaszenie tak samo", "Na wejściu za szybą mogą być jakieś sofy kwiaty".
// Front public entrance of A1, building space, nominal metres x K once.
// Render proportions measured at the facade plane (~23.5 px per metre):
// gold canopy underside ~6.2 m, fascia ~1.2 m, sign box on top of it
// ~38-39 m wide and ~6.5 m high, black belt ~7.6-11.4 m, lounge right above.
public static partial class BraxChampionsArenaArrival
{
    public const float CanopyUnderside=6.20f, CanopyTop=7.40f, CanopyHalfWidth=20f, CanopyFrontZ=-63.6f;
    public const float BeltBottom=7.60f, BeltTop=11.40f;
    // Upper VIP lounge floor (old slab top 18.24) now sits on the belt top,
    // exactly like the render: sofas visible right above the black band.
    public const float LoungeDrop=(BeltTop+.12f)-18.24f;
    const string LobbyModels="Assets/Source/Mistrzowie/ArenaLobby/";

    static float FrontGlassZ(float x)=>-59f*Mathf.Sqrt(Mathf.Max(0f,1f-x*x/(96f*96f)));

    // Orient a quad/triangle so Unity's cross(b-a,c-a) faces the wanted side.
    static void FrontTri(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 facing)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),facing)<0){var s=b;b=c;c=s;}
        int n=v.Count;v.Add(a*K);v.Add(b*K);v.Add(c*K);t.Add(n);t.Add(n+1);t.Add(n+2);
    }
    static void FrontQuad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 facing)
    {
        FrontTri(v,t,a,b,c,facing);FrontTri(v,t,a,c,d,facing);
    }
    static GameObject FrontMesh(Transform parent,string name,List<Vector3> v,List<int> t,Material m,bool solid)
    {
        var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.SetVertices(v);mesh.SetTriangles(t,0);
        // Metric planar UV (1 unit = 1 nominal metre) on the dominant axis.
        mesh.RecalculateNormals();
        var n=mesh.normals;var uv=new Vector2[v.Count];
        for(int i=0;i<v.Count;i++)
        {
            var p=v[i]/K;var a=n[i];
            if(Mathf.Abs(a.y)>.7f)uv[i]=new Vector2(p.x,p.z);
            else if(Mathf.Abs(a.x)>.7f)uv[i]=new Vector2(p.z,p.y);
            else uv[i]=new Vector2(p.x,p.y);
        }
        mesh.uv=uv;mesh.RecalculateTangents();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,"Assets/PlayableGenerated/Mistrzowie/Buildings/Meshes/"+name.Replace(' ','_').Replace('|','_')+".asset");
        var go=new GameObject(name);go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=m;
        if(solid)go.AddComponent<MeshCollider>().sharedMesh=mesh;
        return go;
    }

    // Render: thick champagne-gold canopy straight across the entrance, the
    // black soffit full of warm downlights, no posts (cantilever from the belt).
    static void FrontGoldCanopy(Transform group,Dictionary<string,Material> exterior)
    {
        var root=Group(group,"A1 PUBLIC SOUTH | owner render gold canopy 40 x 4.4-5.9 x 1.2",Vector3.zero);
        const int parts=48;
        float y0=CanopyUnderside,y1=CanopyTop,fz=CanopyFrontZ,hw=CanopyHalfWidth;
        float BackZ(float x)=>FrontGlassZ(x)+.20f; // tucked 20 cm into the glazing line
        var gv=new List<Vector3>();var gt=new List<int>();
        var bv=new List<Vector3>();var bt=new List<int>();
        for(int i=0;i<parts;i++)
        {
            float x0=Mathf.Lerp(-hw,hw,i/(float)parts),x1=Mathf.Lerp(-hw,hw,(i+1)/(float)parts);
            // Black top (seen only from the upper lounge) and black soffit.
            FrontQuad(bv,bt,new Vector3(x0,y1,BackZ(x0)),new Vector3(x1,y1,BackZ(x1)),new Vector3(x1,y1,fz),new Vector3(x0,y1,fz),Vector3.up);
            FrontQuad(bv,bt,new Vector3(x0,y0,BackZ(x0)),new Vector3(x1,y0,BackZ(x1)),new Vector3(x1,y0,fz),new Vector3(x0,y0,fz),Vector3.down);
            // Gold front fascia, full 1.2 m.
            FrontQuad(gv,gt,new Vector3(x0,y0,fz),new Vector3(x0,y1,fz),new Vector3(x1,y1,fz),new Vector3(x1,y0,fz),Vector3.back);
        }
        foreach(float s in new[]{-1f,1f})
        {
            float x=s*hw;
            FrontQuad(gv,gt,new Vector3(x,y0,BackZ(x)),new Vector3(x,y1,BackZ(x)),new Vector3(x,y1,fz),new Vector3(x,y0,fz),new Vector3(s,0,0));
        }
        FrontMesh(root,"A1 owner render canopy gold fascia",gv,gt,exterior["arena_gold"],true);
        FrontMesh(root,"A1 owner render canopy black soffit and top",bv,bt,exterior["arena_black"],true);

        // Warm line under the front edge and the three rows of downlights.
        var light=exterior["light"];
        Box(root,"A1 canopy | warm LED line under gold fascia",light,
            new Vector3(0,y0-.012f,fz+.12f),new Vector3(hw*2f-.30f,.024f,.06f),false);
        int lamps=0;
        foreach(float dz in new[]{.95f,2.25f,3.55f})
            for(int i=0;i<21;i++)
            {
                float x=-18.5f+i*1.85f,z=fz+dz;
                if(z>BackZ(x)-.45f)continue;
                Box(root,"A1 canopy | recessed warm downlight",light,
                    new Vector3(x,y0-.008f,z),new Vector3(.26f,.016f,.26f),false);
                lamps++;
            }
        foreach(float x in new[]{-12f,0f,12f})
        {
            var l=new GameObject("A1 canopy | warm downlight wash").AddComponent<Light>();
            l.transform.SetParent(root,false);l.transform.localPosition=new Vector3(x,y0-.6f,fz+2.2f)*K;
            l.type=LightType.Point;l.color=new Color(1f,.82f,.55f);l.intensity=1.1f;l.range=9f*K;
            l.shadows=LightShadows.None;l.renderMode=LightRenderMode.ForceVertex;
        }
        Debug.Log("A1_OWNER_RENDER_CANOPY width="+(hw*2f)+" front="+fz+" underside="+y0+" top="+y1+
            " downlights="+lamps+" posts=0 gold_fascia=1.2m");
    }

    // ---- entrance lobby furniture behind the front glass (both sides) ----
    static float LobbyFloorY(Transform arenaRoot,float x,float z)
    {
        Physics.SyncTransforms();
        var start=arenaRoot.TransformPoint(new Vector3(x,4f,z)*K);
        float best=.245f,bestErr=float.PositiveInfinity;
        foreach(var hit in Physics.RaycastAll(start,Vector3.down,8f*K,~0,QueryTriggerInteraction.Ignore))
        {
            if(hit.normal.y<.7f)continue;
            float y=arenaRoot.InverseTransformPoint(hit.point).y/K;
            if(Mathf.Abs(y-.245f)<bestErr&&Mathf.Abs(y-.245f)<1f){bestErr=Mathf.Abs(y-.245f);best=y;}
        }
        return best;
    }

    static bool HasTextures(GameObject go)
    {
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
            foreach(var m in r.sharedMaterials)
                if(m)foreach(var n in m.GetTexturePropertyNames())if(m.GetTexture(n))return true;
        return false;
    }

    static bool PlaceLobbyModel(Transform arenaRoot,Transform parent,string file,string label,float x,float z,float yaw,bool solid)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(LobbyModels+file);
        if(!source){BraxBuildQuality.Warn("A1_LOBBY_MODEL_MISSING: "+LobbyModels+file+" (prepare_arena_lobby_models.py)");return false;}
        // Import-order guard (same as R1): if glTFast imported the model before
        // its texture assets, import it once more so materials get their maps.
        if(!HasTextures(source))
        {
            AssetDatabase.ImportAsset(LobbyModels+file,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            source=AssetDatabase.LoadAssetAtPath<GameObject>(LobbyModels+file);
            if(!source)return false;
        }
        var go=PrefabUtility.InstantiatePrefab(source) as GameObject;
        if(!go){BraxBuildQuality.Warn("A1_LOBBY_MODEL_INSTANTIATE_FAILED: "+file);return false;}
        go.name="A1 LOBBY | "+label+" | Poly Haven CC0 real size";
        go.transform.SetParent(parent,false);
        go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        go.transform.localScale=Vector3.one*K; // model is in real metres
        float floor=LobbyFloorY(arenaRoot,x,z);
        go.transform.localPosition=new Vector3(x,floor,z)*K;
        foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        Physics.SyncTransforms();
        var b=RendererBounds(go);
        if(b.size.y<=.001f){BraxBuildQuality.Warn("A1_LOBBY_MODEL_EMPTY: "+file);return false;}
        // Visible bottom exactly on the lobby floor (no floating, no sinking).
        go.transform.position+=Vector3.up*(arenaRoot.TransformPoint(new Vector3(x,floor,z)*K).y-b.min.y);
        if(!HasTextures(go))BraxBuildQuality.Warn("A1_LOBBY_MODEL_UNTEXTURED: "+file);
        if(solid)
        {
            Physics.SyncTransforms();
            b=RendererBounds(go);
            var box=go.AddComponent<BoxCollider>();
            box.center=go.transform.InverseTransformPoint(b.center);
            var size=go.transform.InverseTransformVector(b.size);
            box.size=new Vector3(Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z));
        }
        return true;
    }

    static void EntranceLobbyFurniture(Transform arenaRoot,Dictionary<string,Material> exterior)
    {
        var root=Group(arenaRoot,"A1 LOBBY | entrance lounges behind the front glass (owner render)",Vector3.zero);
        var gold=exterior["arena_gold"];var stone=exterior["dark_stone"];var light=exterior["light"];
        int placed=0,wanted=0;
        foreach(float s in new[]{-1f,1f})
        foreach(float ax in new[]{14f,26f})
        {
            float x=s*ax,z=FrontGlassZ(ax)+4.2f;
            // Sofa faces the glass (-Z): its front is what the street sees.
            wanted++;if(PlaceLobbyModel(arenaRoot,root,"sofa_02/sofa_02_2k.gltf","black tufted leather sofa",x,z,180f,true))placed++;
            float tz=z-1.30f,floor=LobbyFloorY(arenaRoot,x,tz);
            Box(root,"A1 LOBBY | coffee table dark granite top",stone,new Vector3(x,floor+.40f,tz),new Vector3(1.10f,.05f,.62f),false);
            foreach(float lx in new[]{-.45f,.45f})foreach(float lz in new[]{-.22f,.22f})
                Box(root,"A1 LOBBY | coffee table gold leg",gold,new Vector3(x+lx,floor+.19f,tz+lz),new Vector3(.035f,.38f,.035f),false);
            foreach(float side in new[]{-1f,1f})
            {
                wanted++;
                if(PlaceLobbyModel(arenaRoot,root,"modern_arm_chair_01/modern_arm_chair_01_2k.gltf","black leather armchair",
                    x+side*1.70f,tz,side<0?90f:-90f,true))placed++;
            }
            // Floor lamp beside the sofa: gold stem, warm shade.
            float lxp=x+s*1.35f,lzp=z+.10f,lf=LobbyFloorY(arenaRoot,lxp,lzp);
            Box(root,"A1 LOBBY | floor lamp gold base",gold,new Vector3(lxp,lf+.015f,lzp),new Vector3(.34f,.03f,.34f),false);
            Box(root,"A1 LOBBY | floor lamp gold stem",gold,new Vector3(lxp,lf+.80f,lzp),new Vector3(.035f,1.55f,.035f),false);
            Box(root,"A1 LOBBY | floor lamp warm shade",light,new Vector3(lxp,lf+1.62f,lzp),new Vector3(.42f,.34f,.42f),false);
            // Tall plants at both ends of the group, a smaller one by the table.
            foreach(float px in new[]{-3.30f,3.30f})
            {
                wanted++;
                if(PlaceLobbyModel(arenaRoot,root,"potted_plant_01/potted_plant_01_1k.gltf","potted tree",x+px,z+.30f,(ax*37f+px*53f+s*90f)%360f,true))placed++;
            }
        }
        // Plants flanking the entrance doors inside the glass.
        foreach(float s in new[]{-1f,1f})
        {
            wanted++;
            if(PlaceLobbyModel(arenaRoot,root,"potted_plant_02/potted_plant_02_1k.gltf","leafy plant by the door",s*6.3f,FrontGlassZ(6.3f)+1.15f,s*35f,true))placed++;
        }
        Debug.Log("A1_LOBBY_FURNITURE placed="+placed+"/"+wanted+" lounges=4 sofas=4 armchairs=8 plants=10 lamps=4");
        if(placed<wanted)BraxBuildQuality.Warn("A1_LOBBY_FURNITURE incomplete "+placed+"/"+wanted);
    }

    // Owner 9.11: "przed areną prawa lewa palmy mają być". Same owned city palm
    // as the approved T11 casino (BraxMiastoAssety miasto_palmy/palma), real
    // size, LOD from its own nodes. Avenue outside the bollards (|x|=8.6) and
    // the 6.4 m carpet aisle, inside the central walking axis (car parks start
    // at |x|=30), from the planters down to the street sidewalk (z=-133).
    static void EntrancePalmAvenue(Transform arenaRoot,Transform parent)
    {
        var palm=BraxMiastoAssety.Model("miasto_palmy","palma");
        if(!palm){BraxBuildQuality.Warn("A1_ENTRANCE_PALMS_MISSING: miasto_palmy/palma");return;}
        var root=Group(parent,"A1 ENTRANCE | palm avenue left and right (owner 9.11)",Vector3.zero);
        var spots=new List<Vector2>();
        foreach(float s in new[]{-1f,1f})
        {
            foreach(float z in new[]{-80f,-92f,-104f,-116f,-128f})spots.Add(new Vector2(s*13.5f,z));
            spots.Add(new Vector2(s*26.5f,-66f));
        }
        int placed=0;
        foreach(var at in spots)
        {
            float y=.245f;
            try{y=ActualFloorYNominal(arenaRoot,at.x,at.y);}catch(Exception){}
            var world=arenaRoot.TransformPoint(new Vector3(at.x,y,at.y)*K);
            var instance=BraxMiastoAssety.Postaw(root,palm,"A1 entrance palm",world,(at.x*7f+at.y*3f)%360f,1f,false,true);
            if(!instance)continue;
            BraxMiastoAssety.LodyZWezlow(instance,new[]{.25f,.08f,.012f});
            placed++;
        }
        Debug.Log("A1_ENTRANCE_PALMS placed="+placed+"/"+spots.Count+" x=+-13.5 (z -80..-128) and +-26.5 (z -66)");
        if(placed<spots.Count)BraxBuildQuality.Warn("A1_ENTRANCE_PALMS incomplete "+placed+"/"+spots.Count);
    }
}
