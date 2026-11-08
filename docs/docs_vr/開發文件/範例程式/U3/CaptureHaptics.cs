using UnityEngine;
using UnityEngine.XR;

namespace AudioBugs.Tutorial
{
    public sealed class CaptureHaptics : MonoBehaviour
    {
        public bool leftHand;
        public void Pulse()
        {
            var device = InputDevices.GetDeviceAtXRNode(leftHand ? XRNode.LeftHand : XRNode.RightHand);
            if (device.isValid && device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                device.SendHapticImpulse(0, .3f, .1f);
        }
    }
}
