using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LteCar.Server.Data.EntityMapping;

public class CarChannelMapping : IEntityTypeConfiguration<CarChannel>
{
    public void Configure(EntityTypeBuilder<CarChannel> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(f => f.Car)
            .WithMany(v => v.Functions)
            .HasForeignKey(f => f.CarId);
    }
}
