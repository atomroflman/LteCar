using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LteCar.Server.Data.EntityMapping;

public class UserSetupTelemetryMapping : IEntityTypeConfiguration<UserSetupTelemetry>
{
    public void Configure(EntityTypeBuilder<UserSetupTelemetry> builder)
    {
    }
}
