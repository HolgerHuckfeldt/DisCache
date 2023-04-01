using System;

namespace DisCache.Model
{
    public class StatisticRequest
    {
        public string SlotName { get; set; }

        public DateTime From { get; set; }

        public DateTime To { get; set; }
    }
}
