using System;
using UnityEngine;

// D2 station clock in the front arch (owner board: the clock over "DWORZEC"): the hands show the phone's local time.
// Hands turn about their local z (the dial faces -z of the clock group; clockwise as seen from the forecourt).
[DisallowMultipleComponent]
public sealed class BraxChampionsZegar : MonoBehaviour
{
    public Transform godzinowa, minutowa, sekundowa;
    float nastepny;
    void Update()
    {
        if (Time.unscaledTime < nastepny) return;
        nastepny = Time.unscaledTime + .2f;
        var n = DateTime.Now;
        float s = n.Second, mi = n.Minute + s / 60f, h = (n.Hour % 12) + mi / 60f;
        if (godzinowa) godzinowa.localRotation = Quaternion.Euler(0f, 0f, -h * 30f);
        if (minutowa) minutowa.localRotation = Quaternion.Euler(0f, 0f, -mi * 6f);
        if (sekundowa) sekundowa.localRotation = Quaternion.Euler(0f, 0f, -s * 6f);
    }
}
