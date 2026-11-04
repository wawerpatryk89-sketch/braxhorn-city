using UnityEngine;

// SZLABAN PRZED TUNELEM DO MIASTA (build 14, osiedle). Aplikacja „osiedle” nie ma miasta w paczce (decyzja właściciela
// 1.11, 23:35: rejony osobno) — tunel to koniec rejonu: ramię opuszczone, blokada w poprzek wjazdu, tablica
// „MIASTO · WKRÓTCE”, przycisk AKCJA przy szlabanie: „TUNEL · MIASTO WKRÓTCE”. Gdy miasto jest w paczce (wariant łączony),
// ramię podnosi się, kiedy gracz podchodzi (18 m), i opada, gdy odejdzie; blokada tylko przy opuszczonym ramieniu.
public sealed class BraxSzlaban : MonoBehaviour, IBraxInterakcja
{
    public Transform ramie;
    public Collider blokada;
    public GameObject tablica;
    public Vector3 strefa = new Vector3(0f, 0f, 97f);
    public string miasto = "BraxhornCity";
    bool dostepne;
    float kat, szukaj;
    BraxGymPlayer gracz;

    void Start()
    {
        dostepne = BraxContent.Available(miasto);
        if (tablica) tablica.SetActive(!dostepne);
        if (blokada) blokada.enabled = true;
        BraxInterakcje.Dodaj(this);
        Debug.Log("BRAXHORN_TUNEL szlaban: " + (dostepne ? "miasto w paczce — podnosi się przy podejściu" : "zamknięty, MIASTO WKRÓTCE (aplikacja osiedla, koniec rejonu)"));
    }

    void OnDestroy() { BraxInterakcje.Usun(this); }

    void Update()
    {
        if (!dostepne) return;
        if (!gracz && Time.unscaledTime > szukaj) { szukaj = Time.unscaledTime + 1f; gracz = FindAnyObjectByType<BraxGymPlayer>(); }
        bool blisko = gracz && (gracz.transform.position - strefa).sqrMagnitude < 18f * 18f;
        kat = Mathf.MoveTowards(kat, blisko ? 82f : 0f, 40f * Time.deltaTime);
        if (ramie) ramie.localRotation = Quaternion.Euler(0f, 0f, -kat);
        if (blokada) blokada.enabled = kat < 60f;
    }

    public string Kontekst(Vector3 g)
    {
        if (dostepne) return "";
        var d = g - strefa; d.y = 0f;
        return d.magnitude < 6f ? "TUNEL · MIASTO WKRÓTCE" : "";
    }

    public bool Wykonaj(Vector3 g) => !dostepne && Kontekst(g).Length > 0;
}
