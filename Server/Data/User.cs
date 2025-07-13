using System.ComponentModel.DataAnnotations;

namespace LteCar.Server.Data
{
    public class User : EntityBase
    {
        public string? Name { get; set; }
        public int? ActiveVehicleId { get; set; }
        public Car? ActiveVehicle { get; set; }
        public string? SessionToken { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public ICollection<UserChannelDevice> UserChannelDevices { get; set; }
        public ICollection<UserCarSetup> CarSetups { get; set; }
    }
}