using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using TVMountLab;

[InitializeOnLoad]
public static class MountLabXRConfigure {
    const string Root="Assets/TVMountLab/";
    static MountLabXRConfigure(){EditorApplication.delayCall+=Configure;}
    static void Configure(){
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling){EditorApplication.delayCall+=Configure;return;}
        if(!File.Exists(Root+"BuildComplete.txt")){EditorApplication.delayCall+=Configure;return;}
        SessionState.SetBool("MountLabConfiguring",true);
        if(!File.Exists(Root+"XRConfigured.txt")||!File.ReadAllText(Root+"XRConfigured.txt").Contains("assigned=True")){
            Directory.CreateDirectory(Root+"Backups");
            if(!File.Exists(Root+"Backups/ProjectSettings.original.txt"))File.Copy("ProjectSettings/ProjectSettings.asset",Root+"Backups/ProjectSettings.original.txt");
            XRGeneralSettingsPerBuildTarget all;
            if(!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,out all)||!all){all=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(all,Root+"XRGeneralSettings.asset");EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,all,true);}
            if(!all.HasSettingsForBuildTarget(BuildTargetGroup.Standalone))all.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if(!all.HasManagerSettingsForBuildTarget(BuildTargetGroup.Standalone))all.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Standalone);
            var general=all.SettingsForBuildTarget(BuildTargetGroup.Standalone);general.InitManagerOnStart=true;
            bool ok=XRPackageMetadataStore.AssignLoader(general.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",BuildTargetGroup.Standalone);
            OpenXRFeatureSetManager.InitializeFeatureSets();var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            int profiles=0;if(settings){foreach(var f in settings.GetFeatures()){string n=f.GetType().Name;if(n=="OculusTouchControllerProfile"||n=="ValveIndexControllerProfile"||n=="HTCViveControllerProfile"||n=="MetaQuestTouchPlusControllerProfile"){f.enabled=true;EditorUtility.SetDirty(f);profiles++;}}EditorUtility.SetDirty(settings);}
            EditorUtility.SetDirty(all);EditorUtility.SetDirty(general);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
            AssetDatabase.SaveAssets();File.WriteAllText(Root+"XRConfigured.txt","OpenXR Standalone loader assigned="+ok+"\nController interaction profiles enabled="+profiles+"\nWindows graphics: Direct3D11 (recovered from DX12 device failure).\n");
        }
        if(!UnityEngine.Object.FindFirstObjectByType<MountTraining>()){
            var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(active.isDirty)EditorSceneManager.SaveScene(active,Root+"Scenes/RecoverySnapshot.unity",true);
            EditorSceneManager.OpenScene(Root+"Scenes/MountPractice.unity");
        }
        var trainer=UnityEngine.Object.FindFirstObjectByType<MountTraining>();
        if(trainer&&!trainer.head.parent.GetComponent<MountRigTracking>()){
            var rig=trainer.head.parent;var tracking=rig.gameObject.AddComponent<MountRigTracking>();tracking.head=trainer.head;tracking.leftController=trainer.leftHand;tracking.rightController=trainer.rightHand;
            PrefabUtility.SaveAsPrefabAsset(rig.gameObject,Root+"Prefabs/ControllerTrackedRig.prefab");
            EditorSceneManager.SaveScene(trainer.gameObject.scene);
        }
        SessionState.SetBool("MountLabConfiguring",false);
    }
}
