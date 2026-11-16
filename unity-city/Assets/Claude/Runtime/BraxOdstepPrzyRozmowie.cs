using UnityEngine;

// Corrections are measured on the clothed model and blended by the weights of the active legacy clips.
[DefaultExecutionOrder(35)]
public sealed class BraxOdstepPrzyRozmowie : MonoBehaviour
{
    public Animation ruch;
    public Transform lewyBark, prawyBark, lewyLokiec, prawyLokiec;
    public Vector2 spoczynek, rozmowa, kierunekRozmowy;
    public float znakPrzodu = 1f;
    public string klipPrzyBarze;
    public float[] lewaPrzyBarze, prawaPrzyBarze;
    AnimationState idle, talk, bar;
    readonly Quaternion[] przed = new Quaternion[2], po = new Quaternion[2];
    readonly bool[] zmieniono = new bool[2];

    void Start()
    {
        idle = ruch["Idle"];
        talk = ruch["Talk"];
        if (!string.IsNullOrEmpty(klipPrzyBarze)) bar = ruch[klipPrzyBarze];
    }

    void Update() => Przywroc();
    void OnDisable() => Przywroc();

    void Przywroc()
    {
        for (int i = 0; i < 2; i++)
        {
            if (!zmieniono[i]) continue;
            var kosc = i == 0 ? lewyBark : prawyBark;
            if (kosc && Quaternion.Angle(kosc.localRotation, po[i]) < .01f)
                kosc.localRotation = przed[i];
            zmieniono[i] = false;
        }
    }

    void LateUpdate()
    {
        float a = idle != null && idle.enabled ? idle.weight : 0f;
        float b = talk != null && talk.enabled ? talk.weight : 0f;
        float c = bar != null && bar.enabled ? bar.weight : 0f;
        if (a + b + c < .001f) return;
        var kat = (a * spoczynek + b * rozmowa) / Mathf.Max(1f, a+b);
        if (c > .001f && lewaPrzyBarze != null && lewaPrzyBarze.Length > 1)
        {
            float czas = bar.wrapMode == WrapMode.Loop || ruch.wrapMode == WrapMode.Loop
                ? Mathf.Repeat(bar.time,bar.length) : Mathf.Clamp(bar.time,0f,bar.length);
            float f = (lewaPrzyBarze.Length-1)*czas/bar.length;
            int i = Mathf.Min((int)f,lewaPrzyBarze.Length-2);
            kat += c * new Vector2(Mathf.Lerp(lewaPrzyBarze[i],lewaPrzyBarze[i+1],f-i),
                Mathf.Lerp(prawaPrzyBarze[i],prawaPrzyBarze[i+1],f-i));
        }
        var bok = (lewyBark.position - prawyBark.position).normalized;
        var przod = Vector3.Cross(bok, ruch.transform.up).normalized * znakPrzodu;
        float udzial = b / Mathf.Max(.001f, a+b);
        Ustaw(0, lewyBark, lewyLokiec, bok + przod*kierunekRozmowy.x*udzial, kat.x);
        Ustaw(1, prawyBark, prawyLokiec, -bok + przod*kierunekRozmowy.y*udzial, kat.y);
    }

    void Ustaw(int i, Transform bark, Transform lokiec, Vector3 bok, float kat)
    {
        if (kat <= .01f) return;
        Vector3 os = Vector3.Cross(lokiec.position - bark.position, bok);
        if (os.sqrMagnitude < 1e-12f) return;
        przed[i] = bark.localRotation;
        bark.rotation = Quaternion.AngleAxis(kat, os.normalized) * bark.rotation;
        po[i] = bark.localRotation;
        zmieniono[i] = true;
    }
}
