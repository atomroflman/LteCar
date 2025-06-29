using System.Diagnostics;

public class Bash : IDisposable
{
    private readonly Process _process;
    private bool _disposed;

    public Bash()
    {
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
        return _process.StandardInput.WriteLineAsync(cmd);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _process?.Kill();
        _process?.Dispose();
    }
}