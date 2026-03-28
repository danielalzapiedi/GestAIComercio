# Análisis profundo funcional + técnico (GestAI Comercio)

Fecha: 2026-03-26

## Objetivo
Realizar una nueva revisión integral del código (frontend Blazor + API + Application) para detectar riesgos, inconsistencias y mejoras prioritarias.

## Hallazgos críticos

### C1) Exposición de contraseña temporal en UI de usuarios
- En alta de usuarios se muestra en un mensaje de éxito la contraseña temporal generada.
- Esto deja la credencial visible en pantalla y potencialmente en capturas/registros operativos.
- Evidencia: `Users.razor` asigna `_success = $"Usuario creado. Contraseña temporal generada: {_form.Password}"`.
- Recomendación: reemplazar por flujo de invitación o seteo inicial por link temporal; si se mantiene contraseña temporal, mostrarla una sola vez en modal con warning y acción de copiado, nunca persistirla en mensajes de estado comunes.

### C2) Generación de passwords no criptográfica
- La generación de contraseña temporal y de seed usa `Random.Shared`, que no es CSPRNG.
- Evidencia: `Users.razor` y `Program.cs`.
- Riesgo: predictibilidad estadística en escenarios de alta frecuencia.
- Recomendación: migrar a `RandomNumberGenerator.GetInt32` o utilidades criptográficas del framework.

### C3) Password admin seed con default hardcodeado
- Aunque se incorporó configuración por entorno, si no se define `Seed:AdminPassword` se usa `Admin123$`.
- Evidencia: `Program.cs`.
- Riesgo: despliegues no endurecidos con credencial trivial.
- Recomendación: requerir variable de entorno en producción o generar aleatorio + rotación forzada al primer login.

## Hallazgos altos

### H1) Falta de uniformidad en dirty-guard entre formularios críticos
- Se implementó guardado de cambios en clientes/proveedores/sucursales/depósitos, pero no está aplicado de forma transversal en otros formularios operativos (por ejemplo compras, ventas completas, etc.).
- Riesgo: experiencia inconsistente y pérdida de cambios en pantallas no cubiertas.
- Recomendación: abstraer patrón en componente base/reusable.

### H2) Validaciones cliente-side heterogéneas y manuales
- Las validaciones actuales son ad-hoc en cada página (`ValidateEditor`), sin un esquema común ni DataAnnotations reutilizable.
- Riesgo: divergencias de reglas y costo de mantenimiento.
- Recomendación: estandarizar con `EditForm`, modelos de validación y helper común para mensajes.

### H3) ApiClient parsea error envelope, pero no modela validaciones por campo
- Se captura mensaje global y código, pero no hay soporte para errores de campo (e.g. diccionario campo→error).
- Riesgo: formularios con feedback menos preciso.
- Recomendación: extender contrato de error para field-level validation y render por campo.

## Hallazgos medios

### M1) Mezcla de responsabilidades UI/negocio en páginas Razor
- Las páginas concentran carga, validación, mapping DTO, manejo de errores y estado visual.
- Recomendación: mover lógica a servicios/view-models para testabilidad.

### M2) Cobertura de tests automatizados no verificable en este entorno
- No fue posible ejecutar test suite por falta de SDK.
- Recomendación: reforzar CI con build + unit + integración + smoke UI.

### M3) Falta de telemetría funcional en errores UX
- No hay evidencia de eventos para ratio de error por formulario o abandono por validación.
- Recomendación: instrumentar eventos de intento/falla/éxito.

## Backlog recomendado (prioridad)

1. **Seguridad credenciales (inmediato)**
   - Quitar exposición de password temporal en mensajes estándar.
   - Migrar generación de passwords a CSPRNG.
   - Eliminar default `Admin123$` en entornos no-dev.

2. **Consistencia de formularios (corto plazo)**
   - Crear componente base de formulario con: `FormFeedback`, `_saving`, dirty guard y pipeline de validación.
   - Unificar validaciones con DataAnnotations/FluentValidation compartidas entre UI y backend.

3. **Errores por campo (corto/medio)**
   - Extender `ApiClientException` para map de errores de campo.
   - Renderizar mensajes por control con UX estándar.

4. **Calidad y observabilidad (medio)**
   - Pipeline CI obligatorio con build + tests.
   - Métricas UX (abandono, error rate, tiempo por flujo).

## Conclusión
El sistema avanzó en robustez UX (errores visibles, anti-doble submit, guardas de navegación) y mitigó inconsistencias operativas críticas. El siguiente salto de madurez requiere cerrar deuda de **seguridad de credenciales**, **estandarización transversal de formularios** y **observabilidad**.

## Estado de remediación (actualización)
- Se eliminó la exposición de contraseña temporal en mensaje de éxito de alta de usuarios.
- Se migró la generación de contraseñas temporales/seed a RNG criptográfico.
- Se removió el fallback fijo `Admin123$`; ahora se genera credencial aleatoria si no se define `Seed:AdminPassword`.
