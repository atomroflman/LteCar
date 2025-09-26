using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LteCar.Server.Data;

public class CarVideoStream : EntityBase
{
    [Required]
    [MaxLength(50)]
    public string StreamId { get; set; } = string.Empty;

    [Required]
    public int CarId { get; set; }

    [ForeignKey(nameof(CarId))]
    public Car Car { get; set; } = null!;

    [Required]
    [MaxLength(10)]
    public string Protocol { get; set; } = string.Empty; // TCP, UDP

    [Required]
    public int Port { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? ProcessArguments { get; set; }

    [MaxLength(100)]
    public string? StreamPurpose { get; set; } // "video", "telemetry", "control", etc.

    [NotMapped]
    public bool IsRunning => IsActive && EndTime == null;
}