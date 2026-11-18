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
    public const string Fale = "Assets/Source/Claude/Miasto/woda/fale_normal.png";      // unity-tools/claude/woda_fale.py fale
    public const string Kaustyki = "Assets/Source/Claude/Miasto/woda/kaustyki.png";     // unity-tools/claude/woda_fale.py kaustyki
    const string Output = "Assets/PlayableGenerated/CityRebuildWater";
    const string RootName = "Braxhorn harbour water";

    [Serializable]
    sealed class WaterReport
    {
        public string sourceRenderer = ReplacedOriginalRenderer;
        public string sourceDisposition = "Integrator disables this renderer; original glTF and its material remain archived";
        public string normalSource = "Broad analytic swell plus two tiles of an FFT wind-wave normal map (Phillips spectrum, unity-tools/claude/woda_fale.py)";
        public string reflection = "The city sky panorama itself (same texture, rotation and exposure as the skybox); old cubemap/gradient only without it";
        public string seabed = "Depth texture from the beach and island terrain functions and a harbour model (BraxWodaDno); refracted sand with caustics, RGB attenuation per metre";
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
        BraxZapisAssetu.UsunFolder(Output,"woda portu");
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
        material.SetFloat("_ReflectionStrength",1f);
        material.SetFloat("_ReflectionMip",1.5f);
        material.SetFloat("_RippleStrength",1f);
        material.SetFloat("_SwellStrength",1);
        // MIASTO R1 18.11 (właściciel: „nie możemy zrobić bardziej realistycznej wody?”, „woda… nie pasek… widać nic”): fale
        // wiatrowe z widma (mapa normalnych FFT), dno pod wodą (tekstura głębokości z terenu plaży, wyspy i modelu portu), piasek
        // dna widoczny przez płytką wodę z kaustykami, pochłanianie światła w wodzie; niebo w odbiciu ustawia horyzont (UstawNiebo)
        var dno=BraxWodaDno.Tekstura(out float plytkoM2);
        BraxZapisAssetu.Utworz(dno,Output+"/dno_glebokosc.asset");
        material.SetTexture("_DnoTex",dno);
        material.SetVector("_DnoProstokat",BraxWodaDno.Prostokat);
        material.SetFloat("_DnoMax",BraxWodaDno.MaxGlebokosc);
        var fale=TeksturaWody(Fale,false);var kaustyki=TeksturaWody(Kaustyki,false);
        var piasek=BraxNabrzeze.TexPlik(BraxNabrzeze.Piaski+"damp_sand_diff_2k.jpg",false);
        if(fale)material.SetTexture("_FaleNormal",fale);
        if(kaustyki)material.SetTexture("_Kaustyki",kaustyki);
        if(piasek)material.SetTexture("_DnoAlbedo",piasek);
        material.SetFloat("_FaleSkala",10f);material.SetFloat("_KaustykiSkala",3f);material.SetFloat("_DnoSkala",2.4f);
        material.SetColor("_DnoKolor",new Color(.80f,.75f,.67f));                      // jak mokry piasek plaży (BraxNabrzeze.Teren _KMokry)
        material.SetVector("_Pochlanianie",new Vector4(.45f,.09f,.075f,0f));            // czysta woda przybrzeżna: czerwień ginie po ~2 m
        // kolory materiału w przestrzeni liniowej projektu: Unity zamienia Color z sRGB na liniowy — wartości fizyczne (rozproszenie
        // głębokiej wody, słońce) podajemy przez .gamma, żeby shader dostał je dokładnie; piasek dna i otoczenie jak plaża / niebo (sRGB)
        material.SetColor("_DeepColor",new Color(.010f,.052f,.070f).gamma);
        material.SetColor("_SlonceKolor",new Color(1.24f,1.12f,.92f).gamma);          // słońce R1 liniowo: 1.24 × (1, .955, .875) sRGB
        material.SetColor("_Otoczenie",new Color(.60f,.68f,.80f));                     // jak niebo otoczenia R1 (BraxR1Obraz)
        Debug.Log("BRAXHORN_WODA_REALNA fale="+(fale?fale.width+"px":"BRAK")+" kaustyki="+(kaustyki?kaustyki.width+"px":"BRAK")+" piasek_dna="+(piasek?piasek.name:"BRAK")
                  +" dno="+BraxWodaDno.TexW+"x"+BraxWodaDno.TexH+" (płytko < 3 m: "+Mathf.RoundToInt(plytkoM2)+" m²)");
        // Read existing scene daylight without modifying the gym or global sky.
        Color horizon=RenderSettings.ambientEquatorColor,zenith=RenderSettings.ambientSkyColor;
        if(horizon.maxColorComponent>.01f)material.SetColor("_SkyHorizon",horizon);
        if(zenith.maxColorComponent>.01f)material.SetColor("_SkyZenith",Color.Lerp(zenith,new Color(.22f,.36f,.49f),.35f));
        BraxZapisAssetu.Utworz(material,Output+"/HarbourWater.mat");
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

    /// <summary>Tekstura danych wody (liniowa, mipmapy, powtarzana, filtr anizotropowy).</summary>
    static Texture2D TeksturaWody(string path,bool srgb)
    {
        if(!File.Exists(path)){Debug.LogWarning("DO POPRAWKI: BRAXHORN_WODA_REALNA brak "+path);return null;}
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        if(AssetImporter.GetAtPath(path) is TextureImporter imp&&(imp.sRGBTexture!=srgb||!imp.mipmapEnabled||imp.wrapMode!=TextureWrapMode.Repeat||imp.anisoLevel<4||imp.textureType!=TextureImporterType.Default))
        {
            imp.textureType=TextureImporterType.Default;imp.sRGBTexture=srgb;imp.mipmapEnabled=true;imp.wrapMode=TextureWrapMode.Repeat;
            imp.filterMode=FilterMode.Trilinear;imp.anisoLevel=4;imp.alphaSource=TextureImporterAlphaSource.None;imp.maxTextureSize=512;
            imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    /// <summary>Niebo w odbiciu wody = to samo zdjęcie 360°, obrót i ekspozycja co niebo miasta (materiał nieba z horyzontu).
    /// Wszystkie materiały wody (port, morze za kamerą, akwen wyspy). Zwraca liczbę ustawionych materiałów.</summary>
    public static int UstawNiebo(Material niebo)
    {
        if(!niebo||!niebo.HasProperty("_NieboTex")||!niebo.GetTexture("_NieboTex"))return 0;
        var mats=new HashSet<Material>();
        var m0=AssetDatabase.LoadAssetAtPath<Material>(Output+"/HarbourWater.mat");if(m0)mats.Add(m0);
        foreach(var mz in UnityEngine.Object.FindObjectsByType<BraxMorzeZaKamera>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(mz&&mz.woda)mats.Add(mz.woda);
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            foreach(var m in r.sharedMaterials)if(m&&m.shader&&m.shader.name=="Braxhorn/HarbourWaterMobile")mats.Add(m);
        int n=0;
        foreach(var m in mats)
        {
            if(!m.HasProperty("_NieboTex"))continue;
            m.SetTexture("_NieboTex",niebo.GetTexture("_NieboTex"));
            m.SetFloat("_NieboObrot",niebo.GetFloat("_NieboObrot"));
            m.SetFloat("_NieboEkspozycja",niebo.HasProperty("_NieboEkspozycja")?niebo.GetFloat("_NieboEkspozycja"):1f);
            m.SetFloat("_NieboJest",1f);
            // daleko woda = morze nieba (tryb zdjęcia: _FotoHoryzont 1) — przejście 700…1500 m, przed końcem siatki morza (1.6 km)
            bool foto=niebo.HasProperty("_FotoHoryzont")&&niebo.GetFloat("_FotoHoryzont")>.5f;
            if(foto&&niebo.HasProperty("_MorzeKolor"))m.SetColor("_DalekoKolor",niebo.GetColor("_MorzeKolor"));
            m.SetFloat("_DalekoOd",700f);m.SetFloat("_DalekoDo",foto?1500f:0f);
            EditorUtility.SetDirty(m);n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("BRAXHORN_WODA_NIEBO odbicie = panorama "+niebo.GetTexture("_NieboTex").name+", obrót "+niebo.GetFloat("_NieboObrot").ToString("F3")+" rad, materiały wody "+n);
        return n;
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
        BraxZapisAssetu.Utworz(mesh,Output+"/WaterPatch_"+patches.Count.ToString("D2")+".asset");
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
