using DisCache.Types.Model.Statistics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscPersistance.Types.Interfaces
{
    public interface IDiscPersistance
    {
        Task AddSlot(Slot slot);
        Task<ICollection<ActionPerTime>> GetSlot(Slot slot);

        Task<ICollection<ActionPerTime>> GetSlot(Slot slot, DateTime from, DateTime to);
    }

    public interface IDiscMemoryPersistance : IDiscPersistance
    {

    }

    public interface IDiscDatabasePersistance : IDiscPersistance
    {

    }
}
