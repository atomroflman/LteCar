using System.Diagnostics;
using Microsoft.Extensions.Logging;

public class Bash : IDisposable
{
    private readonly Process _process;
    private bool _disposed;
    public ILogger<Bash> Logger { get; }

    public Bash(ILogger<Bash> logger)
    {
        Logger = logger;
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-i", // interactive
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        _process.Start();
    }

    public Task ExecuteAsync(string cmd)
    {
        Logger.LogDebug($"Executing command: {cmd}");
        return _process.StandardInput.WriteLineAsync(cmd);
    }

    public string ExecuteAndRead(string cmd)
    {
        var proc = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{cmd}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        proc.Start();
        string output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();
        return output;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _process?.Kill();
        _process?.Dispose();
    }
}