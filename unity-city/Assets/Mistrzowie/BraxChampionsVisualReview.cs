using System;
using System.Collections;
using System.IO;
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

    IEnumerator Start()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--champions-review")<0)yield break;
        ConfigureHotelIfPresent();
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
        }

        if(sun)sun.intensity=daySun;
        RenderSettings.ambientSkyColor=daySky;RenderSettings.ambientEquatorColor=dayEq;RenderSettings.ambientGroundColor=dayGround;
        bool hotelReview=labels.Length==21&&labels[0]=="01-front-day"&&labels[20]=="21-roof-terrace";
        File.WriteAllText(Path.Combine(output,"complete.txt"),
            hotelReview
            ?"HOTEL SOBCZAK: 21 real standalone-player acceptance screenshots captured; owner visual PASS still pending. APK NOT BUILT.\n"
            :"Real standalone player screenshots; Android performance and visual acceptance still pending.\n");
        Application.Quit(0);
    }
}
