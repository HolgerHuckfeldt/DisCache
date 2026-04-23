using Stamon.Types.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Stamon.Types.Interfaces
{
    public interface IStamonPersistance
    {
        /// <summary>
        /// Identifier used to select the provider from configuration (e.g. Memory, EF, LiteDB).
        /// </summary>
        string ProviderType { get; }

        Task AddMonitor(Monitor monitor);
        Task<ICollection<ActionPerTime>> GetMonitor(Monitor monitor);

        Task<ICollection<ActionPerTime>> GetMonitor(Monitor Monitor, DateTime from, DateTime to);

        Task<ICollection<string>> GetMonitorNames();
    }

    public interface IStamonMemoryPersistance : IStamonPersistance
    {

    }

    public interface IStamonDatabasePersistance : IStamonPersistance
    {

    }
}
