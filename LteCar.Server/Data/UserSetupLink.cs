namespace LteCar.Server.Data
{
    public class UserSetupLink : EntityBase
    {
        public int UserSetupId { get; set; }
        public UserCarSetup UserSetup { get; set; }

        public int? ChannelSourceId { get; set; }
        public UserSetupChannel? ChannelSource { get; set; }
        public int? FilterSourceId { get; set; }
        public UserSetupFilter? FilterSource { get; set; }
        public int? FilterTargetId { get; set; }
        public UserSetupFilter? FilterTarget { get; set; }
        public int? VehicleFunctionTargetId { get; set; }
        public CarChannel? VehicleFunctionTarget { get; set; }

        public LinkType Type => ChannelSourceId == null ^ FilterSourceId == null || FilterTargetId == null ^ VehicleFunctionTargetId == null
            ? LinkType.Invalid
            : ChannelSourceId != null 
                ? VehicleFunctionTargetId != null ? LinkType.ChannelFunction : LinkType.ChannelFilter
                : VehicleFunctionTargetId != null ? LinkType.FilterFunction : LinkType.FilterFilter;

    }
}