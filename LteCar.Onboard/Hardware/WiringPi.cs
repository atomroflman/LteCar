using System;
using System.Runtime.InteropServices;

namespace LteCar.Onboard.Hardware;

public static class WiringPi
{
    private const string WiringPiLib = "libwiringPi.so";

    // --- Setup ---
    [DllImport(WiringPiLib)] public static extern int wiringPiSetup();
    [DllImport(WiringPiLib)] public static extern int wiringPiSetupSys();
    [DllImport(WiringPiLib)] public static extern int wiringPiSetupGpio();
    [DllImport(WiringPiLib)] public static extern int wiringPiSetupPhys();

    // --- GPIO ---
    [DllImport(WiringPiLib)] public static extern void pinMode(int pin, int mode);
    [DllImport(WiringPiLib)] public static extern void digitalWrite(int pin, int value);
    [DllImport(WiringPiLib)] public static extern int digitalRead(int pin);
    [DllImport(WiringPiLib)] public static extern void pullUpDnControl(int pin, int pud);

    // --- PWM ---
    [DllImport(WiringPiLib)] public static extern void pwmWrite(int pin, int value);
    [DllImport(WiringPiLib)] public static extern void pwmSetMode(int mode);
    [DllImport(WiringPiLib)] public static extern void pwmSetRange(uint range);
    [DllImport(WiringPiLib)] public static extern void pwmSetClock(int divisor);
    
    [DllImport(WiringPiLib)] public static extern void pwmToneWrite(int pin, int freq);
    [DllImport(WiringPiLib)] public static extern void gpioClockSet(int pin, int freq) ;

    // --- Timing ---
    [DllImport(WiringPiLib)] public static extern void delay(uint howLong);
    [DllImport(WiringPiLib)] public static extern void delayMicroseconds(uint howLong);
    [DllImport(WiringPiLib)] public static extern uint millis();
    [DllImport(WiringPiLib)] public static extern uint micros();

    // --- Interrupts ---
    public delegate void InterruptCallback();
    
    [DllImport(WiringPiLib)]
    public static extern int wiringPiISR(int pin, int edgeType, InterruptCallback function);

    // --- I2C ---
    [DllImport(WiringPiLib)] public static extern int wiringPiI2CSetup(int devId);
    [DllImport(WiringPiLib)] public static extern int wiringPiI2CRead(int fd);
    [DllImport(WiringPiLib)] public static extern int wiringPiI2CWrite(int fd, int data);
    [DllImport(WiringPiLib)] public static extern int wiringPiI2CWriteReg8(int fd, int reg, int data);
    [DllImport(WiringPiLib)] public static extern int wiringPiI2CWriteReg16(int fd, int reg, int data);
    [DllImport(WiringPiLib)] public static extern int wiringPiI2CReadReg8(int fd, int reg);
    [DllImport(WiringPiLib)] public static extern int wiringPiI2CReadReg16(int fd, int reg);

    // --- SPI ---
    [DllImport(WiringPiLib)] public static extern int wiringPiSPISetup(int channel, int speed);
    [DllImport(WiringPiLib)] public static extern int wiringPiSPIDataRW(int channel, byte[] data, int len);

    // --- Serial (UART) ---
    [DllImport(WiringPiLib)] public static extern int serialOpen(string device, int baud);
    [DllImport(WiringPiLib)] public static extern void serialClose(int fd);
    [DllImport(WiringPiLib)] public static extern int serialFlush(int fd);
    [DllImport(WiringPiLib)] public static extern void serialPutchar(int fd, char c);
    [DllImport(WiringPiLib)] public static extern void serialPuts(int fd, string s);
    [DllImport(WiringPiLib)] public static extern int serialDataAvail(int fd);
    [DllImport(WiringPiLib)] public static extern int serialGetchar(int fd);

    // --- Konstanten für pinMode, pullUpDnControl etc. ---
    public static class PinMode
    {
        public const int INPUT = 0;
        public const int OUTPUT = 1;
        public const int PWM_OUTPUT = 2;
        public const int GPIO_CLOCK = 3;
        public const int SOFT_PWM_OUTPUT = 4;
        public const int SOFT_TONE_OUTPUT = 5;
        public const int PWM_TONE_OUTPUT = 6;
    }

    public static class PullUpDn
    {
        public const int PUD_OFF = 0;
        public const int PUD_DOWN = 1;
        public const int PUD_UP = 2;
    }

    public static class EdgeType
    {
        public const int INT_EDGE_SETUP = 0;
        public const int INT_EDGE_FALLING = 1;
        public const int INT_EDGE_RISING = 2;
        public const int INT_EDGE_BOTH = 3;
    }

    public static class PwmMode
    {
        public const int PWM_MODE_BAL = 0;
        public const int PWM_MODE_MS = 1;
    }
}