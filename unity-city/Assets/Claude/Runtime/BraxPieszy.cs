using UnityEngine;

// PRZECHODZIEŃ (właściciel 1.11: „ludzie mają tylko żyć — chodzić, nic nie atakować”). Chodzi tam i z powrotem po odcinku
// chodnika, czasem przystaje (rozgląda się), przed Braxem staje i czeka. Animacje: Walking / Idle (te same klipy Mixamo co
// Brax, przepięte na szkielet postaci przy buildzie). Daleko od kamery (> 70 m) — bez animacji i co 0,25 s (taniej).
public sealed class BraxPieszy : MonoBehaviour
{
    public Vector3 a, b;
    public float predkosc = 1.25f;
    public Animation anim;
    public string chod = "Walking", stoi = "Idle";
    bool doB = true;
    float pauza, nast;
    Transform gracz;
    static Camera kam;

    void Start()
    {
        if (anim && anim[chod]) { anim[chod].speed = predkosc / 1.3f; anim.Play(chod); }
        transform.position = Vector3.Lerp(a, b, Random.value);
        doB = Random.value > .5f;
    }

    void Update()
    {
        if (!kam) kam = Camera.main;
        float dt = Time.deltaTime;
        if (kam)
        {
            float d2 = (kam.transform.position - transform.position).sqrMagnitude;
            bool daleko = d2 > 70f * 70f;
            if (anim && anim.enabled == daleko) anim.enabled = !daleko;
            if (daleko) { if (Time.time < nast) return; dt = .25f; nast = Time.time + .25f; }
        }
        if (!gracz) { var p = FindAnyObjectByType<BraxGymPlayer>(); if (p) gracz = p.transform; }
        if (pauza > 0f)
        {
            pauza -= dt;
            if (pauza <= 0f && anim) anim.CrossFade(chod, .3f);
            return;
        }
        Vector3 cel = doB ? b : a, d = cel - transform.position; d.y = 0f;
        if (d.magnitude < .3f)
        {
            doB = !doB;
            if (Random.value < .45f) { pauza = Random.Range(2f, 7f); if (anim) anim.CrossFade(stoi, .3f); }
            return;
        }
        var dir = d.normalized;
        // Brax na drodze: stanąć i poczekać (nie wchodzić w niego)
        if (gracz)
        {
            var g = gracz.position - transform.position; g.y = 0f;
            if (g.magnitude < 1.8f && Vector3.Dot(g.normalized, dir) > .4f)
            {
                if (anim && !anim.IsPlaying(stoi)) anim.CrossFade(stoi, .25f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(g), dt * 3f);
                return;
            }
        }
        if (anim && !anim.IsPlaying(chod)) anim.CrossFade(chod, .25f);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), dt * 6f);
        transform.position += dir * Mathf.Min(predkosc * dt, d.magnitude);
    }
}
