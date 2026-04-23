using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stamon.Model;
using Stamon.Types.Interfaces;
using Stamon.Types.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Timers;

namespace Stamon.Services
{
    public class StamonService : IDisposable
    {
        private readonly ILogger<StamonService> _logger;
        private readonly IEnumerable<IStamonDatabasePersistance> _storeDatabases;
        private readonly IStamonMemoryPersistance _storeMemory;
        private readonly IOptions<StarisConfig> _slotConfig;
        private readonly Dictionary<string, StamonSlot> _slots = new();
        private readonly string _forwardTo;

        private readonly ConcurrentQueue<string> _forwards = new ConcurrentQueue<string>();
        //private System.Timers.Timer forwardTimer;

        private object _lock = new object();

        public StamonService(
            IEnumerable<IStamonDatabasePersistance> storeDatabases,
            IStamonMemoryPersistance storeMemory,
            IOptions<StarisConfig> slotConfig,
            ILogger<StamonService> logger)
        {
            _slotConfig = slotConfig;
            _storeDatabases = storeDatabases;
            _storeMemory = storeMemory;
            _logger = logger;

            _forwardTo = slotConfig.Value.Forward;
            var doForward = slotConfig.Value.DoForward;

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
                _slots.Add("default", new StamonSlot(Scale.Second, 120, _storeMemory));
            }

            if (doForward)
            {
                //forwardTimer = new System.Timers.Timer();
                //forwardTimer.Interval = 2000;
                //forwardTimer.Elapsed += ForwardTimer_Elapsed;
            }
        }

        public async Task StatisticEvent(string name)
        {
            try
            {
                foreach (var stamonSlot in _slots.Values)
                {
                    await stamonSlot.StatisticEvent(name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StatisticEvent");
            }
        }

        public async Task<ICollection<ActionPerTime>> GetSlotDatas(string slotName, string eventName)
        {
            return await GetSlot(slotName).Get(eventName);
        }

        public Task<List<string>> GetSlotNames()
        {
            return Task.FromResult(_slots.Keys.ToList());
        }

        public Task<List<string>> GetEventNames()
        {
            var result = new List<string>();
            foreach (var stamonSlot in _slots.Values)
            {
                result.AddRange(stamonSlot.GetMonitorNames());
            }

            return Task.FromResult(result.Distinct().ToList());
        }

        private StamonSlot GetSlot(SlotConfig sc)
        {
            if (sc.Scale == null)
            {
                throw new Exception("Scale may not be null");
            }

            IStamonPersistance store;

            if (string.Equals(sc.Type, "Memory", StringComparison.OrdinalIgnoreCase))
            {
                store = _storeMemory;
            }
            else
            {
                store = ResolveDatabase(sc.Type) ?? (IStamonPersistance)_storeMemory;
            }

            return new StamonSlot(GetScale(sc.Scale), sc.Scale.Duration, store);
        }

        private StamonSlot GetSlot(string slotName)
        {
            StamonSlot? slot = null;

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

        private StamonSlot GetDefaultSlot()
        {
            var s = _slots.FirstOrDefault(s => s.Value.Default);
            return s.Value;
        }

        private IStamonDatabasePersistance? ResolveDatabase(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return _storeDatabases.FirstOrDefault();
            }

            return _storeDatabases.FirstOrDefault(p =>
                       string.Equals(p.ProviderType, type, StringComparison.OrdinalIgnoreCase))
                   ?? _storeDatabases.FirstOrDefault();
        }

        private void ForwardTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                var client = new HttpClient();

                List<string>? toSend = null;

                lock (_lock)
                {
                    toSend = _forwards.ToList();
                    _forwards.Clear();
                }

                if (toSend != null)
                    client.PostAsJsonAsync($"{_forwardTo}/api/", toSend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ForwardTimer_Elapsed");
            }
        }

        public void Dispose()
        {
            //forwardTimer?.Dispose();
        }
    }
}
