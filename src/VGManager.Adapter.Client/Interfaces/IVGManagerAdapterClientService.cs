namespace VGManager.Adapter.Client.Interfaces;

public interface IVGManagerAdapterClientService
{
    Task<(bool isSuccess, string response)> SendAndReceiveMessageAsync(string commandType, string payload, CancellationToken cancellationToken);
}
