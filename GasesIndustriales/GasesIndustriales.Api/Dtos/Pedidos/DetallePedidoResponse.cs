namespace GasesIndustriales.Api.Dtos.Pedidos
{
    public class DetallePedidoResponse
    {
        public int IdDetalle { get; set; }

        public int IdProducto { get; set; }

        public string Producto { get; set; } = string.Empty;

        public decimal Cantidad { get; set; }

        public decimal? PrecioUnitario { get; set; }

        public decimal? Subtotal { get; set; }
    }
}
