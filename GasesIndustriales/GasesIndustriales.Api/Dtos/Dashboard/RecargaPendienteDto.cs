namespace GasesIndustriales.Api.Dtos.Dashboard
{
    public class RecargaPendienteDto
    {
        public int IdEnvio { get; set; }

        public string Proveedor { get; set; } = string.Empty;

        public string? NumeroGuia { get; set; }

        public DateTime FechaEnvio { get; set; }

        public int Pendientes { get; set; }
    }
}
