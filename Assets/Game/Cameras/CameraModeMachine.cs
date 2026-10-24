namespace PlantNursery.Game
{
    /// <summary>
    /// Decides which camera mode is active. Plain C# (no Unity types) so it can be tested directly.
    /// Follow becomes ZoomedOut on the zoom button, or after <see cref="AutoZoomOutSeconds"/> with no input.
    /// While zoomed out, any input (or the zoom button again) returns to Follow.
    /// </summary>
    public sealed class CameraModeMachine
    {
        private float _idle;

        /// <summary>Seconds without input before Follow zooms out.</summary>
        public float AutoZoomOutSeconds { get; }
        /// <summary>Current mode.</summary>
        public CameraMode Mode { get; private set; } = CameraMode.Follow;

        /// <summary>Create with the idle time from config.</summary>
        public CameraModeMachine(float autoZoomOutSeconds)
        {
            AutoZoomOutSeconds = autoZoomOutSeconds;
        }

        /// <summary>Advance one frame. Returns true if the mode changed.</summary>
        /// <param name="dt">Frame time in seconds.</param>
        /// <param name="anyInput">Movement or any button held this frame (not the zoom press itself).</param>
        /// <param name="zoomPressed">The zoom button went down this frame.</param>
        public bool Update(float dt, bool anyInput, bool zoomPressed)
        {
            var before = Mode;
            if (Mode == CameraMode.Follow)
            {
                _idle = anyInput || zoomPressed ? 0f : _idle + dt;
                if (zoomPressed || _idle >= AutoZoomOutSeconds) Mode = CameraMode.ZoomedOut;
            }
            else if (Mode == CameraMode.ZoomedOut)
            {
                if (anyInput || zoomPressed) { Mode = CameraMode.Follow; _idle = 0f; }
            }
            return Mode != before;
        }
    }
}
