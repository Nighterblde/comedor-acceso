# Registro de decisiones

Cada decisión indica su estado, el motivo y las alternativas descartadas. Una decisión aceptada solo cambia con una nueva entrada que la sustituya.

| # | Decisión | Estado |
| --- | --- | --- |
| [D-01](#d-01-dividir-la-plataforma-en-subproyectos) | Dividir la plataforma en subproyectos; empezar por control de acceso y consumo | Aceptada |
| [D-02](#d-02-aplicación-web-central-on-premise) | Aplicación web central on-premise | Aceptada |
| [D-03](#d-03-c-por-capas-y-angular-con-separación-mvc) | C# por capas y Angular con separación MVC | Aceptada |
| [D-04](#d-04-derecho-por-turno-fijo-y-días-laborales) | Derecho por turno fijo y días laborales, leídos de RR. HH. | Aceptada |
| [D-05](#d-05-entrada-por-celular-con-qr-rotativo) | Entrada por celular con QR rotativo | Sustituida por D-21 |
| [D-06](#d-06-el-chef-solo-marca-servido) | El chef solo marca Servido; no aprueba ni rechaza | Aceptada |
| [D-07](#d-07-registros-de-solo-inserción) | Registros de solo inserción | Aceptada |
| [D-08](#d-08-fecha-de-servicio-y-jornada-anterior) | Fecha de servicio y jornada anterior | Aceptada |
| [D-09](#d-09-el-cotejo-de-la-factura-cuenta-comidas-servidas) | El cotejo de la factura cuenta comidas servidas | Aceptada, revisable |
| [D-10](#d-10-integraciones-simuladas-en-la-v1) | Integraciones simuladas en la v1 | Aceptada |
| [D-11](#d-11-uso-de-la-cuenta-de-otro-empleado) | Uso de la cuenta de otro empleado como riesgo aceptado | Aceptada |
| [D-12](#d-12-un-solo-punto-de-entrada) | Un solo punto de entrada | Aceptada |
| [D-13](#d-13-diseño-institucional-sin-identidad-oficial) | Diseño institucional sin identidad oficial | Aceptada |
| [D-14](#d-14-el-conteo-también-se-exporta) | El conteo también se exporta a Excel y PDF | Aceptada |
| [D-15](#d-15-reglas-de-la-captura-de-contingencia) | Reglas de la captura de contingencia | Aceptada |
| [D-16](#d-16-sin-protección-del-último-administrador) | Sin protección del último administrador activo | Aceptada, riesgo conocido |
| [D-17](#d-17-turnos-y-tiempos-de-comida-como-texto-libre) | Turnos y tiempos de comida como texto libre | Aceptada, revisable |
| [D-18](#d-18-detalles-de-la-api-no-fijados-en-el-plan) | Detalles de la API no fijados en el plan | Aceptada |
| [D-19](#d-19-el-pase-no-muestra-el-área) | El pase no muestra el área del empleado (mejora futura) | Pospuesta |
| [D-20](#d-20-la-pantalla-del-chef-no-muestra-foto) | La pantalla del chef no muestra foto (mejora futura) | Pospuesta |
| [D-21](#d-21-qr-permanente-impreso) | QR permanente impreso, sin comprobar presencia | Aceptada, riesgo conocido |
| [D-22](#d-22-estado-de-la-sincronización-para-el-aviso-de-rnf-05) | Estado de la sincronización para el aviso de RNF-05 | Aceptada |
| [D-23](#d-23-detalles-del-frontend-de-reportes-y-administración) | Detalles del frontend de Reportes y Administración | Aceptada |
| [D-24](#d-24-entorno-de-desarrollo-y-pruebas) | Entorno de desarrollo y pruebas | Aceptada |
| [D-25](#d-25-detalles-del-dominio-y-la-persistencia) | Detalles del dominio y la persistencia | Aceptada, riesgo conocido |
| [D-26](#d-26-detalles-de-la-validación-de-acceso) | Detalles de la validación de acceso | Aceptada |
| [D-27](#d-27-sesión-del-empleado-y-bloqueo-por-intentos) | Sesión del empleado y bloqueo por intentos | Aceptada, riesgo conocido |
| [D-28](#d-28-reglas-de-la-sincronización-de-personal) | Reglas de la sincronización de personal | Aceptada, revisable |
| [D-29](#d-29-api-base-seguridad-y-configuración) | API base, seguridad y configuración | Aceptada |
| [D-30](#d-30-pantalla-del-chef-y-endpoints-de-acceso) | Pantalla del chef y endpoints de acceso | Aceptada |

## D-01 Dividir la plataforma en subproyectos

El documento de partida describe 15 módulos más funciones hospitalarias. Especificarlos juntos produce un diseño superficial. Se divide en subproyectos con su propio SRS y plan, y se empieza por el problema más urgente del cliente: saber quién come, si tiene derecho y si repite.

## D-02 Aplicación web central on-premise

Un servidor en la red del cliente con una sola base de datos; los dispositivos usan el navegador.

- **Motivo:** con un solo punto de entrada en red interna, una sola base de datos garantiza que nadie consuma dos veces. Es lo que más importa en un conteo que sirve para pagar al proveedor.
- **Descartado:** aplicación instalada por punto con base local (conflictos al sincronizar sin conexión) y software comercial (mala integración con la base de turnos propia).

## D-03 C# por capas y Angular con separación MVC

Backend en ASP.NET Core sobre .NET 8 LTS con capas Dominio, Aplicación, Infraestructura y API. Frontend en Angular con NgModules: modelos, componentes como vista y servicios como controlador.

- **Motivo:** las reglas de derecho viven en el dominio y se prueban sin base de datos; cambiar la fuente de RR. HH. solo toca Infraestructura.

## D-04 Derecho por turno fijo y días laborales

Cada empleado tiene un turno fijo durante su contrato y un rol de días laborales y descansos; ambos se leen de la base de RR. HH. Sin derecho en días de descanso ni con contrato vencido.

## D-05 Entrada por celular con QR rotativo

> **Sustituida por [D-21](#d-21-qr-permanente-impreso).** El QR ya no rota ni caduca.

El punto muestra un QR que cambia cada 30 s; el token firmado es válido 60 s. El empleado lo escanea con su celular e inicia sesión con sus credenciales de la empresa.

- **Motivo:** más accesible que credenciales físicas, y el QR rotativo impide registrarse sin estar en el comedor (por ejemplo, con una foto del código).
- **Dependencias críticas:** Wi-Fi accesible desde celulares y certificado HTTPS reconocido por los celulares.
- **Futuro:** identificación por huella con el sistema existente del cliente.

## D-06 El chef solo marca Servido

Cada solicitud aparece en la pantalla del chef con foto y nombre. El sistema decide; el chef entrega y marca Servido.

- **Motivo:** si las reglas funcionan, no hay razón para que el chef rechace a alguien. Además, la marca de entrega hace que el conteo refleje comidas realmente servidas.

## D-07 Registros de solo inserción

Consumo, Rechazo y Entrega no se editan ni se borran, por ningún rol. Una restricción única en base de datos impide dos consumos del mismo empleado en el mismo tiempo de comida.

- **Motivo:** el conteo es la base para pagar al proveedor y debe resistir una auditoría.

## D-08 Fecha de servicio y jornada anterior

Cada consumo se asigna a la fecha de servicio: el día en que empezó la jornada a la que pertenece la comida. Una regla puede marcarse como "de la jornada del día anterior", como el desayuno del turno nocturno al salir.

- **Motivo:** quien trabaja el viernes en la noche y descansa el sábado debe poder desayunar el sábado a las 6:00 sin que el sistema lo rechace por día de descanso.

## D-09 El cotejo de la factura cuenta comidas servidas

El reporte de cotejo cuenta comidas servidas y muestra aparte las autorizadas sin servir. Las capturas de contingencia cuentan como servidas.

- **Revisable:** si el cliente decide pagar por comida autorizada, el cambio está en una sola consulta.

## D-10 Integraciones simuladas en la v1

La fuente de RR. HH. y el autenticador de la empresa se programan contra interfaces (`IFuentePersonalRRHH`, `IAutenticadorEmpresa`) con adaptadores simulados. Los reales se agregan al confirmar el acceso con informática.

## D-11 Uso de la cuenta de otro empleado

Si alguien usa credenciales ajenas, la comida se descuenta al titular y el total no aumenta: la factura sigue correspondiendo a comidas servidas. Se acepta el riesgo; el chef detecta los casos evidentes.

## D-12 Un solo punto de entrada

El comedor tiene una sola entrada, donde el chef sirve la comida. Ahí está la única pantalla con el QR rotativo, la lista "Por servir" y la captura manual.

- **Consecuencia:** la concurrencia que importa es la de un mismo empleado por dos canales (celular y captura manual) o el doble clic en Servido; ambas las cubre la restricción única.
- **Futuro:** el modelo conserva la entidad PuntoEntrada, así que agregar otra entrada no cambia el diseño.

## D-13 Diseño institucional sin identidad oficial

La interfaz usa un estilo institucional sencillo (Montserrat, verde institucional y acento dorado) definido en la [guía de diseño](diseno.md), sin logotipos, escudos ni nombres de instituciones.

- **Motivo:** la identidad oficial pertenece al cliente; solo se incorpora si la proporciona y autoriza su uso.

## D-14 El conteo también se exporta

El reporte de conteo (RF-20) se exporta a Excel y PDF igual que el cotejo, con una fila final "Total" que suma la columna Total.

- **Motivo:** RF-23 pide exportar los reportes en general; el plan v1.1 solo exportaba el cotejo y el detalle. El conteo por turno y área sirve, por ejemplo, para repartir el costo entre áreas fuera del sistema.

## D-15 Reglas de la captura de contingencia

El administrador captura la lista en papel como consumos de canal Contingencia (RF-26).

- Solo aplica la restricción única (matrícula, fecha de servicio, tiempo de comida); no se revisan turno, horario ni día de descanso, porque la comida ya se sirvió.
- La matrícula, normalizada, debe existir como empleado sincronizado; si no, se rechaza con "No registrado". **Motivo:** la lista se escribe a mano y un error de dedo dejaría un consumo de alguien inexistente. Confirmado por el dueño del producto.
- La referencia a la lista y el tiempo de comida son obligatorios.
- El consumo queda en el punto de entrada activo (D-12), con la fecha y hora de la captura y el administrador como autor.
- Un duplicado responde 409 con "Ya consumiste este tiempo de comida".

## D-16 Sin protección del último administrador

Un administrador puede desactivar o cambiar de rol a cualquier usuario, incluido el último administrador activo. No se agrega una regla que lo impida.

- **Riesgo conocido:** si se queda sin administradores activos, nadie puede administrar reglas, usuarios, sincronización ni contingencia. El administrador semilla solo se crea cuando no hay ningún usuario, así que la recuperación es una corrección directa en la base de datos.
- **Motivo:** el dueño del producto considera que en la práctica no se desactivarán administradores.

## D-17 Turnos y tiempos de comida como texto libre

Turno y tiempo de comida se guardan como texto, sin catálogo, y se comparan distinguiendo mayúsculas y minúsculas.

- **Consecuencia:** "matutino" y "Matutino" serían turnos distintos y el chequeo de traslape no los compararía. Los nombres deben coincidir con los que entrega RR. HH.
- **Revisable:** si se confirma la lista de turnos con el cliente (SRS §11), conviene un catálogo.

## D-18 Detalles de la API no fijados en el plan

Decisiones de implementación de las Tareas 9 a 12, tomadas con la lectura mínima del plan:

- **Reportes:** Turno, Área y Nombre salen del empleado unido por matrícula; si no existe, "Sin dato" (y nombre vacío). En el detalle, Resultado es "Autorizado" o "Rechazado" y Motivo es el texto exacto del rechazo; los rechazos sin fecha de servicio se filtran por la fecha en que ocurrieron. Fechas faltantes, inválidas o `desde > hasta` responden 400. Los archivos se llaman `<reporte>-<desde>_<hasta>.<ext>` y sus celdas de texto se guardan como texto literal.
- **Administración:** `DELETE /api/admin/reglas?turno=&tiempoComida=` (404 si no existe). Al editar una regla, el traslape no se compara contra ella misma. Los errores 400 y 409 responden `{ "error": "<texto>" }`. El rol de un usuario debe ser uno de los cuatro del sistema; la contraseña es obligatoria al crear y opcional al editar. La baja de puntos y usuarios es `activo = false`.
- **Frontend:** se conservan los nombres generados por Angular 22 (`app-module.ts`, `app-routing-module.ts`). Si falta el rol, el guard redirige a `/login?returnUrl=<ruta>` y, tras iniciar sesión, se vuelve a esa ruta; entrando directo a `/login` no hay página de inicio por rol.
- **Pase del empleado (Tarea 12):** los errores de sesión de la API se traducen a los tres textos del plan ("Credenciales incorrectas", "Cuenta bloqueada por 15 minutos" y, para cualquier otro error o falta de conexión, "No se pudo verificar tu cuenta; acude con el encargado"). Un error de red o del servidor al validar el pase muestra el texto ya especificado "Escanea de nuevo el código de la entrada", sin crear un texto nuevo. `/pase` sin código QR y con sesión muestra ese mismo texto sin validar. El inicio de sesión del celular muestra el punto leído del propio QR (por ejemplo "Entrada P1"). Con el formulario incompleto, el botón Entrar queda desactivado, sin mensaje adicional. En un rechazo "No registrado" se ocultan los datos que la API no envía.

## D-19 El pase no muestra el área

El pase del celular muestra foto, nombre, matrícula, turno, tiempo de comida, reloj y, si es rechazo, el motivo. El área del empleado queda como **mejora futura**, fuera de la v1.

- **Motivo:** la respuesta de validación de la API no incluye el área y el pase cumple su función sin ella. El dueño del producto prefiere enfocarse en lo especificado.
- **Si se retoma:** agregar `area` a `ResultadoAcceso` en la API (el dato ya está en Empleado) y mostrarla en la tarjeta del pase; después, quitar esta nota de la guía de diseño (§4).

## D-20 La pantalla del chef no muestra foto

Las tarjetas "Por servir" y los rechazos de la pantalla del chef muestran el nombre, el turno y el tiempo de comida, sin foto. La foto queda como **mejora futura**, sujeta a que RR. HH. la proporcione (SRS §11). El pase del celular sí la muestra cuando existe (RF-11).

- **Motivo:** al chef solo le interesa a quién servir y si tiene derecho; el dueño del producto prefiere enfocarse en lo especificado.

## D-21 QR permanente impreso

El QR del punto es permanente y va impreso en un cartel en la entrada. Su función es dar acceso a la aplicación: el empleado lo escanea, inicia sesión si no la tiene (se recuerda 30 días) y el sistema valida su derecho y avisa a la pantalla del chef. Sustituye a D-05.

- **El token no caduca.** Sigue firmado (identifica el punto y no se puede falsificar) y solo vale si el punto está activo. El administrador lo obtiene en Administración > Puntos para imprimirlo.
- **Riesgo aceptado:** el QR no comprueba que el empleado esté en el comedor; alguien con una foto del cartel o el enlace puede pedir su comida desde fuera. Cada empleado sigue limitado a un consumo por tiempo de comida, y una comida no recogida queda sin servir y no se paga (D-09). Para invalidar un QR filtrado se desactiva el punto o se cambia `Qr:ClaveFirma` y se reimprime.
- **Futuro:** la identificación preferida es la huella con el sistema del cliente (D-05, RF-16); el QR queda como alternativa. La validación de derecho no cambia con el método de identificación.

## D-22 Estado de la sincronización para el aviso de RNF-05

Se agrega `GET /api/sincronizacion/estado` (roles Supervisor y Administrador), que devuelve `{ ultimaFallida, ultimaEjecucion, ultimaExitosa }`. Reportes y Administración lo consultan para mostrar "La última sincronización con RR. HH. falló; se usan los datos del {fecha}", donde {fecha} es la de la última sincronización exitosa (formato dd/MM/aaaa).

- **Motivo:** el SRS (§8 y RNF-05) pide mostrar el aviso al supervisor y al administrador, pero la bitácora solo se leía con `GET /api/admin/sincronizacion`, que exige rol Administrador y no da la fecha de la última sincronización exitosa. Confirmado por el dueño del producto (Tarea 14).
- **Detalles:** si nunca hubo una sincronización exitosa, la fecha se muestra como "—". Si la consulta del estado falla, no se muestra aviso, para no impedir el uso de la pantalla.

## D-23 Detalles del frontend de Reportes y Administración

Decisiones de implementación de la Tarea 14 donde el plan no dice nada:

- **Nombres de archivo:** las descargas se guardan como `<reporte>-<desde>-<hasta>.<ext>` (por ejemplo `cotejo-2026-11-01-2026-11-15.xlsx`), como fija la prueba del plan; la API sugiere `<reporte>-<desde>_<hasta>` (D-18), pero el navegador usa el nombre que da el frontend.
- **Reportes:** una sola pantalla con pestañas Cotejo de factura, Conteo y Detalle; Consultar y exportar se desactivan sin rango completo. El cotejo muestra los indicadores de servidas y de autorizadas sin servir; el indicador de rechazos de la guía de diseño queda fuera porque el cotejo no los incluye. El detalle se exporta solo a Excel (RF-23).
- **Errores:** se muestra el texto que envía la API (`{ error }` o `title` en un 400); cualquier otro error muestra "Ocurrió un error; intenta de nuevo." (SRS §8).
- **Administración:** pestañas con rutas `/admin/reglas`, `/admin/puntos`, `/admin/usuarios`, `/admin/sincronizacion` y `/admin/contingencia`. Eliminar una regla pide confirmación ("¿Eliminar la regla <turno> · <tiempo de comida>?"). La contraseña de un usuario solo se envía si se escribe. La contingencia confirma con "Registrado: <matrícula> · <fecha dd/MM/aaaa> · <tiempo de comida>", con los datos que devuelve la API (matrícula normalizada). Guardar y Eliminar se desactivan mientras la API responde (diseño §5). Al cambiar el rango o los filtros de un reporte se ocultan los resultados anteriores, para que lo visible coincida con lo que se exporta. La barra superior muestra usuario y rol (diseño §3); el usuario se guarda al iniciar sesión.
- **Claves al editar:** al editar un punto o un usuario, su clave o nombre de usuario queda de solo lectura. **Motivo:** la API guarda por clave, así que cambiarla crearía otro registro y dejaría activo el anterior (en usuarios, una cuenta vieja con acceso). Para "renombrar" se da de alta el nuevo y se desactiva el anterior.
- **Sincronización:** el botón muestra "Sincronizando…" mientras trabaja; el resultado dice "Sincronización correcta." o "La sincronización falló: <error>", seguido de altas, cambios, bajas y omitidos; la bitácora marca cada fila como "Correcta" o "Falló". Después de sincronizar, el aviso de RNF-05 se actualiza sin recargar la página.
- **QR impreso (D-21):** la vista imprimible muestra "Comedor de personal", el nombre y la clave del punto y el QR; al imprimir se ocultan la barra, la navegación y el resto de la página.

## D-24 Entorno de desarrollo y pruebas

Decisiones de las Tareas 1 a 8 sobre herramientas, donde el plan no dice nada.

- **.NET 8 aunque la máquina solo tenga .NET 10:** todos los proyectos apuntan a `net8.0` (SRS §2.4). Si solo está instalado .NET 10, las pruebas se corren con la variable de entorno `DOTNET_ROLL_FORWARD=Major`, que no se guarda en el repositorio.
- **Ajuste solo de pruebas:** el servidor de pruebas de .NET 8 falla al escribir JSON cuando corre sobre .NET 10, así que `ComedorApiFactory` registra un filtro de arranque (`EscritorEstandarFilter`) que lo evita. Vive solo en el proyecto de pruebas y no afecta a la API. **Pendiente:** activarlo solo cuando el runtime sea mayor que 8, para que un CI con .NET 8 pruebe el flujo real.
- **Karma y Jasmine:** Angular CLI 22 genera Vitest por defecto; se usó Karma con Jasmine porque el plan y el SRS §9.1 los nombran.
- **Prueba de arquitectura:** revisa las dependencias de `Comedor.Application` a través de la clase marcador `EnsambladoAplicacion`.
- **Migraciones:** `dotnet-ef` va como herramienta local (`.config/dotnet-tools.json`), y una fábrica de diseño con una cadena localhost sin contraseña permite generar migraciones sin levantar la API.
- **Pruebas de integración:** comparten un contenedor de SQL Server (Testcontainers), y cada fábrica de la API usa una base de datos con nombre único.

## D-25 Detalles del dominio y la persistencia

Tareas 2 y 3.

- **Rechazo con Id:** `Rechazo` tiene un `Guid Id`, aunque la firma del plan no lo incluye, para poder guardarlo en la base de datos.
- **Resultado de la evaluación:** cuando se encontró una regla del turno del empleado, el resultado incluye la regla y la fecha de servicio también en los rechazos posteriores (día de descanso, ya consumió). Así el rechazo queda con su fecha de servicio y tiempo de comida (RF-18).
- **Enumeraciones como texto:** el canal y el motivo de rechazo se guardan como texto en la base de datos, para que las tablas de auditoría se lean sin traducir números.
- **Consumos y entregas duplicados:** la API traduce los errores de restricción única de SQL Server (2601 y 2627) en "Ya consumiste este tiempo de comida" o en "ya servido", y desliga el registro rechazado para que la operación pueda continuar.
- **Riesgos conocidos:** la entrega no tiene llave foránea hacia el consumo; la API valida que el consumo exista antes de servir. La protección de solo inserción vive en `SaveChanges`, así que SQL directo la evitaría; un control en la base de datos (permisos o disparadores) queda como mejora.

## D-26 Detalles de la validación de acceso

Tarea 5.

- **Orden de "¿ya consumió?":** el evaluador del dominio es síncrono y la consulta al repositorio es asíncrona. El caso de uso evalúa las otras cinco reglas y, si autoriza, consulta si ya consumió. Es equivalente porque esa es la última verificación, y la restricción única sigue siendo la garantía ante dos intentos simultáneos (RNF-03).
- **Captura manual:** no comprueba que el punto exista; la limita el rol de quien captura (Punto o Encargado).
- **Token del QR inválido:** el celular recibe "Escanea de nuevo el código de la entrada" sin buscar al empleado y sin registrar nada (RF-10).

## D-27 Sesión del empleado y bloqueo por intentos

Tareas 6 y 8b.

- **Bloqueo:** 5 fallos dentro de 15 minutos bloquean el ID 15 minutos, contados desde el quinto fallo. Mientras dura, ni la contraseña correcta deja entrar y no se consulta al autenticador. Un inicio de sesión correcto borra los fallos. El ID se normaliza (`Trim().ToUpperInvariant()`) antes de contar, así que otra capitalización no evita el bloqueo.
- **Autenticador caído:** si el autenticador de la empresa falla, la respuesta es "servicio no disponible" y no cuenta como fallo.
- **Respuesta de la API:** `POST /api/empleados/sesion` responde 401 con `{ "error": "CredencialesIncorrectas" | "Bloqueado" | "ServicioNoDisponible" }`, y el frontend lo traduce a los textos del plan (D-18). Un cuerpo vacío responde `CredencialesIncorrectas`, nunca 500.
- **Token:** JWT de rol `Empleado`, `sub` = matrícula y 30 días de vigencia; las horas salen de `TimeProvider` en UTC.
- **Estado en memoria:** el control de intentos es una sola instancia para toda la API. Se reinicia si la API se reinicia y no se comparte entre servidores, lo que basta con un solo servidor (D-02).
- **Riesgo conocido:** varias peticiones simultáneas del mismo ID pueden entrar antes de que se registre el quinto fallo, así que en una ráfaga se pueden probar más de 5 contraseñas.

## D-28 Reglas de la sincronización de personal

Tarea 7.

- **Catálogo de turnos:** el plan pide omitir los turnos "no presentes en el catálogo", pero no define el catálogo. Es configurable (`Sincronizacion:Turnos`) y por defecto contiene Matutino, Vespertino y Nocturno (SRS §2.6). Ver también D-17.
- **Filas omitidas:** se omiten las filas con matrícula vacía, con turno fuera del catálogo o con matrícula repetida en la misma carga (se conserva la primera). La bitácora guarda cuántas se omitieron.
- **Fila inválida de un empleado existente:** el empleado **no se da de baja** y queda como estaba. **Motivo:** el SRS §8 dice que el registro inválido "se omite", y el plan solo da de baja al empleado "ausente en la fuente".
- **Reactivación:** un empleado dado de baja que vuelve a aparecer en la fuente se reactiva y cuenta como cambio.
- **Bitácora en cada ejecución:** se registran la ejecución exitosa, la falla de la fuente y también la falla al guardar en la base de datos (RF-04).
- **Programación:** la sincronización corre a la hora `Sincronizacion:Hora` (02:00 por defecto) y se apaga con `Sincronizacion:Habilitada`, que las pruebas usan. El empleado no guarda la "fecha de última sincronización" del SRS §7, porque no está en la firma del plan.
- **Revisable:** si la fuente devuelve una lista vacía, todos los empleados activos se dan de baja con resultado exitoso. Conviene confirmar con informática si una lista vacía debe tratarse como falla.

## D-29 API base, seguridad y configuración

Tarea 8a.

- **Configuración secreta:** `Jwt:Clave`, `Qr:ClaveFirma`, `ConnectionStrings:Comedor` y `Semilla:Admin` no van en `appsettings.json`; en desarrollo se cargan con `dotnet user-secrets` (README). Si falta alguna de las tres primeras, la API no arranca y dice cuál falta. El administrador semilla se crea solo si hay usuario y contraseña configurados y no existe ningún usuario.
- **Autenticador simulado:** sus usuarios de prueba, con contraseñas ficticias, están en `appsettings.Development.json` como datos del simulador; no son credenciales reales.
- **Usuarios del sistema:** las contraseñas se guardan con `PasswordHasher<UsuarioSistema>`. Un usuario inexistente, uno inactivo y una contraseña incorrecta reciben la misma respuesta. Para que el tiempo de respuesta tampoco delate qué usuarios existen, cuando el usuario no es válido se verifica igual contra un hash ficticio. **Pendiente:** ese hash debe actualizarse si cambian las iteraciones de `PasswordHasher`.
- **JWT:** HS256, sin emisor ni audiencia y con 30 segundos de tolerancia de reloj. La API conserva el `sub` del token sin renombrarlo y lee el rol del claim estándar de rol.
- **Errores inesperados:** se registran con el endpoint y el detalle, y el usuario recibe una respuesta genérica (SRS §8).
- **HTTPS:** la API todavía no fuerza HTTPS; depende del certificado pendiente con informática (SRS §11, RNF-06).

## D-30 Pantalla del chef y endpoints de acceso

Tarea 8b.

- **"Por servir" lo decide el servidor:** la lista de solicitudes omite los autorizados sin servir cuya fecha de servicio y tiempo de comida ya no son los de un horario en curso; los servidos y los rechazos se devuelven siempre. Sin `desde`, la lista cubre las últimas 2 horas. Así un autorizado sale de la lista al terminar su horario (SRS §8).
- **Enumeraciones en JSON como texto:** por ejemplo, `estado` vale `"Autorizado"`, `"Rechazado"` o `"EscanearDeNuevo"`.
- **Doble clic en Servido:** la segunda respuesta es 200 con `yaServido = true` y la hora del momento, no la de la entrega original, porque la API no la consulta.
- **URL del QR:** el token va escapado en la URL; un punto inexistente o inactivo responde 404.
- **Quién captura:** la matrícula del celular sale siempre del `sub` del token, y el usuario de la sesión queda como encargado de la captura manual y de la entrega. Un usuario de rol Punto no queda limitado a su propio punto; con un solo punto (D-12) no hace falta.

## D-31 Entorno de la prueba de carga y de la aceptación

Tarea 15. Estado: aplicada. Resultados en [aceptacion.md](aceptacion.md).

- **Entorno fuera del repositorio:** la carga y la aceptación corren contra la API publicada en Release con `ASPNETCORE_ENVIRONMENT=Carga` o `Aceptacion`. Su `appsettings.<entorno>.json` se genera junto a la API publicada y no se versiona, porque lleva claves de prueba y 1,000 contraseñas simuladas (regla "nada de secretos en el repositorio"). Cada corrida usa una base dedicada que se crea vacía (`ComedorCarga`, `ComedorAceptacion`).
- **Preparación dentro de k6:** `setup()` crea el punto, una regla de 00:00 a 23:59 y un usuario Encargado, sincroniza al personal e inicia sesión con los 1,000 empleados antes de medir. Así la prueba no depende de la hora en que corre y mide "con sesión ya iniciada" (RNF-01). Por eso exige una base dedicada.
- **Mezcla de validaciones:** el plan no la fija. Una de cada diez validaciones es captura manual, para medir `{name:manual}`. La fase sostenida usa 1,000 empleados distintos (1,000 consumos, RNF-02). En la fase pico vuelven los primeros 300 empleados y se espera que todos sean rechazados por "ya consumió". Si se quisiera un pico con consumos nuevos, harían falta 1,300 empleados simulados.
- **Alcance de la medición:** API, SQL Server y k6 en el mismo equipo. Lo que agregan la red y el celular se confirma en el entorno del cliente (pasos en aceptacion.md).
- **Runtime:** el equipo de prueba solo tiene .NET 10, así que la API corrió con `DOTNET_ROLL_FORWARD=Major`. Si el servidor del cliente usa .NET 8, la carga se repite ahí.
