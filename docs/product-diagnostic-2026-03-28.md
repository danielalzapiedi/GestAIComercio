# Diagnóstico de Producto — GestAI Comercio

Fecha: 2026-03-28  
Modo: **Diagnóstico de producto (sin implementación)**

## 0) Alcance y método

### Objetivo
Evaluar el estado actual del sistema y proponer un backlog evolutivo priorizado.

### Equipo simulado (roles)
- product-manager (ex release-manager)
- analyst
- ux
- architect
- developer
- qa

### Evidencia revisada
- Documentación funcional/técnica vigente (`docs/ai-context.md`, `docs/roadmap.md`, `docs/current-release.md`, `docs/performance-baseline.md`, `docs/qa-regression-checklist.md`, `docs/product-backlog.md`).
- Capa API (controladores + middleware).
- Capa Application (features, behaviors, contratos de resultado).
- Capa Web Blazor (pantallas transaccionales principales).
- Suite de tests (`GestAI.Tests`).

### Limitaciones del diagnóstico
- No se pudo ejecutar build/test local porque el entorno no tiene SDK .NET (`dotnet: command not found`).

---

## 1) Estado general del producto (alto nivel)

### Lectura ejecutiva
El producto está **funcionalmente avanzado**: cubre maestros, circuito comercial (presupuestos/ventas/compras), inventario, caja, cuenta corriente, fiscal y documentos PDF. La cobertura funcional de negocio es amplia y ya existe un patrón UX más consistente que en iteraciones anteriores.

### Nivel de madurez por dimensión
- **Cobertura funcional:** Alta.
- **Consistencia UX/UI:** Media-Alta (mejoró en formularios críticos, pero no totalmente transversal).
- **Validaciones y errores:** Media (backend robusto con `FluentValidation`, frontend todavía heterogéneo en validación manual).
- **Arquitectura:** Media (Clean + CQRS está presente, pero con archivos “feature” y páginas Razor muy grandes).
- **Testing:** Media (muchos tests de integración de dominio, poca evidencia de tests UI/e2e y límites operativos en CI no confirmados en este entorno).
- **Performance base:** Media (hay baseline + budget automatizado para products, cobertura incompleta para otros endpoints críticos).
- **Riesgo de regresión:** Medio-Alto (por acoplamiento en pantallas grandes y módulos Application monolíticos).

---

## 2) Hallazgos por rol

## 2.1 product-manager
- El backlog actual aparece prácticamente vacío, pero hay señales de **trabajo estructural aún pendiente** (consistencia transversal, performance multi-endpoint, deuda de modularidad).
- La documentación sigue orientada a releases; el producto requiere pasar a un esquema de **roadmap continuo por capacidades** (con épicas, objetivos trimestrales y KPIs).
- Falta tablero explícito de métricas de calidad producto: tiempo de tarea, tasa de error por flujo, abandono de formularios, deuda técnica priorizada por impacto.

## 2.2 analyst
- El sistema cubre procesos clave, pero persisten divergencias de comportamiento entre pantallas similares (paginación, mensajes, validaciones, descarte de cambios).
- La semántica de errores mezcla `AppResult.Fail(...)` (200 OK + failure semántico) con `ProblemDetails` (4xx/5xx), lo que complica criterios funcionales homogéneos.
- Hay oportunidad de formalizar reglas de negocio comunes (pricing, estado documental, transiciones válidas, límites operativos) como contratos verificables y reutilizables.

## 2.3 ux
- Se consolidó uso de componentes de feedback (`FormFeedback`, `EmptyState`), estados visuales y bloques de lectura rápida.
- Persisten patrones mixtos de “dirty guard”: algunas pantallas usan snapshot robusto y otras un confirm genérico en salida.
- Varias pantallas tienen alta densidad visual/operativa en un solo formulario; potencial de fatiga cognitiva y mayor error humano en back-office.
- Falta estandarización de validación por campo en frontend (no solo mensaje global), especialmente en entradas numéricas y listas de ítems.

## 2.4 architect
- La arquitectura base está bien encaminada (CQRS + MediatR + capas), pero la implementación presenta concentración de responsabilidad:
  - `CommerceController` parcial aún centraliza mucho alcance de dominio.
  - Features de Application con archivos de 600-1000 líneas.
  - Pages Razor con lógica UI + estado + mapping + validación + flujo API en el mismo archivo.
- El contrato de error está mejorado con `ValidationProblemDetails` y correlation-id, pero falta uniformidad total para clientes y endpoints legacy.
- Buen avance en performance de productos; falta sistematizar budgets para todo el circuito transaccional crítico.

## 2.5 developer
- Código funcional y relativamente consistente en naming, pero con deuda de mantenibilidad por tamaño y mezcla de responsabilidades.
- Persisten validaciones manuales duplicadas por pantalla (`ValidateEditor` y variantes), dificultando evolución segura.
- El modelo `AppResult` con resultados semánticos sobre 200 OK obliga a lógica defensiva en frontend y complica telemetría de errores reales HTTP.
- Riesgo de regresión alto en archivos grandes donde un cambio menor afecta múltiples escenarios.

## 2.6 qa
- La suite de `GestAI.Tests` cubre múltiples escenarios funcionales de negocio e incluye budget p95/payload para products.
- No hay evidencia en este diagnóstico de ejecución real de build/test por limitación de entorno (sin SDK).
- El checklist QA de regresión existe, pero no aparece una señal de automatización/ejecución continua de ese checklist en CI.
- Riesgo principal QA: cobertura fuerte en aplicación/dominio, pero menor visibilidad de regresiones UX end-to-end (navegación, estados UI, interacción real de usuario).

---

## 3) Backlog de mejoras priorizado

> Formato por ítem: **título · tipo · problema actual · impacto · riesgo · esfuerzo · prioridad**

1. **Unificar contrato de errores API/UI con taxonomía estable**  
   - Tipo: 🔥 Crítica  
   - Problema actual: coexisten errores por `AppResult` (éxito HTTP) y `ProblemDetails` (error HTTP), con consumo heterogéneo en frontend.  
   - Impacto: reduce ambigüedad operativa, mejora diagnósticos y soporte.  
   - Riesgo: alto si se cambia sin estrategia de compatibilidad.  
   - Esfuerzo: Alto.  
   - Prioridad: Alta.

2. **Estandarizar validaciones de formularios con modelo compartido**  
   - Tipo: ⚡ Quick win  
   - Problema actual: validaciones manuales duplicadas por pantalla, inconsistentes por campo.  
   - Impacto: menos errores de usuario, menor deuda de mantenimiento.  
   - Riesgo: medio (impacta varias vistas).  
   - Esfuerzo: Medio.  
   - Prioridad: Alta.

3. **Aplicar dirty-guard transversal en todos los formularios críticos**  
   - Tipo: 🎯 UX  
   - Problema actual: patrón de “cambios sin guardar” no uniforme entre pantallas.  
   - Impacto: evita pérdida de datos y tickets de soporte.  
   - Riesgo: bajo-medio.  
   - Esfuerzo: Medio.  
   - Prioridad: Alta.

4. **Descomponer páginas Razor de alto tamaño a componentes + view models**  
   - Tipo: 🧱 Deuda técnica  
   - Problema actual: páginas de 400-650 líneas con lógica de negocio UI acoplada.  
   - Impacto: mejora testabilidad, legibilidad y velocidad de cambios.  
   - Riesgo: medio-alto por regresión visual si no se hace incrementalmente.  
   - Esfuerzo: Alto.  
   - Prioridad: Alta.

5. **Reducir tamaño de módulos Application (features) por bounded context interno**  
   - Tipo: 🧱 Deuda técnica  
   - Problema actual: archivos de features de hasta ~1000 líneas concentran múltiples casos de uso.  
   - Impacto: baja complejidad ciclomática y acelera onboarding técnico.  
   - Riesgo: medio.  
   - Esfuerzo: Alto.  
   - Prioridad: Alta.

6. **Expandir budget de performance a sales/quotes/purchases/categories**  
   - Tipo: 🚀 Performance  
   - Problema actual: budget automatizado fuerte en products, cobertura incompleta en otros endpoints críticos.  
   - Impacto: detección temprana de degradación en circuitos más sensibles.  
   - Riesgo: bajo.  
   - Esfuerzo: Medio.  
   - Prioridad: Alta.

7. **Incorporar smoke e2e de UX crítica (crear/editar/guardar/cancelar)**  
   - Tipo: 🔥 Crítica  
   - Problema actual: cobertura fuerte de integración backend, baja evidencia de recorrido UI real automatizado.  
   - Impacto: reduce regresiones funcionales visibles por usuario.  
   - Riesgo: medio (infra de testing).  
   - Esfuerzo: Alto.  
   - Prioridad: Alta.

8. **Instrumentar telemetría funcional por flujo y error de validación**  
   - Tipo: 🧠 Mejora de producto  
   - Problema actual: falta panel de métricas operativas UX (abandono, error por campo, latencia percibida).  
   - Impacto: permite priorizar por impacto real y no por intuición.  
   - Riesgo: bajo.  
   - Esfuerzo: Medio.  
   - Prioridad: Media.

9. **Normalizar estados UI en todas las grillas (loading/empty/error/success/disabled)**  
   - Tipo: 🎯 UX  
   - Problema actual: gran avance, pero todavía hay variaciones de comportamiento y microcopy entre módulos.  
   - Impacto: experiencia predecible y menos entrenamiento interno.  
   - Riesgo: bajo.  
   - Esfuerzo: Medio.  
   - Prioridad: Media.

10. **Definir guía única de mensajes operativos y códigos de error de negocio**  
    - Tipo: 🧠 Mejora de producto  
    - Problema actual: mensajes de éxito/error pueden variar por pantalla/caso de uso.  
    - Impacto: mejor soporte, menor ambigüedad para usuarios no técnicos.  
    - Riesgo: bajo.  
    - Esfuerzo: Bajo.  
    - Prioridad: Media.

11. **Separar `CommerceController` por dominios API independientes**  
    - Tipo: 🧱 Deuda técnica  
    - Problema actual: controlador parcial extenso con gran superficie de rutas.  
    - Impacto: despliegue y ownership más claros, menor riesgo de colisión en cambios.  
    - Riesgo: medio (ruteo/compatibilidad).  
    - Esfuerzo: Alto.  
    - Prioridad: Media.

12. **Añadir tests de contrato API (status codes + schema + error envelope)**  
    - Tipo: ⚡ Quick win  
    - Problema actual: muchos tests de handlers, menor verificación sistemática del contrato HTTP externo.  
    - Impacto: mayor confianza en clientes web/mobile e integraciones externas.  
    - Riesgo: bajo.  
    - Esfuerzo: Medio.  
    - Prioridad: Alta.

13. **Optimizar serialización/payload en listados con mayor cardinalidad**  
    - Tipo: 🚀 Performance  
    - Problema actual: sin budgets formalizados para varias consultas pesadas; riesgo de payload creciente.  
    - Impacto: mejor tiempo de respuesta y costo de infraestructura.  
    - Riesgo: medio.  
    - Esfuerzo: Medio.  
    - Prioridad: Media.

14. **Crear tablero de salud técnica (build, tests, cobertura, deuda, p95)**  
    - Tipo: 🧠 Mejora de producto  
    - Problema actual: visibilidad distribuida en docs, sin panel consolidado de decisión.  
    - Impacto: priorización basada en datos para PM + ingeniería.  
    - Riesgo: bajo.  
    - Esfuerzo: Bajo-Medio.  
    - Prioridad: Media.

15. **Hardening de seguridad operativa en configuraciones sensibles (fiscal, seed, secretos)**  
    - Tipo: 🔥 Crítica  
    - Problema actual: se avanzó en contraseñas seed, pero falta checklist continuo de secretos/certificados por entorno.  
    - Impacto: reduce exposición operativa y fallos de cumplimiento.  
    - Riesgo: alto.  
    - Esfuerzo: Medio.  
    - Prioridad: Alta.

---

## 4) Top 10 mejoras recomendadas

Orden sugerido para las próximas iteraciones:
1. Unificar contrato de errores API/UI con taxonomía estable. (🔥)
2. Incorporar smoke e2e de UX crítica. (🔥)
3. Estandarizar validaciones de formularios con modelo compartido. (⚡)
4. Aplicar dirty-guard transversal en formularios críticos. (🎯)
5. Expandir budgets de performance a sales/quotes/purchases/categories. (🚀)
6. Añadir tests de contrato API (status codes + schema). (⚡)
7. Descomponer páginas Razor grandes en componentes + view models. (🧱)
8. Reducir tamaño de features Application por dominio interno. (🧱)
9. Instrumentar telemetría funcional por flujo y error de validación. (🧠)
10. Hardening continuo de seguridad operativa en configuración sensible. (🔥)

---

## 5) Riesgos críticos a corregir primero

1. **Inconsistencia de contrato de errores (HTTP vs AppResult)**: alto riesgo de bugs silenciosos y soporte costoso en UI/integraciones.
2. **Ausencia de e2e crítico automatizado**: riesgo de regresiones visibles por usuario final pese a tests de dominio verdes.
3. **Acoplamiento alto en pantallas y features extensos**: aumenta probabilidad de regresión por efecto colateral.
4. **Cobertura incompleta de budgets de performance**: degradaciones en ventas/presupuestos/compras pueden pasar sin alarma temprana.
5. **Falta de observabilidad funcional orientada a producto**: dificulta priorizar con evidencia real de impacto.

---

## Recomendación de operación inmediata (sin implementar todavía)

- Validar este backlog en sesión conjunta PM + Arquitectura + QA.
- Marcar “Must do now / Next / Later” para 8 semanas.
- Definir KPIs de seguimiento quincenal:
  - tasa de error por flujo,
  - p95 por endpoint crítico,
  - tasa de regresión por sprint,
  - lead time de corrección en incidencias críticas.
