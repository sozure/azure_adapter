namespace VGManager.Adapter.Azure.Settings;

public class ReleasePipelineAdapterSettings
{
    public string[] Replacable { get; set; } = [];
    public string[] ExcludableEnvironments { get; set; } = [];
}
