using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Explicit command-line review only. Ordinary Android play never enters this mode.
public sealed class BraxChampionsVisualReview : MonoBehaviour
{
    public Camera view;
    public BraxGymPlayer player;
    public Vector3[] positions,targets;
    public string[] labels;
    IEnumerator Start()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--champions-review")<0)yield break;
        if(!view||!player||positions==null||targets==null||labels==null||positions.Length!=targets.Length||positions.Length!=labels.Length)
        { Debug.LogError("Champions visual review setup invalid");Application.Quit(2);yield break; }
        string output=Environment.GetEnvironmentVariable("CHAMPIONS_REVIEW_DIR");
        if(string.IsNullOrEmpty(output))output=Path.Combine(Application.persistentDataPath,"ChampionsReview");
        Directory.CreateDirectory(output);
        player.enabled=false;
        var follow=view.GetComponent<BraxClaudeKamera>();if(follow)follow.enabled=false;
        Time.timeScale=0;
        for(int i=0;i<positions.Length;i++)
        {
            view.transform.position=positions[i];view.transform.LookAt(targets[i]);
            // Allow camera-dependent grass and texture streaming to settle; always time-bounded.
            yield return new WaitForSecondsRealtime(5);
            yield return new WaitForEndOfFrame();
            var frame=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            frame.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0,false);
            frame.Apply(false,false);
            File.WriteAllBytes(Path.Combine(output,labels[i]+".png"),frame.EncodeToPNG());
            Destroy(frame);
        }
        File.WriteAllText(Path.Combine(output,"complete.txt"),"Real standalone player screenshots; Android performance and visual acceptance still pending.\n");
        Application.Quit(0);
    }
}
