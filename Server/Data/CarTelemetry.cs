namespace LteCar.Server.Data
{
    public class CarTelemetry : EntityBase
    {
        public string ChannelName { get; set; }
        public int CarId { get; set; }
        public Car Car { get; set; } = null!;
        public int ReadIntervalTicks { get; set; }
        public string TelemetryType { get; set; }
    }
}