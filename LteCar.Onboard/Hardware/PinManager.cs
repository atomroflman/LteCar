namespace LteCar.Onboard.Hardware;

public class PinManager
{
    private readonly Dictionary<int, BasePin> AllocatedPins = new Dictionary<int, BasePin>();

    public IServiceProvider ServiceProvider { get; }

    public PinManager(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    private T AllocatePinInternal<T>(Func<T> factory, int pinNumber) 
        where T : BasePin
    {
        if (AllocatedPins.TryGetValue(pinNumber, out var existingPin))
            throw new InvalidOperationException($"Pin {pinNumber} is already allocated with ({existingPin.GetType().Name}).");
        var pin = factory();
        AllocatedPins.Add(pinNumber, pin);
        return pin;
    }

    public T AllocatePin<T>(int pinNumber) where T : BasePin => AllocatePinInternal<T>(
        () => CreateInstance<T>(pinNumber)
            ?? throw new InvalidOperationException($"Failed to create instance of {typeof(T).Name}"), pinNumber);

    private T CreateInstance<T>(int pinNumber) {
        var t = typeof(T);
        var ctor = t.GetConstructor(new [] {typeof(int), typeof(IServiceProvider)});
        if (ctor is null) 
            throw new MissingMemberException($"ctor(int pinNumber) not found on type: '{t}'! Found: {string.Join("\n", t.GetConstructors().Select(c => $"ctor({string.Join(", ", c.GetParameters().Select(p => p.ParameterType))})"))}");
        var instance = ctor.Invoke(new object[] {pinNumber, ServiceProvider});
        return (T)instance;
    }

    public void FreePin(int pinNumber)
    {
        if (AllocatedPins.TryGetValue(pinNumber, out var pin))
        {
            WiringPi.pinMode(pinNumber, WiringPi.PinMode.INPUT);
            AllocatedPins.Remove(pinNumber);
        }
    }

    public void FreeAll()
    {
        foreach (var kv in AllocatedPins)
        {
            WiringPi.pinMode(kv.Key, WiringPi.PinMode.INPUT);
        }
        AllocatedPins.Clear();
    }
}
