namespace GasesIndustriales.Api.Dtos.Auth
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiraEn { get; set; }

        public UsuarioSesionDto Usuario { get; set; } = new();
    }
}
