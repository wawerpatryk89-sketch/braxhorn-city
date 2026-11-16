using System;
using UnityEngine;

[DefaultExecutionOrder(450)]
public sealed class BraxStawyOsiedla : MonoBehaviour
{
    [Serializable] public class Staw
    {
        public Transform rodzic, kosc;
        public Vector3 punkt;
        public Quaternion obrot;
        public Transform[] posrednie;
    }

    public Staw[] stawy;
    void LateUpdate()
    {
        if (stawy == null) return;
        foreach (var staw in stawy)
        {
            var start = staw.rodzic.rotation * staw.obrot;
            var pivot = staw.rodzic.TransformPoint(staw.punkt);
            for (int i = 0; i < staw.posrednie.Length; i++)
            {
                float t = (i + 1) * .25f;
                staw.posrednie[i].SetPositionAndRotation(Vector3.Lerp(pivot, staw.kosc.position, t),
                    Quaternion.Slerp(start, staw.kosc.rotation, t));
            }
        }
    }
}
