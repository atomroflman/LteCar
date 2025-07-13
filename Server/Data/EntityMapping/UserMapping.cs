using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LteCar.Server.Data.EntityMapping;

public class UserMapping : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(u => u.SessionToken).IsUnique();
        builder.Property(u => u.Name).HasMaxLength(64);
        builder.Property(u => u.SessionToken).HasMaxLength(32);
    }
}
