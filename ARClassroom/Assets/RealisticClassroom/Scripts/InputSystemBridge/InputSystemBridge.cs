#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace RealisticClassroom.InputSystemBridge
{
    /// <summary>Input System package implementation of <see cref="ClassroomInputProvider"/> (compiled only when the package is the active input handler).</summary>
    sealed class InputSystemProvider : ClassroomInputProvider
    {
        public override bool NextViewPressed()
        {
            var k = Keyboard.current;
            return k != null && (k.vKey.wasPressedThisFrame || k.tabKey.wasPressedThisFrame);
        }

        public override int ViewNumberPressed()
        {
            var k = Keyboard.current;
            if (k == null) return -1;
            if (k.digit1Key.wasPressedThisFrame) return 0;
            if (k.digit2Key.wasPressedThisFrame) return 1;
            if (k.digit3Key.wasPressedThisFrame) return 2;
            if (k.digit4Key.wasPressedThisFrame) return 3;
            if (k.digit5Key.wasPressedThisFrame) return 4;
            if (k.digit6Key.wasPressedThisFrame) return 5;
            if (k.digit7Key.wasPressedThisFrame) return 6;
            if (k.digit8Key.wasPressedThisFrame) return 7;
            if (k.digit9Key.wasPressedThisFrame) return 8;
            return -1;
        }

        public override bool PrimaryPressed() { var m = Mouse.current; return m != null && m.leftButton.wasPressedThisFrame; }
        public override bool PrimaryReleased() { var m = Mouse.current; return m != null && m.leftButton.wasReleasedThisFrame; }
        public override bool PrimaryHeld() { var m = Mouse.current; return m != null && m.leftButton.isPressed; }
        public override bool SecondaryPressed() { var m = Mouse.current; return m != null && m.rightButton.wasPressedThisFrame; }
        public override bool SecondaryHeld() { var m = Mouse.current; return m != null && m.rightButton.isPressed; }
        public override Vector2 PointerPosition() { var m = Mouse.current; return m != null ? m.position.ReadValue() : Vector2.zero; }
        public override float ScrollDelta() { var m = Mouse.current; return m != null ? Mathf.Clamp(m.scroll.ReadValue().y / 120f, -3f, 3f) : 0f; }
    }

    static class Registration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            ClassroomInput.Provider = new InputSystemProvider();
            ClassroomInput.ModuleFactory = go =>
            {
                var module = go.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
            };
        }
    }
}
#endif
