using UnityEngine;
using UnityEngine.InputSystem;

namespace PlantNursery.Game
{
    /// <summary>
    /// Debug-only switch (F9) between the default top-down controller and the off-by-default first-person one
    /// (decision 0010). Nothing in gameplay may depend on first person. Moves the camera between its top-down
    /// pose and the first-person pivot.
    /// </summary>
    public sealed class DebugFirstPersonToggle : MonoBehaviour
    {
        [SerializeField] private TopDownPlayerController topDown;
        [SerializeField] private FirstPersonController firstPerson;
        [SerializeField] private Transform cameraTransform;

        private Vector3 _tdPos;
        private Quaternion _tdRot;

        /// <summary>True while the first-person debug view is active.</summary>
        public bool IsFirstPerson { get; private set; }

        /// <summary>Wire references (used by the scene generator).</summary>
        public void Configure(TopDownPlayerController td, FirstPersonController fp, Transform cam)
        {
            topDown = td; firstPerson = fp; cameraTransform = cam;
        }

        /// <summary>Switch view. Does nothing if the controllers are missing.</summary>
        public void Set(bool firstPersonOn)
        {
            if (topDown == null || firstPerson == null || firstPersonOn == IsFirstPerson) return;
            IsFirstPerson = firstPersonOn;
            if (firstPersonOn && cameraTransform != null)
            {
                _tdPos = cameraTransform.localPosition; _tdRot = cameraTransform.localRotation;
                cameraTransform.SetParent(firstPerson.CameraPivot, false);
                cameraTransform.localPosition = Vector3.zero; cameraTransform.localRotation = Quaternion.identity;
            }
            else if (cameraTransform != null)
            {
                cameraTransform.SetParent(transform, false);
                cameraTransform.localPosition = _tdPos; cameraTransform.localRotation = _tdRot;
            }
            topDown.enabled = !firstPersonOn;
            firstPerson.enabled = firstPersonOn;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame) Set(!IsFirstPerson);
        }
    }
}
