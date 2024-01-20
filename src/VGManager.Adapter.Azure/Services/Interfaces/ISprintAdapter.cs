using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface ISprintAdapter
{
    Task<(AdapterStatus, string)> GetCurrentSprintAsync(string project, CancellationToken cancellationToken = default);
}
