using System;
using UnityEngine;

// Runtime elevator for the preserved city controller. Its carriage genuinely
// travels the shaft; the player rides the moving floor without a position warp.
// No movement, camera, rig or animation implementation is replaced.
[DefaultExecutionOrder(150)]
public sealed class BraxCityElevator : MonoBehaviour
{
    public BraxGymPlayer player;
    public Transform cabin;
    public Transform cabinDoorLeft, cabinDoorRight;
    public Transform lowerDoorLeft, lowerDoorRight, upperDoorLeft, upperDoorRight;
    public Vector3 lowerCall, upperCall;
    public float lowerY, upperY, entranceZ;
    public Vector2 shaftCenter;

    enum Phase { Closed, Opening, Open, Closing, Moving }
    Phase phase = Phase.Closed;
    int currentFloor, destination;
    float openness;
    bool passenger;
    Vector3 cabinLeftClosed, cabinRightClosed, lowerLeftClosed, lowerRightClosed;
    Vector3 upperLeftClosed, upperRightClosed;
    const float Speed = 5.8f;
    const float DoorSeconds = 1.35f;
    const float InteractionRadius = 2.75f;

    public bool IsTravelling { get { return phase == Phase.Moving; } }
    public float CurrentY { get { return cabin ? cabin.position.y : lowerY; } }
    public string Context
    {
        get
        {
            if(!player || !cabin) return "";
            Vector3 at = player.transform.position;
            if (phase == Phase.Moving && passenger) return "WINDA — PRZEJAZD";
            if (!NearCabin(at) && !NearCall(at,0) && !NearCall(at,1)) return "";
            if (phase == Phase.Opening || phase == Phase.Closing) return "WINDA — DRZWI";
            if (NearCabin(at) && phase == Phase.Open)
                return currentFloor == 0 ? "WJEDŹ NA DACH" : "ZJEDŹ NA ULICĘ";
            if (NearCabin(at)) return "OTWÓRZ DRZWI WINDY";
            return "PRZYWOŁAJ / OTWÓRZ WINDĘ";
        }
    }

    void Awake()
    {
        if(!player || !player.body || !cabin || !cabinDoorLeft || !cabinDoorRight ||
           !lowerDoorLeft || !lowerDoorRight || !upperDoorLeft || !upperDoorRight ||
           upperY <= lowerY + 10f)
            throw new InvalidOperationException("City elevator is missing real cabin, doors or player controller");
        cabinLeftClosed=cabinDoorLeft.localPosition;
        cabinRightClosed=cabinDoorRight.localPosition;
        lowerLeftClosed=lowerDoorLeft.localPosition;
        lowerRightClosed=lowerDoorRight.localPosition;
        upperLeftClosed=upperDoorLeft.localPosition;
        upperRightClosed=upperDoorRight.localPosition;
        currentFloor=0;destination=0;
        SetDoors(0f);
    }

    bool NearCall(Vector3 at,int floor)
    {
        Vector3 call=floor==0?lowerCall:upperCall;
        if(Mathf.Abs(at.y-(floor==0?lowerY:upperY))>2.3f)return false;
        return Vector2.Distance(new Vector2(at.x,at.z),new Vector2(call.x,call.z))<InteractionRadius;
    }
    bool NearCabin(Vector3 at)
    {
        if(!cabin || Mathf.Abs(at.y-cabin.position.y)>2.3f)return false;
        return Mathf.Abs(at.x-shaftCenter.x)<1.24f &&
               Mathf.Abs(at.z-shaftCenter.y)<1.20f;
    }
    void SetDoors(float fraction)
    {
        openness=Mathf.Clamp01(fraction);
        // Leaves slide into their pockets. When the carriage is absent,
        // a landing remains physically closed and collidable.
        cabinDoorLeft.localPosition=cabinLeftClosed+Vector3.left*(1.53f*openness);
        cabinDoorRight.localPosition=cabinRightClosed+Vector3.right*(1.53f*openness);
        float lo=currentFloor==0&&phase!=Phase.Moving?openness:0f;
        float hi=currentFloor==1&&phase!=Phase.Moving?openness:0f;
        lowerDoorLeft.localPosition=lowerLeftClosed+Vector3.left*(1.53f*lo);
        lowerDoorRight.localPosition=lowerRightClosed+Vector3.right*(1.53f*lo);
        upperDoorLeft.localPosition=upperLeftClosed+Vector3.left*(1.53f*hi);
        upperDoorRight.localPosition=upperRightClosed+Vector3.right*(1.53f*hi);
    }

    public bool TryInteract(Vector3 at)
    {
        if (!NearCabin(at) && !NearCall(at,0) && !NearCall(at,1)) return false;
        if (phase == Phase.Moving || phase == Phase.Opening || phase == Phase.Closing) return true;
        if (NearCabin(at))
        {
            if (phase == Phase.Closed) { phase=Phase.Opening; return true; }
            destination=1-currentFloor;
            passenger=true;
            player.MoveInput("0,0");
            phase=Phase.Closing;
            return true;
        }
        int requested=NearCall(at,0)?0:1;
        if (requested != currentFloor)
        {
            destination=requested;
            passenger=false;
            phase=Phase.Closing;
        }
        else phase=Phase.Opening;
        return true;
    }

    void Update()
    {
        if(!player || !cabin)return;
        // Keep doors on game-frame time, even with slow software-rendered
        // WebGL frames. Only each physical lift substep is capped at 50 ms.
        float delta=Mathf.Min(Time.deltaTime,.333f);
        switch(phase)
        {
            case Phase.Opening:
                SetDoors(openness+delta/DoorSeconds);
                if(openness>=.999f){SetDoors(1);phase=Phase.Open;}
                break;
            case Phase.Closing:
                SetDoors(openness-delta/DoorSeconds);
                if(openness<=.001f)
                {
                    SetDoors(0);
                    if(destination!=currentFloor)
                    {
                        if(passenger && !NearCabin(player.transform.position))
                        { passenger=false;destination=currentFloor;phase=Phase.Opening;break; }
                        phase=Phase.Moving;
                    }
                    else phase=Phase.Closed;
                }
                break;
            case Phase.Moving:
                float target=destination==0?lowerY:upperY;
                float remaining=delta;
                if(passenger)player.MoveInput("0,0");
                // Move the same physical car and original player controller in
                // short measured substeps, never teleporting either endpoint.
                while(remaining>.0001f)
                {
                    float step=Mathf.Min(remaining,.05f);
                    remaining-=step;
                    float before=cabin.position.y;
                    float after=Mathf.MoveTowards(before,target,Speed*step);
                    cabin.position+=Vector3.up*(after-before);
                    if(passenger)
                    {
                        player.body.Move(Vector3.up*(after-before));
                        if(Mathf.Abs(player.transform.position.y-after)>.65f)
                        {
                            Debug.LogError("CITY_LIFT_SAFETY_STOP player lost the physical moving floor");
                            enabled=false;
                            return;
                        }
                    }
                    if(Mathf.Abs(after-target)<.002f)
                    {
                        currentFloor=destination;
                        phase=Phase.Opening;
                        passenger=false;
                        break;
                    }
                }
                break;
        }
    }
}
