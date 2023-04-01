using DisCache.Types.Model.Statistics;
using DiscPersistance.Types.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Disc.Statistics.Services
{
    public class StatisticService : IStatisticService
    {
        private readonly ILogger<StatisticService> _logger;

        private readonly LinkedList<ActionPerTime> _requestPerMinute = new LinkedList<ActionPerTime>();
        private readonly LinkedList<ActionPerTime> _hitPerMinute = new LinkedList<ActionPerTime>();
        private readonly LinkedList<ActionPerTime> _deletePerMinute = new LinkedList<ActionPerTime>();
        private readonly LinkedList<ActionPerTime> _missesPerMinute = new LinkedList<ActionPerTime>();
        private readonly LinkedList<ActionPerTime> _insertPerMinute = new LinkedList<ActionPerTime>();
        private readonly LinkedList<ActionPerTime> _fetchPerMinute = new LinkedList<ActionPerTime>();

        private int _minutes = 15;

        public StatisticService(ILogger<StatisticService> logger)
        {
            _logger = logger;

            _requestPerMinute.AddLast(new ActionPerTime() { Time = GetMinuteTime(), Count = 0 });
            _hitPerMinute.AddLast(new ActionPerTime() { Time = GetMinuteTime(), Count = 0 });
            _missesPerMinute.AddLast(new ActionPerTime() { Time = GetMinuteTime(), Count = 0 });
            _insertPerMinute.AddLast(new ActionPerTime() { Time = GetMinuteTime(), Count = 0 });
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
                    RequestPerSecond();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            
        }

        public void RequestPerSecond()
        {
            var statDateTime = GetMinuteTime();
            CheckStat(_requestPerMinute);
            if (_requestPerMinute.Last != null && _requestPerMinute.Last.Value.Time == statDateTime)
            {
                _requestPerMinute.Last.Value.Count++;
            }
        }

        private Task Hit()
        {
            var statDateTime = GetMinuteTime();
            CheckStat(_hitPerMinute);
            if (_hitPerMinute.Last != null && _hitPerMinute.Last.Value.Time == statDateTime)
            {
                _hitPerMinute.Last.Value.Count++;
            }
            return Task.CompletedTask;
        }

        private Task Fetch()
        {
            var statDateTime = GetMinuteTime();
            CheckStat(_fetchPerMinute);
            if (_fetchPerMinute.Last != null && _fetchPerMinute.Last.Value.Time == statDateTime)
            {
                _fetchPerMinute.Last.Value.Count++;
            }
            return Task.CompletedTask;
        }

        private Task Delete()
        {
            var statDateTime = GetMinuteTime();
            CheckStat(_deletePerMinute);
            if (_deletePerMinute.Last != null && _deletePerMinute.Last.Value.Time == statDateTime)
            {
                _deletePerMinute.Last.Value.Count++;
            }
            return Task.CompletedTask;
        }

        private Task Miss()
        {
            var statDateTime = GetMinuteTime();
            CheckStat(_missesPerMinute);
            if (_missesPerMinute.Last != null && _missesPerMinute.Last.Value.Time == statDateTime)
            {
                _missesPerMinute.Last.Value.Count++;
            }
            return Task.CompletedTask;
        }

        private Task Insert()
        {
            var statDateTime = GetMinuteTime();
            CheckStat(_insertPerMinute);
            if (_insertPerMinute.Last != null && _insertPerMinute.Last.Value.Time == statDateTime)
            {
                _insertPerMinute.Last.Value.Count++;
            }
            return Task.CompletedTask;
        }

        public Task<ICollection<ActionPerTime>> GetRequests(string slotName)
        {
            CheckStat(_requestPerMinute);
            return Task.FromResult(_requestPerMinute.ToList() as ICollection<ActionPerTime>);
        }

        public Task<ICollection<ActionPerTime>> GetRequests(string slotName, DateTime from, DateTime to)
        {
            CheckStat(_requestPerMinute);
            return Task.FromResult(_requestPerMinute.ToList() as ICollection<ActionPerTime>);
        }

        public Task<ICollection<ActionPerTime>> GetHits(string slotName)
        {
            CheckStat(_hitPerMinute);
            return Task.FromResult(_hitPerMinute.ToList() as ICollection<ActionPerTime>);
        }

        public Task<ICollection<ActionPerTime>> GetHits(string slotName, DateTime from, DateTime to)
        {
            CheckStat(_hitPerMinute);
            return Task.FromResult(_hitPerMinute.ToList() as ICollection<ActionPerTime>);
        }

        public Task<ICollection<ActionPerTime>> GetMiss(string slotName)
        {
            CheckStat(_missesPerMinute);
            return Task.FromResult(_missesPerMinute.ToList() as ICollection<ActionPerTime>);
        }

        public Task<ICollection<ActionPerTime>> GetMiss(string slotName, DateTime from, DateTime to)
        {
            CheckStat(_missesPerMinute);
            return Task.FromResult(_missesPerMinute.ToList() as ICollection<ActionPerTime>);
        }

        public Task<ICollection<ActionPerTime>> GetInserts(string slotName)
        {
            CheckStat(_insertPerMinute);
            return Task.FromResult(_insertPerMinute.ToList() as ICollection<ActionPerTime>);
        }

        public Task<ICollection<ActionPerTime>> GetInserts(string slotName, DateTime from, DateTime to)
        {
            CheckStat(_insertPerMinute);
            return Task.FromResult(_insertPerMinute.ToList() as ICollection<ActionPerTime>);
        }

        public Task<List<string>> GetSlotNames()
        {
            return Task.FromResult(new List<string>() { "default" });
        }

        private DateTime GetMinuteTime()
        {
            var dt = DateTime.Now;
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
        }

        private DateTime GetSecondTime()
        {
            var dt = DateTime.Now;
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
        }

        private void CheckStat(LinkedList<ActionPerTime> stat)
        {
            var statTimeNow = GetMinuteTime();

            while (stat.Last != null && stat.Last.Value.Time < statTimeNow)
            {
                var t = stat.Last.Value.Time;
                stat.AddLast(new ActionPerTime() { Count = 0, Time = t.AddMinutes(1) });
                if(stat.Count > _minutes)
                    stat.RemoveFirst();
            }
        }

    }
}
