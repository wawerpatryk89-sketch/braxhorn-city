// The region's sky (BraxChampionsSky, Unity-only importer code) for the tower preview: the same panorama and the same turn
// and exposure (test_wiezowce_20261116 keeps these equal to the real file).
public static class BraxChampionsSky
{
    public const string Slug = "kloofendal_48d_partly_cloudy_puresky";
    public const string Source = "Assets/Source/Mistrzowie/Sky/" + Slug + "_4k.hdr";
    public const float Rotation = 20.8f, Exposure = 1.18f;
}
// the city's working lift (Assets/Claude/Runtime/BraxWinda.cs, a runtime MonoBehaviour): its serialised fields only
public sealed class BraxWinda : UnityEngine.MonoBehaviour
{
    public string nazwa = "WINDA";
    public UnityEngine.Transform kabina, kabinaL, kabinaP;
    public float[] pietraY = new float[0];
    public string[] pietraNazwy = new string[0];
    public bool[] uzytkowe = new bool[0];
    public UnityEngine.Transform[] przystanekL = new UnityEngine.Transform[0], przystanekP = new UnityEngine.Transform[0];
    public UnityEngine.Vector3[] wezwanie = new UnityEngine.Vector3[0];
    public UnityEngine.Vector2 szyb, wnetrze = new UnityEngine.Vector2(.9f, .9f);
    public float przesuw = .75f;
    public int start;
}
