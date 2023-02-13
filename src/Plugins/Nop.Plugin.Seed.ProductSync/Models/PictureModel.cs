namespace Nop.Plugin.Seed.ProductSync.Models;

public class PictureModel
{
    public string FileName { get; set; }
    public string MimeType { get; set; }
    public byte[] ByteData { get; set; }
    public string FileExtention { get; set; }
}