using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

/// <summary>
/// Maps normalized float values (-1 to 1) to PCA9685 PWM outputs using i2cset CLI calls.
/// </summary>
public class Pca9685PwmExtension : IModuleManager
{
    private ILogger<Pca9685PwmExtension> _logger;

    public int BoardAddress { get;set;} 
    public int I2cBus { get; set; }

    public Pca9685PwmExtension(ILogger<Pca9685PwmExtension> logger = null)
    {
        this.boardAddress = boardAddress;
        this.I2cBus = i2cBus;
        _logger = logger;

        _i2cAvailable = CheckI2cSetAvailable();

        if (!_i2cAvailable)
        {
            _logger.LogWarning(
                "'i2cset' was not found on the system. Please install it using:\n\n    sudo apt install -y i2c-tools\n"
            );
        }
    }

    private bool CheckI2cSetAvailable()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "which",
                Arguments = "i2cset",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return !string.IsNullOrWhiteSpace(output) && File.Exists(output);
        }
        catch
        {
            return false;
        }
    }

    public T GetModule<T>(int address) where T : class, IModule 
    {
        if (!typeof(T).IsAssignableTo(typeof(IPwmModule)))
            throw new InvalidOperationException($"Module type {typeof(T).Name} is not a PWM module. Only IPwmModule is supported.");
        return new Pca9685PwmExtensionPwmModule(this, address) as T;
    }

    /// <summary>
    /// Sets the servo output for a given channel using a normalized float value (-1 to 1).
    /// </summary>
    /// <param name="channel">Servo channel (0–15)</param>
    /// <param name="value">Normalized input value (-1.0 to +1.0)</param>
    public void SetServo(float value)
    {
        
    }

    /// <summary>
    /// Calls i2cset to write a value to a register.
    /// </summary>
    private void I2CSet(int register, int value)
    {
        string cmd = $"i2cset -y {i2cBus} 0x{boardAddress:X2} 0x{register:X2} 0x{value:X2}";
        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{cmd}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        proc.Start();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            string error = proc.StandardError.ReadToEnd();
            throw new Exception($"i2cset command failed: {cmd}\n{error}");
        }
    }

    /// <summary>
    /// Checks if the i2cset command is available on the system.
    /// </summary>
    private static bool IsI2cSetAvailable()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "which",
                Arguments = "i2cset",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return !string.IsNullOrWhiteSpace(output) && File.Exists(output);
        }
        catch
        {
            return false;
        }
    }
}

public class Pca9685PwmExtensionPwmModule : IPwmModule
{
    private readonly Pca9685PwmExtension _extension;
    private readonly int _channel;
    private float _lastValue = 0;

    public Pca9685PwmExtensionPwmModule(Pca9685PwmExtension extension, int channel)
    {
        _extension = extension;
        _channel = channel;
    }

    public void SetPwmValue(float value)
    {if (channel < 0 || channel > 15)
            throw new ArgumentOutOfRangeException(nameof(channel), "Channel must be between 0 and 15.");
        if (value < 0f || value > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(value), "Input must be between 0 and 1.0.");

        // ON time is always 0 (start of cycle), OFF time sets pulse width
        int onLow = 0x00;
        int onHigh = 0x00;
        int offLow = pwmValue & 0xFF;
        int offHigh = (pwmValue >> 8) & 0x0F;

        int regBase = 0x06 + 4 * channel;

        _logger?.LogInformation(
            "Setting channel {Channel} to PWM {PWM} (Input={Input}) → Register 0x{Reg:X2}",
            channel, pwmValue, value, regBase
        );

        I2CSet(regBase + 0, onLow);
        I2CSet(regBase + 1, onHigh);
        I2CSet(regBase + 2, offLow);
        I2CSet(regBase + 3, offHigh);
    }

    public float GetPwmValue() => _lastValue;
}