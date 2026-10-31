using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>City-only harbour water; no camera, global setting or source asset is changed.</summary>
public static class BraxCityWaterBuild
{
    public const int ReplacedOriginalMesh = 130;
    public const string ReplacedOriginalRenderer = "CityMesh_130";
    public const float WaterY = -1.7f;
    public const float ShoreX = 84f;
    public static readonly Rect Footprint = new Rect(84,-340,800,800);
    // 31.10 (decyzja właściciela: delikatne fale na całym morzu, pływanie skuterem, wyspa): siatka falująca
    // obejmuje zatokę, plażę, marinę i wyspę (x 84..560, z −120..260), krok 4 m = 8 płatów po 60×24 pól.
    public static readonly Rect MovingMeshFootprint = new Rect(84,-120,476,380);
    const string Template = "Assets/Editor/CityRebuild/Environment/Water/HarbourWater.shader.txt";
    const string Output = "Assets/PlayableGenerated/CityRebuildWater";
    const string RootName = "Braxhorn harbour water";

    [Serializable]
    sealed class WaterReport
    {
        public string sourceRenderer = ReplacedOriginalRenderer;
        public string sourceDisposition = "Integrator disables this renderer; original glTF and its material remain archived";
        public string normalSource = "Analytic derivatives of calm water waves; no verified water normal map was available";
        public string reflection = "Existing default sky cubemap with a soft explicit mip; sky gradient if the cubemap is black";
        public string validation = "Generated topology, bounds and shader import checked; WebGL compilation and real player review still required";
        public float meanWaterY = WaterY, shorelineX = ShoreX, maximumSwellMetres = .16f;
        public int patches, vertices, triangles, addedCameras, addedLights, addedReflectionProbes, addedColliders;
        public double coveredArea;
        public bool visualAccepted, physicalPhoneMeasured;
    }

    /// <summary>Call after city geometry and static lighting, before saving the scene.</summary>
    public static void Apply(Transform parent)
    {
        if(!parent)throw new ArgumentNullException(nameof(parent));
        if(Vector3.Distance(parent.lossyScale,Vector3.one)>.001f || Quaternion.Angle(parent.rotation,Quaternion.identity)>.01f)
            throw new InvalidOperationException("Water parent must preserve metre scale and Unity axes");
        if(!File.Exists(Template))throw new FileNotFoundException("Durable harbour water shader source is missing",Template);
        var previous=parent.Find(RootName);
        if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        if(AssetDatabase.IsValidFolder(Output))AssetDatabase.DeleteAsset(Output);
        Directory.CreateDirectory(Output);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string shaderPath=Output+"/HarbourWater.shader";
        File.Copy(Template,shaderPath,true);
        AssetDatabase.ImportAsset(shaderPath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        var shader=AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        if(!shader)throw new InvalidOperationException("Generated harbour water shader did not import");
        if(ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Harbour water shader import failed: "+string.Join("\n",Array.ConvertAll(ShaderUtil.GetShaderMessages(shader),m=>m.message)));
        var material=new Material(shader){name="Calm harbour water",globalIlluminationFlags=MaterialGlobalIlluminationFlags.None};
        material.SetVector("_NearRectangle",new Vector4(MovingMeshFootprint.xMin,MovingMeshFootprint.yMin,MovingMeshFootprint.xMax,MovingMeshFootprint.yMax));
        material.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        material.SetFloat("_ReflectionStrength",.58f);
        material.SetFloat("_ReflectionMip",3.2f);
        material.SetFloat("_RippleStrength",.8f);
        material.SetFloat("_SwellStrength",1);
        // Read existing scene daylight without modifying the gym or global sky.
        Color horizon=RenderSettings.ambientEquatorColor,zenith=RenderSettings.ambientSkyColor;
        if(horizon.maxColorComponent>.01f)material.SetColor("_SkyHorizon",horizon);
        if(zenith.maxColorComponent>.01f)material.SetColor("_SkyZenith",Color.Lerp(zenith,new Color(.22f,.36f,.49f),.35f));
        AssetDatabase.CreateAsset(material,Output+"/HarbourWater.mat");
        var root=new GameObject(RootName).transform;root.SetParent(parent,false);root.position=Vector3.zero;
        var patches=new List<Rect>();
        // Eight 32x64 m near-quay patches, two-metre grid. A shared analytic
        // function moves matching boundary vertices by the same amount.
        for(int x=0;x<2;x++)for(int z=0;z<4;z++)
            Patch(root,new Rect(84+x*238,-120+z*95,238,95),4,material,patches);
        // The original water extent is preserved. Outside the moving rectangle,
        // flat low-cost patches have animated, distance-filtered surface normals.
        Patch(root,new Rect(84,-340,476,220),800,material,patches);
        Patch(root,new Rect(84,260,476,200),800,material,patches);
        Patch(root,new Rect(560,-340,324,800),800,material,patches);
        var report=Validate(root,patches,material);
        File.WriteAllText(Output+"/water-review.json",JsonUtility.ToJson(report,true));
        AssetDatabase.ImportAsset(Output+"/water-review.json");
        AssetDatabase.SaveAssets();
        Debug.Log("BRAX_HARBOUR_WATER "+JsonUtility.ToJson(report));
    }

    static void Patch(Transform root,Rect bounds,float spacing,Material material,List<Rect> patches)
    {
        int nx=Mathf.Max(1,Mathf.CeilToInt(bounds.width/spacing)),nz=Mathf.Max(1,Mathf.CeilToInt(bounds.height/spacing));
        var vertices=new Vector3[(nx+1)*(nz+1)];var normals=new Vector3[vertices.Length];var uv=new Vector2[vertices.Length];
        var triangles=new int[nx*nz*6];
        for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
        {
            int index=z*(nx+1)+x;
            vertices[index]=new Vector3(Mathf.Lerp(bounds.xMin,bounds.xMax,(float)x/nx),WaterY,Mathf.Lerp(bounds.yMin,bounds.yMax,(float)z/nz));
            normals[index]=Vector3.up;uv[index]=new Vector2(vertices[index].x,vertices[index].z);
        }
        int ti=0;
        for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
        {
            int a=z*(nx+1)+x,b=a+nx+1;
            triangles[ti++]=a;triangles[ti++]=b;triangles[ti++]=a+1;
            triangles[ti++]=a+1;triangles[ti++]=b;triangles[ti++]=b+1;
        }
        string name="Harbour water patch "+patches.Count.ToString("D2");
        var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();
        var displacedBounds=mesh.bounds;displacedBounds.Expand(new Vector3(0,.1f,0));mesh.bounds=displacedBounds;
        AssetDatabase.CreateAsset(mesh,Output+"/WaterPatch_"+patches.Count.ToString("D2")+".asset");
        var obj=new GameObject(name);obj.transform.SetParent(root,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        renderer.lightProbeUsage=LightProbeUsage.Off;
        // Off still uses Unity's default sky reflection and avoids local
        // reflection-probe selection/blending. No new probe is introduced.
        renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        GameObjectUtility.SetStaticEditorFlags(obj,StaticEditorFlags.OccludeeStatic);
        patches.Add(bounds);
    }

    static WaterReport Validate(Transform root,List<Rect> patches,Material material)
    {
        var report=new WaterReport{patches=patches.Count};
        if(patches.Count!=11)throw new InvalidOperationException("Unexpected harbour water patch count");
        for(int i=0;i<patches.Count;i++)for(int j=i+1;j<patches.Count;j++)
        {
            float x=Mathf.Min(patches[i].xMax,patches[j].xMax)-Mathf.Max(patches[i].xMin,patches[j].xMin);
            float z=Mathf.Min(patches[i].yMax,patches[j].yMax)-Mathf.Max(patches[i].yMin,patches[j].yMin);
            if(x>.0001f && z>.0001f)throw new InvalidOperationException("Overlapping harbour water patches");
        }
        foreach(var f in root.GetComponentsInChildren<MeshFilter>())
        {
            if(f.GetComponent<MeshRenderer>().sharedMaterial!=material)throw new InvalidOperationException("Unexpected water material");
            var vertices=f.sharedMesh.vertices;var triangles=f.sharedMesh.triangles;
            report.vertices+=vertices.Length;report.triangles+=triangles.Length/3;
            foreach(var v in vertices)
                if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z)
                    ||Mathf.Abs(v.y-WaterY)>.0001f||v.x<Footprint.xMin-.0001f||v.x>Footprint.xMax+.0001f
                    ||v.z<Footprint.yMin-.0001f||v.z>Footprint.yMax+.0001f)
                    throw new InvalidOperationException("Water vertex changed the source extent or mean height");
            for(int t=0;t<triangles.Length;t+=3)
            {
                Vector3 cross=Vector3.Cross(vertices[triangles[t+1]]-vertices[triangles[t]],vertices[triangles[t+2]]-vertices[triangles[t]]);
                if(cross.y<=0)throw new InvalidOperationException("Inverted or degenerate water triangle");
                report.coveredArea+=cross.y*.5;
            }
        }
        if(Math.Abs(report.coveredArea-640000)>.1)throw new InvalidOperationException("Water patches do not cover the original 800x800 metre surface exactly");
        report.addedCameras=root.GetComponentsInChildren<Camera>().Length;
        report.addedLights=root.GetComponentsInChildren<Light>().Length;
        report.addedReflectionProbes=root.GetComponentsInChildren<ReflectionProbe>().Length;
        report.addedColliders=root.GetComponentsInChildren<Collider>().Length;
        if(report.addedCameras+report.addedLights+report.addedReflectionProbes+report.addedColliders!=0)
            throw new InvalidOperationException("Harbour water added an unexpected camera, light, probe or collider");
        return report;
    }
}
