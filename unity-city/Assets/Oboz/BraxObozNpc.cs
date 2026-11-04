using UnityEngine;

public enum BraxObozActivity { Rest, Training, Meal, Drink, Campfire }

// Authored outdoor routes, collision-aware movement, no teleport fallback.
public sealed partial class BraxObozNpc : MonoBehaviour
{
    public string characterId;
    public BraxGymPlayer player;
    public Animation clips;
    public CharacterController body;
    public Transform[] route = new Transform[0];
    public BraxObozActivity[] activities = new BraxObozActivity[0];
    public bool glovesVerified, hasGloves;
    public float walkSpeed = 1.7f * 1.294f, stopDistance = .35f;
    public float activitySeconds = 24f;
    public bool InSparring { get; private set; }
    public bool Busy => InSparring || reactionLeft > 0;
    public bool Reacting => reactionLeft > 0;
    int routeIndex;
    float remaining, stuckTime, reactionLeft, vertical;
    Vector3 previous;
    bool warned;
    bool wasPaused;
    string playing;

    public bool Allows(BraxObozActivity activity)
    {
        if (activity == BraxObozActivity.Rest || activity == BraxObozActivity.Training) return true;
        return glovesVerified && !hasGloves;
    }
    void Start()
    {
        if (!body || !clips || !player) { Debug.LogError("Niepełne podłączenie NPC: " + characterId, this); enabled = false; return; }
        previous = transform.position; Play("Idle", true);
    }
    void Update()
    {
        if (!player || !player.ready) return;
        SyncNavigationPause();
        // The shared pause stops the player, not Time.timeScale.
        if (wasPaused != player.Paused)
        {
            wasPaused = player.Paused;
            foreach (AnimationState state in clips) state.speed = wasPaused ? 0 : 1;
        }
        if (player.Paused) return;
        float dt = Mathf.Min(Time.deltaTime, .05f);
        if (body.isGrounded && vertical < 0) vertical = -2;
        vertical -= 18f * dt;
        body.Move(Vector3.up * vertical * dt);
        if (reactionLeft > 0) { reactionLeft -= dt; return; }
        if (InSparring) return;
        if (routine) { TickNavigation(dt); return; }
        if (route.Length == 0) { Play("Idle", true); return; }
        var point = route[routeIndex];
        if (!point) { Debug.LogError("Brak punktu trasy " + characterId, this); enabled = false; return; }
        Vector3 delta = point.position - transform.position; delta.y = 0;
        if (delta.magnitude > stopDistance)
        {
            MoveTowards(point.position, dt);
            if ((transform.position - previous).sqrMagnitude < .00001f) stuckTime += dt;
            else stuckTime = 0;
            previous = transform.position;
            if (stuckTime > 3f)
            {
                Play("Idle", true);
                if (!warned) { Debug.LogWarning("BRAXHORN_OBOZ zablokowana trasa: " + characterId + " → " + point.name, this); warned = true; }
            }
            return;
        }
        warned = false; stuckTime = 0;
        var activity = routeIndex < activities.Length ? activities[routeIndex] : BraxObozActivity.Rest;
        if (!Allows(activity)) activity = BraxObozActivity.Rest;
        // Meal/drink/fire require their own verified clip, never reuse a punch.
        string clip = activity == BraxObozActivity.Training ? "Training" : activity == BraxObozActivity.Meal ? "Eat" :
            activity == BraxObozActivity.Drink ? "Drink" : activity == BraxObozActivity.Campfire ? "Sit" : "Idle";
        if (clips[clip] == null) clip = "Idle";
        Play(clip, true);
        remaining += dt;
        if (remaining >= activitySeconds) { remaining = 0; routeIndex = (routeIndex + 1) % route.Length; }
    }
    public void MoveTowards(Vector3 target, float dt)
    {
        if (reactionLeft > 0 || !body) return;
        Vector3 d = target - transform.position; d.y = 0;
        if (d.sqrMagnitude < .01f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 180 * dt);
        body.Move(d.normalized * Mathf.Min(walkSpeed * dt, d.magnitude));
        Play("Walk", true);
    }
    public void Face(Vector3 target, float dt)
    {
        Vector3 d = target - transform.position; d.y = 0;
        if (d.sqrMagnitude > .001f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 220 * dt);
    }
    public void BeginSparring() { InSparring = true; reactionLeft = 0; Play("CombatIdle", true); }
    public void EndSparring() { InSparring = false; reactionLeft = 0; remaining = 0; Play("Idle", true); }
    public void React(string clip)
    {
        if (!clips || clips[clip] == null) return;
        Play(clip, false, true); reactionLeft = clips[clip].length;
    }
    public float Play(string clip, bool loop, bool restart = false)
    {
        if (!clips || clips[clip] == null) return 0;
        if (playing != clip || restart)
        {
            clips[clip].wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
            clips[clip].speed = 1; clips.CrossFade(clip, .14f, PlayMode.StopAll); playing = clip;
        }
        return clips[clip].length;
    }
}
