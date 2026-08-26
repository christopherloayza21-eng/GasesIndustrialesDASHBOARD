const statusText = document.querySelector("#statusText");
const refreshButton = document.querySelector("#refreshButton");
const movimientosBody = document.querySelector("#movimientosBody");
const tabButtons = document.querySelectorAll("[data-view]");
const views = document.querySelectorAll(".view");

const summaryElements = {
  cilindrosDisponibles: document.querySelector("#cilindrosDisponibles"),
  cilindrosEnClientes: document.querySelector("#cilindrosEnClientes"),
  cilindrosEnProveedor: document.querySelector("#cilindrosEnProveedor"),
  pedidosPendientes: document.querySelector("#pedidosPendientes")
};

const state = {
  clientes: [],
  productos: [],
  cilindros: [],
  pedidos: [],
  movimientos: [],
  recargas: [],
  zonas: [],
  conductores: [],
  vehiculos: [],
  proveedores: [],
  detallePedido: []
};

const crudModules = {
  clientes: {
    title: "Clientes",
    endpoint: "/api/clientes",
    key: "idCliente",
    columns: ["idCliente", "razonSocial", "ruc", "telefono", "tipoCliente", "activo"],
    fields: [
      { name: "razonSocial", label: "Razón social", type: "text", required: true },
      { name: "ruc", label: "RUC", type: "text" },
      { name: "telefono", label: "Teléfono", type: "text" },
      { name: "direccion", label: "Dirección", type: "text" },
      { name: "idZona", label: "Zona", type: "select", source: "zonas", value: "idZona", text: "nombre", empty: "Sin zona" },
      { name: "tipoCliente", label: "Tipo", type: "select", options: ["EVENTUAL", "NUEVO", "FRECUENTE"] },
      { name: "requiereGarantia", label: "Requiere garantía", type: "checkbox" }
    ]
  },
  productos: {
    title: "Productos",
    endpoint: "/api/productos",
    key: "idProducto",
    columns: ["idProducto", "codigo", "nombre", "tipoProducto", "unidadMedida", "precioReferencia", "activo"],
    fields: [
      { name: "codigo", label: "Código", type: "text", required: true },
      { name: "nombre", label: "Nombre", type: "text", required: true },
      { name: "tipoProducto", label: "Tipo", type: "select", options: ["GAS", "EQUIPO", "INSUMO", "SERVICIO"] },
      { name: "unidadMedida", label: "Unidad", type: "text", required: true },
      { name: "precioReferencia", label: "Precio referencia", type: "number" }
    ]
  },
  cilindros: {
    title: "Cilindros",
    endpoint: "/api/cilindros",
    key: "idCilindro",
    columns: ["idCilindro", "codigoCilindro", "producto", "estadoActual", "ubicacionActual", "activo"],
    fields: [
      { name: "codigoCilindro", label: "Código cilindro", type: "text", required: true },
      { name: "idProducto", label: "Gas", type: "select", source: "gases", value: "idProducto", text: "nombre", required: true },
      { name: "capacidad", label: "Capacidad", type: "number" },
      { name: "propietarioTipo", label: "Propietario", type: "select", options: ["EMPRESA", "CLIENTE"] },
      { name: "idClientePropietario", label: "Cliente propietario", type: "select", source: "clientes", value: "idCliente", text: "razonSocial", empty: "Empresa" },
      { name: "estadoActual", label: "Estado", type: "select", options: ["LLENO_ALMACEN", "VACIO_ALMACEN", "EN_CLIENTE", "EN_PROVEEDOR"] },
      { name: "ubicacionActual", label: "Ubicación", type: "text" }
    ],
    extraAction: { label: "Historial", action: cargarHistorialCilindro }
  },
  zonas: {
    title: "Zonas",
    endpoint: "/api/zonas",
    key: "idZona",
    columns: ["idZona", "nombre", "descripcion", "activo"],
    fields: [
      { name: "nombre", label: "Nombre", type: "text", required: true },
      { name: "descripcion", label: "Descripción", type: "text" }
    ]
  },
  conductores: {
    title: "Conductores",
    endpoint: "/api/conductores",
    key: "idConductor",
    columns: ["idConductor", "nombre", "telefono", "activo"],
    fields: [
      { name: "nombre", label: "Nombre", type: "text", required: true },
      { name: "telefono", label: "Teléfono", type: "text" }
    ]
  },
  vehiculos: {
    title: "Vehículos",
    endpoint: "/api/vehiculos",
    key: "idVehiculo",
    columns: ["idVehiculo", "placa", "descripcion", "activo"],
    fields: [
      { name: "placa", label: "Placa", type: "text", required: true },
      { name: "descripcion", label: "Descripción", type: "text" }
    ]
  },
  proveedores: {
    title: "Proveedores",
    endpoint: "/api/proveedores",
    key: "idProveedor",
    columns: ["idProveedor", "razonSocial", "ruc", "telefono", "activo"],
    fields: [
      { name: "razonSocial", label: "Razón social", type: "text", required: true },
      { name: "ruc", label: "RUC", type: "text" },
      { name: "telefono", label: "Teléfono", type: "text" },
      { name: "direccion", label: "Dirección", type: "text" }
    ]
  }
};

refreshButton.addEventListener("click", cargarTodo);

tabButtons.forEach((button) => {
  button.addEventListener("click", () => cambiarVista(button.dataset.view));
});

iniciarInterfaz();
cargarTodo();

function iniciarInterfaz() {
  Object.entries(crudModules).forEach(([moduleKey, config]) => {
    const container = document.querySelector(`[data-module="${moduleKey}"]`);

    if (container) {
      construirCrud(container, moduleKey, config);
    }
  });

  construirPedidos();
  construirMovimientos();
  construirRecargas();
}

function cambiarVista(viewId) {
  tabButtons.forEach((button) => button.classList.toggle("active", button.dataset.view === viewId));
  views.forEach((view) => view.classList.toggle("active", view.id === viewId));
}

async function cargarTodo() {
  await cargarCatalogos();
  await Promise.all([
    cargarDashboard(),
    ...Object.keys(crudModules).map(cargarCrud),
    cargarPedidos(),
    cargarMovimientos(),
    cargarRecargas()
  ]);

  refrescarSelects();
}

async function cargarCatalogos() {
  const [
    clientes,
    productos,
    cilindros,
    zonas,
    conductores,
    vehiculos,
    proveedores
  ] = await Promise.all([
    apiJson("/api/clientes?incluirInactivos=true"),
    apiJson("/api/productos?incluirInactivos=true"),
    apiJson("/api/cilindros?incluirInactivos=true"),
    apiJson("/api/zonas?incluirInactivos=true"),
    apiJson("/api/conductores?incluirInactivos=true"),
    apiJson("/api/vehiculos?incluirInactivos=true"),
    apiJson("/api/proveedores?incluirInactivos=true")
  ]);

  state.clientes = clientes;
  state.productos = productos;
  state.cilindros = cilindros;
  state.zonas = zonas;
  state.conductores = conductores;
  state.vehiculos = vehiculos;
  state.proveedores = proveedores;
}

async function cargarDashboard() {
  statusText.textContent = "Cargando datos...";

  try {
    const data = await apiJson("/api/dashboard/resumen");

    summaryElements.cilindrosDisponibles.textContent = data.cilindrosDisponibles;
    summaryElements.cilindrosEnClientes.textContent = data.cilindrosEnClientes;
    summaryElements.cilindrosEnProveedor.textContent = data.cilindrosEnProveedor;
    summaryElements.pedidosPendientes.textContent = data.pedidosPendientes;
    renderizarMovimientosDashboard(data.movimientosRecientes);
    statusText.textContent = "Datos actualizados";
  } catch (error) {
    statusText.textContent = error.message;
  }
}

function construirCrud(container, moduleKey, config) {
  container.innerHTML = `
    <section class="panel form-panel">
      <div class="panel-header">
        <div>
          <p class="eyebrow">${config.title}</p>
          <h2 id="${moduleKey}FormTitle">Registrar</h2>
        </div>
        <p id="status-${moduleKey}">Listo</p>
      </div>
      <form class="form-grid" id="${moduleKey}Form">
        <input type="hidden" name="_id">
        ${config.fields.map((field) => campoHtml(field)).join("")}
        <label class="check-field edit-only">
          <input name="activo" type="checkbox" checked>
          Activo
        </label>
        <div class="form-actions">
          <button type="submit">Guardar</button>
          <button class="secondary-button" type="button" data-cancel-edit="${moduleKey}">Cancelar edición</button>
        </div>
      </form>
    </section>
    <section class="panel">
      <div class="panel-header">
        <div><p class="eyebrow">Consulta</p><h2>Listado</h2></div>
        <label class="check-field"><input data-include-inactive="${moduleKey}" type="checkbox"> Inactivos</label>
      </div>
      <div class="table-wrapper">
        <table>
          <thead><tr>${config.columns.map((column) => `<th>${column}</th>`).join("")}<th>Acción</th></tr></thead>
          <tbody data-body="${moduleKey}"></tbody>
        </table>
      </div>
    </section>
  `;

  container.querySelector("form").addEventListener("submit", (event) => guardarCrud(event, moduleKey, config));
  container.querySelector(`[data-cancel-edit="${moduleKey}"]`).addEventListener("click", () => limpiarFormularioCrud(moduleKey));
  container.querySelector(`[data-include-inactive="${moduleKey}"]`).addEventListener("change", () => cargarCrud(moduleKey));
}

function campoHtml(field) {
  if (field.type === "checkbox") {
    return `<label class="check-field"><input name="${field.name}" type="checkbox"> ${field.label}</label>`;
  }

  if (field.type === "select") {
    return `
      <label>${field.label}
        <select name="${field.name}" ${field.required ? "required" : ""} data-source="${field.source ?? ""}" data-value="${field.value ?? ""}" data-text="${field.text ?? ""}" data-empty="${field.empty ?? ""}">
          ${field.options ? field.options.map((option) => `<option value="${option}">${option}</option>`).join("") : ""}
        </select>
      </label>
    `;
  }

  return `<label>${field.label}<input name="${field.name}" type="${field.type}" ${field.required ? "required" : ""} ${field.type === "number" ? "step=\"0.01\"" : ""}></label>`;
}

async function cargarCrud(moduleKey) {
  const config = crudModules[moduleKey];
  const includeInactive = document.querySelector(`[data-include-inactive="${moduleKey}"]`);
  const url = includeInactive?.checked ? `${config.endpoint}?incluirInactivos=true` : config.endpoint;

  try {
    const rows = await apiJson(url);
    state[moduleKey] = rows;
    renderizarTablaCrud(moduleKey, config, rows);
    setStatus(moduleKey, "Datos actualizados");
  } catch (error) {
    setStatus(moduleKey, error.message);
  }
}

function renderizarTablaCrud(moduleKey, config, rows) {
  const tbody = document.querySelector(`[data-body="${moduleKey}"]`);

  if (!tbody) {
    return;
  }

  if (!rows.length) {
    tbody.innerHTML = `<tr><td colspan="${config.columns.length + 1}">Sin registros.</td></tr>`;
    return;
  }

  tbody.innerHTML = rows.map((row) => `
    <tr>
      ${config.columns.map((column) => `<td>${formatearValor(row[column])}</td>`).join("")}
      <td>
        <button class="table-action" type="button" data-edit="${moduleKey}" data-id="${row[config.key]}">Editar</button>
        ${config.extraAction ? `<button class="table-action secondary-button" type="button" data-extra="${moduleKey}" data-id="${row[config.key]}">${config.extraAction.label}</button>` : ""}
        ${typeof row.activo === "boolean" ? `<button class="table-action" type="button" data-toggle="${moduleKey}" data-id="${row[config.key]}" data-active="${row.activo}">${row.activo ? "Desactivar" : "Reactivar"}</button>` : ""}
      </td>
    </tr>
  `).join("");

  tbody.querySelectorAll("[data-edit]").forEach((button) => {
    button.addEventListener("click", () => editarCrud(button.dataset.edit, Number(button.dataset.id)));
  });

  tbody.querySelectorAll("[data-toggle]").forEach((button) => {
    button.addEventListener("click", () => cambiarEstadoCrud(button.dataset.toggle, Number(button.dataset.id), button.dataset.active === "true"));
  });

  tbody.querySelectorAll("[data-extra]").forEach((button) => {
    const moduleConfig = crudModules[button.dataset.extra];
    button.addEventListener("click", () => moduleConfig.extraAction.action(Number(button.dataset.id)));
  });
}

async function guardarCrud(event, moduleKey, config) {
  event.preventDefault();

  const form = event.currentTarget;
  const id = form.elements._id.value;
  const payload = leerFormulario(form, config.fields);

  if (id) {
    payload.activo = form.elements.activo.checked;
  }

  try {
    await apiJson(id ? `${config.endpoint}/${id}` : config.endpoint, {
      method: id ? "PUT" : "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    limpiarFormularioCrud(moduleKey);
    await cargarTodo();
    setStatus(moduleKey, id ? "Registro actualizado" : "Registro creado");
  } catch (error) {
    setStatus(moduleKey, error.message);
  }
}

function editarCrud(moduleKey, id) {
  const config = crudModules[moduleKey];
  const row = state[moduleKey].find((item) => item[config.key] === id);
  const form = document.querySelector(`#${moduleKey}Form`);

  if (!row || !form) {
    return;
  }

  form.elements._id.value = id;
  config.fields.forEach((field) => {
    const input = form.elements[field.name];

    if (!input) {
      return;
    }

    if (field.type === "checkbox") {
      input.checked = Boolean(row[field.name]);
      return;
    }

    input.value = row[field.name] ?? "";
  });

  form.elements.activo.checked = row.activo ?? true;
  document.querySelector(`#${moduleKey}FormTitle`).textContent = "Editar";
  setStatus(moduleKey, `Editando ID ${id}`);
}

function limpiarFormularioCrud(moduleKey) {
  const form = document.querySelector(`#${moduleKey}Form`);

  if (!form) {
    return;
  }

  form.reset();
  form.elements._id.value = "";
  if (form.elements.activo) {
    form.elements.activo.checked = true;
  }
  document.querySelector(`#${moduleKey}FormTitle`).textContent = "Registrar";
}

async function cambiarEstadoCrud(moduleKey, id, activo) {
  const config = crudModules[moduleKey];
  const url = activo ? `${config.endpoint}/${id}` : `${config.endpoint}/${id}/reactivar`;
  const accion = activo ? "desactivar" : "reactivar";

  if (!confirm(`¿Seguro que deseas ${accion} este registro?`)) {
    return;
  }

  try {
    await apiJson(url, { method: activo ? "DELETE" : "PATCH" });
    await cargarTodo();
    setStatus(moduleKey, activo ? "Registro desactivado" : "Registro reactivado");
  } catch (error) {
    setStatus(moduleKey, error.message);
  }
}

function construirPedidos() {
  const container = document.querySelector(`[data-module="pedidos"]`);

  container.innerHTML = `
    <section class="panel form-panel">
      <div class="panel-header">
        <div><p class="eyebrow">Pedidos</p><h2>Crear pedido</h2></div>
        <p id="status-pedidos">Listo</p>
      </div>
      <form class="form-grid" id="pedidoForm">
        <label>Cliente<select name="idCliente" data-source="clientes" data-value="idCliente" data-text="razonSocial" required></select></label>
        <label>Zona<select name="idZona" data-source="zonas" data-value="idZona" data-text="nombre" data-empty="Sin zona"></select></label>
        <label>Conductor<select name="idConductor" data-source="conductores" data-value="idConductor" data-text="nombre" data-empty="Sin conductor"></select></label>
        <label>Vehículo<select name="idVehiculo" data-source="vehiculos" data-value="idVehiculo" data-text="placa" data-empty="Sin vehículo"></select></label>
        <label class="full-field">Dirección<input name="direccionEntrega" type="text"></label>
        <label class="full-field">Observaciones<input name="observaciones" type="text"></label>
      </form>
      <div class="detail-builder">
        <h3>Detalle del pedido</h3>
        <div class="form-grid compact-form">
          <label>Producto<select id="pedidoProducto" data-source="productos" data-value="idProducto" data-text="nombre"></select></label>
          <label>Cantidad<input id="pedidoCantidad" type="number" step="0.01" min="0.01" value="1"></label>
          <label>Precio<input id="pedidoPrecio" type="number" step="0.01" min="0"></label>
          <button type="button" id="agregarDetallePedido">Agregar producto</button>
        </div>
        <div class="table-wrapper">
          <table>
            <thead><tr><th>Producto</th><th>Cantidad</th><th>Precio</th><th>Subtotal</th><th>Acción</th></tr></thead>
            <tbody id="pedidoDetalleBody"></tbody>
          </table>
        </div>
        <div class="total-row">Total: <strong id="pedidoTotal">0.00</strong></div>
      </div>
      <div class="panel-actions">
        <button type="button" id="crearPedidoButton">Crear pedido</button>
      </div>
    </section>
    <section class="panel">
      <div class="panel-header"><div><p class="eyebrow">Consulta</p><h2>Listado</h2></div></div>
      <div class="table-wrapper">
        <table>
          <thead><tr><th>ID</th><th>Cliente</th><th>Estado</th><th>Total</th><th>Fecha</th><th>Acción</th></tr></thead>
          <tbody id="pedidosBody"></tbody>
        </table>
      </div>
    </section>
  `;

  document.querySelector("#agregarDetallePedido").addEventListener("click", agregarDetallePedido);
  document.querySelector("#crearPedidoButton").addEventListener("click", crearPedido);
}

async function cargarPedidos() {
  try {
    state.pedidos = await apiJson("/api/pedidos");
    renderizarPedidos();
    setStatus("pedidos", "Datos actualizados");
  } catch (error) {
    setStatus("pedidos", error.message);
  }
}

function agregarDetallePedido() {
  const idProducto = Number(document.querySelector("#pedidoProducto").value);
  const cantidad = Number(document.querySelector("#pedidoCantidad").value);
  const precioUnitario = document.querySelector("#pedidoPrecio").value ? Number(document.querySelector("#pedidoPrecio").value) : null;
  const producto = state.productos.find((item) => item.idProducto === idProducto);

  if (!producto || cantidad <= 0) {
    setStatus("pedidos", "Selecciona un producto y una cantidad válida.");
    return;
  }

  state.detallePedido.push({ idProducto, cantidad, precioUnitario });
  renderizarDetallePedido();
}

function renderizarDetallePedido() {
  const body = document.querySelector("#pedidoDetalleBody");

  if (!state.detallePedido.length) {
    body.innerHTML = `<tr><td colspan="5">Agrega productos al pedido.</td></tr>`;
    document.querySelector("#pedidoTotal").textContent = "0.00";
    return;
  }

  body.innerHTML = state.detallePedido.map((detalle, index) => {
    const producto = state.productos.find((item) => item.idProducto === detalle.idProducto);
    const subtotal = detalle.precioUnitario === null ? null : detalle.cantidad * detalle.precioUnitario;

    return `
      <tr>
        <td>${producto?.nombre ?? detalle.idProducto}</td>
        <td>${detalle.cantidad}</td>
        <td>${detalle.precioUnitario ?? "-"}</td>
        <td>${subtotal?.toFixed(2) ?? "-"}</td>
        <td><button class="table-action" type="button" data-remove-detail="${index}">Quitar</button></td>
      </tr>
    `;
  }).join("");

  body.querySelectorAll("[data-remove-detail]").forEach((button) => {
    button.addEventListener("click", () => {
      state.detallePedido.splice(Number(button.dataset.removeDetail), 1);
      renderizarDetallePedido();
    });
  });

  const total = state.detallePedido.reduce((sum, detalle) => sum + (detalle.precioUnitario === null ? 0 : detalle.cantidad * detalle.precioUnitario), 0);
  document.querySelector("#pedidoTotal").textContent = total.toFixed(2);
}

async function crearPedido() {
  const form = document.querySelector("#pedidoForm");
  const payload = {
    idCliente: Number(form.elements.idCliente.value),
    direccionEntrega: normalizar(form.elements.direccionEntrega.value),
    idZona: numeroOpcional(form.elements.idZona.value),
    idConductor: numeroOpcional(form.elements.idConductor.value),
    idVehiculo: numeroOpcional(form.elements.idVehiculo.value),
    observaciones: normalizar(form.elements.observaciones.value),
    detalles: state.detallePedido
  };

  if (!payload.idCliente) {
    setStatus("pedidos", "Selecciona un cliente antes de crear el pedido.", "error");
    return;
  }

  if (!payload.detalles.length) {
    setStatus("pedidos", "Agrega al menos un producto.", "error");
    return;
  }

  try {
    await apiJson("/api/pedidos", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    form.reset();
    state.detallePedido = [];
    renderizarDetallePedido();
    await cargarTodo();
    setStatus("pedidos", "Pedido creado", "success");
  } catch (error) {
    setStatus("pedidos", error.message, "error");
  }
}

function renderizarPedidos() {
  const body = document.querySelector("#pedidosBody");

  body.innerHTML = state.pedidos.length
    ? state.pedidos.map((pedido) => `
      <tr>
        <td>${pedido.idPedido}</td>
        <td>${pedido.cliente}</td>
        <td>${pedido.estadoPedido}</td>
        <td>${pedido.total}</td>
        <td>${formatearFecha(pedido.fechaPedido)}</td>
        <td>
          <button class="table-action" type="button" data-cancelar-pedido="${pedido.idPedido}">Cancelar</button>
          <button class="table-action secondary-button" type="button" data-entregar-pedido="${pedido.idPedido}">Entregado</button>
        </td>
      </tr>
    `).join("")
    : `<tr><td colspan="6">Sin pedidos.</td></tr>`;

  body.querySelectorAll("[data-cancelar-pedido]").forEach((button) => {
    button.addEventListener("click", () => cambiarEstadoPedido(Number(button.dataset.cancelarPedido), "CANCELADO"));
  });

  body.querySelectorAll("[data-entregar-pedido]").forEach((button) => {
    button.addEventListener("click", () => cambiarEstadoPedido(Number(button.dataset.entregarPedido), "ENTREGADO"));
  });
}

async function cambiarEstadoPedido(idPedido, estado) {
  if (!confirm(`¿Confirmas cambiar el pedido a ${estado}?`)) {
    return;
  }

  try {
    const url = estado === "CANCELADO" ? `/api/pedidos/${idPedido}/cancelar` : `/api/pedidos/${idPedido}/estado`;
    const options = estado === "CANCELADO"
      ? { method: "PATCH" }
      : { method: "PATCH", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ estado }) };

    await apiJson(url, options);
    await cargarTodo();
    setStatus("pedidos", `Pedido ${estado.toLowerCase()}`, "success");
  } catch (error) {
    setStatus("pedidos", error.message, "error");
  }
}

function construirMovimientos() {
  const container = document.querySelector(`[data-module="movimientos"]`);

  container.innerHTML = `
    <section class="panel form-panel">
      <div class="panel-header">
        <div><p class="eyebrow">Movimientos</p><h2>Registrar movimiento</h2></div>
        <p id="status-movimientos">Listo</p>
      </div>
      <form class="form-grid" id="movimientoForm">
        <label>Tipo
          <select name="tipo">
            <option value="salida-cliente">Salida a cliente</option>
            <option value="retorno-cliente">Retorno de cliente</option>
            <option value="envio-proveedor">Envío a proveedor</option>
            <option value="retorno-recarga">Retorno de recarga</option>
          </select>
        </label>
        <label>Cilindro<select name="idCilindro" data-source="cilindros" data-value="idCilindro" data-text="codigoCilindro" required></select></label>
        <label>Cliente<select name="idCliente" data-source="clientes" data-value="idCliente" data-text="razonSocial" data-empty="Sin cliente"></select></label>
        <label>Pedido<select name="idPedido" data-source="pedidos" data-value="idPedido" data-text="cliente" data-empty="Sin pedido"></select></label>
        <label>Conductor<select name="idConductor" data-source="conductores" data-value="idConductor" data-text="nombre" data-empty="Sin conductor"></select></label>
        <label>Vehículo<select name="idVehiculo" data-source="vehiculos" data-value="idVehiculo" data-text="placa" data-empty="Sin vehículo"></select></label>
        <label class="full-field">Observación<input name="observacion" type="text"></label>
        <button type="submit">Registrar movimiento</button>
      </form>
    </section>
    <section class="panel">
      <div class="panel-header"><div><p class="eyebrow">Historial</p><h2>Movimientos</h2></div></div>
      <div class="table-wrapper">
        <table>
          <thead><tr><th>ID</th><th>Cilindro</th><th>Movimiento</th><th>Cliente</th><th>Fecha</th><th>Observación</th></tr></thead>
          <tbody id="movimientosListBody"></tbody>
        </table>
      </div>
    </section>
  `;

  document.querySelector("#movimientoForm").addEventListener("submit", crearMovimiento);
}

async function cargarMovimientos() {
  try {
    state.movimientos = await apiJson("/api/movimientos");
    renderizarMovimientos();
    setStatus("movimientos", "Datos actualizados");
  } catch (error) {
    setStatus("movimientos", error.message);
  }
}

async function crearMovimiento(event) {
  event.preventDefault();

  const form = event.currentTarget;
  const tipo = form.elements.tipo.value;
  const payload = {
    idCilindro: Number(form.elements.idCilindro.value),
    idPedido: numeroOpcional(form.elements.idPedido.value),
    idCliente: numeroOpcional(form.elements.idCliente.value),
    idConductor: numeroOpcional(form.elements.idConductor.value),
    idVehiculo: numeroOpcional(form.elements.idVehiculo.value),
    observacion: normalizar(form.elements.observacion.value)
  };

  if (!payload.idCilindro) {
    setStatus("movimientos", "Selecciona un cilindro antes de registrar el movimiento.", "error");
    return;
  }

  try {
    await apiJson(`/api/movimientos/${tipo}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    form.reset();
    await cargarTodo();
    setStatus("movimientos", "Movimiento registrado", "success");
  } catch (error) {
    setStatus("movimientos", error.message, "error");
  }
}

function renderizarMovimientos() {
  const body = document.querySelector("#movimientosListBody");

  body.innerHTML = state.movimientos.length
    ? state.movimientos.map((movimiento) => `
      <tr>
        <td>${movimiento.idMovimiento}</td>
        <td>${movimiento.codigoCilindro}</td>
        <td>${movimiento.tipoMovimiento}</td>
        <td>${movimiento.cliente ?? "-"}</td>
        <td>${formatearFecha(movimiento.fechaMovimiento)}</td>
        <td>${movimiento.observacion ?? "-"}</td>
      </tr>
    `).join("")
    : `<tr><td colspan="6">Sin movimientos.</td></tr>`;
}

function construirRecargas() {
  const container = document.querySelector(`[data-module="recargas"]`);

  container.innerHTML = `
    <section class="panel form-panel">
      <div class="panel-header">
        <div><p class="eyebrow">Recargas</p><h2>Crear envío</h2></div>
        <p id="status-recargas">Listo</p>
      </div>
      <form class="form-grid" id="recargaForm">
        <label>Proveedor<select name="idProveedor" data-source="proveedores" data-value="idProveedor" data-text="razonSocial" required></select></label>
        <label>Número de guía<input name="numeroGuia" type="text"></label>
        <label class="full-field">Observaciones<input name="observaciones" type="text"></label>
        <div class="full-field">
          <p class="field-title">Cilindros vacíos disponibles</p>
          <div class="check-list" id="cilindrosRecargaList"></div>
        </div>
        <button type="submit">Crear envío</button>
      </form>
    </section>
    <section class="panel form-panel">
      <div class="panel-header">
        <div><p class="eyebrow">Retorno</p><h2>Recibir cilindro</h2></div>
      </div>
      <form class="form-grid" id="retornoRecargaForm">
        <label>Envío<select name="idEnvio" id="retornoEnvioSelect"></select></label>
        <label>Cilindro<select name="idCilindro" id="retornoCilindroSelect"></select></label>
        <label class="check-field"><input name="observado" type="checkbox"> Observado</label>
        <label class="full-field">Observación<input name="observacion" type="text"></label>
        <button type="submit">Marcar recibido</button>
      </form>
    </section>
    <section class="panel">
      <div class="panel-header"><div><p class="eyebrow">Consulta</p><h2>Envíos</h2></div></div>
      <div class="table-wrapper">
        <table>
          <thead><tr><th>ID</th><th>Proveedor</th><th>Estado</th><th>Pendientes</th><th>Guía</th><th>Fecha</th><th>Acción</th></tr></thead>
          <tbody id="recargasBody"></tbody>
        </table>
      </div>
    </section>
  `;

  document.querySelector("#recargaForm").addEventListener("submit", crearEnvioRecarga);
  document.querySelector("#retornoRecargaForm").addEventListener("submit", recibirCilindroRecarga);
  document.querySelector("#retornoEnvioSelect").addEventListener("change", cargarCilindrosPendientesEnvio);
}

async function cargarRecargas() {
  try {
    state.recargas = await apiJson("/api/recargas/envios");
    renderizarRecargas();
    renderizarCilindrosRecarga();
    refrescarEnviosRecarga();
    setStatus("recargas", "Datos actualizados");
  } catch (error) {
    setStatus("recargas", error.message);
  }
}

function renderizarCilindrosRecarga() {
  const list = document.querySelector("#cilindrosRecargaList");
  const cilindrosVacios = state.cilindros.filter((cilindro) => cilindro.activo && cilindro.estadoActual === "VACIO_ALMACEN");

  list.innerHTML = cilindrosVacios.length
    ? cilindrosVacios.map((cilindro) => `
      <label class="check-field">
        <input type="checkbox" name="cilindroRecarga" value="${cilindro.idCilindro}">
        ${cilindro.codigoCilindro} - ${cilindro.producto}
      </label>
    `).join("")
    : `<p class="empty-text">No hay cilindros vacíos disponibles para enviar.</p>`;
}

async function crearEnvioRecarga(event) {
  event.preventDefault();

  const form = event.currentTarget;
  const cilindroIds = Array.from(form.querySelectorAll("[name='cilindroRecarga']:checked")).map((item) => Number(item.value));
  const payload = {
    idProveedor: Number(form.elements.idProveedor.value),
    numeroGuia: normalizar(form.elements.numeroGuia.value),
    observaciones: normalizar(form.elements.observaciones.value),
    cilindroIds
  };

  if (!payload.idProveedor) {
    setStatus("recargas", "Selecciona un proveedor antes de crear el envío.", "error");
    return;
  }

  if (!payload.cilindroIds.length) {
    setStatus("recargas", "Selecciona al menos un cilindro vacío para enviar.", "error");
    return;
  }

  try {
    await apiJson("/api/recargas/envios", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    form.reset();
    await cargarTodo();
    setStatus("recargas", "Envío creado", "success");
  } catch (error) {
    setStatus("recargas", error.message, "error");
  }
}

function renderizarRecargas() {
  const body = document.querySelector("#recargasBody");

  body.innerHTML = state.recargas.length
    ? state.recargas.map((envio) => `
      <tr>
        <td>${envio.idEnvio}</td>
        <td>${envio.proveedor}</td>
        <td>${envio.estado}</td>
        <td>${envio.pendientes}</td>
        <td>${envio.numeroGuia ?? "-"}</td>
        <td>${formatearFecha(envio.fechaEnvio)}</td>
        <td><button class="table-action" type="button" data-cerrar-envio="${envio.idEnvio}">Cerrar</button></td>
      </tr>
    `).join("")
    : `<tr><td colspan="7">Sin envíos.</td></tr>`;

  body.querySelectorAll("[data-cerrar-envio]").forEach((button) => {
    button.addEventListener("click", () => cerrarEnvioRecarga(Number(button.dataset.cerrarEnvio)));
  });
}

function refrescarEnviosRecarga() {
  const select = document.querySelector("#retornoEnvioSelect");

  if (!select) {
    return;
  }

  select.innerHTML = state.recargas
    .map((envio) => `<option value="${envio.idEnvio}">#${envio.idEnvio} - ${envio.proveedor} (${envio.estado})</option>`)
    .join("");

  cargarCilindrosPendientesEnvio();
}

async function cargarCilindrosPendientesEnvio() {
  const envioSelect = document.querySelector("#retornoEnvioSelect");
  const cilindroSelect = document.querySelector("#retornoCilindroSelect");

  if (!envioSelect?.value) {
    cilindroSelect.innerHTML = "";
    return;
  }

  try {
    const data = await apiJson(`/api/recargas/envios/${envioSelect.value}`);
    const pendientes = data.cilindros.filter((item) => item.estadoRetorno === "PENDIENTE");

    cilindroSelect.innerHTML = pendientes.length
      ? pendientes.map((item) => `<option value="${item.idCilindro}">${item.codigoCilindro}</option>`).join("")
      : `<option value="">Sin pendientes</option>`;
  } catch (error) {
    setStatus("recargas", error.message);
  }
}

async function recibirCilindroRecarga(event) {
  event.preventDefault();

  const form = event.currentTarget;
  const idEnvio = form.elements.idEnvio.value;
  const idCilindro = form.elements.idCilindro.value;

  if (!idEnvio || !idCilindro) {
    setStatus("recargas", "Selecciona un envío y un cilindro pendiente.");
    return;
  }

  try {
    await apiJson(`/api/recargas/envios/${idEnvio}/cilindros/${idCilindro}/recibir`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        observado: form.elements.observado.checked,
        observacion: normalizar(form.elements.observacion.value)
      })
    });

    form.reset();
    await cargarTodo();
    setStatus("recargas", "Cilindro recibido");
  } catch (error) {
    setStatus("recargas", error.message);
  }
}

async function cerrarEnvioRecarga(idEnvio) {
  if (!confirm("¿Seguro que deseas cerrar este envío de recarga?")) {
    return;
  }

  try {
    await apiJson(`/api/recargas/envios/${idEnvio}/cerrar`, { method: "PATCH" });
    await cargarTodo();
    setStatus("recargas", "Envío cerrado", "success");
  } catch (error) {
    setStatus("recargas", error.message, "error");
  }
}

function leerFormulario(form, fields) {
  const payload = {};

  fields.forEach((field) => {
    const input = form.elements[field.name];

    if (!input) {
      return;
    }

    if (field.type === "checkbox") {
      payload[field.name] = input.checked;
      return;
    }

    if (field.type === "number" || field.value) {
      payload[field.name] = numeroOpcional(input.value);
      return;
    }

    payload[field.name] = normalizar(input.value);
  });

  return payload;
}

function refrescarSelects() {
  document.querySelectorAll("select[data-source]").forEach((select) => {
    const source = select.dataset.source;

    if (!source) {
      return;
    }

    const rows = source === "gases"
      ? state.productos.filter((producto) => producto.activo && producto.tipoProducto === "GAS")
      : (state[source] ?? []).filter((item) => item.activo !== false);
    const current = select.value;
    const empty = select.dataset.empty;

    select.innerHTML = [
      empty ? `<option value="">${empty}</option>` : "",
      ...rows.map((row) => `<option value="${row[select.dataset.value]}">${row[select.dataset.text]}</option>`)
    ].join("");

    if (current) {
      select.value = current;
    }
  });
}

async function cargarHistorialCilindro(id) {
  const data = await apiJson(`/api/cilindros/${id}/movimientos`);
  alert(JSON.stringify(data, null, 2));
}

function renderizarMovimientosDashboard(movimientos) {
  movimientosBody.innerHTML = movimientos.length
    ? movimientos.map((movimiento) => `
      <tr>
        <td>${formatearFecha(movimiento.fechaMovimiento)}</td>
        <td>${movimiento.codigoCilindro}</td>
        <td>${movimiento.producto}</td>
        <td>${movimiento.tipoMovimiento}</td>
        <td>${movimiento.cliente ?? "Sin cliente"}</td>
        <td>${movimiento.observacion ?? ""}</td>
      </tr>
    `).join("")
    : `<tr><td colspan="6">No hay movimientos registrados.</td></tr>`;
}

async function apiJson(url, options = {}) {
  const response = await fetch(url, options);

  if (!response.ok) {
    const message = await leerMensajeError(response);
    throw new Error(message || `Error HTTP ${response.status}`);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

async function leerMensajeError(response) {
  const contentType = response.headers.get("content-type") ?? "";

  if (contentType.includes("application/json")) {
    const error = await response.json();

    if (error.errors) {
      return Object.values(error.errors).flat().join(" ");
    }

    return error.detail ?? error.title ?? JSON.stringify(error);
  }

  return response.text();
}

function setStatus(moduleKey, message, type = "info") {
  const status = document.querySelector(`#status-${moduleKey}`);

  if (status) {
    status.textContent = message;
    status.className = `status-message ${type}`;
  }
}

function normalizar(value) {
  const text = String(value ?? "").trim();
  return text || null;
}

function numeroOpcional(value) {
  return value === "" || value === null || value === undefined ? null : Number(value);
}

function formatearValor(value) {
  if (value === null || value === undefined || value === "") {
    return "-";
  }

  if (typeof value === "boolean") {
    return `<span class="status-pill ${value ? "" : "inactive"}">${value ? "Activo" : "Inactivo"}</span>`;
  }

  if (String(value).includes("T")) {
    const date = new Date(value);

    if (!Number.isNaN(date.getTime())) {
      return formatearFecha(value);
    }
  }

  return value;
}

function formatearFecha(fecha) {
  return new Intl.DateTimeFormat("es-PE", {
    dateStyle: "short",
    timeStyle: "short"
  }).format(new Date(fecha));
}
