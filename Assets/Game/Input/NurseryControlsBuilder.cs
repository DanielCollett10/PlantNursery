using UnityEngine.InputSystem;

namespace PlantNursery.Game.Controls
{
    /// <summary>The input modes; each one has an action map of the same name.</summary>
    public enum InputMode
    {
        Walk,
        Carry,
        BenchView,
        Build,
        UI,
    }

    /// <summary>
    /// Builds the NurseryControls action asset in code (maps Walk, Carry, BenchView, Build, UI),
    /// with keyboard+mouse and gamepad bindings for every action. The Editor writes the result to
    /// Assets/Input/NurseryControls.inputactions; tests use it directly. See docs/decisions/0012.
    /// </summary>
    public static class NurseryControlsBuilder
    {
        /// <summary>Binding group / control scheme for keyboard and mouse.</summary>
        public const string KeyboardMouse = "Keyboard&Mouse";

        /// <summary>Binding group / control scheme for gamepad.</summary>
        public const string Gamepad = "Gamepad";

        /// <summary>Creates a fresh asset with all five maps and both control schemes.</summary>
        public static InputActionAsset Build()
        {
            var asset = UnityEngine.ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "NurseryControls";
            asset.AddControlScheme(KeyboardMouse)
                .WithRequiredDevice("<Keyboard>").WithRequiredDevice("<Mouse>");
            asset.AddControlScheme(Gamepad).WithRequiredDevice("<Gamepad>");

            var walk = asset.AddActionMap("Walk");
            Move(walk);
            Button(walk, "Interact", "<Keyboard>/e", "<Gamepad>/buttonSouth");
            Button(walk, "Zoom", "<Keyboard>/z", "<Gamepad>/rightStickPress");
            Button(walk, "BuildToggle", "<Keyboard>/b", "<Gamepad>/buttonNorth");
            Button(walk, "Menu", "<Keyboard>/escape", "<Gamepad>/start");

            var carry = asset.AddActionMap("Carry");
            Move(carry);
            Button(carry, "Interact", "<Keyboard>/e", "<Gamepad>/buttonSouth");
            Button(carry, "Drop", "<Keyboard>/q", "<Gamepad>/buttonEast");
            Button(carry, "Zoom", "<Keyboard>/z", "<Gamepad>/rightStickPress");
            Button(carry, "Menu", "<Keyboard>/escape", "<Gamepad>/start");

            var bench = asset.AddActionMap("BenchView");
            Move(bench);
            Point(bench);
            Step(bench, "Step");
            Button(bench, "StepPrev", "<Keyboard>/leftBracket", "<Gamepad>/leftShoulder");
            Button(bench, "StepNext", "<Keyboard>/rightBracket", "<Gamepad>/rightShoulder");
            Button(bench, "Interact", "<Mouse>/leftButton", "<Gamepad>/buttonSouth");
            Button(bench, "Water", "<Mouse>/leftButton", "<Gamepad>/buttonWest");
            Button(bench, "Cancel", "<Keyboard>/escape", "<Gamepad>/buttonEast");

            var build = asset.AddActionMap("Build");
            Point(build);
            Step(build, "MoveGhost");
            Button(build, "Place", "<Mouse>/leftButton", "<Gamepad>/buttonSouth");
            Button(build, "Rotate", "<Keyboard>/r", "<Gamepad>/rightShoulder");
            Button(build, "PickUp", "<Keyboard>/e", "<Gamepad>/buttonWest");
            Button(build, "Cancel", "<Keyboard>/escape", "<Gamepad>/buttonEast");
            Button(build, "BuildToggle", "<Keyboard>/b", "<Gamepad>/buttonNorth");

            var ui = asset.AddActionMap("UI");
            Step(ui, "Navigate").AddBinding("<Gamepad>/leftStick", groups: Gamepad);
            Point(ui);
            Button(ui, "Submit", "<Keyboard>/enter", "<Gamepad>/buttonSouth");
            ui.AddAction("Click", InputActionType.Button).AddBinding("<Mouse>/leftButton", groups: KeyboardMouse);
            Button(ui, "Cancel", "<Keyboard>/escape", "<Gamepad>/buttonEast");

            return asset;
        }

        static void Move(InputActionMap map)
        {
            var a = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            a.AddCompositeBinding("2DVector(mode=2)")
                .With("Up", "<Keyboard>/w", KeyboardMouse)
                .With("Down", "<Keyboard>/s", KeyboardMouse)
                .With("Left", "<Keyboard>/a", KeyboardMouse)
                .With("Right", "<Keyboard>/d", KeyboardMouse);
            a.AddBinding("<Gamepad>/leftStick", groups: Gamepad);
        }

        static void Point(InputActionMap map)
        {
            var a = map.AddAction("Point", InputActionType.Value, expectedControlLayout: "Vector2");
            a.AddBinding("<Mouse>/position", groups: KeyboardMouse);
        }

        // Arrow keys + d-pad: the slot-stepping / ghost-stepping pointer.
        static InputAction Step(InputActionMap map, string name)
        {
            var a = map.AddAction(name, InputActionType.Value, expectedControlLayout: "Vector2");
            a.AddCompositeBinding("2DVector(mode=2)")
                .With("Up", "<Keyboard>/upArrow", KeyboardMouse)
                .With("Down", "<Keyboard>/downArrow", KeyboardMouse)
                .With("Left", "<Keyboard>/leftArrow", KeyboardMouse)
                .With("Right", "<Keyboard>/rightArrow", KeyboardMouse);
            a.AddCompositeBinding("2DVector(mode=2)")
                .With("Up", "<Gamepad>/dpad/up", Gamepad)
                .With("Down", "<Gamepad>/dpad/down", Gamepad)
                .With("Left", "<Gamepad>/dpad/left", Gamepad)
                .With("Right", "<Gamepad>/dpad/right", Gamepad);
            return a;
        }

        static void Button(InputActionMap map, string name, string keyboardMousePath, string gamepadPath)
        {
            var a = map.AddAction(name, InputActionType.Button);
            a.AddBinding(keyboardMousePath, groups: KeyboardMouse);
            a.AddBinding(gamepadPath, groups: Gamepad);
        }
    }
}
