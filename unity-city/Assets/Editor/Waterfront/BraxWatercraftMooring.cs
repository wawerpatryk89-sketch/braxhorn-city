using UnityEngine;

// No fabricated geometry. Minimal mooring motion for original CC BY source boats.
[DisallowMultipleComponent]
public sealed class BraxWatercraftMooring : MonoBehaviour
{
    [Range(0f,0.04f)] public float riseMetres=.018f;
    [Range(0f,0.8f)] public float rollDegrees=.35f;
    public float cycleSeconds=8f;
    Vector3 initialPosition; Quaternion initialRotation; float phase;
    void Awake()
    {
        initialPosition=transform.localPosition; initialRotation=transform.localRotation;
        phase=(Mathf.Abs(initialPosition.x*5.1f+initialPosition.z*1.7f)%17f)/17f*2f*Mathf.PI;
    }
    void LateUpdate()
    {
        float p=Time.time*2f*Mathf.PI/Mathf.Max(2f,cycleSeconds)+phase;
        transform.localPosition=initialPosition+Vector3.up*(Mathf.Sin(p)*riseMetres);
        transform.localRotation=initialRotation*Quaternion.Euler(Mathf.Sin(p+.7f)*rollDegrees*.4f,0,Mathf.Sin(p)*rollDegrees);
    }
}