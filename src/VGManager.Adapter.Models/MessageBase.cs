using MessagePack;

namespace VGManager.Adapter.Models;

[MessagePackObject]
public abstract class MessageBase
{
    [Key(0)]
    public string CorrelationId { get; set; } = null!;
    [Key(1)]
    public DateTime Timestamp { get; set; }
    [Key(2)]
    public string Payload { get; set; } = null!;
}
