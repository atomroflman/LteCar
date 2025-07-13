public interface IModule {}

public interface IGpioModule : IModule
{
    /// <summary>
    /// Sets the pin value.
    /// </summary>
    /// <param name="value">The value to set.</param>
    Task SetValue(bool value);

    /// <summary>
    /// Gets the current value of the pin.
    /// </summary>
    /// <returns>The current value of the pin.</returns>
    Task<bool> GetValue();
}
