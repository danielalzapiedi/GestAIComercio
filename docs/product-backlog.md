# Product Backlog

## 🔥 Críticos
- Estandarizar el manejo de errores en formularios críticos (ventas, productos, categorías, compras) para evitar guardados silenciosos y mejorar la confiabilidad operativa.
- Endurecer CORS por entorno y dominio explícito en API (eliminar configuración abierta global en ambientes productivos).
- Reducir el riesgo de regresiones separando responsabilidades en componentes de alto acoplamiento (controlador de comercio y casos de uso concentrados).

## ⚡ Quick Wins
- Extender `ApiClientException` para exponer errores por campo y permitir feedback preciso en formularios.
- Definir checklist QA de regresión por módulo comercial (smoke funcional mínimo por flujo crítico).
- Homogeneizar mensajes de error y éxito en las pantallas que hoy no usan patrón consistente de feedback.

## 🧠 Producto
- Formalizar la política única de pricing para venta rápida y venta completa (precio catálogo vs override permitido).
- Estandarizar comportamiento de filtros y búsqueda en todos los listados para reducir fricción operativa.
- Incorporar observabilidad funcional (eventos de error por formulario, abandono y tiempos de finalización por flujo).

## 🧱 Técnica
- Refactorizar `CommerceFeatures` en submódulos por bounded context para bajar complejidad y facilitar mantenimiento.
- Descomponer `CommerceController` en controladores por dominio funcional para mejorar ownership y trazabilidad.
- Mejorar trazabilidad de errores con contrato uniforme de error, códigos estables y correlation-id.
- Reducir lógica de negocio incrustada en páginas Razor moviendo reglas y orquestación a servicios/view-models testeables.

## 🎯 UX
- Estandarizar validaciones visuales (por campo + resumen) en formularios críticos usando un patrón único.
- Implementar guardado seguro con protección de cambios sin guardar (dirty form guard) en editores operativos.
- Unificar estados de interfaz (loading, empty, error, success, disabled) en pantallas transaccionales.

## 🚀 Performance
- Optimizar consultas con subqueries repetidas en proyecciones de listados para mejorar escalabilidad.
- Definir baseline de performance (p95 por endpoint crítico, presupuesto de payload y tiempos objetivo).
- Revisar paginación y tamaño de respuesta en endpoints de alto tráfico para prevenir degradación temprana.
