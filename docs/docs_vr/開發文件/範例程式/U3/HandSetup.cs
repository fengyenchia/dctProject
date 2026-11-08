using UnityEngine;

namespace AudioBugs.Tutorial
{
    public sealed class HandSetup : MonoBehaviour
    {
        public DesktopLab lab;
        public Transform catcherRoot;
        public Transform leftController;
        public Transform rightController;
        public Behaviour leftUiRay;
        public Behaviour rightUiRay;
        public CaptureHaptics haptics;
        public LabPanel panel;

        public void SelectLeft() { Select(true); }
        public void SelectRight() { Select(false); }
        private void Select(bool left)
        {
            if (lab.Rules.Phase != GamePhase.Ready) return;
            catcherRoot.SetParent(left ? leftController : rightController, false);
            catcherRoot.localPosition = Vector3.zero;
            catcherRoot.localRotation = Quaternion.identity;
            catcherRoot.localScale = Vector3.one;
            leftUiRay.enabled = !left;
            rightUiRay.enabled = left;
            haptics.leftHand = left;
            panel.ClearHover();
        }
    }
}
