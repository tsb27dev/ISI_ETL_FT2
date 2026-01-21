using System.Text.Json;
using System.Globalization;

namespace SmartGardenApi.Services;

public class WeatherService
{
    private readonly HttpClient _httpClient;

    public WeatherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetGardenTemperature(double latitude, double longitude)
    {
        string lat = latitude.ToString(CultureInfo.InvariantCulture);
        string lon = longitude.ToString(CultureInfo.InvariantCulture);

        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current_weather=true";
        
        try 
        {
            var response = await _httpClient.GetStringAsync(url);
            
            using var doc = JsonDocument.Parse(response);
            
            if(doc.RootElement.TryGetProperty("current_weather", out var current) && 
               current.TryGetProperty("temperature", out var tempJson))
            {
                var temp = tempJson.GetDouble();
                return $"{temp}°C";
            }
            
            return "N/A";
        }
        catch
        {
            return "Erro ao obter tempo";
        }
    }
}