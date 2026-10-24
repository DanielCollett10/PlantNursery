using System;
using UnityEngine.InputSystem;

namespace PlantNursery.Game.Controls
{
    /// <summary>Which family of device the player used last.</summary>
    public enum InputDeviceKind
    {
        KeyboardMouse,
        Gamepad,
    }

    /// <summary>
    /// Reports the last-used device kind (keyboard+mouse vs gamepad) from performed actions of one asset,
    /// so deadzone noise doesn't flip it. The kind picks the pointer and the button prompts.
    /// </summary>
    public sealed class InputDeviceTracker : IDisposable
    {
        readonly InputActionAsset _asset;

        /// <summary>The device kind used last. Starts as keyboard+mouse.</summary>
        public InputDeviceKind Current { get; private set; } = InputDeviceKind.KeyboardMouse;

        /// <summary>Raised when <see cref="Current"/> changes.</summary>
        public event Action<InputDeviceKind> DeviceChanged;

        /// <summary>Starts listening to actions of <paramref name="asset"/>.</summary>
        public InputDeviceTracker(InputActionAsset asset)
        {
            _asset = asset ?? throw new ArgumentNullException(nameof(asset));
            InputSystem.onActionChange += OnActionChange;
        }

        /// <summary>Classifies a device: gamepads (and other non keyboard/mouse pointers) vs keyboard+mouse.</summary>
        public static InputDeviceKind Classify(InputDevice device)
        {
            return device is Gamepad ? InputDeviceKind.Gamepad : InputDeviceKind.KeyboardMouse;
        }

        /// <summary>Records that <paramref name="device"/> was just used.</summary>
        public void Report(InputDevice device)
        {
            if (device == null) return;
            var kind = Classify(device);
            if (kind == Current) return;
            Current = kind;
            DeviceChanged?.Invoke(kind);
        }

        void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed) return;
            if (!(obj is InputAction action) || action.actionMap?.asset != _asset) return;
            Report(action.activeControl?.device);
        }

        /// <summary>Stops listening.</summary>
        public void Dispose()
        {
            InputSystem.onActionChange -= OnActionChange;
        }
    }
}
