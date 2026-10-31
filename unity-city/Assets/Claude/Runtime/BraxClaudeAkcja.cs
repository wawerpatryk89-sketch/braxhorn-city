using UnityEngine;

// BRAXHORN R2 — przekaźnik przycisku akcji: strona gry wysyła SendMessage('BraxhornPlayer', 'Command', 'action');
// SendMessage woła metodę Command na WSZYSTKICH komponentach obiektu gracza, więc ten komponent (dokładany w grze do
// obiektu z BraxGymPlayer) dostaje ten sam przycisk bez zmian w skrypcie gracza. Przy wieży: włącz/wyłącz muzykę.
public class BraxClaudeAkcja : MonoBehaviour
{
    public void Command(string key)
    {
        if (key == "action" && BraxClaudeGrajek.Blisko) BraxClaudeGrajek.Blisko.Przelacz();
    }
}
