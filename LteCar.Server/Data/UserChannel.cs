
using System.ComponentModel.DataAnnotations;
using LteCar.Server.Data;

public class UserChannelDevice : EntityBase
{
    [MaxLength(512)]
    public string DeviceName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<UserChannel> Channels { get; set; }
}

public class UserChannel : EntityBase
{
    public int UserChannelDeviceId { get; set; }
    public UserChannelDevice UserChannelDevice { get; set; } 
    
    public string? Name { get; set; }
    public int ChannelId { get; set; }
    public bool IsAxis { get; set; }
    /// <summary>
    /// The number of decimal places to round the value to for axes.
    /// </summary>
    public int Accuracy { get; set; } = 4;
    public float? CalibrationMin { get; set; } = -1;
    public float? CalibrationMax { get; set; } = 1;
}