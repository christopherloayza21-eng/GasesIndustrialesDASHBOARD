using GasesIndustriales.Api.Dtos.Auth;

namespace GasesIndustriales.Api.Services
{
    public interface IAuthService
    {
        Task<LoginResponse?> Login(LoginRequest request);
    }
}
