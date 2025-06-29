using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using LteCar.Onboard.Control;
using LteCar.Onboard.Hardware;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Hardware
{
    public class RaspberryPiGpioManager : IModuleManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RaspberryPiGpioManager> _logger;

        public RaspberryPiGpioManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<RaspberryPiGpioManager>>();
        }

        public T GetModule<T>(int address) where T : class, IModule
        {
            PinFunctionFlags requiredFunction;
            switch (typeof(T))
            {
                case Type t when t.IsAssignableTo(typeof(IPwmModule)):
                    requiredFunction = PinFunctionFlags.PWM;
                    break;
                case Type t when t.IsAssignableTo(typeof(IGpioModule)):
                    requiredFunction = PinFunctionFlags.GPIO;
                    break;
                default:
                    throw new NotSupportedException($"Module type {typeof(T).Name} is not supported.");
            }

            var pinProp = typeof(RaspberryPiPinMap).GetProperty($"PIN_{address}", BindingFlags.Public | BindingFlags.Static);

            if (pinProp == null)
                return null;

            var pinValue = pinProp.GetValue(null);
            if (pinValue == null)
                return null;

            var pinFuncAttr = pinProp.GetCustomAttributes(typeof(PinFunctionAttribute), false)
                .OfType<PinFunctionAttribute>()
                .FirstOrDefault();

            if (pinFuncAttr == null || (pinFuncAttr.Functions & requiredFunction) != requiredFunction)
            {
                _logger.LogWarning(
                    $"Pin {address} does not support the required function {requiredFunction}. " +
                    $"Available functions: {pinFuncAttr?.Functions ?? PinFunctionFlags.None}");
                return null;
            }

            var module = _serviceProvider.GetService<T>();
            if (module is IPinInitializable pinInit)
            {
                pinInit.InitializePin(Convert.ToInt32(pinValue));
            }
            return module;
        }
    }

    public interface IPinInitializable
    {
        void InitializePin(int pinNumber);
    }
}
