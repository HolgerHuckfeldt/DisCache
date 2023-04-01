using DisCache.Types.Model.Statistics;
using DiscPersistance.Types.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscPersistance.Memory.Services
{
    public class PersistanceService : IDiscMemoryPersistance
    {
        private readonly ConcurrentDictionary<string, LinkedList<ActionPerTime>> _entries = new ConcurrentDictionary<string, LinkedList<ActionPerTime>>();

        /// <summary>
        /// Add a slot
        /// </summary>
        /// <param name="name">Name of the solt</param>
        /// <param name="scale">Scale, day, hour and so on.</param>
        /// <param name="duration">Duration, the unit is the scale.</param>
        /// <returns></returns>
        public Task AddSlot(DisCache.Types.Model.Statistics.Slot slot)
        {
            var statDateTime = GetTimeScale(slot.Scale);
            var key = GetKey(slot.Name, slot.Scale);

            if (_entries.TryGetValue(key, out var items))
            {
                CheckStat(items, slot.Scale, slot.Duration);
                if (items.Last != null && items.Last.Value.Time == statDateTime)
                {
                    items.Last.Value.Count++;
                }
            }
            else
            {
                var list = new LinkedList<ActionPerTime>();
                var newElement = new ActionPerTime() { Count = 1, Time = statDateTime };
                list.AddLast(newElement);

                CheckStat(list, slot.Scale, slot.Duration);
                list.Last.Value.Count++;
                _entries.TryAdd(key, list);
            }

            return Task.CompletedTask;
        }

        public Task<ICollection<ActionPerTime>> GetSlot(Slot slot)
        {
            var key = GetKey(slot.Name, slot.Scale);
            
            if (_entries.TryGetValue(key, out var items))
            {
                CheckStat(items,slot.Scale, slot.Duration);
                return Task.FromResult(items as ICollection<ActionPerTime>);
            }

            return Task.FromResult(new List<ActionPerTime>() as ICollection<ActionPerTime>);
        }

        public Task<ICollection<ActionPerTime>> GetSlot(Slot slot, DateTime from, DateTime to)
        {
            var key = GetKey(slot.Name, slot.Scale);

            if (_entries.TryGetValue(key, out var items))
            {
                CheckStat(items, slot.Scale, slot.Duration);
                return Task.FromResult(items as ICollection<ActionPerTime>);
            }

            return Task.FromResult(new List<ActionPerTime>() as ICollection<ActionPerTime>);
        }

        private void CheckStat(LinkedList<ActionPerTime> stat, Scale scale, long duration)
        {
            var statTimeNow = GetTimeScale(scale);

            while (stat.Last != null && stat.Last.Value.Time < statTimeNow)
            {
                var t = stat.Last.Value.Time;
                switch (scale)
                {
                    case Scale.All:
                        stat.AddLast(new ActionPerTime() { Count = 0, Time = DateTime.Now });
                        break;
                    case Scale.Millisecond:
                        stat.AddLast(new ActionPerTime() { Count = 0, Time = t.AddMilliseconds(1) });
                        break;
                    case Scale.Second:
                        stat.AddLast(new ActionPerTime() { Count = 0, Time = t.AddSeconds(1) });
                        break;
                    case Scale.Minute:
                        stat.AddLast(new ActionPerTime() { Count = 0, Time = t.AddMinutes(1) });
                        break;
                    case Scale.Hour:
                        stat.AddLast(new ActionPerTime() { Count = 0, Time = t.AddHours(1) });
                        break;
                    case Scale.Day:
                        stat.AddLast(new ActionPerTime() { Count = 0, Time = t.AddDays(1) });
                        break;
                    case Scale.Month:
                        stat.AddLast(new ActionPerTime() { Count = 0, Time = t.AddMonths(1) });
                        break;
                    case Scale.Year:
                        stat.AddLast(new ActionPerTime() { Count = 0, Time = t.AddYears(1) });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (stat.Count > duration)
                    stat.RemoveFirst();
            }
        }

        private DateTime GetTimeScale(Scale scale)
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
                    return new DateTime(dt.Year, dt.Month, 0, 0, 0, 0);

                case Scale.Year:
                    return new DateTime(dt.Year, 0, 0, 0, 0, 0);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string GetKey(string name, Scale scale)
        {
            return $"{name}_{scale}";
        }

    }
}
