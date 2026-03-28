# Product Backlog

## 🔥 Pendiente prioritario
- Homogeneizar mensajes de error y éxito en las pantallas que aún no usan patrón consistente de feedback.
- Revisar paginación y tamaño de respuesta en endpoints de alto tráfico para prevenir degradación temprana.

## 🧠 Producto
- Formalizar la política única de pricing para venta rápida y venta completa (precio catálogo vs override permitido).
- Estandarizar comportamiento de filtros y búsqueda en todos los listados para reducir fricción operativa.
- Incorporar observabilidad funcional (eventos de error por formulario, abandono y tiempos de finalización por flujo).

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
