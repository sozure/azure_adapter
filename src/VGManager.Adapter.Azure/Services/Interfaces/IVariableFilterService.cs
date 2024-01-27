using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System.Text.RegularExpressions;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IVariableFilterService
{
    IEnumerable<VariableGroup> Filter(IEnumerable<VariableGroup> variableGroups, string filter);

    IEnumerable<KeyValuePair<string, VariableValue>> Filter(IDictionary<string, VariableValue> variables, Regex regex);

    IEnumerable<KeyValuePair<string, VariableValue>> Filter(IDictionary<string, VariableValue> variables, string filter);

    IEnumerable<VariableGroup> FilterWithoutSecrets(bool filterAsRegex, string filter, IEnumerable<VariableGroup> variableGroups);
}
