namespace Schiism.Core.Configuration.FileControl
{
    using Schiism.Core.Configuration.StateControl;
    using System.Text.Json;

    /// <summary>
    /// Seperate class, because the user doesn't actively save/load these settings. They are automatically loaded when the app is opened. They are automatically saved when the user modifies them.
    /// </summary>
    public class ServiceSettingsStore
    {
        private readonly string filePath =
            Path.Combine(AppContext.BaseDirectory, "serviceSettings.json");

        // This should use the ConfigState, since that's where these two settings are stored now.
        public ServiceSaveData Load()
        {
            if (!File.Exists(filePath))
            {
                return new ServiceSaveData(true, true);
            }

            string json = File.ReadAllText(filePath);

            return JsonSerializer.Deserialize<ServiceSaveData>(json)!;
        }

        public void Save(ServiceSaveData ssd)
        {
            string json = JsonSerializer.Serialize(ssd, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(filePath, json);
        }
    }
}
