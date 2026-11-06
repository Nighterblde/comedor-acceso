# Guía de diseño

## Sistema de control de acceso y consumo del comedor de personal

| Campo | Valor |
| --- | --- |
| Versión | 1.1 |
| Fecha | 6 de noviembre de 2026 |
| Estado | Aprobada para implementación |
| Prototipo | [Lienzo de pantallas en Claude](https://claude.ai/artifact/Dpu1MLjke1vM65gV1o9oBn) (privado; se comparte desde su menú Compartir) |
| Requisitos | [SRS](srs.md) · [Plan](plan.md) |

Esta guía es la referencia visual del frontend. Toda pantalla nueva usa estos tokens y componentes; si algo no está aquí, se agrega primero a esta guía.

## 1. Principios

1. **Institucional y sencillo.** Estilo sobrio de sitio público: tipografía Montserrat, verde institucional, acento dorado discreto y fondo claro. Nada decorativo que no ayude a la tarea.
2. **Se lee a distancia.** El pase del celular y la pantalla del chef se leen a 1 o 2 metros: estados en texto grande, color y un ícono, nunca solo color.
3. **Una acción principal por pantalla.** Entrar, Servido, Consultar, Guardar regla.
4. **El sistema decide; la interfaz lo explica.** Todo rechazo muestra su motivo con el texto exacto del SRS.
5. **Sin identidad oficial prestada.** No se usan logotipos, escudos ni nombres de instituciones. El sistema se identifica como "Comedor de personal"; la identidad oficial se agrega solo si el cliente la proporciona y autoriza.

## 2. Tokens

### 2.1 Colores

| Token | Valor | Uso |
| --- | --- | --- |
| `--color-institucional-900` | `#10312B` | Barra superior, títulos |
| `--color-institucional-700` | `#235B4E` | Botones primarios, enlaces, navegación activa |
| `--color-autorizado` | `#1D5C49` | Fondo del pase autorizado, botón Servido, borde de tarjeta por servir |
| `--color-rechazado` | `#8A1F34` | Fondo del pase rechazado, motivos de rechazo, errores de formulario |
| `--color-dorado` | `#BC955C` | Franja bajo la barra superior, anillo de foco, borde animado del pase |
| `--color-dorado-claro` | `#D8C3A0` | Rótulo "Comedor de personal" sobre fondo oscuro |
| `--color-advertencia-fondo` | `#FBF4E6` | Aviso de sincronización fallida |
| `--color-advertencia-borde` | `#E2C98F` | Borde del aviso |
| `--color-advertencia-texto` | `#4D3C19` | Texto del aviso; cifras "sin servir" usan `#6B5426` |
| `--color-fondo` | `#F5F5F2` | Fondo de página |
| `--color-superficie` | `#FFFFFF` | Tarjetas, tablas, formularios |
| `--color-borde` | `#DAD7CD` | Bordes de tarjetas y separadores |
| `--color-borde-campo` | `#8A918C` | Bordes de campos de formulario |
| `--color-texto` | `#1F2421` | Texto principal |
| `--color-texto-secundario` | `#4A514D` | Texto de apoyo |
| `--color-texto-terciario` | `#5A615D` | Notas, marcas de hora |
| `--color-encabezado-tabla` | `#EEF3F0` | Fondo de encabezados de tabla |
| `--color-chip-verde` | `#E8F2EE` | Chip de turno, navegación lateral activa |
| `--color-chip-dorado` | `#F4ECDD` | Chip de tiempo de comida (texto `#5C4520`) |

Todas las combinaciones de texto usadas cumplen contraste 4.5:1 o más. Autorizado y rechazado se distinguen además por ícono (palomita o cruz) y por la palabra en mayúsculas.

### 2.2 Tipografía

Familia: **Montserrat** (Google Fonts, pesos 400, 500, 600, 700 y 800), con respaldo `system-ui, sans-serif`.

| Estilo | Tamaño / peso | Uso |
| --- | --- | --- |
| Estado del pase | 48 px / 800 | AUTORIZADO, RECHAZADO |
| Reloj del pase | 40 px / 700, cifras tabulares | Hora con segundos |
| Título de página | 28 a 30 px / 700 | Encabezado principal |
| Título de sección | 17 a 22 px / 700 | Tarjetas, tablas |
| Cuerpo | 15 px / 400 | Texto general, celdas |
| Campo y botón | 15 a 17 px / 600 | Etiquetas y botones |
| Rótulo superior | 12 px / 600, mayúsculas, espaciado 0.08em | "Comedor de personal" |
| Nota | 13 px / 400 | Ayudas y marcas de hora |

### 2.3 Espaciado, radios y tamaños

| Token | Valor |
| --- | --- |
| Escala de espaciado | 4, 8, 12, 14, 16, 20, 24, 28, 32 px |
| Radio de campos y botones | 6 px |
| Radio de tarjetas | 10 px |
| Radio de chips | 999 px |
| Altura mínima de botón | 48 px (52 a 56 px en celular y pantalla del chef) |
| Altura de campo | 46 a 52 px |
| Ancho máximo de contenido | 1200 a 1240 px |
| Franja dorada bajo la barra superior | 4 px |

### 2.4 Archivo de tokens para Angular

La Tarea 11 crea `frontend/comedor-web/src/styles/_tokens.scss` con estos valores:

```scss
:root {
  --color-institucional-900: #10312B;
  --color-institucional-700: #235B4E;
  --color-autorizado: #1D5C49;
  --color-rechazado: #8A1F34;
  --color-dorado: #BC955C;
  --color-dorado-claro: #D8C3A0;
  --color-advertencia-fondo: #FBF4E6;
  --color-advertencia-borde: #E2C98F;
  --color-advertencia-texto: #4D3C19;
  --color-fondo: #F5F5F2;
  --color-superficie: #FFFFFF;
  --color-borde: #DAD7CD;
  --color-borde-campo: #8A918C;
  --color-texto: #1F2421;
  --color-texto-secundario: #4A514D;
  --color-texto-terciario: #5A615D;
  --color-encabezado-tabla: #EEF3F0;
  --color-chip-verde: #E8F2EE;
  --color-chip-dorado: #F4ECDD;

  --fuente: 'Montserrat', system-ui, sans-serif;
  --radio-control: 6px;
  --radio-tarjeta: 10px;
  --alto-boton: 48px;
  --foco: 3px solid var(--color-dorado);
}
```

## 3. Componentes

| Componente | Especificación |
| --- | --- |
| Barra superior | Fondo `institucional-900`, franja dorada de 4 px abajo. Izquierda: rótulo "Comedor de personal" y nombre de la pantalla. Derecha: usuario y rol. |
| Botón primario | Fondo `institucional-700`, texto blanco 15 a 17 px / 600, alto mínimo 48 px. |
| Botón secundario | Fondo blanco, borde y texto `institucional-700`. Exportar Excel o PDF. |
| Botón Servido | Fondo `autorizado`, ícono de palomita, 52 px de alto, 17 px / 700. |
| Campo de texto | Etiqueta visible arriba (`label for`), borde `borde-campo`, foco con anillo dorado de 3 px. Error: borde `rechazado` de 2 px y mensaje debajo, enlazado con `aria-describedby`. |
| Tarjeta | Superficie blanca, borde `borde`, radio 10 px, relleno 18 a 22 px. Tarjeta por servir: borde superior de 4 px `autorizado`. |
| Chip | Radio completo, 14 px / 600. Turno en verde claro y tiempo de comida en dorado claro. |
| Avatar | Círculo con la foto del empleado; si no hay foto, iniciales sobre verde claro. |
| Tabla | Encabezado `encabezado-tabla`, filas separadas por línea clara, cifras tabulares alineadas a la derecha, fila de total con borde superior de 2 px. Dentro de un contenedor con desplazamiento horizontal en pantallas angostas. |
| Aviso | Fondo y borde de advertencia, ícono de triángulo y una acción en enlace. `role="status"`. |
| Navegación lateral | Lista de enlaces, el activo con fondo `chip-verde` y `aria-current="page"`. En celular pasa arriba del contenido. |

## 4. Pantallas

| Pantalla | Dispositivo | Contenido clave | Requisitos |
| --- | --- | --- | --- |
| Iniciar sesión | Celular 390 × 844 | Punto leído del QR, número de empleado (teclado de texto, acepta letras y números, D-32), contraseña, botón Entrar, aviso de sesión por 30 días, ayuda "acude con el chef". | RF-08, RF-09, RF-31 |
| Pase autorizado | Celular | Fondo verde, ícono, AUTORIZADO, "Pasa con el chef", texto del turno en la fila (RF-33, 28 px, región `role="status"`), tarjeta con foto, nombre, matrícula, chips de turno y tiempo (el área queda como mejora futura, [D-19](decisiones.md#d-19-el-pase-no-muestra-el-área)), reloj con segundos y borde animado. | RF-11, RNF-08 |
| Pase rechazado | Celular | Fondo guinda, ícono, RECHAZADO, tarjeta con el motivo exacto y cuándo ocurrió, datos del empleado, reloj y borde animado. | RF-11, RF-12 |
| Pantalla del chef | TV o pantalla 1280 × 800 o mayor, horizontal | Barra con punto y hora. Al centro, "Sírvele a" y el nombre de la persona en 96 px o más, con chips de turno y tiempo de comida (sin foto, D-20). Abajo, "Siguen" con los nombres que siguen y cuántos hay en la fila. Sin personas: "Esperando al siguiente…". Sin campos de texto ni botones: cualquier tecla, clic o toque marca Servido a la persona mostrada (D-35, D-36). El QR no aparece aquí: va impreso (D-21). En el panel del administrador la misma pantalla trae el botón "Marcar servido" y no reacciona a teclas ni toques. | RF-15, RF-29, RF-30 |
| Panel del administrador | Escritorio, adaptable | Barra superior con usuario, rol y hora de fin de sesión; aviso de sincronización fallida; navegación lateral en tres grupos: Comedor (Fila), Reportes (Cotejo de factura, Conteo, Detalle) y Administración (Reglas, Puntos, Usuarios, Personal, Sincronización, Contingencia). En celular la navegación pasa arriba del contenido. `/panel` abre en la fila durante un horario de comida y en el cotejo fuera de él. | RF-34 |
| Fila en el panel | Escritorio, adaptable | La pantalla del chef dentro del panel: persona actual, siguientes, botón Marcar servido (la tecla y el toque no marcan) y enlace "Abrir pantalla del chef" en pestaña nueva. | RF-34, RF-29 |
| Cotejo de factura | Escritorio, adaptable | Filtro de fechas, Consultar, Exportar Excel y PDF, indicadores de servidas, sin servir y rechazos, tabla por fecha de servicio y tiempo de comida con fila de total. | RF-21, RF-23 |
| Reglas de derecho | Escritorio, adaptable | Dentro del panel (navegación lateral y aviso de sincronización fallida), tabla de reglas, formulario con validación de traslape y casilla "pertenece a la jornada del día anterior". | RF-05, RF-06, RF-28, RNF-05 |

## 5. Comportamiento visual

- **Pase vivo:** el borde del pase cambia de dorado a blanco cada 1.6 s, y el reloj avanza cada segundo. Con `prefers-reduced-motion` la animación se hace más lenta, pero no desaparece, porque es la señal antifraude.
- **QR impreso (D-21):** Administración > Puntos muestra el QR del punto en una vista imprimible; no rota ni tiene cuenta regresiva.
- **Sin conexión:** si la pantalla del chef no puede consultar la fila, muestra "Sin conexión" y sigue intentando.
- **Fila del chef:** se consulta cada 2 s. Al marcar Servido (cualquier tecla, clic o toque) se vuelve a pedir la fila de inmediato y se ignora toda entrada durante 1,5 s, además de las teclas repetidas y mientras haya una petición en curso; así una pulsación doble o sostenida marca solo a la persona mostrada.
- **Estados de carga:** en botones, el texto cambia a "Validando…" y se desactivan para evitar dobles envíos.

## 6. Accesibilidad

- Controles reales: `button`, `a href`, `input` con `label`. Nada clicable sobre `div` o `span`.
- Foco visible en todo control: anillo dorado de 3 px.
- Íconos decorativos con `aria-hidden="true"`; íconos con significado con `role="img"` y `aria-label`.
- Objetivos táctiles de 44 px como mínimo.
- Contraste 4.5:1 o más en todo el texto, incluidas las notas de 13 px.
- `lang="es"` en el documento.
