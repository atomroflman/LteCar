using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LteCar.Server.Data.EntityMapping;

public class CarTelemetryMapping : IEntityTypeConfiguration<CarTelemetry>
{
    public void Configure(EntityTypeBuilder<CarTelemetry> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
