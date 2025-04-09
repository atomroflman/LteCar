namespace LteCar.Onboard;

public class VideoSettings
{
    public static VideoSettings Default => new VideoSettings
    {
        Width = 640,
        Height = 480,
        Framerate = 22
    };

    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Framerate { get; set; }
}