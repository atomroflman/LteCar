using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LteCar.Server.Data.EntityMapping;

public class CarVideoStreamMapping : IEntityTypeConfiguration<CarVideoStream>
{
    public void Configure(EntityTypeBuilder<CarVideoStream> builder)
    {
        builder.ToTable("CarVideoStreams");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StreamId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Protocol)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.Port)
            .IsRequired();

        builder.Property(x => x.StartTime)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.ProcessArguments)
            .HasMaxLength(500);

        builder.Property(x => x.StreamPurpose)
            .HasMaxLength(100);

        // Foreign Key zu Car
        builder.HasOne(x => x.Car)
            .WithMany()
            .HasForeignKey(x => x.CarId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index für bessere Performance
        builder.HasIndex(x => x.StreamId)
            .IsUnique();

        builder.HasIndex(x => new { x.CarId, x.IsActive });
        
        builder.HasIndex(x => x.Port);
    }
}