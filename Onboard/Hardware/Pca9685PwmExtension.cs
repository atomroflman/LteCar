using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Hardware
{

    /// <summary>
    /// Maps normalized float values (-1 to 1) to PCA9685 PWM outputs using i2cset CLI calls.
    /// </summary>
    public class Pca9685PwmExtension : IModuleManager
    {
        public ILogger<Pca9685PwmExtension> Logger {get; }
        public Bash Bash { get; }
        public int BoardAddress { get; set; }
        public int I2cBus { get; set; } = 1; // Default I2C bus

        public Pca9685PwmExtension(ILogger<Pca9685PwmExtension> logger, Bash bash)
        {
            Logger = logger;
            Bash = bash;
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
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd().Trim();
                return !string.IsNullOrWhiteSpace(output) && File.Exists(output);
            }
            catch
            {
                return false;
            }
        }

        private async Task Initialize()
        {
            if (!CheckI2cSetAvailable())
                Logger.LogWarning("i2cset command is not available. Please install i2c-tools.");

            if (BoardAddress < 0 || BoardAddress > 127)
                throw new ArgumentOutOfRangeException(nameof(BoardAddress), "Board address must be between 0 and 127.");

            if (I2cBus < 0 || I2cBus > 15)
                throw new ArgumentOutOfRangeException(nameof(I2cBus), "I2C bus number must be between 0 and 15.");
            await Bash.ExecuteAsync($"i2cset -y {I2cBus} {BoardAddress} 0x00 0x10"); 
            await Bash.ExecuteAsync($"i2cset -y {I2cBus} {BoardAddress} 0xFE 0x79"); 
            await Bash.ExecuteAsync($"i2cset -y {I2cBus} {BoardAddress} 0x00 0x20");
            Logger.LogInformation("PCA9685 initialized on I2C bus {I2cBus} at address 0x{BoardAddress:X2}", I2cBus, BoardAddress);
        }

        public T GetModule<T>(int address) where T : class, IModule
        {
            if (!typeof(T).IsAssignableTo(typeof(IPwmModule)))
                throw new InvalidOperationException($"Module type {typeof(T).Name} is not a PWM module. Only IPwmModule is supported.");
            if (address < 0 || address > 15)
                throw new ArgumentOutOfRangeException(nameof(address), "Address must be between 0 and 15.");
            return new Pca9685PwmExtensionPwmModule(this, address) as T;
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

        public async Task SetPwmValue(float value)
        {
            value = Math.Clamp(value, 0f, 1f);

            // Map value (0..1) to PWM value (0..4095)
            int pwmValue = (int)(value * 4095);

            // ON time is always 0 (start of cycle), OFF time sets pulse width
            int onLow = 0x00;
            int onHigh = 0x00;
            int offLow = pwmValue & 0xFF;
            int offHigh = (pwmValue >> 8) & 0x0F;

            int regBase = 0x06 + 4 * _channel;

            _extension.Logger?.LogInformation(
                "Setting channel {Channel} to PWM {PWM} (Input={Input}) → Register 0x{Reg:X2}",
                _channel, pwmValue, value, regBase
            );

            // You may need to implement I2CSet or use _extension.Bash.ExecuteAsync here
            await _extension.Bash.ExecuteAsync($"i2cset -y {_extension.I2cBus} {_extension.BoardAddress} {regBase} {onLow:X2}");
            await _extension.Bash.ExecuteAsync($"i2cset -y {_extension.I2cBus} {_extension.BoardAddress} {regBase + 1} {onHigh:X2}");
            await _extension.Bash.ExecuteAsync($"i2cset -y {_extension.I2cBus} {_extension.BoardAddress} {regBase + 2} {offLow:X2}");
            await _extension.Bash.ExecuteAsync($"i2cset -y {_extension.I2cBus} {_extension.BoardAddress} {regBase + 3} {offHigh:X2}");
        }

        public  Task<float> GetPwmValue() => Task.FromResult(_lastValue);
    }
}