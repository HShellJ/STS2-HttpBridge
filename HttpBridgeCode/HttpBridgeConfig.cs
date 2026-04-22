using System;
using System.IO;
using System.Text.Json;

namespace STS2HttpBridge.HttpBridgeCode;

internal class HttpBridgeConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 8080;
    public string ApiKey { get; set; } = "";
    public bool EnableCors { get; set; } = true;
    public string AllowedOrigins { get; set; } = "*";
    public int StateCacheDurationMs { get; set; } = 100;

    private static readonly object _lock = new();
    private static HttpBridgeConfig? _current;

    public static HttpBridgeConfig Load()
    {
        lock (_lock)
        {
            if (_current != null)
                return _current;

            string configPath = GetConfigPath();
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    _current = JsonSerializer.Deserialize<HttpBridgeConfig>(json);
                    if (_current != null)
                        return _current;
                }
                catch (Exception ex)
                {
                    BridgeTrace.Log($"Failed to load config: {ex.Message}");
                }
            }

            // Create default config
            _current = new HttpBridgeConfig();
            Save(_current);
            return _current;
        }
    }

    public static void Save(HttpBridgeConfig config)
    {
        lock (_lock)
        {
            try
            {
                string configPath = GetConfigPath();
                string? directory = Path.GetDirectoryName(configPath);
                if (directory != null && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
                _current = config;
            }
            catch (Exception ex)
            {
                BridgeTrace.Log($"Failed to save config: {ex.Message}");
            }
        }
    }

    public static void Reload()
    {
        lock (_lock)
        {
            _current = null;
            Load();
        }
    }

    private static string GetConfigPath()
    {
        // Use same base path as HermesBridge for consistency
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "SlayTheSpire2", "httpbridge", "config.json");
    }
}