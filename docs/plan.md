# Plan de implementación

## Sistema de control de acceso y consumo del comedor de personal

| Campo | Valor |
| --- | --- |
| Versión | 1.2 |
| Fecha | 4 de noviembre de 2026 |
| Implementa | [SRS v1.2](srs.md) |
| Diseño | [Guía de diseño](diseno.md) |
| Tareas | 16 (1 a 15, con la 8 dividida en 8a y 8b) |

> **Para agentes:** usar la subskill subagent-driven-development (recomendada) o executing-plans para implementar este plan tarea por tarea. Los pasos usan casillas para dar seguimiento.

**Objetivo:** construir el sistema de control de acceso y consumo del comedor de personal descrito en el SRS: entrada por celular con QR rotativo, captura manual del encargado, registro inalterable y reportes de cotejo de factura.

**Arquitectura:** monorepo con API REST en ASP.NET Core organizada por capas (Dominio, Aplicación, Infraestructura, API) y frontend Angular con NgModules separado en modelo, vista y controlador. La fuente RR. HH. y el autenticador de la empresa son interfaces con adaptadores simulados en la v1.

**Tecnología:** .NET 8 LTS, ASP.NET Core, EF Core 8 con SQL Server, xUnit, Moq, Testcontainers.MsSql, ClosedXML, QuestPDF; Angular actual con NgModules, Jasmine y Karma, angularx-qrcode; k6 para la prueba de carga.

**Spec:** [docs/srs.md](srs.md). **Diseño:** [docs/diseno.md](diseno.md). Quien ejecute el plan lee los tres.

## Restricciones globales

Toda tarea cumple implícitamente esta lista.

- Monorepo: `backend/`, `frontend/`, `load/`, `docs/`. Solución `backend/Comedor.sln`; app Angular en `frontend/comedor-web/`.
- .NET 8 LTS; `Comedor.Domain` no referencia ningún paquete ni proyecto.
- Dependencias entre proyectos: Application → Domain; Infrastructure → Application; Api → Application e Infrastructure (solo en `Program.cs`).
- Toda hora proviene de `IReloj.Ahora()` (hora local en la zona `Comedor:ZonaHoraria = America/Hermosillo`, Cd. Obregón); ningún código llama `DateTime.Now`.
- Consumo, Rechazo y Entrega son de solo inserción: sin métodos de actualización ni borrado en repositorios; `SaveChanges` rechaza entradas modificadas o borradas de esas entidades.
- Restricción única en Consumo: (Matricula, FechaServicio, TiempoComida). Restricción única en Entrega: (ConsumoId).
- Fecha de servicio = día en que inicia el horario en curso, menos un día si la regla tiene `PerteneceAJornadaAnterior = true`. Horario con fin menor que inicio cruza la medianoche. Fin exclusivo: 13:00–15:00 no incluye las 15:00.
- No puede haber dos reglas del mismo turno con horarios traslapados.
- Día laboral: `Empleado.DiasLaborales` contiene el `DayOfWeek` de la fecha de servicio.
- Contrato vigente si InicioContrato ≤ fecha de servicio ≤ FinContrato (ambos inclusivos).
- Matrícula normalizada con `Trim().ToUpperInvariant()` en toda entrada.
- QR del punto cambia cada 30 s (`Qr:SegundosRotacion = 30`); token válido 60 s (`Qr:SegundosValidez = 60`); firma HMAC-SHA256 con `Qr:ClaveFirma`.
- Sesión de empleado: JWT de rol `Empleado`, `sub` = matrícula, vigencia `Sesion:DiasEmpleado = 30`. Se asume que el ID de la empresa es la matrícula.
- Bloqueo de inicio de sesión de empleado: 5 intentos fallidos en 15 minutos bloquean ese ID 15 minutos.
- Usuarios del sistema: roles `Punto`, `Encargado`, `Supervisor`, `Administrador`; JWT de 12 h (`Jwt:HorasSistema = 12`) salvo `Punto`, de 30 días (`Jwt:DiasPunto = 30`); contraseñas con `PasswordHasher<UsuarioSistema>`.
- Pantalla del chef: consulta solicitudes cada 2 s. "Por servir" = consumos del punto sin entrega cuya fecha de servicio y tiempo de comida son los del horario en curso.
- Textos de UI en español. Motivos exactos: "No registrado", "Contrato vencido", "Hoy es tu día de descanso", "Fuera de horario", "Tu turno no tiene derecho a este tiempo de comida", "Ya consumiste este tiempo de comida". Token inválido: "Escanea de nuevo el código de la entrada".
- Interfaz según la guía de diseño [docs/diseno.md](diseno.md) (tipografía, colores, componentes y pantallas de referencia). Versión de Angular fijada en la Tarea 1 (la estable vigente al generar el proyecto) y anotada en `README.md`.
- Pruebas de integración usan SQL Server en Testcontainers (Docker en desarrollo y en CI).
- Commits pequeños, uno por paso de commit, con prefijos `feat:`, `test:`, `chore:`.

## Foco de revisión

Casos que el SRS implica pero que es fácil dejar sin probar; cada uno tiene su prueba en la tarea indicada.

| # | Entrada o condición | Comportamiento esperado | Prueba en |
| --- | --- | --- | --- |
| 1 | Intento a las 00:15 del 11-nov en un horario 23:30–00:30 | Autorizado con fecha de servicio 10-nov; un segundo intento a las 00:20 es "Ya consumiste" | Tarea 2 y Tarea 5 |
| 2 | Desayuno nocturno 06:15 del sábado (regla de la jornada anterior) de alguien que trabaja viernes y descansa sábado | Autorizado con fecha de servicio viernes | Tarea 2 |
| 3 | Celular y captura manual del mismo empleado al mismo tiempo | Exactamente un consumo; el otro recibe "Ya consumiste", nunca un error 500 | Tarea 8b |
| 4 | Token firmado con otra clave, alterado, de un punto inactivo o con 61 s de antigüedad | "Escanea de nuevo el código de la entrada" y ningún rechazo registrado | Tarea 4 |
| 5 | El chef presiona Servido dos veces o desde dos pantallas | Una sola entrega; la segunda respuesta es 200 con `yaServido = true`, nunca 500 | Tarea 8b |

## Estructura de archivos

```text
backend/
  Comedor.sln
  src/
    Comedor.Domain/
      Empleados/Empleado.cs
      Reglas/ReglaDerecho.cs            horario, cruce de medianoche
      Reglas/EvaluadorDerecho.cs        las cinco verificaciones
      Reglas/ResultadoEvaluacion.cs
      Accesos/Consumo.cs  Rechazo.cs  Entrega.cs  Canal.cs  MotivoRechazo.cs  MotivoRechazoTexto.cs
      Puntos/PuntoEntrada.cs
    Comedor.Application/
      Abstracciones/IReloj.cs  IEmpleadoRepositorio.cs  IReglaRepositorio.cs  IAccesoRepositorio.cs
                    IPuntoRepositorio.cs  IFuentePersonalRRHH.cs  IAutenticadorEmpresa.cs
                    IFirmaToken.cs  IEmisorJwt.cs  IExportador.cs  ConsumoDuplicadoException.cs  EntregaDuplicadaException.cs
      Qr/TokenQr.cs  EmitirTokenQr.cs  ValidadorTokenQr.cs
      Accesos/ValidarAcceso.cs  ResultadoAcceso.cs  MarcarServido.cs  ListarSolicitudes.cs
      Sesion/IniciarSesionEmpleado.cs  ControlIntentos.cs
      Sincronizacion/SincronizarPersonal.cs  ResultadoSincronizacion.cs
      Reportes/GenerarReporte.cs  Filas.cs
      Administracion/AdministrarReglas.cs  AdministrarPuntos.cs  AdministrarUsuarios.cs  RegistrarContingencia.cs
    Comedor.Infrastructure/
      Persistencia/ComedorDbContext.cs  Configuraciones/*.cs  Migraciones/
      Repositorios/*.cs
      Seguridad/FirmaTokenHmac.cs  EmisorJwt.cs
      Externos/FuenteRRHHSimulada.cs  AutenticadorEmpresaSimulado.cs
      Exportacion/ExportadorExcel.cs  ExportadorPdf.cs
      RelojZonaHoraria.cs
    Comedor.Api/
      Program.cs  appsettings.json  datos/empleados-simulados.json
      Controllers/AuthController.cs  EmpleadosSesionController.cs  PuntosController.cs
                  AccesosController.cs  ReportesController.cs  AdminController.cs
      Tareas/SincronizacionNocturna.cs
  tests/
    Comedor.Domain.Tests/  Comedor.Application.Tests/  Comedor.Api.Tests/
frontend/comedor-web/src/app/
  core/        auth.service.ts  auth.interceptor.ts  rol.guard.ts  almacen.ts  core.module.ts
  modelos/     empleado.model.ts  pase.model.ts  consumo.model.ts  reporte.model.ts
  pase/        pase.module.ts  login-empleado.component.*  pase.component.*  registro.component.*  pase.service.ts
  punto/       punto.module.ts  qr-rotativo.component.*  captura-manual.component.*  solicitudes.component.*  punto.service.ts
  reportes/    reportes.module.ts  reportes.component.*  reportes.service.ts
  admin/       admin.module.ts  reglas.component.*  puntos.component.*  usuarios.component.*
               contingencia.component.*  sincronizacion.component.*  admin.service.ts
  login/       login.module.ts  login.component.*      (usuarios del sistema)
load/validacion.k6.js
```

## Tarea 1: Estructura del monorepo y reglas de dependencia

**Archivos:** crear `backend/Comedor.sln`, los cuatro proyectos `src/*` y los tres `tests/*`; `frontend/comedor-web/` (`ng new comedor-web --no-standalone --routing --style=scss`); `.gitignore`; `tests/Comedor.Domain.Tests/ArquitecturaTests.cs`.

**Interfaces:** produce la solución y las referencias de la sección Restricciones globales.

- [ ] **Paso 1: escribir la prueba que falla**

```csharp
[Fact] public void Dominio_NoReferenciaOtrosEnsamblados() =>
    Assert.DoesNotContain(typeof(Comedor.Domain.Empleados.Empleado).Assembly.GetReferencedAssemblies(),
        a => a.Name!.StartsWith("Comedor.") || a.Name!.StartsWith("Microsoft.EntityFrameworkCore"));
[Fact] public void Aplicacion_NoReferenciaInfraestructuraNiApi() => /* mismas aserciones sobre Comedor.Application con "Comedor.Infrastructure" y "Comedor.Api" */;
```

- [ ] **Paso 2:** `dotnet test backend/Comedor.sln` → falla de compilación (no existe `Empleado`).
- [ ] **Paso 3:** crear proyectos y referencias; `Empleado` como `record` vacío temporal.
- [ ] **Paso 4:** `dotnet test backend/Comedor.sln` → PASA. `cd frontend/comedor-web && npx ng test --watch=false --browsers=ChromeHeadless` → PASA.
- [ ] **Paso 5:** `git commit -m "chore: estructura del monorepo y reglas de dependencia"`

## Tarea 2: Dominio, reglas de derecho y fecha de servicio

**Archivos:** crear los archivos de `Comedor.Domain` de la estructura; probar en `tests/Comedor.Domain.Tests/EvaluadorDerechoTests.cs` y `ReglaDerechoTests.cs`.

**Interfaces (produce):**

- `record Empleado(string Matricula, string Nombre, string Area, string Turno, IReadOnlySet<DayOfWeek> DiasLaborales, DateOnly InicioContrato, DateOnly FinContrato, string? FotoUrl, bool Activo)`
- `record ReglaDerecho(string Turno, string TiempoComida, TimeOnly Inicio, TimeOnly Fin, bool PerteneceAJornadaAnterior = false)` con `bool CruzaMedianoche` (Fin < Inicio), `bool Contiene(TimeOnly hora)` (fin exclusivo), `bool SeTraslapaCon(ReglaDerecho otra)` (mismo turno y horarios que se cruzan) y `DateOnly FechaServicio(DateTime ahora)` (día de inicio del horario; menos un día si `PerteneceAJornadaAnterior`).
- `enum MotivoRechazo { NoRegistrado, ContratoVencido, DiaDescanso, FueraDeHorario, TurnoSinDerecho, YaConsumio }`; `static string MotivoRechazoTexto.De(MotivoRechazo)` con los textos exactos de Restricciones globales.
- `enum Canal { Celular, Manual, Contingencia }`
- `record ResultadoEvaluacion(bool Autorizado, MotivoRechazo? Motivo, ReglaDerecho? Regla, DateOnly? FechaServicio)`
- `static ResultadoEvaluacion EvaluadorDerecho.Evaluar(Empleado? empleado, IReadOnlyList<ReglaDerecho> reglas, DateTime ahora, Func<DateOnly, string, bool> yaConsumio)`
- `Consumo.Crear(string matricula, DateOnly fechaServicio, string tiempoComida, DateTime fechaHora, string puntoId, Canal canal, string? encargado, string? referencia = null)`, `Rechazo.Crear(string matricula, DateTime fechaHora, DateOnly? fechaServicio, string? tiempoComida, string puntoId, Canal canal, string? encargado, MotivoRechazo motivo)` y `Entrega.Crear(Guid consumoId, DateTime fechaHora, string usuario)`; propiedades `init` sin setters públicos; `Consumo.Id` y `Entrega.Id` son `Guid` generados al crear.
- `record PuntoEntrada(string Id, string Nombre, bool Activo)`

Orden de `Evaluar`: (1) empleado nulo o inactivo → NoRegistrado; (2) se toma la regla de su turno que contiene la hora y su fecha de servicio, o la fecha del calendario si no hay ninguna; contrato no vigente en esa fecha → ContratoVencido; (3) esa fecha no está en sus días laborales → DiaDescanso; (4) ninguna regla de ningún turno contiene la hora → FueraDeHorario; (5) ninguna regla de su turno la contiene → TurnoSinDerecho; (6) `yaConsumio(fecha, tiempo)` → YaConsumio; si no, autorizado.

- [ ] **Paso 1: escribir las pruebas que fallan.** Reglas fijas: M-Comida 13:00–15:00, V-Cena 19:00–21:00, N-Desayuno 06:00–07:30, N-Cena 23:30–00:30. Contrato 2026-08-01 a 2026-12-31 salvo donde se indica.

| Prueba | Entrada | Aserción |
| --- | --- | --- |
| `Autoriza_MatutinoEnComida` | M, martes 2026-11-10 13:30 | Autorizado, FechaServicio 2026-11-10 |
| `Rechaza_NoRegistrado` / `Rechaza_EmpleadoInactivo` | null / Activo=false | NoRegistrado |
| `Rechaza_ContratoVencido` | fin 2026-11-09, 2026-11-10 13:30 | ContratoVencido |
| `ContratoQueVenceHoy_EsVigente` | fin 2026-11-10, 2026-11-10 13:30 | Autorizado |
| `Rechaza_DiaDescanso` | M, domingo 2026-11-15 13:30 | DiaDescanso |
| `Rechaza_FueraDeHorario` | M, 16:00 | FueraDeHorario |
| `FinEsExclusivo` | M, 15:00 | FueraDeHorario |
| `Rechaza_TurnoSinDerecho` | M, 19:30 | TurnoSinDerecho |
| `Rechaza_YaConsumio` | M, 13:30, yaConsumio=true | YaConsumio |
| `Nocturno_DesayunoEsDeLaJornadaAnterior` | N, miércoles 2026-11-11 06:15 | Autorizado, FechaServicio 2026-11-10 |
| `Nocturno_DesayunoTrasUltimaJornada_DiaSiguienteDescanso` | N, sábado 2026-11-14 06:15 (trabaja viernes, descansa sábado) | Autorizado, FechaServicio 2026-11-13 |
| `CruceMedianoche_DespuesDeMedianoche` | N, 2026-11-11 00:15 | FechaServicio 2026-11-10 |
| `CruceMedianoche_AntesDeMedianoche` | N, 2026-11-10 23:45 | FechaServicio 2026-11-10 |
| `SeTraslapa_MismoTurno` / `NoSeTraslapa_OtroTurno` / `NoSeTraslapa_Contiguas` | M 13:00–15:00 vs M 14:00–16:00 / V 14:00–16:00 / M 15:00–16:00 | true / false / false |
| `Textos_DeMotivo` | cada enum | igual al texto exacto |

Días laborales en todas las pruebas: lunes a viernes, salvo donde se indica. Reglas adicionales: N-Desayuno 06:00–07:30 con `PerteneceAJornadaAnterior = true`.

- [ ] **Paso 2:** `dotnet test backend/tests/Comedor.Domain.Tests` → FALLA (tipos no definidos).
- [ ] **Paso 3:** implementar los tipos con las firmas de arriba.
- [ ] **Paso 4:** mismo comando → PASA (incluye las pruebas de la Tarea 1).
- [ ] **Paso 5:** `git commit -m "feat: dominio y reglas de derecho"`

## Tarea 3: Persistencia con EF Core y SQL Server

**Archivos:** crear `Abstracciones/IEmpleadoRepositorio.cs`, `IReglaRepositorio.cs`, `IAccesoRepositorio.cs`, `IPuntoRepositorio.cs`, `ConsumoDuplicadoException.cs`; `Persistencia/ComedorDbContext.cs`, `Configuraciones/*.cs`, `Repositorios/*.cs`, migración inicial; probar en `tests/Comedor.Api.Tests/Persistencia/AccesoRepositorioTests.cs` con un `SqlServerFixture` (Testcontainers.MsSql) compartido.

**Interfaces (produce):**

- `IAccesoRepositorio`: `Task<bool> YaConsumioAsync(string matricula, DateOnly fechaServicio, string tiempoComida)`, `Task RegistrarConsumoAsync(Consumo c)` (lanza `ConsumoDuplicadoException` ante error SQL 2601 o 2627), `Task RegistrarRechazoAsync(Rechazo r)`, `Task RegistrarEntregaAsync(Entrega e)` (lanza `EntregaDuplicadaException` ante 2601 o 2627), `Task<Consumo?> ObtenerConsumoAsync(Guid id)`, `Task<bool> EstaServidoAsync(Guid consumoId)`, `Task<IReadOnlyList<SolicitudPunto>> SolicitudesAsync(string puntoId, DateTime desde)` (consumos y rechazos del punto desde esa hora, con nombre, foto, turno y si ya está servido). Sin métodos de actualizar ni borrar.
- `record SolicitudPunto(Guid? ConsumoId, DateTime FechaHora, string Matricula, string? Nombre, string? FotoUrl, string? Turno, string? TiempoComida, DateOnly? FechaServicio, bool Autorizado, string? Motivo, bool Servido)` en `Comedor.Application`.
- `IEmpleadoRepositorio`: `Task<Empleado?> ObtenerAsync(string matricula)`, `Task<IReadOnlyList<Empleado>> TodosAsync()`, `Task GuardarAsync(IEnumerable<Empleado> empleados)`. `DiasLaborales` se guarda como una columna de bits (lunes = 1 … domingo = 64).
- `IReglaRepositorio`: `Task<IReadOnlyList<ReglaDerecho>> TodasAsync()`, `Task GuardarAsync(ReglaDerecho r)`, `Task EliminarAsync(string turno, string tiempoComida)`.
- `IPuntoRepositorio`: `Task<PuntoEntrada?> ObtenerAsync(string id)`, `Task<IReadOnlyList<PuntoEntrada>> TodosAsync()`, `Task GuardarAsync(PuntoEntrada p)`.
- `ComedorDbContext.SaveChangesAsync` lanza `InvalidOperationException("Los consumos, rechazos y entregas no se modifican ni se borran")` si hay un Consumo, Rechazo o Entrega en estado Modified o Deleted.

* [ ] **Paso 1: escribir las pruebas que fallan:** `RegistrarConsumo_DosVecesMismaClave_LanzaConsumoDuplicado`; `YaConsumio_TrasRegistrar_EsVerdadero`; `YaConsumio_OtraFechaServicio_EsFalso`; `RegistrarEntrega_DosVeces_LanzaEntregaDuplicada`; `Solicitudes_IncluyeConsumosYRechazosDelPunto_ConServido`; `DiasLaborales_IdaYVuelta` (lunes, miércoles y viernes se guardan y se leen iguales); `ModificarConsumo_LanzaInvalidOperation`; `BorrarEntrega_LanzaInvalidOperation`.
* [ ] **Paso 2:** `dotnet test backend/tests/Comedor.Api.Tests --filter Persistencia` → FALLA.
* [ ] **Paso 3:** implementar DbContext, índices únicos, conversión de `DiasLaborales`, repositorios y migración `Inicial`.
* [ ] **Paso 4:** mismo comando → PASA.
* [ ] **Paso 5:** `git commit -m "feat: persistencia y registro inalterable"`

## Tarea 4: Token QR firmado y rotativo

**Archivos:** crear `Abstracciones/IReloj.cs`, `IFirmaToken.cs`; `Qr/TokenQr.cs`, `EmitirTokenQr.cs`, `ValidadorTokenQr.cs`; `Infrastructure/Seguridad/FirmaTokenHmac.cs`, `RelojZonaHoraria.cs`; probar en `tests/Comedor.Application.Tests/Qr/ValidadorTokenQrTests.cs`.

**Interfaces (produce):**

- `IReloj { DateTime Ahora(); }`
- `record TokenQr(string PuntoId, DateTime EmitidoEn)`
- `IFirmaToken { string Firmar(TokenQr t); TokenQr? Verificar(string token); }`. Formato: `base64url("{PuntoId}|{EmitidoEn.Ticks}") + "." + base64url(HMACSHA256(clave, parte1))`; `Verificar` devuelve null ante formato o firma inválidos.
- `EmitirTokenQr.EjecutarAsync(string puntoId) → Task<string>`; lanza `InvalidOperationException` si el punto no existe o está inactivo.
- `ValidadorTokenQr.ValidarAsync(string token) → Task<string?>`: devuelve el PuntoId solo si la firma es válida, `Ahora() - EmitidoEn ≤ 60 s` y el punto está activo.

* [ ] **Paso 1: escribir las pruebas que fallan:** `TokenRecienEmitido_EsValido`; `TokenCon59Segundos_EsValido`; `TokenCon61Segundos_EsInvalido`; `TokenAlterado_EsInvalido` (cambiar un carácter de la parte 1); `TokenFirmadoConOtraClave_EsInvalido`; `TokenDePuntoInactivo_EsInvalido`; `Emitir_PuntoInactivo_Lanza`. Usar un `IReloj` falso y `FirmaTokenHmac` real.
* [ ] **Paso 2:** `dotnet test backend/tests/Comedor.Application.Tests --filter Qr` → FALLA.
* [ ] **Paso 3:** implementar con `System.Security.Cryptography.HMACSHA256` y comparación con `CryptographicOperations.FixedTimeEquals`.
* [ ] **Paso 4:** mismo comando → PASA.
* [ ] **Paso 5:** `git commit -m "feat: token QR firmado"`

## Tarea 5: Caso de uso ValidarAcceso (celular y manual)

**Archivos:** crear `Accesos/ValidarAcceso.cs`, `ResultadoAcceso.cs`; probar en `tests/Comedor.Application.Tests/Accesos/ValidarAccesoTests.cs` (Moq para repositorios, `IReloj` falso, `ValidadorTokenQr` real con `FirmaTokenHmac`).

**Interfaces:**

- Consume: `EvaluadorDerecho.Evaluar`, `IEmpleadoRepositorio`, `IReglaRepositorio`, `IAccesoRepositorio`, `ValidadorTokenQr`, `IReloj` (Tareas 2 a 4).
- Produce: `enum EstadoAcceso { Autorizado, Rechazado, EscanearDeNuevo }`; `record ResultadoAcceso(EstadoAcceso Estado, string? Motivo, string? Matricula, string? Nombre, string? Turno, string? TiempoComida, string? FotoUrl, DateTime Hora)`; `Task<ResultadoAcceso> PorCelularAsync(string matricula, string tokenQr)`; `Task<ResultadoAcceso> ManualAsync(string matricula, string puntoId, string encargado)`.

Comportamiento: normalizar matrícula; en celular, token inválido → `EscanearDeNuevo` con el texto exacto, sin registrar nada; un rechazo se registra con `Rechazo.Crear`; un autorizado se registra con `Consumo.Crear`; si `RegistrarConsumoAsync` lanza `ConsumoDuplicadoException`, se registra un Rechazo YaConsumio y se devuelve Rechazado con su texto.

- [ ] **Paso 1: escribir las pruebas que fallan:** `Celular_TokenInvalido_EscanearDeNuevo_SinRegistros` (verifica `RegistrarRechazoAsync` y `RegistrarConsumoAsync` nunca llamados); `Celular_Autorizado_RegistraConsumoCanalCelular`; `Manual_Autorizado_RegistraConsumoCanalManualConEncargado`; `Manual_MatriculaConEspaciosYMinusculas_SeNormaliza` (" ab123 " → busca "AB123"); `Rechazo_RegistraMotivo`; `ConsumoDuplicado_DevuelveYaConsumio_YRegistraRechazo`; `CruceMedianoche_SegundoIntento_EsYaConsumio` (repositorio en memoria; 00:15 y 00:20 del 2026-11-11 con regla N-Cena 23:30–00:30).
- [ ] **Paso 2:** `dotnet test backend/tests/Comedor.Application.Tests --filter Accesos` → FALLA.
- [ ] **Paso 3:** implementar `ValidarAcceso`.
- [ ] **Paso 4:** mismo comando → PASA.
- [ ] **Paso 5:** `git commit -m "feat: caso de uso de validación de acceso"`

## Tarea 6: Inicio de sesión del empleado

**Archivos:** crear `Abstracciones/IAutenticadorEmpresa.cs`, `IEmisorJwt.cs`; `Sesion/IniciarSesionEmpleado.cs`, `ControlIntentos.cs`; `Infrastructure/Externos/AutenticadorEmpresaSimulado.cs`, `Seguridad/EmisorJwt.cs`; probar en `tests/Comedor.Application.Tests/Sesion/IniciarSesionEmpleadoTests.cs`.

**Interfaces (produce):**

- `IAutenticadorEmpresa { Task<bool> ValidarAsync(string id, string contrasena); }`; el simulado lee pares de `Autenticacion:Simulada:Usuarios` en appsettings de desarrollo.
- `IEmisorJwt { string Emitir(string sujeto, string rol, TimeSpan vigencia); }`
- `enum ErrorSesion { CredencialesIncorrectas, Bloqueado, ServicioNoDisponible }`; `record ResultadoSesion(bool Exito, string? Token, ErrorSesion? Error)`; `Task<ResultadoSesion> IniciarSesionEmpleado.EjecutarAsync(string id, string contrasena)`.
- `ControlIntentos` en memoria (`IMemoryCache`): `bool EstaBloqueado(string id)`, `void RegistrarFallo(string id)`, `void Limpiar(string id)`, con 5 fallos en 15 min → bloqueo de 15 min según `IReloj`.

* [ ] **Paso 1: escribir las pruebas que fallan:** `CredencialesCorrectas_DevuelveJwtRolEmpleado_SubMatricula_Vigencia30Dias`; `CredencialesIncorrectas_DevuelveError`; `CincoFallos_SextoIntentoBloqueado_AunConContrasenaCorrecta`; `Bloqueo_SeLevantaA15Minutos`; `AutenticadorLanza_DevuelveServicioNoDisponible`; `IdSeNormaliza` (" ab123 " → "AB123").
* [ ] **Paso 2:** `dotnet test backend/tests/Comedor.Application.Tests --filter Sesion` → FALLA.
* [ ] **Paso 3:** implementar; el JWT usa `System.IdentityModel.Tokens.Jwt` con la clave `Jwt:Clave`.
* [ ] **Paso 4:** mismo comando → PASA.
* [ ] **Paso 5:** `git commit -m "feat: inicio de sesión de empleado"`

## Tarea 7: Sincronización de personal

**Archivos:** crear `Abstracciones/IFuentePersonalRRHH.cs`; `Sincronizacion/SincronizarPersonal.cs`, `ResultadoSincronizacion.cs`; entidad y repositorio `BitacoraSincronizacion`; `Infrastructure/Externos/FuenteRRHHSimulada.cs`; `Api/datos/empleados-simulados.json` (20 empleados en turnos Matutino, Vespertino y Nocturno, 2 con contrato vencido); `Api/Tareas/SincronizacionNocturna.cs`; probar en `tests/Comedor.Application.Tests/Sincronizacion/SincronizarPersonalTests.cs`.

**Interfaces (produce):**

- `record EmpleadoFuente(string Matricula, string Nombre, string Area, string Turno, IReadOnlySet<DayOfWeek> DiasLaborales, DateOnly InicioContrato, DateOnly FinContrato, string? FotoUrl)`; `IFuentePersonalRRHH { Task<IReadOnlyList<EmpleadoFuente>> ObtenerEmpleadosAsync(); }`
- `record ResultadoSincronizacion(bool Exito, int Altas, int Cambios, int Bajas, int Omitidos, string? Error)`; `Task<ResultadoSincronizacion> SincronizarPersonal.EjecutarAsync()`; cada ejecución guarda una fila en la bitácora.
- `SincronizacionNocturna : BackgroundService` que ejecuta a la hora `Sincronizacion:Hora` (por defecto `02:00`).

Reglas: matrícula vacía o turno no presente en el catálogo → omitido; empleado local ausente en la fuente → `Activo = false` (baja); la fuente lanza excepción → `Exito = false`, datos locales intactos.

- [ ] **Paso 1: escribir las pruebas que fallan:** `EmpleadoNuevo_CuentaComoAlta`; `CambioDeTurno_CuentaComoCambio`; `AusenteEnFuente_SeDaDeBaja`; `MatriculaVacia_SeOmiteYContinua`; `TurnoDesconocido_SeOmite`; `FuenteCaida_NoModificaDatos_YRegistraError`; `CadaEjecucion_EscribeBitacora`.
- [ ] **Paso 2:** `dotnet test backend/tests/Comedor.Application.Tests --filter Sincronizacion` → FALLA.
- [ ] **Paso 3:** implementar caso de uso, adaptador simulado y tarea programada.
- [ ] **Paso 4:** mismo comando → PASA.
- [ ] **Paso 5:** `git commit -m "feat: sincronización de personal"`

## Tarea 8a: API base, autenticación y roles

**Archivos:** crear `Program.cs` (DI, JWT, CORS para el origen de Angular, ProblemDetails, migración al iniciar), `appsettings.json` con las claves de Restricciones globales, `Controllers/AuthController.cs`; entidad `UsuarioSistema` y su repositorio; probar en `tests/Comedor.Api.Tests/AuthTests.cs` con `WebApplicationFactory<Program>` y el `SqlServerFixture`.

**Interfaces (produce):**

- `POST /api/auth/login` (anónimo) `{usuario, contrasena}` → `{token, rol, expiraEn}`; vigencia 12 h, o 30 días si el rol es `Punto`.
- `GET /api/auth/yo` (cualquier rol del sistema) → `{usuario, rol}`.
- Se siembra un administrador desde `Semilla:Admin` si no hay usuarios.

* [ ] **Paso 1: escribir las pruebas que fallan:** `Login_AdminSemilla_DevuelveToken12h`; `Login_RolPunto_DevuelveToken30Dias`; `Login_ContrasenaIncorrecta_401`; `Yo_SinToken_401`; `Yo_ConToken_DevuelveUsuarioYRol`.
* [ ] **Paso 2:** `dotnet test backend/tests/Comedor.Api.Tests --filter AuthTests` → FALLA.
* [ ] **Paso 3:** implementar configuración y controlador.
* [ ] **Paso 4:** mismo comando → PASA.
* [ ] **Paso 5:** `git commit -m "feat: API base y autenticación por roles"`

## Tarea 8b: Endpoints de acceso, pantalla del chef y concurrencia

**Archivos:** crear `Application/Accesos/MarcarServido.cs`, `ListarSolicitudes.cs`; `Controllers/EmpleadosSesionController.cs`, `PuntosController.cs`, `AccesosController.cs`, `ConsumosController.cs`; probar en `tests/Comedor.Api.Tests/AccesosApiTests.cs`.

**Interfaces:**

- Consume: `ValidarAcceso`, `EmitirTokenQr`, `IniciarSesionEmpleado`, `IAccesoRepositorio` (Tareas 3 a 6).
- Produce: `MarcarServido.EjecutarAsync(Guid consumoId, string usuario) → Task<ResultadoServido>` con `record ResultadoServido(bool YaServido, DateTime ServidoEn)`; consumo inexistente → `KeyNotFoundException`; `EntregaDuplicadaException` → `YaServido = true`. `ListarSolicitudes.EjecutarAsync(string puntoId, DateTime? desde) → Task<IReadOnlyList<SolicitudPunto>>`; sin `desde`, las últimas 2 horas.

| Método y ruta | Rol | Cuerpo | Respuesta |
| --- | --- | --- | --- |
| `POST /api/empleados/sesion` | anónimo | `{id, contrasena}` | `{token}` o 401 con `{error}` |
| `GET /api/puntos/{id}/qr` | Punto, Encargado |  | `{token, url, expiraEn}`; `url` = `{App:UrlPublica}/pase?t={token}` |
| `POST /api/accesos/celular` | Empleado | `{tokenQr}` | `ResultadoAcceso` |
| `POST /api/accesos/manual` | Punto, Encargado | `{matricula, puntoId}` | `ResultadoAcceso` |
| `GET /api/puntos/{id}/solicitudes?desde=` | Punto, Encargado |  | `SolicitudPunto[]`, más recientes primero |
| `POST /api/consumos/{id}/servido` | Punto, Encargado |  | `{yaServido, servidoEn}`; 404 si no existe |

La matrícula del canal celular sale del `sub` del JWT, nunca del cuerpo. El usuario de la sesión queda como encargado de la captura manual y de la entrega.

- [ ] **Paso 1: escribir las pruebas que fallan:** `AccesoCelular_SinToken_401`; `AccesoManual_ConRolEmpleado_403`; `AccesoCelular_TokenQrDeOtraClave_EscanearDeNuevo`; `FlujoCompleto_SesionQrYAcceso_Autorizado`; `Solicitudes_AutorizadoAparecePorServir_ConNombre`; `Servido_MarcaServidoEnSolicitudes`; `Servido_DosVecesSimultaneas_UnaEntrega` (`Task.WhenAll`: una respuesta `yaServido=false`, otra `true`, 1 fila en Entrega, ningún 500); `Servido_ConsumoInexistente_404`; `Concurrencia_CelularYManualSimultaneos_UnSoloConsumo` (exactamente un `Autorizado`, un `Rechazado` con "Ya consumiste este tiempo de comida", ningún 500, 1 fila en Consumo).
- [ ] **Paso 2:** `dotnet test backend/tests/Comedor.Api.Tests --filter AccesosApiTests` → FALLA.
- [ ] **Paso 3:** implementar casos de uso y controladores.
- [ ] **Paso 4:** mismo comando → PASA.
- [ ] **Paso 5:** `git commit -m "feat: endpoints de acceso y pantalla del chef"`

## Tarea 9: Reportes y exportación

**Archivos:** crear `Reportes/GenerarReporte.cs`, `Filas.cs`; `Abstracciones/IExportador.cs`; `Infrastructure/Exportacion/ExportadorExcel.cs` (ClosedXML), `ExportadorPdf.cs` (QuestPDF, `QuestPDF.Settings.License = LicenseType.Community`); `Controllers/ReportesController.cs`; probar en `tests/Comedor.Api.Tests/ReportesTests.cs` con datos sembrados.

**Interfaces (produce):**

- `record FilaConteo(DateOnly FechaServicio, string TiempoComida, string Turno, string Area, int Total)`; `record FilaCotejo(DateOnly FechaServicio, string TiempoComida, int Servidas, int SinServir)`; `record FilaDetalle(DateTime FechaHora, string Matricula, string Nombre, string TiempoComida, string PuntoId, Canal Canal, string Resultado, string? Motivo, bool Servido)`.
- `GenerarReporte`: `ConteoAsync(DateOnly desde, DateOnly hasta)`, `CotejoFacturaAsync(DateOnly desde, DateOnly hasta)`, `DetalleAsync(DateOnly desde, DateOnly hasta, string? matricula, string? puntoId, Canal? canal, MotivoRechazo? motivo)`; rangos inclusivos sobre la fecha de servicio. Los consumos de canal Contingencia cuentan como servidos.
- `IExportador { byte[] CotejoExcel(IReadOnlyList<FilaCotejo> filas, DateOnly desde, DateOnly hasta); byte[] CotejoPdf(...); byte[] DetalleExcel(IReadOnlyList<FilaDetalle> filas); byte[] ConteoExcel(IReadOnlyList<FilaConteo> filas, DateOnly desde, DateOnly hasta); byte[] ConteoPdf(...); }`; la última fila del cotejo es "Total" con las sumas de Servidas y SinServir; la del conteo es "Total" con la suma de Total ([D-14](decisiones.md#d-14-el-conteo-también-se-exporta)).
- Rutas (roles Supervisor y Administrador): `GET /api/reportes/conteo`, `/cotejo`, `/detalle`, `/cotejo.xlsx`, `/cotejo.pdf`, `/detalle.xlsx`, `/conteo.xlsx`, `/conteo.pdf`, con `?desde=yyyy-MM-dd&hasta=yyyy-MM-dd`; fechas faltantes, inválidas o `desde > hasta` → 400.
- Las consultas viven en `Infrastructure` detrás de `Abstracciones/IConsultaReportes.cs`, porque `Application` no ve el `DbContext`. Turno, Área y Nombre salen del empleado unido por matrícula ("Sin dato" si no existe). Más detalles en [D-18](decisiones.md#d-18-detalles-de-la-api-no-fijados-en-el-plan).

* [ ] **Paso 1: escribir las pruebas que fallan:** sembrar el 2026-11-10 tres consumos servidos (2 Comida, 1 Cena), un consumo Comida sin servir, un consumo N-Cena a las 00:15 del 2026-11-11 servido (fecha de servicio 2026-11-10) y un rechazo. `Cotejo_AgrupaPorFechaServicio` (2026-11-10: Comida 2 servidas y 1 sin servir; Cena 2 servidas); `Cotejo_ExcluyeRechazos`; `Cotejo_ContingenciaCuentaComoServida`; `Conteo_AgrupaPorTurnoYArea`; `CotejoExcel_ReleidoConClosedXML_TotalServidasEs4`; `CotejoPdf_EmpiezaConFirmaPdf` (primeros bytes `%PDF`); `Detalle_FiltraPorCanal`; `Reportes_ConRolEncargado_403`.
* [ ] **Paso 2:** `dotnet test backend/tests/Comedor.Api.Tests --filter ReportesTests` → FALLA.
* [ ] **Paso 3:** implementar consultas con LINQ sobre el DbContext, exportadores y controlador.
* [ ] **Paso 4:** mismo comando → PASA.
* [ ] **Paso 5:** `git commit -m "feat: reportes y exportación"`

## Tarea 10: Administración y contingencia

**Archivos:** crear `Administracion/AdministrarReglas.cs`, `AdministrarPuntos.cs`, `AdministrarUsuarios.cs`, `RegistrarContingencia.cs`; `Controllers/AdminController.cs`; probar en `tests/Comedor.Application.Tests/Administracion/*Tests.cs` y `tests/Comedor.Api.Tests/AdminTests.cs`.

**Interfaces (produce), rutas con rol Administrador:**

- `GET|PUT|DELETE /api/admin/reglas` (`{turno, tiempoComida, inicio, fin, perteneceAJornadaAnterior}` con horas `HH:mm`); `AdministrarReglas.GuardarAsync` rechaza `inicio == fin` con `ArgumentException("El inicio y el fin no pueden ser iguales")` y una regla que se traslapa con otra del mismo turno con `ArgumentException("Se traslapa con {tiempoComida} de {inicio} a {fin}")`; la API responde 400 con ese texto.
- `DELETE /api/admin/reglas?turno=&tiempoComida=`; 404 si la regla no existe. Al editar una regla, el traslape no se compara contra ella misma. Los errores 400 y 409 de administración responden `{ "error": "<texto>" }`.
- `GET|PUT /api/admin/puntos` (`{id, nombre, activo}`); la baja es `activo = false`.
- `GET|PUT /api/admin/usuarios` (`{usuario, rol, activo, contrasena?}`); la contraseña se guarda con `PasswordHasher<UsuarioSistema>` y nunca se devuelve. El rol debe ser `Punto`, `Encargado`, `Supervisor` o `Administrador` (400 si no); la contraseña es obligatoria al crear y opcional al editar. No se protege al último administrador activo ([D-16](decisiones.md#d-16-sin-protección-del-último-administrador)).
- `POST /api/admin/sincronizacion` → `ResultadoSincronizacion`; `GET /api/admin/sincronizacion` → últimas 20 filas de bitácora.
- `POST /api/admin/contingencia` `{matricula, fechaServicio, tiempoComida, referenciaLista}` → `RegistrarContingencia.EjecutarAsync` crea un Consumo canal Contingencia con el administrador como autor y la referencia; un duplicado devuelve 409 con "Ya consumiste este tiempo de comida". Sin reglas de derecho; matrícula inexistente → 400 "No registrado"; referencia y tiempo de comida obligatorios; punto = el punto activo; hora = la de captura ([D-15](decisiones.md#d-15-reglas-de-la-captura-de-contingencia)).

* [ ] **Paso 1: escribir las pruebas que fallan:** `Regla_InicioIgualFin_Lanza`; `Regla_TraslapadaEnMismoTurno_Lanza`; `Regla_CruceMedianoche_SeGuarda`; `Regla_JornadaAnterior_SeGuarda`; `Usuario_ContrasenaNoSeDevuelve`; `Usuario_LoginConContrasenaNueva_Funciona`; `Contingencia_RegistraCanalContingencia`; `Contingencia_Duplicado_409`; `Admin_ConRolSupervisor_403`.
* [ ] **Paso 2:** `dotnet test backend/Comedor.sln --filter "FullyQualifiedName~Administracion|FullyQualifiedName~AdminTests"` → FALLA.
* [ ] **Paso 3:** implementar casos de uso y controlador.
* [ ] **Paso 4:** `dotnet test backend/Comedor.sln` → PASA todo el backend.
* [ ] **Paso 5:** `git commit -m "feat: administración y contingencia"`

Comando de prueba del frontend en todas las tareas siguientes, desde `frontend/comedor-web`: `npx ng test --watch=false --browsers=ChromeHeadless --include=<ruta>`. Con Angular 22 la ruta debe apuntar a los archivos de prueba: donde una tarea dice `--include=src/app/<carpeta>/**`, usar `--include='src/app/<carpeta>/**/*.spec.ts'` (con `**` solo, el compilador intenta cargar `.html` y `.scss` y falla).

## Tarea 11: Núcleo del frontend e inicio de sesión del sistema

**Archivos:** crear `modelos/*.model.ts`, `core/almacen.ts`, `auth.service.ts`, `auth.interceptor.ts`, `rol.guard.ts`, `core.module.ts`; `login/login.module.ts`, `login.component.*`; `environments/environment.ts` con `apiUrl`; src/styles/\_tokens.scss con los tokens de docs/diseno.md, importado en styles.scss; rutas en `app-routing.module.ts`.

**Interfaces (produce):**

- Modelos que reflejan los DTOs de la Tarea 8 y 9: `ResultadoAcceso` (con `estado: 'Autorizado' | 'Rechazado' | 'EscanearDeNuevo'`), `TokenQrRespuesta`, `FilaCotejo`, `FilaConteo`, `FilaDetalle`, `ReglaDerecho`, `PuntoEntrada`, `UsuarioSistema`.
- `almacen.ts`: `leer(clave): string | null`, `guardar(clave, valor)`, `borrar(clave)`, cada una envuelta en try/catch.
- `AuthService`: `login(usuario, contrasena): Observable<void>` (guarda `comedor.token.sistema` y `comedor.rol`), `iniciarSesionEmpleado(id, contrasena): Observable<void>` (guarda `comedor.token.empleado`), `tokenSistema()`, `tokenEmpleado()`, `rol()`, `cerrarSesion()`.
- `AuthInterceptor`: agrega `Authorization: Bearer` con el token del empleado en rutas `/api/accesos/celular` y con el del sistema en el resto de `/api/*` salvo login y sesión.
- `rolGuard(roles: string[])`: redirige a `/login?returnUrl=<ruta>` si el rol no está; tras iniciar sesión, `LoginComponent` vuelve a `returnUrl`. Sin `returnUrl` se queda en `/login` (no hay página de inicio por rol).
- Los módulos Pase, Punto, Reportes y Administración se crean vacíos para que la carga diferida compile; las Tareas 12 a 14 los llenan. Se conservan los nombres generados por Angular 22: `app-module.ts` y `app-routing-module.ts` ([D-18](decisiones.md#d-18-detalles-de-la-api-no-fijados-en-el-plan)).
- Rutas: `/login`, `/pase` y /registro (módulo Pase, sin guard), `/punto/:id` (Punto, Encargado), `/reportes` (Supervisor, Administrador), `/admin` (Administrador), con carga diferida por módulo.

* [ ] **Paso 1: escribir las pruebas que fallan** (`HttpTestingController`): `login_guardaTokenYRol`; `interceptor_usaTokenEmpleadoEnAccesoCelular`; `interceptor_usaTokenSistemaEnReportes`; `interceptor_noAgregaTokenEnLogin`; `almacen_noLanzaSiLocalStorageFalla`; `rolGuard_redirigeSinRol`.
* [ ] **Paso 2:** comando con `--include=src/app/core/**` → FALLA.
* [ ] **Paso 3:** implementar; `LoginComponent` solo muestra el formulario y llama a `AuthService`.
* [ ] **Paso 4:** mismo comando → PASA.
* [ ] **Paso 5:** `git commit -m "feat: núcleo del frontend y login del sistema"`

## Tarea 12: Módulo Pase (celular del empleado)

**Archivos:** crear `pase/pase.module.ts`, `pase.service.ts`, `login-empleado.component.*`, `pase.component.*`.

**Interfaces:**

- Consume: `AuthService.iniciarSesionEmpleado`, `tokenEmpleado()` (Tarea 11); `POST /api/accesos/celular` (Tarea 8).
- Produce: `PaseService.validar(tokenQr: string): Observable<ResultadoAcceso>`.

Flujo: `/pase?t=<token>` → sin token de empleado muestra `login-empleado` (ID y contraseña; errores "Credenciales incorrectas", "Cuenta bloqueada por 15 minutos", "No se pudo verificar tu cuenta; acude con el encargado") → con token llama a `validar` → muestra el pase a pantalla completa: verde y "AUTORIZADO" con "Pasa con el chef", o rojo y "RECHAZADO" con el motivo; foto, nombre, turno, tiempo de comida, un reloj con segundos que avanza y un borde con animación CSS continua. `EscanearDeNuevo` muestra el texto exacto sin pase. Un 401 en `validar` borra el token de empleado y vuelve al login conservando `t`.

Registro previo (`/registro`): el mismo `login-empleado`; al iniciar sesión muestra "Listo. El día que abra el comedor solo escanea el código de la entrada" y no llama a `validar`.

- [ ] **Paso 1: escribir las pruebas que fallan:** `sinSesion_muestraLogin`; `conSesion_llamaValidarConToken`; `autorizado_muestraAutorizadoYNombre`; `rechazado_muestraMotivo`; `escanearDeNuevo_muestraTextoExacto`; `relojAvanza` (fakeAsync, `tick(1000)` cambia el segundo); `respuesta401_vuelveALogin`; `registro_trasLogin_muestraListo_sinValidar`.
- [ ] **Paso 2:** comando con `--include=src/app/pase/**` → FALLA.
- [ ] **Paso 3:** implementar; estilos móviles primero, texto del estado de al menos 48 px.
- [ ] **Paso 4:** mismo comando → PASA.
- [ ] **Paso 5:** `git commit -m "feat: pase del empleado y registro previo"`

## Tarea 13: Módulo Punto (pantalla de entrada)

**Archivos:** crear `punto/punto.module.ts`, `punto.service.ts`, `qr-rotativo.component.*`, `solicitudes.component.*`, `captura-manual.component.*`; dependencia `angularx-qrcode`. La pantalla del punto reúne los tres componentes: QR a un lado, lista de solicitudes al centro y captura manual abajo.

**Interfaces:**

- Consume: rutas `qr`, `solicitudes`, `servido` y `accesos/manual` de la Tarea 8b.
- Produce: `PuntoService.obtenerQr(puntoId): Observable<TokenQrRespuesta>`, `solicitudes(puntoId, desde?): Observable<SolicitudPunto[]>`, `marcarServido(consumoId): Observable<{yaServido: boolean}>` y `capturaManual(matricula, puntoId): Observable<ResultadoAcceso>`.

Comportamiento: `qr-rotativo` pide QR al iniciar y cada 30 000 ms; si una petición falla muestra "Sin conexión" y oculta el QR. `solicitudes` consulta cada 2 000 ms; muestra arriba "Por servir" (tarjetas grandes con foto, nombre, turno, tiempo de comida y botón "Servido") y debajo los últimos 10 rechazos con su motivo, sin botones; al presionar Servido la tarjeta sale de la lista. No hay botones de aprobar ni rechazar. `captura-manual` envía al presionar Enter y limpia el campo; su resultado aparece en la lista como cualquier solicitud.

- [ ] **Paso 1: escribir las pruebas que fallan** (fakeAsync): `pideQrAlIniciar`; `renuevaQrCada30Segundos` (`tick(30000)` → segunda petición); `errorDeRed_ocultaQrYMuestraSinConexion`; `solicitudes_consultaCada2Segundos`; `autorizadoSinServir_muestraBotonServido`; `rechazado_noMuestraBotones`; `servido_quitaTarjetaDeLaLista`; `noHayBotonesDeAprobarNiRechazar`; `enter_enviaMatricula`.
- [ ] **Paso 2:** comando con `--include=src/app/punto/**` → FALLA.
- [ ] **Paso 3:** implementar con `timer(0, 30000)`, `timer(0, 2000)` y `switchMap`.
- [ ] **Paso 4:** mismo comando → PASA.
- [ ] **Paso 5:** `git commit -m "feat: pantalla del chef en el punto de entrada"`

## Tarea 14: Módulos Reportes y Administración

**Archivos:** crear `reportes/*` y `admin/*` de la estructura.

**Interfaces:**

- Consume: rutas de las Tareas 9 y 10.
- Produce: `ReportesService.conteo|cotejo|detalle(desde, hasta, filtros?)` y `descargar(ruta, nombreArchivo)` (blob → enlace temporal); `AdminService` con un método por ruta de la Tarea 10.

Pantallas: Reportes con rango de fechas, tablas y botones Excel/PDF; Administración con pestañas Reglas, Puntos, Usuarios, Sincronización (botón "Sincronizar ahora" y bitácora) y Contingencia (formulario con referencia obligatoria). Si la última sincronización falló, Reportes y Administración muestran el aviso "La última sincronización con RR. HH. falló; se usan los datos del {fecha}" (RNF-05).

- [ ] **Paso 1: escribir las pruebas que fallan:** `cotejo_enviaRangoComoQuery`; `descargar_creaArchivoConNombre` (`cotejo-2026-11-01-2026-11-15.xlsx`); `regla_inicioIgualFin_muestraError`; `contingencia_sinReferencia_noEnvia`; `contingencia_409_muestraYaConsumiste`; `sincronizar_muestraResultado`, ultimaSincronizacionFallida\_muestraAviso.
- [ ] **Paso 2:** comando con `--include=src/app/{reportes,admin}/**` → FALLA.
- [ ] **Paso 3:** implementar servicios y componentes.
- [ ] **Paso 4:** `npx ng test --watch=false --browsers=ChromeHeadless` → PASA todo el frontend.
- [ ] **Paso 5:** `git commit -m "feat: reportes y administración en el frontend"`

## Tarea 15: Prueba de carga y aceptación

**Archivos:** crear `load/validacion.k6.js`, `docs/aceptacion.md`.

- [ ] **Paso 1:** escribir el escenario k6: 1,000 empleados simulados con sesión previa; 1 punto de entrada; 50 validaciones por minuto sostenidas durante 20 minutos y un pico de 150 por minuto durante 2 minutos. Umbrales: `http_req_duration{name:celular}` p95 < 2000 ms, `{name:manual}` p95 < 1000 ms, `http_req_failed` < 1 %.
- [ ] **Paso 2:** `k6 run load/validacion.k6.js` contra un entorno con SQL Server → los umbrales pasan; luego verificar que el conteo de Consumo es igual al número de autorizados.
- [ ] **Paso 3:** recorrer cada criterio de la sección 9.2 del SRS en un entorno de prueba y registrar el resultado y la evidencia en `docs/aceptacion.md`.
- [ ] **Paso 4:** `git commit -m "test: carga y aceptación"`

## Fuera de las tareas de código

RNF-06 (HTTPS en la red) y RNF-07 (respaldo diario) se resuelven al instalar en el servidor del cliente: certificado interno en Kestrel o IIS y un trabajo de respaldo de SQL Server. Se documentan en la guía de instalación cuando informática confirme el entorno.
