using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

// Lives on the existing BraxhornPlayer, so its Command receiver observes the
// same pause/sound messages as BraxGymPlayer without changing that controller.
// The single fixed venue AudioSource belongs to this scene, never DontDestroy.
public sealed class BraxNovaMusic : MonoBehaviour
{
    public BraxGymPlayer player;
    public AudioSource venueSource;
    public Bounds hall;
    public Bounds vestibule;
    public Bounds entranceOutside;
    public Vector3 doorway;
    public float volume = .55f;
    private bool muted, paused, background, unfocused, started;
    private float gain, targetGain, nextReport, retryAt;
    private UnityWebRequest request;
    private AudioClip ownedClip;
    private AudioClip pendingClip;
    private bool decoding;
    private float observedLength;
    private string clipLoadState = "not-requested";
    private long responseCode, downloadedBytes;
    private string failure = "", sourceUrl;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void BXNovaMusicState(string json);
#endif
    [Serializable] private sealed class State
    {
        public string track = "Industrial Pulse", venue = "NOVA", error, url, clipLoadState;
        public bool active, loaded, playing, downloading, decoding, muted, paused;
        public float time, duration, volume, targetGain, observedLength;
        public long responseCode,downloadedBytes;
        public float removedIntroSeconds = 27.70f;
    }

    private void Awake()
    {
        if (!player || !venueSource || venueSource.clip)
            throw new InvalidOperationException("NOVA requires one original player and one initially empty venue source");
        sourceUrl = Application.streamingAssetsPath.TrimEnd('/') + "/NOVA/IndustrialPulseClub.mp3";
        venueSource.playOnAwake = false;
        venueSource.loop = true;
        venueSource.volume = 0;
    }

    private float VenueGain(Vector3 p)
    {
        if (hall.Contains(p)) return 1;
        if (vestibule.Contains(p))
            return Mathf.Lerp(.24f, .8f, Mathf.SmoothStep(0, 1,
                Mathf.InverseLerp(vestibule.min.z, vestibule.max.z, p.z)));
        if (!entranceOutside.Contains(p)) return 0;
        float distance = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(doorway.x, doorway.z));
        // A quiet spill from the real doorway announces the party outside.
        // It is not broadcast into the adjacent shop/tower or the basement.
        return .24f * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(distance / 14f)));
    }

    public void Command(string key)
    {
        if (key == "sound") muted = !muted;
        else if (key == "pause") paused = !paused;
        else return;
        if ((paused || muted) && venueSource) venueSource.Pause();
        Report();
    }

    private void OnApplicationPause(bool value) { background = value; }
    private void OnApplicationFocus(bool value) { unfocused = !value; }

    private void Update()
    {
        if (!player || !venueSource || !player.ready) return;
        bool stopped = paused || muted || background || unfocused ||
                       !player.cityWorld || player.cityWorld.loading;
        targetGain = stopped ? 0 : VenueGain(player.transform.position);
        if (stopped)
        {
            gain = 0;
            venueSource.volume = 0;
            if (venueSource.isPlaying) venueSource.Pause();
        }
        else
        {
            gain = Mathf.MoveTowards(gain, targetGain, Time.unscaledDeltaTime / .65f);
            venueSource.volume = volume * gain;
            if (targetGain > 0 && !ownedClip && request == null && Time.unscaledTime >= retryAt)
                StartCoroutine(LoadMusic());
            // Once approached, the party keeps playing in the city even when
            // Brax walks out of earshot. Position affects loudness, not time.
            if (ownedClip && !venueSource.isPlaying)
            {
                if (!started) { venueSource.Play(); started = true; }
                else venueSource.UnPause();
            }
        }
        if (Time.unscaledTime >= nextReport) { nextReport = Time.unscaledTime + .25f; Report(); }
    }

    private IEnumerator LoadMusic()
    {
        failure = "";
        observedLength = 0;clipLoadState = "downloading";
        request = UnityWebRequestMultimedia.GetAudioClip(sourceUrl, AudioType.MPEG);
        // Wait for the actual clip before playback. This also works on WebGL
        // backends that cannot play a partially downloaded AudioClip.
        yield return request.SendWebRequest();
        responseCode = request.responseCode;downloadedBytes = (long)request.downloadedBytes;
        if (request.result == UnityWebRequest.Result.Success)
        {
            decoding = true;
            try { pendingClip = DownloadHandlerAudioClip.GetContent(request); }
            catch (Exception e) { failure = "NOVA audio decode failed: " + e.Message; }
            // WebGL's browser decoder can finish after HTTP completion. Keep
            // the pending clip separate: Update must not Play an empty source
            // or destroy a valid download while its duration is still zero.
            float deadline = Time.realtimeSinceStartup + 45f;
            while (pendingClip)
            {
                observedLength = pendingClip.length;
                clipLoadState = pendingClip.loadState.ToString();
                if (pendingClip.loadState == AudioDataLoadState.Failed) break;
                if (pendingClip.loadState == AudioDataLoadState.Loaded && observedLength > 0) break;
                if (Time.realtimeSinceStartup >= deadline) break;
                yield return null;
            }
            if (pendingClip && pendingClip.loadState == AudioDataLoadState.Loaded &&
                observedLength > 175 && observedLength < 185)
            {
                ownedClip = pendingClip;pendingClip = null;started = false;
                ownedClip.name = "Industrial Pulse - approved NOVA edit (intro removed)";
                venueSource.clip = ownedClip;
                Debug.Log("NOVA_AUDIO_READY duration=" + ownedClip.length + " introRemoved=27.70");
            }
            else
            {
                if (string.IsNullOrEmpty(failure)) failure = "NOVA clip not ready/invalid: state="+
                    clipLoadState+" length="+observedLength+" HTTP="+responseCode+" bytes="+downloadedBytes;
                if (pendingClip) Destroy(pendingClip);
                pendingClip = null;
            }
        }
        else failure = request.error;
        decoding = false;
        request.Dispose(); request = null;
        if (!string.IsNullOrEmpty(failure))
        {
            retryAt = Time.unscaledTime + 30;
            Debug.LogError("NOVA_AUDIO_LOAD_FAILED " + failure);
        }
        Report();
    }

    private void Report()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        BXNovaMusicState(JsonUtility.ToJson(new State {
            active = isActiveAndEnabled, error = failure, url = sourceUrl, loaded = ownedClip,
            playing = venueSource && venueSource.isPlaying, downloading = request != null,
            decoding = decoding, clipLoadState = clipLoadState, observedLength = observedLength,
            responseCode = responseCode, downloadedBytes = downloadedBytes,
            muted = muted, paused = paused || background || unfocused,
            time = venueSource && ownedClip ? venueSource.time : 0,
            duration = ownedClip ? ownedClip.length : 0,
            volume = venueSource ? venueSource.volume : 0, targetGain = targetGain
        }));
#endif
    }

    private void OnDisable()
    {
        if (venueSource) venueSource.Pause();
        Report();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        if (request != null) { request.Abort(); request.Dispose(); request = null; }
        if (venueSource) { venueSource.Stop(); venueSource.clip = null; }
        if (ownedClip) { Destroy(ownedClip); ownedClip = null; }
        if (pendingClip) { Destroy(pendingClip); pendingClip = null; }
        decoding = false;
        Report();
    }
}
