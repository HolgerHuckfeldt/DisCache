using DisCache.Types.Model.Statistics;
using DiscPersistance.EF.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscPersistance.EF.Database
{
    public class StatisticContext : DbContext
    {
        public DbSet<StatisticItem> Items { get; set; }
        public DbSet<Slot> Slots { get; set; }

        public string DbPath { get; }

        public StatisticContext()
        {
            //var folder = Environment.SpecialFolder.LocalApplicationData;
            //var path = Environment.GetFolderPath(folder);
            DbPath = EfStatisticService.DbPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StatisticItem>(entity =>
            {
                entity.HasIndex(b => b.Time);
            });

            modelBuilder.Entity<Slot>(
                entity =>
                {
                    entity.HasIndex(b => b.Name);
                    entity.Property(e => e.Name).HasMaxLength(255);
                    entity.HasIndex(b => b.Scale);
                });
        }
    }

    public class StatisticItem
    {
        public int Id { get; set; }

        public long Count { get; set; }

        public string? Information { get; set; }

        public DateTime Time { get; set; }

        public int SlotId { get; set; }

        public Slot? Slot { get; set; }
    }

    public class Slot
    {
        public int Id { get; set; }

        public string Name { get; set; } = String.Empty;

        public long Duration { get; set; }

        public DateTime Created { get; set; }

        public Scale Scale { get; set; }

        public ICollection<StatisticItem> Items { get; set; } = new List<StatisticItem>();
    }
}
