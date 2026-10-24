namespace PlantNursery.Game
{
    /// <summary>Camera modes (docs/design/controls.md, Cameras). Only Follow and ZoomedOut are implemented.</summary>
    public enum CameraMode
    {
        /// <summary>Tracks the local player (default).</summary>
        Follow,
        /// <summary>Pulled back to show more of the nursery.</summary>
        ZoomedOut,
        /// <summary>Stub: pan to look straight down on a bench. Not reachable yet.</summary>
        BenchView,
        /// <summary>Stub: fixed view of a rush zone. Not reachable yet.</summary>
        FixedArea,
    }
}
