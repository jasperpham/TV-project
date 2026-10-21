using System;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

[InitializeOnLoad]
public static class MountLabXRSetup {
    static AddRequest request;
    const string Root="Assets/TVMountLab/";
    static MountLabXRSetup(){EditorApplication.delayCall+=Begin;}
    static void Begin(){
        if(!File.Exists(Root+"BuildComplete.txt")){EditorApplication.delayCall+=Begin;return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Begin;return;}
        if(File.Exists(Root+"XRPackageRequest.txt"))return;
        if(Type.GetType("UnityEngine.XR.OpenXR.OpenXRLoader, Unity.XR.OpenXR")!=null)return;
        File.WriteAllText(Root+"XRPackageRequest.txt","Requested compatible OpenXR package via Unity Package Manager.\n");
        request=Client.Add("com.unity.xr.openxr");EditorApplication.update+=Poll;
    }
    static void Poll(){if(request==null||!request.IsCompleted)return;EditorApplication.update-=Poll;File.AppendAllText(Root+"XRPackageRequest.txt",request.Status==StatusCode.Success?"Installed "+request.Result.version:"Failed: "+request.Error.message);}
}
