namespace GasesIndustriales.Api.Dtos.Pedidos
{
    public class DetallePedidoRequest
    {
        public int IdProducto { get; set; }

        public decimal Cantidad { get; set; }

        public decimal? PrecioUnitario { get; set; }
    }
}
