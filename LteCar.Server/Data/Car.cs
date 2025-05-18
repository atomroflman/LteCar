using System.ComponentModel.DataAnnotations;
using LteCar.Onboard;

namespace LteCar.Server.Data
{
    public class Car
    {
        public int Id { get; set; }
        [MaxLength(64)]
        public string? Name { get; set; }
        [MaxLength(64)]
        public string CarId { get; set; }
        [MaxLength(64)]
        public string ChannelMapHash { get; set; }
        [MaxLength(64)]
        public string? SeesionId { get; set; }
        
        public int? VideoStreamPort { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public ICollection<CarChannel> Functions { get; set; }

        public VideoSettings VideoSettings { get; set; } = VideoSettings.Default;

        public override string ToString()
        {
            return $"{Name ?? CarId} ({CarId})";
        }
    }
}