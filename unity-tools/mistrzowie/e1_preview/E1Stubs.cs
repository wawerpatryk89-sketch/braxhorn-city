// Stand-in for the city lift (Assets/Claude/Runtime/BraxWinda.cs, R1): only its configuration fields.
using UnityEngine;
public sealed class BraxWinda : MonoBehaviour
{
    public string nazwa = "WINDA";
    public Transform kabina, kabinaL, kabinaP;
    public float[] pietraY = new float[0];
    public string[] pietraNazwy = new string[0];
    public bool[] uzytkowe = new bool[0];
    public Transform[] przystanekL = new Transform[0], przystanekP = new Transform[0];
    public Vector3[] wezwanie = new Vector3[0];
    public Vector2 szyb, wnetrze = new Vector2(.9f, .9f);
    public float przesuw = .75f;
    public int start;
}

// Physics of the E1 games (BraxChampionsKregle / BraxChampionsBilard / BraxChampionsE1Ekrany run in Unity only): the
// preview records their configuration fields.
namespace UnityEngine
{
    public enum RigidbodyInterpolation { None, Interpolate, Extrapolate }
    public enum CollisionDetectionMode { Discrete, Continuous, ContinuousDynamic, ContinuousSpeculative }
    public sealed class Rigidbody : Component { public float mass = 1f; public bool isKinematic; public RigidbodyInterpolation interpolation; public CollisionDetectionMode collisionDetectionMode; }
    public sealed class SphereCollider : Collider { public float radius = .5f; public Vector3 center; }
}
public sealed class BraxChampionsKregle : MonoBehaviour
{
    public Vector3[] faul, zwrotnica, kulaNaZwrotnicy; public Transform[] kregle; public Rigidbody kula; public Renderer[] ekrany, konsole;
    public float rozbieg, glowa, koniecPola, polowaToru, rynna, promien, glebRynny, predkosc, maxKat, srodekCiezkosci;
}
public sealed class BraxChampionsBilard : MonoBehaviour
{
    public Vector3[] stoly, luzy; public float polX, polZ, r, promienLuzy, sila, silaMocna, zasieg; public Rigidbody[] bile; public Transform kij; public Collider[] bandy;
}
public sealed class BraxChampionsE1Ekrany : MonoBehaviour
{
    public BraxChampionsKregle kregle; public BraxChampionsBilard bilard; public Renderer[] tablice, kioski, automaty, cymbergaj;
}
