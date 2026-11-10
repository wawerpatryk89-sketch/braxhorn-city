using UnityEngine;

// Owner 10.11 09:06-09:07: "nie poprawiłeś tego, jak te budynki się pokazują, jak się do nich zbliżasz ... stały problem".
// Cause: BraxGrafika (the game's three graphics modes, shared by every region) cuts each camera's view at 220 / 420 / 700
// world units on a phone (~170 / 325 / 540 m). Miasto Mistrzów is 1.2 x 1.5 km: the arena, towers, hotels, apartments and
// palms further away than that were simply not drawn and appeared at once while coming closer. The LOD changes could not
// fix it. This keeps the REGION camera's view over the whole region (2500 units ~ 1.93 km = the region diagonal); distant
// buildings are drawn with their own low LOD. Only on the region camera: other regions keep BraxGrafika's distance.
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class BraxChampionsZasiegWidzenia : MonoBehaviour
{
    // owner 10.11 19:51 ("daj panoramę w tle"): 18 km - the far coast, the mountain ridges and the sea to the horizon round
    // the region (BraxChampionsHorizon.ViewDistance); the city itself is all within its 1.93 km diagonal as before
    public float zasieg = 18000f;
    Camera kamera;
    bool zgloszone;

    void Awake() { kamera = GetComponent<Camera>(); }

    // LateUpdate: after BraxGrafika applies a mode (scene load, MENU change), before the frame is drawn.
    void LateUpdate()
    {
        if (!kamera || kamera.farClipPlane >= zasieg) return;
        if (!zgloszone)
        {
            zgloszone = true;
            Debug.Log("A1_VIEW_DISTANCE region keeps " + zasieg.ToString("F0") + " (graphics mode set " + kamera.farClipPlane.ToString("F0") + ")");
        }
        kamera.farClipPlane = zasieg;
    }
}
