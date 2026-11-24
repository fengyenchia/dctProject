using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace AudioBugs.Tutorial
{
    // Refresh the tracked-space pivot before XRI locomotion (-210 / -205).
    [DefaultExecutionOrder(-250)]
    public sealed class RightStickLook : MonoBehaviour, IXrTrackingPoseCompensator
    {
        public DesktopLab lab;
        public XROrigin xrOrigin;
        public InputActionReference lookAction;
        public InputActionReference resetAction;
        [Min(0f)] public float yawSpeed = 45f;
        [Min(0f)] public float pitchSpeed = 25f;
        [Range(0f, 45f)] public float maxPitch = 30f;
        [Range(0.05f, 0.8f)] public float deadzone = .2f;
        public bool enablePitch = true;

        public float Pitch => pitch;
        public bool InputArmed => inputArmed;
        public Transform TrackingSpace => offset;

        private Transform offset;
        private Vector3 neutralPosition;
        private Quaternion neutralRotation;
        private float pitch;
        private bool initialized, inputArmed, ownsLookAction, ownsResetAction, subscribed;

        private bool ResolveReferences()
        {
            if (initialized) return true;
            if (xrOrigin == null) xrOrigin = GetComponent<XROrigin>();
            if (lab == null || xrOrigin == null || xrOrigin.Camera == null ||
                xrOrigin.CameraFloorOffsetObject == null) return false;
            offset = xrOrigin.CameraFloorOffsetObject.transform;
            if (offset == xrOrigin.Origin.transform || xrOrigin.Camera.transform.parent != offset) return false;
            neutralPosition = offset.localPosition;
            neutralRotation = offset.localRotation;
            initialized = true;
            return true;
        }

        private void Awake()
        {
            if (ResolveReferences()) return;
            Debug.LogError("RightStickLook needs DesktopLab, XROrigin and a separate Camera Offset with the tracked camera as its child.", this);
            enabled = false;
        }

        private void OnEnable()
        {
            if (!ResolveReferences()) return;
            inputArmed = false;
            if (lookAction != null && !lookAction.action.enabled) { lookAction.action.Enable(); ownsLookAction = true; }
            if (resetAction != null && !resetAction.action.enabled) { resetAction.action.Enable(); ownsResetAction = true; }
            lab.onCalibrationRequested.AddListener(ResetLook);
            lab.onSessionReset.AddListener(ResetLook);
            Application.onBeforeRender += RefreshBeforeRender;
            subscribed = true;
        }

        private void Update()
        {
            if (!ResolveReferences()) return;
            // Correct the current raw pose before XRI reads capsule position/height.
            ApplyPitch();
            if (lookAction == null || !lookAction.action.enabled) { inputArmed = false; return; }
            ProcessInput(lookAction.action.ReadValue<Vector2>(),
                resetAction != null && resetAction.action.WasPressedThisFrame(), Time.deltaTime);
        }

        private void ProcessInput(Vector2 input, bool reset, float deltaTime)
        {
            if (!ResolveReferences()) return;
            if (!lab.useExternalPose || !lab.isActiveAndEnabled || !lab.CanMove || !isActiveAndEnabled ||
                float.IsNaN(input.x) || float.IsNaN(input.y) || float.IsInfinity(input.x) || float.IsInfinity(input.y)) {
                inputArmed = false;
                return;
            }
            if (reset) { ResetLook(); return; }
            if (input.sqrMagnitude <= deadzone * deadzone) { inputArmed = true; return; }
            if (!inputArmed) return;
            float dt = Mathf.Clamp(deltaTime, 0f, .1f);
            float yaw = FilterAxis(input.x) * Mathf.Max(0f, yawSpeed) * dt;
            if (yaw != 0f) xrOrigin.RotateAroundCameraPosition(Vector3.up, yaw);
            if (enablePitch) pitch = Mathf.Clamp(pitch - FilterAxis(input.y) * Mathf.Max(0f, pitchSpeed) * dt, -maxPitch, maxPitch);
            else pitch = 0f;
            ApplyPitch();
        }

        private float FilterAxis(float value)
        {
            float magnitude = Mathf.Abs(value);
            return magnitude <= deadzone ? 0f : Mathf.Sign(value) * Mathf.Clamp01((magnitude - deadzone) / (1f - deadzone));
        }

        private Vector3 NeutralPosition()
        {
            var position = neutralPosition;
            switch (xrOrigin.CurrentTrackingOriginMode) {
                case TrackingOriginModeFlags.Floor: position.y = 0f; break;
                case TrackingOriginModeFlags.Device:
                case TrackingOriginModeFlags.Unbounded: position.y = xrOrigin.CameraYOffset; break;
            }
            return position;
        }

        private void ApplyPitch()
        {
            if (!initialized) return;
            pitch = enablePitch ? Mathf.Clamp(pitch, -Mathf.Max(0f, maxPitch), Mathf.Max(0f, maxPitch)) : 0f;
            var rawHead = xrOrigin.Camera.transform.localPosition;
            var rotation = neutralRotation * Quaternion.AngleAxis(pitch, Vector3.right);
            // Keep the head's neutral world position; rotate head, hands, aim and tip together.
            offset.SetLocalPositionAndRotation(NeutralPosition() + neutralRotation * rawHead - rotation * rawHead, rotation);
        }

        public void RefreshTrackingPose() { if (isActiveAndEnabled && ResolveReferences()) ApplyPitch(); }

        private void LateUpdate() { RefreshTrackingPose(); }

        // Input System has refreshed tracking; run before XRI interactions (100).
        [BeforeRenderOrder(50)]
        private void RefreshBeforeRender() { RefreshTrackingPose(); }

        public void ResetLook()
        {
            pitch = 0f;
            inputArmed = false;
            if (ResolveReferences()) ApplyPitch();
            if (lab != null) lab.Rules.InvalidateStation();
        }

        private void OnDisable()
        {
            if (subscribed) {
                if (lab != null) { lab.onCalibrationRequested.RemoveListener(ResetLook); lab.onSessionReset.RemoveListener(ResetLook); }
                Application.onBeforeRender -= RefreshBeforeRender;
                subscribed = false;
            }
            if (ownsLookAction && lookAction != null) lookAction.action.Disable();
            if (ownsResetAction && resetAction != null) resetAction.action.Disable();
            ownsLookAction = ownsResetAction = false;
            pitch = 0f;
            inputArmed = false;
            if (initialized) ApplyPitch();
        }
    }
}
