using UnityEngine;
using UnityEngine.UIElements;

namespace PlantNursery.Game
{
    /// <summary>
    /// Minimal UI Toolkit HUD: crosshair, interaction prompt and a brief rejection message.
    /// Built in code with a runtime PanelSettings so no extra assets are needed.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class InteractionHud : MonoBehaviour
    {
        [SerializeField] private InteractionController interaction;

        private Label _prompt, _rejection, _held;

        /// <summary>Assign the controller (used by the scene generator).</summary>
        public void Configure(InteractionController controller) { interaction = controller; }

        private void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            if (doc.panelSettings == null)
            {
                var ps = ScriptableObject.CreateInstance<PanelSettings>();
                ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                ps.referenceResolution = new Vector2Int(1920, 1080);
                doc.panelSettings = ps;
            }
            var root = doc.rootVisualElement;
            root.style.flexGrow = 1;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;
            root.pickingMode = PickingMode.Ignore;

            root.Add(MakeLabel("+", 28, Color.white));
            _prompt = MakeLabel(string.Empty, 26, Color.white);
            _rejection = MakeLabel(string.Empty, 24, new Color(1f, 0.4f, 0.4f));
            root.Add(_prompt);
            root.Add(_rejection);
            _held = MakeLabel(string.Empty, 22, Color.white);
            _held.style.position = Position.Absolute;
            _held.style.left = 20;
            _held.style.bottom = 20;
            root.Add(_held);
        }

        private static Label MakeLabel(string text, int size, Color color)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.style.fontSize = size;
            l.style.color = color;
            return l;
        }

        private void Update()
        {
            if (interaction == null || _prompt == null) return;
            _prompt.text = interaction.Prompt;
            _rejection.text = interaction.Rejection;
            var held = interaction.HeldItemId;
            _held.text = string.IsNullOrEmpty(held) ? string.Empty : "Holding: " + held;
        }
    }
}
