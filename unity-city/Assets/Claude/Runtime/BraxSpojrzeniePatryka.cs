using System.Collections;
using UnityEngine;

// Additive correction after the legacy Animation has sampled the current pose.
public sealed class BraxSpojrzeniePatryka : MonoBehaviour
{
    public Transform szyja, glowa, postac;
    public Vector3 przodGlowy;
    Behaviour rozmowca;
    Transform cel;
    Vector2 kat, predkosc;
    Quaternion bazaSzyi, bazaGlowy, ostatniaSzyja, ostatniaGlowa;
    bool zastosowano;

    uint sesja;

    public static IEnumerator Rozmowa(BraxSpojrzeniePatryka a, BraxSpojrzeniePatryka b, IEnumerator dialog)
    {
        uint pierwsza = 0, druga = 0;
        if (a && b)
        {
            pierwsza = ++a.sesja; druga = ++b.sesja;
            a.rozmowca = b; a.cel = b.glowa;
            b.rozmowca = a; b.cel = a.glowa;
        }
        try
        {
            while (dialog.MoveNext()) yield return dialog.Current;
        }
        finally
        {
            (dialog as System.IDisposable)?.Dispose();
            if (a && a.sesja == pierwsza) { a.rozmowca = null; a.cel = null; }
            if (b && b.sesja == druga) { b.rozmowca = null; b.cel = null; }
        }
    }

    static Vector3 Oczy(Transform kosc, Vector3 gora)
    {
        float odstep = kosc.parent ? Vector3.Distance(kosc.position, kosc.parent.position) * .55f : 0f;
        return kosc.position + gora * odstep;
    }

    static Vector2 Kierunek(Vector3 v)
    {
        return new Vector2(Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg,
            Mathf.Atan2(v.y, new Vector2(v.x, v.z).magnitude) * Mathf.Rad2Deg);
    }

    void PrzywrocPoze()
    {
        if (!zastosowano) return;
        if (szyja && Quaternion.Angle(szyja.localRotation, ostatniaSzyja) < .01f)
            szyja.localRotation = bazaSzyi;
        if (glowa && Quaternion.Angle(glowa.localRotation, ostatniaGlowa) < .01f)
            glowa.localRotation = bazaGlowy;
        zastosowano = false;
    }

    void OnDisable()
    {
        PrzywrocPoze();
        kat = predkosc = Vector2.zero;
        rozmowca = null;
        cel = null;
    }

    void LateUpdate()
    {
        PrzywrocPoze();
        if (!glowa || !szyja || !postac) return;
        Vector2 celKata = Vector2.zero;
        bool aktywny = rozmowca && cel && rozmowca.isActiveAndEnabled &&
            (cel.position - glowa.position).sqrMagnitude < 5.4f * 5.4f;
        if (aktywny)
        {
            Vector3 oczy = Oczy(glowa, postac.up);
            Vector3 oczyOsoby = Oczy(cel, postac.up);
            Vector3 wektor = postac.InverseTransformDirection(oczyOsoby - oczy);
            if (wektor.sqrMagnitude > .0025f)
            {
                Vector2 kierunek = Kierunek(wektor);
                Vector2 poza = Kierunek(postac.InverseTransformDirection(glowa.TransformDirection(przodGlowy)));
                float waga = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(65f, 95f, Mathf.Abs(kierunek.x)));
                celKata = waga * new Vector2(Mathf.Clamp(.86f * Mathf.DeltaAngle(poza.x, kierunek.x), -28f, 28f),
                    Mathf.Clamp(.78f * Mathf.DeltaAngle(poza.y, kierunek.y), -12f, 12f));
            }
        }
        kat = Vector2.SmoothDamp(kat, celKata, ref predkosc, aktywny ? .20f : .34f);
        if (kat.sqrMagnitude < .0001f) return;
        bazaSzyi = szyja.localRotation;
        bazaGlowy = glowa.localRotation;
        var glowaPrzed = glowa.rotation;
        var yaw = Quaternion.AngleAxis(kat.x, postac.up);
        var obrot = Quaternion.AngleAxis(-kat.y, yaw * postac.right) * yaw;
        szyja.rotation = Quaternion.Slerp(Quaternion.identity, obrot, .36f) * szyja.rotation;
        glowa.rotation = obrot * glowaPrzed;
        ostatniaSzyja = szyja.localRotation;
        ostatniaGlowa = glowa.localRotation;
        zastosowano = true;
    }
}
