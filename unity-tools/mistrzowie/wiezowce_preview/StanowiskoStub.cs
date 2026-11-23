// The casino games' seat-play screen (BraxChampionsStanowisko: the camera to the machine / the table, the IMGUI buttons) as a
// stand-in: the previews draw no screen and the simulator plays the games' rules directly (Graj, Postaw, Zakrec ...). The
// API is the real one's (test_kasyno_stanowisko_20261123 compares the names); the IMGUI types it needs are here too, the
// previews have none.
using System.Collections.Generic;
using UnityEngine;

public interface IBraxStanowisko
{
    string Tytul(int i);
    void Kadr(int i, Camera kamera, out Vector3 oko, out Quaternion obrot);
    void Panel(int i, BraxChampionsStanowisko.Ui ui);
    void Wstal(int i);
}
public static class BraxChampionsStanowisko
{
    public static bool Aktywne; public static float OdWejscia;
    public static IBraxStanowisko Ostatnia; public static int OstatniIndeks = -1;
    public static readonly List<BraxSeat> Siedzenia = new List<BraxSeat>();
    public static bool Gra(IBraxStanowisko g, int i) => Aktywne && ReferenceEquals(Ostatnia, g) && OstatniIndeks == i;
    public static void Rejestruj(IBraxStanowisko g, BraxSeat s, int i) { if (s != null) Siedzenia.Add(s); }
    public static void Siedziska(Transform root) { }
    public static void Wejdz(IBraxStanowisko g, int i) { Ostatnia = g; OstatniIndeks = i; }
    public static void Wstan() { }
    public static int[] Stawki(int min) => new[] { min, 2 * min, 5 * min, 10 * min };
    public static void Kadruj(Camera cam, Vector3 c, Vector3 n, Vector3 gora, float w, float h, out Vector3 oko, out Quaternion obrot) { oko = c + n * h; obrot = Quaternion.identity; }
    public static void KadrStolu(Camera cam, Transform stol, Vector3 miejsce, Vector3 krupier, float szer, float glab, out Vector3 oko, out Quaternion obrot,
        float pochylenie = 50f, float kuKrupierowi = .45f) { oko = stol.TransformPoint(miejsce); obrot = Quaternion.identity; }
    public static void KadrBlatu(Camera cam, Transform stol, Vector3 srodek, float szer, float glab, float pochylenie, out Vector3 oko, out Quaternion obrot)
    { oko = stol.TransformPoint(srodek); obrot = Quaternion.identity; }
    public sealed class Ui
    {
        public static readonly Color Zloto = Color.white, ZlotoCiemne = Color.white, Szklo = Color.white, Krem = Color.white, Czerwien = Color.white,
            Zielen = Color.white, Szary = Color.white, Granat = Color.white, Przygasly = Color.white;
        public Rect ekran, gora, dol, wstan, kredyt; public float s = 1000f, m = 24f, pasGora = .12f, pasDol = .76f; public Camera kam;
        public bool Klik(Rect r) => false;
        public bool Przycisk(Rect r, string t, Color tlo, Color tekst, bool aktywny = true, bool glowny = false, Color? obrys = null) => false;
        public void Pole(Rect r, string podpis, string wartosc, Color kolor) { }
        public int Stawka(Rect r, int[] kroki, int idx, bool mozna, string podpis = "STAWKA") => idx;
        public void Tekst(Rect r, string t, float rozmiar, Color c, TextAnchor a = TextAnchor.MiddleCenter) { }
        public bool Rzut(Vector3 w, out Vector2 g) { g = Vector2.zero; return false; }
        public bool Dotyk(Vector3 a, Vector3 b, Vector3 c, Vector3 d) => false;
        public void Obrys(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color kol, float grub) { }
        public void Etykieta(Vector3 w, string t, Color tlo, Color tekst, float rozmiar = .024f) { }
    }
}
namespace UnityEngine
{
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public float xMax => x + width; public float yMax => y + height;
        public Vector2 center => new Vector2(x + width * .5f, y + height * .5f);
        public bool Contains(Vector2 p) => p.x >= x && p.x <= xMax && p.y >= y && p.y <= yMax;
    }
    public enum EventType { MouseDown, MouseUp, MouseDrag, Repaint, Layout }
    public class Event { public static Event current = new Event(); public EventType type = EventType.Layout; public Vector2 mousePosition; public bool isMouse; public void Use() { } }
}
