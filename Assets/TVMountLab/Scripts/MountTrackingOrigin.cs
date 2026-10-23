using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace TVMountLab
{
    public sealed class MountTrackingOrigin : MonoBehaviour
    {
        readonly List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
        void OnEnable() { SubsystemManager.GetSubsystems(subsystems); foreach (var subsystem in subsystems) subsystem.trackingOriginUpdated += SetFloor; SetOrigins(); }
        void Start() { SetOrigins(); }
        void OnDisable() { foreach (var subsystem in subsystems) subsystem.trackingOriginUpdated -= SetFloor; }
        void SetOrigins() { foreach (var subsystem in subsystems) SetFloor(subsystem); }
        void SetFloor(XRInputSubsystem subsystem)
        {
            if (subsystem.running && subsystem.GetTrackingOriginMode() != TrackingOriginModeFlags.Floor && (subsystem.GetSupportedTrackingOriginModes() & TrackingOriginModeFlags.Floor) != 0)
                subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
        }
    }
}
