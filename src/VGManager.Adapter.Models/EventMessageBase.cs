using MessagePack;

namespace VGManager.Adapter.Messaging.Models;

[MessagePackObject]
public class EventMessageBase : MessageBase
{
    [Key(3)]
    public string Origin { get; set; } = null!;
    [Key(4)]
    public string EventSource { get; set; } = null!;
    [Key(5)]
    public string EventType { get; set; } = null!;
    [Key(6)]
    public string EventRoute { get; set; } = null!;
}
