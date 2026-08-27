using GasesIndustriales.Api.Data;
using GasesIndustriales.Api.Dtos.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GasesIndustriales.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen([FromQuery] string? tipoMovimiento = null, [FromQuery] string? estadoCilindro = null)
        {
            var tipoMovimientoNormalizado = NormalizarFiltro(tipoMovimiento);
            var estadoCilindroNormalizado = NormalizarFiltro(estadoCilindro);

            var cilindrosDisponibles = await _context.Cilindros
                .AsNoTracking()
                .CountAsync(cilindro =>
                    cilindro.Activo
                    && (cilindro.EstadoActual == "LLENO_ALMACEN" || cilindro.EstadoActual == "VACIO_ALMACEN"));

            var cilindrosEnClientes = await _context.Cilindros
                .AsNoTracking()
                .CountAsync(cilindro => cilindro.Activo && cilindro.EstadoActual == "EN_CLIENTE");

            var cilindrosEnProveedor = await _context.Cilindros
                .AsNoTracking()
                .CountAsync(cilindro =>
                    cilindro.Activo
                    && (cilindro.EstadoActual == "EN_PROVEEDOR" || cilindro.EstadoActual == "EN_RECARGA"));

            var pedidosPendientes = await _context.Pedidos
                .AsNoTracking()
                .CountAsync(pedido => pedido.EstadoPedido == "PENDIENTE" || pedido.EstadoPedido == "ASIGNADO");

            var movimientosQuery =
                from movimiento in _context.MovimientosCilindro.AsNoTracking()
                join cilindro in _context.Cilindros.AsNoTracking()
                    on movimiento.IdCilindro equals cilindro.IdCilindro
                join producto in _context.Productos.AsNoTracking()
                    on cilindro.IdProducto equals producto.IdProducto
                join cliente in _context.Clientes.AsNoTracking()
                    on movimiento.IdCliente equals cliente.IdCliente into clientes
                from cliente in clientes.DefaultIfEmpty()
                select new
                {
                    movimiento.IdMovimiento,
                    cilindro.CodigoCilindro,
                    Producto = producto.Nombre,
                    movimiento.TipoMovimiento,
                    movimiento.FechaMovimiento,
                    Cliente = cliente != null ? cliente.RazonSocial : null,
                    movimiento.Observacion,
                    cilindro.EstadoActual
                };

            if (!string.IsNullOrWhiteSpace(tipoMovimientoNormalizado))
            {
                movimientosQuery = movimientosQuery.Where(item => item.TipoMovimiento == tipoMovimientoNormalizado);
            }

            if (!string.IsNullOrWhiteSpace(estadoCilindroNormalizado))
            {
                movimientosQuery = movimientosQuery.Where(item => item.EstadoActual == estadoCilindroNormalizado);
            }

            var movimientosRecientes = await (
                from item in movimientosQuery
                orderby item.FechaMovimiento descending
                select new MovimientoRecienteDto
                {
                    IdMovimiento = item.IdMovimiento,
                    CodigoCilindro = item.CodigoCilindro,
                    Producto = item.Producto,
                    TipoMovimiento = item.TipoMovimiento,
                    FechaMovimiento = item.FechaMovimiento,
                    Cliente = item.Cliente,
                    Observacion = item.Observacion
                })
                .Take(10)
                .ToListAsync();

            var estadosCilindros = await _context.Cilindros
                .AsNoTracking()
                .Where(cilindro => cilindro.Activo)
                .GroupBy(cilindro => cilindro.EstadoActual)
                .Select(grupo => new EstadoCilindroDto
                {
                    Estado = grupo.Key,
                    Total = grupo.Count()
                })
                .OrderBy(item => item.Estado)
                .ToListAsync();

            var clientesConCilindros = await (
                from cilindro in _context.Cilindros.AsNoTracking()
                join cliente in _context.Clientes.AsNoTracking()
                    on cilindro.UbicacionActual equals cliente.RazonSocial
                where cilindro.Activo && cilindro.EstadoActual == "EN_CLIENTE"
                group cilindro by new { cliente.IdCliente, cliente.RazonSocial } into grupo
                orderby grupo.Count() descending
                select new ClienteConCilindrosDto
                {
                    IdCliente = grupo.Key.IdCliente,
                    Cliente = grupo.Key.RazonSocial,
                    TotalCilindros = grupo.Count(),
                    UltimoMovimiento = grupo.Max(item => item.FechaUltimoMovimiento).HasValue
                        ? grupo.Max(item => item.FechaUltimoMovimiento)!.Value.ToString("yyyy-MM-dd HH:mm")
                        : null
                })
                .Take(8)
                .ToListAsync();

            var recargasPendientes = await (
                from envio in _context.EnviosRecarga.AsNoTracking()
                join proveedor in _context.Proveedores.AsNoTracking()
                    on envio.IdProveedor equals proveedor.IdProveedor
                let pendientes = _context.DetallesEnvioRecarga.Count(detalle =>
                    detalle.IdEnvio == envio.IdEnvio
                    && detalle.EstadoRetorno == "PENDIENTE")
                where pendientes > 0
                orderby envio.FechaEnvio
                select new RecargaPendienteDto
                {
                    IdEnvio = envio.IdEnvio,
                    Proveedor = proveedor.RazonSocial,
                    NumeroGuia = envio.NumeroGuia,
                    FechaEnvio = envio.FechaEnvio,
                    Pendientes = pendientes
                })
                .Take(8)
                .ToListAsync();

            var resumen = new DashboardResumenDto
            {
                CilindrosDisponibles = cilindrosDisponibles,
                CilindrosEnClientes = cilindrosEnClientes,
                CilindrosEnProveedor = cilindrosEnProveedor,
                PedidosPendientes = pedidosPendientes,
                MovimientosRecientes = movimientosRecientes,
                EstadosCilindros = estadosCilindros,
                ClientesConCilindros = clientesConCilindros,
                RecargasPendientes = recargasPendientes
            };

            return Ok(resumen);
        }

        private static string? NormalizarFiltro(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim().ToUpperInvariant();
        }
    }
}
