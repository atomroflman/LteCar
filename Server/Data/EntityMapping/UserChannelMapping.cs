using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LteCar.Server.Data.EntityMapping;

public class UserChannelMapping : IEntityTypeConfiguration<UserChannel>
{
    public void Configure(EntityTypeBuilder<UserChannel> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(64);
        builder.HasOne(c => c.UserChannelDevice)
            .WithMany(d => d.Channels)
            .HasForeignKey(c => c.UserChannelDeviceId);
        builder.HasIndex(c => new { c.UserChannelDeviceId, c.IsAxis, c.ChannelId }).IsUnique();
    }
}
