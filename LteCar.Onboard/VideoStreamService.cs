using System.Diagnostics;
using System.Text.Json;
using LteCar.Shared;
using Microsoft.Extensions.Configuration;

namespace LteCar.Onboard;

public class VideoSettings
{
    public static VideoSettings Default => new VideoSettings
    {
        Width = 640,
        Height = 480,
        Framerate = 22
    };

    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Framerate { get; set; }
}

public class VideoStreamService
{
    private const string CACHE_FILE_NAME = "videoSettings.cache.json";
    private VideoSettings _videoSettings;
    private Process _libcameraProcess;
    private JanusConfiguration _janusConfiguration;
    public ServerConnectionService ServerConnectionService { get; }

    public VideoStreamService(ServerConnectionService serverConnectionService)
    {
        ServerConnectionService = serverConnectionService;
    }
    
    public void UpdateCameraSettings(VideoSettings newSettings)
    {
        var hasChanges = false;
        foreach (var prop in typeof(VideoSettings).GetProperties())
        {
            var newValue = prop.GetValue(newSettings);
            var oldValue = prop.GetValue(_videoSettings);

            if (newValue == null)
                continue;
            
            if (!newValue.Equals(oldValue))
            {
                Console.WriteLine($"Updating {prop.Name} from {oldValue} to {newValue}");
                prop.SetValue(_videoSettings, newValue);
                hasChanges = true;
            }
        }
        if (!hasChanges)
        {
            Console.WriteLine("No changes detected in camera settings.");
            return;
        }
        // Update the camera settings
        _videoSettings = newSettings;
        File.WriteAllText(CACHE_FILE_NAME, JsonSerializer.Serialize(_videoSettings));
        StartLibcameraProcess();
    }
    
    public void Initialize()
    {
        // _janusConfiguration = ServerConnectionService.RequestJanusConfigAsync();
        // Load the video settings from the cache file
        if (File.Exists(CACHE_FILE_NAME))
        {
            var json = File.ReadAllText(CACHE_FILE_NAME);
            _videoSettings = JsonSerializer.Deserialize<VideoSettings>(json);
        }
        else
        {
            _videoSettings = VideoSettings.Default;
        }

        StartLibcameraProcess();
    }
    
    public void ResetCameraSettings()
    {
        // Reset the camera settings to default
        _videoSettings = VideoSettings.Default;
        if (File.Exists(CACHE_FILE_NAME))
            File.Delete(CACHE_FILE_NAME);
        StartLibcameraProcess();
    }

    public void StartLibcameraProcess()
    {
        // Kill the existing process if it's running
        if (_libcameraProcess != null && !_libcameraProcess.HasExited)
        {
            _libcameraProcess.Kill();
            _libcameraProcess.Dispose();
            _libcameraProcess = null;
            Console.WriteLine("Libcamera process killed.");
        }

        var process = new Process();
        var parameters = $"libcamera-vid -t 0 --inline --framerate {_videoSettings.Framerate} --width {_videoSettings.Width} --height {_videoSettings.Height}"
        + $" --codec yuv420 --nopreview -o - | gst-launch-1.0 fdsrc ! videoparse format=i420 width={_videoSettings.Width} height={_videoSettings.Height} framerate={_videoSettings.Framerate}/1" 
        + $" ! vp8enc deadline=1 ! rtpvp8pay pt=100 ! udpsink host=192.168.3.149 port=10000";

//         var parameters = $@"libcamera-vid -t 0 --inline --width {_videoSettings.Width} --height {_videoSettings.Height} --framerate {_videoSettings.Framerate} \
//   --codec h264 --profile high \
//   -o - | gst-launch-1.0 -v fdsrc ! h264parse ! rtph264pay config-interval=1 pt=96 ! \
//   udpsink host={_janusConfiguration.JanusServerHost} port={_janusConfiguration.JanusUdpPort}";
        process.StartInfo.FileName = "bash";
        process.StartInfo.Arguments = $"-c \"{parameters}\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.Start();
        _libcameraProcess = process;
        Console.WriteLine("Libcamera process started with PID: " + process.Id);
        // Read the output stream
        Task.Run(() =>
        {
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                Console.WriteLine(line);
            }
        });

        // Read the error stream
        Task.Run(() =>
        {
            while (!process.StandardError.EndOfStream)
            {
                string line = process.StandardError.ReadLine();
                Console.WriteLine($"ERROR: {line}");
            }
        });
    }
}