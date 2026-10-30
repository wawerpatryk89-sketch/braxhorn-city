using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Closed harbour warehouse exterior, built from the audited Poly Haven factory kit.
/// Call between BraxCityArchitecture.Prepare/Finish after the first street review.
/// This authoring module neither removes source geometry nor adds collision/interiors.
/// </summary>
public static class BraxCityIndustrial
{
    const string Kit = "modular_factory_facade";
    const string Output = "Assets/PlayableGenerated/CityRebuild/Architecture/Industrial";
    const float X0 = 52, X1 = 66, Z0 = -6, Z1 = 20.6f;
    const float Floor = .15f, Storey = 3.5f, CrownY = 10.68f;
    static Material masonry, metal, roof, recess;
    static Vector2 uvOrigin, uvX, uvY;
    static int partCount, trimCount;

    // Integration contract only. Disable these renderers, retaining their source
    // assets and the separate original 14 x 26.6 x 11 m closed-building collider.
    public static readonly int[] WholeSourceRendererIds = { 230, 140 };
    public static readonly Bounds OriginalClosedFootprint = Region(X0,X1,0,11,Z0,Z1);

    public sealed class SourceCut
    {
        public readonly string renderer, label;
        public readonly Bounds volume;
        public SourceCut(string renderer, string label, Bounds volume)
        { this.renderer=renderer; this.label=label; this.volume=volume; }
    }

    // Apply each volume ONLY to its named original renderer. CityMesh_231 also
    // contains port containers at z=-19..-7; disabling the whole batch loses them.
    // Margins are 5 mm around the measured Three.js/glTF component bounds.
    public static readonly SourceCut[] SourceCuts = {
        new SourceCut("CityMesh_231","warehouse_old_shutter_01",Region(65.995f,66.065f,-.005f,4.205f,-1.905f,2.905f)),
        new SourceCut("CityMesh_231","warehouse_old_shutter_02",Region(65.995f,66.065f,-.005f,4.205f,7.095f,11.905f)),
        new SourceCut("CityMesh_224","warehouse_old_lintel_01",Region(65.995f,66.205f,4.195f,4.505f,-2.105f,3.105f)),
        new SourceCut("CityMesh_224","warehouse_old_lintel_02",Region(65.995f,66.205f,4.195f,4.505f,6.895f,12.105f)),
        new SourceCut("CityMesh_229","warehouse_old_upper_windows",Region(65.995f,66.045f,6.495f,8.305f,-4.405f,19.405f)),
        new SourceCut("CityMesh_228","warehouse_old_roof",Region(51.995f,66.205f,10.995f,11.405f,-6.205f,20.805f))
    };

    static Bounds Region(float x0,float x1,float y0,float y1,float z0,float z1)
    { return new Bounds(new Vector3((x0+x1)*.5f,(y0+y1)*.5f,(z0+z1)*.5f),new Vector3(x1-x0,y1-y0,z1-z0)); }

    struct Bay
    {
        public float x, width;
        public string wall, detail, dado;
        public Bay(float x,string wall,string detail,string dado="dado_standard_standard_01",float width=3)
        { this.x=x; this.width=width; this.wall=wall; this.detail=detail; this.dado=dado; }
    }

    public static void Apply(Transform parent)
    {
        if(!parent) throw new ArgumentNullException(nameof(parent));
        if(parent.Find("Port warehouse 3 - authored exterior"))
            throw new InvalidOperationException("Warehouse exterior is already authored under this parent.");
        Directory.CreateDirectory(Output); AssetDatabase.Refresh(); partCount=0; trimCount=0;
        var warehouse=Group("Port warehouse 3 - authored exterior",parent,Vector3.zero,0);
        metal=BraxCityMaterials.Surface("Warehouse painted galvanised steel",new Color(.12f,.14f,.15f),.64f,.45f);
        roof=BraxCityMaterials.Surface("Warehouse zinc roof and folded flashing",new Color(.25f,.27f,.28f),.74f,.58f);
        recess=BraxCityMaterials.Surface("Warehouse unlit depth behind closed windows",new Color(.055f,.063f,.067f),1);
        ReadMasonryUV(warehouse);

        // Source modules remain at (1,1,1): their doors, mortar, mullions and
        // recess depths are not scaled to fit the inherited footprint.
        Elevation(warehouse,"Street end",new Vector3(X0,0,Z1),0,14,false,false);
        Elevation(warehouse,"Promenade loading frontage",new Vector3(X1,0,Z1),90,26.6f,true,false);
        Elevation(warehouse,"Container yard end",new Vector3(X1,0,Z0),180,14,false,true);
        Elevation(warehouse,"Western return",new Vector3(X0,0,Z0),270,26.6f,false,false);
        Corner(warehouse,"North east",new Vector3(X1,0,Z1),0);
        Corner(warehouse,"North west",new Vector3(X0,0,Z1),270);
        Corner(warehouse,"South west",new Vector3(X0,0,Z0),180);
        Corner(warehouse,"South east",new Vector3(X1,0,Z0),90);
        Roof(warehouse);

        int triangles=0,renderers=0;
        foreach(var filter in warehouse.GetComponentsInChildren<MeshFilter>())
        {
            var renderer=filter.GetComponent<MeshRenderer>();
            if(renderer && renderer.enabled && filter.sharedMesh)
            { renderers++; triangles+=filter.sharedMesh.triangles.Length/3; }
        }
        if(warehouse.GetComponentsInChildren<Collider>(true).Length!=0)
            throw new InvalidOperationException("Warehouse authoring must preserve the original collision without duplicating it.");
        Debug.Log("CITY_INDUSTRIAL_AUTHORED source="+Kit+" nativeScaleParts="+partCount
            +" croppedTrimParts="+trimCount+" enabledRenderers="+renderers+" triangles="+triangles
            +" originalCollisionRetained=true interiorBuilt=false visualReview=pending mobileReview=pending");
    }

    static Transform Group(string name,Transform parent,Vector3 at,float yaw)
    {
        var group=new GameObject(name).transform;group.SetParent(parent,false);
        group.localPosition=at;group.localRotation=Quaternion.Euler(0,yaw,0);return group;
    }
    static Transform Part(string name,Transform parent,Vector3 at)
    {
        partCount++;
        return BraxCityArchitecture.Part(Kit,name,parent,at,Quaternion.identity,Vector3.one);
    }

    static void ReadMasonryUV(Transform parent)
    {
        // The audited plain wall is exactly planar, 3 x 3 m, with affine UVs
        // (source glTF maximum fitting error 2.04e-7). Small masonry infill is
        // tessellated at <=0.5 m and samples this same map without stretching.
        var probe=Part("wall_standard_standard_01",parent,Vector3.zero);
        try
        {
            var filters=probe.GetComponentsInChildren<MeshFilter>(true);
            if(filters.Length!=1) throw new InvalidOperationException("Factory plain-wall source hierarchy changed.");
            var filter=filters[0];var source=filter.sharedMesh;
            var points=source.vertices;var uv=source.uv;var bounds=source.bounds;
            if(uv.Length!=points.Length || source.subMeshCount!=1 || bounds.size.z>.0001f
                || Mathf.Abs(bounds.min.x)>.0001f || Mathf.Abs(bounds.max.x-3)>.0001f
                || Mathf.Abs(bounds.min.y)>.0001f || Mathf.Abs(bounds.max.y-3)>.0001f)
                throw new InvalidOperationException("Factory masonry source is no longer the audited metre-scale panel.");
            int a=0,b=-1,c=-1;
            for(int i=1;i<points.Length;i++) if((points[i]-points[a]).sqrMagnitude>.001f){b=i;break;}
            for(int i=1;i<points.Length&&b>=0;i++)
                if(Vector3.Cross(points[b]-points[a],points[i]-points[a]).sqrMagnitude>.001f){c=i;break;}
            if(c<0) throw new InvalidOperationException("Factory masonry has no usable UV basis.");
            var d1=new Vector2(points[b].x-points[a].x,points[b].y-points[a].y);
            var d2=new Vector2(points[c].x-points[a].x,points[c].y-points[a].y);
            float det=d1.x*d2.y-d1.y*d2.x;
            Func<Vector2,Vector2> map=p=>{
                var d=p-new Vector2(points[a].x,points[a].y);
                float s=(d.x*d2.y-d.y*d2.x)/det,t=(d1.x*d.y-d1.y*d.x)/det;
                return uv[a]+s*(uv[b]-uv[a])+t*(uv[c]-uv[a]);
            };
            foreach(int i in Enumerable.Range(0,points.Length))
                if((map(new Vector2(points[i].x,points[i].y))-uv[i]).sqrMagnitude>1e-10f)
                    throw new InvalidOperationException("Factory brick UVs are no longer affine; refuse texture stretching.");
            uvOrigin=map(Vector2.zero);uvX=map(Vector2.right)-uvOrigin;uvY=map(Vector2.up)-uvOrigin;
            masonry=filter.GetComponent<MeshRenderer>().sharedMaterial;
        }
        finally {UnityEngine.Object.DestroyImmediate(probe.gameObject);}
    }

    static Bay Window(float x,string size,string variant)
    {return new Bay(x,"wall_window_centered_"+size+"_"+variant,"window_centered_"+size+"_"+variant);}
    static Bay[] RegularBays(float width,int storey,bool serviceEnd)
    {
        int count=Mathf.FloorToInt(width/3);float start=(width-count*3)*.5f;
        var result=new Bay[count];
        for(int i=0;i<count;i++)
        {
            string size=storey==2?"large":"medium",variant=storey==2?"03":"01";
            result[i]=Window(start+i*3,size,variant);
        }
        if(serviceEnd && storey==0)
            result[1]=new Bay(start+3,"wall_door_recessed_small_01","door_recessed_small_01","dado_door_recessed_small_01");
        return result;
    }
    static Bay[] LoadingBays()
    {
        // Exact inherited bay centres z=9.5 and z=.5, viewed from the promenade.
        // The verified kit supplies closed 2.74 m high doors; they are not
        // stretched to mimic the old 4.2 m painted rectangles.
        return new[]{Window(.4f,"medium","01"),Window(4.2f,"medium","01"),
            new Bay(8.1f,"wall_door_garage_door_01","door_garage_door_01","dado_garage_door_01",6),
            Window(14.1f,"medium","01"),
            new Bay(17.1f,"wall_door_garage_door_01","door_garage_door_01","dado_garage_door_01",6),
            Window(23.3f,"medium","01")};
    }

    static void Elevation(Transform parent,string name,Vector3 at,float yaw,float width,bool loading,bool serviceEnd)
    {
        var face=Group(name,parent,at,yaw);
        for(int storey=0;storey<3;storey++)
        {
            var row=Group("Factory storey "+(storey+1),face,new Vector3(0,Floor+storey*Storey,0),0);
            var bays=loading && storey==0?LoadingBays():RegularBays(width,storey,serviceEnd);
            float cursor=0;
            foreach(var bay in bays)
            {
                if(bay.x<cursor-.001f || bay.x+bay.width>width+.001f)
                    throw new InvalidOperationException("Overlapping warehouse facade modules: "+name);
                if(bay.x>cursor+.001f) MasonryPanel("Full-height brick infill",row,cursor,0,bay.x-cursor,3);
                Part(bay.wall,row,new Vector3(bay.x,0,0));
                Part(bay.detail,row,new Vector3(bay.x,0,0));
                if(storey==0)Part(bay.dado,row,new Vector3(bay.x,0,0));
                cursor=bay.x+bay.width;
            }
            if(cursor<width-.001f)MasonryPanel("Full-height brick end",row,cursor,0,width-cursor,3);
            float bandHeight=storey==2?CrownY-(Floor+storey*Storey+3):.5f;
            MasonryPanel("Brick spandrel and parapet",row,0,3,width,bandHeight);
            Course(row,storey==2?"crown_standard_standard_01":"cornice02_standard_standard_01",
                width,storey==2?CrownY-(Floor+storey*Storey):3.08f);
            // A recessed surface gives transparent source glazing actual depth.
            // It is not an accessible/finished warehouse interior.
            BraxCityArchitecture.Box("Closed warehouse window depth",row,new Vector3(width*.5f,1.5f,-.58f),new Vector3(width,3,.06f),recess);
            foreach(float x in bays.SelectMany(b=>new[]{b.x,b.x+b.width}).Distinct())
            {
                if(x<.25f || x>width-.25f)continue;
                Part("wall_pier_standard_01",row,new Vector3(x,0,.008f));
                if(storey==0)Part("dado_pier_pedestal_01",row,new Vector3(x,0,.008f));
                Part(storey==2?"crown_pier_pedestal_01":"cornice02_pier_standard_01",row,
                    new Vector3(x,storey==2?CrownY-(Floor+storey*Storey):3.08f,.008f));
            }
            BraxCityArchitecture.Batch(row);
        }
        var finish=Group("Warehouse ground seam and drainage",face,Vector3.zero,0);
        MasonryPanel("Ground contact course",finish,0,0,width,Floor);
        Downpipe(finish,.23f);Downpipe(finish,width-.23f);
        if(loading)
        {
            BraxCityArchitecture.Box("Port warehouse original title panel",finish,new Vector3(15.6f,3.4f,.14f),new Vector3(7.6f,.43f,.10f),metal);
            BraxCityArchitecture.Sign(finish,"MAGAZYN PORTOWY 3",new Vector3(15.6f,3.4f,.201f),.28f,new Color(.86f,.84f,.78f));
            foreach(var pair in new[]{new KeyValuePair<float,string>(11.1f,"01"),new KeyValuePair<float,string>(20.1f,"02")})
            {
                BraxCityArchitecture.Box("Loading bay number plate",finish,new Vector3(pair.Key,3.4f,.14f),new Vector3(.64f,.43f,.1f),metal);
                BraxCityArchitecture.Sign(finish,pair.Value,new Vector3(pair.Key,3.4f,.201f),.28f,Color.white);
            }
        }
        BraxCityArchitecture.Batch(finish);
    }

    static void MasonryPanel(string name,Transform parent,float x,float y,float width,float height)
    {
        if(width<=0 || height<=0) return;
        var data=new BraxCitySystemMesh();int nx=Mathf.Max(1,Mathf.CeilToInt(width/.5f)),ny=Mathf.Max(1,Mathf.CeilToInt(height/.5f));
        for(int ix=0;ix<nx;ix++) for(int iy=0;iy<ny;iy++)
        {
            float xa=x+width*ix/nx,xb=x+width*(ix+1)/nx;
            float ya=y+height*iy/ny,yb=y+height*(iy+1)/ny;
            data.Quad(new Vector3(xa,ya,0),new Vector3(xb,ya,0),new Vector3(xb,yb,0),new Vector3(xa,yb,0));
        }
        var obj=BraxCityArchitecture.MeshObject(name,parent,data,masonry);
        var mesh=obj.GetComponent<MeshFilter>().sharedMesh;
        mesh.uv=mesh.vertices.Select(p=>uvOrigin+uvX*p.x+uvY*p.y).ToArray();
        mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);
    }

    static void Course(Transform parent,string module,float width,float y)
    {
        for(float x=0;x<width-.001f;x+=3)
        {
            var part=Part(module,parent,new Vector3(x,y,.004f));float available=width-x;
            if(available>=3-.001f)continue;
            // Crop a derivative at the end instead of scaling the moulding or
            // its original texture. Face transforms are cardinal (0/90/180/270).
            var local=Region(available,4,-2,2,-2,2);var min=Vector3.positiveInfinity;var max=Vector3.negativeInfinity;
            for(int i=0;i<8;i++)
            {
                var p=new Vector3((i&1)==0?local.min.x:local.max.x,(i&2)==0?local.min.y:local.max.y,(i&4)==0?local.min.z:local.max.z);
                p=part.TransformPoint(p);min=Vector3.Min(min,p);max=Vector3.Max(max,p);
            }
            var volume=new Bounds();volume.SetMinMax(min,max);
            int altered=BraxCityMeshCut.Subtract(part,volume,Output,"native_scale_end_"+(trimCount++));
            if(altered==0)throw new InvalidOperationException("Warehouse end trim was not clipped to the inherited footprint.");
            foreach(var filter in part.GetComponentsInChildren<MeshFilter>())
                if(AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith(Output+"/",StringComparison.Ordinal))
                {filter.sharedMesh.RecalculateTangents();EditorUtility.SetDirty(filter.sharedMesh);}
        }
    }

    static void Corner(Transform parent,string name,Vector3 at,float yaw)
    {
        var corner=Group("Warehouse bonded corner "+name,parent,at,yaw);
        for(int storey=0;storey<3;storey++)
        {
            float y=Floor+storey*Storey;
            Part("wall_pier_corner_01",corner,new Vector3(0,y,0));
            if(storey==0)Part("dado_pier_corner_01",corner,new Vector3(0,y,0));
            Part(storey==2?"crown_pier_corner_01":"cornice02_pier_corner_01",corner,
                new Vector3(0,storey==2?CrownY:y+3.08f,0));
        }
        BraxCityArchitecture.Batch(corner);
    }

    static void Downpipe(Transform parent,float x)
    {
        var data=new BraxCitySystemMesh();
        data.Tube(new Vector3(x,.26f,.18f),new Vector3(x,10.42f,.18f),.052f,12);
        data.Tube(new Vector3(x,10.42f,.18f),new Vector3(x,10.58f,-.02f),.052f,12);
        data.Tube(new Vector3(x,.26f,.18f),new Vector3(x,.18f,.27f),.052f,12);
        for(float y=1;y<10.5f;y+=2)
        {
            data.Tube(new Vector3(x,y-.035f,.18f),new Vector3(x,y+.035f,.18f),.061f,12);
            data.Box(new Vector3(x,y,.09f),new Vector3(.15f,.04f,.18f));
        }
        BraxCityArchitecture.MeshObject("Galvanised downpipe with joints and wall clips",parent,data,metal);
    }

    static void Roof(Transform parent)
    {
        var top=Group("Warehouse roof and flashing",parent,Vector3.zero,0);
        // A recessed, closed roof, with physical standing seams and folded edges.
        // No rooftop interior, loading volume or future mission module is added.
        BraxCityArchitecture.Box("Warehouse weatherproof roof",top,new Vector3(59,10.48f,7.3f),new Vector3(13.6f,.14f,26.2f),roof);
        var seams=new BraxCitySystemMesh();
        for(float x=52.65f;x<65.5f;x+=.55f)
            seams.Box(new Vector3(x,10.575f,7.3f),new Vector3(.018f,.06f,26.05f));
        seams.Box(new Vector3(52.19f,10.57f,7.3f),new Vector3(.1f,.18f,26.25f));
        seams.Box(new Vector3(65.81f,10.57f,7.3f),new Vector3(.1f,.18f,26.25f));
        seams.Box(new Vector3(59,10.57f,-5.81f),new Vector3(13.7f,.18f,.1f));
        seams.Box(new Vector3(59,10.57f,20.41f),new Vector3(13.7f,.18f,.1f));
        BraxCityArchitecture.MeshObject("Roof folded edge and standing seams",top,seams,roof);
        BraxCityArchitecture.Batch(top);
    }
}
