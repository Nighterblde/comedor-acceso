# Guía para trabajar en este repositorio

## Fuentes de verdad

- `docs/srs.md`: qué debe hacer el sistema. Si el código y el SRS no coinciden, gana el SRS.
- `docs/plan.md`: cómo se construye, tarea por tarea. Sus "Restricciones globales" aplican a todas las tareas.
- `docs/decisiones.md`: por qué se tomó cada decisión. No revertir una decisión sin consultarlo.

Antes de empezar cada tarea, vuelve a leer su sección del plan y los requisitos del SRS que implementa. Si encuentras una contradicción o un hueco, detente y pregunta; no inventes reglas de negocio.

## Forma de trabajo

- Ejecuta el plan con la skill `subagent-driven-development`: un implementador por tarea y una revisión contra SRS y plan antes de pasar a la siguiente.
- Desarrollo guiado por pruebas: prueba que falla, implementación mínima, prueba que pasa, commit.
- No declares una tarea terminada sin haber corrido sus comandos de verificación y visto el resultado.
- Trabaja en una rama `tarea/<número>-<nombre>` y abre un pull request hacia `main` al terminar cada tarea.

## Reglas que no se negocian

- Consumo, Rechazo y Entrega son de solo inserción. Ningún método actualiza ni borra esos registros.
- Toda hora sale de `IReloj`. Nunca `DateTime.Now` ni `DateTime.UtcNow` en código de producción.
- `Comedor.Domain` no depende de ningún paquete ni de otro proyecto.
- El sistema nunca escribe en la fuente de RR. HH.
- Los textos de motivos de rechazo son exactamente los de "Restricciones globales" en el plan.
- Nada de secretos en el repositorio: claves y cadenas de conexión van en user-secrets o variables de entorno.

## Comandos

```bash
dotnet test backend/Comedor.sln
cd frontend/comedor-web && npx ng test --watch=false --browsers=ChromeHeadless
```

## Idioma

Nombres de dominio, mensajes al usuario, commits y documentación en español. Identificadores técnicos del framework (controladores, interfaces estándar) siguen las convenciones de .NET y Angular.
