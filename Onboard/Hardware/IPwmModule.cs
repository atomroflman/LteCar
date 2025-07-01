namespace LteCar.Onboard.Hardware
{
    public interface IPwmModule : IModule
    {
        /// <summary>
        /// Sets the PWM value.
        /// </summary>
        /// <param name="value">The value to set, typically between 0 and 1.</param>
        Task SetPwmCyclePercentage(float value);

        /// <summary>
        /// Gets the current PWM value.
        /// </summary>
        /// <returns>The current PWM value.</returns>
        Task<float> GetPwmValue();

        /// <summary>
        /// Sets the servo position.
        /// </summary>
        /// <param name="position">The position to set, typically between -1 and 1.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SetServoPosition(float position);
    }
}