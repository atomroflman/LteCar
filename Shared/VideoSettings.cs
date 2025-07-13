namespace LteCar.Onboard;

public class VideoSettings
{
    public static VideoSettings Default => new VideoSettings
    {
        Width = 640,
        Height = 480,
        Framerate = 22,
        Bitrate = 2000000,
        Brightness = 0,
    };

    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Framerate { get; set; }
    public float? Brightness { get; set; }
    public int? Bitrate { get; set; }
}