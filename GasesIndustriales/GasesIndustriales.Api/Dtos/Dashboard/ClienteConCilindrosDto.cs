namespace GasesIndustriales.Api.Dtos.Dashboard
{
    public class ClienteConCilindrosDto
    {
        public int IdCliente { get; set; }

        public string Cliente { get; set; } = string.Empty;

        public int TotalCilindros { get; set; }

        public string? UltimoMovimiento { get; set; }
    }
}
