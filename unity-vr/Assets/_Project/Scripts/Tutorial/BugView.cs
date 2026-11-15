using System.Collections.Generic;
using UnityEngine;

namespace AudioBugs.Tutorial
{
    [RequireComponent(typeof(BoxCollider), typeof(AudioSource))]
    public sealed class BugView : MonoBehaviour
    {
        public Transform visual;
        public GameObject captureVisual;
        [Range(0f, 1f)] public float baseVolume = .12f;
        [Range(0f, 1f)] public float outsideRoomGain = .45f;
        [Range(300f, 22000f)] public float behindWallCutoff = 1200f;
        public bool usePrototypeMaterial = true;
        private Renderer[] renderers;
        private AudioSource source;
        private AudioLowPassFilter wallFilter;
        private float roomGain = 1f;
        private float targetGain = 1f;
        private float captureGain = 1f;
        private bool muffled;
        private bool audioPaused;
        private MaterialPropertyBlock block;
        private readonly List<Material> ownedMaterials = new List<Material>();

        public void Initialize(Shader shader, AudioClip clip, Color color)
        {
            // Unity native objects must be created on the main thread, not in field initializers.
            if (block == null) block = new MaterialPropertyBlock();
            if (captureVisual != null) captureVisual.SetActive(false);
            renderers = (visual == null ? transform : visual).GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers) {
                if (usePrototypeMaterial) {
                    var material = new Material(shader);
                    material.SetColor("_BaseColor", color);
                    renderer.sharedMaterial = material;
                    ownedMaterials.Add(material);
                }
            }
            foreach (var collider in GetComponentsInChildren<Collider>(true))
                collider.enabled = collider.gameObject == gameObject;
            GetComponent<BoxCollider>().isTrigger = true;
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = clip;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 12f;
            source.loop = true;
            source.volume = baseVolume;
            source.Play();
        }

        public void Present(BugState state, bool paused, bool outsideRoom, bool wallBetween)
        {
            foreach (var renderer in renderers) {
                renderer.GetPropertyBlock(block);
                block.SetFloat("_Visibility", state.Visibility);
                renderer.SetPropertyBlock(block);
            }
            audioPaused = paused;
            captureGain = state.Gain;
            targetGain = outsideRoom ? outsideRoomGain : 1f;
            muffled = wallBetween;
            if (paused) source.Pause(); else source.UnPause();
        }

        private void LateUpdate()
        {
            if (source == null || audioPaused || captureGain <= 0f) return;
            float blend = 1f - Mathf.Exp(-12f * Time.deltaTime);
            roomGain = Mathf.Lerp(roomGain, targetGain, blend);
            source.volume = baseVolume * captureGain * roomGain;
            if (muffled && wallFilter == null) wallFilter = gameObject.AddComponent<AudioLowPassFilter>();
            if (wallFilter != null)
                wallFilter.cutoffFrequency = Mathf.Lerp(wallFilter.cutoffFrequency,
                    muffled ? behindWallCutoff : 22000f, blend);
        }

        public void ShowCaptured(Color previewColor)
        {
            GetComponent<BoxCollider>().enabled = false;
            captureGain = 0f;
            source.Stop();
            if (captureVisual != null) {
                foreach (var renderer in renderers) renderer.enabled = false;
                captureVisual.SetActive(true);
                return;
            }
            foreach (var renderer in renderers) {
                renderer.GetPropertyBlock(block);
                block.SetFloat("_Visibility", 1f);
                if (usePrototypeMaterial) block.SetColor("_BaseColor", previewColor);
                renderer.SetPropertyBlock(block);
            }
        }

        private void OnDestroy()
        {
            if (source != null) source.Stop();
            foreach (var material in ownedMaterials) Destroy(material);
        }
    }
}
