using GasesIndustriales.Api.Data;
using GasesIndustriales.Api.Dtos.Usuarios;
using GasesIndustriales.Api.Models;
using GasesIndustriales.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GasesIndustriales.Api.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ISystemClock _clock;

        public UsuariosController(AppDbContext context, IPasswordHasher passwordHasher, ISystemClock clock)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _clock = clock;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsuarios([FromQuery] bool incluirInactivos = false)
        {
            var query = _context.Usuarios.AsNoTracking();

            if (!incluirInactivos)
            {
                query = query.Where(usuario => usuario.Activo);
            }

            var usuarios = await query
                .OrderBy(usuario => usuario.Nombre)
                .Select(usuario => ToResponseDto(usuario))
                .ToListAsync();

            return Ok(usuarios);
        }

        [HttpPost]
        public async Task<IActionResult> CrearUsuario(CrearUsuarioRequest request)
        {
            var validacion = ValidarUsuario(request.Nombre, request.Email, request.Rol, request.Password, passwordObligatorio: true);

            if (validacion is not null)
            {
                return validacion;
            }

            var email = NormalizarEmail(request.Email);

            if (await ExisteEmail(email))
            {
                return Conflict("Ya existe un usuario con ese email.");
            }

            var usuario = new Usuario
            {
                Nombre = request.Nombre.Trim(),
                Email = email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                Rol = NormalizarRol(request.Rol),
                Activo = true,
                FechaCreacion = _clock.UtcNow
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsuarios), new { id = usuario.IdUsuario }, ToResponseDto(usuario));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarUsuario(int id, ActualizarUsuarioRequest request)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario is null)
            {
                return NotFound();
            }

            var validacion = ValidarUsuario(request.Nombre, request.Email, request.Rol, request.Password, passwordObligatorio: false);

            if (validacion is not null)
            {
                return validacion;
            }

            var email = NormalizarEmail(request.Email);

            if (await ExisteEmail(email, id))
            {
                return Conflict("Ya existe otro usuario con ese email.");
            }

            usuario.Nombre = request.Nombre.Trim();
            usuario.Email = email;
            usuario.Rol = NormalizarRol(request.Rol);
            usuario.Activo = request.Activo;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                usuario.PasswordHash = _passwordHasher.Hash(request.Password);
            }

            await _context.SaveChangesAsync();

            return Ok(ToResponseDto(usuario));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DesactivarUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario is null)
            {
                return NotFound();
            }

            usuario.Activo = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id:int}/reactivar")]
        public async Task<IActionResult> ReactivarUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario is null)
            {
                return NotFound();
            }

            usuario.Activo = true;
            await _context.SaveChangesAsync();

            return Ok(ToResponseDto(usuario));
        }

        private async Task<bool> ExisteEmail(string email, int? idUsuarioIgnorado = null)
        {
            return await _context.Usuarios.AnyAsync(usuario =>
                usuario.Email == email
                && (!idUsuarioIgnorado.HasValue || usuario.IdUsuario != idUsuarioIgnorado.Value));
        }

        private IActionResult? ValidarUsuario(string nombre, string email, string rol, string? password, bool passwordObligatorio)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return BadRequest("El nombre del usuario es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return BadRequest("El email del usuario no es válido.");
            }

            if (NormalizarRol(rol) is not "ADMINISTRADOR" and not "TRABAJADOR")
            {
                return BadRequest("El rol debe ser ADMINISTRADOR o TRABAJADOR.");
            }

            if (passwordObligatorio && string.IsNullOrWhiteSpace(password))
            {
                return BadRequest("La contraseña es obligatoria.");
            }

            if (!string.IsNullOrWhiteSpace(password) && password.Length < 8)
            {
                return BadRequest("La contraseña debe tener al menos 8 caracteres.");
            }

            return null;
        }

        private static string NormalizarEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private static string NormalizarRol(string rol)
        {
            return rol.Trim().ToUpperInvariant();
        }

        private static UsuarioResponseDto ToResponseDto(Usuario usuario)
        {
            return new UsuarioResponseDto
            {
                IdUsuario = usuario.IdUsuario,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol,
                Activo = usuario.Activo,
                FechaCreacion = usuario.FechaCreacion
            };
        }
    }
}
