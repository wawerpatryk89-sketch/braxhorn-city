using UnityEngine;
using UnityEngine.AI;

// NavMesh owns route/avoidance; the existing capsule remains movement authority.
public sealed partial class BraxObozNpc : IBraxInterakcja
{
    public BraxObozTrainingRoutine routine;
    public NavMeshAgent navigation;
    public bool AtDestination { get; private set; }
    public int Arrivals { get; private set; }
    public float Travelled { get; private set; }
    public bool NavigationBlocked { get; private set; }
    Vector3 destination;
    bool travelling;
    float noProgress;
    float bestRemaining = float.PositiveInfinity;

    public void HoldRoute()
    {
        EndSparring(); travelling = false;
        if (navigation && navigation.enabled && navigation.isOnNavMesh)
        { navigation.isStopped = true; navigation.ResetPath(); }
    }

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable()
    {
        BraxInterakcje.Usun(this);
        if (navigation && navigation.enabled && navigation.isOnNavMesh) navigation.isStopped = true;
    }
    public bool TravelTo(Vector3 target, bool jog = false)
    {
        if (!navigation || !navigation.enabled || !navigation.isOnNavMesh) return false;
        if (!NavMesh.SamplePosition(target, out var hit, .8f, navigation.areaMask)) return false;
        EndSparring(); destination = hit.position; travelling = true; AtDestination = false;
        NavigationBlocked = false; noProgress = 0; bestRemaining = float.PositiveInfinity;
        navigation.speed = jog ? walkSpeed * 1.8f : walkSpeed;
        navigation.isStopped = false;
        if (!navigation.SetDestination(destination)) { travelling = false; NavigationBlocked = true; return false; }
        return true;
    }
    void SyncNavigationPause()
    {
        if (!routine || !navigation || !navigation.enabled || !navigation.isOnNavMesh) return;
        navigation.isStopped = player.Paused || InSparring || Reacting || !travelling;
        navigation.nextPosition = transform.position;
    }
    void TickNavigation(float dt)
    {
        if (!navigation || !navigation.enabled || !navigation.isOnNavMesh || !travelling) return;
        if (navigation.pathPending) return;
        Vector3 delta = destination - transform.position; delta.y = 0;
        if (delta.magnitude < .28f && Mathf.Abs(destination.y - transform.position.y) < .6f)
        {
            travelling = false; AtDestination = true; Arrivals++; noProgress = 0;
            navigation.isStopped = true; navigation.ResetPath(); Play("Idle", true); return;
        }
        if (navigation.pathStatus != NavMeshPathStatus.PathComplete)
        {
            if (!NavigationBlocked) Debug.LogWarning("BRAXHORN_OBOZ niepełna trasa NPC: " + characterId, this);
            NavigationBlocked = true; Play("Idle", true); return;
        }
        Vector3 velocity = navigation.desiredVelocity; velocity.y = 0;
        Vector3 before = transform.position;
        if (velocity.sqrMagnitude > .01f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(velocity), 240 * dt);
            body.Move(velocity * dt);
            Play(navigation.speed > walkSpeed * 1.1f ? "Run" : "Walk", true);
        }
        else Play("Idle", true);
        float moved = Vector3.Distance(before, transform.position); Travelled += moved;
        // Oscillating against a collider is movement, but not route progress.
        // updatePosition is false: measure from the actual collision controller,
        // not the agent's cached remainingDistance after a destination change.
        var corners = navigation.path.corners;
        float remaining = corners.Length > 1 ? 0 : float.PositiveInfinity;
        Vector3 routePoint = transform.position;
        for (int i = 1; i < corners.Length; i++)
        {
            remaining += Vector3.Distance(routePoint, corners[i]);
            routePoint = corners[i];
        }
        if (!float.IsInfinity(remaining) && remaining < bestRemaining - .15f)
        { bestRemaining = remaining; noProgress = 0; NavigationBlocked = false; }
        else noProgress += dt;
        if (noProgress > 8 && !NavigationBlocked)
        {
            NavigationBlocked = true;
            Debug.LogWarning("BRAXHORN_OBOZ zatrzymana trasa NPC: " + characterId, this);
        }
        navigation.nextPosition = transform.position;
    }
    public string Kontekst(Vector3 p)
    {
        if (!routine || !routine.Running || !player || !player.ready || player.Paused) return "";
        if (BraxObozArena.AnyActive) return "";
        // Inside the current arena AKCJA starts sparring, regardless of registration order.
        var arena = routine.CurrentArena;
        if (arena && !string.IsNullOrEmpty(arena.Kontekst(p))) return "";
        Vector3 delta = p - transform.position;
        if (Mathf.Abs(delta.y) > 2 || new Vector2(delta.x, delta.z).sqrMagnitude > 8) return "";
        if (Physics.Linecast(p + Vector3.up * 1.5f, transform.position + Vector3.up * 1.5f, ~(1 << 8), QueryTriggerInteraction.Ignore)) return "";
        return "ZADANIE · " + characterId.ToUpperInvariant() + " · " + routine.NextOrderLabel(this);
    }
    public bool Wykonaj(Vector3 p)
    {
        if (string.IsNullOrEmpty(Kontekst(p))) return false;
        return routine.OrderNext(this);
    }
}
