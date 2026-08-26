using GasesIndustriales.Api.Data;
using GasesIndustriales.Api.Dtos.Pedidos;
using GasesIndustriales.Api.Models;
using GasesIndustriales.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GasesIndustriales.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ISystemClock _clock;

        public PedidosController(AppDbContext context, ISystemClock clock)
        {
            _context = context;
            _clock = clock;
        }

        [HttpGet]
        public async Task<IActionResult> GetPedidos([FromQuery] string? estado = null)
        {
            var query = _context.Pedidos.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(estado))
            {
                var estadoNormalizado = estado.Trim().ToUpperInvariant();
                query = query.Where(pedido => pedido.EstadoPedido == estadoNormalizado);
            }

            var pedidos = await (
                from pedido in query
                join cliente in _context.Clientes.AsNoTracking()
                    on pedido.IdCliente equals cliente.IdCliente
                orderby pedido.FechaPedido descending
                select new
                {
                    pedido.IdPedido,
                    pedido.FechaPedido,
                    pedido.IdCliente,
                    Cliente = cliente.RazonSocial,
                    pedido.DireccionEntrega,
                    pedido.IdZona,
                    pedido.IdConductor,
                    pedido.IdVehiculo,
                    pedido.EstadoPedido,
                    pedido.Observaciones,
                    Total = _context.DetallesPedido
                        .Where(detalle => detalle.IdPedido == pedido.IdPedido)
                        .Sum(detalle => detalle.Subtotal ?? 0)
                })
                .ToListAsync();

            return Ok(pedidos);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPedidoPorId(int id)
        {
            var pedido = await (
                from item in _context.Pedidos.AsNoTracking()
                join cliente in _context.Clientes.AsNoTracking()
                    on item.IdCliente equals cliente.IdCliente
                where item.IdPedido == id
                select new
                {
                    item.IdPedido,
                    item.FechaPedido,
                    item.IdCliente,
                    Cliente = cliente.RazonSocial,
                    item.DireccionEntrega,
                    item.IdZona,
                    item.IdConductor,
                    item.IdVehiculo,
                    item.EstadoPedido,
                    item.Observaciones
                })
                .FirstOrDefaultAsync();

            if (pedido is null)
            {
                return NotFound();
            }

            var detalles = await GetDetalles(id);
            var total = detalles.Sum(detalle => detalle.Subtotal ?? 0);

            return Ok(new
            {
                Pedido = pedido,
                Detalles = detalles,
                Total = total
            });
        }

        [HttpPost]
        public async Task<IActionResult> CrearPedido(PedidoRequest request)
        {
            var validacion = await ValidarPedido(request);

            if (validacion is not null)
            {
                return validacion;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var pedido = new Pedido
            {
                IdCliente = request.IdCliente,
                FechaPedido = _clock.UtcNow,
                DireccionEntrega = Normalizar(request.DireccionEntrega),
                IdZona = request.IdZona,
                IdConductor = request.IdConductor,
                IdVehiculo = request.IdVehiculo,
                EstadoPedido = "PENDIENTE",
                Observaciones = Normalizar(request.Observaciones)
            };

            try
            {
                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                await GuardarDetalles(pedido.IdPedido, request.Detalles);
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetPedidoPorId), new { id = pedido.IdPedido }, new { pedido.IdPedido });
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                return BadRequest($"No se pudo guardar el pedido. {ObtenerMensajeBaseDatos(ex)}");
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarPedido(int id, PedidoRequest request)
        {
            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido is null)
            {
                return NotFound();
            }

            if (pedido.EstadoPedido != "PENDIENTE")
            {
                return BadRequest("Solo se puede editar un pedido en estado PENDIENTE.");
            }

            var validacion = await ValidarPedido(request);

            if (validacion is not null)
            {
                return validacion;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            pedido.IdCliente = request.IdCliente;
            pedido.DireccionEntrega = Normalizar(request.DireccionEntrega);
            pedido.IdZona = request.IdZona;
            pedido.IdConductor = request.IdConductor;
            pedido.IdVehiculo = request.IdVehiculo;
            pedido.Observaciones = Normalizar(request.Observaciones);

            try
            {
                var detallesActuales = _context.DetallesPedido.Where(detalle => detalle.IdPedido == id);
                _context.DetallesPedido.RemoveRange(detallesActuales);
                await GuardarDetalles(id, request.Detalles);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { pedido.IdPedido });
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                return BadRequest($"No se pudo actualizar el pedido. {ObtenerMensajeBaseDatos(ex)}");
            }
        }

        [HttpPatch("{id:int}/asignacion")]
        public async Task<IActionResult> AsignarReparto(int id, AsignacionPedidoRequest request)
        {
            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido is null)
            {
                return NotFound();
            }

            if (request.IdConductor.HasValue && !await _context.Conductores.AnyAsync(item => item.IdConductor == request.IdConductor && item.Activo))
            {
                return BadRequest("El conductor no existe o está inactivo.");
            }

            if (request.IdVehiculo.HasValue && !await _context.Vehiculos.AnyAsync(item => item.IdVehiculo == request.IdVehiculo && item.Activo))
            {
                return BadRequest("El vehículo no existe o está inactivo.");
            }

            pedido.IdConductor = request.IdConductor;
            pedido.IdVehiculo = request.IdVehiculo;
            await _context.SaveChangesAsync();

            return Ok(pedido);
        }

        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, CambiarEstadoPedidoRequest request)
        {
            var estado = request.Estado.Trim().ToUpperInvariant();

            if (estado is not "PENDIENTE" and not "EN_REPARTO" and not "ENTREGADO" and not "CANCELADO")
            {
                return BadRequest("Estado de pedido inválido.");
            }

            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido is null)
            {
                return NotFound();
            }

            pedido.EstadoPedido = estado;
            await _context.SaveChangesAsync();

            return Ok(pedido);
        }

        [HttpPatch("{id:int}/cancelar")]
        public async Task<IActionResult> CancelarPedido(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido is null)
            {
                return NotFound();
            }

            if (pedido.EstadoPedido == "ENTREGADO")
            {
                return BadRequest("No se puede cancelar un pedido ya entregado.");
            }

            pedido.EstadoPedido = "CANCELADO";
            await _context.SaveChangesAsync();

            return Ok(pedido);
        }

        private async Task<IActionResult?> ValidarPedido(PedidoRequest request)
        {
            if (!await _context.Clientes.AnyAsync(cliente => cliente.IdCliente == request.IdCliente && cliente.Activo))
            {
                return BadRequest("El cliente no existe o está inactivo.");
            }

            if (request.IdZona.HasValue && !await _context.Zonas.AnyAsync(zona => zona.IdZona == request.IdZona && zona.Activo))
            {
                return BadRequest("La zona no existe o está inactiva.");
            }

            if (request.IdConductor.HasValue && !await _context.Conductores.AnyAsync(conductor => conductor.IdConductor == request.IdConductor && conductor.Activo))
            {
                return BadRequest("El conductor no existe o está inactivo.");
            }

            if (request.IdVehiculo.HasValue && !await _context.Vehiculos.AnyAsync(vehiculo => vehiculo.IdVehiculo == request.IdVehiculo && vehiculo.Activo))
            {
                return BadRequest("El vehículo no existe o está inactivo.");
            }

            if (request.Detalles.Count == 0)
            {
                return BadRequest("El pedido debe tener al menos un producto.");
            }

            foreach (var detalle in request.Detalles)
            {
                if (detalle.Cantidad <= 0)
                {
                    return BadRequest("La cantidad debe ser mayor a cero.");
                }

                if (detalle.PrecioUnitario is < 0)
                {
                    return BadRequest("El precio no puede ser negativo.");
                }

                if (!await _context.Productos.AnyAsync(producto => producto.IdProducto == detalle.IdProducto && producto.Activo))
                {
                    return BadRequest($"El producto {detalle.IdProducto} no existe o está inactivo.");
                }
            }

            return null;
        }

        private async Task GuardarDetalles(int idPedido, List<DetallePedidoRequest> detalles)
        {
            foreach (var detalle in detalles)
            {
                decimal? subtotal = detalle.PrecioUnitario.HasValue
                    ? detalle.Cantidad * detalle.PrecioUnitario.Value
                    : null;

                _context.DetallesPedido.Add(new DetallePedido
                {
                    IdPedido = idPedido,
                    IdProducto = detalle.IdProducto,
                    Cantidad = detalle.Cantidad,
                    PrecioUnitario = detalle.PrecioUnitario,
                    Subtotal = subtotal
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task<List<DetallePedidoResponse>> GetDetalles(int idPedido)
        {
            return await (
                from detalle in _context.DetallesPedido.AsNoTracking()
                join producto in _context.Productos.AsNoTracking()
                    on detalle.IdProducto equals producto.IdProducto
                where detalle.IdPedido == idPedido
                select new DetallePedidoResponse
                {
                    IdDetalle = detalle.IdDetalle,
                    IdProducto = detalle.IdProducto,
                    Producto = producto.Nombre,
                    Cantidad = detalle.Cantidad,
                    PrecioUnitario = detalle.PrecioUnitario,
                    Subtotal = detalle.Subtotal
                })
                .ToListAsync();
        }

        private static string? Normalizar(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        private static string ObtenerMensajeBaseDatos(DbUpdateException ex)
        {
            return ex.InnerException?.Message ?? ex.Message;
        }
    }

}
