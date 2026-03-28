# Análisis funcional Sr + UX/UI Sr (GestAI Comercio)

Fecha: 2026-03-25

## 1) Alcance y método

Se realizó una revisión estática del sistema sobre:

- Navegación principal y arquitectura de pantallas (`MainLayout`).
- Flujos SaaS base (login, usuarios, cuenta).
- Flujos comerciales (clientes, productos, ventas, compras, caja, fiscal, reportes).
- Coherencia funcional frontend-backend (pantallas Blazor + endpoints/handlers).

> Nota: no fue posible ejecutar la suite (`dotnet test`) por falta del SDK .NET en el entorno de revisión.

## 2) Inventario funcional detectado

### SaaS / Plataforma
- Login (`/login`).
- Dashboard (`/dashboard`).
- Mi cuenta (`/account`).
- Usuarios del tenant (`/users`).
- Planes (`/plans`).
- Auditoría (`/audit-log`, `/document-history`).
- Tenants plataforma (`/platform/tenants`).

### Comercial
- Maestros: sucursales, depósitos, categorías, productos, listas de precios, importación de productos, clientes, proveedores.
- Circuito comercial: presupuestos, ventas, remitos, facturación.
- Compras y abastecimiento.
- Inventario y movimientos de stock.
- Caja y cuentas corrientes (clientes/proveedores).
- Fiscal/ARCA.
- Reportes operativos.

## 3) Hallazgos críticos y de alto impacto

## C1) Inconsistencia funcional en “Venta rápida”: la UI permite editar precio, pero el backend lo ignora

- En la pantalla de venta rápida se permite modificar `UnitPrice` por línea.
- Al guardar, el payload envía solo `ProductId`, `ProductVariantId` y `Quantity`.
- El backend reconstruye el precio tomando `sku.SalePrice` y no el valor editado en UI.

**Impacto:** alto riesgo de error operativo, pérdida de confianza del usuario y potenciales diferencias de importe entre lo visualizado y lo efectivamente registrado.

**Recomendación:**
1. Definir comportamiento oficial:
   - Opción A: bloquear edición de precio en quick sale.
   - Opción B: soportar precio editable extremo a extremo.
2. Si se adopta B, extender `QuickCommercialLineDto` y `CreateQuickSaleCommand` para incluir `UnitPrice` validado.
3. Agregar test de contrato para asegurar consistencia visual vs persistencia.

## C2) UX de error transversal insuficiente en formularios

- Patrón frecuente de `Save()`/`PostAsync()`/`PutAsync()` sin manejo de error de negocio ni feedback de campo.
- `ApiClient` usa `EnsureSuccessStatusCode()`: ante 4xx/5xx se lanza excepción, pero en muchas pantallas no hay `try/catch` local ni mensaje al usuario.

**Impacto:** percepción de “botón no responde”, pérdida de datos digitados, soporte reactivo (tickets), baja conversión en carga de datos.

**Recomendación:**
1. Estandarizar componente `FormFeedback` + patrón de estados (`loading/success/error`).
2. Mostrar errores por campo (validaciones de backend) y errores globales (toast/alert persistente).
3. Definir guideline: ningún `Save()` sin manejo explícito de error + estado de guardado + bloqueo anti-doble click.

## C3) Riesgo de seguridad operativa: password temporal hardcodeado en alta de usuarios

- En alta de usuarios se setea por defecto `Password = "Temp123$"`.

**Impacto:** patrón inseguro predecible, especialmente sensible en contextos multi-tenant.

**Recomendación:**
1. Eliminar contraseña por defecto fija.
2. Generar password aleatoria por alta y forzar cambio en primer login, o invitar por link temporal de activación.
3. Agregar política visual de seguridad en el flujo de alta.

## C4) Falta de validación UX inmediata en formularios críticos

- Muchos formularios usan `<input>` con bind directo sin `EditForm + DataAnnotations + ValidationMessage`.

**Impacto:** mayor tasa de error y reproceso; el usuario descubre errores tarde (al persistir).

**Recomendación:**
1. Estandarizar `EditForm` en ABMs y documentos operativos.
2. Definir reglas mínimas por entidad (requeridos, formato CUIT/email/teléfono, rangos numéricos).
3. Validación progresiva (on blur) y resumen de errores arriba del formulario.

## 4) Hallazgos funcionales / UX de prioridad media

## M1) Navegación con alta densidad de opciones, sin “modo tarea”

- El menú lateral concentra muchas entradas de operación + administración.

**Mejora sugerida:**
- Incorporar “favoritos” y “recientes”.
- Agrupar por rol (vendedor, administración, depósito).
- Agregar buscador global de comandos/pantallas (tipo command palette).

## M2) Filtros y búsqueda con comportamiento no homogéneo

- Algunas pantallas refrescan con botón “Buscar”, otras dependen de acciones puntuales sin patrón uniforme.

**Mejora sugerida:**
- Estándar de filtros: buscar, limpiar, persistir filtros por pantalla (local storage), badge de filtros activos.

## M3) Reportes operativos en formato tabular sin visual analítica

- La pantalla de reportes presenta datos útiles pero sin visualizaciones (tendencias comparativas, variación, semáforos).

**Mejora sugerida:**
- Incorporar gráficos básicos (línea ventas/compras, barras top productos, alertas de stock).
- Añadir comparativos período anterior y variación %.

## M4) Fiscal/ARCA: buena cobertura funcional, pero UX mejorable en validación guiada

- Existe carga de credenciales y configuración de entorno.
- Falta un “checklist de readiness” explícito (qué falta para salir a producción).

**Mejora sugerida:**
- Estado tipo wizard: “Falta CUIT”, “Falta certificado”, “Falta clave”, “Última prueba OK/Fail”.
- Botón “Probar conexión” con resultado detallado y recomendación accionable.

## M5) Falta de patrón de prevención de pérdida de cambios

- En rutas editor, no se detecta explícitamente “dirty form” antes de salir.

**Mejora sugerida:**
- Guard de navegación con confirmación (“Tenés cambios sin guardar”).

## M6) Accesibilidad: oportunidad de mejora en controles tabulares y formularios

- Hay uso amplio de tablas y botones de acción por fila; conviene reforzar accesibilidad de teclado y semántica.

**Mejora sugerida:**
- Revisar foco visible, orden tab, `aria-label` contextual en acciones repetitivas (“Editar cliente X”).
- Revisar contraste y tamaño de objetivos táctiles en vistas densas.

## 5) Hallazgos de prioridad baja (calidad percibida / escalabilidad UX)

## L1) Terminología y microcopy

- Existen etiquetas muy técnicas en algunos filtros (ej. entidad en auditoría con nombres de clases).

**Mejora sugerida:**
- Mapear a lenguaje de negocio consistente y contextualmente comprensible.

## L2) Estados vacíos y skeletons

- Hay buenos empty states en varias pantallas; puede extenderse a más vistas y tablas intermedias.

**Mejora sugerida:**
- Skeleton loading homogéneo y mensajes de “siguiente acción recomendada”.

## L3) Métricas de éxito UX ausentes

**Mejora sugerida:**
- Instrumentar eventos: tiempo de alta de documento, error rate por formulario, abandono de flujo.

## 6) Backlog priorizado (90 días)

### Ola 1 (0-30 días)
1. Corregir inconsistencia de precio en venta rápida (C1).
2. Eliminar password fija y rediseñar onboarding de usuario (C3).
3. Patrón transversal de manejo de errores + feedback de guardado (C2).
4. Validación UX en formularios de clientes/proveedores/productos/ventas (C4).

### Ola 2 (31-60 días)
1. Estandarización de filtros y búsquedas.
2. Guard de cambios sin guardar.
3. Readiness fiscal guiado con checklist.

### Ola 3 (61-90 días)
1. Reportes con capa visual + comparativos.
2. Mejoras de accesibilidad y microcopy.
3. Favoritos/recientes + búsqueda global de pantallas.

## 7) Quick wins concretos

- Quick sale: ocultar campo precio hasta soportarlo end-to-end.
- Usuarios: remover `Temp123$` y mostrar política de activación segura.
- Formularios ABM: agregar barra superior de estado (“Guardando / Guardado / Error”).
- Reportes: sumar KPI de variación mensual y top cambios críticos de stock.

## 8) Conclusión ejecutiva

El producto tiene una base funcional amplia y bien orientada a operación comercial real (circuito completo de venta, compra, stock, caja y fiscal). La principal oportunidad está en **consistencia operativa + robustez UX**: alinear lo que la pantalla promete con lo que persiste, estandarizar feedback de errores/validaciones y elevar seguridad/experiencia en flujos críticos. Con un plan de 90 días enfocado en los hallazgos C1-C4, el impacto esperado es reducción de errores, mejor adopción y mayor confianza de usuarios de operación.
