using System.ComponentModel.DataAnnotations;
using LteCar.Onboard;
using Microsoft.EntityFrameworkCore;

namespace LteCar.Server.Data
{
    public class LteCarContext : DbContext
    {
        public LteCarContext(DbContextOptions<LteCarContext> options) : base(options) { }

        public DbSet<Car> Cars { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<CarChannel> CarChannels { get; set; }
        

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

    public class Car
    {
        public int Id { get; set; }
        [MaxLength(64)]
        public string? Name { get; set; }
        [MaxLength(64)]
        public string CarId { get; set; }
        [MaxLength(64)]
        public string CarSecret { get; set; }
        [MaxLength(64)]
        public string ChannelMapHash { get; set; }
        [MaxLength(64)]
        public string SeesionId { get; set; }
        
        public int? VideoStreamPort { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public ICollection<CarChannel> Functions { get; set; }

        public VideoSettings VideoSettings { get; set; } = VideoSettings.Default;

        public override string ToString()
        {
            return $"{Name ?? CarId} ({CarId})";
        }
    }

    public class VehicleVideoStream
    {
        public int Id { get; set; }
        public string StreamId { get; set; }
        public int VehicleId { get; set; }
        public Car Vehicle { get; set; }
    }

    public class CarChannel
    {
        public int Id { get; set; }
        [MaxLength(64)]
        public string? DisplayName { get; set; }
        [MaxLength(64)]
        public string ChannelName { get; set; }
        public bool IsEnabled { get; set; }
        public bool RequiresAxis { get; set; }
        public int CarId { get; set; }
        public Car Car { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        [MaxLength(64)]
        public string? Name { get; set; }
        public int? ActiveVehicleId { get; set; }
        public Car? ActiveVehicle { get; set; }
        [MaxLength(64)]
        public string? SessionToken { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.Now;
    }

    public class UserVehicleSetup
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int VehicleId { get; set; }
        public Car Vehicle { get; set; }
        public ICollection<UserSetupLink> UserSetupLinks { get; set; }
        public ICollection<UserSetupChannel> UserSetupChannels { get; set; }
        public ICollection<UserSetupFilter> UserSetupFilters { get; set; }
    }

    public class UserSetupChannel
    {
        public int Id { get; set; }
        public int UserSetupId { get; set; }
        public UserVehicleSetup UserSetup { get; set; }
        [MaxLength(64)]
        public string? Name { get; set; }
        public int ChannelId { get; set; }
        public bool IsAxis { get; set; }
        public float CalibrationMin { get; set; } = -1;
        public float CalibrationMax { get; set; } = 1;
    }

    public class UserSetupFilter
    {
        public int Id { get; set; }
        public int UserSetupId { get; set; }
        public UserVehicleSetup UserSetup { get; set; }
        public int SetupFilterTypeId { get; set; }
        public SetupFilterType SetupFilterType { get; set; }
        [MaxLength(64)]
        public string? Name { get; set; }
        public string? Paramerters { get; set; }
    }

    public class SetupFilterType {
        public int Id { get; set; }
        [MaxLength(256)]
        public string TypeName { get; set; }
        [MaxLength(64)]
        public string? Name { get; set; }
        [MaxLength(512)]
        public string? Description { get; set; }
    }

    public class UserSetupLink 
    {
        public int Id { get; set; }
        public int UserSetupId { get; set; }
        public UserVehicleSetup UserSetup { get; set; }

        public int? ChannelSourceId { get; set; }
        public UserSetupChannel? ChannelSource { get; set; }
        public int? FilterSourceId { get; set; }
        public UserSetupFilter? FilterSource { get; set; }
        public int? FilterTargetId { get; set; }
        public UserSetupFilter? FilterTarget { get; set; }
        public int? VehicleFunctionTargetId { get; set; }
        public CarChannel? VehicleFunctionTarget { get; set; }

        public LinkType Type => ChannelSourceId == null ^ FilterSourceId == null || FilterTargetId == null ^ VehicleFunctionTargetId == null
            ? LinkType.Invalid
            : ChannelSourceId != null 
                ? VehicleFunctionTargetId != null ? LinkType.ChannelFunction : LinkType.ChannelFilter
                : VehicleFunctionTargetId != null ? LinkType.FilterFunction : LinkType.FilterFilter;

    }
    
    public enum LinkType
    {
        Invalid,
        ChannelFunction,
        FilterFilter,
        ChannelFilter,
        FilterFunction
    }
}