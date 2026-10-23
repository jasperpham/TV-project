using UnityEngine;

namespace TVMountLab
{
    [DisallowMultipleComponent]
    public sealed class MountGrabbable : MonoBehaviour
    {
        public MountHandInteractor Owner { get; private set; }
        public Collider[] grabColliders;
        Rigidbody body;
        Vector3 gripPosition;
        Quaternion gripRotation;
        bool wasKinematic, usedGravity;

        public float DistanceTo(Vector3 point)
        {
            float distance = float.PositiveInfinity;
            foreach (var col in grabColliders)
                if (col && col.enabled && col.gameObject.activeInHierarchy)
                    distance = Mathf.Min(distance, Vector3.Distance(point, col.ClosestPoint(point)));
            return distance;
        }

        internal bool Attach(MountHandInteractor hand)
        {
            if (Owner || !isActiveAndEnabled) return false;
            Owner = hand;
            gripPosition = hand.transform.InverseTransformPoint(transform.position);
            gripRotation = Quaternion.Inverse(hand.transform.rotation) * transform.rotation;
            body = GetComponent<Rigidbody>();
            if (body)
            {
                wasKinematic = body.isKinematic;
                usedGravity = body.useGravity;
                body.isKinematic = true;
                body.useGravity = false;
            }
            // Keep the existing model's scale and grip pose; follow the controller from here.
            return true;
        }

        public void FollowController()
        {
            if (Owner) transform.SetPositionAndRotation(Owner.transform.TransformPoint(gripPosition), Owner.transform.rotation * gripRotation);
        }

        void LateUpdate() { FollowController(); }

        internal void Detach(MountHandInteractor hand)
        {
            if (Owner != hand) return;
            if (body)
            {
                body.isKinematic = wasKinematic;
                body.useGravity = usedGravity;
            }
            Owner = null;
        }

        void OnDisable() { if (Owner) Owner.Release(); }
    }
}
