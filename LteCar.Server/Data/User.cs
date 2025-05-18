using System.ComponentModel.DataAnnotations;

namespace LteCar.Server.Data
{
    public class User
    {
        public int Id { get; set; }
        [MaxLength(64)]
        public string? Name { get; set; }
        public int? ActiveVehicleId { get; set; }
        public Car? ActiveVehicle { get; set; }
        [MaxLength(64)]
        public string? SessionToken { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.Now;
    }
}