using UnityEngine;

// BRAXHORN R2 — płynąca woda bez cząsteczek (moduł ParticleSystem wyłączony): przesuwa teksturę materiału wzdłuż UV
// (strumień i przelew fontanny — v rośnie z biegiem wody, więc prędkość v ujemna). Własna kopia materiału na obiekt
// (Renderer.material w Awake), koszt: jedno dodawanie na klatkę. Tylko moduł rdzenia.
public class BraxClaudeWoda : MonoBehaviour
{
    public Vector2 predkosc = new Vector2(0f, -1f);   // UV na sekundę
    Material m; Vector2 off;

    void Awake()
    {
        var r = GetComponent<Renderer>();
        if (r) m = r.material;
    }

    void Update()
    {
        if (!m) return;
        off.x = Mathf.Repeat(off.x + predkosc.x * Time.deltaTime, 1f);
        off.y = Mathf.Repeat(off.y + predkosc.y * Time.deltaTime, 1f);
        m.mainTextureOffset = off;
    }

    void OnDestroy()
    {
        if (m) Destroy(m);
    }
}
