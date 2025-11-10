namespace LteCar.Shared.Video;

public interface IVideoSettings
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Framerate { get; set; }
    public float Brightness { get; set; }
    public int BitrateKbps { get; set; }
}
