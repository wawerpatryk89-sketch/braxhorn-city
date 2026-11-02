using UnityEngine;

// One dedicated voice source per speaker; never reuse footsteps or other effects.
public sealed class BraxGlosPostaci : MonoBehaviour
{
    AudioSource source;
    public void Mow(AudioClip clip)
    {
        if (!source)
        {
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
            source.spatialBlend = 1f; source.minDistance = 2.5f; source.maxDistance = 24f;
            source.rolloffMode = AudioRolloffMode.Logarithmic; source.priority = 120;
        }
        source.Stop(); source.pitch = 1f; source.volume = .85f; source.clip = clip; source.Play();
    }
    void OnDisable() { if (source) source.Stop(); }
    void OnDestroy() { if (source) Destroy(source); }
}
