using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System.Text.RegularExpressions;
using VGManager.Adapter.Azure.Services.Interfaces;

namespace VGManager.Adapter.Azure.Services.VG;

public class VariableFilterService : IVariableFilterService
{
    private readonly ILogger _logger;
    private readonly string SecretVGType = "AzureKeyVault";

    public VariableFilterService(ILogger<VariableFilterService> logger)
    {
        _logger = logger;
    }

    public IEnumerable<VariableGroup> Filter(IEnumerable<VariableGroup> variableGroups, string filter)
    {
        Regex regex;
        try
        {
            regex = new Regex(filter.ToLower(), RegexOptions.None, TimeSpan.FromMilliseconds(5000));
        }
        catch (RegexParseException ex)
        {
            _logger.LogError(ex, "Couldn't parse and create regex. Value: {value}.", filter);
            return variableGroups.Where(vg => filter.ToLower() == vg.Name.ToLower()).ToList();
        }
        try
        {
            return variableGroups.Where(vg => regex.IsMatch(vg.Name.ToLower())).ToList();
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogError(ex, "Regex match timeout. Value: {value}.", filter);
            return Enumerable.Empty<VariableGroup>();
        }
    }

    public IEnumerable<KeyValuePair<string, VariableValue>> Filter(IDictionary<string, VariableValue> variables, Regex regex)
    {
        return variables.Where(v => regex.IsMatch(v.Key.ToLower())).ToList();
    }

    public IEnumerable<KeyValuePair<string, VariableValue>> Filter(IDictionary<string, VariableValue> variables, string filter)
    {
        return variables.Where(v => filter.ToLower() == v.Key.ToLower()).ToList();
    }

    public IEnumerable<VariableGroup> FilterWithoutSecrets(bool filterAsRegex, string filter, IEnumerable<VariableGroup> variableGroups)
    {
        if (filterAsRegex)
        {
            Regex regex;
            try
            {
                regex = new Regex(filter.ToLower(), RegexOptions.None, TimeSpan.FromMilliseconds(5));
            }
            catch (RegexParseException ex)
            {
                _logger.LogError(ex, "Couldn't parse and create regex. Value: {value}.", filter);
                return variableGroups.Where(vg => filter.ToLower() == vg.Name.ToLower() && vg.Type != SecretVGType).ToList();
            }
            return variableGroups.Where(vg => regex.IsMatch(vg.Name.ToLower()) && vg.Type != SecretVGType).ToList();
        }
        return variableGroups.Where(vg => filter.ToLower() == vg.Name.ToLower() && vg.Type != SecretVGType).ToList();
    }
}
