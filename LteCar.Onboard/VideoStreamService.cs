using System.Diagnostics;
x^
namespace LteCar.Onboard;

public class VideoSettings
{
    public static VideoSettings Default => new VideoSettings
    {
        Width = 640,
        Height = 480,
        Framerate = 30
    };

    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Framerate { get; set; }
}

public class VideoStreamService
{
    private VideoSettings _videoSettings;
    private Process _libcameraProcess;

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
        process.StartInfo.FileName = "libcamera-vid";
        process.StartInfo.Arguments = "--width 640 --height 480 --framerate 30 --inline --output -";
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
                Console.WriteLine(line);
            }
        });
    }
}