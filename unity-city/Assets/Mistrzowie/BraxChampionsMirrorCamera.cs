using UnityEngine;

// Owner 10.11 12:33: "Wszystkie lustra, sprawdzasz wszystko. Lustro ma być lustrem, żadnej tandety." Every mirror of the
// region is the city's own live planar mirror (BraxGymPlanarMirror + Braxhorn/GymPlanarMirror, R1, unchanged): a real
// reflection rendered from the mirrored camera, only while the mirror is on screen and near. The generators build the
// mirrors before the region camera exists, so this links each one to the player camera when it wakes (Awake runs
// before the mirror's own Start check) and makes sure the camera draws the mirror layer.
[DisallowMultipleComponent]
public sealed class BraxChampionsMirrorCamera : MonoBehaviour
{
    public const int MirrorLayer = 9;
    void Awake()
    {
        var mirror = GetComponent<BraxGymPlanarMirror>();
        if (!mirror) return;
        if (!mirror.mirrorRenderer) mirror.mirrorRenderer = GetComponent<MeshRenderer>();
        if (mirror.sourceCamera) return;
        var cam = Camera.main;
        if (!cam) { mirror.enabled = false; Debug.LogWarning("A1_MIRROR_NO_CAMERA " + name); return; }
        mirror.sourceCamera = cam;
        cam.cullingMask |= 1 << MirrorLayer;
    }
}
