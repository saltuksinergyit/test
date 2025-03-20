using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FTMPlus.Common.Db
{
    public class ListenerContext : DbContext
    {
        public DbSet<FileProcessingLock> FileProcessingLocks { get; set; }

        public ListenerContext(DbContextOptions<ListenerContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            //modelBuilder.Entity<FileProcessingLock>().Property(l => l.LockKey).UseIdentityColumn();
            //modelBuilder.Entity<FileProcessingLock>()
            //    .HasIndex(l => l.LockKey)
            //    .IsUnique(); // LockKey için UNIQUE index
        }
    }
}

[Table("FileProcessingLock", Schema ="dbo")]
public class FileProcessingLock
{
    [Column("Id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    [Key()]
    public string LockKey { get; set; } = null!;
    public string LockValue { get; set; } = null!;
    [Column("CreateDate")]
    public DateTime CreateDate { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
}

