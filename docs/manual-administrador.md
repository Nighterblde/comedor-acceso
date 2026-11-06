# Manual del administrador

El administrador usa el **Panel del comedor** (acceso directo del escritorio de la computadora del comedor, o `http://<dirección de la computadora>/panel` desde otra computadora de la red de la empresa). Entra con el usuario y la contraseña que se eligieron al instalar. La sesión dura **2 horas**; al terminar, vuelve a entrar.

El panel solo abre desde la red de la empresa, no desde internet ni desde datos móviles.

## El panel

A la izquierda hay un menú con tres grupos:

- **Comedor > Fila:** la misma fila del chef. Puedes ver quién sigue y marcar **Servido** si hace falta. Si no hay un punto de entrada creado, te lo avisa.
- **Reportes:** Cotejo de factura, Conteo y Detalle.
- **Administración:** Reglas, Puntos, Usuarios, Personal, Sincronización y Contingencia.

Arriba aparecen tu usuario y la hora hasta la que dura la sesión. Si la última sincronización con RR. HH. falló, verás un aviso.

## Reportes

1. Elige **Reportes** y el tipo que necesitas.
2. Escribe las fechas **Desde** y **Hasta** y pulsa para consultar.
3. **Cotejo de factura:** cuenta solo las comidas **servidas** y las separa de las autorizadas que no se sirvieron. Sirve para comparar con la factura del proveedor.
4. **Conteo:** cuántas comidas hubo, agrupadas.
5. **Detalle:** cada consumo, con filtros por matrícula, punto, canal y motivo de rechazo.
6. Cada reporte se puede **exportar a Excel o PDF** (el detalle, solo a Excel).

Los consumos y rechazos no se pueden editar ni borrar, ni siquiera siendo administrador.

## Reglas

En **Administración > Reglas** defines **quién tiene derecho a comer y cuándo**: para cada turno, el tiempo de comida (desayuno, comida, cena) y la hora de inicio y de fin. Fuera de ese horario, el sistema responde "Fuera de horario". Los cambios aplican de inmediato.

## Puntos y QR

En **Administración > Puntos** se crean los puntos de entrada y se imprime su QR (**Imprimir QR**). Si la computadora cambia de dirección en la red, hay que reimprimir el QR desde aquí.

## Usuarios

En **Administración > Usuarios** das de alta a quienes usan el sistema, con su contraseña:

- **Chef:** usa la Pantalla del chef. Su sesión no caduca, pero puedes **desactivarlo** para que deje de entrar.
- **Administrador:** usa el panel.

Los empleados que comen **no** se crean aquí: entran con su ID y contraseña de la empresa.

## Importar personal

Aquí se actualiza la lista de quién trabaja en la empresa.

1. Entra a **Administración > Personal**.
2. Pulsa **Descargar plantilla**. Es un Excel con las columnas que el sistema espera; no cambies los encabezados.
3. Llénalo con el personal vigente (una fila por persona) y guárdalo como `.xlsx` de hasta 5 MB.
4. Elige el archivo y pulsa **Importar**.
5. El sistema lo revisa al momento y muestra: Altas, Cambios, Bajas y Omitidos (filas con datos ilegibles).

Si el archivo está dañado o le faltan columnas, no se pierde el último archivo bueno. Todas las madrugadas (2:00) el sistema vuelve a leer este mismo archivo, así que **cada vez que haya cambios, sube un Excel actualizado** con todo el personal.

En **Administración > Sincronización** puedes pulsar **Sincronizar ahora** para repetir la lectura cuando quieras y ver si la última salió bien.

## Contingencia

Si el sistema dejó de funcionar y se sirvió con lista en papel firmada por el chef, en **Administración > Contingencia** se capturan después esas comidas: matrícula, fecha de servicio, tiempo de comida y referencia de la lista en papel.

## Respaldos

El sistema guarda **un respaldo de toda la información cada día a las 3:00 de la mañana**, y conserva 30 días.

- Están en la computadora, en `C:\ProgramData\Comedor\Respaldos`, con nombres como `Comedor-20261106-0300.bak`.
- **Copia esa carpeta** de vez en cuando a una memoria USB o a otra computadora: si se descompone el disco de esta, los respaldos se pierden con ella.
- Para hacer un respaldo en el momento, pide a informática que ejecute `respaldar.ps1` (en `C:\Program Files\Comedor\instalador`).
- Para recuperar un respaldo hace falta informática; no lo hagas por tu cuenta.

Para comprobar que todo funciona, informática puede ejecutar `verificar.ps1` (ver la guía de instalación).
