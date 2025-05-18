using System.ComponentModel.DataAnnotations;

namespace LteCar.Server.Data
{
    public class UserSetupFilter : EntityBase
    {
        public int UserSetupId { get; set; }
        public UserCarSetup UserSetup { get; set; }
        public int SetupFilterTypeId { get; set; }
        public SetupFilterType SetupFilterType { get; set; }
        [MaxLength(64)]
        public string? Name { get; set; }
        public string? Paramerters { get; set; }
    }
}