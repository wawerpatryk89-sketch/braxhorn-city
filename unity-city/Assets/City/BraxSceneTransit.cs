using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Osobny plik (1.11, 22:20): Unity zapisuje skrypt w scenie tylko, gdy nazwa pliku = nazwa klasy; inaczej w APK „brakujący skrypt” → awaria przy wczytywaniu sceny (osiedle 13, miasto od 9).
// PRZEJŚCIE MIĘDZY REJONAMI BEZ PODWÓJNEJ PAMIĘCI (audyt miasta 31.10, Opus). LoadSceneAsync(Single) wczytuje cały nowy rejon,
// zanim zwolni stary — miasto i osiedle przez chwilę siedziały naraz w pamięci telefonu (zamknięcie gry w tunelu).
// Teraz: pusta scena z czarną kamerą → zwolnienie starego rejonu → sprzątanie zasobów → dopiero wtedy wczytanie nowego.
// Działa na osobnym, trwałym obiekcie (obiekt świata ginie razem ze swoją sceną, więc jego korutyna by się urwała).
public sealed class BraxSceneTransit : MonoBehaviour
{
    static BraxSceneTransit inst;
    public static bool Busy{get;private set;}
    GUIStyle style;
    public static void Go(string scene)
    {
        if(Busy)return;
        if(!inst){var g=new GameObject("BRAXHORN przejście rejonu");DontDestroyOnLoad(g);inst=g.AddComponent<BraxSceneTransit>();}
        Busy=true;inst.StartCoroutine(inst.Run(scene));
    }
    IEnumerator Run(string scene)
    {
        BraxStartLog.Etap("przejście tunelem do "+scene);
        var empty=SceneManager.CreateScene("BRAXHORN przejscie "+Time.frameCount);
        var camGo=new GameObject("kamera przejścia");
        SceneManager.MoveGameObjectToScene(camGo,empty);
        var cam=camGo.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;cam.cullingMask=0;
        SceneManager.SetActiveScene(empty);
        yield return null;
        // rejon + jego doczytane obszary (BraxObszary): zwalniamy wszystkie sceny poza pustą
        var doZwolnienia=new System.Collections.Generic.List<Scene>();
        for(int i=0;i<SceneManager.sceneCount;i++){var sc=SceneManager.GetSceneAt(i);if(sc!=empty&&sc.isLoaded)doZwolnienia.Add(sc);}
        foreach(var sc in doZwolnienia)
        {
            var unload=SceneManager.UnloadSceneAsync(sc);
            if(unload!=null)yield return unload;
        }
        yield return Resources.UnloadUnusedAssets();
        GC.Collect();
        var load=SceneManager.LoadSceneAsync(scene,LoadSceneMode.Single);
        if(load==null){Busy=false;throw new InvalidOperationException("Missing original scene "+scene);}
        yield return load;
        Busy=false;
    }
    void OnGUI()
    {
        if(!Busy)return;
        if(style==null)style=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,fontSize=Mathf.Max(18,Screen.height/22)};
        style.normal.textColor=new Color(.86f,.84f,.8f);
        GUI.Label(new Rect(0,0,Screen.width,Screen.height),"WCZYTYWANIE…",style);
    }
}
