using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GasesIndustriales.Api.Data;
using GasesIndustriales.Api.Dtos.Auth;
using GasesIndustriales.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GasesIndustriales.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ISystemClock _clock;

        public AuthService(
            AppDbContext context,
            IConfiguration configuration,
            IPasswordHasher passwordHasher,
            ISystemClock clock)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = passwordHasher;
            _clock = clock;
        }

        public async Task<LoginResponse?> Login(LoginRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Email == email && item.Activo);

            if (usuario is null || !_passwordHasher.Verify(request.Password, usuario.PasswordHash))
            {
                return null;
            }

            return CrearRespuestaLogin(usuario);
        }

        private LoginResponse CrearRespuestaLogin(Usuario usuario)
        {
            var issuer = _configuration["Jwt:Issuer"] ?? "GasesIndustriales.Api";
            var audience = _configuration["Jwt:Audience"] ?? "GasesIndustriales.Frontend";
            var secret = _configuration["Jwt:Secret"];

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("Falta configurar Jwt:Secret en User Secrets.");
            }

            var expirationMinutes = int.TryParse(_configuration["Jwt:ExpirationMinutes"], out var minutes)
                ? minutes
                : 120;

            var expires = _clock.UtcNow.AddMinutes(expirationMinutes);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, usuario.IdUsuario.ToString()),
                new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new(JwtRegisteredClaimNames.Email, usuario.Email),
                new(ClaimTypes.Email, usuario.Email),
                new(ClaimTypes.Name, usuario.Nombre),
                new(ClaimTypes.Role, usuario.Rol)
            };

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: expires,
                signingCredentials: credentials);

            return new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiraEn = expires,
                Usuario = new UsuarioSesionDto
                {
                    IdUsuario = usuario.IdUsuario,
                    Nombre = usuario.Nombre,
                    Email = usuario.Email,
                    Rol = usuario.Rol
                }
            };
        }
    }
}
