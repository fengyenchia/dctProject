using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AudioBugs.Tutorial;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AudioBugs.Editor
{
    public static class QuestLookSetup
    {
        public const string QuestPath = "Assets/_Project/Scenes/QuestLab.unity";

        public static void RefreshAllAndValidate()
        {
            RightStickLookSetup.ConfigureCaptureLabAndValidate();
            RefreshQuestLabAndValidate();
        }

        [MenuItem("Tools/AudioBugs/Refresh QuestLab From CaptureLab")]
        public static void RefreshQuestLabAndValidate()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play before refreshing QuestLab.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Refresh QuestLab", "Replace QuestLab with the saved CaptureLab, disable Simulator and reconnect tracking protection?", "Refresh", "Cancel")) return;
            string oldGuid = AssetDatabase.AssetPathToGUID(QuestPath);
            byte[] captureBefore = File.ReadAllBytes(RightStickLookSetup.CapturePath);
            var scene = EditorSceneManager.OpenScene(RightStickLookSetup.CapturePath);
            RightStickLookSetup.ValidateCaptureWiring();
            Require(EditorSceneManager.SaveScene(scene, QuestPath), "QuestLab could not be copied from CaptureLab.");
            var simulator = scene.GetRootGameObjects().SingleOrDefault(go => go.name == "XR Interaction Simulator");
            Require(simulator != null, "Exactly one XR Interaction Simulator must be present.");
            simulator.SetActive(false);
            var lab = Object.FindFirstObjectByType<DesktopLab>();
            var origin = Object.FindFirstObjectByType<XROrigin>();
            var look = Object.FindFirstObjectByType<RightStickLook>();
            var guard = lab.GetComponent<ExhibitGuard>() ?? lab.gameObject.AddComponent<ExhibitGuard>();
            guard.lab = lab;
            guard.origin = origin.Origin.transform;
            guard.trackingSpace = origin.CameraFloorOffsetObject.transform;
            guard.look = look;
            guard.calibrated = guard.safe = false;
            guard.reason = "Awaiting calibration";
            lab.trackingOrigin = guard.trackingSpace;
            lab.poseValid = lab.calibrationValid = false;
            EditorUtility.SetDirty(guard); EditorUtility.SetDirty(lab);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), "QuestLab tracking wiring could not be saved.");
            AssetDatabase.SaveAssets();
            Require(string.IsNullOrEmpty(oldGuid) || AssetDatabase.AssetPathToGUID(QuestPath) == oldGuid, "QuestLab GUID must be preserved.");
            Require(File.ReadAllBytes(RightStickLookSetup.CapturePath).SequenceEqual(captureBefore), "Refreshing QuestLab must not change CaptureLab.");
            RightStickLookSetup.ValidateCaptureWiring();
            Require(!simulator.activeSelf && origin.gameObject.activeInHierarchy, "Quest must use the Rig without Simulator.");
            Require(scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Camera>(true)).Count(camera => camera.isActiveAndEnabled) == 1,
                "QuestLab must have exactly one active camera.");
            Require(lab.xrLookProvider == look && guard.trackingSpace == origin.CameraFloorOffsetObject.transform,
                "Look and raw physical tracking space must be assigned.");
            RightStickLookSetup.ValidateRuntimeMathAndGates();
            RightStickLookSetup.ValidateInputBindings();
            ValidateBoundaryCoordinates(guard);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/QuestLookValidation.txt", "PASS: CaptureLab copied unchanged; QuestLab GUID retained; Simulator disabled; one camera; right-stick actions wired; guard raw-space bounds unaffected by translation/yaw/pitch; real head displacement detected. Hardware Link/comfort tests remain pending.\n");
            Debug.Log("QUEST_LOOK_VALIDATED: newest CaptureLab copied, Simulator disabled, look/guard wired, raw boundary math and all U3 input gates passed. Quest hardware validation remains pending.");
        }

        private static void ValidateBoundaryCoordinates(ExhibitGuard guard)
        {
            var lab = guard.lab; var look = guard.look;
            var root = guard.origin; var space = guard.trackingSpace; var head = lab.trackedHead;
            var rootPosition = root.position; var rootRotation = root.rotation;
            var offsetPosition = space.localPosition; var offsetRotation = space.localRotation;
            var headPosition = head.localPosition;
            var rulesField = typeof(DesktopLab).GetField("rules", BindingFlags.Instance | BindingFlags.NonPublic);
            var oldRules = rulesField.GetValue(lab);
            var centreField = typeof(ExhibitGuard).GetField("physicalCentre", BindingFlags.Instance | BindingFlags.NonPublic);
            var oldCentre = centreField.GetValue(guard);
            bool pose = lab.poseValid, calibration = lab.calibrationValid;
            try {
                var rules = new GameRules(17); rules.Begin(); rules.CompleteIntro(); rules.CompleteTutorial(); rulesField.SetValue(lab, rules);
                lab.poseValid = lab.calibrationValid = true;
                head.localPosition = new Vector3(.2f, 1.6f, .1f);
                look.ResetLook();
                centreField.SetValue(guard, space.InverseTransformPoint(head.position));
                root.position += new Vector3(15f, 0f, 9f);
                root.RotateAround(head.position, Vector3.up, 70f);
                var step = typeof(RightStickLook).GetMethod("ProcessInput", BindingFlags.Instance | BindingFlags.NonPublic);
                step.Invoke(look, new object[] { Vector2.zero, false, .1f });
                for (int i = 0; i < 15; i++) step.Invoke(look, new object[] { Vector2.up, false, .1f });
                Require(guard.PhysicalRadius < .001f, "Virtual translation/yaw/pitch must not change raw physical radius.");
                head.localPosition += Vector3.right * 1.6f;
                look.RefreshTrackingPose();
                Require(Mathf.Abs(guard.PhysicalRadius - 1.6f) < .001f, "Actual raw displacement must still detect the 1.5 m boundary.");
                guard.calibrated = true;
                typeof(ExhibitGuard).GetMethod("OriginChanged", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(guard, new object[] { null });
                Require(!guard.calibrated && !lab.calibrationValid && !lab.poseValid && lab.Rules.Paused && !lab.CanMove,
                    "Tracking-space changes must invalidate calibration and stop movement immediately.");
                lab.poseValid = lab.calibrationValid = true; lab.Rules.SetPaused(false);
                typeof(ExhibitGuard).GetMethod("FocusChanged", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(guard, new object[] { false });
                Require(!lab.poseValid && lab.Rules.Paused && !lab.CanMove, "Focus loss must immediately stop artificial movement.");
            } finally {
                look.ResetLook();
                head.localPosition = headPosition;
                space.SetLocalPositionAndRotation(offsetPosition, offsetRotation);
                root.SetPositionAndRotation(rootPosition, rootRotation);
                rulesField.SetValue(lab, oldRules); centreField.SetValue(guard, oldCentre);
                lab.poseValid = pose; lab.calibrationValid = calibration;
                guard.calibrated = guard.safe = false;
                guard.reason = "Awaiting calibration";
            }
        }

        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
