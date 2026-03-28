# Current Release Status

## Release actual
- **ID:** R1 — Hardening UX + Calidad Operativa
- **Estado:** En progreso
- **Fecha de actualización:** 2026-03-28

## Validación de pertenencia de tarea
- **Tarea aplicada:** Mejoras de performance del backlog (optimización de subqueries repetidas y reducción de costo en listados paginados).
- **¿Pertenece a la release actual?** Sí, como bloque adelantado de estabilización técnica dentro de R1 para disminuir riesgo de degradación temprana.

## Trabajo realizado en la release
- Implementadas validaciones visuales resumidas previo al guardado en formularios críticos.
- Implementada protección ante pérdida de cambios (dirty form guard + confirmación).
- Unificado manejo de estados de error de carga en vistas transaccionales.
- Extendidos los patrones UX a `Products` y `Categories` para completar el set crítico definido en backlog.
- Optimizados listados críticos para evitar subconsultas duplicadas por fila en tenant/products.
- Depurado `product-backlog.md` para remover del backlog activo los ítems ya implementados/cerrados.

## Pendiente para cerrar release
- Ejecutar y dejar en verde build + tests en pipeline CI.
- Completar checklist de regresión QA de comercio.
- Documentar evidencia final de aceptación funcional y UX.
- Medir p95 y payload por endpoint crítico para validar impacto real de performance.

## Próximo paso recomendado
- Consolidar QA de regresión en CI y cerrar R1 con checklist firmado.
