using UnityEngine;

namespace AudioBugs.Tutorial
{
    // 僅供桌面原型：音量門檻，不含 AEC 或正式人聲分類。
    public sealed class MicRevealPreview : MonoBehaviour
    {
        public DesktopLab lab;
        public int deviceIndex;
        public float threshold = .015f;
        public float minimumVoiceSeconds = .35f;
        public float quietSeconds = .4f;
        public float cooldownSeconds = 1f;
        public float rms;
        public bool audioReady;
        private AudioClip clip;
        private string device;
        private readonly float[] samples = new float[1024];
        private float speaking, quiet, lastTrigger = -10f;
        private bool latched;
        private int lastPosition = -1;
        private float lastSampleAt;

        private void Start()
        {
            if (Microphone.devices.Length == 0) { Debug.LogWarning("No microphone; use V for the lab."); enabled = false; return; }
            device = Microphone.devices[Mathf.Clamp(deviceIndex, 0, Microphone.devices.Length - 1)];
            clip = Microphone.Start(device, true, 1, 48000);
            Debug.Log("MIC PREVIEW ONLY: no AEC. Device: " + device);
        }

        private void Update()
        {
            audioReady = false;
            if (clip == null || !Microphone.IsRecording(device)) { speaking = quiet = 0f; return; }
            int position = Microphone.GetPosition(device);
            if (position < 0 || position == lastPosition || position < samples.Length && lastPosition < 0) {
                if (Time.unscaledTime - lastSampleAt > .2f) speaking = quiet = 0f;
                return;
            }
            float dt = lastPosition < 0 ? 0f : Mathf.Min(((position - lastPosition + clip.samples) % clip.samples) / (float)clip.frequency, .1f);
            if (Time.unscaledTime - lastSampleAt > .2f) { speaking = quiet = 0f; dt = 0f; }
            lastPosition = position;
            lastSampleAt = Time.unscaledTime;
            if (!clip.GetData(samples, (position - samples.Length + clip.samples) % clip.samples)) { speaking = quiet = 0f; return; }
            float sum = 0f;
            foreach (float sample in samples) sum += sample * sample;
            rms = Mathf.Sqrt(sum / samples.Length);
            audioReady = true;
            if (lab == null || !lab.CanInteract || lab.Rules.Phase != GamePhase.Active) { speaking = quiet = 0f; return; }
            if (rms >= threshold) {
                quiet = 0f;
                speaking += dt;
                if (!latched && speaking >= minimumVoiceSeconds) {
                    latched = true;
                    lastTrigger = Time.unscaledTime;
                    lab.TriggerVoiceForDevelopment();
                }
            } else {
                speaking = 0f;
                quiet += dt;
                if (quiet >= quietSeconds && Time.unscaledTime - lastTrigger >= cooldownSeconds) latched = false;
            }
        }

        private void OnDestroy()
        {
            if (device != null) Microphone.End(device);
            if (clip != null) Destroy(clip);
        }
    }
}
