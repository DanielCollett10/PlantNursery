using System;
using System.Collections.Generic;
using System.IO;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Economy;
using PlantNursery.Sim.Hands;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Time;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Owns the <see cref="SimWorld"/>: loads content from StreamingAssets/Content, steps ticks at the
    /// real-time pace of the current day phase, and dispatches sim events to views.
    /// Holds no game rules (decision 0005): views read state and send commands through <see cref="Submit"/>.
    /// </summary>
    public sealed class SimHost : MonoBehaviour
    {
        private const int MaxTicksPerFrame = 10;

        [SerializeField] private bool useDemoProfile;

        private double _tickAccumulator;

        /// <summary>Use time.demo.json overrides (shorter days). Set before <see cref="Initialize"/>.</summary>
        public bool UseDemoProfile { get => useDemoProfile; set => useDemoProfile = value; }
        /// <summary>The local session (holds the local <see cref="PlayerId"/>). Available before <see cref="Initialize"/>.</summary>
        public Session Session { get; } = new Session(new PlayerId(0));
        /// <summary>The local player id, from <see cref="Session"/>.</summary>
        public PlayerId LocalPlayerId => Session.LocalPlayer;
        /// <summary>
        /// Where player commands go. Set by <see cref="Initialize"/>; a <see cref="LoopbackCommandSink"/> for now,
        /// a network sink later. Null before <see cref="Initialize"/>.
        /// </summary>
        public ICommandSink Sink { get; private set; }
        /// <summary>Loaded content, or null before <see cref="Initialize"/>.</summary>
        public ContentDb Content { get; private set; }
        /// <summary>The simulation, or null before <see cref="Initialize"/>.</summary>
        public SimWorld World { get; private set; }
        /// <summary>Game clock.</summary>
        public GameClock Clock { get; private set; }
        /// <summary>Floor grid and placed items.</summary>
        public BuildState Build { get; private set; }
        /// <summary>Surface mini-grid.</summary>
        public SurfaceGrid Surface { get; private set; }
        /// <summary>Held items and container contents.</summary>
        public HandsState Hands { get; private set; }
        /// <summary>Shared coins.</summary>
        public Wallet Wallet { get; private set; }
        /// <summary>True once content is loaded and the world exists.</summary>
        public bool IsReady => World != null;

        /// <summary>Raised for every sim event drained from the world (once per frame, in order).</summary>
        public event Action<ISimEvent> EventDispatched;

        /// <summary>Folder holding the content JSON files.</summary>
        public static string ContentFolder => Path.Combine(Application.streamingAssetsPath, "Content");

        /// <summary>Read every *.json in the content folder. time.demo.json is dropped unless <paramref name="demo"/>.</summary>
        public static Dictionary<string, string> ReadContentFiles(bool demo)
        {
            var files = new Dictionary<string, string>();
            foreach (var path in Directory.GetFiles(ContentFolder, "*.json"))
            {
                var name = Path.GetFileName(path);
                if (!demo && name == ContentLoader.TimeDemoFile) continue;
                files[name] = File.ReadAllText(path);
            }
            return files;
        }

        /// <summary>Load content and build the world. Safe to call more than once (later calls do nothing).</summary>
        /// <exception cref="ContentException">If content is invalid.</exception>
        public void Initialize()
        {
            if (IsReady) return;
            var content = ContentLoader.Load(ReadContentFiles(useDemoProfile));
            var world = new SimWorld();
            var clock = GameClock.Install(world, content.Time);
            var build = new BuildState(content);
            var surface = new SurfaceGrid(content, build);
            var wallet = new Wallet(content.Economy.StartingMoney);
            BuildingModule.Register(world, build);
            SupplierModule.Register(world, content, wallet, build, surface);
            var hands = new HandsState(content, surface);
            HandsModule.Register(world, hands);
            GrowthSystem.Install(world, content, hands, clock);
            SoftLockGuard.Install(world, content, wallet, build, surface, hands, clock);

            Content = content; Clock = clock; Build = build; Surface = surface; Wallet = wallet; Hands = hands;
            World = world;
            Sink = new LoopbackCommandSink(world);
        }

        private void Awake()
        {
            if (Application.isPlaying) Initialize();
        }

        private void Update()
        {
            if (!IsReady) return;
            Advance(Time.deltaTime);
        }

        /// <summary>Advance by real seconds: steps whole ticks at the current phase's pace, then dispatches events.</summary>
        public void Advance(double realSeconds)
        {
            if (!IsReady) return;
            if (!World.IsPaused)
            {
                _tickAccumulator += realSeconds;
                for (var i = 0; i < MaxTicksPerFrame; i++)
                {
                    var perTick = Clock.RealSecondsPerTick;
                    if (_tickAccumulator < perTick) break;
                    _tickAccumulator -= perTick;
                    World.Step(1);
                }
                // Drop backlog (e.g. after a hitch) rather than spiral.
                if (_tickAccumulator > Clock.RealSecondsPerTick) _tickAccumulator = 0;
            }
            DispatchEvents();
        }

        /// <summary>Query-only check of a command (used by the build ghost).</summary>
        public CommandResult Validate(ICommand command) => World.Validate(command);

        /// <summary>
        /// Queue a command through <see cref="Sink"/>. An accepted <see cref="SleepCommand"/> fast-forwards at once:
        /// the world is stepped until the next morning and the events are dispatched. Returns the validation result.
        /// </summary>
        public CommandResult Submit(ICommand command)
        {
            var result = World.Validate(command);
            Sink.Submit(command);
            if (result.IsAccepted && command is SleepCommand)
            {
                World.Step(1); // applies the command; the world then runs on to the next day start
                _tickAccumulator = 0;
                DispatchEvents();
            }
            else if (result.IsAccepted)
            {
                // A tick can last seconds of real time; apply now so views update this frame.
                World.ApplyPending();
                DispatchEvents();
            }
            return result;
        }

        /// <summary>Drain the world's events and hand each to subscribers.</summary>
        public void DispatchEvents()
        {
            foreach (var evt in World.DrainEvents()) EventDispatched?.Invoke(evt);
        }
    }
}
