using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace TVMountLab
{
    // Editor-only control override. Uses the existing hand's grab/release methods.
    [DefaultExecutionOrder(-500), DisallowMultipleComponent]
    public sealed class MountKeyboardTester : MonoBehaviour
    {
        public Camera view;
        public MountHandInteractor leftHand, rightHand;
        public Vector3 startingPosition = new Vector3(0, 1.65f, -0.8f);
        public float moveSpeed = 1.2f, lookSpeed = 85f, handSpeed = 0.65f;
        public bool Active { get; private set; }
        public MountHandInteractor SelectedHand => useLeft ? leftHand : rightHand;
        bool wanted = true, useLeft, grip;
        bool previousBackground, escapeHeld, tabHeld;
#if UNITY_EDITOR_WIN
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int key);
#endif
        static bool Key(UnityEngine.InputSystem.Controls.KeyControl control, int code)
        {
#if UNITY_EDITOR_WIN
            return (GetAsyncKeyState(code) & 0x8000) != 0 || control.isPressed;
#else
            return control.isPressed;
#endif
        }
        InputSettings previousSettings, testSettings;
        public Vector2 LastMoveInput { get; private set; }
        public Vector2 LastLookInput { get; private set; }
        float yaw, pitch, handHeight;
        bool leftEnabled, rightEnabled, headEnabled;
        MountHeadTracking head;
        Vector3 cameraPosition, leftPosition, rightPosition;
        Quaternion cameraRotation, leftRotation, rightRotation;
        GameObject hud;
        UnityEngine.UI.Text text;

        void Update()
        {
            if (!Application.isEditor) return;
            var pad = Keyboard.current;
            if (!Application.isFocused) { if (Active) { leftHand.Release(); rightHand.Release(); grip = true; } return; }
            bool escape = pad != null && Key(pad.escapeKey, 27);
            if (escape && !escapeHeld) wanted = !wanted; escapeHeld = escape;
            if (!wanted || pad == null || !pad.added)
            { if (Active) Exit(); return; }
            if (!Active) Enter(pad);
            if (!Active) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            var look = new Vector2((Key(pad.rightArrowKey, 39) ? 1 : 0) - (Key(pad.leftArrowKey, 37) ? 1 : 0), (Key(pad.upArrowKey, 38) ? 1 : 0) - (Key(pad.downArrowKey, 40) ? 1 : 0)); LastLookInput = look;
            yaw += look.x * lookSpeed * dt; pitch = Mathf.Clamp(pitch - look.y * lookSpeed * dt, -75, 75);
            var flat = Quaternion.Euler(0, yaw, 0);
            var move = Vector2.ClampMagnitude(new Vector2((Key(pad.dKey, 68) ? 1 : 0) - (Key(pad.aKey, 65) ? 1 : 0), (Key(pad.wKey, 87) ? 1 : 0) - (Key(pad.sKey, 83) ? 1 : 0)), 1); LastMoveInput = move;
            view.transform.position += flat * new Vector3(move.x, 0, move.y) * (moveSpeed * dt);
            view.transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            bool tab = Key(pad.tabKey, 9);
            bool switchHand = tab && !tabHeld; tabHeld = tab;
            if (switchHand)
            { SelectedHand.Release(); useLeft = !useLeft; grip = Key(pad.uKey, 85); }
            handHeight = Mathf.Clamp(handHeight + ((Key(pad.rKey, 82) ? 1 : 0) - (Key(pad.fKey, 70) ? 1 : 0)) * handSpeed * dt, -1.1f, 1.8f);
            PositionHands();
            bool pressed = Key(pad.uKey, 85);
            if (pressed && !grip) SelectedHand.GrabNearest();
            if (!pressed && grip) SelectedHand.Release();
            grip = pressed;
            text.text = "KEYBOARD TEST | " + (useLeft ? "LEFT" : "RIGHT") + " HAND | " +
                (SelectedHand.HeldItem ? "Holding " + SelectedHand.HeldItem.name : "Move hand close, then hold U") +
                " | Move " + move.ToString("F1") + " Look " + look.ToString("F1") + "\nWASD Move   Arrows Look   R/F Hand up/down   U Grab/release   Tab Switch hand   Esc XR controls";
        }

        void Enter(Keyboard pad)
        {
            if (!view || !leftHand || !rightHand) return;
            previousBackground = Application.runInBackground; Application.runInBackground = true;
            previousSettings = InputSystem.settings; testSettings = Instantiate(previousSettings);
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = testSettings;
            cameraPosition = view.transform.localPosition; cameraRotation = view.transform.localRotation;
            leftPosition = leftHand.transform.localPosition; leftRotation = leftHand.transform.localRotation;
            rightPosition = rightHand.transform.localPosition; rightRotation = rightHand.transform.localRotation;
            leftEnabled = leftHand.enabled; rightEnabled = rightHand.enabled;
            head = view.GetComponent<MountHeadTracking>(); headEnabled = head && head.enabled;
            leftHand.Release(); rightHand.Release();
            leftHand.enabled = false; rightHand.enabled = false;
            if (head) head.enabled = false;
            XRDevice.DisableAutoXRCameraTracking(view, true);
            view.transform.SetPositionAndRotation(startingPosition, Quaternion.identity);
            yaw = pitch = handHeight = 0; useLeft = false;
            grip = Key(pad.uKey, 85);
            Active = true; PositionHands();
            if (!hud) CreateHud(); hud.SetActive(true);
        }

        void PositionHands()
        {
            // Keep the selected hand ahead of the view; looking up/down also aims the hand.
            leftHand.transform.SetPositionAndRotation(view.transform.TransformPoint(new Vector3(-0.22f, -0.25f + (useLeft ? handHeight : 0), 0.65f)), view.transform.rotation);
            rightHand.transform.SetPositionAndRotation(view.transform.TransformPoint(new Vector3(0.22f, -0.25f + (!useLeft ? handHeight : 0), 0.65f)), view.transform.rotation);
        }

        void Exit()
        {
            if (!Active) return;
            leftHand.Release(); rightHand.Release();
            leftHand.transform.SetLocalPositionAndRotation(leftPosition, leftRotation);
            rightHand.transform.SetLocalPositionAndRotation(rightPosition, rightRotation);
            view.transform.SetLocalPositionAndRotation(cameraPosition, cameraRotation);
            XRDevice.DisableAutoXRCameraTracking(view, false);
            leftHand.enabled = leftEnabled; rightHand.enabled = rightEnabled;
            if (head) head.enabled = headEnabled;
            if (testSettings) { if (InputSystem.settings == testSettings) InputSystem.settings = previousSettings; Destroy(testSettings); testSettings = null; }
            Application.runInBackground = previousBackground;
            Active = false; grip = false; if (hud) hud.SetActive(false);
        }

        void CreateHud()
        {
            hud = new GameObject("Keyboard test controls", typeof(RectTransform), typeof(Canvas));
            var canvas = hud.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = hud.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
            var panel = new GameObject("Controls", typeof(RectTransform)); panel.transform.SetParent(hud.transform, false);
            var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1); rect.sizeDelta = new Vector2(0, 72); rect.anchoredPosition = Vector2.zero;
            var bg = panel.AddComponent<UnityEngine.UI.Image>(); bg.color = new Color(0.01f, 0.025f, 0.04f, 0.92f); bg.raycastTarget = false;
            var label = new GameObject("Text", typeof(RectTransform)); label.transform.SetParent(panel.transform, false);
            text = label.AddComponent<UnityEngine.UI.Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 19; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            var tr = text.rectTransform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(10, 5); tr.offsetMax = new Vector2(-10, -5);
        }
        void OnApplicationFocus(bool focus) { if (!focus && Active) { leftHand.Release(); rightHand.Release(); grip = true; } }
        void OnDisable() { Exit(); }
        void OnDestroy() { Exit(); if (hud) Destroy(hud); }
    }
}



