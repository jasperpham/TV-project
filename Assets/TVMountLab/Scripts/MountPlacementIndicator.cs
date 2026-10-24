using UnityEngine;

namespace TVMountLab
{
    [DefaultExecutionOrder(11000), RequireComponent(typeof(MountAssemblyController))]
    public sealed class MountPlacementIndicator : MonoBehaviour
    {
        public Material lineMaterial;
        MountAssemblyController assembly;
        GameObject visuals;
        LineRenderer ring, guide;
        UnityEngine.UI.Text label;
        Transform board;
        readonly Vector3[] points = new Vector3[65];
        public int DisplayedStep { get; private set; } = -1;

        void Start()
        {
            assembly = GetComponent<MountAssemblyController>();
            visuals = new GameObject("Current placement indicator");
            ring = MakeLine("Placement ring", 0.012f);
            guide = MakeLine("Piece to destination", 0.004f);
            var canvasObject = new GameObject("Destination label", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(visuals.transform, false);
            board = canvasObject.transform;
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(650, 160); rect.localScale = Vector3.one * 0.0015f;
            var background = canvasObject.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.02f, 0.04f, 0.06f, 0.95f); background.raycastTarget = false;
            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(board, false);
            label = textObject.AddComponent<UnityEngine.UI.Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 40; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            var tr = label.rectTransform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(10, 6); tr.offsetMax = new Vector2(-10, -6);
            RefreshIndicator();
        }

        LineRenderer MakeLine(string title, float width)
        {
            var go = new GameObject(title); go.transform.SetParent(visuals.transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = lineMaterial;
            line.useWorldSpace = true; line.widthMultiplier = width; line.numCapVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        void LateUpdate() { RefreshIndicator(); }
        public void RefreshIndicator()
        {
            if (!visuals || !assembly) return;
            bool show = assembly.isActiveAndEnabled && !assembly.IsComplete && assembly.steps.Length > assembly.CompletedSteps;
            visuals.SetActive(show); DisplayedStep = show ? assembly.CompletedSteps : -1;
            if (!show) return;
            var s = assembly.steps[DisplayedStep];
            if (s == null || !s.part || !s.target) { visuals.SetActive(false); DisplayedStep = -1; return; }
            // The markers sit slightly in front of the wall; the actual snap target is unchanged.
            var tvTarget = assembly.steps[assembly.steps.Length - 1].target;
            var forward = tvTarget.forward; var right = tvTarget.right; var up = tvTarget.up;
            var center = s.target.position - forward * 0.10f;
            float radius = assembly.snapDistance;
            for (int i = 0; i < points.Length; i++)
            {
                float a = i * Mathf.PI * 2 / (points.Length - 1);
                points[i] = center + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius;
            }
            ring.positionCount = points.Length; ring.SetPositions(points);
            guide.positionCount = 2; guide.SetPosition(0, s.part.transform.position); guide.SetPosition(1, center);
            bool fits = Vector3.Distance(s.part.transform.position, s.target.position) <= radius &&
                Quaternion.Angle(s.part.transform.rotation, s.target.rotation) <= assembly.snapAngle;
            Color color = fits ? new Color(0.2f, 1f, 0.25f) : new Color(0f, 0.9f, 1f);
            ring.startColor = ring.endColor = color; guide.startColor = guide.endColor = color;
            label.color = color;
            string name = System.Text.RegularExpressions.Regex.Replace(s.part.name, "(?<=[a-z])(?=[A-Z])", " ");
            label.text = (DisplayedStep + 1) + "/8  " + name + "\n" + (fits ? "RELEASE TO SNAP" : "PLACE HERE");
            board.SetPositionAndRotation(center + up * (radius + 0.18f), tvTarget.rotation);
        }
        void OnDisable() { if (visuals) visuals.SetActive(false); }
        void OnDestroy() { if (visuals) Destroy(visuals); }
    }
}
