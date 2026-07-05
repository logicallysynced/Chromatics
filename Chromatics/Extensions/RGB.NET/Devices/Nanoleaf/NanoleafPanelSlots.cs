using System;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Extensions.RGB.NET.Devices.Nanoleaf
{
    // Resolves the persisted panel slot table against the panels the
    // controller reports right now. Slot n maps to LedId.Custom(n+1), and
    // layer assignments reference LedIds, so slots must stay stable across
    // physical changes to the wall:
    // - First adoption (no stored order): slots are the live panelIds in
    //   ascending order.
    // - A stored panelId KEEPS its slot while absent (tombstone), so
    //   removing a panel never shifts the assignments of the panels after
    //   it, and a re-attached panel lands back on its old slot.
    // - New panelIds append to the end in ascending order.
    public static class NanoleafPanelSlots
    {
        public static (IReadOnlyList<int> Slots, IReadOnlyList<int> Added, IReadOnlyList<int> Missing)
            Resolve(IReadOnlyList<int> storedOrder, IReadOnlyCollection<int> livePanelIds)
        {
            var slots = new List<int>(storedOrder ?? Array.Empty<int>());
            var known = new HashSet<int>(slots);
            var live = livePanelIds as ISet<int> ?? new HashSet<int>(livePanelIds);

            var added = live.Where(id => !known.Contains(id)).OrderBy(id => id).ToList();
            slots.AddRange(added);

            var missing = slots.Where(id => !live.Contains(id)).ToList();

            return (slots, added, missing);
        }
    }
}
