using System.ComponentModel.DataAnnotations;

namespace LteCar.Server.Data
{
    public class UserCarSetup
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int CarId { get; set; }
        public Car Car { get; set; }
        [MaxLength(64)]
        public string? CarSecret { get; set; }
        public ICollection<UserSetupLink> UserSetupLinks { get; set; }
        public ICollection<UserSetupChannel> UserSetupChannels { get; set; }
        public ICollection<UserSetupFilter> UserSetupFilters { get; set; }
    }
}