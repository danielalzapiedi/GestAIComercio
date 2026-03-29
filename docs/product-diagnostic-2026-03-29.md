# Diagnóstico de producto — 2026-03-29

## 0) Contexto de trabajo
- **Modo de gestión:** Diagnóstico de producto continuo (sin releases activas).
- **Objetivo de este entregable:** Evaluar el estado actual del sistema y proponer backlog evolutivo priorizado, sin implementar cambios.
- **Equipo simulado:** product-manager, analyst, ux, architect, developer, qa.

---

## 1) Estado general del producto (alto nivel)

### Resumen ejecutivo
El producto se encuentra **funcionalmente avanzado y utilizable** para operación comercial diaria, con un backend robusto en validaciones de comandos/queries y una base de UI que ya incorpora componentes comunes de feedback y estado en varios flujos.

Sin embargo, el diagnóstico detecta una etapa típica de “crecimiento acelerado”: hay señales claras de **fragmentación UX**, **concentración de lógica en archivos muy grandes**, **cobertura de pruebas insuficiente para regresión amplia** y **riesgos de performance** en consultas y carga de datos que hoy funcionan, pero pueden degradar con volumen real.

### Semáforo general
- **Calidad funcional:** 🟡 Buena, con riesgos puntuales por inconsistencias entre pantallas.
- **Experiencia de usuario:** 🟡 Aceptable, pero no homogénea entre módulos.
- **Validaciones:** 🟢 Sólidas en backend (pipeline + middleware uniforme).
- **Consistencia del sistema:** 🟡 Parcial; conviven patrones nuevos y legacy.
- **Arquitectura:** 🟡 Correcta en capas, con deuda por tamaño/composición de features.
- **Código:** 🟡 Mantenible en partes, con focos de alta complejidad.
- **Testing:** 🟡 Smoke y contratos básicos presentes, cobertura limitada para edge cases.
- **Riesgo de regresión:** 🔴 Medio/alto en módulos de comercio por acoplamiento.
- **Performance básica:** 🟡 Correcta en baseline, sin budget ni métricas p95 formalizadas.

---

## 2) Hallazgos por rol

## 2.1 Product-Manager (ex release-manager)
- El repositorio ya declara explícitamente modo continuo sin releases activas, alineado con la nueva forma de trabajo.
- Hay foco en correcciones operativas recientes, pero falta institucionalizar un **marco de priorización vivo** (severidad, impacto, riesgo, owner y fecha objetivo por ítem).
- Se recomienda formalizar una **cadencia de saneamiento** (ej. semanal) separando: bugs críticos, quick wins UX, deuda técnica y optimización.

## 2.2 Analyst
- Flujos críticos (cotización → venta → factura, compras, stock, caja) están implementados, incluyendo smoke de proceso de punta a punta.
- Se observan diferencias de comportamiento entre páginas “homologadas” y otras que usan patrones distintos de mensajes/estados.
- Riesgo funcional: la variabilidad de interacción puede causar errores operativos del usuario final aunque el backend valide correctamente.

## 2.3 UX
- Existen componentes de estado reutilizables (`FormFeedback`, `OperationalStateHint`) ya aplicados en múltiples pantallas, lo cual es una base positiva.
- Persisten vistas con alertas ad-hoc y manejo manual de estado, generando inconsistencia visual y de percepción de resultado.
- Falta estandarización transversal de estados `loading/empty/error/success/disabled` en todos los módulos con reglas únicas de interacción.

## 2.4 Architect
- La arquitectura por capas y mediación CQRS está bien trazada.
- Hay concentración de reglas de dominio en archivos monolíticos (`*Features.cs`) que superan ampliamente tamaño recomendable y elevan costo de cambio.
- Hay oportunidad de segmentación por subdominios (ventas, compras, inventario, cuentas corrientes, fiscal) con handlers y validadores más pequeños para reducir acoplamiento y regresiones.

## 2.5 Developer
- Señal de deuda de código: archivos de alto tamaño y alta densidad de responsabilidades.
- El controlador de commerce concentra gran cantidad de endpoints; aunque usa `partial`, el módulo sigue siendo un “punto caliente” de mantenimiento.
- Faltan guardrails de calidad automáticos (p. ej. umbral de complejidad/cobertura por PR) para prevenir crecimiento de deuda.

## 2.6 QA
- Hay pruebas de contratos HTTP y pruebas de integración relevantes para flujos críticos.
- La suite actual cubre happy paths y reglas puntuales, pero faltan regresiones sistemáticas para: permisos por módulo/rol, errores de validación UI/API, concurrencia y escenarios de datos voluminosos.
- Se recomienda matriz de regresión mínima por módulo con checklists ejecutables y evidencia trazable.

---

## 3) Backlog evolutivo priorizado

> Leyenda de clasificación: 🔥 Crítica · ⚡ Quick win · 🧠 Mejora de producto · 🧱 Deuda técnica · 🎯 UX · 🚀 Performance

### B-01 — Homologar estados UI en todos los formularios de commerce
- **Clasificación:** 🎯 UX / ⚡ Quick win
- **Problema actual:** Coexisten pantallas con `OperationalStateHint` y otras con alertas manuales, generando experiencia inconsistente.
- **Impacto:** Reduce errores de operación y mejora predictibilidad del usuario.
- **Riesgo:** Bajo.
- **Esfuerzo:** Medio.
- **Prioridad:** Alta.

### B-02 — Unificar contrato de errores frontend/backend por código estable
- **Clasificación:** 🔥 Crítica
- **Problema actual:** Hay mapping de errores en middleware/filtro, pero la presentación UI no siempre explota `errorCode`/`correlationId` de forma consistente.
- **Impacto:** Mejora soporte, trazabilidad y resolución de incidentes.
- **Riesgo:** Medio.
- **Esfuerzo:** Medio.
- **Prioridad:** Alta.

### B-03 — Particionar `CommerceController` por dominio funcional
- **Clasificación:** 🧱 Deuda técnica
- **Problema actual:** Un único controller concentra muchos endpoints de dominios distintos.
- **Impacto:** Reduce acoplamiento, facilita ownership y pruebas.
- **Riesgo:** Medio (routing/contratos).
- **Esfuerzo:** Alto.
- **Prioridad:** Alta.

### B-04 — Dividir archivos `*Features.cs` de gran tamaño en slices por caso de uso
- **Clasificación:** 🧱 Deuda técnica
- **Problema actual:** Archivos muy extensos incrementan complejidad y riesgo de regresión colateral.
- **Impacto:** Mejora mantenibilidad, onboarding y velocidad de cambio.
- **Riesgo:** Medio.
- **Esfuerzo:** Alto.
- **Prioridad:** Alta.

### B-05 — Plan de pruebas de regresión por módulo (funcional + permisos)
- **Clasificación:** 🔥 Crítica
- **Problema actual:** Cobertura limitada en escenarios negativos y de borde.
- **Impacto:** Disminuye incidentes en producción y reprocesos.
- **Riesgo:** Bajo.
- **Esfuerzo:** Medio.
- **Prioridad:** Alta.

### B-06 — Observabilidad funcional mínima (dashboard de errores por flujo)
- **Clasificación:** 🧠 Mejora de producto
- **Problema actual:** Existen señales de salud, pero falta disciplina de uso para priorizar backlog con datos reales.
- **Impacto:** Priorización basada en evidencia, no percepción.
- **Riesgo:** Bajo.
- **Esfuerzo:** Medio.
- **Prioridad:** Media.

### B-07 — Optimización de queries críticas y baseline p95 por endpoint
- **Clasificación:** 🚀 Performance
- **Problema actual:** Varias consultas de listados/reportes pueden escalar mal con volumen (joins/includes múltiples, cargas amplias).
- **Impacto:** Mejor tiempo de respuesta y experiencia operativa.
- **Riesgo:** Medio.
- **Esfuerzo:** Alto.
- **Prioridad:** Alta.

### B-08 — Estandarizar validaciones visibles por campo + resumen global
- **Clasificación:** 🎯 UX
- **Problema actual:** Backend valida bien, pero el feedback visual puede variar entre pantallas.
- **Impacto:** Menos frustración y menor tasa de reintento incorrecto.
- **Riesgo:** Bajo.
- **Esfuerzo:** Medio.
- **Prioridad:** Alta.

### B-09 — Endurecer estrategia de migraciones/seed en startup por entorno
- **Clasificación:** 🧱 Deuda técnica / 🔥 Crítica
- **Problema actual:** La app ejecuta migración + seed al iniciar; correcto en dev, riesgoso en despliegues no controlados.
- **Impacto:** Reduce riesgo operativo y tiempos de arranque en producción.
- **Riesgo:** Alto (si se deja como está en topologías complejas).
- **Esfuerzo:** Medio.
- **Prioridad:** Alta.

### B-10 — Definir quality gates (cobertura mínima, complejidad, pruebas smoke obligatorias)
- **Clasificación:** 🧱 Deuda técnica
- **Problema actual:** Faltan umbrales automáticos para evitar regresión de calidad estructural.
- **Impacto:** Mejora sostenibilidad y previene deuda futura.
- **Riesgo:** Bajo.
- **Esfuerzo:** Medio.
- **Prioridad:** Media.

### B-11 — Mejorar accesibilidad y feedback de acciones críticas
- **Clasificación:** 🎯 UX / 🧠 Mejora de producto
- **Problema actual:** No todas las acciones críticas tienen confirmaciones, foco accesible o mensajes accionables uniformes.
- **Impacto:** Menos errores humanos y mejor experiencia inclusiva.
- **Riesgo:** Bajo.
- **Esfuerzo:** Medio.
- **Prioridad:** Media.

### B-12 — Estrategia de pruebas de performance básica en CI (smoke de carga)
- **Clasificación:** 🚀 Performance
- **Problema actual:** Sin chequeo recurrente de degradación, los cambios pueden erosionar tiempos de respuesta gradualmente.
- **Impacto:** Detección temprana de cuellos de botella.
- **Riesgo:** Bajo.
- **Esfuerzo:** Medio.
- **Prioridad:** Media.

---

## 4) Top 10 mejoras recomendadas
1. B-02 — Unificar contrato de errores frontend/backend por código estable.  
2. B-05 — Plan de pruebas de regresión por módulo (funcional + permisos).  
3. B-01 — Homologar estados UI en todos los formularios de commerce.  
4. B-08 — Estandarizar validaciones visibles por campo + resumen global.  
5. B-07 — Optimización de queries críticas y baseline p95 por endpoint.  
6. B-04 — Dividir archivos `*Features.cs` en slices por caso de uso.  
7. B-03 — Particionar `CommerceController` por dominio funcional.  
8. B-09 — Endurecer estrategia de migraciones/seed en startup por entorno.  
9. B-10 — Definir quality gates automáticos de calidad.  
10. B-06 — Observabilidad funcional mínima orientada a priorización.  

---

## 5) Riesgos críticos a corregir primero

### RC-1 — Regresión funcional por cobertura insuficiente en escenarios negativos
- **Riesgo:** Alto.
- **Por qué primero:** Impacta directamente continuidad operativa y confianza del negocio.

### RC-2 — Inconsistencia UX en flujos transaccionales
- **Riesgo:** Alto.
- **Por qué primero:** Genera errores de uso, tickets de soporte y pérdida de productividad.

### RC-3 — Complejidad estructural concentrada en módulos calientes
- **Riesgo:** Medio/alto.
- **Por qué primero:** Cada cambio futuro será más caro y riesgoso.

### RC-4 — Degradación de performance sin métricas de control
- **Riesgo:** Medio/alto.
- **Por qué primero:** El problema aparece tarde y cuesta más corregirlo en producción.

---

## 6) Propuesta de siguiente paso (sin implementación)
1. Acordar top 10 y validar prioridades con negocio/soporte.
2. Transformar cada ítem en ticket con criterios de aceptación y owner.
3. Ejecutar primero bloque de contención: **B-02 + B-05 + B-01 + B-08**.
4. En paralelo, plan técnico para **B-04/B-03** con rollout incremental para minimizar riesgo.
