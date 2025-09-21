using LteCar.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace LteCar.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideoStreamController : Microsoft.AspNetCore.Mvc.ControllerBase
{
    private readonly UDPVideoStreamService _videoStreamService;
    private readonly ILogger<VideoStreamController> _logger;

    public VideoStreamController(UDPVideoStreamService videoStreamService, ILogger<VideoStreamController> logger)
    {
        _videoStreamService = videoStreamService;
        _logger = logger;
    }

    /// <summary>
    /// MJPEG Video Stream Endpoint
    /// Liefert einen kontinuierlichen MJPEG Stream aus dem UDP Video Buffer
    /// </summary>
    /// <param name="carId">Optional: Car ID for logging/tracking purposes</param>
    /// <returns>MJPEG Stream</returns>
    [HttpGet("video")]
    public async Task GetVideoStream([FromQuery] string? carId = null)
    {
        var clientInfo = $"{HttpContext.Connection.RemoteIpAddress}:{HttpContext.Connection.RemotePort}";
        if (!string.IsNullOrEmpty(carId))
        {
            clientInfo = $"{clientInfo} (Car: {carId})";
        }
        
        _logger.LogInformation($"Video stream requested by client: {clientInfo}");
        
        try
        {
            // Client zum Video Service hinzufügen
            _videoStreamService.AddClient(Response);
            
            // Verbindung am Leben halten bis Client disconnected
            var cancellationToken = HttpContext.RequestAborted;
            
            // Warten bis Client die Verbindung schließt
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(() => tcs.SetResult(true));
            
            await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            // Client hat Verbindung getrennt - normal
            _logger.LogDebug($"Video stream client disconnected: {clientInfo}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in video stream for client: {clientInfo}");
        }
        finally
        {
            // Client aus Video Service entfernen
            _videoStreamService.RemoveClient(Response);
            _logger.LogDebug($"Removed video stream client: {clientInfo}");
        }
    }

    /// <summary>
    /// Get status information about the video stream
    /// </summary>
    /// <returns>Video stream status</returns>
    [HttpGet("status")]
    public IActionResult GetStreamStatus()
    {
        // Hier könnten wir Status-Informationen vom UDPVideoStreamService abfragen
        // Für jetzt ein einfacher Status
        return Ok(new
        {
            Status = "Running",
            Port = 10000,
            Timestamp = DateTime.UtcNow,
            Message = "UDP Video Stream Service is operational"
        });
    }
}