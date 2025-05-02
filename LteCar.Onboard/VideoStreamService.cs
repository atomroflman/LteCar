using System.Diagnostics;

namespace LteCar.Onboard;

public class VideoStreamService
{
    public void StartLibcameraProcess()
    {
        // Start the libcamera process
        var process = new Process();
        process.StartInfo.FileName = "libcamera-vid";
        process.StartInfo.Arguments = "--width 640 --height 480 --framerate 30 --inline --output -";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.Start();

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