using UnityEngine;

// Editor/desktop exploration only. Android uses the existing estate touch HUD.
// No replacement movement/camera implementation: all commands go to BraxGymPlayer.
[DisallowMultipleComponent]
public sealed class BraxChampionsDesktopInput : MonoBehaviour
{
    public BraxGymPlayer player;
#if UNITY_EDITOR || UNITY_STANDALONE
    Vector3 lastMouse;
    bool looking, paused, focused = true;
    Vector2 lastMove;
    void Update()
    {
        if(!player||!player.ready||!focused)return;
        if(Input.GetKeyDown(KeyCode.Escape)){paused=!paused;player.Command("pause");Stop();}
        if(paused)return;
        float x=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
        float y=(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0);
        var move=Vector2.ClampMagnitude(new Vector2(x,y),1f)*(Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift)?1f:.65f);
        if(move!=lastMove)
        {
            string value=move.x.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+","+move.y.ToString("F2",System.Globalization.CultureInfo.InvariantCulture);
            player.MoveInput(value);lastMove=move;
        }
        if(Input.GetMouseButton(1))
        {
            if(looking){var d=Input.mousePosition-lastMouse;player.LookDelta(d.x*.16f,-d.y*.12f);}
            lastMouse=Input.mousePosition;looking=true;
        }
        else looking=false;
        if(Input.GetKeyDown(KeyCode.Space))player.Command("jump");
        if(Input.GetKeyDown(KeyCode.E))player.Command("action");
        if(Input.GetKeyDown(KeyCode.R))player.Command("reset");
    }
    void Stop(){if(player)player.MoveInput("0,0");lastMove=Vector2.zero;looking=false;}
    void OnApplicationFocus(bool focus){focused=focus;if(!focus)Stop();}
    void OnDisable(){Stop();}
#endif
}
