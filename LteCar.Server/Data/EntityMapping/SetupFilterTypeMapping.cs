using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LteCar.Server.Data.EntityMapping;

public class SetupFilterTypeMapping : IEntityTypeConfiguration<SetupFilterType>
{
    public void Configure(EntityTypeBuilder<SetupFilterType> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
