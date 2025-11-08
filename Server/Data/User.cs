using System.ComponentModel.DataAnnotations;

namespace LteCar.Server.Data
{
    public class User : EntityBase
    {
        public string? Name { get; set; }
        public int? ActiveVehicleId { get; set; }
        public Car? ActiveVehicle { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public long? TransferCode { get; set; }
        public long SessionId { get; set; }
        public DateTime? TransferCodeExpiresAt { get; set; }
        public ICollection<UserChannelDevice> UserChannelDevices { get; set; }
        public ICollection<UserCarSetup> CarSetups { get; set; }
    }
}