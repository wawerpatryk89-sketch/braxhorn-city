using System;
using UnityEngine;

// MIASTO R1 / M1: realny przejazd, trzy stacje i dwukierunkowy ruch.
// Komponent jest przypięty do prawdziwego modelu metro_train.glb, nie do atrapy.
public sealed class BraxMetro3StopRide : MonoBehaviour, IBraxInterakcja
{
    public float[] stationX = new float[3];
    public string[] stationNames = new string[3];
    public int stationIndex;
    public int direction = 1;
    public float trackZ = 32.3f;
    public float platformY = -11.2f;
    public float boardingZ = 29.7f;
    public float cruisingSpeed = 9.5f;
    public float acceleration = 2.2f;
    public float dwellSeconds = 11f;
    public Vector3 riderLocalPosition = new Vector3(0f, -11.1f, 0f);

    public bool AtStation { get { return arrived; } }
    public string CurrentStation { get { return Valid ? stationNames[stationIndex] : ""; } }

    BraxGymPlayer rider;
    UnityEngine.SceneManagement.Scene riderScene;
    bool arrived = true;
    float departureAt, velocity;
    bool Valid { get { return stationX != null && stationNames != null &&
        stationX.Length == 3 && stationNames.Length == 3 &&
        stationIndex >= 0 && stationIndex < 3; } }

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable()
    {
        BraxInterakcje.Usun(this);
        if (rider) Leave(true);
    }
    void Start()
    {
        if (!Valid)
        {
            Debug.LogWarning("BRAXHORN_METRO_M1: niekompletna trasa trzech stacji");
            enabled = false;
            return;
        }
        arrived = true;
        departureAt = Time.time + dwellSeconds;
    }

    void Update()
    {
        if (!Valid) return;
        if (arrived)
        {
            if (Time.time < departureAt) return;
            arrived = false;
            if (stationIndex + direction < 0 || stationIndex + direction >= stationX.Length)
                direction = -direction;
            velocity = 0f;
        }
        int destination = Mathf.Clamp(stationIndex + direction, 0, stationX.Length - 1);
        float remaining = Mathf.Abs(stationX[destination] - transform.position.x);
        if (remaining < .015f)
        {
            transform.position = new Vector3(stationX[destination], transform.position.y, trackZ);
            stationIndex = destination;
            arrived = true;
            velocity = 0f;
            departureAt = Time.time + dwellSeconds;
            Debug.Log("BRAXHORN_METRO_M1_POCIAG postoju=" + CurrentStation +
                " kierunek_nastepny=" + (stationIndex + direction));
            return;
        }
        float dt = Mathf.Min(Time.deltaTime, .1f);
        float safe = Mathf.Sqrt(Mathf.Max(0f, 2f * acceleration * remaining));
        velocity = Mathf.MoveTowards(velocity, Mathf.Min(cruisingSpeed, safe), acceleration * dt);
        float step = Mathf.Min(remaining, velocity * dt);
        transform.position += new Vector3(Mathf.Sign(stationX[destination] -
            transform.position.x) * step, 0f, 0f);
    }

    public string Kontekst(Vector3 playerPosition)
    {
        if (!Valid || !arrived || rider) return "";
        var stop = new Vector3(stationX[stationIndex], platformY, boardingZ);
        var delta = playerPosition - stop;
        if (Mathf.Abs(delta.y) > 1.7f) return "";
        delta.y = 0f;
        return delta.sqrMagnitude <= 2.7f * 2.7f ?
            "WSIADZ · METRO M1 " + CurrentStation : "";
    }

    public bool Wykonaj(Vector3 playerPosition)
    {
        if (string.IsNullOrEmpty(Kontekst(playerPosition))) return false;
        var player = FindAnyObjectByType<BraxGymPlayer>();
        if (!player || !player.body || player.vessel || player.seat || player.metroRide)
        {
            Debug.LogWarning("BRAXHORN_METRO_M1: nie mozna wejsc — gracz zajety lub niekompletny");
            return false;
        }
        rider = player;
        riderScene = player.gameObject.scene;
        player.body.enabled = false;
        player.metroRide = this;
        player.transform.SetParent(transform, false);
        player.transform.localPosition = riderLocalPosition;
        player.transform.localRotation = Quaternion.identity;
        BraxPose.Get(player).Set(BraxPose.Mode.Ride, transform);
        Debug.Log("BRAXHORN_METRO_M1_WSIADL " + CurrentStation);
        return true;
    }

    public string PassengerContext
    {
        get
        {
            if (!rider) return "";
            return arrived ? "WYSIADZ · METRO M1 " + CurrentStation :
                "METRO M1 · PRZEJAZD DO " +
                (Valid ? stationNames[Mathf.Clamp(stationIndex + direction, 0, 2)] : "STACJI");
        }
    }

    public bool TryAlight()
    {
        if (!rider || !arrived) return false;
        return Leave(false);
    }

    bool Leave(bool emergency)
    {
        if (!rider) return false;
        var p = rider;
        Vector3 dest = new Vector3(stationX[stationIndex], platformY + .08f, boardingZ);
        if (!emergency && !ClearExit(dest, p))
        {
            Debug.LogWarning("BRAXHORN_METRO_M1_WYJSCIE_ZABLOKOWANE " +
                CurrentStation + " " + dest.ToString("F2"));
            return false;
        }
        rider = null;
        BraxPose.Get(p).Set(BraxPose.Mode.None, null);
        p.transform.SetParent(null, true);
        if (riderScene.IsValid() && riderScene.isLoaded && p.gameObject.scene != riderScene)
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(p.gameObject, riderScene);
        p.transform.position = dest;
        p.transform.rotation = Quaternion.Euler(0f, trackZ > 28f ? 180f : 0f, 0f);
        p.metroRide = null;
        if (p.body) p.body.enabled = true;
        Debug.Log("BRAXHORN_METRO_M1_WYSIADL " + CurrentStation);
        return true;
    }

    bool ClearExit(Vector3 dest, BraxGymPlayer p)
    {
        var r = Mathf.Max(.28f, p.body.radius);
        foreach (var hit in Physics.OverlapCapsule(
            dest + Vector3.up * (r + .12f),
            dest + Vector3.up * (Mathf.Max(2f * r, p.body.height) - r),
            r, ~(1 << 29), QueryTriggerInteraction.Ignore))
        {
            if (!hit || !hit.enabled || hit.isTrigger ||
                hit.transform.IsChildOf(transform) ||
                hit.GetComponentInParent<BraxGymPlayer>()) continue;
            return false;
        }
        return true;
    }
}
