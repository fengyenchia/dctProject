using System.Collections.Generic;
using UnityEngine;

namespace AudioBugs.Tutorial
{
    [RequireComponent(typeof(BoxCollider), typeof(AudioSource))]
    public sealed class BugView : MonoBehaviour
    {
        public Transform visual;
        [Range(0f, 1f)] public float baseVolume = .12f;
        public bool usePrototypeMaterial = true;
        private Renderer[] renderers;
        private AudioSource source;
        private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        private readonly List<Material> ownedMaterials = new List<Material>();

        public void Initialize(Shader shader, AudioClip clip, Color color)
        {
            renderers = (visual == null ? transform : visual).GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers) {
                if (usePrototypeMaterial) {
                    var material = new Material(shader);
                    material.SetColor("_BaseColor", color);
                    renderer.sharedMaterial = material;
                    ownedMaterials.Add(material);
                }
            }
            foreach (var collider in GetComponentsInChildren<Collider>())
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

        public void Present(BugState state, bool paused)
        {
            foreach (var renderer in renderers) {
                renderer.GetPropertyBlock(block);
                block.SetFloat("_Visibility", state.Visibility);
                renderer.SetPropertyBlock(block);
            }
            source.volume = baseVolume * state.Gain;
            if (paused) source.Pause(); else source.UnPause();
        }

        private void OnDestroy()
        {
            if (source != null) source.Stop();
            foreach (var material in ownedMaterials) Destroy(material);
        }
    }
}
