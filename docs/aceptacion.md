# Prueba de carga y aceptación

Resultado de la Tarea 15 del [plan](plan.md): prueba de carga (RNF-01 y RNF-02) y recorrido de los criterios de aceptación de la sección 9.2 del [SRS](srs.md). Decisiones de esta tarea en [D-31](decisiones.md#d-31-entorno-de-la-prueba-de-carga-y-de-la-aceptación).

## Resumen

| Parte | Resultado |
| --- | --- |
| Prueba de carga | **Pasa.** p95 celular 6.0 ms (límite 2000 ms), p95 manual 5.9 ms (límite 1000 ms), 0 % de fallos HTTP en cada fase (límite 1 %). Consumos en la base = autorizados (1,000). |
| Criterios 9.2 | **14 de 14 cumplidos en la API y la base del entorno de prueba** (30 verificaciones, 0 fallas). En los criterios 1, 2, 3 y 12 falta confirmar lo que se ve en el celular y en las pantallas. |
| Pruebas automatizadas | Backend 150/150 (Domain 27, Application 60, Api 63); frontend 57/57. |
| Pendiente | Confirmar en la red del comedor, con un celular real, los tiempos que percibe el empleado y la pantalla del chef (ver [Pendiente en el entorno del cliente](#pendiente-en-el-entorno-del-cliente)). |

## Entorno de prueba

- **Fecha:** 2026-11-05, entre 21:46 y 22:50 (hora de Hermosillo). Era jueves; algunos criterios dependen del día.
- **Equipo:** Windows 10 Pro, el mismo equipo para la API, SQL Server y k6, sin red de por medio.
- **Base de datos:** SQL Server 2025 (17.0) local. Se usaron dos bases dedicadas que se crearon vacías: `ComedorCarga` y `ComedorAceptacion`.
- **API:** publicada en Release (`dotnet publish -c Release`) y ejecutada con el runtime de .NET 10 (`DOTNET_ROLL_FORWARD=Major`), porque el equipo no tiene el de .NET 8.
- **Configuración:** archivos `appsettings.Carga.json` y `appsettings.Aceptacion.json` generados junto a la API publicada, fuera del repositorio. Contienen claves de prueba, la cadena de conexión, el administrador semilla, los usuarios del autenticador simulado y la ruta de la fuente de RR. HH. simulada. La sincronización programada quedó apagada.
- **Herramienta de carga:** k6 v2.3.0.

## Prueba de carga

### Escenario

Archivo: [`load/validacion.k6.js`](../load/validacion.k6.js).

- **Preparación (`setup`):** crea el punto `CARGA1`, una regla Matutino/Comida de 00:00 a 23:59 (para que la prueba no dependa de la hora) y un usuario Encargado; sincroniza al personal e inicia sesión con los 1,000 empleados. Los tiempos de esta preparación no cuentan en los umbrales.
- **Fase sostenida:** 50 validaciones por minuto durante 20 minutos, cada una de un empleado distinto (C0001 a C1000). Una de cada diez es captura manual del chef; las demás son por celular con el QR del punto.
- **Fase pico:** 150 validaciones por minuto durante 2 minutos. Vuelven los primeros 300 empleados, que ya comieron; se espera que todos sean rechazados con "Ya consumiste este tiempo de comida".
- **Umbrales:** `http_req_duration{name:celular}` p95 < 2000 ms, `{name:manual}` p95 < 1000 ms, y `http_req_failed` < 1 % en cada fase (`{scenario:sostenido}` y `{scenario:pico}`), de modo que las peticiones de la preparación no cuentan.
- **Comprobaciones:** cada respuesta es 200; la primera validación de cada empleado da `Autorizado`, y cualquier repetición da `Rechazado` con el motivo exacto "Ya consumiste este tiempo de comida". Esto incluye el pico y la iteración extra que `constant-arrival-rate` puede lanzar al final de una fase.

### Cómo repetirla

1. Crear una base vacía y una fuente de RR. HH. simulada con 1,000 empleados de turno Matutino, que trabajen todos los días y tengan contrato vigente, con matrículas C0001 a C1000.
2. Configurar el autenticador simulado con la contraseña `carga-<matrícula>` para cada empleado, y definir un administrador semilla.
3. Arrancar la API apuntando a esa base y correr:

   ```bash
   k6 run -e API=http://localhost:5080 -e ADMIN_USUARIO=<admin> -e ADMIN_CONTRASENA=<contraseña> load/validacion.k6.js
   ```

4. Al terminar, comparar `SELECT COUNT(*) FROM Consumos` con la métrica `autorizados` de k6.

Para un ensayo corto: `-e EMPLEADOS=40 -e MINUTOS_SOSTENIDO=1 -e MINUTOS_PICO=1`, contra una base nueva. Con menos de 50 empleados, todas las validaciones del pico son repeticiones y las comprobaciones se cumplen.

### Resultado

Corrida completa: 2026-11-05, de 22:25:26 a 22:47:28, sobre una base nueva. k6 terminó con código 0.

| Métrica | Valor | Umbral |
| --- | --- | --- |
| Celular, p95 | 6.0 ms (máx. 96.5 ms) | < 2000 ms ✓ |
| Manual, p95 | 5.9 ms (máx. 53.2 ms) | < 1000 ms ✓ |
| Fallos HTTP, fase sostenida | 0 de 1,001 (0.00 %) | < 1 % ✓ |
| Fallos HTTP, fase pico | 0 de 301 (0.00 %) | < 1 % ✓ |
| Validaciones | 1,302 (1,001 sostenidas y 301 en el pico) | — |
| Autorizados (k6) | 1,000 | — |
| Rechazados (k6) | 302 | — |

Conteo en la base después de la corrida (consultas SQL guardadas con la evidencia):

| Consulta | Resultado |
| --- | --- |
| `Consumos` | 1,000 (900 por celular, 100 por captura manual) |
| Matrículas distintas en `Consumos` | 1,000 |
| `Rechazos` | 302, todos con motivo `YaConsumio`, matrículas de C0001 a C0301 |

**El conteo de Consumo es igual al número de autorizados.**

**Una comprobación de 3,906 falló, y fue por un defecto del script, no del sistema.** `constant-arrival-rate` lanzó una iteración más de lo previsto en cada fase (1,001 y 301). La iteración 1,001 de la fase sostenida repitió a C0001, que ya había comido, y el sistema la rechazó correctamente con "Ya consumiste". La comprobación de ese momento esperaba `Autorizado` para toda la fase. Se corrigió para que la primera validación de cada empleado espere `Autorizado` y cualquier repetición espere el rechazo. La corrección se verificó con una corrida corta (40 empleados, 1 + 1 minutos): 40 autorizados, 161 rechazos con el motivo exacto y todas las comprobaciones cumplidas.

Una primera corrida completa (21:46 a 22:08), hecha antes de corregir los umbrales, dio resultados equivalentes: p95 de 3.1 ms y 2.6 ms, 0 fallos, 1,000 consumos y 300 rechazos.

**Alcance de la medición:** mide la API y SQL Server con 1,000 empleados y el ritmo de RNF-02. No incluye la red Wi-Fi, el navegador del celular ni la cámara. Esa parte se confirma en el entorno del cliente.

## Criterios de aceptación (SRS 9.2)

El recorrido se hizo contra la API de aceptación, con la fuente simulada de 20 empleados (`backend/src/Comedor.Api/datos/empleados-simulados.json`) y E9999 como empleado que no existe en la fuente. Se usaron llamadas HTTP con los mismos endpoints que el frontend y consultas directas a la base para confirmar qué se registró. La última columna lista las pruebas automatizadas que cubren el mismo criterio; todas pasaron en esta tarea.

| # | Criterio | Resultado | Evidencia del recorrido | Pruebas automatizadas |
| --- | --- | --- | --- | --- |
| 1 | Empleado vigente, en día laboral, turno y horario ve el pase Autorizado en menos de 2 s con sesión iniciada | ✓ API; celular pendiente | E1001 (Matutino, jueves laboral) por celular: `Autorizado` en 53 ms (tiempo de la API, sin cámara ni red). | `FlujoCompleto_SesionQrYAcceso_Autorizado`, `autorizado_muestraAutorizadoYNombre` |
| 2 | La solicitud aparece en la pantalla del chef en menos de 3 s, con nombre, turno y tiempo de comida, en "Por servir" | ✓ API; pantalla pendiente | `GET /api/puntos/P1/solicitudes` trae a E1001 con nombre "Ana Torres", turno Matutino, tiempo Cena y sin servir, en 77 ms. La pantalla consulta cada 2 s, así que el peor caso **estimado** es de unos 2.1 s; no se midió en la pantalla. | `Solicitudes_AutorizadoAparecePorServir_ConNombre`, `solicitudes_consultaCada2Segundos`, `tarjeta_muestraNombreSinFoto` |
| 3 | Al marcar Servido sale de la lista y queda una entrega; un segundo clic no crea otra | ✓ API; pantalla pendiente | Primer clic `yaServido=false`, segundo clic `yaServido=true`; en la base hay 1 entrega para ese consumo y la lista lo marca como servido. | `Servido_DosVecesSimultaneas_UnaEntrega`, `PrimeraVez_RegistraEntrega_YaServidoFalso`, `servido_quitaTarjetaDeLaLista` |
| 4 | QR alterado, firmado con otra clave o de un punto inactivo pide "Escanea de nuevo" y no registra rechazo | ✓ | Los tres casos devuelven `EscanearDeNuevo` con "Escanea de nuevo el código de la entrada". Había 0 rechazos antes y 0 después. | `TokenAlterado_EsInvalido`, `TokenFirmadoConOtraClave_EsInvalido`, `TokenDePuntoInactivo_EsInvalido`, `Celular_TokenInvalido_EscanearDeNuevo_SinRegistros` |
| 5 | Cada uno de los seis motivos de rechazo aparece en el pase cuando corresponde | ✓ | Por celular: E9999 "No registrado"; E1007 "Contrato vencido"; E1010 (descansa los jueves) "Hoy es tu día de descanso"; E1002 (Vespertino, sin regla a esa hora) "Tu turno no tiene derecho a este tiempo de comida"; E1001 por segunda vez "Ya consumiste este tiempo de comida"; E1004 sin ninguna regla vigente "Fuera de horario". | `Rechaza_*`, `Textos_DeMotivo`, `rechazado_muestraMotivo`, `rechazadoNoRegistrado_conNulos_muestraMotivoSinEtiquetasVacias` |
| 6 | Un segundo intento en el mismo tiempo de comida, por cualquier canal, es rechazado | ✓ | E1004: celular `Autorizado`, después manual "Ya consumiste…". E1011: manual `Autorizado`, después celular "Ya consumiste…". | `ConsumoDuplicado_DevuelveYaConsumio_YRegistraRechazo`, `Rechaza_YaConsumio` |
| 7 | Dos validaciones simultáneas del mismo empleado producen un solo consumo | ✓ | E1013: celular y manual enviados a la vez. Uno quedó `Autorizado` y el otro "Ya consumiste…"; en la base hay 1 consumo. | `Concurrencia_CelularYManualSimultaneos_UnSoloConsumo`, `RegistrarConsumo_DosVecesMismaClave_LanzaConsumoDuplicado` |
| 8 | El turno nocturno desayuna al salir y el consumo queda en la jornada trabajada, aunque al día siguiente descanse | ✓ | E1009 (Nocturno, trabaja de sábado a miércoles) validó el jueves 2026-11-05 a las 22:09: `Autorizado` y `FechaServicio` = 2026-11-04 en la base. Para poder probarlo a esa hora, la regla Nocturno/Desayuno de jornada anterior se abrió de 00:00 a 23:59; el horario real de salida solo lo cubren las pruebas de dominio citadas. | `Nocturno_DesayunoTrasUltimaJornada_DiaSiguienteDescanso`, `Nocturno_DesayunoEsDeLaJornadaAnterior` |
| 9 | La captura manual aplica las mismas reglas que el celular | ✓ | Los mismos seis motivos del criterio 5, por captura manual, dan los mismos textos. E1020 por captura manual: `Autorizado`. | `Manual_Autorizado_RegistraConsumoCanalManualConEncargado`, `Manual_MatriculaConEspaciosYMinusculas_SeNormaliza` |
| 10 | Ningún rol puede editar ni borrar consumos, entregas o rechazos | ✓ | La evidencia principal es el código: ningún controlador declara rutas PUT, PATCH ni DELETE sobre consumos, entregas o rechazos (las únicas rutas de edición son de reglas, puntos y usuarios, en `AdminController`). En el recorrido, 45 intentos de PUT, PATCH y DELETE sobre esas rutas con Administrador, Supervisor y Encargado respondieron todos con un error (código 400 o mayor) y no cambiaron nada. | `ModificarConsumo_LanzaInvalidOperation`, `BorrarEntrega_LanzaInvalidOperation` |
| 11 | Un empleado con contrato vencido en la fuente pierde el derecho tras la siguiente sincronización | ✓ | Se cambió `finContrato` de E1016 al 2026-11-04 en la fuente. La sincronización respondió `exito=true, cambios=1`; `FinContrato` en la base pasó de 2026-12-31 a 2026-11-04; el pase devolvió "Contrato vencido". | `Rechaza_ContratoVencido` (el rechazo). Ninguna prueba automatizada cubre que la sincronización actualice `FinContrato`; esa parte la respalda solo este recorrido. |
| 12 | Con la fuente RR. HH. caída, el sistema sigue validando y muestra el aviso | ✓ API; pantalla pendiente | Sin el archivo de la fuente, la sincronización devolvió `exito=false` y `GET /api/sincronizacion/estado` (Supervisor) devolvió `ultimaFallida=true` con la fecha de la última exitosa. E1017 validó `Autorizado` con la copia local. | `FuenteCaida_NoModificaDatos_YRegistraError`, `Estado_Supervisor_VeUltimaFallidaYFechaDeLaUltimaExitosa`, `ultimaSincronizacionFallida_muestraAviso`, `sincronizar_actualizaAviso` |
| 13 | El cotejo cuenta solo servidas, separa las autorizadas sin servir y exporta a Excel y PDF | ✓ | Cotejo del 2026-11-04 al 2026-11-05: Desayuno 0 servidas / 1 sin servir; Cena 1 servida / 5 sin servir. Coincide con la base (1 consumo con entrega, 6 sin entrega). Los rechazos no se cuentan. Excel: 200, firma `PK`, 6,547 bytes. PDF: 200, firma `%PDF`, 22,815 bytes. | `Cotejo_ExcluyeRechazos`, `Cotejo_ContingenciaCuentaComoServida`, `CotejoExcel_ReleidoConClosedXML_TotalServidasEs4`, `CotejoPdf_EmpiezaConFirmaPdf` |
| 14 | La prueba de carga cumple RNF-01 y RNF-02 | ✓ | Ver [Prueba de carga](#prueba-de-carga). | — |

## Pendiente en el entorno del cliente

Lo siguiente no se puede verificar en un equipo de desarrollo. Hay que confirmarlo cuando informática instale el sistema en el servidor y la red del comedor (RNF-06 y RNF-07 también se resuelven en esa instalación, ver el plan, "Fuera de las tareas de código").

1. **Criterio 1 con celular real:** un empleado con sesión iniciada escanea el QR impreso con la cámara del celular, conectado a la red del comedor. Se mide con cronómetro desde que la cámara lee el código hasta que se ve el pase Autorizado: debe ser menos de 2 s. Repetir con al menos dos modelos de celular (Android y iPhone).
2. **Criterio 2 en la pantalla del chef:** con la pantalla del punto abierta en el equipo de la entrada, medir desde que el empleado ve su pase hasta que aparece la tarjeta en "Por servir": debe ser menos de 3 s.
3. **Criterio 3 en pantalla:** al pulsar Servido, la tarjeta desaparece de "Por servir".
4. **Criterio 12 en pantalla:** con la fuente real de RR. HH. desconectada, una sincronización manual hace aparecer el aviso en Reportes y en Administración.
5. **Carga por la red:** repetir `load/validacion.k6.js` desde un equipo distinto al servidor, conectado a la misma red que usarán los celulares. La API debe apuntar a **una base dedicada en el servidor del cliente**, con la fuente simulada de 1,000 empleados. **Nunca contra la base de producción:** `setup()` deja un usuario Encargado de prueba, una regla que da derecho todo el día y el punto `CARGA1`, y los 1,000 consumos y 300 rechazos no se pueden borrar porque esas tablas son de solo inserción.

## Observaciones

Lo siguiente se vio durante el recorrido. No impide aceptar el sistema y no se cambió nada; se deja para que lo revise el dueño del producto.

- Cuando la fuente RR. HH. falla, la respuesta de "Sincronizar ahora" y la bitácora guardan el mensaje técnico de la excepción, en inglés y con la ruta del archivo en el servidor (por ejemplo, "Could not find file '…\empleados.json'"). Solo lo ven el Administrador y el Supervisor.

## Instalación v2

**Estado: PENDIENTE.** Tarea 10 del plan v2. Los archivos del instalador (`instalador/comedor.iss`, `publicar.ps1`, `configurar.ps1`, `respaldar.ps1`, `verificar.ps1`) y las guías están escritos, pero **no se compiló `InstalarComedor.exe` ni se ejecutó nada en una máquina**: no hay Inno Setup en el equipo de desarrollo y no se instalan servicios ni reglas de firewall allí. Solo se comprobó que los `.ps1` no tienen errores de sintaxis (`[System.Management.Automation.Language.Parser]::ParseFile`: 0 errores en los cuatro). La primera compilación real se hizo el 6 de noviembre de 2026 con Inno Setup 7.1.1: falló en una línea que empezaba con `#13#10` (el preprocesador la tomaba por una directiva; también habría fallado en Inno Setup 6), se corrigió y `publicar.ps1` genera `salidaInstalarComedor.exe` (unos 47 MB) sin advertencias. **Sigue sin ejecutarse el instalador**: nada de esta lista se ha comprobado en una máquina.

Hay que hacerlo en una máquina virtual con Windows 10 u 11 limpia, sin .NET ni SQL Server, con `publicar.ps1` ya ejecutado en la PC de desarrollo. Marcar cada fila con el resultado real.

| N.º | Comprobación | Resultado esperado | Hecho |
| --- | --- | --- | --- |
| 1 | Con la VM limpia, correr `verificar.ps1` | Todo en rojo, sin errores del script, código de salida distinto de 0 | [ ] |
| 2 | `publicar.ps1` en la PC de desarrollo | Genera `salida\InstalarComedor.exe` sin errores; si falta `iscc`, avisa con un mensaje claro | [ ] |
| 3 | Confirmar la descarga y la instalación de SQL Server Express (paquete en inglés, D-55) en un Windows con idioma español (México); el primer intento falló por el idioma del paquete | Se descarga el instalador web de Microsoft y la instalación en silencio termina; la instancia `SQLEXPRESS` existe | [ ] |
| 4 | Instalar con `InstalarComedor.exe` (administrador `admin1`, contraseña de 8 o más caracteres, puerto 80) | El asistente rechaza contraseñas de menos de 8 caracteres; instala en `C:\Program Files\Comedor`; crea los dos accesos directos | [ ] |
| 5 | Con el puerto 80 ocupado (por ejemplo, otro programa escuchando) abrir el asistente | Avisa y propone 8080; los accesos directos y el QR llevan `:8080` | [ ] |
| 6 | `verificar.ps1` tras instalar | Todo en verde: servicio, `/api/salud` 200, regla de firewall, tarea de respaldo, IP del QR | [ ] |
| 7 | `appsettings.Local.json` | Existe en `C:\ProgramData\Comedor`; solo SYSTEM y Administradores lo leen (`icacls`); claves de 48 caracteres; `Fuente:Tipo` = `Excel`; la dirección no lleva `:puerto` con el 80 | [ ] |
| 8 | Reiniciar Windows y volver a correr `verificar.ps1` | Todo en verde sin abrir ningún programa; el servicio arrancó solo | [ ] |
| 9 | Matar el proceso `Comedor.Api.exe` | El servicio se reinicia solo en unos segundos | [ ] |
| 9a | Reiniciar Windows con SQL Server arrancando lento (o detener `MSSQL$SQLEXPRESS` y reiniciar solo `Comedor`) | El servicio `Comedor` espera a SQL Server (dependencia) y queda en ejecución sin intervención; el inicio es automático retrasado | [ ] |
| 9b | Instalación en PC limpia con primera migración lenta (más de 30 s) | El asistente espera hasta 3 minutos; si no arranca, avisa y remite a `verificar.ps1` sin fallar | [ ] |
| 9c | Interrumpir `configurar.ps1` tras crear el archivo (simular error de `icacls`) y volver a instalar | No queda `appsettings.Local.json` vacío; el asistente vuelve a pedir usuario, contraseña y puerto | [ ] |
| 9d | `verificar.ps1` desde PowerShell sin elevar | Dice "Abre PowerShell como administrador y vuelve a ejecutar" y sale con código 1 | [ ] |
| 10 | Panel del comedor: iniciar sesión con el administrador, crear un punto, **Imprimir QR** | Se imprime el QR con la dirección de la PC | [ ] |
| 11 | Pantalla del chef (acceso directo) | Abre a pantalla completa en Edge y llega a la pantalla del chef o a su inicio de sesión | [ ] |
| 12 | Importar el Excel de personal desde **Administración > Personal** | Altas correctas; la sincronización de las 02:00 no desactiva al personal importado | [ ] |
| 13 | Desde un celular en la misma red: escanear el QR, iniciar sesión y ver la fila en la TV. **En una instalación nueva ningún empleado puede iniciar sesión** (el autenticador de la empresa es simulado y producción no trae usuarios simulados; pendiente D-40): agregar antes en `appsettings.Local.json` un usuario de prueba en `Autenticacion:Simulada:Usuarios` (ver `docs/instalacion.md`, "Probar con usuarios simulados"), con la matrícula de alguien del Excel de personal, y reiniciar el servicio | Sin el usuario simulado, el inicio de sesión del empleado se rechaza. Con él: pase Autorizado y la persona aparece en la fila de la pantalla del chef | [ ] |
| 14 | Desde una red distinta o con datos móviles, abrir la dirección | No abre (el firewall solo permite la red local) | [ ] |
| 15 | Correr `respaldar.ps1` (como Administrador y como SYSTEM, por la tarea programada) | Aparece `Comedor-<aaaaMMdd-HHmm>.bak` en `C:\ProgramData\Comedor\Respaldos` y una línea en `respaldos.log` | [ ] |
| 16 | Tarea "Comedor - Respaldo" en el Programador de tareas, ejecutar manualmente | Termina con resultado 0x0 y crea otro `.bak` | [ ] |
| 17 | Borrado a los 30 días: poner a un `.bak` de prueba una fecha de modificación de hace 31 días y correr `respaldar.ps1` | Se borra solo ese archivo | [ ] |
| 18 | Reinstalar encima con el mismo `.exe` (actualización) | No pide usuario, contraseña ni puerto; `appsettings.Local.json` queda intacto (misma fecha y claves); los datos y el QR impreso siguen funcionando; `verificar.ps1` en verde | [ ] |
| 19 | Cambiar la IP de la PC y correr `verificar.ps1` | Muestra "La IP cambió: reimprime el QR desde Panel > Puntos", la IP nueva, la IP con la que se hizo el QR actual y las dos opciones (`-ActualizarIp` o reservar la IP anterior); código de salida distinto de 0 | [ ] |
| 20 | Desinstalar | Se quitan el servicio, la regla de firewall, la tarea de respaldo y los accesos directos; la base y `C:\ProgramData\Comedor` (con los respaldos) se conservan | [ ] |
| 21 | Reinstalar tras desinstalar | Reutiliza la configuración y la base existentes; los datos siguen | [ ] |
| 22 | Instalar con la instancia `SQLEXPRESS` ya presente (primero sin dar permisos a `NT AUTHORITY\SYSTEM`, para ver el fallo, y luego con el rol `sysadmin` o con la base `Comedor` creada y `SYSTEM` como `db_owner`) | No descarga ni instala SQL Server otra vez. Sin el permiso, el servicio se reinicia en bucle (se documenta en `docs/instalacion.md`). Con el permiso: el servicio `Comedor` queda en ejecución y `/api/salud` responde 200 | [ ] |
| 23 | Instalar sin internet y sin SQL Express | El asistente explica que no pudo descargar y no instala nada a medias | [ ] |
| 24 | Reiniciar la PC e iniciar sesión en Windows con el modo quiosco de Edge | La Pantalla del chef abre directamente en la fila, sin pedir inicio de sesión. Si falla (por ejemplo, el modo quiosco usa InPrivate y borra el token de sesión de 10 años), documentar otra forma de abrir la pantalla | [ ] |
| 25 | Tras un cambio de IP, `configurar.ps1 -ActualizarIp` y un QR reimpreso funcionan | `-ActualizarIp` cambia solo `App:UrlPublica` (claves y semilla intactas, permisos restringidos) y reinicia el servicio; `verificar.ps1` pasa a verde; el QR reimpreso desde Panel > Puntos abre desde un celular. Antes, `verificar.ps1` muestra la IP del QR actual y las dos opciones (D-53) | [ ] |
