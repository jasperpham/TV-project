using System;
using System.Collections.Generic;
using UnityEngine;

namespace TVMountLab
{
    // Runs after the existing hand/prop LateUpdates. Never changes their scripts.
    [DefaultExecutionOrder(10000), DisallowMultipleComponent]
    public sealed class MountAssemblyController : MonoBehaviour
    {
        [Serializable]
        public sealed class Step
        {
            public MountGrabbable part;
            public Transform target;
            public string instruction;
        }
        public Step[] steps = Array.Empty<Step>();
        public UnityEngine.UI.Text wallInstructions;
        [Min(0.001f)] public float snapDistance = 0.18f;
        [Range(0, 180)] public float snapAngle = 180f;
        public bool allowEditorSimulation = true;
        public int CompletedSteps { get; private set; }
        public bool IsComplete => ready && CompletedSteps == steps.Length;
        public string Status { get; private set; }
        bool ready;
        bool[] held;
        Vector3[] positions, scales;
        Quaternion[] rotations;
        Rigidbody[] bodies;
        bool[] kinematic, gravity, grabEnabled;
        RigidbodyConstraints[] constraints;

        void Start()
        {
            if (!ValidateConfiguration(out string reason)) { Fail(reason); return; }
            int count = steps.Length;
            held = new bool[count]; positions = new Vector3[count]; scales = new Vector3[count];
            rotations = new Quaternion[count]; bodies = new Rigidbody[count];
            kinematic = new bool[count]; gravity = new bool[count];
            grabEnabled = new bool[count]; constraints = new RigidbodyConstraints[count];
            for (int i = 0; i < count; i++)
            {
                var p = steps[i].part;
                positions[i] = p.transform.position; rotations[i] = p.transform.rotation;
                scales[i] = p.transform.localScale; grabEnabled[i] = p.enabled;
                bodies[i] = p.GetComponent<Rigidbody>();
                if (bodies[i])
                {
                    kinematic[i] = bodies[i].isKinematic; gravity[i] = bodies[i].useGravity;
                    constraints[i] = bodies[i].constraints;
                }
            }
            ready = true;
            SetAvailability(); ShowStep();
        }

        public bool ValidateConfiguration(out string reason)
        {
            reason = null;
            if (steps == null || steps.Length != 8) { reason = "Exactly eight assembly steps are required."; return false; }
            if (snapDistance <= 0 || float.IsNaN(snapDistance)) { reason = "Snap distance must be positive."; return false; }
            var parts = new HashSet<MountGrabbable>(); var targets = new HashSet<Transform>();
            foreach (var s in steps)
            {
                if (s == null || !s.part || !s.target) { reason = "A step is missing its part or target."; return false; }
                if (!parts.Add(s.part) || !targets.Add(s.target)) { reason = "Parts and targets must be unique."; return false; }
                if (s.target == s.part.transform || s.target.IsChildOf(s.part.transform))
                { reason = "A target cannot move with its own part."; return false; }
                if (!s.part.gameObject.activeInHierarchy || !s.target.gameObject.activeInHierarchy)
                { reason = "All assembly parts and targets must be active."; return false; }
                if (s.part.grabColliders == null || s.part.grabColliders.Length == 0)
                { reason = s.part.name + " needs grab colliders."; return false; }
            }
            return true;
        }

        void LateUpdate() { TickAssembly(); }

        // Public for deterministic simulator checks; normal gameplay calls this in LateUpdate.
        public void TickAssembly()
        {
            if (!ready || !enabled) return;
            for (int i = 0; i < steps.Length; i++)
            {
                if (!steps[i].part || !steps[i].target) { Fail("An assembly reference was removed."); return; }
                bool nowHeld = steps[i].part.Owner != null;
                bool released = held[i] && !nowHeld;
                held[i] = nowHeld;
                if (i == CompletedSteps && released) TrySnap(i);
            }
            // Keep installed parts at their world-space target without parenting under the scaled TV.
            for (int i = 0; i < CompletedSteps; i++) ApplyPose(i);
        }

        void TrySnap(int index)
        {
            var s = steps[index];
            if (Vector3.Distance(s.part.transform.position, s.target.position) > snapDistance)
            { ShowStep("Move closer, then grip and release again."); return; }
            if (Quaternion.Angle(s.part.transform.rotation, s.target.rotation) > snapAngle)
            { ShowStep("Match the angle, then grip and release again."); return; }
            if (bodies[index])
            {
                if (!bodies[index].isKinematic)
                { bodies[index].linearVelocity = Vector3.zero; bodies[index].angularVelocity = Vector3.zero; }
                bodies[index].useGravity = false; bodies[index].isKinematic = true;
            }
            ApplyPose(index);
            CompletedSteps++;
            SetAvailability(); ShowStep();
        }

        void ApplyPose(int i)
        {
            var s = steps[i];
            s.part.transform.SetPositionAndRotation(s.target.position, s.target.rotation);
            // Target scale is deliberately ignored, including the nonuniform TV scale.
            s.part.transform.localScale = scales[i];
        }

        void SetAvailability()
        {
            for (int i = 0; i < steps.Length; i++) steps[i].part.enabled = i >= CompletedSteps;
        }

        void ShowStep(string hint = null)
        {
            Status = IsComplete ? "ASSEMBLY COMPLETE\nTV mounted. Well done!" :
                "TV MOUNT ASSEMBLY  |  " + (CompletedSteps + 1) + "/" + steps.Length + "\n" +
                steps[CompletedSteps].instruction + "\nGrip: hold to carry, release at the ring.\nLeft stick: move. Right stick: turn.";
            if (!string.IsNullOrEmpty(hint)) Status += "\n" + hint;
            if (wallInstructions) wallInstructions.text = Status;
        }

        void Fail(string reason)
        {
            Status = "Assembly setup error: " + reason;
            if (wallInstructions) wallInstructions.text = Status;
            Debug.LogError(Status, this); ready = false; enabled = false;
        }

        [ContextMenu("Reset assembly (Play Mode)")]
        public void ResetAssembly()
        {
            if (!Application.isPlaying || !ready) return;
            for (int i = 0; i < steps.Length; i++)
            {
                var p = steps[i].part;
                if (p.Owner) p.Owner.Release();
                p.transform.SetPositionAndRotation(positions[i], rotations[i]);
                p.transform.localScale = scales[i]; held[i] = false;
                if (bodies[i])
                {
                    bodies[i].isKinematic = kinematic[i]; bodies[i].useGravity = gravity[i];
                    bodies[i].constraints = constraints[i];
                    if (!kinematic[i]) { bodies[i].linearVelocity = Vector3.zero; bodies[i].angularVelocity = Vector3.zero; }
                }
            }
            CompletedSteps = 0; SetAvailability(); ShowStep();
        }

#if UNITY_EDITOR
        [ContextMenu("Simulator - complete next step (Play Mode)")]
        public void SimulateNextStep()
        {
            if (!Application.isPlaying || !ready || IsComplete || !allowEditorSimulation) return;
            var hands = FindObjectsByType<MountHandInteractor>(FindObjectsSortMode.None);
            MountHandInteractor hand = Array.Find(hands, h => h.isActiveAndEnabled && !h.HeldItem);
            if (!hand) { Debug.LogWarning("Release one hand first.", this); return; }
            var s = steps[CompletedSteps];
            if (!hand.TryGrab(s.part)) return;
            TickAssembly();
            // Place through the real grip-follow path, then exercise the real release API.
            var delta = s.target.rotation * Quaternion.Inverse(s.part.transform.rotation);
            hand.transform.rotation = delta * hand.transform.rotation;
            s.part.FollowController();
            hand.transform.position += s.target.position - s.part.transform.position;
            s.part.FollowController();
            hand.Release(); TickAssembly();
        }
#endif
    }
}


