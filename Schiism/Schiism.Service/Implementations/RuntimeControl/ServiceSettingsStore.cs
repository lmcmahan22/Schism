using Schiism.Core.Abstractions.RuntimeControl;
using Schiism.Core.Models.RuntimeControl;
using System.Text.Json;

namespace Schiism.Service.Implementations.RuntimeControl
{
    public class ServiceSettingsStore : IServiceSettingsStore
    {
        private readonly string filePath =
            Path.Combine(AppContext.BaseDirectory, "serviceSettings.json");

        public ServiceRuntimeSettings Load()
        {
            if (!File.Exists(filePath))
            {
                return new ServiceRuntimeSettings
                {
                    AutoStart = true,
                    AutoRestart = false,
                };
            }

            string json = File.ReadAllText(filePath);

            return JsonSerializer.Deserialize<ServiceRuntimeSettings>(json)!;
        }

        public void Save(ServiceRuntimeSettings settings)
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(filePath, json);
        }
    }
}
