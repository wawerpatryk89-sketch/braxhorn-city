using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

// CI-only smoke test of the actual camp player and generated Legacy clips.
// No scene is saved, no APK is built, and no production controls are replaced.
public static partial class BraxObozCharacters
{
    const string PlayReviewKey="Braxhorn.Monika.PlayReview";
    const string PlayReviewRoot="Monika runtime review";
    static BraxGymPlayer reviewPlayer;
    static Camera reviewCamera;
    static Transform[] reviewBones;
    static RuntimeMotionReport runtimeReport;
    static readonly List<RuntimeMotionFrame> runtimeFrames=new List<RuntimeMotionFrame>();
    static readonly List<string> runtimeErrors=new List<string>();
    static double runtimeDeadline;
    static float runtimeStart=-1;
    static int runtimeFrame=-1,runtimeStage=-1;
    static Vector3 runtimeOrigin,pauseOrigin;
    static string runtimeOutput;
    [Serializable] sealed class RuntimeMotionFrame
    {
        public float time;public int frame;public string stage,clip;
        public Vector3 position;public MotionFrame pose;public bool paused;
    }
    [Serializable] sealed class RuntimeMotionReport
    {
        public string unity,commit,controller="BraxGymPlayer",captureMethod="cpu-baked-mesh";
        public bool playModeTested,phoneTested=false,appearanceApproved=false,complete;
        public float distance,pauseDisplacement;public RuntimeMotionFrame[] frames;public string[] errors;
    }

    public static void ReviewMonikaRuntime()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Próba wymaga izolowanej sesji CI");
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Unity już jest w Play Mode");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var manifest=ReadManifest();var spec=manifest.characters.Single(c=>c.id=="monika");
        var root=new GameObject(PlayReviewRoot);root.layer=8;root.transform.position=new Vector3(0,.08f,0);
        var visual=CreateCached(spec,manifest.worldScale,sourceReviewOnly:true);visual.transform.SetParent(root.transform,false);
        var body=root.AddComponent<CharacterController>();body.height=spec.realHeight*manifest.worldScale;
        body.radius=.27f*manifest.worldScale;body.center=Vector3.up*body.height*.5f;body.stepOffset=.25f*manifest.worldScale;body.skinWidth=.035f;
        var player=root.AddComponent<BraxGymPlayer>();player.visual=visual.transform;player.body=body;
        player.clips=visual.GetComponent<Animation>();player.bags=Array.Empty<Transform>();player.ConfigureCamera(3.5f,body.height*.62f);
        var cameraGo=new GameObject("Runtime player camera");player.view=cameraGo.AddComponent<Camera>();player.view.fieldOfView=58;cameraGo.AddComponent<AudioListener>();
        var floor=new GameObject("Temporary physics floor");var collider=floor.AddComponent<BoxCollider>();collider.size=new Vector3(100,.2f,100);floor.transform.position=Vector3.down*.1f;
        var light=new GameObject("Runtime review light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(35,-30,0);
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.5f,.5f);
        AssetDatabase.SaveAssets();
        SessionState.SetBool(PlayReviewKey,true);
        SubscribeRuntimeReview();EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    static void ResumeRuntimeReview(){if(SessionState.GetBool(PlayReviewKey,false))SubscribeRuntimeReview();}
    static void SubscribeRuntimeReview()
    {
        runtimeDeadline=EditorApplication.timeSinceStartup+180;
        EditorApplication.update-=TickRuntimeReview;EditorApplication.update+=TickRuntimeReview;
        Application.logMessageReceived-=RuntimeLog;Application.logMessageReceived+=RuntimeLog;
    }
    static void RuntimeLog(string text,string stack,LogType type)
    {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)runtimeErrors.Add(text);}
    static void TickRuntimeReview()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>runtimeDeadline)throw new TimeoutException("Play Mode Moniki: limit 180 s");
            if(!EditorApplication.isPlaying)return;
            if(!reviewPlayer)
            {
                var root=GameObject.Find(PlayReviewRoot);if(!root)throw new InvalidOperationException("Brak Moniki w scenie testowej");
                reviewPlayer=root.GetComponent<BraxGymPlayer>();
                reviewBones=reviewPlayer.visual.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("mixamorig",StringComparison.OrdinalIgnoreCase)).ToArray();
                reviewCamera=new GameObject("Runtime pose camera").AddComponent<Camera>();reviewCamera.cullingMask=1<<27;
                reviewCamera.orthographic=true;reviewCamera.nearClipPlane=.01f;reviewCamera.farClipPlane=100;
                reviewCamera.clearFlags=CameraClearFlags.SolidColor;reviewCamera.backgroundColor=new Color(.12f,.14f,.17f);
                runtimeOutput=Path.GetFullPath(Path.Combine(Application.dataPath,"../../build/MonikaRuntimeReview"));Directory.CreateDirectory(runtimeOutput);
                runtimeReport=new RuntimeMotionReport{unity=Application.unityVersion,commit=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local",playModeTested=true};
            }
            if(!reviewPlayer.ready)return;
            if(runtimeStart<0){runtimeStart=Time.time;runtimeOrigin=reviewPlayer.transform.position;Time.captureDeltaTime=1f/30f;}
            if(Time.frameCount==runtimeFrame)return;runtimeFrame=Time.frameCount;
            float time=Time.time-runtimeStart;
            int stage=time<1?0:time<3?1:time<5?2:time<6?3:time<8?4:time<9?5:time<10?6:time<12?7:8;
            if(stage==8){FinishRuntimeReview();return;}
            string[] stages={"idle","walk","run","stop","punch","pause","resume","punch-repeat"};
            if(stage!=runtimeStage)
            {
                runtimeStage=stage;
                reviewPlayer.MoveInput(stage==1?"0,0.4":stage==2?"0,1":"0,0");
                if(stage==4||stage==7)reviewPlayer.Command("punch");
                if(stage==5){pauseOrigin=reviewPlayer.transform.position;reviewPlayer.Command("pause");}
                if(stage==6){runtimeReport.pauseDisplacement=Vector3.Distance(pauseOrigin,reviewPlayer.transform.position);reviewPlayer.Command("pause");}
            }
            var active=reviewPlayer.clips.Cast<AnimationState>().Where(s=>s.enabled).OrderByDescending(s=>s.weight).FirstOrDefault();
            var position=reviewPlayer.transform.position;
            if(float.IsNaN(position.sqrMagnitude)||float.IsInfinity(position.sqrMagnitude)||position.y<-.1f)throw new InvalidOperationException("Nieprawidłowa pozycja gracza");
            runtimeFrames.Add(new RuntimeMotionFrame{time=time,frame=runtimeFrame,stage=stages[stage],clip=active?.name??"none",position=position,pose=MeasureMotion(reviewBones,time),paused=reviewPlayer.Paused});
            if(runtimeFrames.Count%3==0)
            {
                CaptureRuntimeCamera(reviewPlayer.view,Path.Combine(runtimeOutput,stages[stage]+"-game-"+runtimeFrames.Count.ToString("D4")+".png"));
                using(var snapshot=new MotionSnapshot(reviewPlayer.visual.gameObject))
                foreach(string view in new[]{"front","back","side"})CaptureMotion(snapshot.renderers,reviewCamera,view,Path.Combine(runtimeOutput,stages[stage]+"-"+view+"-"+runtimeFrames.Count.ToString("D4")+".png"));
            }
        }
        catch(Exception ex){runtimeErrors.Add(ex.GetType().Name+": "+ex.Message);FinishRuntimeReview();}
    }
    static void CaptureRuntimeCamera(Camera camera,string path)
    {
        var rt=RenderTexture.GetTemporary(1136,640,24,RenderTextureFormat.ARGB32);var active=RenderTexture.active;var target=camera.targetTexture;
        var image=new Texture2D(1136,640,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1136,640),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{camera.targetTexture=target;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);}
    }
    static void FinishRuntimeReview()
    {
        EditorApplication.update-=TickRuntimeReview;Application.logMessageReceived-=RuntimeLog;
        SessionState.SetBool(PlayReviewKey,false);Time.captureDeltaTime=0;
        if(runtimeReport==null)runtimeReport=new RuntimeMotionReport{unity=Application.unityVersion,commit=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local"};
        if(reviewPlayer)runtimeReport.distance=Vector3.Distance(runtimeOrigin,reviewPlayer.transform.position);
        foreach(string required in new[]{"Idle","Walk","Run","Punch","CombatIdle"})
            if(!runtimeFrames.Any(f=>f.clip==required))runtimeErrors.Add("Nie odtworzono "+required);
        if(runtimeReport.distance<5)runtimeErrors.Add("Za mały dystans próby ruchu");
        if(!runtimeFrames.Any(f=>f.paused)||runtimeReport.pauseDisplacement>.001f)runtimeErrors.Add("Nieprawidłowa pauza");
        runtimeReport.complete=runtimeErrors.Count==0&&runtimeFrames.Count>=100;
        runtimeReport.frames=runtimeFrames.ToArray();runtimeReport.errors=runtimeErrors.ToArray();
        if(string.IsNullOrEmpty(runtimeOutput))runtimeOutput=Path.GetFullPath(Path.Combine(Application.dataPath,"../../build/MonikaRuntimeReview"));
        Directory.CreateDirectory(runtimeOutput);File.WriteAllText(Path.Combine(runtimeOutput,"runtime-review.json"),JsonUtility.ToJson(runtimeReport,true));
        Debug.Log("BRAXHORN_MONIKA_RUNTIME complete="+runtimeReport.complete+" samples="+runtimeFrames.Count+" phone_tested=false");
        EditorApplication.Exit(runtimeReport.complete?0:1);
    }
}
