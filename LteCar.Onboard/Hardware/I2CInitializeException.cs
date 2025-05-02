namespace LteCar.Onboard.Hardware;

[System.Serializable]
public class I2CInitializeException : Exception
{
    public I2CInitializeException() { }
    public I2CInitializeException(string message) : base(message) { }
    public I2CInitializeException(string message, System.Exception inner) : base(message, inner) { }
    protected I2CInitializeException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}