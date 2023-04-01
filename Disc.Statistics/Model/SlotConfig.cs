using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace Disc.Statistics.Model
{
    public class StatisticConfig
    {
        public List<SlotConfig>? Provider { get; set; }
    }

    public class SlotConfig
    {
        public string Name { get; set; } = "";
        public bool Default { get; set; }
        public string Type { get; set; }
        public ScaleConfig? Scale { get; set; }
    }

    public class ScaleConfig
    {
        public int Duration { get; set; }
        public string Scale { get; set; } = "";
    }
}
