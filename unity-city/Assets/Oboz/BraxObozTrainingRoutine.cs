using System;
using UnityEngine;
using UnityEngine.AI;

// Six authored places: two in the cage, two at bags, two resting nearby.
// Rotating occupants share one assignment owner; an order swaps roles, never clones actors.
public sealed class BraxObozTrainingRoutine : MonoBehaviour
{
    public BraxGymPlayer player;
    public BraxObozNpc[] participants = new BraxObozNpc[0];
    public Transform[] places = new Transform[0];
    public BraxWorek[] trainingBags = new BraxWorek[0];
    public NavMeshData navigationData;
    public BraxObozCampAudio audio;
    public float roundSeconds = 45;
    public bool Running { get; private set; }
    public int CompletedRounds { get; private set; }
    public int RoleChanges { get; private set; }
    public int BagStrikes { get; private set; }
    public int SparringStrikes { get; private set; }
    public int Orders { get; private set; }
    public string Status { get; private set; } = "ZBIÓRKA";
    public bool SuspendedForPlayer { get; private set; }
    readonly int[] occupants = { 0, 1, 2, 3, 4, 5 };
    readonly float[] bagWait = new float[2], bagImpact = { -1, -1 };
    NavMeshDataInstance navigationInstance;
    bool sparring, strikePending;
    int attacker;
    float remaining, attackWait, impactLeft, recovery, followThrough;

    void OnEnable()
    {
        if (navigationData) navigationInstance = NavMesh.AddNavMeshData(navigationData);
    }
    void Start()
    {
        if (!player || !navigationData || participants.Length != 6 || places.Length != 6 || trainingBags.Length != 2)
        { Debug.LogError("Niepełny plan zajęć obozu", this); enabled = false; return; }
        for (int i = 0; i < 6; i++)
        {
            var npc = participants[i];
            if (!npc || !places[i] || !npc.navigation || npc.routine != this)
            { Debug.LogError("Niepełne podłączenie planu zajęć", this); enabled = false; return; }
            npc.navigation.enabled = true;
            if (!npc.navigation.isOnNavMesh)
            { Debug.LogError("NPC poza siatką przejść: " + npc.characterId, this); enabled = false; return; }
        }
        Running = true; AssignPlaces();
    }
    void OnDisable()
    {
        Running = false;
        foreach (var npc in participants)
        {
            if (!npc) continue;
            npc.EndSparring();
            if (npc.navigation) npc.navigation.enabled = false;
        }
        if (navigationInstance.valid) navigationInstance.Remove();
    }
    public int RoleOf(BraxObozNpc npc)
    {
        for (int slot = 0; slot < occupants.Length; slot++) if (participants[occupants[slot]] == npc) return slot;
        return -1;
    }
    public string NextOrderLabel(BraxObozNpc npc)
    {
        int slot = RoleOf(npc);
        return slot < 2 ? "ODPOCZYNEK" : slot < 4 ? "SPARING" : "WOREK";
    }
    public bool OrderNext(BraxObozNpc npc)
    {
        int slot = RoleOf(npc); if (!Running || slot < 0 || player.Paused) return false;
        int target = slot < 2 ? 4 : slot < 4 ? 0 : 2;
        int other = occupants[target]; occupants[target] = occupants[slot]; occupants[slot] = other;
        Orders++; RoleChanges += 2; AssignPlaces();
        npc.React("Taunt");
        if(audio)audio.Order(npc,target);
        return true;
    }
    void AssignPlaces()
    {
        if(audio)audio.FinishCurrentTurn();
        sparring = strikePending = false; remaining = roundSeconds; attackWait = 1.2f; recovery = 0;
        for (int i = 0; i < 2; i++) { bagWait[i] = .5f + i * .45f; bagImpact[i] = -1; }
        for (int slot = 0; slot < 6; slot++)
        {
            var npc = participants[occupants[slot]];
            bool jog = slot >= 4 && CompletedRounds % 2 == 1;
            if (!npc.TravelTo(places[slot].position, jog))
            { Debug.LogError("Nie wyznaczono drogi do zajęcia: " + npc.characterId, this); Running = false; }
        }
        Status = "ZMIANA STANOWISK";
    }
    void Update()
    {
        if (!Running || !player || !player.ready || player.Paused) return;
        if (BraxObozArena.AnyActive)
        {
            if (!SuspendedForPlayer)
            {
                SuspendedForPlayer = true;
                foreach (var npc in participants) if (!BraxObozArena.Uses(npc)) npc.HoldRoute();
            }
            return;
        }
        if (SuspendedForPlayer) { SuspendedForPlayer = false; AssignPlaces(); }
        float dt = Mathf.Min(Time.deltaTime, .05f);
        for (int i = 0; i < 2; i++) TickBag(i, dt);
        var restingA=participants[occupants[4]];var restingB=participants[occupants[5]];
        if(restingA.AtDestination&&restingB.AtDestination)
        {restingA.Face(restingB.transform.position,dt);restingB.Face(restingA.transform.position,dt);}
        var first = participants[occupants[0]]; var second = participants[occupants[1]];
        if (!sparring)
        {
            if (!first.AtDestination || !second.AtDestination || first.Reacting || second.Reacting) return;
            first.BeginSparring(); second.BeginSparring(); sparring = true; attacker = CompletedRounds % 2;
            Status = "SPARING · RUNDA " + (CompletedRounds + 1);
        }
        first.Face(second.transform.position, dt); second.Face(first.transform.position, dt);
        remaining -= dt;
        if (remaining <= 0)
        {
            CompletedRounds++;
            int previous = occupants[0]; for (int i = 0; i < 5; i++) occupants[i] = occupants[i + 1]; occupants[5] = previous;
            RoleChanges += 6; AssignPlaces(); return;
        }
        if (recovery > 0) { recovery -= dt; return; }
        var from = attacker == 0 ? first : second; var to = attacker == 0 ? second : first;
        if (strikePending)
        {
            impactLeft -= dt;
            if (impactLeft > 0) return;
            strikePending = false;
            Vector3 delta = to.transform.position - from.transform.position; delta.y = 0;
            if (delta.sqrMagnitude < 2.4f * 2.4f && Vector3.Dot(from.transform.forward, delta.normalized) > .6f &&
                !Physics.Linecast(from.transform.position + Vector3.up * 1.5f, to.transform.position + Vector3.up * 1.5f, ~(1 << 8), QueryTriggerInteraction.Ignore))
            {
                bool blocked=SparringStrikes % 3 == 2;
                to.React(blocked ? "Block" : "HeadHit"); SparringStrikes++;
                if(audio)audio.Contact(to,blocked);
            }
            recovery = Mathf.Max(.8f, followThrough); attackWait = .8f; attacker = 1 - attacker; return;
        }
        if (first.Reacting || second.Reacting) return;
        first.Play("CombatIdle", true); second.Play("CombatIdle", true);
        attackWait -= dt;
        if (attackWait > 0) return;
        float length = from.Play("Punch", false, true);
        impactLeft = Mathf.Min(.43f, length * .55f); followThrough = Mathf.Max(0, length - impactLeft); strikePending = true;
    }
    void TickBag(int index, float dt)
    {
        var npc = participants[occupants[index + 2]]; var bag = trainingBags[index];
        if (!npc.AtDestination || npc.Reacting || !bag) return;
        npc.Face(bag.transform.position, dt);
        if (bagImpact[index] >= 0)
        {
            bagImpact[index] -= dt;
            if (bagImpact[index] <= 0)
            {
                bagImpact[index] = -1;
                if (bag.StrikeFromNpc(npc.transform.position, npc.transform.forward)) BagStrikes++;
            }
        }
        bagWait[index] -= dt;
        if (bagWait[index] > 0 && bagWait[index] < .35f && bagImpact[index] < 0) npc.Play("CombatIdle", true);
        if (bagWait[index] > 0) return;
        float length = npc.Play("Punch", false, true);
        bagImpact[index] = Mathf.Min(.43f, length * .55f); bagWait[index] = Mathf.Max(1.35f, length + .4f);
    }
}
