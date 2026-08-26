namespace GasesIndustriales.Api.Dtos.Pedidos
{
    public class PedidoRequest
    {
        public int IdCliente { get; set; }

        public string? DireccionEntrega { get; set; }

        public int? IdZona { get; set; }

        public int? IdConductor { get; set; }

        public int? IdVehiculo { get; set; }

        public string? Observaciones { get; set; }

        public List<DetallePedidoRequest> Detalles { get; set; } = new();
    }
}
