using UnityEngine;

namespace AudioBugs.Tutorial
{
    // 修復完成進 ExitRoute 時提示一次；追蹤失效／暫停時同步暫停語音。
    public sealed class ReturnToStartGuide : MonoBehaviour
    {
        public DesktopLab lab;
        public AudioSource promptSource;
        public AudioClip returnPrompt;
        private bool announced;
        private bool pausedByGuide;

        private void Update()
        {
            if (lab == null) return;
            if (lab.Rules.Phase != GamePhase.ExitRoute) {
                if (announced && promptSource != null) promptSource.Stop();
                announced = false;
                pausedByGuide = false;
                return;
            }
            if (promptSource == null || returnPrompt == null) return;
            if (!lab.CanInteract) {
                if (announced && promptSource.isPlaying) {
                    promptSource.Pause();
                    pausedByGuide = true;
                }
                return;
            }
            if (!announced) {
                announced = true;
                promptSource.clip = returnPrompt;
                promptSource.loop = false;
                promptSource.spatialBlend = 0f;
                promptSource.Play();
            } else if (pausedByGuide) {
                pausedByGuide = false;
                promptSource.UnPause();
            }
        }

        private void OnDisable()
        {
            if (promptSource != null && announced) promptSource.Stop();
        }
    }
}
