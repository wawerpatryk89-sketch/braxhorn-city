using System;
using UnityEngine;

// MIASTO MISTRZÓW - the casinos' service at the entrance (owner 16.11 20:29: "Porządna recepcja, konkretna, na maksa ...
// szatnia tam powinna być ... internet patrz i pokaż, jak się robi luksus"), working as in the real casinos:
//  - RECEPCJA (the players club desk of every big casino): AKCJA signs the player up to the BRAXHORN club card - the first
//    time the welcome free play of 1 000 credits (once), later the hostess greets him with his credit;
//  - SZATNIA (the coat check): AKCJA hands the jacket in - the attendant hangs it on a free hook and gives the numbered
//    tag (the number stays with the player, kept on the phone); AKCJA again gives it back for the tag;
//  - KASA (the cashier's cage): AKCJA changes 1 000 credits at the window (the casino credit until the game has its wallet).
// One component per casino holds its points (the generator places them: BraxChampionsKasynoSprzet).
public sealed class BraxChampionsKasynoObsluga : MonoBehaviour, IBraxInterakcja
{
    public const int Recepcja = 0, Szatnia = 1, Kasa = 2;
    [Serializable] public sealed class Punkt
    {
        public int rodzaj;
        public Transform miejsce;                    // where the guest stands (in front of the counter)
        public Transform kurtka;                     // the cloakroom: the player's jacket on its hook (shown while it hangs there)
    }
    public Punkt[] punkty = new Punkt[0];
    public string nazwa = "KASYNO";
    public const int Powitanie = 1000, Wymiana = 1000;
    const string KluczKlub = "braxhorn_kasyno_klub", KluczNumerek = "braxhorn_kasyno_szatnia_numerek";

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }
    void Start() { foreach (var p in punkty) if (p != null && p.kurtka) p.kurtka.gameObject.SetActive(Numerek > 0); }

    public static bool Klub => PlayerPrefs.GetInt(KluczKlub, 0) == 1;
    public static int Numerek => PlayerPrefs.GetInt(KluczNumerek, 0);

    int Najblizszy(Vector3 p)
    {
        int best = -1; float bd = float.MaxValue;
        for (int i = 0; i < punkty.Length; i++)
        {
            var q = punkty[i]; if (q == null || !q.miejsce) continue;
            float s = q.miejsce.lossyScale.x; var d = p - q.miejsce.position;
            if (Mathf.Abs(d.y) > 1.2f * s) continue; d.y = 0f;
            float dd = d.magnitude; if (dd < 1.1f * s && dd < bd) { bd = dd; best = i; }
        }
        return best;
    }
    public string Kontekst(Vector3 gracz)
    {
        int i = Najblizszy(gracz); if (i < 0) return "";
        switch (punkty[i].rodzaj)
        {
            case Recepcja: return Klub ? "RECEPCJA · KARTA KLUBU BRAXHORN" : "RECEPCJA · ZAŁÓŻ KARTĘ KLUBU · " + Powitanie + " NA START";
            case Szatnia: return Numerek > 0 ? "SZATNIA · ODBIERZ KURTKĘ · NUMEREK " + Numerek : "SZATNIA · ODDAJ KURTKĘ";
            default: return "KASA · WYMIEŃ " + Wymiana + " KREDYTÓW";
        }
    }
    public bool Wykonaj(Vector3 gracz)
    {
        int i = Najblizszy(gracz); if (i < 0) return false;
        var p = punkty[i];
        switch (p.rodzaj)
        {
            case Recepcja:
                if (!Klub)
                {
                    PlayerPrefs.SetInt(KluczKlub, 1); PlayerPrefs.Save(); BraxChampionsKasyno.Wplac(Powitanie);
                    BraxChampionsGry.Pokaz("WITAMY W KLUBIE BRAXHORN! +" + Powitanie + " KREDYTÓW", 3.2f);
                }
                else BraxChampionsGry.Pokaz("WITAMY PONOWNIE W " + nazwa + " · KREDYT " + BraxChampionsKasyno.Kredyt, 2.6f);
                break;
            case Szatnia:
                if (Numerek > 0)
                {
                    int n = Numerek; PlayerPrefs.SetInt(KluczNumerek, 0); PlayerPrefs.Save();
                    BraxChampionsGry.Pokaz("SZATNIA · KURTKA ODDANA ZA NUMEREK " + n, 2.6f);
                }
                else
                {
                    int n = 1 + (int)((uint)Environment.TickCount % 250u); PlayerPrefs.SetInt(KluczNumerek, n); PlayerPrefs.Save();
                    BraxChampionsGry.Pokaz("SZATNIA · TWÓJ NUMEREK: " + n, 3.0f);
                }
                foreach (var q in punkty) if (q != null && q.kurtka) q.kurtka.gameObject.SetActive(Numerek > 0);
                break;
            default:
                BraxChampionsKasyno.Wplac(Wymiana);
                BraxChampionsGry.Pokaz("KASA · +" + Wymiana + " KREDYTÓW · KREDYT " + BraxChampionsKasyno.Kredyt, 2.6f);
                break;
        }
        return true;
    }
}
