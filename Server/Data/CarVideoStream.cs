namespace LteCar.Server.Data
{
    public class CarVideoStream : EntityBase
    {
        public string StreamName { get; set; }
        public int VehicleId { get; set; }
        public Car Vehicle { get; set; }
    }
}