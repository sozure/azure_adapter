namespace VGManager.Adapter.Api;

public static class Constants
{
    public static class SettingKeys
    {
        public const string HealthChecksSettings = nameof(HealthChecksSettings);
        public const string VGManagerAdapterCommandResponseProducer = nameof(VGManagerAdapterCommandResponseProducer);
        public const string VGManagerAdapterCommandResponseConsumer = nameof(VGManagerAdapterCommandResponseConsumer);
    }

    public static class Cors
    {
        public static string AllowSpecificOrigins { get; set; } = "_allowSpecificOrigins";
    }
}
