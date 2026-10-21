using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace TVMountLab {
// Self-contained tracking makes the rig prefab reusable outside the practice scene.
public class MountRigTracking : MonoBehaviour {
    public Transform head,leftController,rightController;
    readonly List<XRInputSubsystem> inputs=new List<XRInputSubsystem>();
    readonly List<XRDisplaySubsystem> displays=new List<XRDisplaySubsystem>();
    bool floorSet;
    static void Track(Transform target,XRNode node){if(!target)return;var device=InputDevices.GetDeviceAtXRNode(node);if(device.TryGetFeatureValue(CommonUsages.isTracked,out bool tracked)&&tracked){if(device.TryGetFeatureValue(CommonUsages.devicePosition,out Vector3 p))target.localPosition=p;if(device.TryGetFeatureValue(CommonUsages.deviceRotation,out Quaternion q))target.localRotation=q;}}
    void LateUpdate(){
        SubsystemManager.GetSubsystems(inputs);if(!floorSet)foreach(var input in inputs)if(input.running)floorSet|=input.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
        SubsystemManager.GetSubsystems(displays);if(displays.Exists(d=>d.running))Track(head,XRNode.Head);
        Track(leftController,XRNode.LeftHand);Track(rightController,XRNode.RightHand);
    }
}
}
