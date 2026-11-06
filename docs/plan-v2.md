# Plan de implementación v2: operación en la empresa

## Fila del chef, tres roles, panel único e instalación con un .exe

| Campo | Valor |
| --- | --- |
| Versión | 1.0 |
| Fecha | 6 de noviembre de 2026 |
| Implementa | [SRS v2.0](srs.md) |
| Decisiones | [D-33 a D-43](decisiones.md#d-33-tres-roles-empleado-chef-y-administrador) |
| Diseño | [Guía de diseño](diseno.md) |
| Tareas | 11 |
| Plan anterior | [Plan v1](plan.md), terminado |

> **Para agentes:** usar la skill `subagent-driven-development` (recomendada) o `executing-plans` para implementar este plan tarea por tarea. Los pasos usan casillas para dar seguimiento.

**Objetivo:** que el sistema funcione en la empresa sin técnicos: el chef solo mira la TV y presiona cualquier tecla para servir, los empleados hacen fila desde su celular, el administrador tiene un solo panel, y todo se instala con `InstalarComedor.exe` y arranca solo con Windows.

**Arquitectura:** se conserva la arquitectura por capas. La API publica también el frontend (un solo proceso y un solo puerto) y corre como servicio de Windows. La pantalla del chef deja de ser una lista y muestra la **fila de servicio**: el primer autorizado sin servir en grande. La fuente de RR. HH. se lee de una vista de SQL Server o de un Excel importado desde el panel. Las rutas del chef y del administrador solo responden desde la red de la empresa.

**Tecnología:** la de v1 (.NET 8, ASP.NET Core, EF Core 8, SQL Server, Angular 22 con NgModules, xUnit, Moq, Testcontainers, Jasmine/Karma, ClosedXML) más `Microsoft.Extensions.Hosting.WindowsServices`, SQL Server 2022 Express e Inno Setup 6.

**Spec:** [docs/srs.md](srs.md) v2.0. Quien ejecute el plan lee el SRS, este plan y las decisiones D-33 a D-42.

## Restricciones globales

Toda tarea cumple esta lista y las restricciones globales del [plan v1](plan.md#restricciones-globales), salvo donde esta lista las cambia.

- Roles: `Empleado`, `Chef` y `Administrador` ([D-33](decisiones.md#d-33-tres-roles-empleado-chef-y-administrador)). Desaparecen `Punto`, `Encargado` y `Supervisor`; los usuarios existentes migran: `Punto` y `Encargado` → `Chef`, `Supervisor` → `Administrador`.
- Sesión del chef: JWT de 3,650 días (`Jwt:DiasChef = 3650`), revocable: cada petición de Chef o Administrador comprueba que el usuario exista, esté activo y conserve su rol ([D-34](decisiones.md#d-34-sesión-del-chef-sin-caducidad-y-revocable)). Sesión del administrador: 2 h (`Jwt:HorasAdministrador = 2`).
- Fila de servicio: autorizados sin servir del horario en curso, ordenados por `FechaHora` y, a igual hora, por `ConsumoId` ([D-35](decisiones.md#d-35-fila-de-servicio-en-la-pantalla-del-chef)).
- La TV del chef no tiene campos de texto ni botones de aprobar o rechazar. Cualquier tecla, clic o toque marca Servido a la persona que se muestra en grande. Tras marcar, se ignora toda entrada durante 1,500 ms; también se ignoran las teclas repetidas (`KeyboardEvent.repeat`).
- Sin captura manual en la TV ([D-36](decisiones.md#d-36-sin-captura-manual-en-la-pantalla-del-chef)). `POST /api/accesos/manual` se conserva con rol Chef para la huella futura.
- Rutas solo desde la red de la empresa ([D-38](decisiones.md#d-38-panel-y-pantalla-del-chef-solo-desde-la-red-de-la-empresa)): todo `/api/*` salvo `/api/empleados/sesion`, `/api/accesos/celular`, `/api/accesos/mi-turno` y `/api/salud`. Fuera de la red responden 403 con `{ "error": "Disponible solo desde la red de la empresa" }`.
- El administrador puede hacer todo lo que hace el chef: ver la fila y marcar Servido desde su panel ([D-37](decisiones.md#d-37-panel-único-del-administrador)).
- Un solo proceso: la API sirve el frontend desde `wwwroot` en el puerto `Comedor:Puerto` (80 por defecto; 8080 si el 80 está ocupado), en todas las interfaces. Con el puerto 80 la dirección no lleva `:puerto` ([D-43](decisiones.md#d-43-dirección-e-identidad-profesionales)). En producción `apiUrl` es `''` (mismo origen).
- Configuración de la instalación en `%ProgramData%\Comedor\appsettings.Local.json`, fuera del repositorio y de la carpeta del programa; se lee después de `appsettings.json`.
- Sin rastros de Angular: título "Comedor de personal", ícono propio y la fuente Montserrat servida por la propia aplicación (D-43).
- Textos nuevos de la interfaz, exactos: "Sírvele a", "Esperando al siguiente…", "Siguen", "Sin conexión", "Eres el {n}.º en la fila", "Te están sirviendo: pasa con el chef", "Servido. ¡Buen provecho!".

## Foco de revisión

| # | Entrada o condición | Comportamiento esperado | Prueba en |
| --- | --- | --- | --- |
| 1 | El chef presiona dos veces seguidas, deja una tecla apretada o toca dos veces la pantalla | Solo se marca servida la persona mostrada; la siguiente aparece pero no se marca hasta otra pulsación pasados 1,500 ms | Tarea 4 |
| 2 | Dos empleados validan en el mismo milisegundo | Orden estable por `FechaHora` y `ConsumoId`; el celular y la TV coinciden en quién va primero | Tarea 3 |
| 3 | La IP de la PC del comedor cambia (DHCP) y el QR impreso apunta a la IP vieja | `verificar.ps1` lo detecta, avisa "La IP cambió: reimprime el QR" y la guía explica cómo reservar la IP en el router | Tarea 10 |
| 4 | El administrador desactiva al chef, o le cambia el rol, mientras la TV tiene sesión de 10 años | La siguiente consulta de la TV recibe 401 y vuelve al inicio de sesión | Tarea 2 |
| 5 | Un autorizado del desayuno no recogió y ya empezó la comida | No aparece en la fila de la comida; queda como autorizado sin servir en el cotejo | Tarea 3 |

## Estructura de archivos

```text
backend/src/
  Comedor.Application/
    Usuarios/UsuarioSistema.cs                 Roles: Empleado, Chef, Administrador
    Sesion/OpcionesJwt.cs                      DiasChef, HorasAdministrador
    Sesion/IniciarSesionSistema.cs             vigencia por rol + Inicio
    Accesos/ConsultarFila.cs  FilaServicio.cs  (nuevo)
    Accesos/ConsultarMiTurno.cs  MiTurno.cs    (nuevo)
    Sincronizacion/ImportarPersonal.cs         (nuevo)
  Comedor.Infrastructure/
    Externos/FuenteRRHHSqlServer.cs            (nuevo)
    Externos/FuenteRRHHExcel.cs                (nuevo)
    Externos/DiasLaboralesTexto.cs             (nuevo)
    Persistencia/Migraciones/<fecha>_TresRoles.cs
  Comedor.Api/
    Configuracion/ServiciosComedor.cs          roles, validación de usuario en el token, fuente por tipo
    Configuracion/RedLocal.cs                  (nuevo) EsRedLocal + middleware
    Controllers/SaludController.cs             (nuevo)
    Controllers/PersonalController.cs          (nuevo) importar y plantilla
    Program.cs                                 servicio de Windows, estáticos, puerto, config local
frontend/comedor-web/src/
  environments/environment.ts                  apiUrl '' en producción
  environments/environment.development.ts      apiUrl http://localhost:5277
  app/punto/   fila.component.*  punto.service.ts      (se borran solicitudes y captura-manual)
  app/pase/    pase.component.*  pase.service.ts       turno en la fila
  app/panel/   panel.module.ts  panel.component.*      (nuevo) shell con navegación
  app/panel/   personal.component.*                    (nuevo) importar Excel
instalador/
  comedor.iss  publicar.ps1  configurar.ps1  respaldar.ps1  verificar.ps1
docs/
  instalacion.md  manual-chef.md  manual-administrador.md
```

## Tarea 1: Tres roles y migración de usuarios

**Archivos:** modificar `Usuarios/UsuarioSistema.cs` (clase `Roles`), `Administracion/AdministrarUsuarios.cs`, los `[Authorize]` de todos los controladores, `rol.guard` en `app-routing-module.ts`, `usuarios.component.*`; crear la migración `TresRoles`. Probar en `tests/Comedor.Api.Tests/RolesTests.cs` y `AdminTests.cs`.

**Interfaces (produce):**
- `Roles.Empleado = "Empleado"`, `Roles.Chef = "Chef"`, `Roles.Administrador = "Administrador"`, `Roles.TodosDelSistema = "Chef,Administrador"`.
- Migración `TresRoles`: `UPDATE UsuariosSistema SET Rol = 'Chef' WHERE Rol IN ('Punto','Encargado')` y `UPDATE UsuariosSistema SET Rol = 'Administrador' WHERE Rol = 'Supervisor'`. `Down` vacío (no se puede saber el rol original).
- Roles por ruta: Chef y Administrador → `GET /api/puntos/{id}/solicitudes`, `GET /api/puntos/{id}/fila`, `POST /api/consumos/{id}/servido`, `POST /api/accesos/manual`, `GET /api/puntos/{id}/qr` y `GET /api/auth/yo`. Administrador → `/api/admin/*`, `/api/reportes/*`, `/api/sincronizacion/*`.

- [ ] **Paso 1: escribir las pruebas que fallan:** `Migracion_PuntoYEncargado_PasanAChef_SupervisorAAdministrador` (insertar las tres filas con SQL, aplicar la migración, leer roles); `Usuario_RolSupervisor_400`; `Usuario_RolChef_SeGuarda`; `Reportes_ConRolChef_403`; `Reportes_ConRolAdministrador_200`; `Servido_ConRolChef_200`; `Servido_ConRolAdministrador_200`; frontend `usuarios_soloOfreceChefYAdministrador`.
- [ ] **Paso 2:** `dotnet test backend/Comedor.sln --filter "FullyQualifiedName~RolesTests|FullyQualifiedName~AdminTests"` → FALLA.
- [ ] **Paso 3:** implementar; la migración se genera con `dotnet ef migrations add TresRoles` y se le agregan los dos `UPDATE`.
- [ ] **Paso 4:** `dotnet test backend/Comedor.sln` y `npx ng test --watch=false --browsers=ChromeHeadless` → PASA.
- [ ] **Paso 5:** `git commit -m "feat: tres roles (Empleado, Chef, Administrador)"`

## Tarea 2: Sesión del chef sin caducidad, revocable, e inicio por rol

**Archivos:** modificar `Sesion/OpcionesJwt.cs`, `Sesion/IniciarSesionSistema.cs`, `ServiciosComedor.cs` (evento `OnTokenValidated`), `AuthController.cs`, `appsettings.json`, `login.component.ts`, `auth.service.ts`. Probar en `tests/Comedor.Api.Tests/AuthTests.cs` y `core.spec.ts`.

**Interfaces (produce):**
- `OpcionesJwt { int DiasChef = 3650; int HorasAdministrador = 2; }`; se eliminan `HorasSistema` y `DiasPunto`.
- `ResultadoSesionSistema(bool Exito, string? Token, string? Rol, DateTime? ExpiraEn, string? Inicio)`. `Inicio` = `/punto/{id del primer punto activo, por Id}` para Chef y `/panel` para Administrador. Si un chef inicia sesión y no hay ningún punto activo, responde 409 con `{ "error": "No hay un punto de entrada activo; pide al administrador que lo cree." }`.
- `POST /api/auth/login` → `{ token, rol, expiraEn, inicio }`.
- `OnTokenValidated`: si el rol del token es Chef o Administrador, carga el usuario por `sub`; si no existe, está inactivo o su rol es otro, `contexto.Fail("Sesión revocada")` → 401.
- Frontend: guarda `inicio` con la sesión (`comedor.inicio`) y, tras iniciar sesión sin `returnUrl`, navega a él. Nueva ruta `/inicio`: con sesión navega a `comedor.inicio`; sin sesión, a `/login?returnUrl=/inicio`. Es la ruta que abren los accesos directos. Un 401 en cualquier llamada del chef o del administrador borra la sesión y lleva a `/login?returnUrl=<ruta actual>` con el aviso "Tu sesión terminó; vuelve a iniciar sesión". El panel muestra en la barra superior "Sesión hasta las {hh:mm}".

- [ ] **Paso 1: escribir las pruebas que fallan:** `Login_Chef_Expira3650Dias_EInicioEnPunto`; `Login_Administrador_Expira2Horas_EInicioEnPanel`; `TokenChef_UsuarioDesactivado_401`; `TokenAdministrador_RolCambiadoAChef_401EnReportes`; `TokenEmpleado_NoConsultaUsuariosDelSistema` (el empleado no está en `UsuariosSistema` y sigue entrando); frontend `login_sinReturnUrl_navegaAInicio`, `rutaInicio_conSesion_vaAlInicioGuardado`, `rutaInicio_sinSesion_vaALogin`, `Login_ChefSinPuntoActivo_409`, `respuesta401_borraSesionYVaALogin`, `panel_muestraHoraDeFinDeSesion`.
- [ ] **Paso 2:** `dotnet test backend/tests/Comedor.Api.Tests --filter AuthTests` y `--include='src/app/{core,login}/**/*.spec.ts'` → FALLA.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** mismos comandos → PASA.
- [ ] **Paso 5:** `git commit -m "feat: sesión del chef sin caducidad y revocable"`

## Tarea 3: Fila de servicio y turno del empleado (backend)

**Archivos:** crear `Accesos/FilaServicio.cs`, `ConsultarFila.cs`, `MiTurno.cs`, `ConsultarMiTurno.cs`; modificar `PuntosController.cs`, `AccesosController.cs`, `ServiciosComedor.cs`. Probar en `tests/Comedor.Application.Tests/Accesos/FilaTests.cs` y `tests/Comedor.Api.Tests/AccesosApiTests.cs`.

**Interfaces:**
- Consume: `IAccesoRepositorio.SolicitudesAsync`, `IReglaRepositorio`, `IReloj` y el filtro de horario en curso de `ListarSolicitudes`. Extraer ese filtro a `HorarioEnCurso.Contiene(IReadOnlyList<ReglaDerecho> reglas, DateTime ahora, string tiempoComida, DateOnly fechaServicio) → bool` en `Accesos/HorarioEnCurso.cs` y usarlo en ambos.
- Produce: `record FilaServicio(SolicitudPunto? Actual, IReadOnlyList<SolicitudPunto> Siguientes, int Total)`; `ConsultarFila.EjecutarAsync(string puntoId) → Task<FilaServicio>`: autorizados sin servir del horario en curso de las últimas 12 horas, orden ascendente por `FechaHora` y luego `ConsumoId`; `Actual` = el primero; `Siguientes` = los 3 siguientes; `Total` = todos los que esperan, incluido `Actual`.
- `enum EstadoTurno { SinTurno, EnFila, Sirviendo, Servido }`; `record MiTurno(EstadoTurno Estado, int? Posicion)`; `ConsultarMiTurno.EjecutarAsync(string matricula) → Task<MiTurno>`: busca el consumo autorizado más reciente de la matrícula en el horario en curso; servido → `Servido`; primero de la fila de su punto → `Sirviendo` con `Posicion = 1`; otro lugar → `EnFila` con su posición; sin consumo → `SinTurno`.
- `GET /api/puntos/{id}/fila` (Chef) → `FilaServicio`. `GET /api/accesos/mi-turno` (Empleado; matrícula del `sub`) → `MiTurno`.

- [ ] **Paso 1: escribir las pruebas que fallan:** `Fila_OrdenPorLlegada`; `Fila_MismaHora_DesempataPorConsumoId`; `Fila_ExcluyeServidosYRechazos`; `Fila_SoloDelHorarioEnCurso` (autorizado sin servir del desayuno no aparece a la hora de la comida); `Fila_SiguientesMaximoTres_TotalCuentaTodos`; `MiTurno_Primero_Sirviendo`; `MiTurno_Tercero_EnFilaPosicion3`; `MiTurno_TrasServido_Servido`; `MiTurno_SinConsumo_SinTurno`; API `Fila_ConRolEmpleado_403`, `MiTurno_SinToken_401`.
- [ ] **Paso 2:** `dotnet test backend/Comedor.sln --filter "FullyQualifiedName~Fila|FullyQualifiedName~MiTurno"` → FALLA.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** `dotnet test backend/Comedor.sln` → PASA.
- [ ] **Paso 5:** `git commit -m "feat: fila de servicio y turno del empleado"`

## Tarea 4: Pantalla del chef para la TV

**Archivos:** crear `punto/fila.component.*`; modificar `punto.component.*`, `punto.module.ts`, `punto.service.ts`, `modelos/consumo.model.ts`; borrar `solicitudes.component.*`, `captura-manual.component.*` y sus pruebas. Probar en `punto/punto.spec.ts`. Actualizar `docs/diseno.md` §4 y §5 (pantalla del chef).

**Interfaces:**
- Consume: `GET /api/puntos/{id}/fila` y `POST /api/consumos/{id}/servido` (Tareas 1 y 3).
- Produce: `FilaComponent` con la entrada `modo: 'tv' | 'panel'` (por defecto `'tv'`; el panel de la Tarea 6 usa `'panel'`, donde solo marca el botón "Marcar servido"); `PuntoService.fila(puntoId): Observable<FilaServicio>` y `marcarServido(consumoId)`; modelo `FilaServicio { actual: SolicitudPunto | null; siguientes: SolicitudPunto[]; total: number }`.

Comportamiento: pantalla completa en horizontal. Con `actual`: "Sírvele a", el nombre a 96 px o más, turno y tiempo de comida; abajo "Siguen" con los nombres de `siguientes` y el número restante. Sin `actual`: "Esperando al siguiente…". La fila se consulta cada 2,000 ms; si falla, "Sin conexión" y sigue intentando. `@HostListener('document:keydown')`, `document:click` y `document:touchstart` llaman a `servir()`: si hay `actual`, no hay petición en curso, no pasaron 1,500 ms desde la última marca y la tecla no es repetida, envía `marcarServido(actual.consumoId)` y vuelve a pedir la fila. La barra superior muestra el punto y la hora. Sin campos de texto ni botones.

- [ ] **Paso 1: escribir las pruebas que fallan** (fakeAsync): `conActual_muestraSirveleAYNombre`; `sinActual_muestraEsperando`; `muestraSiguientesYTotal`; `teclaCualquiera_marcaServidoDelActual`; `clic_marcaServido`; `toque_marcaServido`; `sinActual_teclaNoEnviaNada`; `dosTeclasSeguidas_unaSolaPeticion`; `teclaPasados1500ms_marcaAlSiguiente`; `teclaRepetida_ignorada`; `consultaCada2Segundos`; `errorDeRed_muestraSinConexion`; `noHayCamposNiBotones`.
- [ ] **Paso 2:** `--include='src/app/punto/**/*.spec.ts'` → FALLA.
- [ ] **Paso 3:** implementar con `timer(0, 2000)` y `switchMap`.
- [ ] **Paso 4:** `npx ng test --watch=false --browsers=ChromeHeadless` → PASA.
- [ ] **Paso 5:** `git commit -m "feat: pantalla del chef con fila de servicio"`

## Tarea 5: Turno en la fila en el celular

**Archivos:** modificar `pase/pase.component.*`, `pase.service.ts`, `modelos/pase.model.ts`. Probar en `pase/pase.spec.ts`.

**Interfaces:**
- Consume: `GET /api/accesos/mi-turno` (Tarea 3).
- Produce: `PaseService.miTurno(): Observable<MiTurno>`; modelo `MiTurno { estado: 'SinTurno' | 'EnFila' | 'Sirviendo' | 'Servido'; posicion: number | null }`.

Comportamiento: con el pase Autorizado, el celular consulta `miTurno` cada 3,000 ms y muestra bajo AUTORIZADO: `EnFila` → "Eres el {posicion}.º en la fila"; `Sirviendo` → "Te están sirviendo: pasa con el chef"; `Servido` → "Servido. ¡Buen provecho!" y deja de consultar; `SinTurno` → deja de consultar sin texto. Si una consulta falla, conserva el último texto y reintenta. El pase rechazado no consulta.

- [ ] **Paso 1: escribir las pruebas que fallan:** `autorizado_consultaTurnoCada3Segundos`; `enFila_muestraPosicion`; `sirviendo_muestraPasaConElChef`; `servido_muestraBuenProvecho_yDejaDeConsultar`; `rechazado_noConsultaTurno`; `errorAlConsultar_conservaTexto`.
- [ ] **Paso 2:** `--include='src/app/pase/**/*.spec.ts'` → FALLA.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** mismo comando → PASA.
- [ ] **Paso 5:** `git commit -m "feat: turno en la fila en el celular"`

## Tarea 6: Panel único del administrador

**Archivos:** crear `panel/panel.module.ts`, `panel.component.*`, `panel/fila-panel.component.*`; modificar `app-routing-module.ts`, `reportes.module.ts`, `admin.module.ts`, `aviso-sincronizacion.component.ts`. Probar en `panel/panel.spec.ts`. Actualizar `docs/diseno.md` §4.

**Interfaces:**
- Produce: ruta `/panel` (rol Administrador) con barra superior (usuario y rol), aviso de sincronización y navegación lateral en dos grupos: **Reportes** (`/panel/cotejo`, `/panel/conteo`, `/panel/detalle`) y **Administración** (`/panel/reglas`, `/panel/puntos`, `/panel/usuarios`, `/panel/personal`, `/panel/sincronizacion`, `/panel/contingencia`), más un grupo **Comedor** con `/panel/fila`: la misma fila de la TV dentro del panel (persona actual, siguientes y botón "Marcar servido"; aquí la tecla no marca, solo el botón) y el enlace "Abrir pantalla del chef" (`/punto/{id}` en una pestaña nueva). `/panel` abre `/panel/fila` durante un horario de comida y `/panel/cotejo` fuera de él. `/reportes` y `/admin` redirigen a `/panel`.
- `FilaComponent` (Tarea 4) recibe la entrada `modo: 'tv' | 'panel'`: en `tv` escucha teclas, clics y toques; en `panel` solo el botón.
- Las pantallas existentes se reutilizan como rutas hijas; las pestañas Cotejo, Conteo y Detalle de `ReportesComponent` pasan a ser rutas.

- [ ] **Paso 1: escribir las pruebas que fallan:** `panel_muestraLosTresGrupos`; `panel_enHorario_abreFila`; `panel_fueraDeHorario_abreCotejo`; `filaEnPanel_teclaNoMarca_botonSiMarca`; `abrirPantallaDelChef_enlaceAPunto`; `rutaReportes_redirigeAPanel`; `rutaAdmin_redirigeAPanel`; `panel_conRolChef_vaALogin`; `navegacion_marcaRutaActiva` (`aria-current="page"`).
- [ ] **Paso 2:** `--include='src/app/{panel,reportes,admin}/**/*.spec.ts'` → FALLA.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** `npx ng test --watch=false --browsers=ChromeHeadless` → PASA.
- [ ] **Paso 5:** `git commit -m "feat: panel único del administrador"`

## Tarea 7: Panel y pantalla del chef solo desde la red de la empresa

**Archivos:** crear `Comedor.Api/Configuracion/RedLocal.cs`; modificar `Program.cs`. Probar en `tests/Comedor.Api.Tests/RedLocalTests.cs`.

**Interfaces (produce):**
- `static bool RedLocal.EsRedLocal(IPAddress? ip, IHeaderDictionary encabezados)`: `true` si no hay encabezados `CF-Connecting-IP` ni `X-Forwarded-For` y la IP es nula (servidor de pruebas), de loopback o privada: 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16, 169.254.0.0/16, fc00::/7, fe80::/10, incluidas las IPv4 mapeadas a IPv6.
- `RedLocal.RutasPublicas = ["/api/empleados/sesion", "/api/accesos/celular", "/api/accesos/mi-turno", "/api/salud"]`. Un middleware antes de la autenticación responde 403 con `{ "error": "Disponible solo desde la red de la empresa" }` a cualquier `/api/*` que no esté en esa lista cuando `EsRedLocal` es `false`. Los archivos del frontend no se filtran.

- [ ] **Paso 1: escribir las pruebas que fallan:** `EsRedLocal_IpsPrivadasYLoopback_True` (teoría con 127.0.0.1, ::1, 10.1.2.3, 172.20.0.1, 192.168.1.50, ::ffff:192.168.1.50); `EsRedLocal_IpPublica_False` (8.8.8.8, 2001:4860::1); `EsRedLocal_ConCfConnectingIp_False`; `EsRedLocal_ConXForwardedFor_False`; `Login_DesdeTunel_403`; `Reportes_DesdeTunel_403`; `SesionEmpleado_DesdeTunel_NoEs403`; `AccesoCelular_DesdeTunel_NoEs403`.
- [ ] **Paso 2:** `dotnet test backend/tests/Comedor.Api.Tests --filter RedLocalTests` → FALLA.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** `dotnet test backend/Comedor.sln` → PASA.
- [ ] **Paso 5:** `git commit -m "feat: panel y chef solo desde la red de la empresa"`

## Tarea 8: Fuente de RR. HH. en SQL Server o Excel

**Archivos:** crear `Externos/DiasLaboralesTexto.cs`, `FuenteRRHHSqlServer.cs`, `FuenteRRHHExcel.cs`, `Sincronizacion/ImportarPersonal.cs`, `Controllers/PersonalController.cs`, `panel/personal.component.*`; modificar `ServiciosComedor.cs`, `admin.service.ts`. Probar en `tests/Comedor.Api.Tests/FuenteRRHHTests.cs`, `PersonalTests.cs` y `panel/panel.spec.ts`.

**Interfaces:**
- Columnas, en la vista y en el Excel, con estos nombres exactos: `Matricula`, `Nombre`, `Area`, `Turno`, `DiasLaborales`, `InicioContrato`, `FinContrato`, `FotoUrl` (opcional).
- `DiasLaboralesTexto.Parse(string texto) → IReadOnlySet<DayOfWeek>`: letras `L, M, X, J, V, S, D` separadas por coma, sin importar espacios ni mayúsculas (`"L,M,X,J,V"` = lunes a viernes). Una letra desconocida lanza `FormatException`.
- `FuenteRRHHSqlServer(string cadena, string vista) : IFuentePersonalRRHH`: `SELECT` de esas columnas desde `vista`; el nombre de la vista se valida contra `^[A-Za-z0-9_.\[\]]+$` antes de usarlo.
- `FuenteRRHHExcel(string ruta) : IFuentePersonalRRHH`: primera hoja, fila 1 = encabezados; una fila con fecha o días inválidos se descarta como fila omitida.
- Configuración `Fuente:Tipo` = `Simulada` (por defecto), `SqlServer` (`Fuente:Cadena`, `Fuente:Vista`, por defecto `vw_EmpleadosComedor`) o `Excel` (`Fuente:RutaExcel`, por defecto `%ProgramData%\Comedor\personal.xlsx`).
- `ImportarPersonal.EjecutarAsync(Stream excel) → Task<ResultadoSincronizacion>`: guarda el archivo en `Fuente:RutaExcel` y sincroniza con `FuenteRRHHExcel` usando `SincronizarPersonal` (D-28 aplica igual).
- `POST /api/admin/personal/importar` (multipart, campo `archivo`, solo `.xlsx`, máximo 5 MB; si no, 400 "Sube un archivo .xlsx de hasta 5 MB") → `ResultadoSincronizacion`. `GET /api/admin/personal/plantilla.xlsx` → plantilla con los encabezados y una fila de ejemplo.
- Pestaña **Personal** del panel: botón "Descargar plantilla", selector de archivo, botón "Importar" y el resultado (altas, cambios, bajas, omitidos).

- [ ] **Paso 1: escribir las pruebas que fallan:** `DiasLaborales_LunesAViernes`; `DiasLaborales_EspaciosYMinusculas`; `DiasLaborales_LetraDesconocida_Lanza`; `Excel_LeeEmpleados`; `Excel_FilaConFechaInvalida_SeOmite`; `SqlServer_LeeVista` (Testcontainers: crear una tabla y la vista `vw_EmpleadosComedor`); `SqlServer_VistaConNombreInvalido_Lanza`; `Importar_Xlsx_SincronizaYDevuelveResultado`; `Importar_NoXlsx_400`; `Importar_ConRolChef_403`; `Plantilla_DevuelveXlsxConEncabezados`; frontend `personal_importar_muestraResultado`, `personal_sinArchivo_noEnvia`.
- [ ] **Paso 2:** `dotnet test backend/Comedor.sln --filter "FullyQualifiedName~FuenteRRHH|FullyQualifiedName~PersonalTests"` → FALLA.
- [ ] **Paso 3:** implementar; la fuente se elige en `ServiciosComedor` según `Fuente:Tipo`.
- [ ] **Paso 4:** `dotnet test backend/Comedor.sln` y `npx ng test --watch=false --browsers=ChromeHeadless` → PASA.
- [ ] **Paso 5:** `git commit -m "feat: fuente de RR. HH. en SQL Server o Excel"`

## Tarea 9: Un solo ejecutable que sirve el frontend y corre como servicio

**Archivos:** modificar `Program.cs`, `Comedor.Api.csproj`, `environments/environment.ts` (producción), crear `environments/environment.development.ts` y `Controllers/SaludController.cs`; modificar `angular.json` (`fileReplacements` en desarrollo). Probar en `tests/Comedor.Api.Tests/HospedajeTests.cs`.

**Interfaces (produce):**
- `GET /api/salud` (anónimo) → `{ "estado": "ok", "version": "<versión del ensamblado>" }`.
- `Program`: `builder.Host.UseWindowsService(o => o.ServiceName = "Comedor")`; agrega `%ProgramData%\Comedor\appsettings.Local.json` (opcional) después de los demás archivos de configuración; escucha en `http://0.0.0.0:{Comedor:Puerto}` (80 por defecto) salvo que `ASPNETCORE_URLS` esté definido; `UseDefaultFiles`, `UseStaticFiles` y `MapFallbackToFile("index.html")` solo para rutas que no empiezan con `/api`.
- Frontend: `environment.ts` (producción) con `apiUrl: ''`; `environment.development.ts` con `apiUrl: 'http://localhost:5277'`.

- [ ] **Paso 1: escribir las pruebas que fallan:** `Salud_Anonimo_200ConVersion`; `RutaDelFrontend_DevuelveIndexHtml` (con un `wwwroot/index.html` de prueba); `RutaApiInexistente_404NoIndex`; `ConfiguracionLocal_SobrescribeAppsettings` (un archivo temporal pasado por la variable `COMEDOR_CONFIG_LOCAL`, que solo usan las pruebas, cambia `Comedor:ZonaHoraria`).
- [ ] **Paso 2:** `dotnet test backend/tests/Comedor.Api.Tests --filter HospedajeTests` → FALLA.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** `dotnet test backend/Comedor.sln`, `npx ng test --watch=false --browsers=ChromeHeadless` y `npx ng build` → PASA; `dist/comedor-web/browser/index.html` existe.
- [ ] **Paso 5:** `git commit -m "feat: ejecutable único con frontend y servicio de Windows"`

## Tarea 10: Instalador InstalarComedor.exe, respaldos y guías

**Archivos:** crear `instalador/comedor.iss`, `publicar.ps1`, `configurar.ps1`, `respaldar.ps1`, `verificar.ps1`, `docs/instalacion.md`, `docs/manual-chef.md`, `docs/manual-administrador.md`; modificar `README.md`.

**Interfaces (produce):**
- `publicar.ps1`: `npx ng build` → copia `dist/comedor-web/browser` a `publicacion/wwwroot`; `dotnet publish backend/src/Comedor.Api -c Release -r win-x64 --self-contained true -o publicacion`; luego `iscc instalador/comedor.iss` → `salida/InstalarComedor.exe`. No necesita .NET instalado en la PC de la empresa.
- `comedor.iss` (Inno Setup 6), asistente en español con una página propia: usuario y contraseña del administrador inicial (mínimo 8 caracteres) y puerto: 80 por defecto; si está ocupado, el asistente lo dice y propone 8080. Instala en `C:\Program Files\Comedor` y, en orden: (1) si no existe la instancia `SQLEXPRESS`, descarga e instala SQL Server 2022 Express en silencio con `/QS /ACTION=Install /FEATURES=SQLEngine /INSTANCENAME=SQLEXPRESS /SQLSYSADMINACCOUNTS="NT AUTHORITY\SYSTEM" "BUILTIN\Administrators" /IACCEPTSQLSERVERLICENSETERMS`; (2) corre `configurar.ps1`; (3) crea el servicio `Comedor` (inicio automático, cuenta LocalSystem, reinicio ante fallas) y lo arranca; (4) crea accesos directos en el escritorio: **Pantalla del chef** (`msedge --kiosk http://localhost:{puerto}/inicio --edge-kiosk-type=fullscreen`) y **Panel del comedor** (`http://localhost:{puerto}/panel`). La desinstalación quita el servicio, la regla de firewall, la tarea de respaldo y los accesos directos, y conserva la base y `%ProgramData%\Comedor`.
- `configurar.ps1 -Puerto <int> -AdminUsuario <string> -AdminContrasena <string>`: si no existe, crea `%ProgramData%\Comedor\appsettings.Local.json` con `Jwt:Clave` y `Qr:ClaveFirma` aleatorias de 48 caracteres, la cadena `Server=.\SQLEXPRESS;Database=Comedor;Trusted_Connection=True;TrustServerCertificate=True`, `Semilla:Admin`, `Comedor:Puerto` y `App:UrlPublica = http://<IP de la red local>` (con `:<puerto>` solo si no es 80); deja ese archivo legible solo para SYSTEM y Administradores; crea la regla de firewall "Comedor" (TCP, puerto, `RemoteAddress LocalSubnet`) y la tarea programada "Comedor - Respaldo" diaria a las 03:00. Si el archivo ya existe (actualización), no lo toca.
- `respaldar.ps1`: `BACKUP DATABASE Comedor` a `%ProgramData%\Comedor\Respaldos\Comedor-<aaaaMMdd-HHmm>.bak` y borra los respaldos de más de 30 días (RNF-07).
- `verificar.ps1`: comprueba y muestra en verde o rojo: servicio `Comedor` en ejecución, `GET /api/salud` 200, regla de firewall, tarea de respaldo y que el host de `App:UrlPublica` sea una IP actual del equipo; si no lo es, muestra "La IP cambió: reimprime el QR desde Panel > Puntos" y la IP nueva.
- Guías en lenguaje sencillo, sin términos técnicos: `instalacion.md` (requisitos, instalar, reservar la IP en el router, imprimir el QR, verificar, actualizar, desinstalar), `manual-chef.md` (una página: abrir "Pantalla del chef", presionar cualquier tecla para servir, qué hacer si dice "Sin conexión") y `manual-administrador.md` (panel, reportes, reglas, usuarios, importar personal, respaldos).

- [ ] **Paso 1: escribir la prueba de humo** `instalador/verificar.ps1` y la lista de comprobación de `instalacion.md`, §"Verificación".
- [ ] **Paso 2:** en una máquina virtual con Windows 10 o 11 limpia, sin .NET ni SQL Server, correr `verificar.ps1` → todo en rojo.
- [ ] **Paso 3:** implementar `publicar.ps1`, `comedor.iss`, `configurar.ps1` y `respaldar.ps1`; generar `InstalarComedor.exe`.
- [ ] **Paso 4:** en la misma VM: instalar con el .exe; `verificar.ps1` → todo en verde; reiniciar Windows y volver a verificar; desde un celular en la misma red, escanear el QR impreso desde el panel, iniciar sesión y ver la fila en la TV; correr `respaldar.ps1` y confirmar el `.bak`; reinstalar encima (actualización) y confirmar que los datos siguen; desinstalar y confirmar que la base y los respaldos se conservan. Registrar el resultado en `docs/aceptacion.md`, sección "Instalación v2".
- [ ] **Paso 5:** `git commit -m "feat: instalador, respaldos y guías de uso"`

## Tarea 11: Dirección e identidad profesionales

**Archivos:** modificar `frontend/comedor-web/src/index.html`, `public/` (ícono), `src/styles.scss`, `pase/pase.component.ts`; crear `src/assets/fuentes/` con Montserrat (licencia OFL incluida) y `public/icono.svg`. Probar en `pase/pase.spec.ts` y `app.spec.ts`. Actualizar `docs/diseno.md` §2.2.

**Interfaces (produce):**
- Sin rastros de Angular: se borra `favicon.ico`; `index.html` usa `icono.svg` (un plato con cubiertos en verde institucional `#235B4E` sobre fondo claro, sin logotipos de terceros) y `<meta name="theme-color" content="#10312B">`; el título es "Comedor de personal" en toda pantalla, más el nombre de la sección (por ejemplo "Panel · Cotejo de factura").
- Montserrat se sirve desde la aplicación (`@font-face` en `styles.scss` con los pesos 400, 500, 600, 700 y 800); se quitan los `<link>` a Google Fonts para que el sistema se vea igual sin internet.
- El celular quita el código del QR de la barra de direcciones en cuanto lo lee: `history.replaceState` deja la dirección como `/pase`, y el token se conserva en memoria y en `sessionStorage` para reintentos de la misma visita.

- [ ] **Paso 1: escribir las pruebas que fallan:** `index_sinFaviconDeAngular_conIconoPropio`; `index_sinGoogleFonts`; `titulo_cambiaPorSeccion`; `pase_quitaTokenDeLaDireccion`; `pase_recargaConTokenEnSesion_validaDeNuevo`.
- [ ] **Paso 2:** `--include='src/app/{pase,app}*/**/*.spec.ts'` y la prueba de `index.html` → FALLA.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** `npx ng test --watch=false --browsers=ChromeHeadless` y `npx ng build` → PASA; `dist/comedor-web/browser` no contiene `favicon.ico`.
- [ ] **Paso 5:** `git commit -m "feat: identidad propia y dirección limpia"`

## Fuera de este plan

- **Acceso desde datos móviles u otro Wi-Fi y nombre propio en la dirección** ([D-41](decisiones.md#d-41-acceso-desde-internet-implementación-futura), [D-43](decisiones.md#d-43-dirección-e-identidad-profesionales)): Cloudflare Tunnel con dominio propio; la dirección pasa de `http://192.168.1.20` a `https://comedor.tuempresa.com`. La Tarea 7 ya deja el panel y la pantalla del chef cerrados ante el túnel; activar el acceso será configuración, no código.
- **Inicio de sesión real del empleado** ([D-40](decisiones.md#d-40-contraseña-de-la-empresa-pendiente-y-bloqueante-para-producción)): el adaptador de `IAutenticadorEmpresa` se hace cuando se conozca el sistema de la empresa. **Sin él no se puede usar en producción.**
- **Huella** (D-21, RF-16): futura; reutiliza el canal manual.
