# Product Backlog

## 🔥 Pendiente prioritario
- Sin ítems críticos pendientes.

## 🧠 Producto
- Sin ítems de producto pendientes.

## 🧱 Técnica
- Refactorizar `CommerceFeatures` en submódulos por bounded context para bajar complejidad y facilitar mantenimiento.
- Reducir lógica de negocio incrustada en páginas Razor moviendo reglas y orquestación a servicios/view-models testeables.

## 🚀 Performance
- Medir y publicar resultados reales de p95/payload por endpoint crítico para validar baseline en entorno integrado.

## ✅ Cerrado
- Estandarizar manejo de errores en formularios críticos (ventas, productos, categorías, compras).
- Endurecer CORS por entorno y dominio explícito en API.
- Reducir acoplamiento crítico con separación de responsabilidades en controller/handlers.
- Extender `ApiClientException` con errores por campo.
- Definir checklist QA de regresión de comercio.
- Estandarizar validaciones visuales + dirty guard + estados de interfaz en pantallas transaccionales.
- Optimizar consultas con subqueries repetidas en listados críticos.
- Definir baseline inicial de performance (objetivos p95/payload y estrategia de medición).
- Homogeneizar mensajes de error/éxito en pantallas maestras pendientes (`branches`, `warehouses`, `customers`, `suppliers`) con `FormFeedback`.
- Ajustar límite máximo de paginación en backend para endpoints de alto tráfico (cap global de `PageSize` a 50).
- Formalizar política única de pricing entre venta rápida y venta estándar con regla explícita de catálogo vs override.
- Estandarizar comportamiento de filtros/búsqueda en listados maestros con acciones consistentes de buscar/limpiar.
- Incorporar observabilidad funcional mínima en módulos maestros (load/search/save/toggle) con telemetría de resultado.
