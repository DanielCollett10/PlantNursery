using System;
using System.Collections.Generic;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Storage
{
    /// <summary>Why a HandsV2 crate action was refused. The name is the command result message.</summary>
    public enum CrateRejection
    {
        /// <summary>Accepted.</summary>
        None,
        /// <summary>The player already holds something.</summary>
        HandsFull,
        /// <summary>The crate has no such item or entity.</summary>
        NotInCrate,
        /// <summary>The player holds nothing.</summary>
        NothingHeld
    }

    /// <summary>
    /// Per-player hands, version 2: each <see cref="PlayerId"/> holds at most one <see cref="HeldThing"/>.
    /// Separate from the old <c>Hands.HandsState</c>, which is untouched. Each player only ever touches their own hand.
    /// </summary>
    public sealed class HandsV2
    {
        private readonly ContentDb _content;
        private readonly StorageCrate _crate;
        private readonly Dictionary<int, HeldThing> _held = new Dictionary<int, HeldThing>();

        /// <summary>Creates hands over a crate.</summary>
        public HandsV2(ContentDb content, StorageCrate crate)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _crate = crate ?? throw new ArgumentNullException(nameof(crate));
        }

        /// <summary>What the player holds, or null.</summary>
        public HeldThing GetHeld(PlayerId player) => _held.TryGetValue(player.Value, out var h) ? h : null;

        /// <summary>Put a thing into an empty hand. False (no change) if the hand is full or the thing is null.</summary>
        public bool TryGive(PlayerId player, HeldThing thing)
        {
            if (thing == null || _held.ContainsKey(player.Value)) return false;
            _held[player.Value] = thing;
            return true;
        }

        /// <summary>Empty the hand and return what was in it, or null.</summary>
        public HeldThing TakeHeld(PlayerId player)
        {
            if (!_held.TryGetValue(player.Value, out var h)) return null;
            _held.Remove(player.Value);
            return h;
        }

        /// <summary>Query-only check for taking an item id from the crate.</summary>
        public CrateRejection CanTake(PlayerId player, string itemId)
        {
            if (_held.ContainsKey(player.Value)) return CrateRejection.HandsFull;
            return itemId != null && _crate.HasItem(itemId) && _content.ItemDefs.ContainsKey(itemId)
                ? CrateRejection.None : CrateRejection.NotInCrate;
        }

        /// <summary>Query-only check for taking a container entity from the crate.</summary>
        public CrateRejection CanTake(PlayerId player, int entityId)
        {
            if (_held.ContainsKey(player.Value)) return CrateRejection.HandsFull;
            return _crate.FindContainer(entityId) != null ? CrateRejection.None : CrateRejection.NotInCrate;
        }

        /// <summary>Take one item by id into the empty hand (a container entity if one matches, else loose or counted stock).</summary>
        public CrateRejection Take(PlayerId player, string itemId)
        {
            var r = CanTake(player, itemId);
            if (r != CrateRejection.None) return r;
            HeldThing thing = null;
            foreach (var c in _crate.Containers)
                if (c.Def.ItemId == itemId) { thing = HeldThing.OfContainer(_crate.TakeContainer(c.Id)); break; }
            if (thing == null) thing = _crate.TakeThing(itemId);
            if (thing == null && _crate.TryTakeStock(itemId)) thing = HeldThing.Fresh(_content.ItemDefs[itemId]);
            if (thing == null) return CrateRejection.NotInCrate;
            _held[player.Value] = thing;
            return CrateRejection.None;
        }

        /// <summary>Take one container entity by entity id into the empty hand.</summary>
        public CrateRejection Take(PlayerId player, int entityId)
        {
            var r = CanTake(player, entityId);
            if (r != CrateRejection.None) return r;
            _held[player.Value] = HeldThing.OfContainer(_crate.TakeContainer(entityId));
            return CrateRejection.None;
        }

        /// <summary>Query-only check for returning the held thing.</summary>
        public CrateRejection CanReturn(PlayerId player) =>
            _held.ContainsKey(player.Value) ? CrateRejection.None : CrateRejection.NothingHeld;

        /// <summary>Put the held thing back in the crate, keeping its charges, quality or seeds.</summary>
        public CrateRejection Return(PlayerId player)
        {
            var r = CanReturn(player);
            if (r != CrateRejection.None) return r;
            var h = _held[player.Value];
            _held.Remove(player.Value);
            if (h.Kind == HeldKind.Container) _crate.AddContainer(h.Container); else _crate.AddThing(h);
            return CrateRejection.None;
        }
    }
}
