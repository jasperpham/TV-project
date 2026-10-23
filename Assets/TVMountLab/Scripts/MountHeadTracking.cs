using UnityEngine;
using UnityEngine.XR;

namespace TVMountLab
{
    public sealed class MountHeadTracking : MonoBehaviour
    {
        void OnEnable() { Application.onBeforeRender += Track; }
        void OnDisable() { Application.onBeforeRender -= Track; }
        void Update() { Track(); }
        void Track()
        {
            var device = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (!device.isValid) return;
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)) transform.localPosition = position;
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation)) transform.localRotation = rotation;
        }
    }
}
