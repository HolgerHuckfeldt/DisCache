using DisCache.Types.Model.Statistics;
using DiscPersistance.Types.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Disc.Statistics.Services
{

    public class StatisticSlot
    {
        private readonly IDiscPersistance _store;

        private readonly Scale _scale;
        private readonly long _duration;

        private readonly Slot _slotRequest;
        private readonly Slot _slotInsert;
        private readonly Slot _slotHit;
        private readonly Slot _slotMiss;
        private readonly Slot _slotFetcht;
        private readonly Slot _slotDelete;

        public StatisticSlot(Scale scale, long duration, IDiscPersistance store, bool isDefault = false)
        {
            _store = store;
            _scale = scale;
            _duration = duration;
            Default = isDefault;

            _slotRequest = new Slot() { Name = "Request", Scale = _scale, Duration = _duration };
            _slotInsert = new Slot() { Name = "Insert", Scale = _scale, Duration = _duration };
            _slotHit = new Slot() { Name = "Hit", Scale = _scale, Duration = _duration };
            _slotDelete = new Slot() { Name = "Delete", Scale = _scale, Duration = _duration };
            _slotMiss = new Slot() { Name = "Miss", Scale = _scale, Duration = _duration };
            _slotFetcht = new Slot() { Name = "Fetch", Scale = _scale, Duration = _duration };
        }

        public async Task StatisticEvent(StatisticsType type)
        {
            switch (type)
            {
                case StatisticsType.InsertsPerTime:
                    await Insert();
                    break;
                case StatisticsType.FetchesPerTime:
                    await Fetch();
                    break;
                case StatisticsType.DeletesPerTime:
                    await Delete();
                    break;
                case StatisticsType.HitsPerTime:
                    await Hit();
                    break;
                case StatisticsType.MissesPerTime:
                    await Miss();
                    break;
                case StatisticsType.RequestsPerTime:
                    await Request();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public bool Default { get; set; }

        public async Task Request()
        {
            await _store.AddSlot(_slotRequest);
        }

        public async Task Hit()
        {
            await _store.AddSlot(_slotHit);
        }

        public async Task Fetch()
        {
            await _store.AddSlot(_slotFetcht);
        }

        public async Task Delete()
        {
            await _store.AddSlot(_slotDelete);
        }

        public async Task Miss()
        {
            await _store.AddSlot(_slotMiss);
        }

        public async Task Insert()
        {
            await _store.AddSlot(_slotInsert);
        }

        public async Task<ICollection<ActionPerTime>> GetRequests()
        {
            return await _store.GetSlot(_slotRequest);
        }

        public async Task<ICollection<ActionPerTime>> GetRequests(DateTime from, DateTime to)
        {
            return await _store.GetSlot(_slotRequest, from, to);
        }

        public async Task<ICollection<ActionPerTime>> GetHits()
        {
            return await _store.GetSlot(_slotHit);
        }

        public async Task<ICollection<ActionPerTime>> GetHits(DateTime from, DateTime to)
        {
            return await _store.GetSlot(_slotHit, from, to);
        }

        public async Task<ICollection<ActionPerTime>> GetMiss()
        {
            return await _store.GetSlot(_slotMiss);
        }

        public async Task<ICollection<ActionPerTime>> GetMiss(DateTime from, DateTime to)
        {
            return await _store.GetSlot(_slotMiss, from, to);
        }

        public async Task<ICollection<ActionPerTime>> GetInserts()
        {
            return await _store.GetSlot(_slotInsert);
        }

        public async Task<ICollection<ActionPerTime>> GetInserts(DateTime from, DateTime to)
        {
            return await _store.GetSlot(_slotInsert, from, to);
        }

        public async Task<ICollection<ActionPerTime>> GetDeletes()
        {
            return await _store.GetSlot(_slotDelete);
        }

        public async Task<ICollection<ActionPerTime>> GetDeletes(DateTime from, DateTime to)
        {
            return await _store.GetSlot(_slotDelete, from, to);
        }

        private DateTime GetTimeScale()
        {
            var dt = DateTime.Now;

            switch (_scale)
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

    }
}
