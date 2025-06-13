using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LteCar.Server.Data.EntityMapping;

public class UserCarSetupMapping : IEntityTypeConfiguration<UserCarSetup>
{
    public void Configure(EntityTypeBuilder<UserCarSetup> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(u => u.User)
            .WithMany(c => c.CarSetups)
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(u => u.Car)
            .WithMany(c => c.UserCarSetups)
            .HasForeignKey(u => u.CarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
