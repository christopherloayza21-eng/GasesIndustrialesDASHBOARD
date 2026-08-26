namespace GasesIndustriales.Api.Dtos.Maestros
{
    public class VehiculoRequest
    {
        public string Placa { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;
    }
}
