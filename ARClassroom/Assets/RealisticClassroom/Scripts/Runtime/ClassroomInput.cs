using UnityEngine;
using UnityEngine.EventSystems;

namespace RealisticClassroom
{
    /// <summary>
    /// Small input facade so the demo works with the legacy Input Manager, the Input System package, or both.
    /// The Input System implementation lives in the optional RealisticClassroom.InputSystemBridge assembly and registers itself at startup.
    /// </summary>
    public abstract class ClassroomInputProvider
    {
        public abstract bool NextViewPressed();
        /// <summary>Index (0-8) of a pressed number key, or -1.</summary>
        public abstract int ViewNumberPressed();
        public abstract bool PrimaryPressed();
        public abstract bool PrimaryReleased();
        public abstract bool PrimaryHeld();
        public abstract bool SecondaryPressed();
        public abstract bool SecondaryHeld();
        public abstract Vector2 PointerPosition();
        public abstract float ScrollDelta();
    }

    public static class ClassroomInput
    {
        static ClassroomInputProvider provider;

        /// <summary>Creates the UI input module on the EventSystem object. The Input System bridge replaces this.</summary>
        public static System.Action<GameObject> ModuleFactory;

        public static ClassroomInputProvider Provider
        {
            get
            {
                if (provider == null) provider = CreateDefault();
                return provider;
            }
            set { provider = value; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            provider = null;
            ModuleFactory = null;
        }

        static ClassroomInputProvider CreateDefault()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return new LegacyProvider();
#else
            return new NullProvider();
#endif
        }

        public static void AddUiInputModule(GameObject eventSystemObject)
        {
            if (ModuleFactory != null) { ModuleFactory(eventSystemObject); return; }
#if ENABLE_LEGACY_INPUT_MANAGER
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

        public static bool NextViewPressed() { return Provider.NextViewPressed(); }
        public static int ViewNumberPressed() { return Provider.ViewNumberPressed(); }
        public static bool PrimaryPressed() { return Provider.PrimaryPressed(); }
        public static bool PrimaryReleased() { return Provider.PrimaryReleased(); }
        public static bool PrimaryHeld() { return Provider.PrimaryHeld(); }
        public static bool SecondaryPressed() { return Provider.SecondaryPressed(); }
        public static bool SecondaryHeld() { return Provider.SecondaryHeld(); }
        public static Vector2 PointerPosition() { return Provider.PointerPosition(); }
        public static float ScrollDelta() { return Provider.ScrollDelta(); }

        sealed class NullProvider : ClassroomInputProvider
        {
            public override bool NextViewPressed() { return false; }
            public override int ViewNumberPressed() { return -1; }
            public override bool PrimaryPressed() { return false; }
            public override bool PrimaryReleased() { return false; }
            public override bool PrimaryHeld() { return false; }
            public override bool SecondaryPressed() { return false; }
            public override bool SecondaryHeld() { return false; }
            public override Vector2 PointerPosition() { return Vector2.zero; }
            public override float ScrollDelta() { return 0f; }
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        sealed class LegacyProvider : ClassroomInputProvider
        {
            public override bool NextViewPressed() { return Input.GetKeyDown(KeyCode.V) || Input.GetKeyDown(KeyCode.Tab); }
            public override int ViewNumberPressed()
            {
                for (int i = 0; i < 9; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) return i;
                return -1;
            }
            public override bool PrimaryPressed() { return Input.GetMouseButtonDown(0); }
            public override bool PrimaryReleased() { return Input.GetMouseButtonUp(0); }
            public override bool PrimaryHeld() { return Input.GetMouseButton(0); }
            public override bool SecondaryPressed() { return Input.GetMouseButtonDown(1); }
            public override bool SecondaryHeld() { return Input.GetMouseButton(1); }
            public override Vector2 PointerPosition() { return Input.mousePosition; }
            public override float ScrollDelta() { return Input.mouseScrollDelta.y; }
        }
#endif
    }
}
