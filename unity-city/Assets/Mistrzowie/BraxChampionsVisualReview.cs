using System;
using System.Collections;
using System.IO;
using Unity.Profiling;
using UnityEngine;

// Explicit command-line review only. Ordinary Android play never enters this mode.
// Generic Champions scenes keep the original 5-frame review. When HOTEL SOBCZAK T03
// exists, the review automatically switches to the owner's exact 21-frame acceptance set.
public sealed class BraxChampionsVisualReview : MonoBehaviour
{
    public Camera view;
    public BraxGymPlayer player;
    public Vector3[] positions,targets;
    public string[] labels;
    public bool[] nightFrames;

    Vector3 HotelWorld(BraxHotelSobczakFloorStreamer hotel,float x,float y,float z)
        =>hotel.transform.TransformPoint(new Vector3(x,y,z)*hotel.metreScale);

    void ConfigureHotelIfPresent()
    {
        var hotel=FindFirstObjectByType<BraxHotelSobczakFloorStreamer>();
        if(!hotel)return;

        // Rebuilt hotel 9.11: root carries K once (metreScale 1); points in nominal metres from the plot centre,
        // ground floor +0.30, floor 1 +6.30, guest floor n at 11.30 + 3.5 (n-2), roof terrace +102.30.
        labels=new[]{
            "01-front-day","02-front-night","03-three-quarter-left","04-three-quarter-right",
            "05-side","06-rear","07-entrance-canopy","08-lobby","09-reception","10-bar",
            "11-restaurant","12-spa","13-kitchen","14-typical-corridor","15-standard-room",
            "16-bathroom","17-elevators","18-stair","19-podium-terrace","20-top-floor-view","21-roof-terrace"
        };
        positions=new[]{
            HotelWorld(hotel,0,40,-110),HotelWorld(hotel,0,40,-110),
            HotelWorld(hotel,-85,36,-95),HotelWorld(hotel,85,36,-95),
            HotelWorld(hotel,-100,34,0),HotelWorld(hotel,0,34,100),
            HotelWorld(hotel,0,2.0f,-40),
            HotelWorld(hotel,-1.5f,1.95f,-14),HotelWorld(hotel,-2,1.9f,-11),HotelWorld(hotel,-10,1.9f,-11),
            HotelWorld(hotel,-19,8.0f,-17.5f),HotelWorld(hotel,6,8.0f,16.5f),HotelWorld(hotel,-6,8.0f,15),
            HotelWorld(hotel,-15.8f,12.95f,-3.8f),HotelWorld(hotel,2.4f,12.9f,-9),HotelWorld(hotel,-1.4f,12.9f,-7.9f),
            HotelWorld(hotel,0,12.95f,-4.7f),HotelWorld(hotel,11.9f,13.0f,-4.5f),
            HotelWorld(hotel,-19.5f,13.0f,-17.5f),HotelWorld(hotel,0,100.4f,-14.8f),
            HotelWorld(hotel,-12,106.3f,-14)
        };
        targets=new[]{
            HotelWorld(hotel,0,50,0),HotelWorld(hotel,0,50,0),
            HotelWorld(hotel,0,50,0),HotelWorld(hotel,0,50,0),
            HotelWorld(hotel,0,52,0),HotelWorld(hotel,0,52,0),
            HotelWorld(hotel,0,4.5f,-19),
            HotelWorld(hotel,2,1.8f,-4),HotelWorld(hotel,11,2.4f,-8.5f),HotelWorld(hotel,-17,1.6f,-11),
            HotelWorld(hotel,0,7.5f,-6),HotelWorld(hotel,12.5f,6.8f,9.5f),HotelWorld(hotel,-15,7.0f,10),
            HotelWorld(hotel,10,12.7f,-3.8f),HotelWorld(hotel,-2.2f,12.3f,-13.5f),HotelWorld(hotel,1,12.2f,-5.4f),
            HotelWorld(hotel,0,12.7f,-2.4f),HotelWorld(hotel,14,11.8f,0),
            HotelWorld(hotel,5,12,-16),HotelWorld(hotel,0,99,-60),
            HotelWorld(hotel,2,104.3f,0)
        };
        nightFrames=new bool[labels.Length];nightFrames[1]=true;
    }

    // owner 12.11 00:30 ("jak mi nie potwierdzicie, że są, to ci nie puszczę"): two extra frames from the real player in
    // the arena hall, one at each owner mural (left FIGHTERS, right BELT), from the floor beside the cage
    void AddArenaMuralFrames()
    {
        Bounds? Find(string prefix)
        {
            foreach(var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(r.transform.parent&&r.transform.parent.name.StartsWith(prefix,StringComparison.Ordinal)&&r.name.Contains("tile"))
                {
                    var b=r.bounds;
                    foreach(Transform t in r.transform.parent){var rr=t.GetComponent<Renderer>();if(rr)b.Encapsulate(rr.bounds);}
                    return b;
                }
            return null;
        }
        var left=Find("A1 MURAL LEFT_FIGHTERS");var right=Find("A1 MURAL RIGHT_BELT");
        if(left==null||right==null){Debug.LogWarning("CHAMPIONS_REVIEW_ARENA_MURALS missing left="+(left!=null)+" right="+(right!=null));return;}
        var a=left.Value;var b=right.Value;
        var mid=(a.center+b.center)*.5f;var eye=new Vector3(mid.x,Mathf.Min(a.min.y,b.min.y)-1.2f,mid.z);
        Array.Resize(ref positions,positions.Length+2);Array.Resize(ref targets,targets.Length+2);Array.Resize(ref labels,labels.Length+2);
        int n=labels.Length;
        positions[n-2]=eye+(a.center-eye).normalized*20f;positions[n-2].y=eye.y;targets[n-2]=a.center;labels[n-2]="22-arena-mural-lewy-zawodnicy";
        positions[n-1]=eye+(b.center-eye).normalized*20f;positions[n-1].y=eye.y;targets[n-1]=b.center;labels[n-1]="23-arena-mural-prawy-pas";
        if(nightFrames!=null&&nightFrames.Length==n-2)Array.Resize(ref nightFrames,n);
        Debug.Log("CHAMPIONS_REVIEW_ARENA_MURALS left="+a.center+" right="+b.center+" eye="+eye);
    }

    // owner 12.11 ("na wszystko chcę dowody, zdjęcia", "uruchom to, co chciałeś ... żebyś zrobił to wszystko dobrze"): frames
    // from the real player of what was repaired on 12.11 - the cage, a changing room with the bag, a flat's living room
    // from both ends (the TV, the painted walls and ceiling, no kitchen run), the station's train from both sides, the
    // gallery G1 inside and outside, the promenade - and the region's five open-air frames (the sun's shadows).
    // Every place is found in the scene by what stands there, so the frames follow the generators.
    void Add(string label,Vector3 eye,Vector3 target)
    {
        int n=labels.Length;
        Array.Resize(ref positions,n+1);Array.Resize(ref targets,n+1);Array.Resize(ref labels,n+1);
        positions[n]=eye;targets[n]=target;labels[n]=(n+1).ToString("D2")+"-"+label;
        if(nightFrames!=null&&nightFrames.Length==n)Array.Resize(ref nightFrames,n+1);
    }
    static Bounds? BoundsOf(Transform t)
    {
        if(!t)return null;
        Bounds? b=null;
        foreach(var r in t.GetComponentsInChildren<Renderer>(true)){if(b==null)b=r.bounds;else{var x=b.Value;x.Encapsulate(r.bounds);b=x;}}
        return b;
    }
    void AddRegionFrames(Vector3[] basePositions,Vector3[] baseTargets,string[] baseLabels)
    {
        var all=FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        Transform Named(Func<string,bool> ok){foreach(var t in all)if(ok(t.name))return t;return null;}
        int before=labels.Length;
        // the arena: the cage from the hall floor, a changing room with the bag on its bracket
        var cage=Named(n=>n=="kolizja podestu oktagonu");
        if(cage){var c=cage.position;Add("arena-klatka",c+new Vector3(13f,4.2f,-13f),c+Vector3.up*2.2f);}
        var bag=Named(n=>n.StartsWith("A1 szatnia | worek_bokserski",StringComparison.Ordinal));
        var gloves=Named(n=>n.StartsWith("A1 szatnia | rekawice",StringComparison.Ordinal));
        if(bag&&gloves)
        {
            var c=bag.position+Vector3.up*1.6f;var d=gloves.position-bag.position;d.y=0;d.Normalize();
            Add("arena-szatnia-worek",c+d*4.6f+Vector3.up*.3f,c);
            Add("arena-szatnia-lawka",gloves.position-d*2.6f+Vector3.up*1.4f,gloves.position);
        }
        // a flat: the living room seen from the sofa toward the TV, and from the TV into the room
        BraxTelewizor flat=null,totem=null;
        foreach(var tv in FindObjectsByType<BraxTelewizor>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(!tv.maMiejsce)continue;
            if(!flat&&tv.name.StartsWith("APT ",StringComparison.Ordinal)&&tv.gameObject.activeInHierarchy)flat=tv;
            if(!totem&&tv.name.StartsWith("G1 totem",StringComparison.Ordinal))totem=tv;
        }
        if(flat)
        {
            var c=flat.transform.TransformPoint(flat.srodekLokalny);var n=flat.transform.TransformDirection(flat.przodLokalny).normalized;
            Add("apartament-salon-telewizor",c+n*4.2f+Vector3.up*.7f,c-Vector3.up*.2f);
            Add("apartament-salon-sciany-sufit",c+n*.5f+Vector3.up*.7f,c+n*6f+Vector3.up*.9f);
        }
        // the station: the train from both sides of the platform track
        var train=BoundsOf(Named(n=>n.StartsWith("D2 pociąg | lokomotywa",StringComparison.Ordinal)));
        if(train!=null)
        {
            var b=train.Value;bool alongX=b.size.x>=b.size.z;
            var along=alongX?Vector3.right:Vector3.forward;var side=alongX?Vector3.forward:Vector3.right;
            float half=alongX?b.extents.x:b.extents.z;
            Add("dworzec-pociag-peron-a",b.center-along*(half*.8f)-side*9f+Vector3.up*1.5f,b.center-along*(half*.2f));
            Add("dworzec-pociag-peron-b",b.center+along*(half*.8f)+side*9f+Vector3.up*1.5f,b.center+along*(half*.2f));
        }
        // the gallery G1: at a totem in the mall, and the building from outside
        if(totem)
        {
            var c=totem.transform.TransformPoint(totem.srodekLokalny);var n=totem.transform.TransformDirection(totem.przodLokalny).normalized;
            Add("galeria-pasaz-totem",c+n*8f+Vector3.up*.6f,c);
        }
        var g1=BoundsOf(Named(n=>n.StartsWith("G1 |",StringComparison.Ordinal)));
        if(g1!=null){var b=g1.Value;Add("galeria-z-zewnatrz",b.center+new Vector3(b.extents.x+55f,22f,-(b.extents.z+55f)),b.center);}
        var prom=BoundsOf(Named(n=>n.Contains("PROMENADA")&&n.Contains("SHOPS")));
        if(prom!=null){var b=prom.Value;Add("promenada-sklepy",new Vector3(b.center.x-b.extents.x*.25f,b.min.y+7f,b.center.z-18f),b.center);}
        // the open air (shadows): the generators' own five frames
        if(basePositions!=null&&baseTargets!=null&&baseLabels!=null&&basePositions.Length==baseTargets.Length&&basePositions.Length==baseLabels.Length)
            for(int i=0;i<basePositions.Length;i++)Add("rejon-"+baseLabels[i],basePositions[i],baseTargets[i]);
        Debug.Log("CHAMPIONS_REVIEW_REGION_FRAMES added="+(labels.Length-before)+" cage="+(cage!=null)+" bag="+(bag!=null)+" flat="+(flat!=null)+" train="+(train!=null)+" totem="+(totem!=null)+" g1="+(g1!=null)+" promenade="+(prom!=null));
    }

    IEnumerator Start()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--champions-review")<0)yield break;
        // The region is read in parts (BraxChampionsObszary): the review reads every area first, so each area scene is
        // proven to load in a real player and every frame finds its place.
        if(FindAnyObjectByType<BraxChampionsObszary>())
        {
            BraxChampionsObszary.Wszystko=true;
            float limit=Time.realtimeSinceStartup+900f;
            yield return null;yield return null;
            while(!(BraxChampionsObszary.Spokoj&&BraxChampionsObszary.Laduje==0)&&Time.realtimeSinceStartup<limit)yield return null;
            Debug.Log("CHAMPIONS_REVIEW_AREAS loaded="+BraxChampionsObszary.Wczytane+"/"+BraxChampionsObszary.Razem+" failed="+BraxChampionsObszary.Bledy+" t="+Time.realtimeSinceStartup.ToString("F1")+" s");
        }
        var basePositions=positions;var baseTargets=targets;var baseLabels=labels;
        ConfigureHotelIfPresent();
        if(positions!=null&&targets!=null&&labels!=null)AddArenaMuralFrames();
        if(positions!=null&&targets!=null&&labels!=null&&!ReferenceEquals(labels,baseLabels))AddRegionFrames(basePositions,baseTargets,baseLabels);
        if(!view||!player||positions==null||targets==null||labels==null||positions.Length!=targets.Length||positions.Length!=labels.Length)
        { Debug.LogError("Champions visual review setup invalid");Application.Quit(2);yield break; }
        if(nightFrames!=null&&nightFrames.Length!=0&&nightFrames.Length!=labels.Length)
        { Debug.LogError("Champions visual review night-frame setup invalid");Application.Quit(2);yield break; }

        string output=Environment.GetEnvironmentVariable("CHAMPIONS_REVIEW_DIR");
        if(string.IsNullOrEmpty(output))output=Path.Combine(Application.persistentDataPath,"ChampionsReview");
        Directory.CreateDirectory(output);

        player.enabled=false;
        if(player.body)player.body.enabled=false;
        if(player.visual)player.visual.gameObject.SetActive(false);
        var follow=view.GetComponent<BraxClaudeKamera>();if(follow)follow.enabled=false;

        var sun=RenderSettings.sun;
        float daySun=sun?sun.intensity:0;
        Color daySky=RenderSettings.ambientSkyColor,dayEq=RenderSettings.ambientEquatorColor,dayGround=RenderSettings.ambientGroundColor;
        Time.timeScale=0;

        // what each frame costs to draw (the owner's "gra zacina"): batches, set-pass calls, triangles and shadow casters
        // of the last rendered frame, written beside the pictures (statystyki.txt)
        var batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count");
        var setPass=ProfilerRecorder.StartNew(ProfilerCategory.Render,"SetPass Calls Count");
        var triangles=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count");
        var casters=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Shadow Casters Count");
        var stats=new System.Text.StringBuilder("klatka\tbatches\tsetpass\ttrojkaty\trzucajace_cien\tjakosc="+QualitySettings.names[QualitySettings.GetQualityLevel()]+" cienie="+QualitySettings.shadows+" do "+QualitySettings.shadowDistance+"\n");
        for(int i=0;i<positions.Length;i++)
        {
            bool night=nightFrames!=null&&nightFrames.Length==labels.Length&&nightFrames[i];
            if(sun)sun.intensity=night ? 0.06f : daySun;
            RenderSettings.ambientSkyColor=night?new Color(.025f,.04f,.09f):daySky;
            RenderSettings.ambientEquatorColor=night?new Color(.035f,.04f,.065f):dayEq;
            RenderSettings.ambientGroundColor=night?new Color(.018f,.02f,.03f):dayGround;

            // Move the hidden review player too: hotel streaming uses player position,
            // so the correct guest floor / B1 / B2 is genuinely active before capture.
            player.transform.position=positions[i];
            Physics.SyncTransforms();
            view.transform.position=positions[i];view.transform.LookAt(targets[i]);

            yield return new WaitForSecondsRealtime(5);
            yield return new WaitForEndOfFrame();
            var frame=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            frame.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0,false);
            frame.Apply(false,false);
            File.WriteAllBytes(Path.Combine(output,labels[i]+".png"),frame.EncodeToPNG());
            Destroy(frame);
            stats.Append(labels[i]).Append('\t').Append(batches.LastValue).Append('\t').Append(setPass.LastValue).Append('\t').Append(triangles.LastValue).Append('\t').Append(casters.LastValue).Append('\n');
        }
        File.WriteAllText(Path.Combine(output,"statystyki.txt"),stats.ToString());
        batches.Dispose();setPass.Dispose();triangles.Dispose();casters.Dispose();

        if(sun)sun.intensity=daySun;
        RenderSettings.ambientSkyColor=daySky;RenderSettings.ambientEquatorColor=dayEq;RenderSettings.ambientGroundColor=dayGround;
        bool hotelReview=labels.Length>=21&&labels[0]=="01-front-day"&&labels[20]=="21-roof-terrace";
        File.WriteAllText(Path.Combine(output,"complete.txt"),
            hotelReview
            ?"HOTEL SOBCZAK: 21 real standalone-player acceptance screenshots captured; owner visual PASS still pending. APK NOT BUILT.\n"
            :"Real standalone player screenshots; Android performance and visual acceptance still pending.\n");
        Application.Quit(0);
    }
}
