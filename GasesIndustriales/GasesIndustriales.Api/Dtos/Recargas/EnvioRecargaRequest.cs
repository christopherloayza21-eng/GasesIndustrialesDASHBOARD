namespace GasesIndustriales.Api.Dtos.Recargas
{
    public class EnvioRecargaRequest
    {
        public int IdProveedor { get; set; }

        public string? NumeroGuia { get; set; }

        public string? Observaciones { get; set; }

        public List<int> CilindroIds { get; set; } = new();
    }
}
