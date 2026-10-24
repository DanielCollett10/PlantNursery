using System;
using UnityEngine.InputSystem;

namespace PlantNursery.Game.Controls
{
    /// <summary>Enables exactly one action map (the current <see cref="InputMode"/>) of an asset.</summary>
    public sealed class InputModeSwitcher
    {
        readonly InputActionAsset _asset;

        /// <summary>The mode whose map is currently enabled.</summary>
        public InputMode Mode { get; private set; }

        /// <summary>Raised after the mode changes.</summary>
        public event Action<InputMode> ModeChanged;

        /// <summary>Creates a switcher and enables the Walk map.</summary>
        public InputModeSwitcher(InputActionAsset asset)
        {
            _asset = asset ?? throw new ArgumentNullException(nameof(asset));
            Apply(InputMode.Walk);
        }

        /// <summary>Disables every other map and enables the one for <paramref name="mode"/>.</summary>
        public void SetMode(InputMode mode)
        {
            if (mode == Mode && _asset.FindActionMap(mode.ToString(), true).enabled) return;
            Apply(mode);
            ModeChanged?.Invoke(mode);
        }

        /// <summary>The action map for a mode.</summary>
        public InputActionMap MapFor(InputMode mode) => _asset.FindActionMap(mode.ToString(), true);

        void Apply(InputMode mode)
        {
            foreach (var map in _asset.actionMaps)
            {
                if (map.name == mode.ToString()) map.Enable();
                else map.Disable();
            }
            Mode = mode;
        }
    }
}
