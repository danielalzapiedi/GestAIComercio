# Tablero de salud técnica y de producto

Fecha: 2026-03-28

## Objetivo
Consolidar métricas mínimas para seguimiento operativo continuo.

## Vista en aplicación
- Ruta UI: `/commerce/product-health`
- Fuente: telemetría funcional en memoria (`ProductTelemetryService`).
- Muestra:
  - eventos totales 24h,
  - errores 24h,
  - validaciones fallidas,
  - resumen por flow,
  - top errores de validación.

## KPIs técnicos recomendados (fuente CI)
- Build verde por rama principal.
- `dotnet test` verde y regresiones por sprint.
- Cobertura por capa (Application / API / Web).
- Deuda técnica priorizada por impacto/riesgo.
- p95 y payload en endpoints críticos (`products`, `sales`, `quotes`, `purchases`, `categories`).

## Cadencia sugerida
- Revisión diaria de errores.
- Revisión semanal de KPIs técnicos.
- Revisión quincenal de roadmap de mejoras.
