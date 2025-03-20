using Microsoft.EntityFrameworkCore;
using System;

namespace FTMPlus.Common.Db
{
    public class TransactionContext : DbContext
    {
        public DbSet<FileProcessingLock> FileProcessingLocks { get; set; }

        public TransactionContext(DbContextOptions<TransactionContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileProcessingLock>()
                .HasIndex(l => l.LockKey)
                .IsUnique(); // LockKey için UNIQUE index
        }
    }
} 