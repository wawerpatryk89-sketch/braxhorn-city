using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class BraxObozCharacters
{
    const string LifeKey = "Braxhorn.Camp.LifeReview";
    static BraxObozSceneInfo lifeScene;
    static BraxObozTrainingRoutine lifeRoutine;
    static Camera lifeCamera, lifePoseCamera;
    static string lifeOutput;
    static double lifeDeadline;
    static float lifeStart = -1, lifeSample, lifeCapture, lifePauseMove, lifePauseBag;
    static int lifeFrame = -1, lifeOrders;
    static bool lifePaused, lifeResumed;
    static BraxObozArena lifePlayerArena;
    static float lifePlayerFightAt = -1;
    static bool lifePlayerFightStarted, lifePlayerFightEnded, lifePlayerFightSuspended;
    static readonly bool[] lifePlayerArenas = new bool[3];
    static int lifeFightBagStart, lifeFightBagStrikes;
    static Vector3 lifePlayerOrigin;
    static Vector3[] lifePausePositions;
    static Quaternion[] lifePauseRotations;
    static readonly List<LifeFrame> lifeFrames = new List<LifeFrame>();
    static readonly List<string> lifeErrors = new List<string>();
    static readonly Dictionary<string,int> lifePoseCounts = new Dictionary<string,int>();
    [Serializable] sealed class LifeActor
    {
        public string id, clip;
        public int role, arrivals;
        public float travelled;
        public bool arrived, blocked, navigationOwnsPosition, navigationOwnsRotation;
        public Vector3 position;
    }
    [Serializable] sealed class LifeFrame
    {
        public float time;
        public bool paused;
        public int rounds, bagStrikes, sparringStrikes, orders;
        public int arena;
        public LifeActor[] actors;
    }
    [Serializable] sealed class LifeReport
    {
        public string unity, commit, scene = BraxObozActors.ScenePath;
        public bool complete, playModeTested, phoneTested = false, appearanceApproved = false;
        public float pauseDisplacement, pauseBagDegrees;
        public int rounds, roleChanges, bagStrikes, sparringStrikes, orders;
        public int spokenLines, voiceExchanges, contactSounds;
        public int[] arenaRounds;
        public bool[] playerSparringArenas;
        public int bagStrikesDuringPlayerSparring;
        public bool playerSparringStarted, playerSparringEnded, routineSuspendedForPlayer;
        public LifeFrame[] frames;
        public string[] errors;
    }
    // Uses the production generator, controllers, colliders, navigation and interaction API.
    // Round length is accelerated; the test player is positioned on a mat for the
    // existing sparring interaction. NPC navigation remains physical. Never publishes APK.
    public static void ReviewCampLife()
    {
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Próba obozu wymaga izolowanej sesji batch");
        // Match BraxObozBuild.Configure; a clean diagnostic editor defaults to Gamma.
        PlayerSettings.colorSpace=ColorSpace.Linear;
        Directory.CreateDirectory(Generated); AssetDatabase.Refresh();
        var empty = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(empty, Generated + "/LifeReviewBootstrap.unity");
        BraxObozEnvironment.Prepare(); BraxObozActors.Assemble();
        EditorSceneManager.OpenScene(BraxObozActors.ScenePath, OpenSceneMode.Single);
        var scene = Object.FindFirstObjectByType<BraxObozSceneInfo>();
        var structural = BraxObozValidation.Check(scene);
        if (!structural.structuralPassed) throw new InvalidOperationException(string.Join("; ", structural.structuralErrors));
        ReviewFinalPoses(scene);
        // In-memory test value; production asset retains 45 seconds.
        scene.GetComponent<BraxObozTrainingRoutine>().roundSeconds = 8;
        SessionState.SetBool(LifeKey, true); SubscribeLifeReview(); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    static void ResumeLifeReview() { if (SessionState.GetBool(LifeKey, false)) SubscribeLifeReview(); }
    static void SubscribeLifeReview()
    {
        lifeDeadline = EditorApplication.timeSinceStartup + 1200;
        EditorApplication.update -= TickLifeReview; EditorApplication.update += TickLifeReview;
        Application.logMessageReceived -= LifeLog; Application.logMessageReceived += LifeLog;
    }
    static void LifeLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert ||
            (type == LogType.Warning && message.Contains("trasa NPC"))) lifeErrors.Add(message);
    }
    static void TickLifeReview()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > lifeDeadline) throw new TimeoutException("Próba życia obozu: limit 1200 s");
            if (!EditorApplication.isPlaying) return;
            if (!lifeScene)
            {
                lifeScene = Object.FindFirstObjectByType<BraxObozSceneInfo>();
                if (!lifeScene) throw new InvalidOperationException("Brak sceny obozu w Play Mode");
                lifeRoutine = lifeScene.GetComponent<BraxObozTrainingRoutine>();
                lifeOutput = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/CampLifeReview"));
                Directory.CreateDirectory(lifeOutput);
                lifeCamera = new GameObject("Camp review camera").AddComponent<Camera>();
                lifeCamera.nearClipPlane = .1f; lifeCamera.farClipPlane = 400; lifeCamera.fieldOfView = 55;
                lifeCamera.transform.position = new Vector3(34, 30, -48);
                lifeCamera.transform.LookAt(new Vector3(0, 0, -22));
                lifePoseCamera = new GameObject("Camp pose review camera").AddComponent<Camera>();
                lifePoseCamera.cullingMask = 1 << 27; lifePoseCamera.orthographic = true;
                lifePoseCamera.nearClipPlane = .01f; lifePoseCamera.farClipPlane = 300;
                lifePoseCamera.clearFlags = CameraClearFlags.SolidColor;
                lifePoseCamera.backgroundColor = new Color(.12f,.14f,.17f);
                var bus=lifeScene.GetComponentsInChildren<Transform>().Single(t=>t.name==BraxObozParkedBus.Name);
                var camera=new GameObject("Parked bus review camera").AddComponent<Camera>();
                camera.nearClipPlane=.1f;camera.farClipPlane=400;camera.fieldOfView=55;
                camera.transform.position=bus.position+new Vector3(12,7,16);camera.transform.LookAt(bus.position+Vector3.up*2);
                CaptureRuntimeCamera(camera,Path.Combine(lifeOutput,"parked-bus.png"));Object.DestroyImmediate(camera.gameObject);
            }
            if (!lifeScene.player.ready) return;
            if (lifeStart < 0) { lifeStart = Time.time; Time.captureDeltaTime = .05f; }
            if (lifeFrame == Time.frameCount) return; lifeFrame = Time.frameCount;
            float time = Time.time - lifeStart;
            if (!lifeRoutine.Running) throw new InvalidOperationException("Plan zajęć nie działa");
            if (time >= 25 && !lifePaused)
            {
                lifePaused = true;
                lifePausePositions = lifeRoutine.participants.Select(n => n.transform.position).ToArray();
                lifePauseRotations = lifeRoutine.trainingBags.Select(b => b.transform.rotation).ToArray();
                lifeScene.player.Command("pause");
            }
            if (time >= 27 && !lifeResumed)
            {
                lifeResumed = true;
                if(!lifeRoutine.audio.Paused)lifeErrors.Add("Głosy nie przyjęły pauzy");
                for (int i=0;i<6;i++) lifePauseMove = Mathf.Max(lifePauseMove, Vector3.Distance(lifePausePositions[i],lifeRoutine.participants[i].transform.position));
                for (int i=0;i<2;i++) lifePauseBag = Mathf.Max(lifePauseBag, Quaternion.Angle(lifePauseRotations[i],lifeRoutine.trainingBags[i].transform.rotation));
                lifeScene.player.Command("pause");
            }
            if (time >= 80 && lifeOrders == 0 || time >= 120 && lifeOrders == 1)
            {
                var npc = lifeRoutine.participants.First(n => lifeRoutine.RoleOf(n) >= 4);
                if (!npc.Wykonaj(npc.transform.position + npc.transform.forward * 1.5f))
                    throw new InvalidOperationException("Istniejące API AKCJA nie przyjęło zadania dla " + npc.characterId);
                lifeOrders++;
            }
            if (time >= 160 && !lifePlayerArena && !lifePlayerArenas[lifeRoutine.ArenaIndex])
            {
                var arena=lifeRoutine.CurrentArena;
                var pair=lifeRoutine.participants.Where(n=>lifeRoutine.RoleOf(n)<2).ToArray();
                bool bagReady=lifeRoutine.participants.Any(n=>lifeRoutine.RoleOf(n)>=2&&lifeRoutine.RoleOf(n)<4&&n.AtDestination);
                if(bagReady && pair.Length==2 && pair.All(n=>n.AtDestination&&!n.Reacting))
                {
                    lifePlayerOrigin=lifeScene.player.transform.position;
                    var point=arena.transform.position+new Vector3(0,.70f,1.8f)*BraxObozSceneInfo.WorldScale;
                    lifeScene.player.body.enabled=false;lifeScene.player.transform.position=point;lifeScene.player.body.enabled=true;
                    lifeScene.player.MoveInput("0,0");
                    // Exercise the actual AKCJA registry, not just the arena directly.
                    if(!BraxInterakcje.Wykonaj(point)||!arena.Active)throw new InvalidOperationException("Monika nie może dołączyć do pary w "+arena.label);
                    int orders=lifeRoutine.Orders;
                    if(pair[0].Wykonaj(point)||lifeRoutine.OrderNext(pair[0])||lifeRoutine.Orders!=orders)
                        throw new InvalidOperationException("Polecenie treningowe przerywa sparing Moniki");
                    lifePlayerArena=arena;lifePlayerFightStarted=true;lifePlayerFightAt=time;
                    lifeFightBagStart=lifeRoutine.BagStrikes;
                }
            }
            if(lifePlayerArena)
            {
                lifePlayerFightSuspended|=lifeRoutine.SuspendedForPlayer;
                if(time-lifePlayerFightAt>=8)
                {
                    if(!lifePlayerArena.Active||!BraxInterakcje.Wykonaj(lifeScene.player.transform.position)||lifePlayerArena.Active)
                        throw new InvalidOperationException("Nie zakończono sparingu Moniki istniejącą AKCJĄ");
                    lifePlayerArenas[lifeRoutine.ArenaIndex]=true;
                    int hits=lifeRoutine.BagStrikes-lifeFightBagStart;
                    if(hits<1)throw new InvalidOperationException("Trening worków zatrzymał się podczas sparingu Moniki");
                    lifeFightBagStrikes+=hits;
                    lifePlayerFightEnded=true;
                    lifePlayerArena=null;
                    lifeScene.player.body.enabled=false;lifeScene.player.transform.position=lifePlayerOrigin;lifeScene.player.body.enabled=true;
                }
            }
            if (time >= lifeSample)
            {
                lifeSample = time + .25f;
                lifeFrames.Add(new LifeFrame { time = time, paused = lifeScene.player.Paused,
                    rounds = lifeRoutine.CompletedRounds, bagStrikes = lifeRoutine.BagStrikes,
                    sparringStrikes = lifeRoutine.SparringStrikes, orders = lifeRoutine.Orders, arena=lifeRoutine.ArenaIndex,
                    actors = lifeRoutine.participants.Select(n => new LifeActor {
                        id = n.characterId, clip = ActiveLifeClip(n), role = lifeRoutine.RoleOf(n),
                        arrivals = n.Arrivals, travelled = n.Travelled, arrived = n.AtDestination,
                        blocked = n.NavigationBlocked, position = n.transform.position,
                        navigationOwnsPosition = n.navigation.updatePosition,
                        navigationOwnsRotation = n.navigation.updateRotation }).ToArray() });
            }
            if (time >= lifeCapture)
            {
                lifeCapture = time + 4;
                string suffix = Mathf.RoundToInt(time*10).ToString("D5");
                if(lifeRoutine.CurrentArena)
                {
                    var centre=lifeRoutine.CurrentArena.transform.position;
                    lifeCamera.transform.position=centre+new Vector3(12,10,-15);
                    lifeCamera.transform.LookAt(centre+Vector3.up);
                }
                CaptureRuntimeCamera(lifeCamera, Path.Combine(lifeOutput,"camp-"+suffix+".png"));
                CaptureRuntimeCamera(lifeScene.player.view, Path.Combine(lifeOutput,"player-"+suffix+".png"));
                foreach (var npc in lifeRoutine.participants)
                {
                    string clip = ActiveLifeClip(npc), key = npc.characterId + "-" + clip;
                    lifePoseCounts.TryGetValue(key,out int count);
                    if (count >= 3) continue;
                    lifePoseCounts[key] = count + 1;
                    using (var snapshot = new MotionSnapshot(npc.clips.gameObject))
                        foreach (string view in new[]{"front","back","side"})
                            CaptureMotion(snapshot.renderers,lifePoseCamera,view,Path.Combine(lifeOutput,key+"-"+view+"-"+suffix+".png"));
                }
                Debug.Log("BRAXHORN_CAMP_LIFE time="+time.ToString("F1")+" arena="+lifeRoutine.ArenaIndex+" rounds="+lifeRoutine.CompletedRounds+" bag="+lifeRoutine.BagStrikes+" sparring="+lifeRoutine.SparringStrikes);
            }
            if (time >= 420) FinishLifeReview();
        }
        catch (Exception ex) { lifeErrors.Add(ex.GetType().Name+": "+ex.Message); FinishLifeReview(); }
    }
    static string ActiveLifeClip(BraxObozNpc npc)
        => npc.clips.Cast<AnimationState>().Where(s=>s.enabled).OrderByDescending(s=>s.weight).FirstOrDefault()?.name ?? "none";
    static void FinishLifeReview()
    {
        EditorApplication.update -= TickLifeReview; Application.logMessageReceived -= LifeLog;
        SessionState.SetBool(LifeKey,false); Time.captureDeltaTime = 0;
        var report = new LifeReport { unity=Application.unityVersion, commit=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local",
            playModeTested=lifeFrames.Count>0, pauseDisplacement=lifePauseMove, pauseBagDegrees=lifePauseBag };
        if (lifeRoutine)
        {
            report.rounds=lifeRoutine.CompletedRounds; report.roleChanges=lifeRoutine.RoleChanges;
            report.arenaRounds=lifeRoutine.ArenaRounds;
            report.playerSparringArenas=lifePlayerArenas;
            report.bagStrikesDuringPlayerSparring=lifeFightBagStrikes;
            report.playerSparringStarted=lifePlayerFightStarted;report.playerSparringEnded=lifePlayerFightEnded;
            report.routineSuspendedForPlayer=lifePlayerFightSuspended;
            if(report.arenaRounds.Any(n=>n<1))lifeErrors.Add("Nie ukończono rundy we wszystkich trzech arenach");
            if(!lifePlayerFightStarted||!lifePlayerFightEnded||!lifePlayerFightSuspended||lifeRoutine.SuspendedForPlayer)
                lifeErrors.Add("Niepełny sparing Moniki / powrót planu zajęć");
            if(lifePlayerArenas.Any(done=>!done))lifeErrors.Add("Monika nie sprawdziła wszystkich trzech aren");
            report.bagStrikes=lifeRoutine.BagStrikes; report.sparringStrikes=lifeRoutine.SparringStrikes; report.orders=lifeRoutine.Orders;
            if(lifeRoutine.audio)
            {
                report.spokenLines=lifeRoutine.audio.SpokenLines;report.voiceExchanges=lifeRoutine.audio.Exchanges;report.contactSounds=lifeRoutine.audio.Contacts;
                if(report.spokenLines<2||report.contactSounds!=report.sparringStrikes)lifeErrors.Add("Niepełne zdarzenia rozmów/trafień audio");
            }
            else lifeErrors.Add("Brak wspólnego odtwarzania głosów");
            if(report.rounds<3)lifeErrors.Add("Nie ukończono trzech rund");
            if(report.bagStrikes<8||report.sparringStrikes<6)lifeErrors.Add("Za mało rzeczywistych trafień");
            if(report.orders!=2)lifeErrors.Add("Nie potwierdzono dwóch poleceń Moniki");
            foreach(var npc in lifeRoutine.participants)
                if(npc.Travelled<4||npc.Arrivals<2||npc.NavigationBlocked)lifeErrors.Add(npc.characterId+": niepełny obieg zajęć, dystans="+npc.Travelled+", dojścia="+npc.Arrivals);
        }
        else lifeErrors.Add("Brak planu zajęć");
        if(!lifePaused||!lifeResumed||lifePauseMove>.001f||lifePauseBag>.001f)lifeErrors.Add("Nieprawidłowa pauza postaci/worków");
        report.complete=lifeErrors.Count==0&&lifeFrames.Count>=500;
        if(lifeFrames.Any(f=>f.actors.Any(a=>a.navigationOwnsPosition||a.navigationOwnsRotation)))
        {lifeErrors.Add("Nawigacja przejęła transformację kontrolera kolizji");report.complete=false;}
        report.frames=lifeFrames.ToArray(); report.errors=lifeErrors.ToArray();
        if(string.IsNullOrEmpty(lifeOutput))lifeOutput=Path.GetFullPath(Path.Combine(Application.dataPath,"../../build/CampLifeReview"));
        Directory.CreateDirectory(lifeOutput); File.WriteAllText(Path.Combine(lifeOutput,"camp-life-review.json"),JsonUtility.ToJson(report,true));
        foreach (var error in report.errors) Debug.LogError("BRAXHORN_CAMP_LIFE failure: " + error);
        Debug.Log("BRAXHORN_CAMP_LIFE complete="+report.complete+" frames="+lifeFrames.Count+" phone_tested=false");
        EditorApplication.Exit(report.complete?0:1);
    }
}
