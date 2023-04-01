using Stamon.Types.Model;
using System.Collections.Generic;

namespace DisCache.Model
{
    public class StatisticResult
    {
        public ICollection<ActionPerTime> Requetsts { get; set; }
        public ICollection<ActionPerTime> Hits { get; set; }
        public ICollection<ActionPerTime> Misses { get; set; }
        public ICollection<ActionPerTime> Inserts { get; set; }
    }
}
