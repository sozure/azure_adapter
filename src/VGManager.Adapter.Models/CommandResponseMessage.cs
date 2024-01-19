using MessagePack;
using VGManager.Adapter.Models.Interfaces;

namespace VGManager.Adapter.Models;

[MessagePackObject]
public abstract class CommandResponseMessage : MessageBase, ICommandResponse
{
    [Key(3)]
    public Guid CommandInstanceId { get; set; }
    [Key(4)]
    public string Origin { get; set; } = null!;
    [Key(5)]
    public string CommandResponseSource { get; set; } = null!;
    [Key(6)]
    public string CommandResponseType { get; set; } = null!;
    [Key(7)]
    public string CommandResponseRoute { get; set; } = null!;
    [Key(8)]
    public bool IsSuccess { get; set; }
}
