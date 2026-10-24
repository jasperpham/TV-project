using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace TVMountLab
{
    public sealed class MountTrackingOrigin : MonoBehaviour
    {
        readonly List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
        float nextRefresh;
        void OnEnable() { Refresh(); }
        void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 1f;
            Refresh();
        }
        void OnDisable() { Unsubscribe(); }
        void Unsubscribe()
        {
            foreach (var subsystem in subsystems) subsystem.trackingOriginUpdated -= SetFloor;
        }
        void Refresh()
        {
            // XR may initialize after scene objects, or restart after headset sleep.
            Unsubscribe();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var subsystem in subsystems)
            {
                subsystem.trackingOriginUpdated += SetFloor;
                SetFloor(subsystem);
            }
        }
        void SetFloor(XRInputSubsystem subsystem)
        {
            if (subsystem.running && subsystem.GetTrackingOriginMode() != TrackingOriginModeFlags.Floor &&
                (subsystem.GetSupportedTrackingOriginModes() & TrackingOriginModeFlags.Floor) != 0)
                subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
        }
    }
}
