namespace GasesIndustriales.Api.Dtos.Movimientos
{
    public class MovimientoRequest
    {
        public int IdCilindro { get; set; }

        public int? IdPedido { get; set; }

        public int? IdCliente { get; set; }

        public int? IdConductor { get; set; }

        public int? IdVehiculo { get; set; }

        public string? Observacion { get; set; }
    }
}
