// Runtime parts of D2 (Unity only): the preview records their configuration fields.
using UnityEngine;
public sealed class BraxChampionsZegar : MonoBehaviour { public Transform godzinowa, minutowa, sekundowa; }
public sealed class BraxChampionsD2Pociag : MonoBehaviour { public float wjazdX, wyjazdX, drugiTorZ, predkosc, hamowanie, postoj, przerwa; public bool staly; public int Tor => 0; public int Kurs => 0; public bool Stoi => true; public bool Wjezdza => false; public float DoOdjazdu => 40f; }
public sealed class BraxChampionsD2DrzwiPociagu : MonoBehaviour { public Transform[] skrzydla; public Vector3[] wysuw, przesuw; public Transform stopien; public Vector3 stopienWysuw; public float promien, czas; public Collider[] blokady; }
