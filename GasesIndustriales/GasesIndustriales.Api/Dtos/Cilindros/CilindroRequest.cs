namespace GasesIndustriales.Api.Dtos.Cilindros
{
    public class CilindroRequest
    {
        public string CodigoCilindro { get; set; } = string.Empty;

        public int IdProducto { get; set; }

        public decimal? Capacidad { get; set; }

        public string PropietarioTipo { get; set; } = "EMPRESA";

        public int? IdClientePropietario { get; set; }

        public string EstadoActual { get; set; } = "LLENO_ALMACEN";

        public string? UbicacionActual { get; set; }

        public bool Activo { get; set; } = true;
    }
}
