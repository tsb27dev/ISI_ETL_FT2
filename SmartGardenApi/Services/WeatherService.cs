using System.Text.Json;
using System.Globalization; // <--- 1. IMPORTANTE: Adiciona isto

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
        // 2. CORREÇÃO: Converter para string usando InvariantCulture (usa Ponto em vez de Vírgula)
        string lat = latitude.ToString(CultureInfo.InvariantCulture);
        string lon = longitude.ToString(CultureInfo.InvariantCulture);

        // Calling external API
        // Nota que agora uso as variáveis 'lat' e 'lon' que já são strings formatadas com ponto
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current_weather=true";
        
        try 
        {
            var response = await _httpClient.GetStringAsync(url);
            
            using var doc = JsonDocument.Parse(response);
            
            // É mais seguro verificar se a propriedade existe antes de tentar ler
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