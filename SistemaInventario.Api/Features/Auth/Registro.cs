using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using SistemaInventario.Api.Domain.Entities;
using SistemaInventario.Api.Infrastructure.Database;

namespace SistemaInventario.Api.Features.Auth;

// --- DTOs (Request / Response) ---
public class RegistroRequest
{
    public string Cedula { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
public class RegistroResponse {
    public string Mensaje { get; set; } = string.Empty;

}

// --- Endpoint / Controlador ---
public class RegistroEndpoint {
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/registro", (RegistroRequest request, RegistroHandler handler) =>
        {
            return handler.Handle(request);
        })
        .AllowAnonymous() // Permite que cualquiera acceda sin estar logueado
        .WithTags("Autenticación y Cuentas") // Organiza el endpoint en Swagger
        .WithSummary("Registro autónomo de nuevos usuarios en el sistema")
        .WithDescription("Endpoint público de autoservicio. Descartará cualquier rol enviado e impondrá el rol 'User' por defecto por motivos de seguridad. Valida la Cédula Ecuatoriana (Módulo 10) y hashea la contraseña de forma unidireccional.");
    }
}

// --- L�gica de Negocio (Handler) ---
public class RegistroHandler
{
    private readonly ApplicationDbContext _context;

    // Inyectamos el DbContext que me acabas de mostrar
    public RegistroHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public IResult Handle(RegistroRequest request)
    {
        // 1. Validación matemática de la Cédula Ecuatoriana (Módulo 10)
        if (!ValidarModulo10(request.Cedula))
        {
            return Results.BadRequest(new RegistroResponse { Mensaje = "Cédula inválida." });
        }

        // 2. Control de Unicidad (Email o Cédula duplicados)
        bool existeUsuario = _context.Usuarios.Any(u => u.Email == request.Email || u.Cedula == request.Cedula);
        if (existeUsuario)
        {
            return Results.Conflict(new RegistroResponse { Mensaje = "El correo electrónico o la cédula ya se encuentran registrados." });
        }

        // 3. Cifrado de contraseña y asignación inmutable de Rol
        var nuevoUsuario = new Usuario
        {
            Id = Guid.NewGuid(), // Generamos el UUID requerido por la especificación
            Cedula = request.Cedula,
            Nombre = request.Nombre,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), // Hashing seguro
            Rol = "User" // Prevención de escalabilidad de privilegios
        };

        // 4. Persistencia en la Base de Datos
        _context.Usuarios.Add(nuevoUsuario);
        _context.SaveChanges();

        return Results.Created($"/api/usuarios/{nuevoUsuario.Id}", new RegistroResponse { Mensaje = "Cuenta creada con éxito." });
    }

    // Algoritmo interno para validar la identidad de la cédula
    private bool ValidarModulo10(string cedula)
    {
        if (string.IsNullOrWhiteSpace(cedula) || cedula.Length != 10) return false;
        if (!int.TryParse(cedula.Substring(0, 2), out int provincia) || provincia < 1 || provincia > 24) return false;

        int[] coeficientes = { 2, 1, 2, 1, 2, 1, 2, 1, 2 };
        int suma = 0;

        for (int i = 0; i < 9; i++)
        {
            int digito = int.Parse(cedula[i].ToString());
            int producto = digito * coeficientes[i];
            if (producto >= 10) producto -= 9;
            suma += producto;
        }

        int digitoVerificador = int.Parse(cedula[9].ToString());
        int decenaSuperior = (suma + 9) / 10 * 10;
        int resultado = decenaSuperior - suma;
        if (resultado == 10) resultado = 0;

        return resultado == digitoVerificador;
    }
}