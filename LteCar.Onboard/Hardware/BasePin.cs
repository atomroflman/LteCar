namespace LteCar.Onboard.Hardware;

public abstract class BasePin
{
    public int PinNumber { get; }
    public IServiceProvider ServiceProvider { get; }

    protected BasePin(int pinNumber, IServiceProvider serviceProvider)
    {
        PinNumber = pinNumber;
        ServiceProvider = serviceProvider;
    }
}
