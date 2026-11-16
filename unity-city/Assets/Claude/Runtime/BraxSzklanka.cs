using UnityEngine;

[DefaultExecutionOrder(60)]
public sealed class BraxSzklanka : MonoBehaviour
{
    public Transform postac, dlon, ciecz;
    public Animation ruch;
    public Vector3 dnoWDloni;
    public Quaternion obrotWDloni = Quaternion.identity;

    public void Podaj(Transform miejsce)
    {
        gameObject.SetActive(true);
        Dopasuj();
    }

    public void Schowaj() { gameObject.SetActive(false); }

    void LateUpdate() { Dopasuj(); }

    void Dopasuj()
    {
        if (!dlon) return;
        transform.SetPositionAndRotation(dlon.TransformPoint(dnoWDloni),dlon.rotation*obrotWDloni);
        if (!ciecz || !ruch || !ruch.IsPlaying("Drink"))
        {
            if (ciecz) ciecz.gameObject.SetActive(false);
            return;
        }
        var stan = ruch["Drink"];
        float sekunda = stan.time;
        if (stan.wrapMode == WrapMode.Loop || ruch.wrapMode == WrapMode.Loop)
            sekunda = Mathf.Repeat(sekunda,stan.length);
        float poziom = .46f*(1f-Mathf.InverseLerp(63f/24f,99f/24f,sekunda));
        float pochylenie = Vector3.Dot(transform.up,Vector3.up);
        poziom = Mathf.Min(poziom,Mathf.Clamp01(pochylenie));
        ciecz.gameObject.SetActive(poziom>.001f);
        var skala = ciecz.localScale;
        skala.y = .075f*poziom;
        ciecz.localScale = skala;
    }
}
