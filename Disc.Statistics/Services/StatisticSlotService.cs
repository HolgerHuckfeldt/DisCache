using Disc.Statistics.Model;
using DisCache.Types.Model.Statistics;
using DiscPersistance.Types.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Disc.Statistics.Services
{
    public class StatisticSlotService : IStatisticService
    {
        private readonly ILogger<StatisticSlotService> _logger;
        private readonly IDiscPersistance _storeDatabase;
        private readonly IDiscMemoryPersistance _storeMemory;
        private readonly IOptions<StatisticConfig> _slotConfig;

        private readonly Dictionary<string, StatisticSlot> _slots = new Dictionary<string, StatisticSlot>();

        public StatisticSlotService(
            IDiscDatabasePersistance storeDatabase,
            IDiscMemoryPersistance storeMemory,
            IOptions<StatisticConfig> slotConfig,
            ILogger<StatisticSlotService> logger)
        {
            _slotConfig = slotConfig;
            _storeDatabase = storeDatabase;
            _storeMemory = storeMemory;
            _logger = logger;

            if (_slotConfig.Value is { Provider: { } } && _slotConfig.Value.Provider.Any())
            {
                foreach (var config in _slotConfig.Value.Provider)
                {
                    var newSlot = GetSlot(config);
                    newSlot.Default = config.Default;
                    _slots.Add(config.Name, newSlot);
                }
            }
            else
            {
                _slots.Add("default", new StatisticSlot(Scale.Second, 120, _storeMemory));
            }
        }

        public async Task StatisticEvent(StatisticsType type)
        {
            try
            {
                switch (type)
                {
                    case StatisticsType.InsertsPerTime:
                        foreach (var statisticSlot in _slots)
                        {
                            await statisticSlot.Value.Insert();
                        }

                        break;
                    case StatisticsType.FetchesPerTime:
                        foreach (var statisticSlot in _slots)
                        {
                            await statisticSlot.Value.Fetch();
                        }

                        break;
                    case StatisticsType.DeletesPerTime:
                        foreach (var statisticSlot in _slots)
                        {
                            await statisticSlot.Value.Delete();
                        }

                        break;
                    case StatisticsType.HitsPerTime:
                        foreach (var statisticSlot in _slots)
                        {
                            await statisticSlot.Value.Hit();
                        }

                        break;
                    case StatisticsType.MissesPerTime:
                        foreach (var statisticSlot in _slots)
                        {
                            await statisticSlot.Value.Miss();
                        }

                        break;
                    case StatisticsType.RequestsPerTime:
                        foreach (var statisticSlot in _slots)
                        {
                            await statisticSlot.Value.Request();
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in StatisticEvent", ex);
            }
        }

        public Task<List<string>> GetSlotNames()
        {
            return Task.FromResult(_slots.Keys.ToList());
        }

        public async Task<ICollection<ActionPerTime>> GetRequests(string slotName)
        {
            return await GetSlot(slotName).GetRequests();
        }

        public async Task<ICollection<ActionPerTime>> GetRequests(string slotName, DateTime from, DateTime to)
        {
            return await GetSlot(slotName).GetRequests(from, to);
        }

        public async Task<ICollection<ActionPerTime>> GetHits(string slotName)
        {
            return await GetSlot(slotName).GetHits();
        }

        public async Task<ICollection<ActionPerTime>> GetHits(string slotName, DateTime from, DateTime to)
        {
            return await GetSlot(slotName).GetHits(from, to);
        }

        public async Task<ICollection<ActionPerTime>> GetMiss(string slotName)
        {
            return await GetSlot(slotName).GetMiss();
        }

        public async Task<ICollection<ActionPerTime>> GetMiss(string slotName, DateTime from, DateTime to)
        {
            return await GetSlot(slotName).GetMiss(from, to);
        }

        public async Task<ICollection<ActionPerTime>> GetInserts(string slotName)
        {
            return await GetSlot(slotName).GetInserts();
        }

        public async Task<ICollection<ActionPerTime>> GetInserts(string slotName, DateTime from, DateTime to)
        {
            return await GetSlot(slotName).GetInserts(from, to);
        }

        private StatisticSlot GetSlot(SlotConfig sc)
        {
            if (sc.Scale == null)
            {
                throw new Exception("Scale may not be null");
            }

            IDiscPersistance store;

            switch (sc.Type)
            {
                case "EF":
                    store = _storeDatabase;
                    break;
                case "Memory":
                    store = _storeMemory;
                    break;
                default:
                    store = _storeMemory;
                    break;
            }

            return new StatisticSlot(GetScale(sc.Scale), sc.Scale.Duration, store);
        }

        private StatisticSlot GetSlot(string slotName)
        {
            StatisticSlot? slot = null;

            if (slotName == "default")
                slot = GetDefaultSlot();
            else
            {
                if (!_slots.TryGetValue(slotName, out slot))
                {
                    throw new ArgumentException($"Unknown slot {slotName}");
                }
            }

            return slot;
        }

        private Scale GetScale(ScaleConfig sc)
        {
            switch (sc.Scale)
            {
                case "Second":
                    return Scale.Second;
                case "Millisecond":
                    return Scale.Millisecond;
                case "Minute":
                    return Scale.Minute;
                case "Day":
                    return Scale.Day;
                case "All":
                    return Scale.All;
                case "Month":
                    return Scale.Month;
                case "Year":
                    return Scale.Year;
                case "Hour":
                    return Scale.Hour;
                default:
                    return Scale.Minute;
            }
        }

        private StatisticSlot GetDefaultSlot()
        {
            var s = _slots.FirstOrDefault(s => s.Value.Default);
            return s.Value;
        }

    }
}
