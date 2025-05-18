using System.ComponentModel.DataAnnotations;

namespace LteCar.Server.Data
{
    public class SetupFilterType 
    {
        public int Id { get; set; }
        [MaxLength(256)]
        public string TypeName { get; set; }
        [MaxLength(64)]
        public string? Name { get; set; }
        [MaxLength(512)]
        public string? Description { get; set; }
    }
}