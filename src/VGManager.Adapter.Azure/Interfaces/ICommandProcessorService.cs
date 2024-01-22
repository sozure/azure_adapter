using VGManager.Communication.Models;

namespace VGManager.Adapter.Interfaces;

public interface ICommandProcessorService
{
    Task ProcessCommandAsync(CommandMessageBase commandMessage, CancellationToken cancellationToken = default);
}
