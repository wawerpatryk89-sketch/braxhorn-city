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
