using UnityEngine;
using UnityEngine.UI;

namespace AudioBugs.Tutorial
{
    public sealed class CaptureModelPreview : MonoBehaviour
    {
        public DesktopLab lab;
        public Camera previewCamera;
        public RawImage display;
        public Transform modelRoot;
        public GameObject[] variantModels = new GameObject[GameRules.VariantSlotCount];
        [Min(0.1f)] public float seconds = 1.5f;

        private GameObject model;
        private GameRules activeRules;
        private float remaining;

        private void OnEnable()
        {
            Hide();
            if (lab != null) lab.onCaptured.AddListener(ShowLatestCapture);
        }

        private void Update()
        {
            if (model == null) return;
            if (lab.Rules != activeRules || lab.Rules.Phase == GamePhase.Recycling) {
                Hide();
                return;
            }
            if (lab.Rules.Paused) return;
            remaining -= Time.deltaTime;
            if (remaining <= 0f) Hide();
        }

        private void ShowLatestCapture()
        {
            Hide();
            if (previewCamera == null || display == null || modelRoot == null
                || previewCamera.targetTexture == null) return;

            var collected = lab.Rules.Collected;
            if (collected.Count == 0) return;
            int variant = collected[collected.Count - 1];
            if (variant >= variantModels.Length || variantModels[variant] == null) return;

            model = Instantiate(variantModels[variant], modelRoot, false);
            foreach (var part in model.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = modelRoot.gameObject.layer;
            activeRules = lab.Rules;
            remaining = seconds;
            display.texture = previewCamera.targetTexture;
            display.enabled = true;
            previewCamera.enabled = true;
        }

        private void Hide()
        {
            if (model != null) {
                model.SetActive(false);
                Destroy(model);
            }
            model = null;
            if (display != null) display.enabled = false;
            if (previewCamera != null) previewCamera.enabled = false;
        }

        private void OnDisable()
        {
            if (lab != null) lab.onCaptured.RemoveListener(ShowLatestCapture);
            Hide();
        }
    }
}
