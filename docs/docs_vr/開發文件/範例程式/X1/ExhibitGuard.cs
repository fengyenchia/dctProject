using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;

namespace AudioBugs.Tutorial
{
    // QuestLab only. Validate raw tracking before gameplay (-300) and locomotion.
    [DefaultExecutionOrder(-400)]
    public sealed class ExhibitGuard : MonoBehaviour
    {
        public DesktopLab lab;
        public Transform origin;
        public Transform trackingSpace;
        public RightStickLook look;
        public string reason = "Awaiting calibration";
        public bool calibrated, safe;
        private Vector3 physicalCentre;
        private readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
        private readonly List<XRInputSubsystem> inputs = new List<XRInputSubsystem>();
        private readonly List<XRDisplaySubsystem> foundDisplays = new List<XRDisplaySubsystem>();
        private readonly List<XRInputSubsystem> foundInputs = new List<XRInputSubsystem>();

        public float PhysicalRadius {
            get {
                if (trackingSpace == null || lab == null || lab.trackedHead == null) return float.PositiveInfinity;
                var offset = trackingSpace.InverseTransformPoint(lab.trackedHead.position) - physicalCentre;
                return new Vector2(offset.x, offset.z).magnitude;
            }
        }

        private void OnEnable()
        {
            if (lab == null || origin == null || trackingSpace == null || look == null) {
                Debug.LogError("ExhibitGuard needs Lab, upright XR Origin, Camera Offset tracking space and RightStickLook.", this);
                if (lab != null) { lab.poseValid = false; lab.calibrationValid = false; lab.SetPaused(true); }
                enabled = false;
                return;
            }
            calibrated = safe = false;
            lab.calibrationValid = false;
            StopInteraction("Stand at physical centre and Calibrate");
            lab.onCalibrationRequested.AddListener(Calibrate);
            lab.onSessionReset.AddListener(SessionReset);
            RefreshSubsystems();
        }

        private void RefreshSubsystems()
        {
            SubsystemManager.GetSubsystems(foundDisplays);
            SubsystemManager.GetSubsystems(foundInputs);
            bool changed = displays.Count != foundDisplays.Count || inputs.Count != foundInputs.Count;
            foreach (var display in displays) if (!foundDisplays.Contains(display)) changed = true;
            foreach (var input in inputs) if (!foundInputs.Contains(input)) changed = true;
            for (int i = displays.Count - 1; i >= 0; i--) {
                if (foundDisplays.Contains(displays[i])) continue;
                displays[i].displayFocusChanged -= FocusChanged;
                displays.RemoveAt(i);
            }
            for (int i = inputs.Count - 1; i >= 0; i--) {
                if (foundInputs.Contains(inputs[i])) continue;
                inputs[i].trackingOriginUpdated -= OriginChanged;
                inputs.RemoveAt(i);
            }
            foreach (var display in foundDisplays) {
                if (displays.Contains(display)) continue;
                displays.Add(display);
                display.displayFocusChanged += FocusChanged;
            }
            foreach (var input in foundInputs) {
                if (inputs.Contains(input)) continue;
                inputs.Add(input);
                input.trackingOriginUpdated += OriginChanged;
            }
            if (changed && calibrated) InvalidateTrackingSpace("XR subsystem changed; recalibrate");
        }

        private void FocusChanged(bool value) { if (!value) StopInteraction("XR display lost focus"); }
        private void OriginChanged(XRInputSubsystem _) { InvalidateTrackingSpace("Tracking origin changed; recalibrate"); }

        private void InvalidateTrackingSpace(string why)
        {
            calibrated = false;
            lab.calibrationValid = false;
            lab.Rules.InvalidateStation();
            StopInteraction(why);
        }

        private void SessionReset()
        {
            calibrated = false;
            lab.calibrationValid = false;
            StopInteraction("Next session: check physical centre and Calibrate");
        }

        private static bool Tracked(XRNode node)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            return device.isValid && device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked
                && device.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState state)
                && (state & (InputTrackingState.Position | InputTrackingState.Rotation)) == (InputTrackingState.Position | InputTrackingState.Rotation);
        }

        private bool TrackingReady()
        {
            RefreshSubsystems();
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            return isActiveAndEnabled && foundDisplays.Exists(display => display.running) && foundInputs.Exists(input => input.running)
                && SessionFocused()
                && Tracked(XRNode.Head) && Tracked(XRNode.LeftHand) && Tracked(XRNode.RightHand)
                && head.TryGetFeatureValue(CommonUsages.userPresence, out bool worn) && worn;
        }

        private static bool SessionFocused()
        {
            // Query the current OpenXR state, including after late enable/reconnect.
            try { return OpenXRUtility.IsSessionFocused; }
            catch (System.DllNotFoundException) { return false; }
            catch (System.EntryPointNotFoundException) { return false; }
        }

        private void StopInteraction(string why)
        {
            safe = false; reason = why;
            if (lab == null) return;
            lab.poseValid = false;
            lab.SetPaused(true);
        }

        private void Update()
        {
            if (!TrackingReady()) { StopInteraction("Check Link, worn state, focus and both controllers"); return; }
            if (!calibrated) { StopInteraction("Stand at physical centre and Calibrate"); return; }
            float radius = PhysicalRadius;
            if (radius > 1.5f) { StopInteraction("Outside 1.5 m radius; return to centre"); return; }
            safe = true; lab.poseValid = true;
            reason = radius > 1.2f ? "Near boundary" : "Ready; resume explicitly if paused";
        }

        [ContextMenu("Calibrate at physical centre")]
        public void Calibrate()
        {
            if (!TrackingReady() || lab.trackedHead == null) { StopInteraction("Cannot calibrate without valid Link pose"); return; }
            lab.SetPaused(true);
            // Also covers this component's own button, not only the shared event.
            look.ResetLook();
            look.RefreshTrackingPose();
            var head = lab.trackedHead;
            origin.RotateAround(head.position, Vector3.up, -head.eulerAngles.y);
            if (lab.Rules.Phase == GamePhase.Ready) {
                var entry = lab.exitAnchor == null ? Vector3.zero : lab.exitAnchor.position;
                origin.position += new Vector3(entry.x - head.position.x, 0f, entry.z - head.position.z);
            }
            physicalCentre = trackingSpace.InverseTransformPoint(head.position);
            lab.SetPhysicalTrackingCentre(trackingSpace, physicalCentre);
            lab.Rules.InvalidateStation();
            calibrated = safe = true;
            lab.poseValid = lab.calibrationValid = true;
            reason = "Calibrated; check the entry and Resume";
        }

        private void OnGUI()
        {
            if (lab == null || !lab.showDeveloperPanel) return;
            GUILayout.BeginArea(new Rect(12, 390, 560, 100), GUI.skin.box);
            GUILayout.Label("QUEST GUARD | " + reason);
            if (GUILayout.Button("Calibrate at physical centre")) Calibrate();
            GUILayout.EndArea();
        }

        private void OnDisable()
        {
            foreach (var display in displays) display.displayFocusChanged -= FocusChanged;
            foreach (var input in inputs) input.trackingOriginUpdated -= OriginChanged;
            displays.Clear(); inputs.Clear();
            if (lab == null) return;
            lab.onCalibrationRequested.RemoveListener(Calibrate);
            lab.onSessionReset.RemoveListener(SessionReset);
            lab.calibrationValid = false;
            StopInteraction("Tracking guard disabled; re-enable and recalibrate");
        }
    }
}
