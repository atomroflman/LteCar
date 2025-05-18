namespace LteCar.Server.Data
{
    public class CarVideoStream
    {
        public int Id { get; set; }
        public string StreamName { get; set; }
        public int VehicleId { get; set; }
        public Car Vehicle { get; set; }
    }
}