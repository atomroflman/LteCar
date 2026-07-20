namespace LteCar.Server.Data
{
    public class CarTelemetry : EntityBase
    {
        public string ChannelName { get; set; }
        public int CarId { get; set; }
        public Car Car { get; set; } = null!;
        public int ReadIntervalTicks { get; set; }
        public string TelemetryType { get; set; }

        public LteCar.Shared.Channels.TelemetryDataType DataType { get; set; } = LteCar.Shared.Channels.TelemetryDataType.String;
        public string? Unit { get; set; }
        public byte? Decimals { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}