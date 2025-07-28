using FlightsDiggingApp.Properties;

namespace FlightsDiggingApp.Utils
{
    public class Base64BuilderHelper
    {
        internal static void CreateApiPropertiesFile(EnvironmentProperties env)
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "api_properties_values.json");

            if (!File.Exists(filePath))
            {
                var base64 = env.API_PROPERTIES_JSON_BASE64;

                if (!string.IsNullOrEmpty(base64))
                {
                    try
                    {
                        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                        File.WriteAllText(filePath, json);
                        Console.WriteLine($"Created api_properties_values.json at {filePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to create config file: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("API_PROPERTIES_JSON_BASE64 environment variable is missing.");
                }
            }
            else
            { 
                Console.WriteLine($"Config file already exists at {filePath}, skipping creation.");
            }
        }
    }
}
