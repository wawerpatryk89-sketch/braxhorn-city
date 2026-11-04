using UnityEngine;
using UnityEngine.SceneManagement;

// 24 ślady w istniejącym shaderze terenu: bez nakładających się płaszczyzn i bez deformacji siatki.
public sealed class BraxSladyPiasek : MonoBehaviour
{
    const int N = 24;
    const float CzasZycia = 45f;
    static readonly Vector4[] slady = new Vector4[N];
    static readonly float[] czasy = new float[N];
    static int nast, ile;
    static readonly int idS = Shader.PropertyToID("_BraxSlady"), idN = Shader.PropertyToID("_BraxSladyN"),
        idC = Shader.PropertyToID("_BraxSladyCzas"), idT = Shader.PropertyToID("_BraxSladyWiek"), idB = Shader.PropertyToID("_BraxSladyGranice");
    Transform lewa, prawa;
    CharacterController cc;
    float spocznikL = float.NaN, spocznikP = float.NaN, ostatniaL, ostatniaP;
    bool gorL, gorP;
    Vector3 poprzedniaPozycja;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        SceneManager.sceneLoaded -= Scena; SceneManager.sceneUnloaded -= Wyjscie;
        Wyczysc();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= Scena; SceneManager.sceneLoaded += Scena;
        SceneManager.sceneUnloaded -= Wyjscie; SceneManager.sceneUnloaded += Wyjscie;
        Dopnij();
    }
    static void Scena(Scene s, LoadSceneMode m) => Dopnij();
    static void Wyjscie(Scene s) => Wyczysc();
    static void Wyczysc()
    {
        nast = ile = 0; Shader.SetGlobalFloat(idN, 0f);
        Shader.SetGlobalVector(idB, Vector4.zero);
    }
    static void Dopnij()
    {
        var p = Object.FindAnyObjectByType<BraxGymPlayer>();
        if (p && !p.GetComponent<BraxSladyPiasek>()) p.gameObject.AddComponent<BraxSladyPiasek>();
    }
    void Start()
    {
        cc = GetComponent<CharacterController>(); poprzedniaPozycja = transform.position;
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (!lewa && t.name.EndsWith("LeftFoot")) lewa = t;
            if (!prawa && t.name.EndsWith("RightFoot")) prawa = t;
        }
    }
    void LateUpdate()
    {
        Shader.SetGlobalFloat(idC, Time.time);
        if ((transform.position - poprzedniaPozycja).sqrMagnitude > 100f) Wyczysc();
        poprzedniaPozycja = transform.position;
        if (!cc || !cc.enabled) return;
        bool ruch = cc.isGrounded && new Vector2(cc.velocity.x, cc.velocity.z).sqrMagnitude > .16f;
        Stopa(lewa, ref spocznikL, ref gorL, ref ostatniaL, ruch);
        Stopa(prawa, ref spocznikP, ref gorP, ref ostatniaP, ruch);
    }
    void Stopa(Transform foot, ref float rest, ref bool lifted, ref float last, bool moving)
    {
        if (!foot) return;
        float h = foot.position.y - transform.position.y;
        if (float.IsNaN(rest) || (!moving && cc.isGrounded))
        { rest = float.IsNaN(rest) ? h : Mathf.Lerp(rest, h, .1f); lifted = false; return; }
        if (h > rest + .07f) { lifted = true; return; }
        if (!lifted || h > rest + .03f) return;
        lifted = false;
        if (!moving || Time.time - last < .18f) return;
        if (!Physics.Raycast(foot.position + Vector3.up * .3f, Vector3.down, out var hit, .65f,
            ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore)) return;
        var sand = hit.collider.GetComponent<BraxPiasekPowierzchnia>();
        // Miasto zachowuje dotychczasowe ślady; wyspa ma dokładną maskę piasku z generatora.
        if (sand ? !sand.JestPiasek(hit) : !hit.collider.name.StartsWith("Plaża: teren")) return;
        var dir = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        slady[nast] = new Vector4(hit.point.x, hit.point.z, dir.z, dir.x);
        czasy[nast] = Time.time; nast = (nast + 1) % N; ile = Mathf.Min(N, ile + 1); last = Time.time;
        Shader.SetGlobalVectorArray(idS, slady); Shader.SetGlobalFloatArray(idT, czasy); Shader.SetGlobalFloat(idN, ile);
        var bounds = new Vector4(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
        for (int i = 0; i < ile; i++) if (Time.time - czasy[i] < CzasZycia)
        { bounds.x = Mathf.Min(bounds.x, slady[i].x - .3f); bounds.y = Mathf.Min(bounds.y, slady[i].y - .3f);
          bounds.z = Mathf.Max(bounds.z, slady[i].x + .3f); bounds.w = Mathf.Max(bounds.w, slady[i].y + .3f); }
        Shader.SetGlobalVector(idB, bounds);
    }
    void OnDestroy() => Wyczysc();
}
