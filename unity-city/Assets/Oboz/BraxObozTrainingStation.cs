using UnityEngine;

// Stanowiska sa dostepne bez opuszczania obozu i bez zmiany kontrolera postaci.
// Liczniki serii nie zastepuja jeszcze animacji wyspecjalizowanych pod hantle.
public sealed class BraxObozTrainingStation : MonoBehaviour, IBraxInterakcja
{
    public string label;
    public Vector2 footprint;
    public BraxGymPlayer player;
    public float sessionSeconds = 10f;
    public int completedSets;
    float finishAt;
    bool exercising;

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); exercising = false; }
    bool Nearby(Vector3 p)
    {
        Vector3 d=p-transform.position;
        d.y=0;
        float radius=Mathf.Clamp(Mathf.Min(footprint.x,footprint.y)*.38f,1.3f,3.2f);
        return d.sqrMagnitude<(radius*radius);
    }
    public string Kontekst(Vector3 p)
    {
        if (!player || !player.ready || player.Paused || BraxObozArena.AnyActive || !Nearby(p))return "";
        return exercising ? "ZAKOŃCZ TRENING · "+label : "ROZPOCZNIJ TRENING · "+label;
    }
    public bool Wykonaj(Vector3 p)
    {
        if (string.IsNullOrEmpty(Kontekst(p)))return false;
        if (exercising)
        {
            exercising=false;
            BraxSport.Pokaz("TRENING ZAKOŃCZONY · "+label,2);
            return true;
        }
        exercising=true;finishAt=Time.time+Mathf.Max(3f,sessionSeconds);
        // Zrobiono interakcje; animacja treningowa jest tylko jesli istnieje
        // kompatybilny klip. Brak klipu nie wykrzywia szkieletu i nie blokuje APK.
        if (player.clips && player.clips["Training"]!=null)
            player.clips.CrossFade("Training",.18f,PlayMode.StopAll);
        BraxSport.Pokaz("TRENING · "+label,3);
        return true;
    }
    void Update()
    {
        if (!exercising)return;
        if (!player || player.Paused || !Nearby(player.transform.position))
        {
            exercising=false;
            return;
        }
        if (Time.time<finishAt)return;
        exercising=false;completedSets++;
        BraxSport.Pokaz("SERIA UKOŃCZONA · "+label+" · "+completedSets,3);
    }
}
