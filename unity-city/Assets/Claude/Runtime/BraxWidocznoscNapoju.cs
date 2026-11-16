using UnityEngine;

[DefaultExecutionOrder(60)]
public sealed class BraxWidocznoscNapoju : MonoBehaviour
{
    public Animation ruch;
    public Renderer szklo, plyn;
    public float wysokosc;

    void LateUpdate()
    {
        bool pije = ruch && ruch.IsPlaying("Drink");
        if (szklo) szklo.enabled = pije;
        if (!plyn) return;
        plyn.enabled = false;
        if (!pije) return;
        var stan = ruch["Drink"];
        float czas = stan.wrapMode == WrapMode.Loop || ruch.wrapMode == WrapMode.Loop
            ? Mathf.Repeat(stan.time,stan.length) : stan.time;
        float poziom = .46f*(1f-Mathf.InverseLerp(63f/24f,99f/24f,czas));
        poziom = Mathf.Min(poziom, Mathf.Clamp01(Vector3.Dot(transform.up,Vector3.up)));
        if (poziom < .001f) return;
        var t = plyn.transform;
        float polowa = wysokosc * .40f * poziom;
        var skala = t.localScale;
        skala.y = polowa;
        t.localScale = skala;
        t.localPosition = Vector3.up * (.12f*wysokosc + polowa);
        plyn.enabled = true;
    }

    void OnDisable() { if (szklo) szklo.enabled = false; if (plyn) plyn.enabled = false; }
}
