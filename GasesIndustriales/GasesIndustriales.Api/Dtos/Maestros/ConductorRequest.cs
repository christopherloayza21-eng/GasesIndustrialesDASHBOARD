namespace GasesIndustriales.Api.Dtos.Maestros
{
    public class ConductorRequest
    {
        public string Nombre { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        public bool Activo { get; set; } = true;
    }
}
