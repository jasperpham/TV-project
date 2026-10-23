using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace TVMountLab
{
    [DisallowMultipleComponent]
    public sealed class MountHandInteractor : MonoBehaviour
    {
        public XRNode node = XRNode.LeftHand;
        public Renderer[] handRenderers;
        public MountGrabbable[] items;
        [Min(0.01f)] public float grabRadius = 0.12f;
        public MountGrabbable HeldItem { get; private set; }
        bool[] rendererStates;
        bool wasPressed, hadTracking;
        InputAction gripAction;

        void OnEnable()
        {
            string usage = node == XRNode.LeftHand ? "LeftHand" : "RightHand";
            gripAction = new InputAction("Mount grip " + usage, InputActionType.Value, "<XRController>{" + usage + "}/grip");
            gripAction.Enable();
        }

        void Update()
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            bool tracked = device.isValid && device.TryGetFeatureValue(CommonUsages.isTracked, out bool value) && value;
            if (!tracked)
            {
                if (hadTracking) Release();
                hadTracking = false;
                wasPressed = false;
                return;
            }
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)) transform.localPosition = position;
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation)) transform.localRotation = rotation;
            float grip = gripAction != null ? gripAction.ReadValue<float>() : 0f;
            bool pressed = grip >= (wasPressed ? 0.35f : 0.65f);
            if (!hadTracking) { wasPressed = pressed; hadTracking = true; return; }
            if (pressed && !wasPressed) GrabNearest();
            if (!pressed && wasPressed) Release();
            wasPressed = pressed;
        }

        public bool GrabNearest()
        {
            MountGrabbable nearest = null;
            float distance = grabRadius;
            foreach (var item in items)
            {
                if (!item || !item.isActiveAndEnabled || item.Owner) continue;
                float candidate = item.DistanceTo(transform.position);
                if (candidate <= distance) { nearest = item; distance = candidate; }
            }
            return nearest && TryGrab(nearest);
        }

        public bool TryGrab(MountGrabbable item)
        {
            if (HeldItem || !item || !item.Attach(this)) return false;
            HeldItem = item;
            rendererStates = new bool[handRenderers.Length];
            for (int i = 0; i < handRenderers.Length; i++)
            {
                if (!handRenderers[i]) continue;
                rendererStates[i] = handRenderers[i].enabled;
                handRenderers[i].enabled = false;
            }
            return true;
        }

        public void Release()
        {
            if (HeldItem) HeldItem.Detach(this);
            HeldItem = null;
            if (rendererStates != null)
                for (int i = 0; i < handRenderers.Length && i < rendererStates.Length; i++)
                    if (handRenderers[i]) handRenderers[i].enabled = rendererStates[i];
            rendererStates = null;
        }

        void OnDisable() { Release(); wasPressed = false; hadTracking = false; gripAction?.Dispose(); gripAction = null; }
        void OnApplicationFocus(bool focused) { if (!focused) { Release(); hadTracking = false; } }
        void OnApplicationPause(bool paused) { if (paused) { Release(); hadTracking = false; } }
        void OnDrawGizmosSelected() { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, grabRadius); }
    }
}
