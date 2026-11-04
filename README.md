# DG Viajes

Sistema web de gestión para una agencia de viajes: destinos, paquetes, hospedajes con fotos, clientes, reservas con acompañantes y pagos, con usuarios y roles.

**Proyecto Integrador Final · Sitio Web MVC** — Darío Godoy y Deborah Gomez

## Problema que resuelve

Una agencia que trabaja con planillas vende más plazas de las que tiene, supera la capacidad de los hospedajes, calcula mal los importes y pierde el rastro de los cobros. DG Viajes centraliza todo: controla plazas y capacidad por fechas, calcula el importe de cada reserva, registra pagos con su saldo y guarda quién hizo cada operación.

**Tecnologías:** ASP.NET Core MVC (.NET 10), C#, MySQL con ADO.NET (MySqlConnector), Vue 3, Bootstrap, cookies para la web y JWT para la API.

## Instalación

Requisitos: .NET 10 SDK y MySQL 8.0.16 o superior.

1. Clonar el repositorio y abrir `DG-viajes/DG-viajes`.
2. Ejecutar `docs/dg_viajes.sql` en MySQL Workbench (crea la base, las 10 tablas y los datos de prueba).
3. Cargar los secretos, que no se guardan en el repositorio:
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:MySqlConnection" "Server=localhost;Port=3306;Database=dg_viajes;User=root;Password=TU_CONTRASEÑA;"
   dotnet user-secrets set "Jwt:Key" "UN-TEXTO-ALEATORIO-DE-32-CARACTERES-O-MAS"
   ```
4. `dotnet run` y abrir `http://localhost:5213`.

## Usuarios de prueba

Contraseña de todos: **`123456`**

| Email | Rol | Nota |
|---|---|---|
| `admin@dgviajes.com` | Administrador | Acceso total: eliminar, ABM de usuarios y auditoría |
| `empleado@dgviajes.com` | Empleado | Alta y modificación, sin eliminar ni usuarios |
| `sofia@dgviajes.com` | Empleado | |
| `baja@dgviajes.com` | Empleado | Inactivo: no puede iniciar sesión |

## Matriz de evidencias

| Requerimiento | Estado | Código | Evidencia |
|---|---|---|---|
| 1. 6+ tablas y relaciones | ✅ | [Models](DG-viajes/DG-viajes/Models) · [Script SQL](docs/dg_viajes.sql) | [DER](docs/der.png) |
| 2. Login, roles y avatar | ✅ | [CuentaController](DG-viajes/DG-viajes/Controllers/CuentaController.cs) · [UsuariosController](DG-viajes/DG-viajes/Controllers/UsuariosController.cs) | [Captura](docs/evidencias/02-login-roles-avatar.png) |
| 3. Manejo de archivos | ✅ | [HospedajesController](DG-viajes/DG-viajes/Controllers/HospedajesController.cs) (`SubirFoto`, `MarcarPortada`, `EliminarFoto`) | [GIF](docs/evidencias/03-fotos-hospedaje.gif) |
| 4. ABM en Vue + AJAX | ✅ | [Hospedajes/Index.cshtml](DG-viajes/DG-viajes/Views/Hospedajes/Index.cshtml) · [hospedajes.js](DG-viajes/DG-viajes/wwwroot/js/hospedajes.js) | [GIF](docs/evidencias/04-abm-vue.gif) |
| 5. Paginado por servidor | ✅ | [ABMController](DG-viajes/DG-viajes/Controllers/ABMController.cs) · [RepositorioCliente](DG-viajes/DG-viajes/Repositories/RepositorioCliente.cs) (`LIMIT/OFFSET` + `COUNT`) | [Captura](docs/evidencias/05-paginado-page2.png) |
| 6. Selección asíncrona | ✅ | [ReservasController](DG-viajes/DG-viajes/Controllers/ReservasController.cs) (`BuscarClientes`, `BuscarPaquetes`, `BuscarHospedajes`) · [reservas.js](DG-viajes/DG-viajes/wwwroot/js/reservas.js) | [GIF](docs/evidencias/06-busqueda-asincrona.gif) |
| 7. Web API + JWT | ✅ | [AuthApiController](DG-viajes/DG-viajes/Controllers/AuthApiController.cs) · [PaquetesApiController](DG-viajes/DG-viajes/Controllers/PaquetesApiController.cs) · [ReservasApiController](DG-viajes/DG-viajes/Controllers/ReservasApiController.cs) | [Colección Postman](docs/DG-Viajes.postman_collection.json) · [Captura](docs/evidencias/07-api-jwt.png) |

**Modelo:** 10 tablas. Relaciones 1:N (destino → paquetes y hospedajes, reserva → acompañantes y pagos) y la reserva depende de cliente, paquete y hospedaje, lo que resuelve la relación N:M entre clientes y paquetes.

## Web API

Token JWT válido 60 minutos. Los GET exigen `Authorization: Bearer <token>`.

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/auth/login` | Recibe `{ "email", "password" }` y devuelve el token |
| GET | `/api/paquetes` · `/api/paquetes/{id}` | Listado paginado con filtros, y detalle con plazas disponibles |
| GET | `/api/reservas` · `/api/reservas/{id}` | Listado paginado, y detalle con acompañantes y saldo |

La web no consume la API, por eso se adjunta la colección: importar `docs/DG-Viajes.postman_collection.json` y ejecutar primero **Login (admin)**.

## Reglas de negocio

- **Importe** = precio por persona × pasajeros + precio por noche × noches. Se calcula en el servidor y queda fijo.
- **Plazas y capacidad:** no se superan las plazas del paquete ni la capacidad del hospedaje en las fechas del viaje. La reserva se guarda en una transacción para evitar ventas simultáneas de la última plaza.
- **Pagos:** un pago Total debe ser igual al saldo; una Seña o un Parcial, menores. Los pagos no se borran: se anulan, y al cancelar una reserva se anulan sus pagos.
- **Bajas lógicas:** destinos, clientes, hospedajes y usuarios quedan inactivos en lugar de borrarse, para conservar el historial.

## Fuera de alcance

Ajustes y devoluciones de pagos. Las reservas no se editan una vez creadas (se confirman o se cancelan).

