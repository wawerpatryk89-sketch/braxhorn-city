using UnityEngine;

// Znacznik dziennika startu (1.11, Opus): pierwsze Awake w scenie gry. Razem z BraxStartLog dzieli wywalenie na etapy:
// „2a wczytywanie sceny” → (dane z dysku) → „2b 90% dane wczytane” → „2c włączanie sceny” (build 14: allowSceneActivation
// w BraxStart) → (integracja: tekstury/siatki na GPU) → „2- włączanie sceny: pierwsze Awake” → (Awake/OnEnable obiektów sceny) →
// „2 włączona scena” → (Start obiektów) → „4 pierwszy render” → (karta graficzna) → „5 po renderze” → „3 pierwsza klatka”.
[DefaultExecutionOrder(-32000)]
public sealed class BraxStartZnacznik : MonoBehaviour
{
    void Awake()
    {
        BraxStartLog.Etap("2- włączanie sceny " + gameObject.scene.name + ": pierwsze Awake");
    }
    void Start() { BraxStartLog.Etap("2+ pierwsze Start w scenie " + gameObject.scene.name); }
}
