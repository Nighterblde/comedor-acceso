# Registro de decisiones

Cada decisión indica su estado, el motivo y las alternativas descartadas. Una decisión aceptada solo cambia con una nueva entrada que la sustituya.

| # | Decisión | Estado |
| --- | --- | --- |
| [D-01](#d-01-dividir-la-plataforma-en-subproyectos) | Dividir la plataforma en subproyectos; empezar por control de acceso y consumo | Aceptada |
| [D-02](#d-02-aplicación-web-central-on-premise) | Aplicación web central on-premise | Aceptada |
| [D-03](#d-03-c-por-capas-y-angular-con-separación-mvc) | C# por capas y Angular con separación MVC | Aceptada |
| [D-04](#d-04-derecho-por-turno-fijo-y-días-laborales) | Derecho por turno fijo y días laborales, leídos de RR. HH. | Aceptada |
| [D-05](#d-05-entrada-por-celular-con-qr-rotativo) | Entrada por celular con QR rotativo | Aceptada |
| [D-06](#d-06-el-chef-solo-marca-servido) | El chef solo marca Servido; no aprueba ni rechaza | Aceptada |
| [D-07](#d-07-registros-de-solo-inserción) | Registros de solo inserción | Aceptada |
| [D-08](#d-08-fecha-de-servicio-y-jornada-anterior) | Fecha de servicio y jornada anterior | Aceptada |
| [D-09](#d-09-el-cotejo-de-la-factura-cuenta-comidas-servidas) | El cotejo de la factura cuenta comidas servidas | Aceptada, revisable |
| [D-10](#d-10-integraciones-simuladas-en-la-v1) | Integraciones simuladas en la v1 | Aceptada |
| [D-11](#d-11-uso-de-la-cuenta-de-otro-empleado) | Uso de la cuenta de otro empleado como riesgo aceptado | Aceptada |
| [D-12](#d-12-un-solo-punto-de-entrada) | Un solo punto de entrada | Aceptada |
| [D-13](#d-13-diseño-institucional-sin-identidad-oficial) | Diseño institucional sin identidad oficial | Aceptada |
| [D-14](#d-14-el-conteo-también-se-exporta) | El conteo también se exporta a Excel y PDF | Aceptada |

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
