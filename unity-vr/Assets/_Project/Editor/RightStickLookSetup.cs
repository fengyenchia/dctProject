using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AudioBugs.Tutorial;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace AudioBugs.Editor
{
    public static class RightStickLookSetup
    {
        public const string CapturePath = "Assets/_Project/Scenes/CaptureLab.unity";
        public const string InputPath = "Assets/_Project/Data/RightStickLook.inputactions";

        [MenuItem("Tools/AudioBugs/Configure CaptureLab Right Stick Look")]
        public static void ConfigureCaptureLabAndValidate()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play before configuring CaptureLab.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(CapturePath);
            var lab = Object.FindFirstObjectByType<DesktopLab>();
            var origin = Object.FindFirstObjectByType<XROrigin>();
            Require(lab != null && origin != null, "CaptureLab needs DesktopLab and one XR Origin.");
            Require(origin.Camera != null && origin.CameraFloorOffsetObject != null, "Tracked head and Camera Offset must exist.");
            var turn = origin.transform.Find("Locomotion/Turn");
            var teleport = origin.transform.Find("Locomotion/Teleportation");
            if (turn != null) turn.gameObject.SetActive(false);
            if (teleport != null) teleport.gameObject.SetActive(false);
            Require(lab.xrMoveProvider != null, "DesktopLab XR Move Provider must be assigned.");
            var move = new SerializedObject(lab.xrMoveProvider);
            var rightMove = move.FindProperty("m_RightHandMoveInput.m_InputSourceMode");
            Require(rightMove != null, "XRI Move Provider input field was not found.");
            rightMove.intValue = 0; // XRInputValueReader.InputSourceMode.Unused
            move.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(lab.xrMoveProvider);
            var look = origin.GetComponent<RightStickLook>() ?? origin.gameObject.AddComponent<RightStickLook>();
            look.lab = lab;
            look.xrOrigin = origin;
            lab.xrLookProvider = look;
            look.lookAction = FindActionReference("Look");
            look.resetAction = FindActionReference("ResetPitch");
            look.yawSpeed = 45f;
            look.pitchSpeed = 25f;
            look.maxPitch = 30f;
            look.deadzone = .2f;
            look.enablePitch = true;
            EditorUtility.SetDirty(look);
            EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
            Require(EditorSceneManager.SaveScene(lab.gameObject.scene), "CaptureLab could not be saved.");
            AssetDatabase.SaveAssets();
            ValidateRuntimeMathAndGates();
            ValidateCaptureWiring();
            Debug.Log("RIGHT_STICK_LOOK_VALIDATED: CaptureLab wiring, yaw, pitch, pivot, bounds, pause, tracking, UI, neutral rearm and reset.");
        }

        public static InputActionReference FindActionReference(string actionName)
        {
            var reference = AssetDatabase.LoadAllAssetsAtPath(InputPath).OfType<InputActionReference>()
                .FirstOrDefault(item => item.action != null && item.action.name == actionName);
            Require(reference != null, "Missing input action reference: " + actionName);
            return reference;
        }

        public static void ValidateCaptureWiring()
        {
            var look = Object.FindFirstObjectByType<RightStickLook>();
            Require(look != null && look.lab != null && look.xrOrigin != null && look.lookAction != null && look.resetAction != null,
                "Right-stick scene references must all be assigned.");
            Require(look.lookAction.action.bindings.Any(binding => binding.path == "<XRController>{RightHand}/primary2DAxis"),
                "Look must use the right controller's complete Vector2 stick input.");
            Require(look.resetAction.action.bindings.Any(binding => binding.path == "<XRController>{RightHand}/primary2DAxisClick"),
                "Reset must use the right stick click.");
            Require(look.xrOrigin.transform.Find("Locomotion/Turn")?.gameObject.activeSelf == false, "Built-in Turn must remain disabled.");
            var move = new SerializedObject(look.lab.xrMoveProvider);
            Require(move.FindProperty("m_RightHandMoveInput.m_InputSourceMode").intValue == 0,
                "Right stick cannot simultaneously move and turn.");
        }

        public static void ValidateRuntimeMathAndGates()
        {
            var testRoot = new GameObject("RightStickLookValidation");
            var labObject = new GameObject("ValidationLab");
            RightStickLook look = null;
            try {
                var offset = new GameObject("Camera Offset").transform;
                offset.SetParent(testRoot.transform, false);
                var head = new GameObject("Tracked Head");
                head.transform.SetParent(offset, false);
                head.transform.localPosition = new Vector3(.2f, 1.65f, .15f);
                head.transform.localRotation = Quaternion.Euler(5f, 12f, 0f);
                var hand = new GameObject("Tracked Hand").transform;
                hand.SetParent(offset, false);
                hand.localPosition = new Vector3(.4f, 1.2f, .5f);
                var camera = head.AddComponent<Camera>();
                var origin = testRoot.AddComponent<XROrigin>();
                origin.Origin = testRoot;
                origin.Camera = camera;
                origin.CameraFloorOffsetObject = offset.gameObject;
                var lab = labObject.AddComponent<DesktopLab>();
                var moveGateProbe = labObject.AddComponent<Light>();
                lab.xrMoveProvider = moveGateProbe;
                lab.useExternalPose = true;
                lab.trackedHead = head.transform;
                lab.trackedAim = hand;
                lab.trackedTip = hand;
                lab.Rules.Begin(); lab.Rules.CompleteIntro(); lab.Rules.CompleteTutorial();
                look = testRoot.AddComponent<RightStickLook>();
                look.lab = lab; look.xrOrigin = origin;
                look.lookAction = FindActionReference("Look"); look.resetAction = FindActionReference("ResetPitch");
                Invoke(look, "OnEnable");
                var rawHead = head.transform.localPosition;
                var rawRotation = head.transform.localRotation;
                var neutralWorld = head.transform.position;
                var neutralHandDelta = hand.position - head.transform.position;
                InvokeInput(look, Vector2.zero);
                for (int i = 0; i < 60; i++) InvokeInput(look, Vector2.up);
                Require(Mathf.Abs(look.Pitch + 30f) < .001f, "Up input must look up and clamp at -30 degrees.");
                Require(Vector3.Distance(head.transform.position, neutralWorld) < .001f, "Pitch must preserve world head height/position.");
                Require(head.transform.localPosition == rawHead && Quaternion.Angle(head.transform.localRotation, rawRotation) < .001f,
                    "TrackedPoseDriver's raw pose must not be overwritten.");
                Require(Vector3.Distance(hand.position - head.transform.position,
                    Quaternion.AngleAxis(-30f, Vector3.right) * neutralHandDelta) < .001f, "Head and controller aim must pitch together.");
                head.transform.localPosition += new Vector3(.3f, 0f, 0f);
                Invoke(look, "RefreshBeforeRender");
                Require(Vector3.Distance(head.transform.position, neutralWorld + Vector3.right * .3f) < .001f,
                    "Fresh before-render tracking must preserve neutral translation.");
                Require(Vector3.Distance(offset.InverseTransformPoint(head.transform.position), head.transform.localPosition) < .001f,
                    "Physical boundary coordinates must remain raw tracking coordinates.");
                look.ResetLook();
                InvokeInput(look, Vector2.right); // Must remain disarmed until neutral.
                Require(Quaternion.Angle(testRoot.transform.rotation, Quaternion.identity) < .001f, "Reset requires stick neutral.");
                InvokeInput(look, Vector2.zero); InvokeInput(look, Vector2.right);
                Require(Mathf.Abs(Mathf.DeltaAngle(testRoot.transform.eulerAngles.y, 4.5f)) < .001f, "Right input must yaw right at configured rate.");
                Require(Vector3.Distance(testRoot.transform.up, Vector3.up) < .001f, "Root must stay upright for gravity/capsule movement.");
                var rotation = testRoot.transform.rotation;
                var position = head.transform.position;
                lab.SetPaused(true); InvokeInput(look, Vector2.one);
                Require(!moveGateProbe.enabled, "SetPaused must disable the XRI move provider immediately.");
                Require(Quaternion.Angle(rotation, testRoot.transform.rotation) < .001f && look.Pitch == 0f, "Pause must stop yaw and pitch.");
                head.transform.localRotation = Quaternion.Euler(15f, 20f, 0f); Invoke(look, "RefreshBeforeRender");
                Require(Quaternion.Angle(head.transform.localRotation, Quaternion.Euler(15f, 20f, 0f)) < .001f,
                    "Head tracking must continue while paused.");
                lab.SetPaused(false); InvokeInput(look, Vector2.one);
                Require(Quaternion.Angle(rotation, testRoot.transform.rotation) < .001f, "Held stick cannot resume artificial movement.");
                InvokeInput(look, Vector2.zero); InvokeInput(look, Vector2.one);
                Require(Quaternion.Angle(rotation, testRoot.transform.rotation) > 1f && look.Pitch < 0f, "Neutral must rearm both axes.");
                look.enablePitch = false; Invoke(look, "RefreshBeforeRender");
                Require(look.Pitch == 0f, "Disabling pitch must restore neutral without another stick movement.");
                look.enablePitch = true;
                CheckGate(look, lab, () => lab.poseValid = false, () => lab.poseValid = true, "tracking loss");
                CheckGate(look, lab, () => lab.uiBusy = true, () => lab.uiBusy = false, "UI interaction");
                CheckGate(look, lab, () => lab.calibrationValid = false, () => lab.calibrationValid = true, "invalid calibration");
                CheckGate(look, lab, () => lab.useExternalPose = false, () => lab.useExternalPose = true, "desktop pose mode");
                InvokeInput(look, Vector2.zero); InvokeInput(look, Vector2.up);
                var yaw = testRoot.transform.rotation;
                InvokeInput(look, Vector2.zero, true);
                Require(look.Pitch == 0f && Quaternion.Angle(yaw, testRoot.transform.rotation) < .001f && lab.Rules.San == 100,
                    "Stick click must reset only artificial pitch, not yaw or gameplay data.");
                InvokeInput(look, Vector2.zero); InvokeInput(look, Vector2.up);
                var savedPitch = look.Pitch;
                lab.onResetRequested.Invoke();
                Require(!lab.ResetForNextSession() && Mathf.Approximately(savedPitch, look.Pitch), "Rejected reset requests must preserve pitch.");
                lab.onCalibrationRequested.Invoke();
                Require(look.Pitch == 0f && !look.InputArmed, "Calibration must reset the artificial view offset.");
                InvokeInput(look, Vector2.zero); InvokeInput(look, Vector2.up);
                lab.AbortSession();
                Require(look.Pitch == 0f && !look.InputArmed, "Actual abort must reset pitch.");
                lab.Rules.Begin(); lab.Rules.CompleteIntro(); lab.Rules.CompleteTutorial();
                InvokeInput(look, Vector2.zero); InvokeInput(look, Vector2.up);
                var target = lab.Rules.Groups.First().Id;
                lab.Rules.Tick(3.1f, target);
                lab.Rules.AuthorizeStation(true, true, true, true);
                Require(lab.Rules.ConfirmRecycle(true, true) && lab.Rules.CompleteRepair() && lab.Rules.ArriveAtExit(true) &&
                    lab.Rules.CompleteEndingVideo() && lab.Rules.OpenThankYou() && lab.Rules.FinishThankYou(), "Test must run the actual ending flow.");
                Require(lab.ResetForNextSession() && look.Pitch == 0f && !look.InputArmed, "Successful next-session reset must clear pitch.");
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/RightStickLookValidation.txt", "PASS: yaw, pitch limits, raw tracking, head pivot, hands, before-render translation, upright root, pause, tracking, UI, neutral rearm, stick reset, new session, calibration.\n");
            } finally {
                if (look != null) Invoke(look, "OnDisable");
                Object.DestroyImmediate(testRoot);
                Object.DestroyImmediate(labObject);
            }
        }

        private static void CheckGate(RightStickLook look, DesktopLab lab, Action stop, Action resume, string label)
        {
            var rotation = look.xrOrigin.transform.rotation; var pitch = look.Pitch;
            stop(); InvokeInput(look, Vector2.one); resume(); InvokeInput(look, Vector2.one);
            Require(Quaternion.Angle(rotation, look.xrOrigin.transform.rotation) < .001f && Mathf.Approximately(pitch, look.Pitch),
                label + " must stop movement and require a fresh neutral input.");
        }

        private static void InvokeInput(RightStickLook look, Vector2 input, bool reset = false)
        {
            typeof(RightStickLook).GetMethod("ProcessInput", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(look, new object[] { input, reset, .1f });
        }

        private static void Invoke(RightStickLook look, string name)
        {
            typeof(RightStickLook).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(look, null);
        }

        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
