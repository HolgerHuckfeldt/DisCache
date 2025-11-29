using Stamon.Types.Model;

namespace Persistance.LiteDb.Models
{
    internal class SlotDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Scale Scale { get; set; }
        public long Duration { get; set; }
    }
}
