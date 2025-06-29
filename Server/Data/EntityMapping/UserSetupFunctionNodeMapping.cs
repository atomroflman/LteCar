using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LteCar.Server.Data.EntityMapping;

public class UserSetupFunctionNodeMapping : IEntityTypeConfiguration<UserSetupFunctionNode>
{
    public void Configure(EntityTypeBuilder<UserSetupFunctionNode> builder)
    {
        builder.Property(x => x.SetupFunctionName)
            .IsRequired()
            .HasMaxLength(100);
    }
}