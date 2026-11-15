using UnityEngine;

namespace AudioBugs.Tutorial
{
    public sealed class RepairSequence : MonoBehaviour
    {
        public DesktopLab lab;
        public Animator animator;
        public ParticleSystem repairParticles;
        public int particlesPerCapture = 12;
        [Min(.1f)] public float particleOnlySeconds = 3f;
        private bool started;
        private float elapsed;
        private void Start() { if (animator != null) animator.enabled = false; }

        private void Update()
        {
            if (lab.Rules.Phase == GamePhase.Ready) {
                if (!started) return;
                started = false;
                if (animator != null) {
                    // 新場先套用第 0 幀的破損外觀，再關閉 Animator。
                    animator.enabled = true;
                    animator.Play("Repair", 0, 0f);
                    animator.Update(0f);
                    animator.enabled = false;
                }
                if (repairParticles != null) repairParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return;
            }
            if (lab.Rules.Phase != GamePhase.Recycling) return;
            if (!started && lab.CanInteract) {
                started = true;
                elapsed = 0f;
                if (animator != null) {
                    animator.enabled = true;
                    animator.Play("Repair", 0, 0f);
                }
                if (repairParticles != null) {
                    repairParticles.Play(true);
                    repairParticles.Emit(lab.Rules.Collected.Count * Mathf.Max(0, particlesPerCapture));
                }
            }
            if (!started) return;
            if (animator != null) animator.speed = lab.CanInteract ? 1f : 0f;
            if (started && repairParticles != null) {
                if (!lab.CanInteract && repairParticles.isPlaying) repairParticles.Pause(true);
                else if (lab.CanInteract && repairParticles.isPaused) repairParticles.Play(true);
            }
            if (animator == null && lab.CanInteract) {
                elapsed += Time.deltaTime;
                if (elapsed >= particleOnlySeconds) FinishRepair();
            }
        }
        // 加在 Repair 動畫最後一格的 Animation Event。
        public void FinishRepair() { lab.CompleteRepair(); }
    }
}
