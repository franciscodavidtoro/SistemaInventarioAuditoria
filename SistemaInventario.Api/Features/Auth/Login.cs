using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SistemaInventario.Api.Infrastructure.Database;

namespace SistemaInventario.Api.Features.Auth;

// --- DTOs (Request / Response) ---
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
}

// --- Endpoint / Controlador ---
public static class LoginEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", (LoginRequest request, LoginHandler handler) =>
        {
            return handler.Handle(request);
        })
        .AllowAnonymous()
        .WithTags("Autenticación y Cuentas")
        .WithSummary("Autenticar credenciales de usuario y emitir token JWT")
        .WithDescription("Valida el correo y la contraseña contra el hash de la base de datos. Si tiene éxito, devuelve un token stateless firmado.");
    }
}

// --- Lógica de Negocio (Handler) ---
public class LoginHandler
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    // Aquí inyectamos la base de datos y la configuración (para leer el appsettings.json)
    public LoginHandler(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public IResult Handle(LoginRequest request)
    {
        // 1. Buscar al usuario por su correo electrónico
        var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == request.Email);

        // 2. Verificar que el usuario exista y que la contraseña coincida con el Hash usando BCrypt
        if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
        {
            return Results.Unauthorized(); // Retorna 401 si las credenciales son incorrectas
        }

        // 3. Generación del Token JWT usando la estructura del equipo
        var tokenHandler = new JwtSecurityTokenHandler();

        // Obtenemos los valores navegando dentro del bloque "JwtSettings"
        var secretKey = _configuration["JwtSettings:SecretKey"];
        var issuer = _configuration["JwtSettings:Issuer"];
        var audience = _configuration["JwtSettings:Audience"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("La clave secreta del JWT no está configurada.");
        }

        var key = Encoding.ASCII.GetBytes(secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email)
            }),
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = issuer,     
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);
        return Results.Ok(new LoginResponse { Token = tokenString });
    }
}