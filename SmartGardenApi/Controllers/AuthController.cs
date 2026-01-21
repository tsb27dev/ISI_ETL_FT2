using Microsoft.AspNetCore.Mvc;
using SmartGardenApi.Data;
using SmartGardenApi.Models;
using SmartGardenApi.Services;
using Microsoft.AspNetCore.Authorization;

namespace SmartGardenApi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IGardenRepository _repo;
    private readonly AuthService _authService;

    public AuthController(IGardenRepository repo, AuthService authService)
    {
        _repo = repo;
        _authService = authService;
    }

    // 1. REGISTAR (Cria Conta)
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(string username, string password)
    {
        if (await _repo.UsernameExistsAsync(username))
            return BadRequest("Username já existe.");

        var user = new User
        {
            Username = username,
            PasswordHash = _authService.HashPassword(password)
        };

        await _repo.CreateUserAsync(user);

        return Ok("Utilizador criado com sucesso.");
    }

    // 2. LOGIN (Retorna o Token)
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string username, string password)
    {
        var user = await _repo.GetUserByUsernameAsync(username);
        
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
        var user = await _repo.GetUserByUsernameAsync(username);
        
        if (user == null || !_authService.VerifyPassword(oldPassword, user.PasswordHash))
            return BadRequest("Dados inválidos.");

        await _repo.UpdateUserPasswordHashAsync(user.Id, _authService.HashPassword(newPassword));

        return Ok("Password atualizada.");
    }

    // 4. ELIMINAR CONTA
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteAccount(string username, string password)
    {
        var user = await _repo.GetUserByUsernameAsync(username);

        if (user == null || !_authService.VerifyPassword(password, user.PasswordHash))
            return BadRequest("Dados inválidos ou utilizador não encontrado.");

        await _repo.DeleteUserAsync(user.Id);

        return Ok("Conta eliminada.");
    }
}