using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TVMountLab {
public class MountTraining : MonoBehaviour {
    public Transform head, leftHand, rightHand, drillTip, bit, television, tvDock;
    public Transform[] fasteners;
    public Renderer[] indicators;
    public TextMesh instructions;
    public AudioSource motor;
    public Material readyMaterial, doneMaterial;
    public float placementDistance = .17f, drivingDistance = .13f, driveSeconds = 1.4f;
    int current; bool inserted, mounted; float progress; bool previousLeft;
    Vector3 tvStart; Quaternion tvRotation;
    readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
    public int CompletedScrews => current;
    public bool Mounted => mounted;
    void Start() { tvStart=television.position; tvRotation=television.rotation; Refresh(); }
    static bool Button(XRNode node, InputFeatureUsage<bool> usage) { return InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(usage,out bool v)&&v; }
    static bool Pose(Transform t, XRNode node) {
        var d=InputDevices.GetDeviceAtXRNode(node);
        if(!d.TryGetFeatureValue(CommonUsages.isTracked,out bool tracked)||!tracked) return false;
        if(d.TryGetFeatureValue(CommonUsages.devicePosition,out Vector3 p))t.localPosition=p;
        if(d.TryGetFeatureValue(CommonUsages.deviceRotation,out Quaternion q))t.localRotation=q;
        return true;
    }
    void Update() {
        SubsystemManager.GetSubsystems(displays); bool xr=displays.Exists(d=>d.running);
        if(xr) Pose(head,XRNode.Head);
        bool leftTracked=Pose(leftHand,XRNode.LeftHand); Pose(rightHand,XRNode.RightHand);
        bool place=Button(XRNode.LeftHand,CommonUsages.triggerButton);
        bool drive=Button(XRNode.RightHand,CommonUsages.triggerButton);
        bool snap=Button(XRNode.LeftHand,CommonUsages.gripButton);
        bool desktopPlace=false,desktopDrive=false,desktopSnap=false,reset=false;
#if ENABLE_INPUT_SYSTEM
        if(Keyboard.current!=null) { desktopPlace=Keyboard.current.eKey.wasPressedThisFrame; desktopDrive=Keyboard.current.dKey.isPressed; desktopSnap=Keyboard.current.tKey.wasPressedThisFrame; reset=Keyboard.current.rKey.wasPressedThisFrame; }
#endif
        if(reset)ResetExercise();
        if(current<fasteners.Length) {
            var screw=fasteners[current];
            if(!inserted && (desktopPlace || (place&&!previousLeft&&Vector3.Distance(leftHand.position,screw.position)<placementDistance))) {
                inserted=true; screw.gameObject.SetActive(true); Refresh();
            }
            bool aligned=Vector3.Distance(drillTip.position,screw.position)<drivingDistance && Vector3.Dot(drillTip.forward,screw.forward)>.75f;
            bool running=inserted&&(desktopDrive||(drive&&aligned));
            if(running) {
                bit.Rotate(0,0,1600*Time.deltaTime,Space.Self);
                progress=Mathf.Min(1,progress+Time.deltaTime/driveSeconds);
                screw.localPosition=new Vector3(screw.localPosition.x,screw.localPosition.y,Mathf.Lerp(-.105f,-.045f,progress));
                screw.Rotate(0,0,600*Time.deltaTime,Space.Self);
                if(progress>=1) { indicators[current].sharedMaterial=doneMaterial;current++;inserted=false;progress=0;Refresh(); }
            }
            if(motor) { if(running&&!motor.isPlaying)motor.Play(); if(!running&&motor.isPlaying)motor.Stop(); }
        } else {
            if(motor&&motor.isPlaying)motor.Stop();
            if(!mounted&&(desktopSnap||(snap&&leftTracked&&Vector3.Distance(leftHand.position,television.position)<.75f))) {
                television.SetPositionAndRotation(tvDock.position,tvDock.rotation);mounted=true;Refresh();
            }
        }
        previousLeft=place;
    }
    void Refresh() {
        if(!instructions)return;
        instructions.text=mounted?"MOUNT COMPLETE\n4 / 4 fasteners secured\nR  Reset practice":current==4?"04 / HANG THE SCREEN\nApproach TV + left grip to mount\nDesktop: T  Hang TV   |   R  Reset":inserted?$"03 / DRIVE FASTENER {current+1}\nAlign bit + hold right trigger\nDesktop: hold D   |   R  Reset":$"02 / PLACE FASTENER {current+1} OF 4\nMove left hand to halo + trigger\nDesktop: E  Place   |   R  Reset";
    }
    public void ResetExercise() {
        current=0; inserted=false;mounted=false;progress=0;
        for(int i=0;i<fasteners.Length;i++){fasteners[i].gameObject.SetActive(false);var p=fasteners[i].localPosition;p.z=-.105f;fasteners[i].localPosition=p;indicators[i].sharedMaterial=readyMaterial;}
        television.SetPositionAndRotation(tvStart,tvRotation);Refresh();
    }
}
}
