namespace LteCar.Shared.Video;

/// <summary>
/// Model for updating video settings from the UI.
/// </summary>
public class VideoSettingsModel
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Framerate { get; set; }
    public float Brightness { get; set; }
    public int BitrateKbps { get; set; }
    public bool Enabled { get; set; }

    public void ApplySettings(IVideoSettings settings)
    {
        settings.Width = Width;
        settings.Height = Height;
    
        settings.Framerate = Framerate;
        settings.Brightness = Brightness;
    
        settings.BitrateKbps = BitrateKbps * 1024;
    }
}