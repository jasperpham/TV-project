using UnityEngine;
using UnityEngine.XR;

namespace TVMountLab
{
    [DefaultExecutionOrder(100), RequireComponent(typeof(CharacterController))]
    public sealed class MountVRLocomotion : MonoBehaviour
    {
        public Transform head;
        [Min(0)] public float moveSpeed = 1.2f;
        [Range(15, 90)] public float turnDegrees = 30;
        CharacterController body;
        bool turnArmed = true;
        float fallSpeed;
        void Start()
        {
            body = GetComponent<CharacterController>();
            // Held models must not push or block the player's movement capsule.
            foreach (var p in FindObjectsByType<MountGrabbable>(FindObjectsSortMode.None))
                foreach (var col in p.grabColliders ?? System.Array.Empty<Collider>())
                    if (col) Physics.IgnoreCollision(body, col);
        }
        void Update()
        {
            if (!head || !body) return;
            var headset = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (!headset.isValid || !headset.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) || !tracked)
            { fallSpeed = 0; return; }
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            float height = Mathf.Clamp(head.localPosition.y, 0.8f, 2.3f);
            body.height = height;
            body.center = new Vector3(head.localPosition.x, height * 0.5f + 0.02f, head.localPosition.z);
            Vector2 move = Vector2.zero, turn = Vector2.zero;
            if (left.isValid) left.TryGetFeatureValue(CommonUsages.primary2DAxis, out move);
            if (right.isValid) right.TryGetFeatureValue(CommonUsages.primary2DAxis, out turn);
            if (Mathf.Abs(turn.x) < 0.25f) turnArmed = true;
            if (turnArmed && Mathf.Abs(turn.x) > 0.7f)
            {
                body.enabled = false;
                transform.RotateAround(head.position, Vector3.up, Mathf.Sign(turn.x) * turnDegrees);
                body.enabled = true; turnArmed = false;
            }
            if (move.magnitude < 0.2f) move = Vector2.zero;
            move = Vector2.ClampMagnitude(move, 1);
            var direction = Quaternion.Euler(0, head.eulerAngles.y, 0) * new Vector3(move.x, 0, move.y);
            fallSpeed = body.isGrounded ? -1f : Mathf.Max(-10, fallSpeed + Physics.gravity.y * Time.deltaTime);
            body.Move((direction * moveSpeed + Vector3.up * fallSpeed) * Time.deltaTime);
        }
    }
}
