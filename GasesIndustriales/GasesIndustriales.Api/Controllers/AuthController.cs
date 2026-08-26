using System.Security.Claims;
using GasesIndustriales.Api.Dtos.Auth;
using GasesIndustriales.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GasesIndustriales.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email y contraseña son obligatorios.");
            }

            var response = await _authService.Login(request);

            if (response is null)
            {
                return Unauthorized("Credenciales incorrectas.");
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new UsuarioSesionDto
            {
                IdUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "0"),
                Nombre = User.Identity?.Name ?? string.Empty,
                Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                Rol = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty
            });
        }
    }
}
