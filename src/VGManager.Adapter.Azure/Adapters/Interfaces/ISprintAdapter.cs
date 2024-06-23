using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Adapters.Interfaces;

public interface ISprintAdapter
{
    Task<(AdapterStatus, string)> GetCurrentSprintAsync(string project, CancellationToken cancellationToken = default);
}
