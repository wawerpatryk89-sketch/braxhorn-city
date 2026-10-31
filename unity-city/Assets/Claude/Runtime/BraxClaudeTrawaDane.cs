using UnityEngine;

// BRAXHORN R2 — dane kęp trawy (warstwa 2): jedna kępa = (x, z, obrót w stopniach, skala) + wysokość nasady y.
// Zapisywane przez edytor (BraxClaudeOsiedle) jako zasób; rysuje je BraxClaudeTrawa.
// Wysokość kępy w metrach = skala × wysokosc; powyżej granicaLaki to łąka (dalszy zasięg, wiatr), poniżej murawa.
public class BraxClaudeTrawaDane : ScriptableObject
{
    public Vector4[] kepy = new Vector4[0];
    public float[] y = new float[0];       // wysokość nasady (trawnik A −0.01, B −0.02, minus zagłębienie na przechył)
    public float wysokosc = .32f;          // wysokość kępy przy skali 1 (m)
    public float granicaLaki = .15f;       // m: wyżej = łąka
}
