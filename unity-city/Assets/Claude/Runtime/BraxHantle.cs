using System;
using UnityEngine;

namespace Braxhorn.Characters
{
    [RequireComponent(typeof(Animator)), DisallowMultipleComponent]
    public sealed class BraxHantle : MonoBehaviour
    {
        [Serializable]
        public class Grip
        {
            public Transform wrist, middle, middle2, middle3, thumb3, index, little, weight;
            public Vector3 meshAxis, meshCenter;
            public float meshLength, relativePalm;
            public Vector3 BarGrip => .42f * middle3.position + .36f * middle.position
                + .11f * middle2.position + .11f * thumb3.position;

            public void Place()
            {
                var center = .25f * middle.position + .40f * middle2.position
                    + .15f * middle3.position + .20f * wrist.position;
                var axis = index.position - little.position;
                float palm = Vector3.Distance(wrist.position, middle.position);
                float parentScale = Mathf.Abs(weight.parent.lossyScale.x);
                if (axis.sqrMagnitude < 1e-12f || parentScale < 1e-8f) return;
                weight.localScale = Vector3.one * (.128f * palm / (relativePalm * meshLength * parentScale));
                weight.rotation = Quaternion.FromToRotation(meshAxis, axis.normalized);
                weight.position = center - weight.TransformVector(meshCenter);
            }
        }

        [SerializeField] private Grip[] hands;
        [SerializeField] private Transform bar;
        [SerializeField] private Vector3 barAxis, barCenter;
        [SerializeField] private float barLength;
        private static readonly int Squat = Animator.StringToHash("GymBackSquat");
        private Animator animator;
        private static readonly int Curl = Animator.StringToHash("GymBicepCurl");

        public void Configure(Grip[] grips, Transform squatBar, Vector3 axis, Vector3 center, float length)
        {
            hands = grips;
            bar = squatBar;
            barAxis = axis;
            barCenter = center;
            barLength = length;
        }
        private void Awake() => animator = GetComponent<Animator>();

        private void OnDisable()
        {
            if (bar) bar.gameObject.SetActive(false);
            if (hands == null) return;
            foreach (var grip in hands) if (grip.weight) grip.weight.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (hands == null) return;
            int state = animator && animator.isActiveAndEnabled && animator.runtimeAnimatorController && !animator.IsInTransition(0)
                ? animator.GetCurrentAnimatorStateInfo(0).shortNameHash : 0;
            bool show = state == Curl;
            bool squat = state == Squat;
            if (squat)
            {
                Vector3 left = hands[0].BarGrip, right = hands[1].BarGrip;
                var span = left - right;
                float palm = Vector3.Distance(hands[0].wrist.position, hands[0].middle.position);
                bar.localScale = Vector3.one * (1.02f * palm / (hands[0].relativePalm * barLength * Mathf.Abs(bar.parent.lossyScale.x)));
                bar.rotation = Quaternion.FromToRotation(barAxis, span.normalized);
                bar.position = (left + right) * .5f - bar.TransformVector(barCenter);
            }
            if (bar.gameObject.activeSelf != squat) bar.gameObject.SetActive(squat);
            foreach (var grip in hands)
            {
                if (show) grip.Place();
                if (grip.weight.gameObject.activeSelf != show) grip.weight.gameObject.SetActive(show);
            }
        }
    }
}
