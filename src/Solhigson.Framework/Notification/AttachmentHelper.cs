namespace Solhigson.Framework.Notification;

public record AttachmentHelper
{
    public byte[] Data { get; set; }
    public string Name { get; set; }
    public string ContentType { get; set; }
}