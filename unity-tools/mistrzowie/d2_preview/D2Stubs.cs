// Runtime parts of D2 (Unity only): the preview records their configuration fields.
using UnityEngine;
public sealed class BraxChampionsZegar : MonoBehaviour { public Transform godzinowa, minutowa, sekundowa; }
public sealed class BraxChampionsD2Pociag : MonoBehaviour { public float wjazdX, wyjazdX, drugiTorZ, predkosc, hamowanie, postoj, przerwa; public int Tor => 0; public int Kurs => 0; public bool Stoi => true; public bool Wjezdza => false; public float DoOdjazdu => 40f; }
