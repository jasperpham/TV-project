using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TVMountLab;

[InitializeOnLoad]
public static class MountLabChecks {
    const string Root="Assets/TVMountLab/";
    static double started;
    static bool queued;
    static int observed=-1;
    static double phaseStart;
    static MountLabChecks(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
    static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("MountLabCheck",false)){started=EditorApplication.timeSinceStartup;queued=false;observed=-1;}}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(SessionState.GetBool("MountLabConfiguring",false))return;
        if(!File.Exists(Root+"BuildComplete.txt"))return;
        if(!EditorApplication.isPlaying&&!EditorApplication.isPlayingOrWillChangePlaymode&&!File.Exists(Root+"Checks.txt")&&!SessionState.GetBool("MountLabCheck",false)){
            var t=UnityEngine.Object.FindFirstObjectByType<MountTraining>();if(!t)return;
            Capture("Preview.png",t.head.GetComponent<Camera>());
            SessionState.SetBool("MountLabCheck",true);EditorApplication.isPlaying=true;return;
        }
        if(!EditorApplication.isPlaying||!SessionState.GetBool("MountLabCheck",false))return;
        var trainer=UnityEngine.Object.FindFirstObjectByType<MountTraining>();if(!trainer||Keyboard.current==null)return;
        double elapsed=EditorApplication.timeSinceStartup-started;
        // Exercise the actual keyboard input path, including hold-to-drive timing.
        if(elapsed<1)return;
        if(trainer.CompletedScrews<4&&elapsed<90){
            if(observed!=trainer.CompletedScrews){observed=trainer.CompletedScrews;phaseStart=EditorApplication.timeSinceStartup;}
            if(EditorApplication.timeSinceStartup-phaseStart<.4)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.E));
            else InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.D));
        }else if(!queued){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.T));queued=true;}
        else if(trainer.Mounted||elapsed>92){
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            bool passed=trainer.CompletedScrews==4&&trainer.Mounted;
            File.WriteAllText(Root+"Checks.txt",(passed?"PASS":"FAIL")+"\nDesktop input exercise: "+trainer.CompletedScrews+"/4 screws secured, TV mounted="+trainer.Mounted+"\nHeadset tracking requires hardware verification.\n");
            Capture("CompletedPreview.png",trainer.head.GetComponent<Camera>());
            SessionState.SetBool("MountLabCheck",false);EditorApplication.isPlaying=false;
        }
    }
    static void Capture(string name,Camera camera){
        if(!camera)return;var rt=new RenderTexture(1600,900,24);var old=camera.targetTexture;var prev=RenderTexture.active;
        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Root+name,image.EncodeToPNG());
        camera.targetTexture=old;RenderTexture.active=prev;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
    }
}
