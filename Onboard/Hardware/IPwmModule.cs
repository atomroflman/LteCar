namespace LteCar.Onboard.Hardware
{
    public interface IPwmModule : IModule
    {
        /// <summary>
        /// Sets the PWM value.
        /// </summary>
        /// <param name="value">The value to set, typically between 0 and 1.</param>
        Task SetPwmValue(float value);

        /// <summary>
        /// Gets the current PWM value.
        /// </summary>
        /// <returns>The current PWM value.</returns>
        Task<float> GetPwmValue();
    }
}