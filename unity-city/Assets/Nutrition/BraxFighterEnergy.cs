using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

// One actual resource across city/estate loads. Existing movement/combat rules
// are not changed by this shop feature; expenditure and refill update this state.
public sealed class BraxFighterEnergy : MonoBehaviour
{
    const string SaveKey="Braxhorn.fighter-energy.v1";
    public static BraxFighterEnergy Instance { get; private set; }
    [SerializeField] float maximum=100f;
    [SerializeField] float runningCostPerMetre=.12f;
    [SerializeField] float trainingCostPerHit=1.5f;
    public float Value { get; private set; }
    public float Maximum=>maximum;
    public int Refills { get; private set; }
    public bool HasEnergy=>Value>.001f;
    public event Action Changed;
    BraxGymPlayer player;
    BraxFighterEnergyObserver observer;
    Vector3 lastPosition;
    int lastHits;
    float nextReport;
    bool background,dirty,owner;
    GUIStyle heading,valueStyle;
    string hudValue="100 / 100";
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void BXNutritionState(string json);
#endif
    [Serializable] sealed class Snapshot
    {
        public bool active=true,hasEnergy,inRange,inShop,paused;
        public float energy,maximum;
        public int refills;
        public string area,context;
    }
    void Awake()
    {
        if(Instance&&Instance!=this){enabled=false;Destroy(gameObject);return;}
        if(maximum<=0||runningCostPerMetre<0||trainingCostPerHit<0)
            throw new InvalidOperationException("Invalid fighter energy configuration");
        owner=true;Instance=this;DontDestroyOnLoad(gameObject);
        float saved=PlayerPrefs.GetFloat(SaveKey,maximum);
        Value=float.IsNaN(saved)||float.IsInfinity(saved)?maximum:Mathf.Clamp(saved,0,maximum);
    }
    void OnEnable(){if(owner)SceneManager.sceneLoaded+=SceneLoaded;}
    void OnDisable(){if(owner){SceneManager.sceneLoaded-=SceneLoaded;Save();}}
    void Start(){if(owner)BindPlayer();}
    void SceneLoaded(Scene scene,LoadSceneMode mode){BindPlayer();}
    void BindPlayer()
    {
        player=UnityEngine.Object.FindFirstObjectByType<BraxGymPlayer>();
        if(!player)return;
        observer=player.GetComponent<BraxFighterEnergyObserver>();
        if(!observer)observer=player.gameObject.AddComponent<BraxFighterEnergyObserver>();
        observer.Bind(this,player);
        lastPosition=player.transform.position;lastHits=player.trainingHits;
        Save();
    }
    void LateUpdate()
    {
        if(!owner||!player||!observer)return;
        var position=player.transform.position;
        float distance=Vector2.Distance(new Vector2(position.x,position.z),new Vector2(lastPosition.x,lastPosition.z));
        int hits=Mathf.Max(0,player.trainingHits-lastHits);
        lastPosition=position;lastHits=player.trainingHits;
        if(player.ready&&!observer.Paused&&!background&&!(player.cityWorld&&player.cityWorld.loading))
        {
            var run=player.clips?player.clips["Run"]:null;
            // Review travel/scene arrivals are not running distance. The
            // original controller's clamped simulation step stays below 1m.
            float metres=distance<=1.5f&&run!=null&&run.enabled&&run.weight>.5f?distance:0f;
            float cost=metres*runningCostPerMetre+hits*trainingCostPerHit;
            if(cost>0)SetValue(Value-cost);
        }
        if(Time.unscaledTime>=nextReport){nextReport=Time.unscaledTime+.25f;Report();}
    }
    void SetValue(float value)
    {
        float clamped=Mathf.Clamp(value,0,maximum);
        if(Mathf.Abs(clamped-Value)<.00001f)return;
        Value=clamped;dirty=true;Changed?.Invoke();
    }
    public bool Refill()
    {
        if(!owner||Value>=maximum-.001f)return false;
        SetValue(maximum);Refills++;Save();Report();return true;
    }
    public bool IsPaused=>background||(observer&&observer.Paused);
    public void ReviewEnergy(string text)
    {
        // Existing project diagnostic mode already permits positional cuts.
        // Never interpret this fixture as earned gameplay or a continuous walk.
        if(!Uri.TryCreate(Application.absoluteURL,UriKind.Absolute,out var uri))return;
        if(Array.IndexOf(uri.Query.TrimStart('?').Split('&'),"qa=1")<0)return;
        if(float.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out float value)
            &&!float.IsNaN(value)&&!float.IsInfinity(value)&&value>=0&&value<=maximum)
        {SetValue(value);Report();}
    }
    void Report()
    {
        hudValue=Mathf.CeilToInt(Value)+" / "+Mathf.RoundToInt(maximum);
        var counter=player&&player.cityWorld?player.cityWorld.nutritionCounter:null;
        var state=new Snapshot{energy=Value,maximum=maximum,hasEnergy=HasEnergy,refills=Refills,
            inRange=counter&&counter.InRange,inShop=counter&&counter.InShop,paused=IsPaused,
            area=player&&player.cityWorld?player.cityWorld.area:"",context=counter?counter.Context:""};
#if UNITY_WEBGL && !UNITY_EDITOR
        BXNutritionState(JsonUtility.ToJson(state));
#endif
    }
    void OnGUI()
    {
        if(!owner||!player||!player.ready)return;
        if(heading==null)
        {
            heading=new GUIStyle(GUI.skin.label){fontSize=11,alignment=TextAnchor.MiddleLeft};
            heading.normal.textColor=new Color(.80f,.77f,.70f);
            valueStyle=new GUIStyle(heading){fontSize=15,alignment=TextAnchor.MiddleRight,fontStyle=FontStyle.Bold};
            valueStyle.normal.textColor=new Color(.96f,.93f,.86f);
        }
        float scale=Mathf.Clamp(Screen.height/800f,.8f,1.25f);
        var safe=Screen.safeArea;
        float x=safe.x+18*scale,y=Screen.height-safe.yMax+76*scale,w=174*scale,h=44*scale;
        var old=GUI.color;
        GUI.color=new Color(.055f,.07f,.075f,.88f);GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);
        GUI.color=Color.white;
        GUI.Label(new Rect(x+10*scale,y+3*scale,80*scale,24*scale),"ENERGIA",heading);
        GUI.Label(new Rect(x+80*scale,y+3*scale,84*scale,24*scale),hudValue,valueStyle);
        GUI.color=new Color(.23f,.25f,.25f);GUI.DrawTexture(new Rect(x+10*scale,y+31*scale,w-20*scale,4*scale),Texture2D.whiteTexture);
        GUI.color=HasEnergy?new Color(.76f,.64f,.43f):new Color(.78f,.29f,.16f);
        GUI.DrawTexture(new Rect(x+10*scale,y+31*scale,(w-20*scale)*(Value/maximum),4*scale),Texture2D.whiteTexture);
        GUI.color=old;
    }
    void Save(){if(owner&&dirty){PlayerPrefs.SetFloat(SaveKey,Value);PlayerPrefs.Save();dirty=false;}}
    void OnApplicationPause(bool paused){background=paused;if(paused)Save();}
    void OnApplicationFocus(bool focused){background=!focused;if(!focused)Save();}
    void OnApplicationQuit(){Save();}
    void OnDestroy(){if(Instance==this){Save();Instance=null;}}
}
