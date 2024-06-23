using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System.Text.RegularExpressions;
using VGManager.Adapter.Azure.Services.Interfaces;

namespace VGManager.Adapter.Azure.Helper;

public class VariableFilterService(ILogger<VariableFilterService> logger) : IVariableFilterService
{
    private readonly string SecretVGType = "AzureKeyVault";

    public IEnumerable<VariableGroup> Filter(IEnumerable<VariableGroup> variableGroups, string variableGroupFilter)
    {
        Regex regex;
        try
        {
            regex = new Regex(variableGroupFilter.ToLower(), RegexOptions.None, TimeSpan.FromMilliseconds(5000));
        }
        catch (RegexParseException ex)
        {
            logger.LogError(ex, "Couldn't parse and create regex. Value: {value}.", variableGroupFilter);
            return variableGroups.Where(vg => variableGroupFilter.ToLower() == vg.Name.ToLower()).ToList();
        }
        try
        {
            return variableGroups.Where(vg => regex.IsMatch(vg.Name.ToLower())).ToList();
        }
        catch (RegexMatchTimeoutException ex)
        {
            logger.LogError(ex, "Regex match timeout. Value: {value}.", variableGroupFilter);
            return Enumerable.Empty<VariableGroup>();
        }
    }

    public IEnumerable<KeyValuePair<string, VariableValue>> Filter(IDictionary<string, VariableValue> variables, Regex keyRegex)
    {
        return variables.Where(v => keyRegex.IsMatch(v.Key.ToLower())).ToList();
    }

    public IEnumerable<KeyValuePair<string, VariableValue>> Filter(IDictionary<string, VariableValue> variables, string keyFilter)
    {
        return variables.Where(v => keyFilter.ToLower() == v.Key.ToLower()).ToList();
    }

    public IEnumerable<VariableGroup> FilterWithoutSecrets(bool filterAsRegex, string variableGroupFilter, IEnumerable<VariableGroup> variableGroups)
    {
        if (filterAsRegex)
        {
            Regex regex;
            try
            {
                regex = new Regex(variableGroupFilter.ToLower(), RegexOptions.None, TimeSpan.FromMilliseconds(5));
            }
            catch (RegexParseException ex)
            {
                logger.LogError(ex, "Couldn't parse and create regex. Value: {value}.", variableGroupFilter);
                return variableGroups.Where(vg => variableGroupFilter.ToLower() == vg.Name.ToLower() && vg.Type != SecretVGType).ToList();
            }
            return variableGroups.Where(vg => regex.IsMatch(vg.Name.ToLower()) && vg.Type != SecretVGType).ToList();
        }
        return variableGroups.Where(vg => variableGroupFilter.ToLower() == vg.Name.ToLower() && vg.Type != SecretVGType).ToList();
    }
}
