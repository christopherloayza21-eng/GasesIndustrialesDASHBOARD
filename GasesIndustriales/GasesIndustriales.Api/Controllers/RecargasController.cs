using GasesIndustriales.Api.Data;
using GasesIndustriales.Api.Dtos.Recargas;
using GasesIndustriales.Api.Models;
using GasesIndustriales.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GasesIndustriales.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecargasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ISystemClock _clock;

        public RecargasController(AppDbContext context, ISystemClock clock)
        {
            _context = context;
            _clock = clock;
        }

        [HttpGet("envios")]
        public async Task<IActionResult> GetEnvios()
        {
            var envios = await (
                from envio in _context.EnviosRecarga.AsNoTracking()
                join proveedor in _context.Proveedores.AsNoTracking()
                    on envio.IdProveedor equals proveedor.IdProveedor
                orderby envio.FechaEnvio descending
                select new
                {
                    envio.IdEnvio,
                    envio.IdProveedor,
                    Proveedor = proveedor.RazonSocial,
                    envio.FechaEnvio,
                    envio.NumeroGuia,
                    envio.Estado,
                    envio.Observaciones,
                    Pendientes = _context.DetallesEnvioRecarga.Count(detalle => detalle.IdEnvio == envio.IdEnvio && detalle.EstadoRetorno == "PENDIENTE")
                })
                .ToListAsync();

            return Ok(envios);
        }

        [HttpGet("envios/{id:int}")]
        public async Task<IActionResult> GetEnvioPorId(int id)
        {
            var envio = await _context.EnviosRecarga.AsNoTracking().FirstOrDefaultAsync(item => item.IdEnvio == id);

            if (envio is null)
            {
                return NotFound();
            }

            var detalles = await (
                from detalle in _context.DetallesEnvioRecarga.AsNoTracking()
                join cilindro in _context.Cilindros.AsNoTracking()
                    on detalle.IdCilindro equals cilindro.IdCilindro
                where detalle.IdEnvio == id
                orderby cilindro.CodigoCilindro
                select new
                {
                    detalle.IdDetalleEnvio,
                    detalle.IdCilindro,
                    cilindro.CodigoCilindro,
                    detalle.FechaRetorno,
                    detalle.EstadoRetorno,
                    detalle.Observacion
                })
                .ToListAsync();

            return Ok(new { Envio = envio, Cilindros = detalles });
        }

        [HttpPost("envios")]
        public async Task<IActionResult> CrearEnvio(EnvioRecargaRequest request)
        {
            if (!await _context.Proveedores.AnyAsync(proveedor => proveedor.IdProveedor == request.IdProveedor && proveedor.Activo))
            {
                return BadRequest("El proveedor no existe o está inactivo.");
            }

            if (request.CilindroIds.Count == 0)
            {
                return BadRequest("Debes agregar al menos un cilindro al envío.");
            }

            var cilindros = await _context.Cilindros
                .Where(cilindro => request.CilindroIds.Contains(cilindro.IdCilindro) && cilindro.Activo)
                .ToListAsync();

            if (cilindros.Count != request.CilindroIds.Distinct().Count())
            {
                return BadRequest("Uno o más cilindros no existen o están inactivos.");
            }

            if (cilindros.Any(cilindro => cilindro.EstadoActual != "VACIO_ALMACEN"))
            {
                return BadRequest("Solo se pueden enviar a recarga cilindros en estado VACIO_ALMACEN.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var envio = new EnvioRecarga
            {
                IdProveedor = request.IdProveedor,
                FechaEnvio = _clock.UtcNow,
                NumeroGuia = Normalizar(request.NumeroGuia),
                Estado = "ENVIADO",
                Observaciones = Normalizar(request.Observaciones)
            };

            _context.EnviosRecarga.Add(envio);
            await _context.SaveChangesAsync();

            foreach (var cilindro in cilindros)
            {
                cilindro.EstadoActual = "EN_PROVEEDOR";
                cilindro.UbicacionActual = "PROVEEDOR";
                cilindro.FechaUltimoMovimiento = _clock.UtcNow;

                _context.DetallesEnvioRecarga.Add(new DetalleEnvioRecarga
                {
                    IdEnvio = envio.IdEnvio,
                    IdCilindro = cilindro.IdCilindro,
                    EstadoRetorno = "PENDIENTE"
                });

                _context.MovimientosCilindro.Add(new MovimientoCilindro
                {
                    IdCilindro = cilindro.IdCilindro,
                    TipoMovimiento = "ENVIO_RECARGA",
                    FechaMovimiento = _clock.UtcNow,
                    Observacion = $"Envío de recarga {envio.NumeroGuia ?? envio.IdEnvio.ToString()}"
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetEnvioPorId), new { id = envio.IdEnvio }, new { envio.IdEnvio });
        }

        [HttpPatch("envios/{idEnvio:int}/cilindros/{idCilindro:int}/recibir")]
        public async Task<IActionResult> RecibirCilindro(int idEnvio, int idCilindro, RecibirCilindroRequest request)
        {
            var detalle = await _context.DetallesEnvioRecarga
                .FirstOrDefaultAsync(item => item.IdEnvio == idEnvio && item.IdCilindro == idCilindro);

            if (detalle is null)
            {
                return NotFound();
            }

            if (detalle.EstadoRetorno == "RECIBIDO")
            {
                return BadRequest("Ese cilindro ya fue recibido.");
            }

            var cilindro = await _context.Cilindros.FindAsync(idCilindro);

            if (cilindro is null)
            {
                return NotFound("El cilindro no existe.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            detalle.EstadoRetorno = request.Observado ? "OBSERVADO" : "RECIBIDO";
            detalle.FechaRetorno = _clock.UtcNow;
            detalle.Observacion = Normalizar(request.Observacion);

            cilindro.EstadoActual = request.Observado ? "VACIO_ALMACEN" : "LLENO_ALMACEN";
            cilindro.UbicacionActual = "ALMACEN";
            cilindro.FechaUltimoMovimiento = _clock.UtcNow;

            _context.MovimientosCilindro.Add(new MovimientoCilindro
            {
                IdCilindro = idCilindro,
                TipoMovimiento = "RETORNO_RECARGA",
                FechaMovimiento = _clock.UtcNow,
                Observacion = Normalizar(request.Observacion) ?? $"Retorno de recarga {idEnvio}"
            });

            var quedanPendientes = await _context.DetallesEnvioRecarga
                .AnyAsync(item => item.IdEnvio == idEnvio && item.IdCilindro != idCilindro && item.EstadoRetorno == "PENDIENTE");

            var envio = await _context.EnviosRecarga.FindAsync(idEnvio);

            if (envio is not null)
            {
                envio.Estado = quedanPendientes ? "PARCIAL" : "COMPLETADO";
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(detalle);
        }

        [HttpPatch("envios/{id:int}/cerrar")]
        public async Task<IActionResult> CerrarEnvio(int id)
        {
            var envio = await _context.EnviosRecarga.FindAsync(id);

            if (envio is null)
            {
                return NotFound();
            }

            var pendientes = await _context.DetallesEnvioRecarga
                .AnyAsync(detalle => detalle.IdEnvio == id && detalle.EstadoRetorno == "PENDIENTE");

            if (pendientes)
            {
                return BadRequest("No se puede cerrar el envío porque aún tiene cilindros pendientes.");
            }

            envio.Estado = "COMPLETADO";
            await _context.SaveChangesAsync();

            return Ok(envio);
        }

        private static string? Normalizar(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }
    }

}
