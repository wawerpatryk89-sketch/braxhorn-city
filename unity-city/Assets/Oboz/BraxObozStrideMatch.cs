using System;
using System.Collections.Generic;
using UnityEngine;

// Executes after the shared movement controllers and before animation sampling.
// Only camp Walk/Run playback is matched; original clips and movement speeds stay intact.
[DefaultExecutionOrder(100)]
public sealed class BraxObozStrideMatch : MonoBehaviour
{
    public BraxGymPlayer player;
    public BraxObozNpc npc;
    public CharacterController body;
    public Animation clips;
    [SerializeField] float walkWorldSpeed,runWorldSpeed;
    public float WalkWorldSpeed => walkWorldSpeed;
    public float RunWorldSpeed => runWorldSpeed;
    public bool Matching {get;private set;}
    public float ObservedWorldSpeed {get;private set;}
    public float AppliedRate {get;private set;}

    AnimationState walk,run,idle;
    AnimationState[] actions=Array.Empty<AnimationState>();
    Vector3 previousPosition;
    bool positionKnown,initialized,ownsWalk,ownsRun;
    float beforeWalk,beforeRun;

    public void Configure(BraxGymPlayer ownerPlayer,BraxObozNpc ownerNpc,
        CharacterController capsule,Animation animation,float measuredWalk,float measuredRun)
    {
        player=ownerPlayer;npc=ownerNpc;body=capsule;clips=animation;
        walkWorldSpeed=measuredWalk;runWorldSpeed=measuredRun;
        initialized=false;positionKnown=false;
    }

    void Initialize()
    {
        if(initialized || !clips)return;
        walk=clips["Walk"];run=clips["Run"];idle=clips["Idle"];
        var other=new List<AnimationState>();
        foreach(AnimationState state in clips)
            if(state.name!="Walk" && state.name!="Run" && state.name!="Idle")other.Add(state);
        actions=other.ToArray();initialized=true;
    }

    public static bool TryPlaybackRate(float actualSpeed,float measuredSpeed,bool canMatch,out float rate)
    {
        rate=0f;
        if(!canMatch || measuredSpeed<=0f || actualSpeed<0f ||
            float.IsNaN(actualSpeed) || float.IsInfinity(actualSpeed) ||
            float.IsNaN(measuredSpeed) || float.IsInfinity(measuredSpeed))return false;
        rate=actualSpeed/measuredSpeed;
        return !float.IsNaN(rate) && !float.IsInfinity(rate);
    }

    bool ActionActive()
    {
        if(npc && npc.Busy)return true;
        if(!npc && BraxObozArena.AnyActive)return true;
        foreach(var state in actions)
            if(state.enabled && state.weight>.001f)return true;
        return false;
    }

    static bool Active(AnimationState state)=>state!=null && state.enabled && state.weight>.001f;
    void ReleaseRates()
    {
        if(clips)
        {
            if(ownsWalk && walk!=null)walk.speed=beforeWalk;
            if(ownsRun && run!=null)run.speed=beforeRun;
        }
        ownsWalk=ownsRun=false;Matching=false;
    }

    void OnEnable(){previousPosition=transform.position;positionKnown=true;}
    void Update()
    {
        Initialize();
        Vector3 position=transform.position;
        Vector3 moved=position-previousPosition;moved.y=0f;
        previousPosition=position;
        if(!positionKnown){positionKnown=true;return;}
        // Keep the shared pause's zero speeds. No jump, reaction or combat clip
        // is sampled, modified or retimed by this component.
        if(player && player.Paused){Matching=false;ObservedWorldSpeed=0f;return;}
        if(!body || !body.enabled || !clips || !initialized || !player || !player.ready ||
            Time.deltaTime<=0f || moved.sqrMagnitude>body.height*body.height*4f)
        {ReleaseRates();return;}
        AnimationState state=Active(walk)?walk:Active(run)?run:null;
        bool stable=state!=null && state.weight>=.999f && !Active(idle) &&
            !(Active(walk)&&Active(run));
        bool canMatch=stable && body.isGrounded && !ActionActive() &&
            (npc || (!player.seat && !player.vessel && !player.autoGracza));
        ObservedWorldSpeed=moved.magnitude/Time.deltaTime;
        float measured=state==walk?walkWorldSpeed:runWorldSpeed;
        if(!TryPlaybackRate(ObservedWorldSpeed,measured,canMatch,out float rate))
        {ReleaseRates();return;}
        // Root displacement includes every CharacterController.Move substep.
        // A blocked capsule produces rate 0, rather than walking over one spot.
        if(state==walk)
        {
            if(ownsRun && run!=null){run.speed=beforeRun;ownsRun=false;}
            if(!ownsWalk || !npc)beforeWalk=walk.speed;
            ownsWalk=true;walk.speed=rate;
        }
        else
        {
            if(ownsWalk && walk!=null){walk.speed=beforeWalk;ownsWalk=false;}
            if(!ownsRun || !npc)beforeRun=run.speed;
            ownsRun=true;run.speed=rate;
        }
        Matching=true;AppliedRate=rate;
    }

    void OnDisable()
    {
        if(!player || !player.Paused)ReleaseRates();
        else{ownsWalk=ownsRun=false;Matching=false;}
        positionKnown=false;
    }
}
