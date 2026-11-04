using UnityEngine;

// One opponent per sparring. Rules are exposed for the owner's balance review.
public sealed class BraxObozArena : MonoBehaviour, IBraxInterakcja
{
    public string label = "SPARING";
    public BraxGymPlayer player;
    public BraxObozNpc opponent;
    public BraxObozProgress progress;
    public Vector2 halfSize = new Vector2(4.5f, 4.5f);
    public float roundSeconds = 90, hitRange = 2.1f, punchDamage = 8, kickDamage = 12;
    public bool Active { get; private set; }
    public string Status { get; private set; } = "";
    static BraxObozArena active;
    public static bool AnyActive => active && active.Active;
    public static bool Uses(BraxObozNpc npc) => AnyActive && active.opponent == npc;
    float playerHealth, npcHealth, remaining, attackWait, strikeLeft, recoverLeft, followThrough;
    bool strikePending;
    int playerScore, npcScore;
    bool Inside(Vector3 point, float margin = 0)
    {
        var p = transform.InverseTransformPoint(point);
        return Mathf.Abs(p.x) < halfSize.x + margin && Mathf.Abs(p.z) < halfSize.y + margin && Mathf.Abs(p.y) < 2.5f;
    }
    void OnEnable() { BraxInterakcje.Dodaj(this); BraxGymPlayer.OnCios += Hit; }
    void OnDisable() { BraxInterakcje.Usun(this); BraxGymPlayer.OnCios -= Hit; Cancel(); }
    public string Kontekst(Vector3 p)
    {
        if (!player || !player.ready || !opponent || !Inside(p)) return "";
        if (Active) return "ZAKOŃCZ SPARING";
        return !active && !opponent.Busy && Inside(opponent.transform.position) ? "SPARING · " + opponent.characterId.ToUpperInvariant() : "";
    }
    public bool Wykonaj(Vector3 p)
    {
        if (string.IsNullOrEmpty(Kontekst(p))) return false;
        if (Active) { Cancel(); return true; }
        active = this; Active = true; playerHealth = npcHealth = 100; playerScore = npcScore = 0;
        remaining = roundSeconds; attackWait = 1.2f; recoverLeft = 0; strikePending = false;
        opponent.BeginSparring(); Status = "SPARING · " + opponent.characterId.ToUpperInvariant(); return true;
    }
    void Update()
    {
        if (!Active) return;
        if (!player || !opponent || !player.ready) { Cancel(); return; }
        if (player.Paused) return;
        float dt = Mathf.Min(Time.deltaTime, .1f);
        if (!Inside(player.transform.position, 1f) || !Inside(opponent.transform.position, 1f)) { Cancel(); return; }
        remaining -= dt;
        if (remaining <= 0) { Finish(playerScore == npcScore ? 0 : playerScore > npcScore ? 1 : -1); return; }
        opponent.Face(player.transform.position, dt);
        if (recoverLeft > 0) { recoverLeft -= dt; return; }
        if (opponent.Reacting) return;
        Vector3 delta = player.transform.position - opponent.transform.position; delta.y = 0;
        if (strikePending)
        {
            strikeLeft -= dt;
            if (strikeLeft <= 0)
            {
                strikePending = false;
                recoverLeft = followThrough;
                if (CanHit(opponent.transform.position, opponent.transform.forward, player.transform.position))
                {
                    playerHealth = Mathf.Max(0, playerHealth - punchDamage); npcScore++;
                    player.PlayReaction("HeadHit");
                    if (playerHealth <= 0) { Finish(-1); return; }
                }
            }
            return;
        }
        attackWait -= dt;
        if (delta.magnitude > hitRange * .85f) { opponent.MoveTowards(player.transform.position, dt); return; }
        if (attackWait > 0) { opponent.Play("CombatIdle", true); return; }
        float length = opponent.Play("Punch", false, true);
        strikePending = true; strikeLeft = Mathf.Min(.43f, length * .65f);
        followThrough = Mathf.Max(0, length - strikeLeft);
        attackWait = Mathf.Max(1.15f, length + .35f);
    }
    bool CanHit(Vector3 from, Vector3 forward, Vector3 to)
    {
        Vector3 d = to - from; d.y = 0;
        if (d.sqrMagnitude > hitRange * hitRange || (d.sqrMagnitude > .001f && Vector3.Dot(forward, d.normalized) < .5f)) return false;
        // Layer 8 contains the actors; only world geometry can occlude a hit.
        return !Physics.Linecast(from + Vector3.up * 1.5f, to + Vector3.up * 1.5f, ~(1 << 8), QueryTriggerInteraction.Ignore);
    }
    void Hit(Vector3 from, Vector3 forward, bool kick)
    {
        if (!Active || !player || player.Paused || recoverLeft > 0 || !opponent) return;
        if ((from - player.transform.position).sqrMagnitude > .1f || !CanHit(from, forward, opponent.transform.position)) return;
        npcHealth = Mathf.Max(0, npcHealth - (kick ? kickDamage : punchDamage)); playerScore++;
        strikePending = false; attackWait = 1.1f;
        opponent.React(kick ? "HitToBody" : "HeadHit"); recoverLeft = .55f;
        if (npcHealth <= 0) Finish(1);
    }
    void Finish(int result)
    {
        Status = result == 0 ? "REMIS" : result > 0 ? "WYGRANY SPARING" : "PRZEGRANY SPARING";
        if (progress && result != 0) progress.FinishSparring(result > 0);
        StopFight(); BraxSport.Pokaz(Status, 3); BraxSport.UpewnijHud();
    }
    public void Cancel() { if (!Active) return; Status = "SPARING PRZERWANY"; StopFight(); }
    void StopFight()
    {
        Active = false; strikePending = false;
        if (active == this) active = null;
        if (opponent) opponent.EndSparring();
    }
}
