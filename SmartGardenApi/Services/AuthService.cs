using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SmartGardenApi.Models;

namespace SmartGardenApi.Services;

public class AuthService
{
    // Em produção, isto estaria no appsettings.json!
    public const string SecretKey = "ChaveSuperSecretaDoSmartGardenApi2025!!!"; 

    // Gera o Token JWT
    // AuthService.cs
public string GenerateToken(User user)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    // De ASCII para UTF8
    var key = Encoding.UTF8.GetBytes(SecretKey); 
    
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()), new Claim("name", user.Username) }),
        Expires = DateTime.UtcNow.AddMinutes(5), // 5 Minutos de validade
        // algoritmo é HmacSha256
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}

    // Hash Simples da Password (SHA256)
    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    // Verificar Password
    public bool VerifyPassword(string inputPassword, string storedHash)
    {
        return HashPassword(inputPassword) == storedHash;
    }
}