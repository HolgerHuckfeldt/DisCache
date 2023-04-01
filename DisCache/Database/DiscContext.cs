using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace DisCache.Database
{

    public class DiscContext : DbContext
    {
        public DbSet<KeyException> KeyExceptions { get; set; }

        public string DbPath { get; }

        public DiscContext()
        {
            DbPath = Configuration.DbPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KeyException>(
                entity =>
                {
                    entity.HasIndex(b => b.ActiveFrom);
                    entity.HasIndex(b => b.ActiveTo);
                    entity.HasIndex(i => i.IsActive);
                });
        }
    }

    public class KeyException
    {
        public int Id { get; set; }

        public DateTime? ActiveFrom { get; set; }

        public DateTime? ActiveTo { get; set; }

        public bool IsActive { get; set; }

        public string Key { get; set; }

        public int Action { get; set; }

        public string Detail { get; set; }
    }

    public enum KeyAction
    {
        None = 0,
        Ignore = 1,
        Rewrite = 2
    }


}
