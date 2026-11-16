using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;

public sealed class BraxProbaRuchu : MonoBehaviour
{
    public BraxGymPlayer player;
    public Camera kamera;
    string opis;
    float stick;
    static readonly BindingFlags Pola = BindingFlags.NonPublic | BindingFlags.Instance;

    IEnumerator Start()
    {
        var dest = Environment.GetEnvironmentVariable("BRAX_PROBA_KLATKI");
        Directory.CreateDirectory(dest);
        Application.runInBackground = true;
        while (!player.ready) yield return null;
        // Podaj kontrolowane pozycje kciuka do tego samego przeliczenia i MoveInput co na Androidzie.
        var controls = new GameObject("Joystick testowy").AddComponent<BraxTouchControls>();
        controls.enabled = false;
        var type = controls.GetType();
        type.GetField("player", Pola).SetValue(controls,player);
        type.GetMethod("Uklad", Pola).Invoke(controls,null);
        float radius = (float)type.GetField("R", Pola).GetValue(controls);
        if (radius <= 0) throw new InvalidOperationException("Brak promienia joysticka");
        var origin = new Vector2(100,100);
        type.GetField("joyOrigin", Pola).SetValue(controls,origin);
        var now = type.GetField("joyNow", Pola);
        var send = type.GetMethod("WyslijRuch", Pola);
        float[] levels = {0,.5f,.95f,0};
        string[] states = {"Idle","Walk","Run","Idle"};
        string[] labels = {"Spoczynek","Chód","Bieg","Zatrzymanie"};
        float walkSpeed = 0;
        var end = new WaitForEndOfFrame();
        Time.captureFramerate = 24;
        var frame = new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        var face = player.GetComponentInChildren<BraxMimikaOsiedla>();
        var properties = new MaterialPropertyBlock();
        float blinkMax = 0f;
        for (int phase=0;phase<levels.Length;phase++)
        {
            stick=levels[phase]; opis=labels[phase];
            now.SetValue(controls,origin + Vector2.up * radius * stick);
            send.Invoke(controls,null);
            Vector3 start=Vector3.zero;
            for(int i=0;i<48;i++)
            {
                yield return end;
                face.twarz.GetPropertyBlock(properties);
                blinkMax = Mathf.Max(blinkMax,properties.GetFloat("_Blink"));
                if (i==23) start=player.transform.position;
                frame.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);
                frame.Apply(false);
                File.WriteAllBytes(Path.Combine(dest,(phase*48+i).ToString("D4")+".jpg"),frame.EncodeToJPG(88));
            }
            var delta=player.transform.position-start; delta.y=0;
            if (!player.clips.IsPlaying(states[phase]) || (float.IsNaN(delta.magnitude) || float.IsInfinity(delta.magnitude)))
                throw new InvalidOperationException("Niepoprawny stan " + states[phase]);
            if (phase==1 && (delta.magnitude<.5f || delta.magnitude>2.5f) ||
                phase==2 && delta.magnitude < walkSpeed*1.5f ||
                (phase==0 || phase==3) && delta.magnitude>.08f)
                throw new InvalidOperationException("Niepoprawne przemieszczenie " + delta.magnitude);
            if (phase==1) walkSpeed=delta.magnitude;
            Debug.Log("BRAX_PROBA " + states[phase]+" wychylenie="+stick+" dystans_1s="+delta.magnitude.ToString("F3",CultureInfo.InvariantCulture));
        }
        if (blinkMax < .80f) throw new InvalidOperationException("Brak domknięcia powiek Patryka");
        Time.captureFramerate = 0;
        var clip = Resources.Load<AudioClip>("OsiedleGlosy/9a79ffe04cb33bdb_m");
        if (!clip) throw new InvalidOperationException("Brak kwestii Patryka");
        opis = "Dobra, pamiętam.";
        player.gameObject.AddComponent<BraxGlosPostaci>().Mow(clip);
        float peakJaw = 0f, peakRound = 0f;
        float finish = Time.realtimeSinceStartup + clip.length + .5f;
        while (Time.realtimeSinceStartup < finish)
        {
            yield return end;
            peakJaw = Mathf.Max(peakJaw,face.twarz.GetBlendShapeWeight(face.szczeka));
            peakRound = Mathf.Max(peakRound,face.twarz.GetBlendShapeWeight(face.wargi));
        }
        if (peakJaw < 25f || peakRound < 5f || face.twarz.GetBlendShapeWeight(face.szczeka) > 3f)
            throw new InvalidOperationException("Nieprawidłowe usta podczas lub po kwestii");
        File.WriteAllText(Path.Combine(dest,"wynik.txt"),"PASS Idle Walk Run Idle; PASS blink and speech");

        Application.Quit(0);
    }

    void LateUpdate()
    {
        Vector3 target=player.transform.position+Vector3.up*1.12f;
        kamera.transform.position=target+new Vector3(4.6f,.42f,2.0f);
        kamera.transform.LookAt(target);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(16,12,Screen.width-32,35),"OSIEDLE  |  Patryk   -   " + opis);
        GUI.Label(new Rect(16,42,Screen.width-32,35),"Wychylenie joysticka: "+Mathf.RoundToInt(stick*100f)+"%   |   Unity 6");
    }
}
