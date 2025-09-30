using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

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

    // Transport details (existing)
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

    // Metadata fields (new)
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // "camera", "sensor", "diagnostic"

    [MaxLength(50)]
    public string? Location { get; set; } // "front", "rear", "left", "right", "interior"

    [Required]
    public int Priority { get; set; } = 1;

    [Required]
    public bool Enabled { get; set; } = true;

    public DateTime LastStatusUpdate { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public bool IsRunning => IsActive && Enabled && EndTime == null;
}