using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class GitFileAdapter : IGitFileAdapter
{
    private readonly IHttpClientProvider _clientProvider;
    private readonly ILogger _logger;

    private readonly string[] Extensions = { "yaml" };

    public GitFileAdapter(IHttpClientProvider clientProvider, ILogger<GitFileAdapter> logger)
    {
        _clientProvider = clientProvider;
        _logger = logger;
    }

    public async Task<(AdapterStatus, IEnumerable<string>)> GetFilePathAsync(
        string organization,
        string pat,
        string repositoryId,
        string fileName,
        string branch,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            _logger.LogInformation("Request file path from {project} git project.", repositoryId);
            _clientProvider.Setup(organization, pat);
            return await GetFilePathAsync(branch, repositoryId, fileName, cancellationToken);
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            _logger.LogError(ex, "{project} git project is not found.", repositoryId);
            return (AdapterStatus.ProjectDoesNotExist, Enumerable.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file path from {project} git project.", repositoryId);
            return (AdapterStatus.Unknown, Enumerable.Empty<string>());
        }
    }

    public async Task<(AdapterStatus, IEnumerable<string>)> GetConfigFilesAsync(
        string organization,
        string pat,
        string repositoryId,
        string? extension,
        string branch,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            _logger.LogInformation("Get config files from {project} git project.", repositoryId);
            _clientProvider.Setup(organization, pat);
            return await GetConfigFilesAsync(branch, repositoryId, extension, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting config files from {project} git project.", repositoryId);
            return (AdapterStatus.Unknown, Enumerable.Empty<string>());
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
            using var client = await _clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
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
            _logger.LogError(ex, "Error getting file path from {project} git project.", repositoryId);
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
            using var client = await _clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
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
            _logger.LogError(ex, "Error getting file path from {project} git project.", repositoryId);
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
