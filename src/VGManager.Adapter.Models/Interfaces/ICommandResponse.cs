namespace VGManager.Adapter.Messaging.Models.Interfaces;

public interface ICommandResponse
{
    Guid CommandInstanceId { get; set; }
}
