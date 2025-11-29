using System;

namespace Persistance.LiteDb.Models
{
    internal class ItemDocument
    {
        public string Id { get; set; } = string.Empty;
        public string SlotId { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public long Count { get; set; }
    }
}
