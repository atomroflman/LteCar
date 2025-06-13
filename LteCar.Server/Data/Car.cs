using System.ComponentModel.DataAnnotations;
using LteCar.Onboard;

namespace LteCar.Server.Data
{
    public class Car : EntityBase
    {
        public string? Name { get; set; }
        public string CarId { get; set; }
        public string ChannelMapHash { get; set; }
        
        public int? VideoStreamPort { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public ICollection<CarChannel> Functions { get; set; }

        public VideoSettings VideoSettings { get; set; } = VideoSettings.Default;
        public ICollection<UserCarSetup> UserCarSetups { get; set; }

        public override string ToString()
        {
            return $"{Name ?? CarId} ({CarId})";
        }
    }
}