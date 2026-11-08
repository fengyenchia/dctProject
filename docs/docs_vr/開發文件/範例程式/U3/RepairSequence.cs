using UnityEngine;

namespace AudioBugs.Tutorial
{
    public sealed class RepairSequence : MonoBehaviour
    {
        public DesktopLab lab;
        public Animator animator;
        private bool started;
        private void Start() { animator.enabled = false; }
        private void Update()
        {
            if (lab.Rules.Phase == GamePhase.Ready) { started = false; animator.enabled = false; return; }
            if (lab.Rules.Phase != GamePhase.Recycling) return;
            if (!started) { started = true; animator.enabled = true; animator.Play("Repair", 0, 0f); }
            animator.speed = lab.CanInteract ? 1f : 0f;
        }
        // 加在 Repair 動畫最後一格的 Animation Event。
        public void FinishRepair() { lab.CompleteRepair(); }
    }
}
