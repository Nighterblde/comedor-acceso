# Especificación de Requisitos de Software (SRS)

## Sistema de control de acceso y consumo del comedor de personal

| Campo | Valor |
| --- | --- |
| Fecha | 6 de noviembre de 2026 |
| Estado | v1 implementada; v2 aprobada para planeación |
| Alcance | Subproyecto 1 de la plataforma del comedor |
| Documentos asociados | [Plan v1](plan.md) · [Plan v2](plan-v2.md) · [Guía de diseño](diseno.md) · [Decisiones](decisiones.md) · [Carga y aceptación](aceptacion.md) |
| Fuente editable | Documento colaborativo en Claude (este archivo es una exportación) |

**Versión 2.1.** Las decisiones D-NN se detallan en el [registro de decisiones](decisiones.md).

| Versión | Cambios                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
|---------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1.0     | Versión inicial aprobada para planeación.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| 1.1     | Un solo punto de entrada, donde el chef sirve la comida. Referencia a la guía de diseño.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| 1.2     | RF-23 precisa qué reportes se exportan y en qué formato (D-14). RF-26 precisa las reglas de la captura de contingencia (D-15).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| 1.3     | El QR del punto es permanente e impreso y no comprueba presencia; se elimina la caducidad del token (RF-07, RF-10, D-21). La pantalla del chef muestra el nombre sin foto; la foto queda como mejora futura (RF-29, D-20).                                                                                                                                                                                                                                                                                                                                                                                                                          |
| 1.4     | Riesgos aceptados D-16 y D-21 en 2.7. Turno y tiempo de comida como texto libre (D-17). Mejoras futuras D-19 y D-20 en 10. Se corrigen dos menciones de foto en la pantalla del chef (2.2 y 9.2).                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| 1.5     | La matrícula puede llevar letras y números; el celular abre el teclado de texto (D-32). Se agrega el formato de la matrícula a los pendientes con el cliente (11).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| 1.6     | RNF-05: el aviso de sincronización fallida muestra la fecha de la última sincronización exitosa (D-22). RF-25: la clave de un punto y el nombre de un usuario no se editan (D-23). §7: el empleado no guarda la fecha de última sincronización (D-28). §8: reglas de filas inválidas en la sincronización (D-28). §2.7: riesgos del control de intentos en memoria (D-27) y de la protección de solo inserción en la aplicación (D-25). §11: lista vacía de RR. HH. (D-28) y HTTPS sin forzar (D-29). §9.2: resultado de la prueba de carga y de la aceptación (D-31).                                                                              |
| 2.0     | Operación en la empresa (plan v2): tres roles (D-33); sesión del chef sin caducidad y revocable (D-34); fila de servicio en la TV y turno en el celular (RF-29, RF-30, RF-33, D-35); sin captura manual en la TV (RF-15, D-36); panel único (RF-34, D-37); panel y chef solo desde la red de la empresa (RF-37, D-38); instalador .exe y respaldos (RF-35, RNF-07, RNF-11, D-39); fuente de RR. HH. en SQL Server o Excel (RF-36, D-42); contraseña de la empresa bloqueante (D-40); acceso desde internet futuro (D-41); dirección e identidad profesionales (RF-38, D-43); sesión del administrador de 2 horas y fila en su panel (RF-24, RF-34). |
| 2.1     | RF-09: el empleado puede cerrar sesión desde el pase y entrar con otra cuenta sin volver a escanear (D-54). |

## 1. Introducción

### 1.1 Propósito

Este documento especifica los requisitos del sistema de control de acceso y consumo del comedor de personal. Es la base acordada para el diseño, la implementación y las pruebas, y sirve como plan del proyecto.

### 1.2 Alcance

El sistema identifica a cada empleado al llegar al comedor, valida si tiene derecho al tiempo de comida en curso según su turno, impide consumos repetidos y conserva un registro inalterable. Ese registro es la base para validar la factura del proveedor del comedor y para el control interno.

Este es el **subproyecto 1** de una plataforma mayor. Menús, inventarios, compras, costos, inocuidad y personal de cocina quedan fuera (sección 10).

### 1.3 Definiciones

| Término          | Significado                                                                                                                      |
|------------------|----------------------------------------------------------------------------------------------------------------------------------|
| Empleado         | Persona del personal con matrícula en la base de RR. HH. del cliente.                                                            |
| Turno            | Jornada fija asignada al empleado durante la vigencia de su contrato (por ejemplo, matutino).                                    |
| Tiempo de comida | Servicio con horario definido: desayuno, comida, cena o colación.                                                                |
| Regla de derecho | Relación configurable turno → tiempos de comida → horario.                                                                       |
| Consumo          | Registro de que un empleado recibió un tiempo de comida.                                                                         |
| Rechazo          | Intento de acceso denegado, con su motivo.                                                                                       |
| Punto de entrada | Entrada del comedor: un QR impreso permanente y la pantalla del chef (una TV) con la fila de servicio.                           |
| Chef             | Chef: ve en la TV a quién servir y presiona cualquier tecla para marcarlo como servido. Desde la v2 el rol se llama Chef (D-33). |
| Fuente RR. HH.   | Base de datos existente del cliente con empleados, turnos y contratos.                                                           |

## 2. Descripción general

### 2.1 Perspectiva del producto

Es un sistema nuevo e independiente que corre en un servidor dentro de la red del cliente. Lee la fuente de RR. HH. en modo solo lectura; no la modifica.

### 2.2 Usuarios del sistema

| Rol           | Qué hace                                                                                                                                                                                                                                                                                                |
|---------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Empleado      | Escanea con la cámara de su celular el QR impreso de la entrada e inicia sesión con su ID y contraseña de la empresa. Si tiene derecho, entra a la fila y su celular le dice su lugar y cuándo le toca.                                                                                                 |
| Chef          | Mira la TV de la entrada, que muestra en grande a quién servir. Entrega la comida y presiona cualquier tecla (o toca la pantalla) para marcarlo como servido. No aprueba, no rechaza y no captura nada: el sistema decide. Su sesión no caduca.                                                         |
| Administrador | Usa el panel único, con todas las funciones: la fila del comedor (puede marcar Servido como el chef), reportes y cotejo de factura, reglas de derecho, puntos e impresión del QR, usuarios, importación de personal, sincronización y contingencia. Sesión de 2 horas. Solo desde la red de la empresa. |

### 2.3 Escala de operación

Un comedor con un solo punto de entrada, donde el chef sirve la comida, y de 300 a 1,000 comidas por servicio. El modelo de datos admite agregar puntos en el futuro sin cambiar el diseño.

### 2.4 Restricciones

- Frontend en Angular (versión actual, con NgModules), organizado en modelo, vista y controlador (sección 5); la vista del empleado es adaptada a celular y la del chef a una TV horizontal.

- Backend en C# con ASP.NET Core sobre .NET 8 LTS y arquitectura por capas (sección 5).

- Base de datos SQL Server Express, separada de la base de RR. HH., que solo se lee (D-39).

- Instalación on-premise en una PC con Windows de la empresa, con un instalador .exe que la deja como servicio que arranca solo (D-39). La empresa no tiene personal técnico.

- Entrada en autoservicio desde el celular; el chef sirve según la fila que muestra la TV.

### 2.5 Supuestos

- El turno de cada empleado es fijo durante la vigencia de su contrato.

- Cada empleado tiene derecho a una comida por cada tiempo que le corresponde según su turno.

- El turno nocturno puede tener tiempos de comida a la mañana siguiente (por ejemplo, desayuno al salir) y horarios que crucen la medianoche; el consumo se asigna a la **fecha de servicio**, el día en que inicia ese horario.

- La comida es una prestación: no hay cobro ni caja.

- Los empleados inician sesión con el ID y la contraseña de la empresa; el sistema que los valida está por definir.

- Todos los empleados tienen celular con cámara y navegador (D-36); la huella será la alternativa.

### 2.6 Dependencias

- Acceso de lectura a la fuente RR. HH.: una vista de SQL Server con columnas fijas o, mientras tanto, un archivo Excel que el administrador importa desde el panel (D-42). En desarrollo, adaptador simulado.

- **Bloqueante para producción:** el sistema de la empresa que valida el ID y la contraseña del empleado (D-40). Hasta conocerlo, el autenticador es simulado.

- Una PC con Windows 10, 11 o Windows Server en la red de la empresa, encendida durante los servicios, con su IP reservada en el router (D-39).

- Una TV o pantalla en la entrada conectada a esa PC, con teclado, control o pantalla táctil, para la fila de servicio.

- Wi-Fi en el comedor accesible desde celulares personales y con ruta a la PC del comedor. El acceso desde datos móviles es futuro (D-41).

- Un cartel impreso con el QR del punto, en la entrada.

### 2.7 Riesgos aceptados

- **Uso de la cuenta de otro empleado:** si alguien inicia sesión con credenciales ajenas, la comida se descuenta al titular y el total no aumenta; la factura sigue correspondiendo a comidas servidas. El chef detecta solo casos evidentes.

- **QR sin comprobación de presencia (D-21):** alguien con una foto del cartel o con el enlace puede pedir su comida desde fuera del comedor. Cada empleado sigue limitado a un consumo por tiempo de comida, y una comida no recogida queda sin servir y no se paga (D-09), salvo el caso del punto siguiente. Para invalidar un QR filtrado se desactiva el punto o se cambia la clave de firma y se reimprime.

- **Quien no se presenta y está primero en la fila (D-35):** la TV lo sigue mostrando hasta que el chef presiona una tecla, y esa comida se cuenta como servida.

- **Sin protección del último administrador (D-16):** un administrador puede desactivar a cualquier usuario, incluido el último administrador activo. Si ocurre, la recuperación es una corrección directa en la base de datos.

- **Bloqueo por intentos en memoria (D-27):** el conteo de intentos fallidos se reinicia si la API se reinicia, y en una ráfaga de peticiones simultáneas se pueden probar más de 5 contraseñas antes del bloqueo.

- **Solo inserción protegida por la aplicación (D-25):** la API impide editar o borrar consumos, rechazos y entregas, pero quien tenga acceso directo a la base de datos podría hacerlo. Un control en la base de datos (permisos o disparadores) queda como mejora.

- **HTTP en la red local (D-39):** hasta que exista HTTPS (D-41), la contraseña del empleado viaja sin cifrar dentro de la red de la empresa.

- **Cambio de IP de la PC (D-39):** el QR impreso lleva la IP; si cambia, deja de funcionar hasta reimprimirlo. Se mitiga reservando la IP en el router; el dominio propio (D-41) lo elimina.

## 3. Requisitos funcionales

Prioridad: **Alta** = indispensable para la v1; **Media** = deseable en la v1.

| ID                             | Requisito                                                                                                                                                                                                                                                                                                                                                                                                                                                       | Prioridad |
|--------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------|
| **Sincronización de personal** |                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |           |
| RF-01                          | El sistema importa de la fuente RR. HH. matrícula, nombre, foto (si existe), área, turno, rol de días laborales y descansos, e inicio y fin de contrato.                                                                                                                                                                                                                                                                                                        | Alta      |
| RF-02                          | La sincronización corre automáticamente cada noche y puede lanzarse bajo demanda por un administrador.                                                                                                                                                                                                                                                                                                                                                          | Alta      |
| RF-03                          | El sistema nunca escribe en la fuente RR. HH.                                                                                                                                                                                                                                                                                                                                                                                                                   | Alta      |
| RF-04                          | Cada sincronización queda registrada: fecha, resultado y empleados agregados, modificados y dados de baja.                                                                                                                                                                                                                                                                                                                                                      | Media     |
| **Reglas de derecho**          |                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |           |
| RF-05                          | El administrador configura qué tiempos de comida corresponden a cada turno y su horario (inicio y fin). Un horario puede cruzar la medianoche (fin menor que inicio).                                                                                                                                                                                                                                                                                           | Alta      |
| RF-06                          | La fecha de servicio es el día en que inició la jornada a la que pertenece la comida: el día en que inicia el horario, o el día anterior si la regla está marcada "pertenece a la jornada del día anterior" (por ejemplo, el desayuno del turno nocturno al salir). El contrato debe estar vigente en esa fecha.                                                                                                                                                | Alta      |
| RF-27                          | Un empleado no tiene derecho si la fecha de servicio es día de descanso según su rol sincronizado; motivo "Hoy es tu día de descanso".                                                                                                                                                                                                                                                                                                                          | Alta      |
| RF-28                          | El sistema impide guardar dos reglas del mismo turno con horarios traslapados.                                                                                                                                                                                                                                                                                                                                                                                  | Alta      |
| **Entrada desde el celular**   |                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |           |
| RF-07                          | Cada punto de entrada tiene un QR permanente, impreso en un cartel, con un token firmado que identifica el punto. El administrador lo obtiene desde Administración para imprimirlo. El QR da acceso a la aplicación; no comprueba que el empleado esté en el comedor (D-21).                                                                                                                                                                                    | Alta      |
| RF-08                          | El empleado escanea el QR con la cámara de su celular; el QR abre la aplicación web con el token.                                                                                                                                                                                                                                                                                                                                                               | Alta      |
| RF-09                          | El empleado inicia sesión con su ID y contraseña de la empresa; la sesión se recuerda en el celular durante 30 días (configurable). El número de empleado puede llevar letras y números, así que el celular abre el teclado de texto (D-32). El pase tiene un botón «Cerrar sesión» que borra la sesión del celular y vuelve al inicio de sesión del mismo punto, sin anular el consumo ya registrado (D-54).                                                                                                                                                                                                                    | Alta      |
| RF-10                          | El token del QR no caduca. El servidor solo acepta tokens con firma válida y de un punto activo; si no, el celular pide escanear de nuevo y no se registra rechazo.                                                                                                                                                                                                                                                                                             | Alta      |
| RF-11                          | El celular muestra un pase a pantalla completa: Autorizado o Rechazado, foto, nombre, turno, tiempo de comida, hora, y una animación continua que distingue el pase vivo de una captura de pantalla.                                                                                                                                                                                                                                                            | Alta      |
| RF-12                          | Un rechazo muestra su motivo: no registrado, contrato vencido, día de descanso, fuera de horario, turno sin derecho a este tiempo o ya consumió.                                                                                                                                                                                                                                                                                                                | Alta      |
| RF-13                          | El rechazo es definitivo; no hay excepciones manuales en la v1.                                                                                                                                                                                                                                                                                                                                                                                                 | Alta      |
| RF-14                          | Un empleado puede consumir solo una vez por tiempo de comida y fecha de servicio, sin importar el canal (celular, manual o contingencia).                                                                                                                                                                                                                                                                                                                       | Alta      |
| RF-15                          | Eliminado de la pantalla del chef en la v2 (D-36): todos los empleados usan su celular y la huella será la alternativa. Una comida servida sin registro se captura como contingencia (RF-26).                                                                                                                                                                                                                                                                   | Alta      |
| RF-16                          | La identificación es independiente de la validación, de modo que se puedan agregar otros métodos (por ejemplo, huella) sin cambiar las reglas.                                                                                                                                                                                                                                                                                                                  | Alta      |
| RF-31                          | Etapa de registro previo: un enlace permite iniciar sesión y dejar el celular listo antes del arranque, sin registrar consumo.                                                                                                                                                                                                                                                                                                                                  | Media     |
| **Pantalla del chef**          |                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |           |
| RF-29                          | La pantalla del chef muestra la fila de servicio: los autorizados sin servir del horario en curso, en orden de llegada. La primera persona aparece en grande ("Sírvele a", nombre, turno y tiempo de comida) y debajo cuántos y quiénes siguen. Se actualiza cada 2 segundos. La foto queda como mejora futura (D-20, D-35).                                                                                                                                    | Alta      |
| RF-30                          | Cualquier tecla, clic o toque en la TV marca como servida a la persona que se muestra en grande. Tras marcar, se ignora toda entrada durante 1.5 segundos y las teclas repetidas. No hay campos ni botones de aprobar o rechazar. Un consumo se sirve una sola vez (D-35).                                                                                                                                                                                      | Alta      |
| **Registro de consumos**       |                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |           |
| RF-17                          | Cada consumo autorizado guarda empleado, fecha de servicio, tiempo de comida, fecha y hora exacta, punto de entrada, canal (celular, manual o contingencia) y el encargado cuando aplica.                                                                                                                                                                                                                                                                       | Alta      |
| RF-18                          | Cada rechazo se guarda con los mismos datos y su motivo.                                                                                                                                                                                                                                                                                                                                                                                                        | Alta      |
| RF-32                          | Cada Servido se guarda como una entrega (consumo, fecha y hora, chef), de solo inserción.                                                                                                                                                                                                                                                                                                                                                                       | Alta      |
| RF-19                          | Ningún consumo, rechazo ni entrega puede editarse o borrarse por ningún rol.                                                                                                                                                                                                                                                                                                                                                                                    | Alta      |
| **Reportes**                   |                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |           |
| RF-20                          | Conteo de consumos por fecha de servicio, tiempo de comida, turno y área, en un rango de fechas.                                                                                                                                                                                                                                                                                                                                                                | Alta      |
| RF-21                          | Reporte para cotejar la factura del proveedor: comidas **servidas** por fecha de servicio y tiempo de comida en el periodo facturado, y aparte las autorizadas sin servir.                                                                                                                                                                                                                                                                                      | Alta      |
| RF-22                          | Detalle de consumos y rechazos filtrable por empleado, fecha, punto, canal y motivo.                                                                                                                                                                                                                                                                                                                                                                            | Media     |
| RF-23                          | Exportación de reportes: conteo y cotejo a Excel y PDF; detalle a Excel. Cada exportación de conteo y cotejo termina con una fila "Total".                                                                                                                                                                                                                                                                                                                      | Alta      |
| **Administración y seguridad** |                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |           |
| RF-24                          | Hay tres roles: Empleado, Chef y Administrador (D-33). Chef y Administrador inician sesión con usuario y contraseña propios; acceso por rol con JWT. La sesión del chef no caduca (10 años) y la del administrador dura 2 horas; al vencer, el panel pide iniciar sesión y vuelve a la misma pantalla. Desactivar a un usuario o cambiarle el rol cierra su sesión al instante (D-34). Al iniciar sesión, el chef va a su pantalla y el administrador al panel. | Alta      |
| RF-25                          | Alta, baja y edición de puntos de entrada y usuarios del sistema. La clave de un punto y el nombre de un usuario no se editan: para renombrar se da de alta el nuevo y se desactiva el anterior (D-23).                                                                                                                                                                                                                                                         | Alta      |
| RF-33                          | Con el pase Autorizado, el celular muestra el lugar del empleado en la fila y se actualiza cada 3 segundos: "Eres el {n}.º en la fila", "Te están sirviendo: pasa con el chef" y "Servido. ¡Buen provecho!" (D-35).                                                                                                                                                                                                                                             | Alta      |
| RF-34                          | El administrador tiene un solo panel con navegación lateral y todas las funciones: Comedor (la fila en vivo, con botón "Marcar servido" y enlace para abrir la pantalla del chef), Reportes (cotejo, conteo, detalle) y Administración (reglas, puntos, usuarios, personal, sincronización, contingencia). Durante un horario de comida el panel abre en la fila (D-37).                                                                                        | Alta      |
| RF-35                          | El sistema se instala con InstalarComedor.exe, que instala SQL Server Express si falta, deja la aplicación como servicio de Windows que arranca sola, abre el puerto solo para la red local, programa el respaldo diario y crea los accesos directos Pantalla del chef y Panel del comedor (D-39).                                                                                                                                                              | Alta      |
| RF-36                          | La fuente de RR. HH. es una vista de SQL Server o un Excel que el administrador importa desde el panel, con columnas fijas y plantilla descargable; la importación sincroniza al momento con las mismas reglas que la sincronización nocturna (D-42).                                                                                                                                                                                                           | Alta      |
| RF-37                          | La pantalla del chef y el panel solo responden desde la red de la empresa; por internet solo se atiende lo que usa el celular del empleado (D-38).                                                                                                                                                                                                                                                                                                              | Alta      |
| RF-38                          | Dirección e identidad profesionales: puerto 80 (la dirección no lleva número de puerto), el celular quita el código del QR de la barra de direcciones, ícono y título propios ("Comedor de personal") sin rastros de Angular, y la fuente servida por la aplicación. Ocultar la IP por completo llega con el dominio propio (D-41, D-43).                                                                                                                       | Media     |

## 4. Requisitos no funcionales

| ID     | Categoría      | Requisito                                                                                                                                                                                                                                                                                                |
|--------|----------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| RNF-01 | Rendimiento    | Con sesión ya iniciada, el pase aparece en menos de 2 segundos desde que se escanea el QR; en la TV, marcar Servido muestra a la siguiente persona en menos de 1 segundo.                                                                                                                                |
| RNF-02 | Capacidad      | Soporta en su punto de entrada 1,000 consumos por servicio, con picos de 50 validaciones por minuto, sin degradarse.                                                                                                                                                                                     |
| RNF-03 | Integridad     | Dos validaciones simultáneas del mismo empleado producen un solo consumo (restricción única en base de datos sobre matrícula, fecha de servicio y tiempo de comida).                                                                                                                                     |
| RNF-04 | Auditoría      | Los registros de consumo, entrega y rechazo son de solo inserción; ningún endpoint ni rol los modifica.                                                                                                                                                                                                  |
| RNF-05 | Disponibilidad | Si la fuente RR. HH. no responde, el sistema sigue validando con la última copia sincronizada. Reportes y Administración muestran al supervisor y al administrador "La última sincronización con RR. HH. falló; se usan los datos del {fecha}", con la fecha de la última sincronización exitosa (D-22). |
| RNF-06 | Seguridad      | Tokens de QR firmados con HMAC-SHA256 y clave del servidor; contraseñas de usuarios del sistema con hash; HTTPS dentro de la red; las credenciales de la empresa nunca se guardan.                                                                                                                       |
| RNF-07 | Respaldo       | Respaldo automático diario de la base de datos del sistema a las 03:00, conservando 30 días; lo programa el instalador (D-39).                                                                                                                                                                           |
| RNF-08 | Usabilidad     | El pase se distingue a 2 metros por color (verde o rojo) y texto grande; en la TV, el nombre de quien se sirve se lee a 3 metros.                                                                                                                                                                        |
| RNF-09 | Compatibilidad | Celular: Chrome en Android y Safari en iOS actuales. Pantalla del chef: Edge a pantalla completa en la PC del comedor, conectada a una TV. Panel: Chrome y Edge actuales.                                                                                                                                |
| RNF-10 | Mantenibilidad | Arquitectura por capas en el backend y separación modelo-vista-controlador en el frontend (sección 5); reglas de dominio cubiertas por pruebas unitarias.                                                                                                                                                |
| RNF-11 | Portabilidad   | Se instala en Windows 10, 11 o Windows Server con un solo instalador, sin instalar .NET antes (D-39).                                                                                                                                                                                                    |

## 5. Arquitectura

Un solo programa, instalado como servicio de Windows en la PC del comedor, expone la API REST en C# y también publica la aplicación Angular, en un solo puerto (80, para que la dirección no lleve número de puerto). Los celulares, la TV del chef y el panel la abren en el navegador. Las reglas de negocio viven en el dominio y no dependen de frameworks ni de la base de datos.

![Arquitectura: frontend MVC y backend por capas](assets/arquitectura.png)

arquitectura · frontend MVC y backend por capas

Infraestructura implementa las interfaces que define Aplicación; la API solo la conecta al arrancar (línea punteada). En la v2 la API también publica el frontend y la fuente de RR. HH. es una vista de SQL Server o un Excel importado (D-39, D-42).

### 5.1 Backend en C#: arquitectura por capas

| Capa            | Proyecto               | Contenido                                                                                                                                                                                                                                                                                                | Depende de                                      |
|-----------------|------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------|
| Dominio         | Comedor.Domain         | Entidades (Empleado, Turno, TiempoComida, ReglaDerecho, Consumo, Rechazo, Entrega, PuntoEntrada), reglas puras ("¿tiene derecho?", "¿ya comió?", fecha de servicio) y motivos de rechazo.                                                                                                                | Nada                                            |
| Aplicación      | Comedor.Application    | Casos de uso: EmitirTokenQr, ValidarAcceso, ListarSolicitudes, MarcarServido, IniciarSesionEmpleado, SincronizarPersonal, GenerarReporte, AdministrarReglas. Interfaces (IEmpleadoRepositorio, IConsumoRepositorio, IFuentePersonalRRHH, IAutenticadorEmpresa, IFirmaToken, IReloj, IExportador) y DTOs. | Dominio                                         |
| Infraestructura | Comedor.Infrastructure | EF Core sobre SQL Server, repositorios, adaptadores de la fuente RR. HH. y del autenticador de la empresa (simulados en la v1), firma HMAC de tokens, exportación a Excel (ClosedXML) y PDF (QuestPDF).                                                                                                  | Aplicación                                      |
| Presentación    | Comedor.Api            | Controladores REST, autenticación JWT por roles, tarea programada de sincronización, registro de dependencias.                                                                                                                                                                                           | Aplicación (Infraestructura solo para conectar) |

Un proyecto de pruebas por capa: Comedor.Domain.Tests, Comedor.Application.Tests y Comedor.Api.Tests (integración).

### 5.2 Frontend en Angular: modelo, vista y controlador

Angular se basa en componentes; la separación MVC se aplica así:

- **Modelo:** interfaces TypeScript (empleado.model.ts, pase.model.ts, consumo.model.ts) que reflejan los DTOs de la API.

- **Vista:** componentes y plantillas que solo muestran datos y capturan eventos.

- **Controlador:** servicios Angular (pase.service.ts, punto.service.ts, reportes.service.ts, admin.service.ts, auth.service.ts) con la lógica de pantalla; son los únicos que llaman a la API.

Tres módulos funcionales: **Pase** (celular del empleado: inicio de sesión, pase y lugar en la fila), **Chef** (TV de la entrada: fila de servicio) y **Panel** (administrador: reportes y administración), más un módulo compartido de autenticación. La interfaz sigue la guía de diseño del repositorio ([docs/diseno.md](diseno.md)): estilo institucional sencillo, tipografía Montserrat, verde institucional con acento dorado, sin logotipos oficiales hasta que el cliente los proporcione.

### 5.3 Base de datos

SQL Server, natural con .NET y probable en la infraestructura del cliente. Si informática exige otro motor, EF Core permite cambiarlo tocando solo Infraestructura.

## 6. Flujo de validación en la entrada

Antes de las verificaciones, el servidor confirma que el token del QR es auténtico y de un punto activo (RF-10); si no, el celular pide escanear de nuevo y no se registra rechazo. En la captura manual este paso no aplica. Después, cada intento pasa por seis verificaciones en orden; la primera que falla define el motivo del rechazo. Todo resultado aparece al instante en la pantalla del chef; los autorizados entran a la fila de servicio y la TV muestra en grande al primero hasta que el chef presiona cualquier tecla (D-35).

![Flujo de validación: seis verificaciones](assets/flujo-validacion.png)

flujo de validación · 6 verificaciones

La verificación "¿ya consumió?" y el registro del consumo ocurren en una sola transacción con restricción única (empleado, fecha, tiempo de comida), de modo que dos intentos simultáneos (por ejemplo, celular y captura manual) no pueden registrar a la misma persona dos veces (RNF-03).

**Secuencia técnica (celular):** el administrador obtiene GET /api/puntos/{id}/qr una vez e imprime el QR con la URL /pase?t=\<token\> → el celular abre esa URL; si no hay sesión, auth.service.ts llama POST /api/empleados/sesion (valida con IAutenticadorEmpresa y devuelve un JWT de empleado) → pase.service.ts llama POST /api/accesos/celular con el token → ValidarAcceso verifica el token, aplica las reglas y guarda el consumo o el rechazo → el celular muestra el pase.

**Secuencia técnica (TV del chef):** la pantalla pide GET /api/puntos/{id}/fila cada 2 s y muestra en grande al primero → cualquier tecla, clic o toque envía POST /api/consumos/{id}/servido con el consumo mostrado → la TV vuelve a pedir la fila. El celular del empleado autorizado consulta GET /api/accesos/mi-turno cada 3 s para mostrar su lugar.

## 7. Modelo de datos

Ocho entidades cubren la v1; Consumo, Entrega y Rechazo son de solo inserción.

| Entidad      | Campos principales                                                                                                                                      | Notas                                                                                                                                                                                                     |
|--------------|---------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Empleado     | Matrícula (clave), nombre, foto, área, turno, días laborales (conjunto de días de la semana), inicio y fin de contrato, activo                          | Copia de la fuente RR. HH.; solo la modifica la sincronización. La fecha de cada sincronización se consulta en la bitácora (D-28). El formato real del rol de descansos se ajusta al confirmar la fuente. |
| Turno        | Nombre (Matutino, Vespertino, Nocturno)                                                                                                                 | Texto libre en la v1, sin catálogo; debe coincidir exactamente, con mayúsculas y minúsculas, con el valor de RR. HH. (D-17).                                                                              |
| TiempoComida | Nombre (Desayuno, Comida, Cena, Colación)                                                                                                               | Texto libre en la v1, igual que el turno (D-17).                                                                                                                                                          |
| ReglaDerecho | Turno, tiempo de comida, hora de inicio, hora de fin, pertenece a la jornada del día anterior (sí/no)                                                   | Configurable. Fin menor que inicio = cruza la medianoche. Sin traslapes dentro de un turno.                                                                                                               |
| PuntoEntrada | Clave, nombre, activo                                                                                                                                   | Un QR impreso y una pantalla de solicitudes por punto.                                                                                                                                                    |
| Consumo      | Id, matrícula, fecha de servicio, tiempo de comida, fecha y hora exacta, punto de entrada, canal (Celular, Manual, Contingencia), encargado (si aplica) | Restricción única (matrícula, fecha de servicio, tiempo de comida). Sin actualización ni borrado.                                                                                                         |
| Entrega      | Id, consumo (único), fecha y hora, chef                                                                                                                 | Una por consumo. Sin actualización ni borrado.                                                                                                                                                            |
| Rechazo      | Id, matrícula, fecha y hora, fecha de servicio y tiempo de comida (si aplican), punto de entrada, canal, encargado (si aplica), motivo                  | Sin actualización ni borrado.                                                                                                                                                                             |

Tablas de apoyo: UsuarioSistema (usuario, hash de contraseña, rol, activo) y BitacoraSincronizacion (fecha, resultado, altas, cambios, bajas, error).

## 8. Manejo de errores y contingencia

Ningún error puede producir un consumo duplicado ni perder uno registrado.

| Situación                                                                                     | Comportamiento del sistema                                                                                                                                                                                                    |
|-----------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| La fuente RR. HH. no responde durante la sincronización                                       | Se conserva la última copia; se registra el error en la bitácora y se muestra un aviso al supervisor y al administrador.                                                                                                      |
| La sincronización trae datos inválidos (matrícula vacía o repetida, turno fuera del catálogo) | Se omite ese registro, se anota en la bitácora y la sincronización continúa con los demás. Si el empleado ya existía, queda como estaba; no se da de baja. Un empleado dado de baja que vuelve a aparecer se reactiva (D-28). |
| Dos intentos simultáneos del mismo empleado (dos celulares, o celular y captura manual)       | La restricción única permite solo un consumo; el segundo recibe "Rechazado: ya consumió".                                                                                                                                     |
| Token de QR alterado, firmado con otra clave o de un punto inactivo                           | El celular muestra "Escanea de nuevo el código de la entrada"; no se registra rechazo.                                                                                                                                        |
| ID o contraseña de la empresa incorrectos                                                     | El celular muestra "Credenciales incorrectas"; tras 5 intentos fallidos en 15 minutos se bloquea ese ID por 15 minutos.                                                                                                       |
| El servicio de autenticación de la empresa no responde                                        | El celular muestra "No se pudo verificar tu cuenta; acude con el encargado" y el chef usa la captura manual.                                                                                                                  |
| Empleado sin celular, sin batería o sin Wi-Fi                                                 | Acude con el administrador, que registra su comida como contingencia (RF-26); la huella será la alternativa (D-36).                                                                                                           |
| El chef presiona dos veces o deja una tecla apretada                                          | Tras marcar, la TV ignora toda entrada durante 1.5 s y las teclas repetidas, así que no marca por error a la siguiente persona. La entrega es única por consumo (D-35).                                                       |
| Un autorizado nunca recoge su comida                                                          | Queda en la fila hasta el fin del horario; luego sale y aparece en el reporte como autorizado sin servir. Si está primero y no se presenta, la TV lo sigue mostrando hasta que el chef presiona una tecla (riesgo en 2.7).    |
| La pantalla del punto pierde conexión                                                         | Muestra "Sin conexión" y sigue intentando; al reconectar, recupera la lista "Por servir".                                                                                                                                     |
| El servidor no está disponible                                                                | Procedimiento de contingencia: lista en papel firmada por el chef, capturada después por un administrador como consumos de canal Contingencia.                                                                                |
| Error inesperado en la API                                                                    | Se registra en bitácora técnica con fecha, endpoint y detalle; el usuario ve un mensaje genérico.                                                                                                                             |

La captura de contingencia agrega un requisito: **RF-26 (Media)** registrar consumos de canal Contingencia con autor y referencia a la lista en papel, sujetos a la misma restricción única. Solo puede capturarlos un administrador. No se aplican las reglas de derecho (la comida ya se sirvió), pero la matrícula debe existir como empleado sincronizado; si no, se rechaza con "No registrado". La referencia y el tiempo de comida son obligatorios. Un consumo repetido se rechaza con "Ya consumiste este tiempo de comida" (D-15).

## 9. Pruebas y criterios de aceptación

### 9.1 Estrategia de pruebas

| Nivel                   | Qué cubre                                                                                              | Herramienta                   |
|-------------------------|--------------------------------------------------------------------------------------------------------|-------------------------------|
| Unitarias de dominio    | Cada regla de derecho y cada motivo de rechazo, incluidos límites de horario y de vigencia de contrato | xUnit                         |
| Unitarias de aplicación | Casos de uso con repositorios simulados                                                                | xUnit + Moq                   |
| Integración             | API + SQL Server real: restricción única, inmutabilidad de consumos, sincronización                    | xUnit + WebApplicationFactory |
| Frontend                | Servicios y componentes de la pantalla de entrada                                                      | Jasmine/Karma o Jest          |
| Carga                   | Un punto con 1,000 validaciones por servicio y picos de 50 por minuto                                  | k6 o JMeter                   |

### 9.2 Criterios de aceptación

**Resultado de la aceptación (Tarea 15, 5 de noviembre de 2026, D-31):** los 14 criterios se cumplen en la API y la base del entorno de prueba, y la prueba de carga pasa (p95 de 6 ms por celular y 5.9 ms en captura manual, 0 % de fallos, 1,000 consumos = 1,000 autorizados). Los criterios 1, 2, 3 y 12 quedan sin marcar hasta confirmarlos con un celular real y la pantalla del chef en la red del comedor. El detalle está en [aceptacion.md](aceptacion.md).

- [ ] Un empleado vigente, en día laboral, en su turno y dentro del horario ve el pase Autorizado en su celular en menos de 2 segundos tras escanear, con sesión ya iniciada.

- [ ] La misma solicitud aparece en la pantalla del chef en menos de 3 segundos, con nombre, turno y tiempo de comida, en la lista "Por servir".

- [ ] Al marcar Servido, sale de la lista y queda una entrega registrada; un segundo clic no crea otra.

- [x] Un QR alterado, firmado con otra clave o de un punto inactivo pide "Escanea de nuevo" y no registra rechazo.

- [x] Cada uno de los seis motivos de rechazo aparece en el pase cuando corresponde.

- [x] Un segundo intento del mismo empleado en el mismo tiempo de comida, por cualquier canal, es rechazado.

- [x] Dos validaciones simultáneas del mismo empleado producen un solo consumo.

- [x] Un empleado del turno nocturno puede desayunar al salir y su consumo queda en la fecha de la jornada que trabajó, aunque el día siguiente sea su descanso.

- [x] La captura manual del chef aplica las mismas reglas que el celular.

- [x] Ningún rol puede editar ni borrar consumos, entregas o rechazos.

- [x] Un empleado con contrato vencido en la fuente RR. HH. pierde el derecho tras la siguiente sincronización.

- [ ] Con la fuente RR. HH. caída, el sistema sigue validando y muestra el aviso.

- [x] El reporte de cotejo cuenta solo comidas servidas, separa las autorizadas sin servir, y se exporta a Excel y PDF.

- [x] La prueba de carga cumple RNF-01 y RNF-02.

**Criterios de la v2** (plan v2, por verificar):

- [ ] La TV del chef muestra en grande a la primera persona de la fila; cualquier tecla, clic o toque la marca como servida y aparece la siguiente.

- [ ] Dos pulsaciones seguidas o una tecla apretada marcan solo a una persona.

- [ ] El celular de un autorizado muestra su lugar en la fila y luego "Servido. ¡Buen provecho!".

- [ ] La TV del chef no pide iniciar sesión de nuevo tras reiniciar la PC; desactivar al chef en el panel cierra su sesión.

- [ ] El administrador entra a un solo panel con reportes y administración; los roles disponibles son Chef y Administrador.

- [ ] Una petición al panel que llega por un túnel o desde fuera de la red responde 403.

- [ ] Importar el Excel de personal desde el panel da altas, cambios, bajas y omitidos correctos.

- [ ] En una PC con Windows limpia, InstalarComedor.exe deja el sistema funcionando, arranca solo tras reiniciar y genera el respaldo diario.

## 10. Fuera de alcance y subproyectos futuros

La v1 excluye explícitamente:

- Excepciones manuales al rechazo y registro de visitantes o personal externo.

- Adaptador real del autenticador de la empresa: falta saber qué sistema valida las contraseñas (D-40). **Bloquea el uso en producción.**

- **Acceso desde datos móviles u otro Wi-Fi (D-41):** Cloudflare Tunnel con dominio propio. Implementación futura.

- Torniquetes o barreras físicas.

- Cobro, caja y facturación.

- Dietas hospitalarias y atención a pacientes.

- **v2:** aviso de privacidad en el primer inicio de sesión, con el texto que proporcione el cliente.

**Huella (v2):** el cliente ya tiene un sistema de identificación por huella sobre su base de empleados. Es la alternativa si informática no permite usar la contraseña corporativa, y la ruta para mayor seguridad después. Conviene reunirse con ellos para evaluar si puede usarse en la entrada. La arquitectura lo permite sin cambiar las reglas (RF-16).

**Mejoras futuras ya identificadas:** mostrar el área del empleado en el pase (D-19) y la foto en la pantalla del chef, si RR. HH. la proporciona (D-20).

Subproyectos posteriores de la plataforma, cada uno con su propio SRS:

1.  Planeación de menús y recetario.

2.  Inventarios y almacén (lotes, caducidades, PEPS).

3.  Compras y proveedores.

4.  Producción y costos por ración.

5.  Control sanitario e inocuidad (bitácoras de temperatura, limpieza, plagas).

6.  Control del personal de cocina.

7.  Tablero de indicadores y alertas integradas.

## 11. Pendientes por confirmar con el cliente

Estas decisiones pueden cambiar requisitos; cada una indica a cuáles afecta.

- [ ] **Bloqueante:** sistema contra el que se validan el ID y la contraseña de la empresa, y si informática lo permite (RF-09, D-40).

- [ ] Reunión sobre el sistema de huella existente y si puede reutilizarse (sección 10).

- [ ] Wi-Fi accesible desde celulares personales. HTTPS y acceso desde datos móviles llegan con el dominio propio (D-41, RNF-06).

- [ ] Motor de la base de empleados y quién puede crear la vista vw_EmpleadosComedor; mientras tanto, importación por Excel (RF-36, D-42), incluido el formato del rol de descansos (RF-27).

- [ ] Si la fuente RR. HH. entrega una lista vacía, ¿debe tratarse como falla? Hoy da de baja a todos los empleados activos (D-28).

- [ ] Formato de la matrícula en la fuente RR. HH.: si es solo numérica o puede llevar letras (D-32).

- [ ] PC para el comedor (Windows 10, 11 o Server), TV para el chef y reserva de su IP en el router (D-39).

- [ ] Lista de turnos y horarios de cada tiempo de comida, cuáles pertenecen a la jornada del día anterior y cómo tratar horarios que se traslapen (RF-05, RF-06, RF-28).

- [ ] Formato y periodo de la factura del proveedor para el reporte de cotejo (RF-21).

- [ ] Disponibilidad de foto del empleado en la fuente RR. HH. (RF-11, RF-29).

- [ ] Licencia de QuestPDF: es gratuita para organizaciones con ingresos anuales menores a 1 millón de dólares; confirmar si aplica.

## Anexo A: Enfoques evaluados

Se evaluaron tres formas de construir el sistema; se eligió el enfoque 1.

| Enfoque                                           | Cómo funciona                                                                                                                                                  | A favor                                                                                                                                         | En contra                                                                                                                                            |
|---------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1\. Aplicación web central (recomendado)          | Un servidor en la red del cliente con base de datos y lógica; las tablets usan el navegador; un proceso programado sincroniza empleados y turnos desde RR. HH. | Lo más sencillo de construir, mantener y actualizar (una sola vez en el servidor). Evitar dobles registros es fácil con una sola base de datos. | Si el servidor o la red interna fallan, la entrada se detiene. Se mitiga con contingencia: lista en papel que se captura después.                    |
| 2\. Aplicación instalada por punto con base local | Cada tablet o PC tiene su copia de los datos y sincroniza con el servidor.                                                                                     | Sigue funcionando aunque se caiga la red.                                                                                                       | Mucho más compleja. Registros simultáneos sin conexión generan conflictos en el conteo para facturar. Varias aplicaciones que instalar y actualizar. |
| 3\. Adaptar software comercial                    | Comprar un producto de comedores o asistencia y configurarlo.                                                                                                  | Sale rápido.                                                                                                                                    | Rara vez se integra bien con una base de turnos propia; reportes de validación de factura limitados; dependencia del fabricante.                     |
