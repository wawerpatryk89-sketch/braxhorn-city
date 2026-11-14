using System;
using UnityEngine;

namespace Braxhorn.Characters
{
    [RequireComponent(typeof(Animator)), DisallowMultipleComponent]
    public sealed class BraxResidentMotion : MonoBehaviour
    {
        public enum Everyday { Idle, Walk, Run, Jump, SitDown, SeatedIdle, StandUp, Talk, Wave, Point }
        [SerializeField] private string[] attackStates = Array.Empty<string>();
        private static readonly int Combat = Animator.StringToHash("Combat");
        private Animator animator;
        public bool InCombat { get; private set; }

        public void SetAttackList(string[] states) => attackStates = (string[])states.Clone();

        private void Awake() => animator = GetComponent<Animator>();

        private void OnEnable()
        {
            InCombat = false;
            if (!animator.runtimeAnimatorController) return;
            animator.SetBool(Combat, false);
            animator.Play("Base Layer.Idle", 0, 0f);
        }

        public bool TryEveryday(Everyday action)
        {
            if (InCombat || !Enum.IsDefined(typeof(Everyday), action)) return false;
            return Play(action.ToString(), false);
        }

        public bool TryAttack(int index)
        {
            if (index < 0 || index >= attackStates.Length) return false;
            return Play(attackStates[index], true);
        }

        // Called by gameplay only after a confirmed hit, including the first hit.
        public bool ReceiveHit(bool head)
        {
            return Play(head ? "HitHead" : "HitBody", attackStates.Length > 0);
        }

        public bool TryDodge() => InCombat && Play("Dodge", false);
        public bool TryBlock() => InCombat && Play("Block", false);

        public void EndCombat()
        {
            if (!InCombat) return;
            InCombat = false;
            animator.SetBool(Combat, false);
            Play("Idle", false);
        }

        private bool Play(string state, bool enterCombat)
        {
            if (!isActiveAndEnabled || !animator || !animator.runtimeAnimatorController ||
                string.IsNullOrEmpty(state)) return false;
            var hash = Animator.StringToHash("Base Layer." + state);
            if (!animator.HasState(0, hash)) return false;
            if (enterCombat && !InCombat)
            {
                InCombat = true;
                animator.SetBool(Combat, true);
            }
            animator.CrossFadeInFixedTime(hash, 0.10f, 0, 0f);
            return true;
        }
    }
}
