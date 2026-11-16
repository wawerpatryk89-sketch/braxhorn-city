using UnityEngine;

public sealed class BraxMimikaOsiedla : MonoBehaviour
{
    public SkinnedMeshRenderer twarz;
    public int szczeka, wargi;
    public bool widoczneOczy = true;
    static readonly int Blink = Shader.PropertyToID("_Blink");
    static readonly int Speech = Shader.PropertyToID("_Speech");
    MaterialPropertyBlock material;
    System.Random los;
    AudioSource glos;
    byte[] obwiednia;
    readonly float[] probki = new float[256];
    float nastepne, poczatek, usta, zaokraglenie;

    void Awake() { material = new MaterialPropertyBlock(); }

    void OnEnable()
    {
        los = new System.Random(GetInstanceID());
        poczatek = -10f;
        nastepne = Time.time + .4f + (float)los.NextDouble()*3.6f;
    }

    public void Wypowiedz(AudioSource zrodlo)
    {
        glos = zrodlo;
        var amplitudy = Resources.Load<TextAsset>("OsiedleMimika/" + glos.clip.name);
        obwiednia = amplitudy ? amplitudy.bytes : null;
        usta = 0f;
    }

    Vector2 RuchWarg()
    {
        if (!glos || !glos.isPlaying || !glos.clip) return Vector2.zero;
        if (obwiednia != null && obwiednia.Length >= 2)
        {
            float indeks = glos.timeSamples * (50f/glos.clip.frequency);
            int ostatnia = obwiednia.Length/2-1;
            int i = Mathf.Clamp((int)indeks,0,ostatnia);
            int a = 2*i, b = 2*Mathf.Min(i+1,ostatnia);
            return Vector2.Lerp(new Vector2(obwiednia[a],obwiednia[a+1]),
                                new Vector2(obwiednia[b],obwiednia[b+1]),indeks-i)/255f;
        }
        // Support voices supplied by a later content update that has no baked envelope yet.
        glos.GetOutputData(probki,0);
        float suma = 0;
        foreach (float v in probki) suma += v*v;
        return new Vector2(Mathf.Clamp01((Mathf.Sqrt(suma/probki.Length)-.009f)*6f),0f);
    }

    void LateUpdate()
    {
        float t = Time.time;
        if (t >= nastepne)
        {
            poczatek = t;
            nastepne = t + 2.3f + (float)los.NextDouble()*4.2f;
        }
        float faza = t-poczatek;
        float powieki = faza<.072f ? Mathf.SmoothStep(0f,1f,faza/.072f)
            : 1f-Mathf.SmoothStep(0f,1f,(faza-.084f)/.12f);
        Vector2 gloski = RuchWarg();
        float cel = gloski.x * .78f;
        float szybkosc = cel>usta ? 36f : 28f;
        usta = Mathf.Lerp(usta,cel,1f-Mathf.Exp(-szybkosc*Time.deltaTime));
        twarz.SetBlendShapeWeight(szczeka,usta*100f);
        zaokraglenie = Mathf.Lerp(zaokraglenie,gloski.y*.69f,1f-Mathf.Exp(-22f*Time.deltaTime));
        twarz.SetBlendShapeWeight(wargi,zaokraglenie*100f);
        twarz.GetPropertyBlock(material);
        material.SetFloat(Blink,widoczneOczy ? powieki : 0f);
        material.SetFloat(Speech,usta);
        twarz.SetPropertyBlock(material);
    }
}
