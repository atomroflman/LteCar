using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LteCar.Server.Data.EntityMapping;

public class CarMapping : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(64);
        builder.Property(x => x.CarId).HasMaxLength(64);
        builder.Property(x => x.ChannelMapHash).HasMaxLength(64);
        builder.HasMany(v => v.Functions)
            .WithOne(f => f.Car)
            .HasForeignKey(f => f.CarId);
        builder.OwnsOne(v => v.VideoSettings, b =>
        {
            b.WithOwner();
            b.Property(v => v.Width).HasColumnName("VideoWidth");
            b.Property(v => v.Height).HasColumnName("VideoHeight");
            b.Property(v => v.Framerate).HasColumnName("VideoFramerate");
            b.Property(v => v.Brightness).HasColumnName("VideoBrightness");
            b.Property(v => v.Bitrate).HasColumnName("VideoBitrate");
        });
    }
}
