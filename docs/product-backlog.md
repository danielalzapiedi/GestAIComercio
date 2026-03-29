# Product Backlog

## 🔥 Críticos
- **Unificar contrato de errores frontend/backend por código estable** (incluyendo exposición y uso consistente de `errorCode` + `correlationId` en toda la UX operativa).
- **Plan de pruebas de regresión por módulo (funcional + permisos)**, priorizando escenarios negativos, de borde y control de accesos.
- **Endurecer estrategia de migraciones/seed en startup por entorno** para reducir riesgo operativo en despliegues no controlados.

## ⚡ Quick Wins
- **Homologar estados UI en todos los formularios de commerce**, reutilizando el mismo patrón de feedback y operación.

## 🧠 Producto
- **Observabilidad funcional mínima por flujo** para priorizar backlog con evidencia real de errores/incidentes.
- **Mejorar accesibilidad y feedback de acciones críticas** con mensajes accionables y comportamiento consistente.

## 🧱 Técnica
- **Particionar `CommerceController` por dominio funcional** para reducir acoplamiento y mejorar mantenibilidad.
- **Dividir archivos `*Features.cs` en slices por caso de uso** para bajar complejidad y riesgo de regresión colateral.
- **Definir quality gates automáticos** (cobertura mínima, complejidad y pruebas smoke obligatorias).

## 🎯 UX
- **Estandarizar validaciones visibles por campo + resumen global** con criterios de interacción homogéneos.

## 🚀 Performance
- **Optimización de queries críticas y baseline p95 por endpoint** en módulos de mayor uso transaccional.
- **Estrategia de pruebas de performance básica en CI** para detectar degradación de forma temprana.
