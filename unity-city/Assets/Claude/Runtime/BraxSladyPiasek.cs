using UnityEngine;
using UnityEngine.SceneManagement;

// ŚLADY STÓP NA PIASKU (wykonawca A, 2.11; właściciel z39: „piasek ma się uginać pod nogami”). Bez cząsteczek, bez siatek
// i bez naklejek na piasku: krok gracza (stopa schodzi na ziemię — jak w BraxKroki) na terenie piasku (plaża miejska, wyspa)
// dopisuje odcisk (x, z, kąt stopy, czas) do pierścienia 24 śladów; shader terenu Braxhorn/NabrzezeTeren sam rysuje
// wgłębienie (ciemniejszy, ubity piasek + pochylona normalna na brzegach dołka), które po 45 s się zaciera.
// Globalne: _BraxSlady[24], _BraxSladyN, _BraxSladyCzas. Dokładany sam do gracza po wczytaniu sceny (jak BraxKroki) — bez
// zmian w BraxGymPlayer. Klasa = plik, bez pól pod #if.
public sealed class BraxSladyPiasek : MonoBehaviour
{
    const int N = 24;
    static readonly Vector4[] slady = new Vector4[N];
    static int nast, ile;
    static readonly int idS = Shader.PropertyToID("_BraxSlady"), idN = Shader.PropertyToID("_BraxSladyN"), idC = Shader.PropertyToID("_BraxSladyCzas");

    Transform lewa, prawa; bool gorL, gorP; float czekaj;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init() { Dopnij(); SceneManager.sceneLoaded += (s, m) => Dopnij(); Shader.SetGlobalVectorArray(idS, slady); Shader.SetGlobalFloat(idN, 0f); }
    static void Dopnij()
    {
        var p = Object.FindAnyObjectByType<BraxGymPlayer>();
        if (p && !p.GetComponent<BraxSladyPiasek>()) p.gameObject.AddComponent<BraxSladyPiasek>();
    }

    void Start()
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (!lewa && t.name.EndsWith("LeftFoot")) lewa = t;
            if (!prawa && t.name.EndsWith("RightFoot")) prawa = t;
        }
    }

    static bool Piasek(Vector3 p, out Vector3 punkt)
    {
        punkt = p;
        if (!Physics.Raycast(p + Vector3.up * .6f, Vector3.down, out var hit, 1.6f, ~0, QueryTriggerInteraction.Ignore)) return false;
        var n = hit.collider.name;
        if (!(n.StartsWith("Plaża: teren") || n.StartsWith("Wyspa: teren"))) return false;
        punkt = hit.point; return true;
    }

    void Stopa(Transform f, ref bool gora)
    {
        if (!f) return;
        float h = f.position.y - transform.position.y;
        if (h > .16f) { gora = true; return; }
        if (!gora || h > .09f) return;
        gora = false;
        if (!Piasek(f.position, out var p)) return;
        var fwd = transform.forward; float kat = Mathf.Atan2(fwd.x, fwd.z);
        slady[nast] = new Vector4(p.x, p.z, kat, Time.time); nast = (nast + 1) % N; ile = Mathf.Min(N, ile + 1);
        Shader.SetGlobalVectorArray(idS, slady); Shader.SetGlobalFloat(idN, ile);
    }

    void Update()
    {
        Shader.SetGlobalFloat(idC, Time.time);
        if (Time.time < czekaj) return;
        Stopa(lewa, ref gorL); Stopa(prawa, ref gorP);
    }
}
