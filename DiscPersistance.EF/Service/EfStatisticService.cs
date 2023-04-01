using DisCache.Types.Model.Statistics;
using DiscPersistance.EF.Database;
using DiscPersistance.Types.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slot = DiscPersistance.EF.Database.Slot;
using StatisticItem = DiscPersistance.EF.Database.StatisticItem;

namespace DiscPersistance.EF.Service
{
    public class EfStatisticService : IDiscDatabasePersistance
    {

        public static string DbPath;


        public EfStatisticService(string dbPath)
        {
            DbPath = dbPath;

            try
            {
                using var db = new StatisticContext();
                db.Database.Migrate();
            }
            catch (Exception ex)
            {

            }
        }

        public async Task AddSlot(DisCache.Types.Model.Statistics.Slot slot)
        {
            await using var db = new Database.StatisticContext();

            var entry = await db.Slots.FirstOrDefaultAsync(s => s.Name == slot.Name);

            var scaledTime = GetScaledTime(slot.Scale, DateTime.Now);

            if (entry != null)
            {
                var lastStatEntry = await db.Items.OrderByDescending(s => s.Time).FirstOrDefaultAsync(s => s.SlotId == entry.Id);
                if (lastStatEntry != null)
                {
                    if (lastStatEntry.Time == scaledTime)
                    {
                        lastStatEntry.Count += 1;
                    }
                    else
                    {
                        db.Items.Add(new Database.StatisticItem()
                        {
                            Slot = entry,
                            SlotId = entry.Id,
                            Time = GetScaledTime(slot.Scale, DateTime.Now),
                            Count = 1
                        });
                    }
                }
            }
            else
            {
                var newSlot = new Slot() { Name = slot.Name, Scale = slot.Scale, Created = DateTime.Now, Duration = slot.Duration };
                newSlot.Items = new List<StatisticItem>();
                newSlot.Items.Add(new Database.StatisticItem()
                {
                    Time = GetScaledTime(slot.Scale, DateTime.Now),
                    Count = 1
                });
                db.Slots.Add(newSlot);

                await db.SaveChangesAsync();
            }

            await db.SaveChangesAsync();

        }

        public async Task<ICollection<ActionPerTime>> GetSlot(DisCache.Types.Model.Statistics.Slot slot)
        {
            return await GetSlot(
                slot,
                GetScaledTime(slot.Scale, DateTime.Now, slot.Duration),
                GetScaledTime(slot.Scale, DateTime.Now, 0));
        }

        public async Task<ICollection<ActionPerTime>> GetSlot(DisCache.Types.Model.Statistics.Slot slot, DateTime from, DateTime to)
        {
            await using var db = new Database.StatisticContext();

            // Get the slot with that name
            var dbSlot = await db.Slots.FirstOrDefaultAsync(s => s.Name == slot.Name && s.Scale == slot.Scale);

            if (dbSlot != default)
            {
                var items = db.Items.Where(s => s.SlotId == dbSlot.Id && s.Time >= from && s.Time <= to);

                var result = new LinkedList<ActionPerTime>();
                foreach (var item in items)
                {
                    result.AddLast(new ActionPerTime() { Count = item.Count, Time = item.Time });
                }

                return NormalizeList(slot.Scale, slot.Duration, result);
            }
            return GetEmptyList(slot.Scale, slot.Duration); ;
        }


        public LinkedList<ActionPerTime> NormalizeList(Scale scale, long duration, LinkedList<ActionPerTime> itemList)
        {
            LinkedList<ActionPerTime> result = new LinkedList<ActionPerTime>();
            // Kleineste Datum in der Liste
            var startDate = GetScaledTime(scale, DateTime.Now, duration);

            // Größtes Datum in der Liste.
            var endDate = GetScaledTime(scale, DateTime.Now, 0);

            // von hinten nach vorne.
            if (itemList.Count == 0)
            {
                return GetEmptyList(scale, duration);
            }

            if (itemList.First.Value.Time > startDate)
            {
                itemList.AddFirst(new ActionPerTime() { Count = 0, Time = startDate });
            }

            using var enumerator = itemList.Reverse().GetEnumerator();
            enumerator.MoveNext();
            var item = enumerator.Current;
            var scaledItemTime = GetScaledTime(scale, item.Time);

            long idx = duration;
            var topItem = endDate;
            while (idx > 0)
            {
                if (scaledItemTime < topItem)
                {
                    result.AddFirst(new ActionPerTime() { Count = 0, Time = topItem });
                    topItem = DecrementScaledTime(scale, topItem);
                }
                else
                {
                    result.AddFirst(item);
                    topItem = DecrementScaledTime(scale, topItem);
                    if (enumerator.MoveNext())
                    {
                        item = enumerator.Current;
                        scaledItemTime = GetScaledTime(scale, item.Time);
                    }
                    else
                    {
                        break;
                    }
                }

                idx--;
            }

            return result;
        }


        private LinkedList<ActionPerTime> GetEmptyList(Scale scale, long duration)
        {
            if (duration == 0)
                return new LinkedList<ActionPerTime>();

            var now = GetScaledTime(scale, DateTime.Now);
            var from = GetScaledTime(scale, DateTime.Now, duration);
            var result = new LinkedList<ActionPerTime>();
            while (duration > 0)
            {
                result.AddLast(new ActionPerTime()
                {
                    Time = from,
                    Count = 0
                });
                duration -= 1;
                from = GetScaledTime(scale, DateTime.Now, duration);
            }

            return result;
        }

        private DateTime GetScaledTime(Scale scale, DateTime dt, long duration = 0)
        {

            switch (scale)
            {
                case Scale.All:
                    return dt;

                case Scale.Millisecond:
                    return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond) - TimeSpan.FromMilliseconds(duration);

                case Scale.Second:
                    return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second) - TimeSpan.FromSeconds(duration);

                case Scale.Minute:
                    return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0) - TimeSpan.FromMinutes(duration);

                case Scale.Hour:
                    return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0) - TimeSpan.FromHours(duration);

                case Scale.Day:
                    return DateTime.Today.AddDays(duration * -1);

                case Scale.Month:
                    return DateTime.Today.AddMonths((int)duration * -1);

                case Scale.Year:
                    return DateTime.Today.AddYears((int)duration * -1);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private int ScaledTimeDuration(Scale scale, DateTime a, DateTime b)
        {
            var start = GetScaledTime(scale, a);
            var end = GetScaledTime(scale, b);

            switch (scale)
            {
                case Scale.All:
                    return (int)(end - start).TotalMilliseconds;
                case Scale.Millisecond:
                    return (int)(end -start).TotalMilliseconds;
                case Scale.Second:
                    return (int)(end - start).TotalSeconds;
                case Scale.Minute:
                    return (int)(end - start).TotalMinutes;
                case Scale.Hour:
                    return (int)(end - start).TotalHours;
                case Scale.Day:
                    return (int)(end - start).TotalDays;
                //case Scale.Month:
                //    return (int)(end - start).Total;
                //case Scale.Year:
                //    return (int)(end - start).TotalMilliseconds;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
            }
        }

        private DateTime DecrementScaledTime(Scale scale, DateTime t)
        {
            switch (scale)
            {
                case Scale.All:
                    return t.AddMilliseconds(-1);
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

        private DateTime IncrementScaledTime(Scale scale, DateTime t)
        {
            switch (scale)
            {
                case Scale.All:
                    return t.AddMilliseconds(1);
                case Scale.Millisecond:
                    return t.AddMilliseconds(1);
                case Scale.Second:
                    return t.AddSeconds(1);
                case Scale.Minute:
                    return t.AddMinutes(1);
                case Scale.Hour:
                    return t.AddHours(1);
                case Scale.Day:
                    return t.AddDays(1);
                case Scale.Month:
                    return t.AddMonths(1);
                case Scale.Year:
                    return t.AddYears(1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
            }
        }

        private string GetKey(string name, Scale scale)
        {
            return $"{name}_{scale}";
        }

    }
}
