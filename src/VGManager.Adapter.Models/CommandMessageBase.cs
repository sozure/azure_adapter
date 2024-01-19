using MessagePack;
using VGManager.Adapter.Messaging.Models.Interfaces;

namespace VGManager.Adapter.Messaging.Models;

[MessagePackObject]
public abstract class CommandMessageBase : MessageBase, ICommandRequest
{
    [Key(3)]
    public Guid InstanceId { get; set; }
    [Key(4)]
    public string Destination { get; set; } = null!;
    [Key(5)]
    public string CommandSource { get; set; } = null!;
    [Key(6)]
    public string CommandType { get; set; } = null!;
    [Key(7)]
    public string CommandRoute => $"{Destination}/{CommandSource}/{CommandType}";
}
