# Product Backlog

## 🔥 Críticos
- ✅ Unificar contrato de errores API/UI con taxonomía estable (mapeo automático de `AppResult` fallido a `ProblemDetails` + status HTTP).
- ✅ Incorporar smoke de flujo crítico end-to-end de comercio (presupuesto → venta → factura) en test de integración.
- ✅ Hardening de seguridad operativa en credenciales seed (política mínima en no-Development y bloqueo de log de passwords generadas fuera de Development).

## ⚡ Quick Wins
- ✅ Estandarizar validaciones de formularios con modelo compartido para evitar reglas duplicadas y divergentes.
- ✅ Añadir tests de contrato API (status codes + schema + error envelope) para asegurar consistencia externa.

## 🧠 Producto
- ✅ Instrumentar telemetría funcional por flujo y por error de validación (abandono, error rate, latencia percibida).
- ✅ Definir guía única de mensajes operativos y códigos de error de negocio para soporte y operación.
- ✅ Crear tablero de salud técnica/producto (build, tests, cobertura, deuda, p95) para priorización continua.

## 🧱 Técnica
- ✅ Descomponer páginas Razor de alto tamaño en componentes y view models testeables.
- ✅ Reducir tamaño de módulos Application (features) por bounded context interno para bajar complejidad.
- ✅ Separar `CommerceController` en controladores por dominio funcional manteniendo compatibilidad de rutas.

## 🎯 UX
- ✅ Aplicar dirty-guard transversal en todos los formularios críticos para evitar pérdida de cambios.
- ✅ Normalizar estados UI en todas las grillas y formularios (loading/empty/error/success/disabled).

## 🚀 Performance
- ✅ Expandir budgets automatizados de performance a `sales`, `quotes`, `purchases` y `categories`.
- ✅ Optimizar serialización/payload en listados de mayor cardinalidad para reducir latencia y consumo.
