using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGardenApi.Data;
using SmartGardenApi.Models;
using SmartGardenApi.Services;

namespace SmartGardenApi.Controllers;

[ApiController]
[Route("[controller]")] // Fica /auth
public class AuthController : ControllerBase
{
    private readonly GardenContext _context;
    private readonly AuthService _authService;

    public AuthController(GardenContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    // 1. REGISTAR (Cria Conta)
    [HttpPost("register")]
    public async Task<IActionResult> Register(string username, string password)
    {
        if (await _context.Users.AnyAsync(u => u.Username == username))
            return BadRequest("Username já existe.");

        var user = new User
        {
            Username = username,
            PasswordHash = _authService.HashPassword(password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok("Utilizador criado com sucesso.");
    }

    // 2. LOGIN (Retorna o Token)
    [HttpPost("login")]
    public async Task<IActionResult> Login(string username, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        
        if (user == null || !_authService.VerifyPassword(password, user.PasswordHash))
            return Unauthorized("Username ou Password incorretos.");

        var token = _authService.GenerateToken(user);
        
        // Retorna o token num objeto JSON
        return Ok(new { Token = token });
    }

    // 3. ALTERAR PASSWORD
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(string username, string oldPassword, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        
        if (user == null || !_authService.VerifyPassword(oldPassword, user.PasswordHash))
            return BadRequest("Dados inválidos.");

        user.PasswordHash = _authService.HashPassword(newPassword);
        await _context.SaveChangesAsync();

        return Ok("Password atualizada.");
    }

    // 4. ELIMINAR CONTA
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteAccount(string username, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null || !_authService.VerifyPassword(password, user.PasswordHash))
            return BadRequest("Dados inválidos ou utilizador não encontrado.");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok("Conta eliminada.");
    }
}