# Guía de instalación del Comedor

Esta guía es para quien instala el sistema en la empresa. No necesitas saber programar. Todo se hace con un solo archivo: `InstalarComedor.exe`.

## Qué necesitas

- Una computadora con **Windows 10, Windows 11 o Windows Server**, que se quede **encendida** durante el horario del comedor y conectada por cable a la red de la empresa.
- Ser **administrador** de esa computadora.
- **Internet** solo durante la instalación, si la computadora no tiene SQL Server Express (el instalador lo descarga solo; pesa varios cientos de MB).
- Un **celular** conectado a la misma red, para probar al final.
- Una **impresora**, para el código QR de la entrada.
- No hace falta instalar nada más.

## Instalar

1. Copia `InstalarComedor.exe` a la computadora y haz doble clic. Si Windows pide permiso, acepta.
2. El asistente te pide tres datos:
   - **Usuario y contraseña del administrador.** Con ellos entrarás al panel. La contraseña debe tener **al menos 8 caracteres**. Anótala en un lugar seguro.
   - **Puerto.** Deja el **80**. Si ya lo usa otro programa, el asistente te lo dice y propone el **8080**; acepta esa propuesta.
3. Pulsa **Instalar** y espera. Puede tardar varios minutos: si hace falta, primero instala la base de datos (SQL Server Express), y luego deja el sistema funcionando.
4. Al terminar verás dos accesos directos en el escritorio: **Pantalla del chef** y **Panel del comedor**.

El sistema queda funcionando solo y **arranca cada vez que se enciende la computadora**; nadie tiene que abrir ningún programa.

Dos cosas que hace la instalación y conviene saber:

- Permite la entrada al sistema **solo desde la red de la empresa**.
- Programa un **respaldo todos los días a las 3:00 de la mañana**, que se guarda en `C:\ProgramData\Comedor\Respaldos` y se conserva 30 días.

## Reservar la IP en el router

El código QR que se imprime lleva la dirección de esta computadora en la red (por ejemplo, `192.168.1.20`). **Si esa dirección cambia, los QR impresos dejan de funcionar.** Para evitarlo, hay que pedirle al router que **siempre le dé la misma dirección** a esta computadora:

1. Entra a la página de configuración del router (la dirección suele estar escrita en una etiqueta del aparato). Si no tienes acceso, pide ayuda al área de informática.
2. Busca una opción llamada **"Reserva de DHCP"**, **"Asignación de IP estática"** o **"Dirección IP fija"**.
3. Elige esta computadora de la lista y reserva la dirección que ya tiene.
4. Guarda y, si el router lo pide, reinícialo.

Para saber la dirección actual: en esta computadora, abre **Símbolo del sistema**, escribe `ipconfig` y busca "Dirección IPv4".

## Imprimir el QR

1. Abre **Panel del comedor** (el acceso directo del escritorio) e inicia sesión con el administrador.
2. Entra a **Administración > Puntos**. Si no hay ningún punto de entrada, créalo (por ejemplo, "Entrada del comedor") y guarda.
3. En su fila, pulsa **Imprimir QR** y luego **Imprimir**.
4. Pega la hoja en la entrada del comedor.

Este QR sirve para siempre mientras la dirección de la computadora no cambie. Si cambia, hay que volver a imprimirlo (ver "Verificación").

## Verificación

Haz esta lista cuando termines de instalar, después de reiniciar la computadora y cada vez que algo parezca fallar.

**Con la computadora:**

1. Abre PowerShell como administrador: pulsa **Inicio**, escribe `PowerShell`, haz clic derecho en el resultado y elige **Ejecutar como administrador**. Luego escribe esta línea y pulsa Enter:
   `powershell -ExecutionPolicy Bypass -File "C:\Program Files\Comedor\instalador\verificar.ps1"`
   Si dice "Abre PowerShell como administrador y vuelve a ejecutar", no abriste la ventana con permisos de administrador.
2. Debe mostrar en **verde**:
   - [ ] El servicio "Comedor" está en ejecución.
   - [ ] El sistema responde.
   - [ ] Existe la regla de firewall "Comedor".
   - [ ] Existe la tarea de respaldo "Comedor - Respaldo".
   - [ ] La dirección del QR es una dirección actual de la computadora.
3. Si algo sale en **rojo**, no sigas: revisa la sección "Si algo sale mal".
4. Si dice **"La IP cambió: reimprime el QR desde Panel > Puntos"**, la dirección de la computadora cambió. Reserva la IP en el router (ver arriba) y vuelve a imprimir el QR.

**Con un celular, conectado a la red de la empresa:**

- [ ] Escanea el QR impreso con la cámara.
- [ ] Inicia sesión con tu ID y contraseña de la empresa.
- [ ] Ves el pase **Autorizado** (o el motivo del rechazo).
- [ ] En la computadora, la **Pantalla del chef** muestra a la persona en la fila.

## Antes de empezar a usarlo: cargar al personal

El sistema usa un archivo de Excel con el personal. Entra al panel, a **Administración > Personal**, descarga la plantilla, llénala y súbela (ver la guía del administrador). Hasta que lo hagas, nadie tiene derecho a comer. Este archivo es también el que el sistema revisa todas las madrugadas, así que cada vez que cambie el personal basta con subir el Excel nuevo.

## Actualizar

1. Cierra la Pantalla del chef si está abierta.
2. Ejecuta el `InstalarComedor.exe` nuevo, sobre la misma computadora.
3. El asistente **no vuelve a pedir** usuario, contraseña ni puerto: conserva todo lo que ya tenías (personal, consumos, claves, QR impresos).
4. Corre `verificar.ps1` otra vez.

## Desinstalar

1. Abre **Configuración de Windows > Aplicaciones > Aplicaciones instaladas**, busca "Comedor de personal" y elige **Desinstalar**.
2. Se quitan el servicio, el permiso en el firewall, el respaldo programado y los accesos directos.
3. **Se conservan** la base de datos y la carpeta `C:\ProgramData\Comedor` (configuración, archivo de personal y respaldos), para no perder información por accidente. Si de verdad quieres borrarlos, hazlo con ayuda de informática.

## Si algo sale mal

- **"El servicio no está en ejecución":** reinicia la computadora y vuelve a correr `verificar.ps1`. Si sigue igual, llama a quien instaló el sistema.
- **"El sistema no responde" en una computadora con puerto 8080:** en las direcciones hay que escribir `:8080` al final.
- **"La IP cambió":** reserva la IP en el router y reimprime el QR.
- **No hay respaldos:** revisa que exista la tarea "Comedor - Respaldo" y que la carpeta `C:\ProgramData\Comedor\Respaldos` tenga archivos recientes.

---

## Nota técnica (para informática)

- La configuración está en `C:\ProgramData\Comedor\appsettings.Local.json`. Solo la leen SYSTEM y los administradores. Se crea una vez; las actualizaciones no la tocan. Se lee **después** de las variables de entorno, así que sus valores mandan sobre ellas.
- La fuente de personal se instala en **Excel** (`Fuente:Tipo = "Excel"`), para que la sincronización nocturna lea el mismo archivo que importa el administrador (D-51). Para usar la vista de SQL Server, hay que cambiar ese valor y agregar la cadena de la fuente (D-49).
- El respaldo manual se hace con `respaldar.ps1`, en la misma carpeta que `verificar.ps1`.
- La aplicación usa HTTP dentro de la red de la empresa (D-39).
