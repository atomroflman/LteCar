using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace LteCar.Server.Data
{
    public class LteCarContext : DbContext
    {
        public LteCarContext(DbContextOptions<LteCarContext> options) : base(options) { }

        public DbSet<Car> Cars { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<CarChannel> CarChannels { get; set; }
        public DbSet<CarTelemetry> CarTelemetry { get; set; }
        public DbSet<UserCarSetup> UserSetups { get; set; }
        public DbSet<UserSetupTelemetry> UserSetupTelemetries { get; set; }
        public DbSet<SetupFilterType> SetupFilterTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Use all IEntityTypeConfiguration classes in this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }
    }
}