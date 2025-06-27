public interface IModule {}

public interface IGpioModule : IModule
{
    /// <summary>
    /// Sets the pin value.
    /// </summary>
    /// <param name="value">The value to set.</param>
    void SetValue(bool value);

    /// <summary>
    /// Gets the current value of the pin.
    /// </summary>
    /// <returns>The current value of the pin.</returns>
    bool GetValue();
}

public interface IPwmModule : IModule
{
    /// <summary>
    /// Sets the PWM value.
    /// </summary>
    /// <param name="value">The value to set, typically between 0 and 1.</param>
    void SetPwmValue(float value);

    /// <summary>
    /// Gets the current PWM value.
    /// </summary>
    /// <returns>The current PWM value.</returns>
    float GetPwmValue();
}

public interface IModuleManager {
    public T GetModule<T>(int address) where T : class, IModule;
}

public interface IModuleManagerFactory
{
    IModuleManager CreateModuleManager(string moduleType, Dictionary<string, object> options);
}