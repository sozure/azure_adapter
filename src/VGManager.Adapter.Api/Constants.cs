namespace VGManager.Adapter.Api;

public static class Constants
{
    public static class SettingKeys
    {
        public const string HealthChecksSettings = nameof(HealthChecksSettings);
        public const string VGManagerAdapterCommandResponseProducer = nameof(VGManagerAdapterCommandResponseProducer);
        public const string VGManagerAdapterCommandConsumer = nameof(VGManagerAdapterCommandConsumer);
        public const string GitRepositoryAdapterSettings = nameof(GitRepositoryAdapterSettings);
        public const string ReleasePipelineAdapterSettings = nameof(ReleasePipelineAdapterSettings);
        public const string ExtensionSettings = nameof(ExtensionSettings);
    }

    public static class Cors
    {
        public static string AllowSpecificOrigins { get; set; } = "_allowSpecificOrigins";
    }
}
