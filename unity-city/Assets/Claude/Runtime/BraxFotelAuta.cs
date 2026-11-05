using UnityEngine;

// Fotel w aucie: zmierzony prześwit kabiny (jednostki świata), zapisany przy budowie auta (BraxClaudeOsiedleAutaModel).
// Build 45 (właściciel 2.11 20:28: „każdy człowiek powinien się zmieścić do auta”): oprócz prześwitu nad biodrem —
// MAPA DACHU nad tym konkretnym miejscem (kierowca / pasażer osobno): siatka nx × nz w układzie fotela (x w prawo, z do
// przodu, od punktu siedziska), wartość = wysokość wewnętrznej strony dachu nad siedziskiem. Głowa odchylonej osoby jest
// za biodrem (bliżej słupka B, gdzie dach opada) — liczy się dach nad głową, nie średnia ani punkt nad biodrem.
// kabina: sportowe (odchylenie oparcia 25–30°), osobowe (18–22°), dostawcze (8–12°) — z wysokości auta.
public sealed class BraxFotelAuta : MonoBehaviour
{
    public float przeswit = .95f;
    public string kabina = "osobowe";
    public float odchylenieBaza = 18f, odchylenieMax = 22f;
    public float mapaX0, mapaZ0, mapaKrok;
    public int mapaNx, mapaNz;
    public float[] mapa = new float[0];

    public bool MaMape => mapa != null && mapaNx >= 2 && mapaNz >= 2 && mapa.Length == mapaNx * mapaNz && mapaKrok > 0f;

    // Dach nad punktem (x, z) w układzie fotela; ostrożnie: minimum z czterech sąsiednich węzłów siatki (krawędź dachu,
    // słupek). Poza siatką — najbliższy brzeg. Bez mapy — prześwit nad biodrem.
    public float Dach(float x, float z)
    {
        if (!MaMape) return przeswit;
        float fx = Mathf.Clamp((x - mapaX0) / mapaKrok, 0f, mapaNx - 1), fz = Mathf.Clamp((z - mapaZ0) / mapaKrok, 0f, mapaNz - 1);
        int ix = Mathf.Min((int)fx, mapaNx - 2), iz = Mathf.Min((int)fz, mapaNz - 2);
        return Mathf.Min(Mathf.Min(mapa[iz * mapaNx + ix], mapa[iz * mapaNx + ix + 1]), Mathf.Min(mapa[(iz + 1) * mapaNx + ix], mapa[(iz + 1) * mapaNx + ix + 1]));
    }

    // Rodzaj kabiny i zakres odchylenia oparcia z realnej wysokości auta (m, bez skali świata).
    public static void Rodzaj(float wysokoscRealna, out string kabina, out float baza, out float max)
    {
        if (wysokoscRealna < 1.30f) { kabina = "sportowe"; baza = 25f; max = 30f; }
        else if (wysokoscRealna > 1.65f) { kabina = "dostawcze"; baza = 8f; max = 12f; }
        else { kabina = "osobowe"; baza = 18f; max = 22f; }
    }
}
