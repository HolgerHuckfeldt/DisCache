using DisCache.Types.Model.Statistics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscPersistance.Types.Interfaces
{
    public interface IStatisticService
    {
        Task StatisticEvent(StatisticsType type);

        Task<ICollection<ActionPerTime>> GetRequests(string slotName);
        Task<ICollection<ActionPerTime>> GetRequests(string slotName, DateTime from, DateTime to);

        Task<ICollection<ActionPerTime>> GetHits(string slotName);
        Task<ICollection<ActionPerTime>> GetHits(string slotName, DateTime from, DateTime to);

        Task<ICollection<ActionPerTime>> GetMiss(string slotName);
        Task<ICollection<ActionPerTime>> GetMiss(string slotName, DateTime from, DateTime to);

        Task<ICollection<ActionPerTime>> GetInserts(string slotName);
        Task<ICollection<ActionPerTime>> GetInserts(string slotName, DateTime from, DateTime to);

        Task<List<string>> GetSlotNames();
    }
}
