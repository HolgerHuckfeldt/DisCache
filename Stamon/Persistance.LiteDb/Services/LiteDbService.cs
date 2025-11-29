using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistance.LiteDb.Models;
using Stamon.Types.Interfaces;
using Stamon.Types.Model;

namespace Persistance.LiteDb.Services
{
    internal class LiteDbService : IStamonDatabasePersistance, IDisposable
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<SlotDocument> _slots;
        private readonly ILiteCollection<ItemDocument> _items;
        private readonly ILogger<LiteDbService> _logger;
        private readonly ConcurrentDictionary<string, SlotDocument> _slotCache = new ConcurrentDictionary<string, SlotDocument>();
        private readonly object _lock = new object();

        public LiteDbService(IOptions<LiteDbOptions> options, ILogger<LiteDbService> logger)
        {
            _logger = logger;
            _db = new LiteDatabase(options.Value.ConnectionString);
            _slots = _db.GetCollection<SlotDocument>("slots");
            _items = _db.GetCollection<ItemDocument>("items");

            _slots.EnsureIndex(x => x.Id, true);
            _slots.EnsureIndex(x => x.Name);
            _slots.EnsureIndex(x => x.Scale);

            _items.EnsureIndex(x => x.Id, true);
            _items.EnsureIndex(x => x.SlotId);
            _items.EnsureIndex(x => x.Time);
        }

        public Task AddMonitor(Monitor monitor)
        {
            try
            {
                lock (_lock)
                {
                    var slot = GetOrCreateSlot(monitor);
                    var bucketTime = GetTimeScale(monitor.Scale);
                    var itemId = $"{slot.Id}|{bucketTime.Ticks}";

                    var item = _items.FindById(itemId);
                    if (item == null)
                    {
                        _items.Insert(new ItemDocument
                        {
                            Id = itemId,
                            SlotId = slot.Id,
                            Time = bucketTime,
                            Count = 1
                        });
                    }
                    else
                    {
                        item.Count++;
                        _items.Update(item);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding monitor {MonitorName}", monitor.Name);
            }

            return Task.CompletedTask;
        }

        public Task<ICollection<ActionPerTime>> GetMonitor(Monitor monitor)
        {
            var from = GetScaledTime(monitor.Scale, DateTime.Now, monitor.Duration);
            var to = GetScaledTime(monitor.Scale, DateTime.Now, 0);
            return GetMonitor(monitor, from, to);
        }

        public Task<ICollection<ActionPerTime>> GetMonitor(Monitor monitor, DateTime from, DateTime to)
        {
            var slot = GetSlot(monitor);
            if (slot == null)
            {
                return Task.FromResult<ICollection<ActionPerTime>>(new List<ActionPerTime>());
            }

            var items = _items.Find(i =>
                    i.SlotId == slot.Id &&
                    i.Time >= from &&
                    i.Time <= to)
                .OrderBy(i => i.Time)
                .Select(i => new ActionPerTime { Count = i.Count, Time = i.Time })
                .ToList();

            var normalized = NormalizeList(monitor.Scale, monitor.Duration, items);
            return Task.FromResult<ICollection<ActionPerTime>>(normalized);
        }

        public Task<ICollection<string>> GetMonitorNames()
        {
            var names = _slots.FindAll().Select(s => s.Name).Distinct().ToList();
            return Task.FromResult<ICollection<string>>(names);
        }

        private SlotDocument GetOrCreateSlot(Monitor monitor)
        {
            var key = $"{monitor.Name}|{monitor.Scale}";
            if (_slotCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var slot = _slots.FindById(key);
            if (slot == null)
            {
                slot = new SlotDocument
                {
                    Id = key,
                    Name = monitor.Name,
                    Scale = monitor.Scale,
                    Duration = monitor.Duration
                };
                _slots.Insert(slot);
            }

            _slotCache[key] = slot;
            return slot;
        }

        private SlotDocument? GetSlot(Monitor monitor)
        {
            var key = $"{monitor.Name}|{monitor.Scale}";
            if (_slotCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var slot = _slots.FindById(key);
            if (slot != null)
            {
                _slotCache[key] = slot;
            }

            return slot;
        }

        private static DateTime GetTimeScale(Scale scale)
        {
            var dt = DateTime.Now;

            switch (scale)
            {
                case Scale.All:
                    return dt;
                case Scale.Millisecond:
                    return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
                case Scale.Second:
                    return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
                case Scale.Minute:
                    return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
                case Scale.Hour:
                    return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
                case Scale.Day:
                    return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
                case Scale.Month:
                    return new DateTime(dt.Year, dt.Month, 1, 0, 0, 0);
                case Scale.Year:
                    return new DateTime(dt.Year, 1, 1, 0, 0, 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
            }
        }

        private static DateTime GetScaledTime(Scale scale, DateTime dt, long duration = 0)
        {
            switch (scale)
            {
                case Scale.All:
                    return dt;
                case Scale.Millisecond:
                    return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond)
                           - TimeSpan.FromMilliseconds(duration);
                case Scale.Second:
                    return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second)
                           - TimeSpan.FromSeconds(duration);
                case Scale.Minute:
                    return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0)
                           - TimeSpan.FromMinutes(duration);
                case Scale.Hour:
                    return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0)
                           - TimeSpan.FromHours(duration);
                case Scale.Day:
                    return DateTime.Today.AddDays(duration * -1);
                case Scale.Month:
                    return DateTime.Today.AddMonths((int)duration * -1);
                case Scale.Year:
                    return DateTime.Today.AddYears((int)duration * -1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
            }
        }

        private static LinkedList<ActionPerTime> NormalizeList(Scale scale, long duration, IList<ActionPerTime> items)
        {
            var result = new LinkedList<ActionPerTime>();
            if (duration == 0)
            {
                foreach (var item in items)
                {
                    result.AddLast(item);
                }
                return result;
            }

            var endDate = GetScaledTime(scale, DateTime.Now);
            var startDate = GetScaledTime(scale, DateTime.Now, duration);

            if (!items.Any() || items.First().Time > startDate)
            {
                items = items.Prepend(new ActionPerTime { Count = 0, Time = startDate }).ToList();
            }

            var idx = duration;
            var currentItemIndex = items.Count - 1;
            var currentTop = endDate;

            while (idx > 0)
            {
                ActionPerTime? current = currentItemIndex >= 0 ? items[currentItemIndex] : null;
                if (current != null)
                {
                    var scaledItemTime = GetScaledTime(scale, current.Time);
                    if (scaledItemTime < currentTop)
                    {
                        result.AddFirst(new ActionPerTime { Count = 0, Time = currentTop });
                        currentTop = DecrementScaledTime(scale, currentTop);
                    }
                    else
                    {
                        result.AddFirst(current);
                        currentTop = DecrementScaledTime(scale, currentTop);
                        currentItemIndex--;
                    }
                }
                else
                {
                    result.AddFirst(new ActionPerTime { Count = 0, Time = currentTop });
                    currentTop = DecrementScaledTime(scale, currentTop);
                }

                idx--;
            }

            return result;
        }

        private static DateTime DecrementScaledTime(Scale scale, DateTime t)
        {
            switch (scale)
            {
                case Scale.All:
                case Scale.Millisecond:
                    return t.AddMilliseconds(-1);
                case Scale.Second:
                    return t.AddSeconds(-1);
                case Scale.Minute:
                    return t.AddMinutes(-1);
                case Scale.Hour:
                    return t.AddHours(-1);
                case Scale.Day:
                    return t.AddDays(-1);
                case Scale.Month:
                    return t.AddMonths(-1);
                case Scale.Year:
                    return t.AddYears(-1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
            }
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
