using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TVMountLab;

public static class MountAssemblySetup
{
    static Transform[] SceneTransforms() => UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .Where(t => t.gameObject.scene == SceneManager.GetActiveScene()).ToArray();
    static Transform Unique(string name) => SceneTransforms().Single(t => t.name == name);
    static Transform Child(Transform parent, string name) => parent.Cast<Transform>().Single(t => t.name == name);

    [MenuItem("TV Mount Lab/Configure assembly")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode before setup.");
        if (SceneManager.GetActiveScene().name != "MountPractice") throw new Exception("Open MountPractice first.");
        if (UnityEngine.Object.FindFirstObjectByType<MountAssemblyController>()) throw new Exception("Assembly is already configured.");
        var all = SceneTransforms();
        var hardware = all.Single(t => t.name == "MountingHardware" || t.name == "MountingHarware");
        var targets = Unique("AssemblyTargets"); var tv = Unique("TV");
        string[] partNames = { "TVPlankTopLeft", "TVPlankTopRight", "TVPlankBottomLeft", "TVPlankBottomRight", "WallPlankTop", "WallPlankBottom", "CenterPlate" };
        string[] targetNames = { "TPTL", "TPTR", "TPBL", "TPBR", "WPT", "WPB", "CenterPlate" };
        string[] instructions = { "Fit the top-left TV bracket.", "Fit the top-right TV bracket.", "Fit the bottom-left TV bracket.", "Fit the bottom-right TV bracket.", "Fit the upper wall rail.", "Fit the lower wall rail.", "Fit the center plate.", "Lift the TV into its final position and release." };
        var parts = partNames.Select(n => Child(hardware, n)).Append(tv).ToArray();
        var anchors = targetNames.Select(n => Child(targets, n)).ToList();
        var hands = UnityEngine.Object.FindObjectsByType<MountHandInteractor>(FindObjectsSortMode.None);
        if (hands.Length != 2) throw new Exception("Expected the existing two hand interactors.");
        // Validate everything before touching the scene. Existing target positions remain authoritative.
        foreach (var p in parts) if (p.GetComponentsInChildren<Collider>().Length == 0) throw new Exception(p.name + " has no collider.");
        System.IO.Directory.CreateDirectory("SceneBackups");
        string backup = "SceneBackups/MountPractice.before-assembly." + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity";
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), backup, true)) throw new Exception("Backup failed.");
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Configure TV assembly");
        try
        {
            var finalCandidates = targets.Cast<Transform>().Where(t => !targetNames.Contains(t.name)).ToArray();
            Transform final;
            if (finalCandidates.Length == 0)
            {
                var go = new GameObject("TVFinal"); Undo.RegisterCreatedObjectUndo(go, "Add missing final anchor");
                final = go.transform; final.SetParent(targets, false); final.SetPositionAndRotation(tv.position, tv.rotation);
            }
            else if (finalCandidates.Length == 1) final = finalCandidates[0];
            else throw new Exception("More than one unknown final target. Setup stopped.");
            anchors.Add(final);
            var controller = Undo.AddComponent<MountAssemblyController>(targets.gameObject);
            controller.steps = new MountAssemblyController.Step[8];
            for (int i = 0; i < 8; i++)
            {
                var grab = parts[i].GetComponent<MountGrabbable>();
                if (!grab) grab = Undo.AddComponent<MountGrabbable>(parts[i].gameObject);
                Undo.RecordObject(grab, "Configure assembly pickup");
                grab.grabColliders = parts[i].GetComponentsInChildren<Collider>(); grab.enabled = true;
                EditorUtility.SetDirty(grab);
                controller.steps[i] = new MountAssemblyController.Step { part = grab, target = anchors[i], instruction = instructions[i] };
            }
            foreach (var hand in hands)
            {
                Undo.RecordObject(hand, "Register assembly items");
                hand.items = (hand.items ?? Array.Empty<MountGrabbable>()).Concat(controller.steps.Select(s => s.part)).Where(p => p).Distinct().ToArray();
                EditorUtility.SetDirty(hand);
            }
            var board = new GameObject("Assembly Wall Instructions", typeof(RectTransform), typeof(Canvas));
            Undo.RegisterCreatedObjectUndo(board, "Add wall instructions");
            var canvas = board.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var rect = board.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(900, 360); rect.localScale = Vector3.one * 0.0015f;
            // Use final TV pose and renderer bounds; no raw local offsets under the scaled TV.
            var bounds = tv.GetComponent<Renderer>().bounds;
            rect.SetPositionAndRotation(final.position + tv.right * (bounds.extents.x + 0.85f) - tv.forward * 0.08f, final.rotation);
            var bg = board.AddComponent<UnityEngine.UI.Image>(); bg.color = new Color(0.025f, 0.055f, 0.07f, 0.97f); bg.raycastTarget = false;
            var label = new GameObject("Instructions", typeof(RectTransform)); label.transform.SetParent(board.transform, false);
            var text = label.AddComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 42; text.color = Color.white; text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
            text.text = "TV MOUNT ASSEMBLY\nFollow the steps. Grip, move, release.\nPress Play to begin.";
            var tr = text.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(30, 20); tr.offsetMax = new Vector2(-30, -20);
            controller.wallInstructions = text;
            if (!controller.ValidateConfiguration(out string reason)) throw new Exception(reason);
            EditorUtility.SetDirty(controller); EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new Exception("Scene save failed.");
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = targets.gameObject;
            Debug.Log("Assembly configured: eight ordered steps. Backup: " + backup);
        }
        catch { Undo.RevertAllDownToGroup(group); throw; }
    }

    [MenuItem("TV Mount Lab/Simulator/Next assembly step")]
    public static void Next() { UnityEngine.Object.FindFirstObjectByType<MountAssemblyController>()?.SimulateNextStep(); }
    [MenuItem("TV Mount Lab/Simulator/Reset assembly")]
    public static void Reset() { UnityEngine.Object.FindFirstObjectByType<MountAssemblyController>()?.ResetAssembly(); }

    public static string RunChecks()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Tests require Play Mode.");
        var c = UnityEngine.Object.FindFirstObjectByType<MountAssemblyController>();
        var hand = UnityEngine.Object.FindObjectsByType<MountHandInteractor>(FindObjectsSortMode.None).First(h => !h.HeldItem);
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        Vector3 hp = hand.transform.position; Quaternion hr = hand.transform.rotation;
        var scales = c.steps.Select(s => s.part.transform.localScale).ToArray();
        try
        {
            c.ResetAssembly(); c.TickAssembly(); Check(c.CompletedSteps == 0, "Idle parts auto-snapped.");
            Check(hand.TryGrab(c.steps[1].part), "Loose piece cannot be grabbed."); hand.Release();
            var s = c.steps[0]; Check(hand.TryGrab(s.part), "Current step cannot be grabbed.");
            c.TickAssembly(); Check(c.CompletedSteps == 0, "Held part snapped.");
            hand.Release(); s.part.transform.position = s.target.position + Vector3.up * 2;
            c.TickAssembly(); Check(c.CompletedSteps == 0, "Distant release snapped.");
            s.part.transform.SetPositionAndRotation(s.target.position, s.target.rotation); c.TickAssembly();
            Check(c.CompletedSteps == 0, "Idle proximity caused a snap without a new release.");
            c.snapAngle = 15;
            Check(hand.TryGrab(s.part), "Retry failed."); c.TickAssembly(); hand.Release();
            s.part.transform.SetPositionAndRotation(s.target.position, s.target.rotation * Quaternion.Euler(90, 0, 0));
            c.TickAssembly(); Check(c.CompletedSteps == 0, "Rotation tolerance ignored."); c.snapAngle = 180;
            for (int i = 0; i < 8; i++)
            {
                c.SimulateNextStep(); Check(c.CompletedSteps == i + 1, "Step failed: " + i);
                Check(!c.steps[i].part.enabled && !c.steps[i].part.Owner, "Snapped part still grabbable or owned.");
                Check(Vector3.Distance(c.steps[i].part.transform.position, c.steps[i].target.position) < 0.0001f, "Position mismatch.");
                Check(Quaternion.Angle(c.steps[i].part.transform.rotation, c.steps[i].target.rotation) < 0.01f, "Rotation mismatch.");
                Check(c.steps[i].part.transform.localScale == scales[i], "Part scale changed.");
            }
            Check(c.IsComplete && c.wallInstructions.text.Contains("COMPLETE"), "Completion text missing.");
            c.ResetAssembly(); Check(c.CompletedSteps == 0 && c.steps[0].part.enabled && c.steps[7].part.enabled, "Reset failed.");
            return "PASS: release-only snapping, held/idle rejection, distance, rotation, order, all eight real grab/release transitions, pose, unchanged scales, locked parts, completion text, reset.";
        }
        finally { hand.Release(); hand.transform.SetPositionAndRotation(hp, hr); c.snapAngle = 180; c.ResetAssembly(); }
    }
}


