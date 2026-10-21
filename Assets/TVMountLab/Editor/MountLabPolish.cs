using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TVMountLab;

[InitializeOnLoad]
public static class MountLabPolish {
    const string Root="Assets/TVMountLab/";
    static MountLabPolish(){EditorApplication.delayCall+=Apply;}
    static void Apply(){
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}
        var trainer=UnityEngine.Object.FindFirstObjectByType<MountTraining>();if(!trainer){EditorApplication.delayCall+=Apply;return;}
        if(File.Exists(Root+"Polished.txt"))return;
        foreach(var text in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)){
            // TV text belongs to the prefab and is updated below.
            if(text.name=="Screen typography")continue;
            text.characterSize*=text.name=="Title"?4:5;
            EditorUtility.SetDirty(text);
        }
        var glass=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Midnight Glass.mat");glass.SetFloat("_Smoothness",.2f);glass.SetFloat("_Metallic",.05f);EditorUtility.SetDirty(glass);
        var graphite=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Graphite.mat");graphite.color=new Color(.065f,.08f,.095f);EditorUtility.SetDirty(graphite);
        var tv=PrefabUtility.LoadPrefabContents(Root+"Prefabs/FlatScreenTV.prefab");var caption=tv.GetComponentInChildren<TextMesh>();caption.characterSize*=4;PrefabUtility.SaveAsPrefabAsset(tv,Root+"Prefabs/FlatScreenTV.prefab");PrefabUtility.UnloadPrefabContents(tv);
        var drill=PrefabUtility.LoadPrefabContents(Root+"Prefabs/ControllerDrill.prefab");drill.transform.Find("Motor housing").GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Meshes/FacetedCylinder.asset");PrefabUtility.SaveAsPrefabAsset(drill,Root+"Prefabs/ControllerDrill.prefab");PrefabUtility.UnloadPrefabContents(drill);
        EditorSceneManager.SaveScene(trainer.gameObject.scene);AssetDatabase.SaveAssets();
        Capture(trainer.head.GetComponent<Camera>());
        File.WriteAllText(Root+"Polished.txt","Instruction typography enlarged; rougher TV glass; faceted drill housing.\n");
        Selection.activeGameObject=trainer.gameObject;
        if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,1.45f,2.2f),Quaternion.Euler(5,0,0),4f);
    }
    static void Capture(Camera camera){
        var rt=new RenderTexture(1600,900,24);var old=camera.targetTexture;var prev=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Root+"Preview.png",tex.EncodeToPNG());camera.targetTexture=old;RenderTexture.active=prev;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);
    }
}
