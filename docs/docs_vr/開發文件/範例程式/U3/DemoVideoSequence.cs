using UnityEngine;
using UnityEngine.Video;

namespace AudioBugs.Tutorial
{
    public sealed class DemoVideoSequence : MonoBehaviour
    {
        public DesktopLab lab;
        public VideoPlayer video;
        public VideoClip introClip;
        public VideoClip tutorialClip;
        public GameObject panel;
        private GamePhase expected = GamePhase.Ready;
        private bool finished;
        private VideoPlayer.EventHandler handler;

        private void Start() { video.playOnAwake = false; video.isLooping = false; panel.SetActive(false); }
        private void Update()
        {
            var phase = lab.Rules.Phase;
            if (phase == GamePhase.Ready) {
                if (handler != null) { video.loopPointReached -= handler; handler = null; }
                if (expected != GamePhase.Ready || video.isPlaying || video.isPrepared) video.Stop();
                expected = GamePhase.Ready; finished = false;
            }
            bool showing = phase == GamePhase.Intro || phase == GamePhase.Tutorial;
            panel.SetActive(showing);
            if (!showing) { if (video.isPlaying) video.Stop(); return; }
            if (phase != expected) {
                if (handler != null) video.loopPointReached -= handler;
                expected = phase; finished = false;
                var boundPhase = phase;
                video.Stop(); video.clip = phase == GamePhase.Intro ? introClip : tutorialClip;
                if (video.clip == null) { Debug.LogError("Assign both demo clips before disabling Automatic Demos."); return; }
                handler = _ => { if (lab.Rules.Phase == boundPhase) finished = true; };
                video.loopPointReached += handler; video.Prepare();
            }
            if (!lab.CanInteract) { if (video.isPlaying) video.Pause(); return; }
            if (finished) {
                if (phase == GamePhase.Intro) lab.Rules.CompleteIntro(); else lab.Rules.CompleteTutorial();
            } else if (video.isPrepared && !video.isPlaying) video.Play();
        }
        private void OnDestroy() { if (video != null && handler != null) video.loopPointReached -= handler; }
    }
}
