namespace LteCar.Onboard.Hardware;

public class GpioInputPin : BasePin
{
    internal GpioInputPin(int pinNumber, IServiceProvider serviceProvider) : base(pinNumber, serviceProvider)
    {
        WiringPi.pinMode(pinNumber, WiringPi.PinMode.INPUT);
    }

    public int Read()
    {
        return WiringPi.digitalRead(PinNumber);
    }
}
