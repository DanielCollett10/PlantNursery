using UnityEngine;
using UnityEngine.InputSystem;

namespace PlantNursery.Game
{
    /// <summary>
    /// Top-down CharacterController movement at <see cref="WalkSpeed"/> m/s from the Walk map's Move action
    /// (WASD or left stick), relative to the camera's ground-plane heading. Unity owns the body (decision 0003);
    /// nothing here is a game rule. See docs/decisions/0010 and docs/design/controls.md.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class TopDownPlayerController : MonoBehaviour
    {
        /// <summary>Walking speed in metres per second.</summary>
        public const float WalkSpeed = 3f;

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private Transform viewCamera;
        [SerializeField] private float gravity = -20f;

        private CharacterController _controller;
        private InputAction _move;
        private float _verticalSpeed;

        /// <summary>Last world-space facing on the ground plane (zero until the player moves).</summary>
        public Vector3 Facing { get; private set; }

        /// <summary>Assign the input asset and the camera that defines "up the screen".</summary>
        public void Configure(InputActionAsset asset, Transform cameraTransform)
        {
            actions = asset; viewCamera = cameraTransform;
        }

        /// <summary>
        /// Converts a stick/WASD vector to a world direction on the ground plane, relative to the camera heading.
        /// Magnitude is clamped to 1.
        /// </summary>
        public static Vector3 ToWorld(Vector2 input, Transform camera)
        {
            input = Vector2.ClampMagnitude(input, 1f);
            var fwd = camera != null ? Vector3.ProjectOnPlane(camera.forward, Vector3.up) : Vector3.forward;
            if (fwd.sqrMagnitude < 1e-4f)
                fwd = camera != null ? Vector3.ProjectOnPlane(camera.up, Vector3.up) : Vector3.forward; // looking straight down
            fwd.Normalize();
            var right = Vector3.Cross(Vector3.up, fwd);
            return right * input.x + fwd * input.y;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _controller.height = 1.8f;
            _controller.radius = 0.3f;
            _controller.center = new Vector3(0f, 0.9f, 0f);
        }

        private void OnEnable()
        {
            if (actions == null) { Debug.LogError("TopDownPlayerController needs 'actions' assigned.", this); return; }
            var map = actions.FindActionMap("Walk", true);
            _move = map.FindAction("Move", true);
            map.Enable();
        }

        private void OnDisable()
        {
            _move = null;
        }

        private void Update()
        {
            if (_move == null) return;
            var dir = ToWorld(_move.ReadValue<Vector2>(), viewCamera);
            if (dir.sqrMagnitude > 1e-4f) Facing = dir.normalized;
            _verticalSpeed = _controller.isGrounded ? -1f : _verticalSpeed + gravity * Time.deltaTime;
            var velocity = dir * WalkSpeed;
            velocity.y = _verticalSpeed;
            _controller.Move(velocity * Time.deltaTime);
        }
    }
}
