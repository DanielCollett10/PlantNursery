using PlantNursery.Sim.Building;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Time;

namespace PlantNursery.Sim.Hands
{
    /// <summary>What the player's crosshair is on. Unity fills this in from a raycast.</summary>
    public enum TargetKind
    {
        /// <summary>Nothing in reach.</summary>
        None,
        /// <summary>A surface item (can, packet, tray, pot) or one of its cells.</summary>
        SurfaceItem,
        /// <summary>The tap.</summary>
        Tap,
        /// <summary>The computer.</summary>
        Computer,
        /// <summary>The bed.</summary>
        Bed
    }

    /// <summary>A raycast target described in sim terms.</summary>
    public readonly struct InteractTarget
    {
        /// <summary>Kind of target.</summary>
        public TargetKind Kind { get; }
        /// <summary>Surface item id (for <see cref="TargetKind.SurfaceItem"/>).</summary>
        public int SurfaceItemId { get; }
        /// <summary>Cell index inside a container, or -1 when the whole item is aimed at.</summary>
        public int CellIndex { get; }

        public InteractTarget(TargetKind kind, int surfaceItemId = 0, int cellIndex = -1)
        { Kind = kind; SurfaceItemId = surfaceItemId; CellIndex = cellIndex; }

        /// <summary>Nothing targeted.</summary>
        public static InteractTarget None => new InteractTarget(TargetKind.None);
    }

    /// <summary>The single action offered for a target and held item.</summary>
    public sealed class InteractionOffer
    {
        /// <summary>Prompt text for the HUD, e.g. "E: Water".</summary>
        public string Prompt { get; }
        /// <summary>Command to send, or null for a UI action (<see cref="OpensShop"/>).</summary>
        public ICommand Command { get; }
        /// <summary>True when the action opens the shop UI instead of sending a command.</summary>
        public bool OpensShop { get; }

        public InteractionOffer(string prompt, ICommand command, bool opensShop = false)
        { Prompt = prompt; Command = command; OpensShop = opensShop; }
    }

    /// <summary>
    /// Maps (held item, target) to the one offered action, per docs/design/controls.md.
    /// Query-only: the command handlers still validate everything.
    /// </summary>
    public static class Interaction
    {
        /// <summary>Which target kind a placed floor item is (tap, computer, bed), else None.</summary>
        public static TargetKind KindForPlacedItem(string itemId)
        {
            switch (itemId)
            {
                case "tap": return TargetKind.Tap;
                case "computer": return TargetKind.Computer;
                case "bed": return TargetKind.Bed;
                default: return TargetKind.None;
            }
        }

        /// <summary>The offer for this player, or null when nothing can be done.</summary>
        public static InteractionOffer Offer(PlayerId player, InteractTarget target, HandsState hands, SurfaceGrid surface)
        {
            switch (target.Kind)
            {
                case TargetKind.Computer: return new InteractionOffer("E: Use computer", null, true);
                case TargetKind.Bed: return new InteractionOffer("E: Sleep", new SleepCommand(player));
                case TargetKind.Tap:
                    return IsHolding(hands, player, "watering_can")
                        ? new InteractionOffer("E: Refill can", new RefillCan(player)) : null;
                case TargetKind.SurfaceItem: return OfferForItem(player, target, hands, surface);
                default: return null;
            }
        }

        private static InteractionOffer OfferForItem(PlayerId player, InteractTarget t, HandsState hands, SurfaceGrid surface)
        {
            var item = Find(surface, t.SurfaceItemId);
            if (item == null) return null;
            var held = hands.GetHeld(player);
            if (held == null) return new InteractionOffer("E: Pick up", new PickUp(player, item.Id));

            if (held.ItemId == "watering_can")
            {
                var plant = PlantAt(hands, item, t.CellIndex);
                return plant != null ? new InteractionOffer("E: Water", new WaterPlant(player, plant.Id)) : null;
            }
            if (held.ItemId == "seed_packet" && t.CellIndex >= 0)
                return new InteractionOffer("E: Sow", new Sow(player, item.Id, t.CellIndex));
            return null;
        }

        private static Plant PlantAt(HandsState hands, SurfaceItem item, int cell)
        {
            foreach (var p in hands.GetContents(item).Plants)
                if (cell < 0 || p.CellIndex == cell) return p;
            return null;
        }

        private static SurfaceItem Find(SurfaceGrid surface, int id)
        {
            foreach (var i in surface.Items) if (i.Id == id) return i;
            return null;
        }

        private static bool IsHolding(HandsState hands, PlayerId player, string itemId)
        {
            var h = hands.GetHeld(player);
            return h != null && h.ItemId == itemId;
        }
    }
}
