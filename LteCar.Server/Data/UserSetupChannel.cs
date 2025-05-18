using System.ComponentModel.DataAnnotations;

namespace LteCar.Server.Data
{
    public class UserSetupChannel : EntityBase
    {
        public int UserSetupId { get; set; }
        public UserCarSetup UserSetup { get; set; }
        [MaxLength(64)]
        public string? Name { get; set; }
        public int ChannelId { get; set; }
        public bool IsAxis { get; set; }
        public float CalibrationMin { get; set; } = -1;
        public float CalibrationMax { get; set; } = 1;
    }
}