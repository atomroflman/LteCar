using System.Reflection;
using System.Text.Json;
using LteCar.Onboard.Control.ControlTypes;
using LteCar.Onboard.Hardware;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Control;

public class ControlExecutionService
{
    public IServiceProvider ServiceProvider { get; }
    public ILogger<ControlExecutionService> Logger { get; }

    private readonly Dictionary<string, ControlTypeBase> _controls = new();
    public ControlExecutionService(IServiceProvider serviceProvider, ILogger<ControlExecutionService> logger)
    {
        ServiceProvider = serviceProvider;
        Logger = logger;
    }
    
    public void Initialize()
    {
        var channelMapFile = new FileInfo("channelMap.json");
        if (!channelMapFile.Exists)
            throw new FileNotFoundException("channelMap.json could not be found");
        var channelMap = JsonSerializer.Deserialize<ChannelMap>(channelMapFile.OpenRead());
        if (channelMap == null)
            throw new Exception("channelMap.json could not be deserialized");
        WiringPi.wiringPiSetupGpio();
        foreach (var channel in channelMap)
        {
            var controlType = GetControlType(channel.Value.ControlType);
            Logger.LogDebug($"Got type {controlType.Name} for channel {channel.Key}.");
            var control = ServiceProvider.GetService(controlType) as ControlTypeBase;
            if (control == null)
                throw new Exception($"ControlType '{channel.Value.ControlType}' can not be instantiated");
            var functions = PinFunctionFlags.None;
            if (channel.Value.PhysicalGpio != null) {
                var prop = typeof(RaspberryPiPinMap).GetProperty($"PIN_{channel.Value.PhysicalGpio}");
                functions = prop.GetCustomAttribute<PinFunctionAttribute>()!.Functions;
                var gpio = prop.GetValue(null) as int?;
                if (gpio == null)
                {
                    Logger.LogError($"Channel {prop.Name} does not resolve a GPIO-Pin. Skipping channel.");
                    continue;
                }
                control.Pin = gpio!.Value;
            }
            control.TestDisabled = channel.Value.IgnoreTest;
            
            control.Name = channel.Key;
            control.Options = channel.Value.Options;
            if (!functions.HasFlag(control.RequiredFunctions))
                Logger.LogWarning($"Required function: '{control.RequiredFunctions}' not met by Channel {channel.Value.PhysicalGpio} (GPIO: {control.Pin})");
            control.Initialize();
            _controls.Add(channel.Key, control);
            Logger.LogInformation($"Initialized Channel: {channel.Key} - {channel.Value.ControlType}@{channel.Value.PhysicalGpio} (GPIO: {control.Pin})");
        }
    }

    public async Task RunControlTestsAsync() 
    {
        foreach (var c in _controls) {
            Logger.LogInformation($"Testing: {c.Key}");
            await c.Value.RunTestAsync();
        }
    }
    
    public void SetControl(string channel, decimal value)
    {
        if (!_controls.ContainsKey(channel))
        {
            Logger.LogError($"Channel not configured: {channel}");
        }
        var control = _controls[channel];
        Logger.LogDebug($"New Channel Value: {channel}: {value} => PIN: {control}");
        control.OnControlRecived(value);
    }

    public void ReleaseControl()
    {
        foreach (var control in _controls)
        {
            control.Value.OnControlReleased();
        }
    }

    private Type GetControlType(string valueControlType)
    {
        var t = typeof(ControlTypeBase).Assembly.GetTypes()
            .FirstOrDefault(type => (type.GetCustomAttributes(typeof(ControlTypeAttribute), false).FirstOrDefault() as ControlTypeAttribute)?.TypeName == valueControlType);
        if (t == null)
            throw new Exception($"ControlType {valueControlType} not found");
        return t;
    }
}

