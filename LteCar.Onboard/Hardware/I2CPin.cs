namespace LteCar.Onboard.Hardware;

public class I2CPin : BasePin
{
    private Dictionary<int, I2CDevice> _devices = new Dictionary<int, I2CDevice>();

    internal I2CPin(int pinNumber, int deviceAddress, IServiceProvider serviceProvider) : base(pinNumber, serviceProvider)
    {
    }

    public I2CDevice InitializeDevice(int deviceAddress) {
        var deviceHandle = WiringPi.wiringPiI2CSetup(deviceAddress);
        if (deviceHandle == -1)
            throw new I2CInitializeException();
        var dev = new I2CDevice(deviceHandle);
        _devices.Add(deviceAddress, dev);
        return dev;
    }
}
