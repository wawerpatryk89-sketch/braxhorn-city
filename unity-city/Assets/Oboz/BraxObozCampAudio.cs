using System;
using System.Collections.Generic;
using UnityEngine;

// Pre-rendered Piper lines, shared BraxGlosPostaci playback, never text UI.
// One exchange at a time, audibility and proximity checked for each turn.
public sealed class BraxObozCampAudio : MonoBehaviour
{
    [Serializable] public sealed class Line { public string id, voice; public AudioClip clip; }
    public Line[] lines = new Line[0];
    public BraxObozTrainingRoutine routine;
    public int SpokenLines { get; private set; }
    public int Exchanges { get; private set; }
    public int Contacts { get; private set; }
    public bool Paused { get; private set; }
    struct Turn { public Transform actor; public string id; }
    readonly Queue<Turn> pending = new Queue<Turn>();
    readonly Dictionary<Transform,BraxGlosPostaci> speakers = new Dictionary<Transform,BraxGlosPostaci>();
    float wait, nextExchange = 5;
    int conversation, acknowledgements;
    Transform partnerA, partnerB;

    void Update()
    {
        if (!routine || !routine.player || !routine.player.ready) return;
        bool paused = routine.player.Paused;
        if (paused != Paused)
        {
            Paused = paused;
            foreach (var voice in speakers.Values) if (voice) voice.Pauza(paused);
        }
        if (paused) return;
        if (!routine.Running) { StopExchange(); return; }
        // Audio duration is wall time: physics' 50 ms cap would stretch conversational
        // gaps on slower phones although the WAV itself still plays at normal speed.
        float dt = Time.deltaTime; wait -= dt; nextExchange -= dt;
        if (partnerA && partnerB && Vector3.Distance(partnerA.position,partnerB.position)>8*BraxGlosPostaci.K)
            StopExchange();
        if (pending.Count>0 && wait<=0)
        {
            var turn=pending.Dequeue();
            if (!turn.actor || !Audible(turn.actor)) { StopExchange(); return; }
            string key=Voice(turn.actor); AudioClip clip=null;
            foreach(var entry in lines) if(entry.id==turn.id && entry.voice==key){clip=entry.clip;break;}
            if (!clip) { Debug.LogError("Brak nagrania obozu: "+key+"/"+turn.id,this); StopExchange(); return; }
            if (!speakers.TryGetValue(turn.actor,out var speaker))
            {
                speaker=turn.actor.GetComponent<BraxGlosPostaci>();
                if(!speaker)speaker=turn.actor.gameObject.AddComponent<BraxGlosPostaci>();
                speakers[turn.actor]=speaker;
            }
            speaker.Mow(clip,BraxGlosPostaci.Rodzaj.Okrzyk);
            wait=clip.length+.35f; SpokenLines++; return;
        }
        if (pending.Count>0 || wait>0 || nextExchange>0) return;
        for(int pass=0;pass<2;pass++)
        {
            int first=((conversation+pass)%2==0)?0:4;
            if(first==0 && BraxObozArena.AnyActive)continue;
            BraxObozNpc a=null,b=null;
            foreach(var npc in routine.participants)
            { int role=routine.RoleOf(npc); if(role==first)a=npc;if(role==first+1)b=npc; }
            if(!a||!b||!a.AtDestination||!b.AtDestination||!Audible(a.transform)||!Audible(b.transform))continue;
            int variation=(conversation/2)%2;
            string prefix=first==0?"spar":"rest";
            if(StartExchange(a.transform,prefix+(variation*2),b.transform,prefix+(variation*2+1)))
            {conversation++;return;}
        }
        nextExchange=3;
    }
    bool Audible(Transform actor) => (actor.position-routine.player.transform.position).sqrMagnitude <
        BraxGlosPostaci.Cisza*BraxGlosPostaci.Cisza;
    bool StartExchange(Transform a,string first,Transform b,string second)
    {
        if(Vector3.Distance(a.position,b.position)>8*BraxGlosPostaci.K)return false;
        StopExchange(); partnerA=a;partnerB=b;
        pending.Enqueue(new Turn{actor=a,id=first});pending.Enqueue(new Turn{actor=b,id=second});
        Exchanges++;nextExchange=24;return true;
    }
    public void Order(BraxObozNpc npc,int target)
    {
        if(!npc||!Audible(npc.transform))return;
        StartExchange(routine.player.transform,target<2?"orderSpar":target<4?"orderBag":"orderRest",
            npc.transform,"ack"+(acknowledgements++%2));
    }
    public void Contact(BraxObozNpc npc,bool block)
    {
        if(!npc)return;
        var sound=npc.GetComponent<BraxObozContactAudio>();
        if(sound){sound.Contact(block);Contacts++;}
    }
    string Voice(Transform actor)
    {
        if(actor==routine.player.transform)return "pl_PL-gosia-medium";
        var npc=actor.GetComponent<BraxObozNpc>();
        string id=npc?npc.characterId:"";
        return id=="mis"||id=="gorr"?"pl_PL-bass-high":
            id=="zyro"||id=="rakk"?"pl_PL-mc_speech-medium":"pl_PL-darkman-medium";
    }
    public void StopExchange()
    {
        pending.Clear(); wait=0; partnerA=partnerB=null;
        foreach(var voice in speakers.Values)if(voice)voice.Zatrzymaj();
    }
    public void FinishCurrentTurn()
    {pending.Clear();partnerA=partnerB=null;}
    void OnDisable(){StopExchange();}
}
