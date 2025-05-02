namespace LteCar.Onboard.Hardware;

public abstract class BasePin
{
    public int PinNumber { get; }

    protected BasePin(int pinNumber)
    {
        PinNumber = pinNumber;
    }
}
