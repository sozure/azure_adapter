using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System.Text.RegularExpressions;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IVariableFilterService
{
    IEnumerable<VariableGroup> Filter(IEnumerable<VariableGroup> variableGroups, string variableGroupFilter);

    IEnumerable<KeyValuePair<string, VariableValue>> Filter(IDictionary<string, VariableValue> variables, Regex keyRegex);

    IEnumerable<KeyValuePair<string, VariableValue>> Filter(IDictionary<string, VariableValue> variables, string keyFilter);

    IEnumerable<VariableGroup> FilterWithoutSecrets(bool filterAsRegex, string variableGroupFilter, IEnumerable<VariableGroup> variableGroups);
}
