using UnityEngine;

// KRAN Z WODĄ (MIASTO R1, 11.11.2026 — właściciel 20:00: „od razu robimy wszystko funkcjonalne… Woda w kranie leci”).
// Strumień wody z perlatora (renderer z kodu) włącza się, gdy gracz stoi przy umywalce (≤ promien, ten sam poziom), i gaśnie,
// gdy odejdzie. Strumień lekko „żyje” (drganie grubości).
// Sprawdzanie co 0.15 s; poza zasięgiem zero pracy. Klasa w pliku o tej samej nazwie, pola publiczne bez #if (zasady 1–2).
public sealed class BraxKran : MonoBehaviour
{
    public Renderer strumien;
    public float promien = 1.4f;
    bool leci; float nast; Vector3 skala0 = Vector3.one;
    static Transform gracz; static float szukaj;

    void Awake() { if (strumien) { skala0 = strumien.transform.localScale; strumien.enabled = false; } }

    void Update()
    {
        if (Time.time >= nast)
        {
            nast = Time.time + .15f;
            if (!gracz && Time.unscaledTime >= szukaj) { szukaj = Time.unscaledTime + 1f; var p = FindAnyObjectByType<BraxGymPlayer>(); if (p) gracz = p.transform; }
            bool b = false;
            if (gracz) { var v = gracz.position - transform.position; b = Mathf.Abs(v.y + 1f) < 1.6f && new Vector2(v.x, v.z).magnitude < promien; }
            if (b != leci) { leci = b; if (strumien) strumien.enabled = b; Debug.Log("BRAXHORN_KRAN " + (b ? "woda leci" : "zakręcony") + ": " + name); }
        }
        if (leci && strumien)
        {
            float f = 1f + .12f * Mathf.Sin(Time.time * 31f) * Mathf.Sin(Time.time * 7.3f);
            strumien.transform.localScale = new Vector3(skala0.x * f, skala0.y, skala0.z * f);
        }
    }
}
