// using System;
// using System.Device.I2c;
// using System.Threading.Tasks;
// using Microsoft.Extensions.Logging;

// namespace LteCar.Onboard.Hardware
// {
//     public class Pca9685GpioModuleManager : IModuleManager, IDisposable
//     {
//         private readonly I2cDevice _device;
//         private readonly ILogger<Pca9685GpioModuleManager> _logger;
//         public int BoardAddress { get; }
//         public int I2cBus { get; }

//         public Pca9685GpioModuleManager(ILogger<Pca9685GpioModuleManager> logger)
//         {
//             _logger = logger;
            
//             var settings = new I2cConnectionSettings(I2cBus, BoardAddress + 0x40) // PCA9685 default address is 0x40
//             {
//                 BusSpeed = I2cBusSpeed.FastMode
//             };
//             _device = I2cDevice.Create(settings);
//             Initialize();
//         }

//         private void WriteRegister(byte register, byte value)
//         {
//             _device.Write(new byte[] { register, value });
//         }

//         private void SetPwmChannel(int channel, int on, int off)
//         {
//             int regBase = 0x06 + 4 * channel;
//             WriteRegister((byte)regBase, (byte)(on & 0xFF));
//             WriteRegister((byte)(regBase + 1), (byte)((on >> 8) & 0x0F));
//             WriteRegister((byte)(regBase + 2), (byte)(off & 0xFF));
//             WriteRegister((byte)(regBase + 3), (byte)((off >> 8) & 0x0F));
//         }

//         private void Initialize()
//         {
//             // MODE1 register, restart oscillator
//             WriteRegister(0x00, 0x01);
//             // MODE2 register, totem pole output
//             WriteRegister(0x01, 0x04);
//         }

//         public T GetModule<T>(int address) where T : class, IModule
//         {
//             if (typeof(T) == typeof(IPwmModule))
//                 return new Pca9685GpioPwmModule(this, address) as T;
//             throw new NotSupportedException($"Only IPwmModule is supported by this manager.");
//         }

//         public void Dispose()
//         {
//             _device?.Dispose();
//         }

//         internal void SetPwm(int channel, int on, int off) => SetPwmChannel(channel, on, off);
//     }

//     public class Pca9685GpioPwmModule : IPwmModule
//     {
//         private readonly Pca9685GpioModuleManager _manager;
//         private readonly int _channel;
//         private float _lastValue = 0;

//         public Pca9685GpioPwmModule(Pca9685GpioModuleManager manager, int channel)
//         {
//             _manager = manager;
//             _channel = channel;
//         }

//         public Task SetPwmValue(float value)
//         {
//             value = Math.Clamp(value, 0f, 1f);
//             _lastValue = value;
//             int pwmValue = (int)(value * 4095);
//             int on = 0;
//             int off = pwmValue;
//             _manager.SetPwm(_channel, on, off);
//             return Task.CompletedTask;
//         }

//         public Task<float> GetPwmValue() => Task.FromResult(_lastValue);
//     }
// }
