using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Finds the player's movement controllers (top-down, plus the debug first-person one) so UI screens can
    /// take input away and give it back. Holds no rules.
    /// </summary>
    public static class PlayerControllers
    {
        /// <summary>True when the top-down or first-person controller is enabled.</summary>
        public static bool Active
        {
            get
            {
                var td = Object.FindAnyObjectByType<TopDownPlayerController>();
                var fp = Object.FindAnyObjectByType<FirstPersonController>();
                return (td != null && td.enabled) || (fp != null && fp.enabled);
            }
        }

        /// <summary>Turn off every controller, or turn back on the one the debug toggle selected.</summary>
        public static void SetActive(bool on)
        {
            var td = Object.FindAnyObjectByType<TopDownPlayerController>();
            var fp = Object.FindAnyObjectByType<FirstPersonController>();
            var toggle = Object.FindAnyObjectByType<DebugFirstPersonToggle>();
            var firstPerson = toggle != null && toggle.IsFirstPerson;
            if (td != null) td.enabled = on && !firstPerson;
            if (fp != null) fp.enabled = on && firstPerson;
        }
    }
}
