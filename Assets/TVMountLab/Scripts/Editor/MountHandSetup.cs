using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using TVMountLab;

[InitializeOnLoad]
public static class MountHandSetup
{
    const string ScenePath = "Assets/TVMountLab/Scenes/MountPractice.unity";
    const string Request = "MountHandSetup.request";
    const string Report = "MountHandValidation.txt";
    static MountHandSetup() { EditorApplication.delayCall += InstallRequested; EditorApplication.playModeStateChanged += PlayChanged; }

    static void InstallRequested()
    {
        if (File.Exists("MountHandRetest.request") && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.Delete("MountHandRetest.request");
            RunChecks(false);
            SessionState.SetBool("MountHands.RunPlayChecks", true);
            EditorApplication.isPlaying = true;
            return;
        }
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += InstallRequested; return; }
        try
        {
            if (SceneManager.GetActiveScene().path != ScenePath) throw new Exception("Expected MountPractice to remain the active scene.");
            Setup();
            File.Delete(Request);
            RunChecks(false);
            SessionState.SetBool("MountHands.RunPlayChecks", true);
            EditorApplication.isPlaying = true;
        }
        catch (Exception error) { File.WriteAllText(Report, error.ToString()); Debug.LogException(error); }
    }

    [MenuItem("TV Mount Lab/Validate independent hand grabbing")]
    public static void Validate() { RunChecks(EditorApplication.isPlaying); }

    static void PlayChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("MountHands.RunPlayChecks", false)) return;
        SessionState.SetBool("MountHands.RunPlayChecks", false);
        try { RunChecks(true); }
        catch (Exception error) { File.AppendAllText(Report, "\nPLAY CHECK FAILED: " + error); Debug.LogException(error); }
        finally { EditorApplication.isPlaying = false; }
    }

    static GameObject Named(string name)
    {
        var matches = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t.gameObject.scene == SceneManager.GetActiveScene() && t.name == name).ToArray();
        if (matches.Length != 1) throw new Exception("Expected one scene object named " + name + "; found " + matches.Length);
        return matches[0].gameObject;
    }

    static void Setup()
    {
        var left = Named("Left Controller / Screw Hand");
        var right = Named("Right Controller / Drill Hand");
        var drill = Named("Drill");
        var screw = Named("Screw_Cross_Round");
        if (left.GetComponent<MountHandInteractor>() || right.GetComponent<MountHandInteractor>()) throw new Exception("Already configured; refusing duplicate setup.");
        string backup = "SceneBackups/MountPractice.before-independent-hands." + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity";
        Directory.CreateDirectory("SceneBackups");
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), backup, true)) throw new Exception("Could not save scene backup.");
        File.WriteAllText(Report, "Backup: " + Path.GetFullPath(backup) + "\n");
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Configure existing drill and screw with independent VR hands");
        // These were display-only children of the controller placeholders. Preserve world poses.
        Undo.SetTransformParent(drill.transform, null, "Make existing drill available for pickup");
        Undo.SetTransformParent(screw.transform, null, "Make existing screw available for pickup");
        var items = new[] { PrepareItem(drill), PrepareItem(screw) };
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = new Color(0.62f, 0.70f, 0.78f);
        const string matPath = "Assets/TVMountLab/HandVisual.mat";
        AssetDatabase.CreateAsset(material, matPath);
        ConfigureHand(left, XRNode.LeftHand, "OVRHand_L", material, items);
        ConfigureHand(right, XRNode.RightHand, "OVRHand_R", material, items);
        Undo.AddComponent<MountHeadTracking>(Named("Head / Main Camera"));
        // Match the existing floor-level controller coordinates in the OpenXR tracking origin.
        var origin = Undo.AddComponent<MountTrackingOrigin>(Named("04 / XR Training Rig"));
        EditorUtility.SetDirty(origin);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new Exception("Could not save configured scene.");
        File.AppendAllText(Report, "Configured the existing scene instances; preserved world poses and source prefabs.\n");
    }

    static MountGrabbable PrepareItem(GameObject item)
    {
        var grab = Undo.AddComponent<MountGrabbable>(item);
        var colliders = item.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            var renderers = item.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new Exception(item.name + " has no visible model.");
            var bounds = new Bounds(item.transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
            foreach (var renderer in renderers)
            {
                var b = renderer.bounds;
                for (int i = 0; i < 8; i++) bounds.Encapsulate(item.transform.InverseTransformPoint(b.center + Vector3.Scale(b.extents, new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
            }
            var box = Undo.AddComponent<BoxCollider>(item);
            box.center = bounds.center; box.size = Vector3.Max(bounds.size, Vector3.one * 0.005f);
            colliders = new Collider[] { box };
        }
        grab.grabColliders = colliders;
        // Static colliders are sufficient for pose-preserving pickup. Do not add gravity or move the props.
        EditorUtility.SetDirty(grab);
        return grab;
    }

    static void ConfigureHand(GameObject controller, XRNode node, string modelName, Material material, MountGrabbable[] items)
    {
        string path = AssetDatabase.FindAssets(modelName + " t:Model", new[] { "Packages/com.meta.xr.sdk.interaction" })
            .Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == modelName);
        if (path == null) throw new Exception("Installed SDK hand model not found: " + modelName);
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), controller.transform);
        Undo.RegisterCreatedObjectUndo(visual, "Add SDK hand mesh");
        visual.name = node == XRNode.LeftHand ? "Left Hand Visual" : "Right Hand Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        foreach (var animator in visual.GetComponentsInChildren<Animator>()) animator.enabled = false;
        var renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new Exception("SDK hand has no renderer.");
        foreach (var renderer in renderers) { renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray(); renderer.enabled = true; }
        var hand = Undo.AddComponent<MountHandInteractor>(controller);
        hand.node = node; hand.handRenderers = renderers; hand.items = items;
        EditorUtility.SetDirty(hand);
        File.AppendAllText(Report, node + " mesh: " + path + "; bounds=" + renderers[0].bounds.size + "\n");
    }

    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void RunChecks(bool play)
    {
        var left = Named("Left Controller / Screw Hand").GetComponent<MountHandInteractor>();
        var right = Named("Right Controller / Drill Hand").GetComponent<MountHandInteractor>();
        Check(left && right && left.node != right.node, "Scene hands must have independent tracked interactors.");
        Check(left.items.Length == 2 && right.items.SequenceEqual(left.items), "Both hands must allow both existing items.");
        Check(left.items.All(i => i && i.grabColliders.Length > 0), "Both props need pickup colliders.");
        Check(left.handRenderers.All(r => r.enabled) && right.handRenderers.All(r => r.enabled), "Both hand visuals must start visible.");
        // Exercise the actual interaction code on disposable objects, preserving scene prop transforms.
        var root = new GameObject("Hand validation temporary objects");
        try
        {
            MountHandInteractor MakeHand(string name)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(root.transform);
                var hand = go.AddComponent<MountHandInteractor>(); hand.handRenderers = new[] { go.GetComponent<Renderer>() }; hand.items = new MountGrabbable[0]; return hand;
            }
            MountGrabbable MakeItem(string name)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(root.transform);
                var item = go.AddComponent<MountGrabbable>(); item.grabColliders = new[] { go.GetComponent<Collider>() }; return item;
            }
            var l = MakeHand("L"); var r = MakeHand("R"); var a = MakeItem("Drill test"); var b = MakeItem("Screw test");
            Check(l.TryGrab(a), "Left pickup failed");
            Check(!l.handRenderers[0].enabled && r.handRenderers[0].enabled && l.enabled && l.gameObject.activeSelf, "Pickup must hide only grabbing renderer");
            Check(!r.TryGrab(a), "A prop must not have two owners");
            Check(r.TryGrab(b), "Independent simultaneous pickup failed");
            Check(!l.TryGrab(b), "A hand must not hold two items");
            var old = a.transform.position; l.transform.position += Vector3.right;
            a.FollowController();
            Check(Vector3.Distance(a.transform.position, old + Vector3.right) < 0.001f, "Grabbed object does not follow controller");
            l.Release();
            Check(l.handRenderers[0].enabled && !r.handRenderers[0].enabled && b.Owner == r, "Release must restore only its own hand");
            r.Release(); Check(r.handRenderers[0].enabled && !a.Owner && !b.Owner, "Release restoration failed");
            Check(l.TryGrab(b) && r.TryGrab(a), "Either item must be usable in either hand");
            if (play)
            {
                l.enabled = false;
                Check(!b.Owner && l.handRenderers[0].enabled, "Disabled interactor must release and restore visuals");
                a.gameObject.SetActive(false);
                Check(!r.HeldItem && r.handRenderers[0].enabled, "Disabled prop must release and restore visuals");
            }
            l.Release(); r.Release();
            l.items = new[] { a }; l.transform.position = Vector3.one * 20;
            Check(!l.GrabNearest(), "Distant object must not be grabbed");
            File.AppendAllText(Report, (play ? "PLAY MODE" : "EDITOR") + " PASS: independent pickup, simultaneous hold, exclusivity, either-hand swap, follow, renderer-only hiding, release restore, range" + (play ? ", disable cleanup" : "") + ".\n");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        Debug.Log("TV Mount Lab: independent hand grabbing validation passed (" + (play ? "play" : "editor") + ").");
    }
}
