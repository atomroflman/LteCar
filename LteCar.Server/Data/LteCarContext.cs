using Microsoft.AspNetCore.Mvc;
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
        public DbSet<UserSetupTelemetry> UserSetupTelemetries { get; set; }
        public DbSet<SetupFilterType> SetupFilterTypes { get; set; }

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

            var carChannel = modelBuilder.Entity<CarChannel>();
            carChannel.HasOne(f => f.Car)
                .WithMany(v => v.Functions)
                .HasForeignKey(f => f.CarId);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.SessionToken)
                .IsUnique();

            modelBuilder.Entity<UserSetupFlowNodeBase>()
                .ToTable("UserSetupFlowNodes")
                .HasDiscriminator<string>("NodeType")
                .HasValue<UserSetupCarChannelNode>("C")
                .HasValue<UserSetupFunctionNode>("F")
                .HasValue<UserSetupUserChannelNode>("U");
            modelBuilder.Entity<UserSetupLink>()
                .HasOne(l => l.UserSetupFromNode)
                .WithMany()
                .HasForeignKey(l => l.UserSetupFromNodeId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserSetupLink>()
                .HasOne(l => l.UserSetupToNode)
                .WithMany()
                .HasForeignKey(l => l.UserSetupToNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserChannelDevice>()
                .HasOne(d => d.User)
                .WithMany(u => u.UserChannelDevices)
                .HasForeignKey(d => d.UserId);
            modelBuilder.Entity<UserChannelDevice>()
                .HasIndex(d => new { d.UserId, d.DeviceName })
                .IsUnique();
            modelBuilder.Entity<UserChannel>()
                .HasOne(c => c.UserChannelDevice)
                .WithMany(d => d.Channels)
                .HasForeignKey(c => c.UserChannelDeviceId);
            modelBuilder.Entity<UserChannel>()
                .HasIndex(c => new { c.UserChannelDeviceId, c.IsAxis, c.ChannelId })
                .IsUnique();
            modelBuilder.Entity<UserChannel>()
                .Property(c => c.Name)
                .HasMaxLength(64);

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