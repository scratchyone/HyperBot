using Microsoft.EntityFrameworkCore;
using HyperBot.Models;

namespace HyperBot.Data
{
    public class DataContext : DbContext
    {
        public DataContext()
        {
        }
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=data.db3");
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Prefix>(entity =>
                {
                    entity.HasKey(e => new { e.PrefixText, e.Guild });
                });
        }
        public DbSet<PinboardItem> PinboardItems { get; set; }
        public DbSet<Prefix> Prefixes { get; set; }
        public DbSet<PagerItem> Pagers { get; set; }
    }
}