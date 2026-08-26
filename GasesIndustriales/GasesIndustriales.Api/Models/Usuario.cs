namespace GasesIndustriales.Api.Models
{
    public class Usuario
    {
        public int IdUsuario { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Rol { get; set; } = "TRABAJADOR";

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; }
    }
}
