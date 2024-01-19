using VGManager.Adapter.Models;

namespace VGManager.Adapter.Services.Interfaces;

public interface ICommandProcessorService
{
    Task ProcessCommandAsync(CommandMessageBase commandMessage, CancellationToken cancellationToken = default);
}
