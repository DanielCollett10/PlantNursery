using NUnit.Framework;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Genetics;
using PlantNursery.Sim.Plants;
using UnityEngine;

namespace PlantNursery.Game.Tests
{
    /// <summary>Pure state-to-look mappings for plants and containers (T-152).</summary>
    public class PlantLookTests
    {
        private GameObject _hostGo;
        private SimHost _host;

        [SetUp]
        public void SetUp()
        {
            _hostGo = new GameObject("SimHost");
            _host = _hostGo.AddComponent<SimHost>();
            _host.Initialize();
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(_hostGo); }

        private Plant AmberMoss()
        {
            var genome = new Genome(_host.Content, "hearthmoss", "hearthmoss_amber");
            return new Plant(1, "hearthmoss", "hearthmoss_amber", genome, "plug_tray", 0);
        }

        [Test]
        public void AmberPhenotypeGivesAmberTint()
        {
            var look = PlantLook.From(AmberMoss(), 2);
            Assert.AreEqual(PlantLook.Amber, look.Tint);
        }

        [Test]
        public void VioletAndBloomColoursMap()
        {
            Assert.AreEqual(PlantLook.Violet, PlantLook.ColourFor("violet"));
            Assert.AreEqual(PlantLook.Rose, PlantLook.ColourFor("rose"));
            Assert.AreEqual(WorldLayout.LeafRed, PlantLook.ColourFor("red"));
        }

        [Test]
        public void ScaleRunsFromFirstToLastStage()
        {
            Assert.AreEqual(0.1f, PlantLook.ScaleFor(0, 4), 1e-4f);
            Assert.AreEqual(1f, PlantLook.ScaleFor(3, 4), 1e-4f);
            Assert.Greater(PlantLook.ScaleFor(2, 4), PlantLook.ScaleFor(1, 4));
            Assert.AreEqual(1f, PlantLook.ScaleFor(1, 2), 1e-4f);
        }

        [Test]
        public void WiltedPlantDroopsAndDesaturates()
        {
            var plant = AmberMoss();
            var healthy = PlantLook.From(plant, 2);
            plant.IsWilted = true;
            var wilted = PlantLook.From(plant, 2);
            Assert.AreEqual(0f, healthy.Droop);
            Assert.AreEqual(1f, wilted.Droop);
            Color.RGBToHSV(healthy.Tint, out _, out var sHealthy, out _);
            Color.RGBToHSV(wilted.Tint, out _, out var sWilted, out _);
            Assert.Less(sWilted, sHealthy);
        }

        [Test]
        public void DeadPlantIsBrownGrey()
        {
            var plant = AmberMoss();
            plant.IsDead = true;
            var look = PlantLook.From(plant, 2);
            Assert.IsTrue(look.Dead);
            Assert.AreEqual(WorldLayout.DeadColour, look.Tint);
        }

        [Test]
        public void GaugeShowsBelowThirtyAndDryCueAtZero()
        {
            Assert.IsFalse(ContainerLook.From(30f).GaugeVisible);
            Assert.IsFalse(ContainerLook.From(100f).GaugeVisible);
            Assert.IsTrue(ContainerLook.From(29.9f).GaugeVisible);
            Assert.IsFalse(ContainerLook.From(29.9f).Dry);
            var dry = ContainerLook.From(0f);
            Assert.IsTrue(dry.GaugeVisible);
            Assert.IsTrue(dry.Dry);
            Assert.AreEqual(0.25f, ContainerLook.From(25f).GaugeFill, 1e-4f);
        }
    }
}
