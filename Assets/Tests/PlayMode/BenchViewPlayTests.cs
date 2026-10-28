using System.Collections;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Storage;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace PlantNursery.Game.PlayModeTests
{
    /// <summary>
    /// Bench view (T-157): enter by interacting, pan straight down, exit by walking away, point at a slot with a virtual mouse
    /// (real Mouse.current position) and with a virtual gamepad (d-pad and bumpers), highlight and ghost.
    /// </summary>
    public sealed class BenchViewPlayTests : InputTestFixture
    {
        private Keyboard _keyboard;
        private Gamepad _gamepad;
        private GameObject _hostGo, _player, _rigGo, _camGo, _viewGo;
        private InputActionAsset _asset;
        private SimHost _host;
        private PlacedItem _bench;
        private CameraRig _rig;
        private Camera _cam;
        private BenchViewController _view;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _asset = PlantNursery.Game.Controls.NurseryControlsBuilder.Build();

            _hostGo = new GameObject("SimHost");
            _host = _hostGo.AddComponent<SimHost>();
            foreach (var p in _host.Build.PlacedItems) if (p.ItemId == "bench_3m") _bench = p;
            Assert.IsNotNull(_bench, "starter nursery has a bench");

            _player = new GameObject("Player");
            _player.SetActive(false);
            _player.transform.position = InFrontOfBench(0.4f);
            _player.AddComponent<WorldInteractions>().Configure(_host, _player.transform, _asset);
            _player.SetActive(true);

            _rigGo = new GameObject("Rig");
            _rigGo.SetActive(false);
            _camGo = new GameObject("Cam");
            _camGo.tag = "Untagged";
            _cam = _camGo.AddComponent<Camera>();
            _camGo.transform.SetParent(_rigGo.transform, false);
            _rig = _rigGo.AddComponent<CameraRig>();
            _rig.Configure(_host, _player.transform, _camGo.transform, _asset);
            _rigGo.SetActive(true);

            _viewGo = new GameObject("BenchView");
            _viewGo.SetActive(false);
            _view = _viewGo.AddComponent<BenchViewController>();
            _view.Configure(_host, _player.transform, _rig, _cam, _asset, null);
            _viewGo.SetActive(true);
        }

        public override void TearDown()
        {
            Destroy(_viewGo); Destroy(_rigGo); Destroy(_player); Destroy(_hostGo); Destroy(_asset);
            base.TearDown();
        }

        private static void Destroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        private Vector3 BenchMin() => new Vector3(_bench.Anchor.X * 0.5f, 0f, _bench.Anchor.Z * 0.5f);

        /// <summary>A point in front (south) of the bench, <paramref name="gap"/> metres from its front edge, mid-bench.</summary>
        private Vector3 InFrontOfBench(float gap) => BenchMin() + new Vector3(_bench.Width * 0.25f, 0f, -gap);

        // A sideways shift of up to 5 cells (bench is 6 cells long) still overlaps along the back row.
        private static bool BenchSpaceShares(int shiftCells) => System.Math.Abs(shiftCells) < 6;

        private PlantNursery.Sim.Core.PlayerId Me => _host.Session.LocalPlayer;

        private IEnumerator PressInteract()
        {
            yield return null; yield return null;
            Press(_keyboard.eKey);
            yield return null;
            Release(_keyboard.eKey);
            yield return null;
        }

        private IEnumerator EnterAndSettle()
        {
            yield return PressInteract();
            Assert.IsTrue(_view.InBenchView, "interacting at the bench enters bench view");
            yield return new WaitForSeconds(0.8f); // the pan is 0.4 s
        }

        private Vector2 ScreenOf(SlotAddress a, float planeY)
        {
            var rect = BenchSpace.SlotRectOf(_host.Content, _host.Build, a).Value;
            Assert.IsTrue(BenchSpace.TryLocalToWorld(_host.Content, _bench, rect.CentreU, rect.CentreV, out var x, out var z));
            var sp = _cam.WorldToScreenPoint(new Vector3((float)x, planeY, (float)z));
            Assert.Greater(sp.z, 0f, "slot is in front of the camera");
            return new Vector2(sp.x, sp.y);
        }

        [UnityTest]
        public IEnumerator Interact_PansStraightDown_AndWalkingAwayExits()
        {
            yield return EnterAndSettle();
            Assert.AreEqual(CameraMode.BenchView, _rig.Mode);
            Assert.Greater(Vector3.Dot(_camGo.transform.forward, Vector3.down), 0.99f, "looking straight down");
            var c = BenchMin() + new Vector3(_bench.Width * 0.25f, 0f, _bench.Depth * 0.25f);
            Assert.AreEqual(c.x, _camGo.transform.position.x, 0.05f);
            Assert.AreEqual(c.z, _camGo.transform.position.z, 0.05f);

            _player.transform.position = InFrontOfBench(0.8f); // still within 1.0 m
            yield return null; yield return null;
            Assert.IsTrue(_view.InBenchView);

            _player.transform.position = InFrontOfBench(1.3f); // beyond 1.0 m
            yield return null; yield return null;
            Assert.IsFalse(_view.InBenchView, "walking > 1.0 m away exits");
            Assert.AreEqual(CameraMode.Follow, _rig.Mode);
        }

        [UnityTest]
        public IEnumerator BackToBackBenches_ShareOneView()
        {
            _host.Build.Inventory.Add("bench_3m", 1);
            // Back to back: a 180-degree bench whose back row touches ours. Shift along until the spot is free.
            PlacedItem other = null;
            for (var dx = 0; dx <= 6 && other == null; dx++)
                foreach (var sx in new[] { dx, -dx })
                {
                    var anchor = new Cell(_bench.Anchor.X + sx, _bench.Anchor.Z + 2);
                    if (_host.Build.CanPlace("bench_3m", ItemLayer.Floor, anchor, Rotation.Deg180) != BuildRejection.None) continue;
                    if (BenchSpaceShares(sx)) { other = _host.Build.Place("bench_3m", ItemLayer.Floor, anchor, Rotation.Deg180, out _); break; }
                }
            Assert.IsNotNull(other, "found a free back-to-back spot");
            yield return PressInteract();
            Assert.IsTrue(_view.InBenchView);
            CollectionAssert.AreEquivalent(new[] { _bench.Id, other.Id }, _view.ViewBenchIds);
            yield return new WaitForSeconds(0.8f);
            // The view is centred on both benches (z spans both depths).
            var zc = (_bench.Anchor.Z * 0.5f + (other.Anchor.Z + other.Depth) * 0.5f) / 2f;
            Assert.AreEqual(zc, _camGo.transform.position.z, 0.05f);
        }

        [UnityTest]
        public IEnumerator Mouse_PointsAtSlot_AndLightsItInThePlayerColour()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            yield return EnterAndSettle();
            var target = SlotAddress.Strip(_bench.Id, "watering_can", 0, 0);
            Assert.IsNotNull(_host.Nursery.Contents.Get(target), "the can sits in strip slot 0,0 (empty-handed picks occupied slots)");

            Set(mouse.position, ScreenOf(target, 0.9f));
            yield return null; yield return null;
            Assert.IsTrue(_view.UsingMouse);
            Assert.IsTrue(_view.PointedSlot.HasValue, _view.DebugText);
            Assert.AreEqual(target.Key, _view.PointedSlot.Value.Key, _view.DebugText);

            var rect = BenchSpace.SlotRectOf(_host.Content, _host.Build, target).Value;
            BenchSpace.TryLocalToWorld(_host.Content, _bench, rect.CentreU, rect.CentreV, out var x, out var z);
            Assert.AreEqual((float)x, _view.HighlightPosition.x, 0.01f);
            Assert.AreEqual((float)z, _view.HighlightPosition.z, 0.01f);
            var expected = BenchViewController.PlayerColour(Me);
            Assert.AreEqual(expected.r, _view.HighlightColour.r, 0.01f);
            Assert.AreEqual(expected.g, _view.HighlightColour.g, 0.01f);
            Assert.AreEqual(expected.b, _view.HighlightColour.b, 0.01f);
        }

        [UnityTest]
        public IEnumerator Mouse_Ghost_GreenOnValidSlot_RedWithSimReasonOnInvalid()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            var canSlot = SlotAddress.Strip(_bench.Id, "watering_can", 0, 0);
            Assert.IsTrue(_host.Submit(new PickUpFromSlot(Me, canSlot)).IsAccepted);
            yield return EnterAndSettle();

            Set(mouse.position, ScreenOf(canSlot, 0.9f));
            yield return null; yield return null;
            Assert.IsTrue(_view.PointedSlot.HasValue, _view.DebugText);
            Assert.AreEqual(canSlot.Key, _view.PointedSlot.Value.Key, _view.DebugText);
            Assert.IsTrue(_view.GhostVisible);
            Assert.IsTrue(_view.GhostValidNow, "putting the can back on its own strip slot is valid");

            // Over the planting area the can cannot go: the Sim's reason is shown.
            var planting = SlotAddress.Planting(_bench.Id, "pot_small", 3, 1);
            Set(mouse.position, ScreenOf(planting, 0.9f));
            yield return null; yield return null;
            Assert.IsTrue(_view.PointedSlot.HasValue, _view.DebugText);
            Assert.IsFalse(_view.PointedSlot.Value.IsStrip);
            Assert.IsTrue(_view.GhostVisible);
            Assert.IsFalse(_view.GhostValidNow);
            Assert.AreEqual("WrongSize", _view.GhostReason);
        }

        [UnityTest]
        public IEnumerator Gamepad_StartsNearPlayer_AndDpadAndBumpersStepSlots()
        {
            yield return EnterAndSettle();
            yield return null;
            Assert.IsFalse(_view.UsingMouse, "no mouse: the gamepad stepper is the pointer");
            Assert.IsTrue(_view.PointedSlot.HasValue, "starts at the slot nearest the player");
            var start = _view.PointedSlot.Value;
            var expectedStart = new SlotStepper(_host.Content, _host.Build, null, _bench.Rotation)
                .Start(_view.ViewBenchIds, _player.transform.position.x, _player.transform.position.z);
            Assert.AreEqual(expectedStart.Value.Key, start.Key);

            Press(_gamepad.dpad.right);
            yield return null;
            Release(_gamepad.dpad.right);
            yield return null;
            var afterRight = _view.PointedSlot.Value;
            Assert.AreEqual(start.Slot.Col + 1, afterRight.Slot.Col, "d-pad right moves one column");
            Assert.AreEqual(start.Slot.Row, afterRight.Slot.Row);
            Assert.AreEqual(start.Slot.Size, afterRight.Slot.Size);

            Press(_gamepad.rightShoulder);
            yield return null;
            Release(_gamepad.rightShoulder);
            yield return null;
            Assert.AreEqual(start.Slot.Col + 2, _view.PointedSlot.Value.Slot.Col, "right bumper steps on");

            Press(_gamepad.leftShoulder);
            yield return null;
            Release(_gamepad.leftShoulder);
            yield return null;
            Assert.AreEqual(start.Slot.Col + 1, _view.PointedSlot.Value.Slot.Col, "left bumper steps back");

            var rect = BenchSpace.SlotRectOf(_host.Content, _host.Build, _view.PointedSlot.Value).Value;
            BenchSpace.TryLocalToWorld(_host.Content, _bench, rect.CentreU, rect.CentreV, out var x, out var z);
            Assert.AreEqual((float)x, _view.HighlightPosition.x, 0.01f);
            Assert.AreEqual((float)z, _view.HighlightPosition.z, 0.01f);
        }

        // ---------- T-158: pick up, put down and sow ----------

        private PlantNursery.Sim.Storage.HeldThing Held => _host.Nursery.Hands.GetHeld(Me);

        private SlotAddress TraySlot => SlotAddress.Planting(_bench.Id, "plug_tray", 0, 0);

        private IEnumerator Settle() { yield return null; yield return null; }

        private IEnumerator ClickMouse(Mouse mouse)
        {
            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return null;
        }

        private IEnumerator PressPad(UnityEngine.InputSystem.Controls.ButtonControl button)
        {
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }

        private IEnumerator PointMouseAt(Mouse mouse, SlotAddress a)
        {
            Set(mouse.position, ScreenOf(a, 0.9f));
            yield return Settle();
            Assert.IsTrue(_view.PointedSlot.HasValue, _view.DebugText);
            Assert.AreEqual(a.Key, _view.PointedSlot.Value.Key, _view.DebugText);
        }

        /// <summary>Step the gamepad pointer to a slot with the d-pad (bounded).</summary>
        private IEnumerator StepPadTo(SlotAddress a)
        {
            for (var i = 0; i < 40 && !(_view.PointedSlot.HasValue && _view.PointedSlot.Value.Key == a.Key); i++)
            {
                yield return PressPad(i < 8 ? _gamepad.dpad.right : _gamepad.dpad.left);
            }
            Assert.IsTrue(_view.PointedSlot.HasValue && _view.PointedSlot.Value.Key == a.Key, "stepped to " + a.Key + ": " + _view.DebugText);
        }

        /// <summary>Swap the tray in slot 0,0 for an empty one and put a chimebell packet in the player's hands.</summary>
        private PlantNursery.Sim.Plants.Container EmptyTrayInSlotAndPacketInHand()
        {
            Assert.IsTrue(_host.Submit(new PickUpFromSlot(Me, TraySlot)).IsAccepted);
            _host.Nursery.Hands.TakeHeld(Me);
            var tray = new PlantNursery.Sim.Plants.EntityFactory(_host.World, _host.Content).CreateContainer("plug_tray");
            _host.Nursery.Hands.TryGive(Me, HeldThing.OfContainer(tray));
            Assert.IsTrue(_host.Submit(new PutInSlot(Me, TraySlot)).IsAccepted);
            Assert.IsTrue(_host.Nursery.Hands.TryGive(Me, HeldThing.Fresh(_host.Content.ItemDefs["seeds_chimebell_white"])));
            return tray;
        }

        [UnityTest]
        public IEnumerator Mouse_TakePot_PlaceIt()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            yield return EnterAndSettle();

            yield return PointMouseAt(mouse, TraySlot);
            yield return ClickMouse(mouse);
            Assert.AreEqual("PickUpFromSlot", _view.LastActionName);
            Assert.IsNotNull(Held, "the tray is in the hands");
            Assert.IsNull(_host.Nursery.Contents.Get(TraySlot));

            yield return PointMouseAt(mouse, TraySlot);
            Assert.IsTrue(_view.GhostValidNow, _view.GhostReason);
            yield return ClickMouse(mouse);
            Assert.AreEqual("PutInSlot", _view.LastActionName);
            Assert.IsNull(Held, "hands are empty again");
            Assert.IsNotNull(_host.Nursery.Contents.Get(TraySlot), "the tray is back in its slot");
        }

        [UnityTest]
        public IEnumerator Mouse_Packet_SowsTray_AndRejectsOutsideAContainer()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            yield return EnterAndSettle();
            var tray = EmptyTrayInSlotAndPacketInHand();

            // Over the strip's can there is no container: the Sim's rejection is shown.
            var canSlot = SlotAddress.Strip(_bench.Id, "watering_can", 0, 0);
            yield return PointMouseAt(mouse, canSlot);
            yield return ClickMouse(mouse);
            Assert.AreEqual("SowSpecies", _view.LastActionName);
            Assert.IsFalse(_view.LastActionResult.IsAccepted);
            Assert.AreEqual("NotInSlot", _view.LastActionResult.Message);
            Assert.IsNotEmpty(_view.Rejection, "the rejection is shown");
            Assert.AreEqual(0, tray.PlantIds.Count);

            yield return PointMouseAt(mouse, TraySlot);
            yield return ClickMouse(mouse);
            Assert.IsTrue(_view.LastActionResult.IsAccepted, _view.LastActionResult.Message);
            Assert.AreEqual(6, tray.PlantIds.Count, "one interact sows the whole tray");
            Assert.IsNull(Held, "the empty packet is used up");
        }

        [UnityTest]
        public IEnumerator Gamepad_TakePot_PlaceIt()
        {
            yield return EnterAndSettle();
            yield return null;
            Assert.IsFalse(_view.UsingMouse);

            yield return StepPadTo(TraySlot);
            yield return PressPad(_gamepad.buttonSouth);
            Assert.AreEqual("PickUpFromSlot", _view.LastActionName);
            Assert.IsNotNull(Held, "the tray is in the hands");
            Assert.IsNull(_host.Nursery.Contents.Get(TraySlot));

            yield return StepPadTo(TraySlot);
            yield return PressPad(_gamepad.buttonSouth);
            Assert.AreEqual("PutInSlot", _view.LastActionName);
            Assert.IsNull(Held);
            Assert.IsNotNull(_host.Nursery.Contents.Get(TraySlot));
        }

        [UnityTest]
        public IEnumerator Gamepad_Packet_SowsTray()
        {
            yield return EnterAndSettle();
            yield return null;
            var tray = EmptyTrayInSlotAndPacketInHand();
            yield return Settle();

            yield return StepPadTo(TraySlot);
            yield return PressPad(_gamepad.buttonSouth);
            Assert.AreEqual("SowSpecies", _view.LastActionName);
            Assert.IsTrue(_view.LastActionResult.IsAccepted, _view.LastActionResult.Message);
            Assert.AreEqual(6, tray.PlantIds.Count);
            Assert.IsNull(Held);
        }
    }
}
