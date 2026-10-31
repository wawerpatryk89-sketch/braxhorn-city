using UnityEngine;

// BRAXHORN R2 — kamera osiedla: bliska płaszczyzna dobierana co klatkę + cienie stabilne (migotanie w oddali):
//  • 0.25 m na otwartej przestrzeni: głębia 2.5× dokładniejsza (błąd = z² / (near · 2^24)) — pasy na asfalcie 1.6 cm
//    nad jezdnią przestają migotać nie od 164 m, tylko od 259 m, gdzie mgła (exp2 0.0042) zakrywa już ~70%;
//  • 0.1 m, gdy kamera jest blisko czegoś z kolizją: kolizja kamery gracza (SphereCast r 0.18, cofnięcie 0.15) zostawia ją
//    ok. 0.18 m od ściany bocznej, a rogi bliskiej płaszczyzny 0.25 m są dalej (0.40 m przy FOV 55° i 1280×576) — weszłyby
//    w ścianę i byłoby przez nią widać. Wejście na 0.1 m: kula = odległość rogu + 5 cm (z FOV i proporcji okna, więc też
//    telefon w pionie); powrót na 0.25 m dopiero przy rogu + 25 cm (histereza — bez przełączania co klatkę przy ścianie).
//  • cienie: dopasowanie StableFit i zasięg 60 m ustawiane przy starcie sceny osiedla (ustawienia jakości w edytorze
//    dotyczą tylko aktywnego poziomu jakości edytora — tu działa w grze na każdym poziomie).
// Bez warstwy gracza (8), bez wyzwalaczy. Kolejność 10000: po ustawieniu kamery przez BraxGymPlayer.LateUpdate.
[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(Camera))]
public class BraxClaudeKamera : MonoBehaviour
{
    public float daleko = .25f, blisko = .1f, wejscie = .05f, wyjscie = .25f, cienie = 60f;
    Camera cam;
    bool przyScianie;

    void Awake()
    {
        cam = GetComponent<Camera>();
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        if (QualitySettings.shadowDistance < cienie) QualitySettings.shadowDistance = cienie;
    }

    void LateUpdate()
    {
        if (!cam) return;
        float t = Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad), a = cam.aspect;
        float rog = daleko * Mathf.Sqrt(1f + t * t + t * t * a * a);          // odległość rogu bliskiej płaszczyzny od kamery
        przyScianie = Physics.CheckSphere(transform.position, rog + (przyScianie ? wyjscie : wejscie), ~(1 << 8), QueryTriggerInteraction.Ignore);
        float n = przyScianie ? blisko : daleko;
        if (cam.nearClipPlane != n) cam.nearClipPlane = n;
    }
}
