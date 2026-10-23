using UnityEngine;
using UnityEngine.InputSystem;

namespace PlantNursery.Game
{
    /// <summary>
    /// CharacterController-based first person movement and mouse look.
    /// Unity owns the player body (decision 0003); nothing here is a game rule.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        /// <summary>Eye height above the feet in metres.</summary>
        public const float EyeHeight = 1.65f;

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float lookSensitivity = 0.1f;
        [SerializeField] private float gravity = -20f;

        private CharacterController _controller;
        private InputAction _move, _look;
        private float _pitch, _verticalSpeed;

        /// <summary>Assign the input asset and camera pivot (used by the scene generator).</summary>
        public void Configure(InputActionAsset asset, Transform pivot)
        {
            actions = asset; cameraPivot = pivot;
        }

        /// <summary>The camera pivot at eye height.</summary>
        public Transform CameraPivot => cameraPivot;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _controller.height = 1.8f;
            _controller.radius = 0.3f;
            _controller.center = new Vector3(0f, 0.9f, 0f);
            if (cameraPivot != null) cameraPivot.localPosition = new Vector3(0f, EyeHeight, 0f);
        }

        private void OnEnable()
        {
            if (actions == null) return;
            var map = actions.FindActionMap("Player", true);
            _move = map.FindAction("Move", true);
            _look = map.FindAction("Look", true);
            map.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            if (actions != null) actions.FindActionMap("Player", false)?.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (_move == null) return;
            var look = _look.ReadValue<Vector2>() * lookSensitivity;
            transform.Rotate(0f, look.x, 0f);
            _pitch = Mathf.Clamp(_pitch - look.y, -85f, 85f);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            var input = _move.ReadValue<Vector2>();
            var velocity = (transform.right * input.x + transform.forward * input.y) * walkSpeed;
            _verticalSpeed = _controller.isGrounded ? -1f : _verticalSpeed + gravity * Time.deltaTime;
            velocity.y = _verticalSpeed;
            _controller.Move(velocity * Time.deltaTime);
        }
    }
}
