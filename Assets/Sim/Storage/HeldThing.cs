using PlantNursery.Sim.Content;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Storage
{
    /// <summary>What kind of thing a hand holds.</summary>
    public enum HeldKind
    {
        /// <summary>A pot or tray entity.</summary>
        Container,
        /// <summary>A carry tray.</summary>
        CarryTray,
        /// <summary>A watering can with charges.</summary>
        Can,
        /// <summary>A plant bundle with a quality grade.</summary>
        Bundle,
        /// <summary>A seed packet with seeds left.</summary>
        SeedPacket
    }

    /// <summary>One thing held in a hand (HandsV2) or kept loose in the crate. Plain data.</summary>
    public sealed class HeldThing
    {
        /// <summary>The kind of thing.</summary>
        public HeldKind Kind { get; }
        /// <summary>Item definition id.</summary>
        public string ItemId { get; }
        /// <summary>The container entity (Kind Container only), else null.</summary>
        public Container Container { get; }
        /// <summary>Water charges left (cans).</summary>
        public int Charges { get; set; }
        /// <summary>Quality grade (bundles).</summary>
        public QualityGrade Quality { get; set; } = QualityGrade.Good;
        /// <summary>Seeds left (seed packets).</summary>
        public int SeedsLeft { get; set; }

        /// <summary>Creates a held thing.</summary>
        public HeldThing(HeldKind kind, string itemId, Container container = null)
        {
            Kind = kind; ItemId = itemId; Container = container;
        }

        /// <summary>Wrap a container entity.</summary>
        public static HeldThing OfContainer(Container c) => new HeldThing(HeldKind.Container, c.Def.ItemId, c);

        /// <summary>A fresh (full / new) non-container thing for an item id, shaped by its definition.</summary>
        public static HeldThing Fresh(ItemDef def)
        {
            if (def.WaterCapacity > 0) return new HeldThing(HeldKind.Can, def.Id) { Charges = def.WaterCapacity };
            if (def.PacketSeeds > 0 || def.SeedCount > 0)
                return new HeldThing(HeldKind.SeedPacket, def.Id) { SeedsLeft = def.PacketSeeds > 0 ? def.PacketSeeds : def.SeedCount };
            if (def.Capacity > 0) return new HeldThing(HeldKind.CarryTray, def.Id);
            return new HeldThing(HeldKind.Bundle, def.Id);
        }
    }
}
