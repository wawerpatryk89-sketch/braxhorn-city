using System.Collections;
using UnityEngine;

public sealed class BraxPokazCiosow : MonoBehaviour, IBraxInterakcja
{
    public static readonly string[] Ciosy = { "Jab", "Cross", "Hook", "HookShort", "HookReverse" };
    public Animation ruch;
    public bool Trwa { get; private set; }

    void OnEnable() => BraxInterakcje.Dodaj(this);

    void OnDisable()
    {
        BraxInterakcje.Usun(this);
        StopAllCoroutines();
        Trwa = false;
    }

    public string Kontekst(Vector3 gracz)
    {
        if (!ruch || Trwa || Vector3.Distance(transform.position, gracz) > 2.6f * BraxGlosPostaci.K) return "";
        return ruch.IsPlaying("Idle") || ruch.IsPlaying("CombatIdle") ? "POKAŻ 5 CIOSÓW" : "";
    }

    public bool Wykonaj(Vector3 gracz)
    {
        if (Kontekst(gracz).Length == 0) return false;
        StartCoroutine(Pokaz());
        return true;
    }

    IEnumerator Pokaz()
    {
        Trwa = true;
        string powrot = ruch.IsPlaying("CombatIdle") ? "CombatIdle" : "Idle";
        foreach (string cios in Ciosy)
        {
            var stan = ruch[cios];
            stan.wrapMode = WrapMode.Once;
            stan.time = 0f;
            ruch.CrossFade(cios, .09f);
            yield return new WaitForSeconds(Mathf.Max(.05f,stan.length-.09f));
            if (!ruch.IsPlaying(cios) && ruch.isPlaying) { Trwa = false; yield break; }
        }
        ruch.CrossFade(powrot,.12f);
        Trwa = false;
    }
}
