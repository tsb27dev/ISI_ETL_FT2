using SmartGardenApi.Data;
using SmartGardenApi.Models;
using CoreWCF;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace SmartGardenApi.Services.Soap;

[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
public class PlantSoapService : IPlantSoapService
{
    private readonly IGardenRepository _repo;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PlantSoapService(IGardenRepository repo, IHttpContextAccessor httpContextAccessor)
    {
        _repo = repo;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Verifica se o utilizador está autenticado via JWT Bearer token.
    /// Verifica primeiro o HttpContext.User, depois valida manualmente o token do header Authorization.
    /// Lança FaultException se não estiver autenticado.
    /// </summary>
    private void EnsureAuthenticated()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        // Primeiro, verifica se já está autenticado via pipeline do ASP.NET Core
        if (httpContext != null && httpContext.User.Identity?.IsAuthenticated == true)
        {
            return; // Já autenticado
        }

        // Se não estiver autenticado, tenta validar o token manualmente do header
        if (httpContext != null)
        {
            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                if (ValidateToken(token))
                {
                    return; // Token válido
                }
            }
        }

        throw new FaultException("Autenticação necessária. Forneça um Bearer token válido no header Authorization.");
    }

    /// <summary>
    /// Valida manualmente o token JWT.
    /// </summary>
    private bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(Services.AuthService.SecretKey);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            tokenHandler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<Plant>> GetAllPlants()
    {
        EnsureAuthenticated();
        return await _repo.GetPlantsAsync();
    }

    public async Task AddPlant(string name, string location, double humidity)
    {
        EnsureAuthenticated();
        var plant = new Plant 
        { 
            Name = name, 
            Location = location, 
            RequiredHumidity = humidity,
            LastWatered = DateTime.Now 
        };
        
        await _repo.CreatePlantAsync(plant);
    }

    public async Task UpdatePlant(int id, string name, string location, double humidity)
    {
        EnsureAuthenticated();
        
        var existing = await _repo.GetPlantByIdAsync(id);
        if (existing == null) throw new FaultException($"Planta com ID {id} não encontrada.");

        existing.Name = name;
        existing.Location = location;
        existing.RequiredHumidity = humidity;

        var affected = await _repo.UpdatePlantAsync(existing);
        if (affected == 0) throw new FaultException($"Planta com ID {id} não encontrada.");
    }
    
}