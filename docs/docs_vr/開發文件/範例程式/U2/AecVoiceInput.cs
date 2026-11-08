using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
namespace AudioBugs.Tutorial
{
    // 掛在唯一的 AudioListener 上；輸出混音只作 AEC 參考，不改播放資料。
    public sealed class AecVoiceInput : MonoBehaviour
    {
        public DesktopLab lab;
        public int deviceIndex;
        public float cleanRms, threshold = .01f;
        public bool audioReady;
        public string status;
        private IntPtr processor;
        private AudioClip clip;
        private string device;
        private readonly object gate = new object();
        private readonly float[] mic = new float[480], clean = new float[480];
        private long lastRender;
        private int readPosition;
        private float lastMicAt, speaking, quiet, lastTrigger = -10;
        private bool latched = true;
        [DllImport("exhibit_aec", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr aec_create();
        [DllImport("exhibit_aec", CallingConvention = CallingConvention.Cdecl)] private static extern void aec_destroy(IntPtr handle);
        [DllImport("exhibit_aec", CallingConvention = CallingConvention.Cdecl)] private static extern int aec_render(IntPtr handle, float[] data, int count, int channels);
        [DllImport("exhibit_aec", CallingConvention = CallingConvention.Cdecl)] private static extern int aec_capture(IntPtr handle, float[] input, float[] output);
        private void Start() {
            try {
                if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor)
                    throw new InvalidOperationException("AEC pilot uses Windows x64 DLL; Mac uses explicit preview only");
                if (AudioSettings.outputSampleRate != 48000) throw new InvalidOperationException("Set Unity output sample rate to 48000");
                if (Microphone.devices.Length == 0) throw new InvalidOperationException("No microphone");
                device = Microphone.devices[Mathf.Clamp(deviceIndex, 0, Microphone.devices.Length - 1)];
                clip = Microphone.Start(device, true, 2, 48000);
                if (clip == null || clip.channels != 1 || clip.frequency != 48000) throw new InvalidOperationException("Need mono 48000 Hz mic input");
                processor = aec_create(); if (processor == IntPtr.Zero) throw new InvalidOperationException("AEC init failed");
                lastMicAt = Time.unscaledTime; status = "Waiting for output reference and valid quiet";
            } catch (Exception error) { status = error.Message; Debug.LogError(status); enabled = false; }
        }
        private void OnAudioFilterRead(float[] data, int channels) {
            lock (gate) {
                if (processor != IntPtr.Zero && aec_render(processor, data, data.Length, channels) == 1)
                    Interlocked.Exchange(ref lastRender, System.Diagnostics.Stopwatch.GetTimestamp());
            }
        }
        private void Update() {
            bool reference = (System.Diagnostics.Stopwatch.GetTimestamp() - Interlocked.Read(ref lastRender))
                / (double)System.Diagnostics.Stopwatch.Frequency < .2;
            int write = clip == null ? -1 : Microphone.GetPosition(device);
            if (!reference || write < 0 || !Microphone.IsRecording(device)) { NotReady("Missing fresh playback reference or mic"); return; }
            int available = (write - readPosition + clip.samples) % clip.samples;
            if (Time.unscaledTime - lastMicAt > .2f || available > 9600) {
                readPosition = write; lastMicAt = Time.unscaledTime; NotReady("Resync input; wait for quiet"); return;
            }
            if (available < mic.Length) { if (Time.unscaledTime - lastMicAt > .15f) NotReady("Stale mic"); return; }
            while (available >= mic.Length) {
                if (!clip.GetData(mic, readPosition)) { NotReady("Read failed"); return; }
                readPosition = (readPosition + mic.Length) % clip.samples; available -= mic.Length; lastMicAt = Time.unscaledTime;
                int vad; lock (gate) vad = aec_capture(processor, mic, clean);
                if (vad < 0) { NotReady("AEC failed"); return; }
                float sum = 0; foreach (float value in clean) sum += value * value;
                cleanRms = Mathf.Sqrt(sum / clean.Length); audioReady = true; lab.audioReady = true;
                if (!lab.CanInteract || lab.Rules.Phase != GamePhase.Active) { speaking = quiet = 0; latched = true; continue; }
                if (vad == 1 && cleanRms >= threshold) {
                    quiet = 0; speaking += .01f;
                    if (!latched && speaking >= .35f) { latched = true; lastTrigger = Time.unscaledTime; lab.TriggerVoiceForDevelopment(); }
                } else { speaking = 0; quiet += .01f; if (quiet >= .4f && Time.unscaledTime - lastTrigger >= 1) latched = false; }
                status = latched ? "Speech latched; wait for quiet" : "Audio ready";
            }
        }
        private void NotReady(string why) { audioReady = false; if (lab != null) lab.audioReady = false; speaking = quiet = 0; latched = true; status = why; }
        private void OnDestroy() {
            if (device != null) Microphone.End(device);
            lock (gate) { if (processor != IntPtr.Zero) { aec_destroy(processor); processor = IntPtr.Zero; } }
            if (clip != null) Destroy(clip); if (lab != null) lab.audioReady = false;
        }
    }
}
