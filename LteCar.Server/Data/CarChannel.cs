using System.ComponentModel.DataAnnotations;

namespace LteCar.Server.Data
{
    public class CarChannel : EntityBase
    {
        [MaxLength(64)]
        public string? DisplayName { get; set; }
        [MaxLength(64)]
        public string ChannelName { get; set; }
        public bool IsEnabled { get; set; }
        public bool RequiresAxis { get; set; }
        public int CarId { get; set; }
        public Car Car { get; set; }
    }
}