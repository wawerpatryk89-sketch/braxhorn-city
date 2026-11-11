// Runtime parts used by G1 (Unity only): the preview records their configuration fields.
using UnityEngine;
public class BraxSchodyRuchome : MonoBehaviour { public Vector3 krok; public float predkosc; }
public sealed class BraxSchodyRuchomeJazda : MonoBehaviour { public Vector3 gora, dol; public float szer = 1f; public bool wGore = true; public float predkosc = .5f; public float okresPoreczy = .5f; public Renderer[] porecze = new Renderer[0]; }
// the city's shop goods placer (BraxSklepyTowar, R1): in the preview the owner's pack is never there - nothing is placed
public static class BraxSklepyTowar
{
    public static int Postawione, Braki;
    public static bool Zastepuje(string typ) => true;
    public static bool Ma(string typ) => false;
    public static Transform Korzen(Transform bg, string szyld) { var t = new GameObject("Towar: " + szyld).transform; t.SetParent(bg, false); return t; }
    public static void Polka(string typ, Transform parent, Vector3 start, Vector3 wzdl, Vector3 wglab, float dl, System.Random rng, float maxWys = 99f) { Braki++; }
    public static void Lada(string typ, Transform parent, Vector3 start, Vector3 wzdl, Vector3 wglab, float dl, System.Random rng) { Braki++; }
    public static void Wozki(Transform parent, Transform kolizje, Vector3 p, Vector3 wzdl, int ile) { }
    public static void Zakoncz(Transform t, string szyld) { }
}
public sealed class BraxChampionsKino : MonoBehaviour
{
    public Renderer[] ekrany = new Renderer[0]; public Vector3[] salaMin = new Vector3[0], salaMax = new Vector3[0], glosnik = new Vector3[0];
    public int[] rzedy = new int[0], miejsca = new int[0]; public Renderer[] widok = new Renderer[0]; public int[] widokSala = new int[0];
    public Light[] swiatla = new Light[0]; public int[] swiatlaSala = new int[0]; public Renderer[] tablice = new Renderer[0], plakaty = new Renderer[0], drzwiEkrany = new Renderer[0];
    public Renderer[] fotele = new Renderer[0]; public int[] foteleSala = new int[0];
    public int[] plakatFilm = new int[0]; public Vector3 kiosk, kasa; public float zasieg = 1.8f; public string folder = "Mistrzowie/Kino", plikRepertuaru = "repertuar.json";
}

// a working TV screen (runtime BraxTelewizor, global): the preview keeps its fields only
public sealed class BraxTelewizor : MonoBehaviour { public bool wlaczony = true, pion; public float zasieg = 26f, zasiegPrzycisku = 2.8f; public Material obraz, wylaczony; public int start; }
