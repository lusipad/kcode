using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Kcode.UI;

namespace Kcode.Core;

public class ConfigLoader
{
    public dynamic Load(string path)
    {
        if (!File.Exists(path))
        {
            MessageSystem.ShowWarning($"Config file not found at '{path}'. Using defaults.");
            return GetDefaults();
        }

        try 
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var yaml = File.ReadAllText(path);
            return deserializer.Deserialize<dynamic>(yaml);
        }
        catch (Exception ex)
        {
            MessageSystem.ShowError($"Failed to load config: {ex.Message}. Using defaults.");
            return GetDefaults();
        }
    }

    private dynamic GetDefaults()
    {
        return new Dictionary<string, object>
        {
            { 
                "app", new Dictionary<string, object> 
                { 
                    { "version", "0.1.0-dev" },
                    { "port", "COM3" }
                } 
            },
            {
                "machine", new Dictionary<string, object>
                {
                    { "type", "Virtual" }
                }
            }
        };
    }
}
