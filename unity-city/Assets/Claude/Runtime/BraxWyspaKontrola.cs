using System;
using UnityEngine;
using UnityEngine.Profiling;

// Punkty odbioru zapisane w scenie, niezależne od statycznego stanu generatora.
// Telemetria mierzy pamięć przydzieloną przez Unity, NIE całkowity RAM procesu Androida.
public sealed class BraxWyspaKontrola : MonoBehaviour
{
    [Serializable] public struct Punkt { public string nazwa; public Vector3 pozycja; }
    public int wersja = 1;
    public Punkt[] punkty = Array.Empty<Punkt>();
    int klatki;
    double czas;
    float najdluzsza;
    void Update()
    {
        if (Time.timeScale <= 0) return;
        float dt = Time.unscaledDeltaTime;
        czas += dt; klatki++; najdluzsza = Mathf.Max(najdluzsza, dt);
        if (czas >= 30) Zapisz();
    }
    void Zapisz()
    {
        if (czas < 1) return;
        Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "BRAXHORN_WYSPA_POMIAR seconds={0:F2} average_fps={1:F2} max_frame_ms={2:F2} unity_allocated_bytes={3}",
            czas, klatki / czas, najdluzsza * 1000, Profiler.GetTotalAllocatedMemoryLong()));
        czas = 0; klatki = 0; najdluzsza = 0;
    }
    void OnDisable() => Zapisz();
}
