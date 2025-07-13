using System.ComponentModel.DataAnnotations;

namespace LteCar.Server.Data
{
    public class UserCarSetup : EntityBase
    {
        public int UserId { get; set; }
        public User User { get; set; }
        public int CarId { get; set; }
        public Car Car { get; set; }
        [MaxLength(64)]
        public string? CarSecret { get; set; }
        public ICollection<UserSetupTelemetryNode> TelemetryNodes { get; set; }
    }
}