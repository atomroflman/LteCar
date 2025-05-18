using Microsoft.EntityFrameworkCore;

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
        public DbSet<UserSetupFilter> UserSetupFilters { get; set; }
        public DbSet<UserSetupChannel> UserSetupChannels { get; set; }
        public DbSet<UserSetupTelemetry> UserSetupTelemetries { get; set; }
        public DbSet<UserSetupLink> UserSetupLinks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Car>()
                .HasMany(v => v.Functions)
                .WithOne(f => f.Car)
                .HasForeignKey(f => f.CarId);
            modelBuilder.Entity<Car>()
                .OwnsOne(v => v.VideoSettings, b =>
                {
                    b.WithOwner();
                    b.Property(v => v.Width).HasColumnName("VideoWidth");
                    b.Property(v => v.Height).HasColumnName("VideoHeight");
                    b.Property(v => v.Framerate).HasColumnName("VideoFramerate");
                    b.Property(v => v.Brightness).HasColumnName("VideoBrightness");
                    b.Property(v => v.Bitrate).HasColumnName("VideoBitrate");
                });

            modelBuilder.Entity<UserSetupFilter>()
                .HasOne(f => f.UserSetup)
                .WithMany(u => u.UserSetupFilters)
                .HasForeignKey(f => f.UserSetupId);

            modelBuilder.Entity<UserSetupChannel>()
                .HasOne(c => c.UserSetup)
                .WithMany(u => u.UserSetupChannels)
                .HasForeignKey(c => c.UserSetupId);
            var userSetupLink = modelBuilder.Entity<UserSetupLink>();
            userSetupLink.HasOne(l => l.UserSetup)
                .WithMany(u => u.UserSetupLinks)
                .HasForeignKey(l => l.UserSetupId)
                .OnDelete(DeleteBehavior.ClientCascade);
            userSetupLink.HasOne(l => l.ChannelSource)
                .WithMany()
                .HasForeignKey(l => l.ChannelSourceId)
                .OnDelete(DeleteBehavior.ClientCascade);
            userSetupLink.HasOne(l => l.FilterSource)
                .WithMany()
                .HasForeignKey(l => l.FilterSourceId)
                .OnDelete(DeleteBehavior.ClientCascade);
            userSetupLink.HasOne(l => l.FilterTarget)
                .WithMany()
                .HasForeignKey(l => l.FilterTargetId)
                .OnDelete(DeleteBehavior.ClientCascade);
            userSetupLink.HasOne(l => l.VehicleFunctionTarget)
                .WithMany()
                .HasForeignKey(l => l.VehicleFunctionTargetId)
                .OnDelete(DeleteBehavior.ClientCascade);
            var carChannel = modelBuilder.Entity<CarChannel>();
            carChannel.HasOne(f => f.Car)
                .WithMany(v => v.Functions)
                .HasForeignKey(f => f.CarId);

            base.OnModelCreating(modelBuilder);
        }
    }

    public class UserSetupTelemetry : EntityBase
    {
        public int CarTelemetryId { get; set; }
        public CarTelemetry CarTelemetry { get; set; } = null!;
        public int UserSetupId { get; set; }
        public UserCarSetup UserSetup { get; set; } = null!;
        public int Order { get; set; }
        public int? OverrideTicks { get; set; }
    }
}