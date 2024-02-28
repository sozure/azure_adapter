using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class GitFileAdapter(IHttpClientProvider clientProvider, ILogger<GitFileAdapter> logger) : IGitFileAdapter
{
    private readonly string[] Extensions = { "yaml" };

    public async Task<BaseResponse<Dictionary<string, object>>> GetFilePathAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<GitFileBaseRequest<string>>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
            }

            logger.LogInformation("Request file path from {project} git project.", payload.RepositoryId);
            clientProvider.Setup(payload.Organization, payload.PAT);
            var result = await GetFilePathAsync(payload.Branch, payload.RepositoryId, payload.AdditionalInformation ?? string.Empty, cancellationToken);
            return ResponseProvider.GetResponse(result);
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            logger.LogError(ex, "{project} git project is not found.", payload?.RepositoryId ?? "Unknown");
            return ResponseProvider.GetResponse((AdapterStatus.ProjectDoesNotExist, Enumerable.Empty<string>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting file path from {project} git project.", payload?.RepositoryId ?? "Unknown");
            return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
        }
    }

    public async Task<BaseResponse<Dictionary<string, object>>> GetConfigFilesAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<GitFileBaseRequest<string>>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
            }

            logger.LogInformation("Get config files from {project} git project.", payload.RepositoryId);
            clientProvider.Setup(payload.Organization, payload.PAT);
            var result = await GetConfigFilesAsync(payload.Branch, payload.RepositoryId, payload.AdditionalInformation ?? string.Empty, cancellationToken);
            return ResponseProvider.GetResponse(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting config files from {project} git project.", payload?.RepositoryId ?? "Unknown");
            return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
        }
    }

    private async Task<(AdapterStatus, IEnumerable<string>)> GetFilePathAsync(
        string version,
        string repositoryId,
        string fileName,
        CancellationToken cancellationToken
        )
    {
        try
        {
            using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
            var request = new GitItemRequestData()
            {
                ItemDescriptors = new GitItemDescriptor[]
                {
                    new()
                    {
                        RecursionLevel = VersionControlRecursionType.Full,
                        Version = version,
                        VersionType = GitVersionType.Branch,
                        Path = "/"
                    }
                }
            };
            var itemsBatch = await client.GetItemsBatchAsync(request, repositoryId, cancellationToken: cancellationToken);
            var result = new List<string>();
            foreach (var itemBatch in itemsBatch)
            {
                var elements = itemBatch.Where(item => item.Path.Contains(fileName)).ToList();
                if (elements.Any())
                {
                    result.Add(elements.FirstOrDefault()?.Path ?? string.Empty);
                }
            }
            return (AdapterStatus.Success, result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting file path from {project} git project.", repositoryId);
            return (AdapterStatus.Unknown, Enumerable.Empty<string>());
        }
    }

    private async Task<(AdapterStatus, IEnumerable<string>)> GetConfigFilesAsync(
        string version,
        string repositoryId,
        string? extension,
        CancellationToken cancellationToken
        )
    {
        try
        {
            using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
            var request = new GitItemRequestData()
            {
                ItemDescriptors = new GitItemDescriptor[]
                {
                    new()
                    {
                        RecursionLevel = VersionControlRecursionType.Full,
                        Version = version,
                        VersionType = GitVersionType.Branch,
                        Path = "/"
                    }
                }
            };
            var itemsBatch = await client.GetItemsBatchAsync(request, repositoryId, cancellationToken: cancellationToken);
            var result = new List<string>();
            var hasExtensionSpecification = !string.IsNullOrEmpty(extension);
            foreach (var itemBatch in itemsBatch)
            {
                var elements = hasExtensionSpecification ?
                    itemBatch.Where(item => item.Path.EndsWith(extension ?? string.Empty)).ToList() :
                    GetConfigFiles(itemBatch);

                foreach (var element in elements)
                {
                    result.Add(element.Path);
                }
            }
            return (AdapterStatus.Success, result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting file path from {project} git project.", repositoryId);
            return (AdapterStatus.Unknown, Enumerable.Empty<string>());
        }
    }

    private IEnumerable<GitItem> GetConfigFiles(IEnumerable<GitItem> items)
    {
        var result = new List<GitItem>();
        foreach (var item in items)
        {
            var extension = item.Path.Split('.').LastOrDefault();
            if (Extensions.Contains(extension ?? string.Empty))
            {
                result.Add(item);
            }
        }
        return result;
    }
}
