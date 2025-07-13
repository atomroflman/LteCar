using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Hardware
{

    /// <summary>
    /// Maps normalized float values (-1 to 1) to PCA9685 PWM outputs using i2cset CLI calls.
    /// </summary>
    public class Pca9685PwmExtension : IModuleManager
    {
        private bool _initialized;
        private const int PCA9685_DEFAULT_ADDRESS = 0x40; // Default I2C address for PCA9685

        public ILogger<Pca9685PwmExtension> Logger { get; }
        public Bash Bash { get; }
        public int BoardAddress { get; set; }
        public int I2cBus { get; set; } = 1; // Default I2C bus
        public int BoardI2cAddress
        {
            get => BoardAddress + PCA9685_DEFAULT_ADDRESS;
        }

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
            await Bash.ExecuteAsync($"i2cset -y {I2cBus} {BoardI2cAddress} 0x00 0x10"); // Set MODE1 register to 0x10 (sleep mode)
            await Bash.ExecuteAsync($"i2cset -y {I2cBus} {BoardI2cAddress} 0xFE 0x79"); // Set PRE_SCALE register for 50Hz (0x79)
            await Bash.ExecuteAsync($"i2cset -y {I2cBus} {BoardI2cAddress} 0x00 0x20"); // Set MODE1 register to 0x20 (wake up, auto increment enabled)
            Logger.LogInformation("PCA9685 initialized on I2C bus {I2cBus} at address 0x{BoardAddress:X2} (@I2C: {BoardI2cAddress})", I2cBus, BoardAddress, BoardI2cAddress);
        }

        public T GetModule<T>(int address) where T : class, IModule
        {
            if (!_initialized)
            {
                Initialize().Wait();
                _initialized = true;
            }
            Logger.LogInformation("Creating PWM module of type {ModuleType} at address {Address}", typeof(T).Name, address);
            if (!typeof(T).IsAssignableTo(typeof(IPwmModule)))
                throw new InvalidOperationException($"Module type {typeof(T).Name} is not a PWM module. Only IPwmModule is supported.");
            if (address < 0 || address > 15)
                throw new ArgumentOutOfRangeException(nameof(address), "Address must be between 0 and 15.");
            return new Pca9685PwmExtensionPwmModule(this, address) as T;
        }
    }

    public class Pca9685PwmExtensionPwmModule : IPwmModule
    {
        // TODO: Add otions to configure servo pulse width limits
        private const int SERVO_MIN_PULSE = 105; // Minimum pulse width in microseconds
        private const int SERVO_MAX_PULSE = 550; // Maximum pulse width in microseconds
        private const int PWM_FREQUENCY = 50; // PWM frequency in Hz

        private readonly Pca9685PwmExtension _extension;
        private readonly int _channel;
        private readonly int _regBase;
        private float _lastValue = 0;

        public Pca9685PwmExtensionPwmModule(Pca9685PwmExtension extension, int channel)
        {
            _extension = extension;
            _channel = channel;
            _regBase = 0x06 + 4 * channel; // Base register for the channel
        }

        public async Task SetServoPosition(float position)
        {
            position = Math.Clamp(position, -1f, 1f);
            var pwmValue = (int)Math.Round((SERVO_MAX_PULSE - SERVO_MIN_PULSE) * ((position + 1) / 2) + SERVO_MIN_PULSE);
            await SendPwmUpdate(pwmValue);
        }

        public async Task SetPwmCyclePercentage(float value)
        {
            value = Math.Clamp(value, 0f, 1f);

            // Map value (0..1) to PWM value (0..4095)
            int pwmValue = (int)(value * 4095);
            await SendPwmUpdate(pwmValue);
        }
        
        /// <summary>
        /// Setzt die Pulsbreite (High-Zeit) in Millisekunden für einen PWM-Kanal.
        /// Die Periode ist durch die Frequenz (z.B. 20ms bei 50Hz) festgelegt.
        /// </summary>
        /// <param name="pulseWidthMs">Pulsbreite in Millisekunden (z.B. 1.5 für 1,5ms)</param>
        public async Task SetPulseWidthMilliseconds(float pulseWidthMs)
        {
            // PCA9685 arbeitet mit 4096 Steps pro Periode
            // Periode = 1000ms / Frequenz (z.B. 20ms bei 50Hz)
            float periodMs = 1000f / PWM_FREQUENCY;
            if (pulseWidthMs < 0 || pulseWidthMs > periodMs)
                throw new ArgumentOutOfRangeException(nameof(pulseWidthMs), $"Pulse width must be between 0 and {periodMs} ms.");
            int pwmValue = (int)Math.Round(4095 * (pulseWidthMs / periodMs));
            await SendPwmUpdate(pwmValue);
        }

        private async Task SendPwmUpdate(int pwmValue)
        {
            _extension.Logger?.LogDebug(
               "Setting channel {Channel} to PWM {PWM} (Input={Input}) on Register 0x{Reg:X2}",
               _channel, pwmValue, pwmValue, _regBase
           );
            // ON time is always 0 (start of cycle), OFF time sets pulse width
            int onLow = 0x00;
            int onHigh = 0x00;
            int offLow = pwmValue & 0xFF;
            int offHigh = (pwmValue >> 8) & 0x0F;

            await _extension.Bash.ExecuteAsync($"i2cset -y {_extension.I2cBus} {_extension.BoardI2cAddress} {_regBase} 0x{onLow:X2}");
            await _extension.Bash.ExecuteAsync($"i2cset -y {_extension.I2cBus} {_extension.BoardI2cAddress} {_regBase + 1} 0x{onHigh:X2}");
            await _extension.Bash.ExecuteAsync($"i2cset -y {_extension.I2cBus} {_extension.BoardI2cAddress} {_regBase + 2} 0x{offLow:X2}");
            await _extension.Bash.ExecuteAsync($"i2cset -y {_extension.I2cBus} {_extension.BoardI2cAddress} {_regBase + 3} 0x{offHigh:X2}");
        }

        public Task<float> GetPwmValue() => Task.FromResult(_lastValue);
    }
}