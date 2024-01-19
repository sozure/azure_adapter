namespace VGManager.Adapter.Messaging.Models.Interfaces;

public interface ICommandRequest
{
    Guid InstanceId { get; set; }
}
