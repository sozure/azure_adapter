namespace VGManager.Adapter.Azure.Settings;

public class GitRepositoryAdapterSettings
{
    public required char StartingChar { get; set; }
    public required char EndingChar { get; set; }
    public required string SecretYamlKind { get; set; }
    public required string SecretYamlElement { get; set; }
    public required string VariableYamlKind { get; set; }
    public required string VariableYamlElement { get; set; }
}
