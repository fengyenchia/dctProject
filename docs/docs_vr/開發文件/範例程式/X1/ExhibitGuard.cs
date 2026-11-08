using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
namespace AudioBugs.Tutorial
{
    // 只掛 QuestLab；桌面模擬不以缺少實體裝置當成失效。
    [DefaultExecutionOrder(-100)]
    public sealed class ExhibitGuard : MonoBehaviour
    {
        public DesktopLab lab;
        public Transform origin;
        public string reason = "Awaiting calibration";
        public bool calibrated, safe;
        private bool displayFocused = true;
        private readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
        private readonly List<XRInputSubsystem> inputs = new List<XRInputSubsystem>();
        private void Start() {
            lab.poseValid = false; lab.calibrationValid = false;
            lab.onCalibrationRequested.AddListener(Calibrate);
            SubsystemManager.GetSubsystems(displays); SubsystemManager.GetSubsystems(inputs);
            foreach (var display in displays) display.displayFocusChanged += FocusChanged;
            foreach (var input in inputs) input.trackingOriginUpdated += OriginChanged;
        }
        private void FocusChanged(bool value) { displayFocused = value; if (!value) StopInteraction("XR display lost focus"); }
        private void OriginChanged(XRInputSubsystem _) { calibrated = false; lab.calibrationValid = false; lab.Rules.InvalidateStation(); StopInteraction("Tracking origin changed; recalibrate"); }
        private static bool Tracked(XRNode node) {
            var d = InputDevices.GetDeviceAtXRNode(node);
            return d.isValid && d.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked
                && d.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState state)
                && (state & (InputTrackingState.Position | InputTrackingState.Rotation)) == (InputTrackingState.Position | InputTrackingState.Rotation);
        }
        private bool TrackingReady() {
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            // 工作人員可操作另一個桌面視窗；遊戲仍以頭戴的 XR 焦點判定。
            return displayFocused && displays.Exists(d => d.running)
                && Tracked(XRNode.Head) && Tracked(XRNode.LeftHand) && Tracked(XRNode.RightHand)
                && head.TryGetFeatureValue(CommonUsages.userPresence, out bool worn) && worn;
        }
        private void StopInteraction(string why) { safe = false; reason = why; lab.poseValid = false; lab.SetPaused(true); }
        private void Update() {
            if (!TrackingReady()) { StopInteraction("Check Link, worn state, focus and both controllers"); return; }
            if (!calibrated) { StopInteraction("Stand at physical centre and Calibrate"); return; }
            float radius = new Vector2(lab.trackedHead.position.x, lab.trackedHead.position.z).magnitude;
            if (radius > 1.5f) { StopInteraction("Outside 1.5 m radius; return to centre"); return; }
            safe = true; lab.poseValid = true; reason = radius > 1.2f ? "Near boundary" : "Ready; resume explicitly if paused";
        }
        public void Calibrate() {
            if (!TrackingReady() || origin == null || lab.trackedHead == null) { StopInteraction("Cannot calibrate without valid Link pose"); return; }
            lab.SetPaused(true); var head = lab.trackedHead;
            origin.RotateAround(head.position, Vector3.up, -head.eulerAngles.y);
            origin.position -= new Vector3(head.position.x, 0, head.position.z);
            lab.Rules.InvalidateStation(); calibrated = true; lab.calibrationValid = true;
            safe = true; lab.poseValid = true; reason = "Calibrated; check station and Resume";
        }
        private void OnGUI() { GUILayout.BeginArea(new Rect(12, 390, 560, 100), GUI.skin.box);
            GUILayout.Label("QUEST GUARD | " + reason);
            if (GUILayout.Button("Calibrate at physical centre")) Calibrate(); GUILayout.EndArea(); }
        private void OnDestroy() {
            foreach (var display in displays) display.displayFocusChanged -= FocusChanged;
            foreach (var input in inputs) input.trackingOriginUpdated -= OriginChanged;
            if (lab != null) lab.onCalibrationRequested.RemoveListener(Calibrate);
        }
    }
}
