using System.ComponentModel.DataAnnotations;

namespace LteCar.Server.Configuration;

public class ApplicationConfiguration
{
    public const string SectionName = "";

    [Required]
    public bool RunJanusServer { get; set; } = true;

    [Required]
    public ConnectionStrings ConnectionStrings { get; set; } = new();

    [Required]
    public JanusConfiguration JanusConfiguration { get; set; } = new();
}

public class ConnectionStrings
{
    [Required]
    [MinLength(1)]
    public string DefaultConnection { get; set; } = string.Empty;
}

public class JanusConfiguration
{
    public const string SectionName = "JanusConfiguration";

    [Required]
    [MinLength(1)]
    public string HostName { get; set; } = "localhost";

    [Range(1024, 65535)]
    public int UdpPortRangeStart { get; set; } = 10000;

    [Range(1024, 65535)]
    public int UdpPortRangeEnd { get; set; } = 10200;

    [Range(1024, 65535)]
    public int TcpPortRangeStart { get; set; } = 11000;

    [Range(1024, 65535)]
    public int TcpPortRangeEnd { get; set; } = 11200;

    public void Validate()
    {
        if (UdpPortRangeStart >= UdpPortRangeEnd)
            throw new InvalidOperationException("UdpPortRangeStart must be less than UdpPortRangeEnd");

        if (TcpPortRangeStart >= TcpPortRangeEnd)
            throw new InvalidOperationException("TcpPortRangeStart must be less than TcpPortRangeEnd");

        if (UdpPortRangeEnd - UdpPortRangeStart < 10)
            throw new InvalidOperationException("UDP port range must contain at least 10 ports");

        if (TcpPortRangeEnd - TcpPortRangeStart < 10)
            throw new InvalidOperationException("TCP port range must contain at least 10 ports");
    }
}