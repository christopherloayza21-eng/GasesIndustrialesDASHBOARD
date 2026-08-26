namespace GasesIndustriales.Api.Dtos.Maestros
{
    public class ZonaRequest
    {
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;
    }
}
