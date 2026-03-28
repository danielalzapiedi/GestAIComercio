# Roadmap de Producto (2026)

## Release R1 — Hardening UX + Calidad Operativa (Activa)
**Ventana objetivo:** Marzo 2026  
**Objetivo:** Reducir errores operativos en flujos comerciales críticos y estandarizar la experiencia de formularios.

### Alcance
- Estandarizar validaciones visuales (por campo + resumen) en formularios críticos.
- Incorporar guardado seguro con detección de cambios sin guardar.
- Unificar estados transaccionales: loading / empty / error / success / disabled.
- Homogeneizar feedback de error/éxito en pantallas de ventas, presupuestos y compras.

### Criterios de salida
- Build de solución en verde.
- Tests automáticos en verde.
- Evidencia de QA de regresión mínima en comercio.

---

## Release R2 — Seguridad API + Observabilidad
**Ventana objetivo:** Abril 2026  
**Objetivo:** Endurecer seguridad de exposición API y mejorar trazabilidad operativa.

### Alcance previsto
- Endurecer CORS por entorno y dominios explícitos.
- Contrato de error uniforme con códigos estables y correlation-id.
- Observabilidad funcional básica por formularios críticos.

---

## Release R3 — Escalabilidad Técnica de Commerce
**Ventana objetivo:** Mayo 2026  
**Objetivo:** Reducir complejidad estructural para acelerar evolución del módulo comercial.

### Alcance previsto
- Descomponer `CommerceController` por dominios funcionales.
- Reducir lógica de negocio embebida en Razor moviéndola a servicios testeables.
- Optimización de consultas críticas y baseline p95 por endpoints principales.
