namespace GasesIndustriales.Api.Dtos.Usuarios
{
    public class ActualizarUsuarioRequest
    {
        public string Nombre { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Password { get; set; }

        public string Rol { get; set; } = "TRABAJADOR";

        public bool Activo { get; set; } = true;
    }
}
