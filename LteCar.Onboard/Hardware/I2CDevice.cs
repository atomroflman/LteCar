namespace LteCar.Onboard.Hardware;

public class I2CDevice {
    int _deviceHandle;
    internal I2CDevice(int deviceHandle)
    {
        _deviceHandle = deviceHandle;
    }

    public int ReadByte()
    {
        return WiringPi.wiringPiI2CRead(_deviceHandle);
    }

    public void WriteByte(byte data)
    {
        WiringPi.wiringPiI2CWrite(_deviceHandle, data);
    }

    public int ReadRegister(byte register)
    {
        return WiringPi.wiringPiI2CReadReg8(_deviceHandle, register);
    }

    public void WriteRegister(byte register, byte data)
    {
        WiringPi.wiringPiI2CWriteReg8(_deviceHandle, register, data);
    }
}
