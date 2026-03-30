# Current Delivery Status

## Modo actual
- **Modo:** Diagnóstico de producto continuo (sin releases activas)
- **Estado:** En progreso
- **Fecha de actualización:** 2026-03-29

## Contexto
- El equipo opera en evolución continua por backlog priorizado.
- El foco vigente en este ciclo fue corregir issues visuales y de legibilidad reportados por usuario final.

## Release / modo vigente
- **Release activa en roadmap:** No hay release activa; el repositorio está en **modo diagnóstico continuo**.
- **¿La tarea actual pertenece al modo vigente?** Sí. Es un hotfix visual/funcional de render monetario en UI comercial.

## Tarea aplicada en este ciclo
- **Tarea:** Corrección visual transversal:
  1. mostrar nombre/apellido real del usuario conectado en el menú superior,
  2. eliminar textos con separadores conflictivos (`??`) en pantallas comerciales,
  3. mejorar responsividad para pantallas tipo notebook.
- **¿Pertenece al modo actual?** Sí. Impacta directamente experiencia de uso y percepción de calidad.

## Tarea aplicada (actualización 2026-03-29)
- **Tarea:** Hotfix de render de moneda para evitar literalización del sufijo `.ToString("C")` al usar fallback numérico en Razor.
- **Pantalla afectada:** Caja (`/cash`), chip de saldo en hero.
- **Impacto funcional:** garantiza visualización de importe formateado incluso cuando no hay dashboard cargado (`0` como fallback).

## Tarea aplicada (actualización 2026-03-29 - ajuste adicional)
- **Tarea:** Corrección de expresiones Razor en chips de conteo para evitar que se renderice `??` como texto literal.
- **Pantallas corregidas:** Clientes, Presupuestos, Ventas, Facturas, Remitos, Compras, Categorías, Sucursales, Depósitos y Proveedores.
- **Impacto funcional:** los contadores ahora muestran correctamente `0` cuando no hay resultados y no exhiben texto técnico en UI.

## Tarea aplicada (actualización 2026-03-29 - ajuste UX clientes)
- **Tarea:** Eliminación de texto contextual fuera de lugar en la barra de filtros de Clientes.
- **Detalle:** se removió “Base comercial de clientes” del bloque junto a los botones Buscar/Limpiar para mantener foco en acciones del filtro.
- **Impacto UX:** mejora jerarquía visual y evita ruido semántico en la zona de interacción primaria.

## Tarea aplicada (actualización 2026-03-29 - ajuste UX sucursales)
- **Tarea:** Eliminación de texto contextual fuera de lugar en la barra de filtros de Sucursales.
- **Detalle:** se removió “Gestión de estructura comercial” del bloque junto a los botones Buscar/Limpiar para mantener consistencia con el patrón UX.
- **Impacto UX:** interfaz más limpia y menor distracción en la zona de acciones.

## Tarea aplicada (actualización 2026-03-29 - ajuste UX depósitos/proveedores)
- **Tarea:** Eliminación de textos contextuales fuera de lugar en barras de filtros.
- **Detalle:** se removieron “Operación logística” (Depósitos) y “Red de abastecimiento” (Proveedores) del bloque junto a Buscar/Limpiar.
- **Impacto UX:** mayor limpieza visual y consistencia entre pantallas maestras.

## Tarea aplicada (actualización 2026-03-29 - fix reportes EF)
- **Tarea:** Corrección de query LINQ en reporte operativo para evitar excepción de traducción en EF Core.
- **Detalle técnico:** se separó la proyección agrupada a tipo anónimo SQL-translatable y el mapeo a `TopProductReportDto` se hace en memoria después de `ToListAsync`.
- **Impacto funcional:** el endpoint de reportes vuelve a responder sin `InvalidOperationException` en el ranking de productos.

## Tarea aplicada (actualización 2026-03-29 - fix categorías paginación)
- **Tarea:** Ajuste de `pageSize` en carga de categorías para selector de padre.
- **Detalle técnico:** se redujo `ParentOptionsPageSize` de 100 a 50 para cumplir la validación `InclusiveBetween(1, 50)` del backend.
- **Impacto funcional:** elimina la `ValidationException` al abrir Categorías y permite cargar opciones de categoría padre correctamente.

## Tarea aplicada (actualización 2026-03-30 - hotfix depósitos/sucursales)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Corrección de validación en Depósitos al cargar sucursales para filtros/formulario.
- **Síntoma reportado:** `FluentValidation.ValidationException` en `GetBranches` por `PageSize=100` fuera de rango (`1..50`).
- **Detalle técnico:** en `Warehouses.razor` se reemplazó el literal `pageSize=100` por la constante `BranchOptionsPageSize = 50` para alinear UI con contrato del backend.
- **Impacto funcional:** elimina la excepción al abrir Depósitos y restaura la carga de sucursales en filtro y alta/edición.

## Tarea aplicada (actualización 2026-03-30 - mejoras UX/UI clientes)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Ajustes de usabilidad y armonía visual en la pantalla de Clientes.
- **Detalle técnico:**
  1. se agregó estado explícito de error de carga con acción de reintento,
  2. se reequilibró la grilla de filtros y se añadió búsqueda por tecla Enter,
  3. se mejoró legibilidad de tabla con truncado seguro en celdas largas,
  4. se migraron acciones por fila a menú contextual de tres puntos cuando hay múltiples acciones disponibles, con estilo moderno no invasivo (ícono sin apariencia de botón tradicional).
- **Impacto UX:** mayor claridad entre estados (error vs vacío), mejor ritmo visual en filtros y tabla, menor fricción operativa en búsquedas.

## Tarea aplicada (actualización 2026-03-30 - ajuste estado visual editor)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Corrección de `OperationalStateHint` para evitar mostrar “Estado: success” de forma permanente en formularios de alta/edición.
- **Detalle técnico:** se eliminó el estado success por default del componente compartido; ahora sólo renderiza cuando aplica un estado operativo real (`loading`, `error`, `guardando` o `con cambios pendientes`).
- **Impacto UX:** desaparece el mensaje de éxito falso en editor de Clientes (y pantallas que reutilizan el componente), reduciendo ruido y confusión.

## Tarea aplicada (actualización 2026-03-30 - fix navegación nuevo cliente)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Corrección de navegación y estado en `Clientes` para alta nueva.
- **Síntoma reportado:** en algunos casos el botón “Nuevo cliente” requería doble click y, tras editar un cliente, al crear uno nuevo se arrastraban datos del último editado.
- **Detalle técnico:** se cambió el enrutado a parámetro explícito (`@page "/customers/{Mode}"`) para que la transición `"/customers"` → `"/customers/new"` dispare siempre actualización de parámetros; `IsNewRoute` ahora depende de `Mode == "new"` y `IsEditRoute` de `EditId`. Además, `NewItem()` inicializa `_form` antes de navegar.
- **Impacto UX/funcional:** apertura consistente con un solo click y formulario de alta siempre limpio, sin datos residuales de edición.

## Tarea aplicada (actualización 2026-03-30 - presupuestos alineado a premisas de clientes)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Aplicación de las mismas premisas UX/funcionales en pantalla de Presupuestos.
- **Detalle técnico:**
  1. búsqueda por Enter en filtros,
  2. estado explícito de error de carga con CTA de reintento,
  3. fallback seguro de `_result` ante error para evitar estados nulos frágiles,
  4. migración de acciones por fila a menú contextual de tres puntos,
  5. truncado visual en celdas largas de comprobante/cliente.
- **Impacto UX:** mayor consistencia con Clientes, menor ruido visual en grilla y mejor robustez ante fallos de API.

## Tarea aplicada (actualización 2026-03-30 - refinamiento UX presupuestos)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Implementación de quick wins UX sobre Presupuestos tras revisión visual.
- **Detalle técnico:**
  1. se eliminó la duplicación de feedback de error de carga para evitar mensajes repetidos,
  2. se ajustó microcopy del estado vacío para lenguaje orientado a negocio (sin referencias internas),
  3. se reforzó jerarquía visual de tabla destacando total y estado.
- **Impacto UX:** pantalla más limpia, menor ruido cognitivo y lectura más rápida de información clave.

## Tarea aplicada (actualización 2026-03-30 - ajuste filtros/menú acciones grillas)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Corrección visual en botones Buscar/Limpiar y visibilidad de dropdown en acciones por fila.
- **Detalle técnico:**
  1. se aplicó `text-nowrap` y nueva distribución responsive en filtros (Clientes/Presupuestos) para evitar quiebre de texto en botones,
  2. se habilitó contenedor de tabla apto para dropdown (`ui-table-responsive-actions`) y cards con overflow visible (`ui-data-card-actions`) para que el menú de tres puntos no quede recortado por scroll/overflow.
- **Impacto UX:** acciones más legibles y menú contextual completamente visible al abrir, sin clipping dentro de la grilla.

## Tarea aplicada (actualización 2026-03-30 - ajuste fino post-feedback cliente/presupuestos)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Corrección visual tras feedback de usuario final.
- **Detalle técnico:**
  1. se retiró el comportamiento de overflow extendido en la grilla de Clientes para recuperar estética original de la card,
  2. en Presupuestos se reforzó el dropdown de acciones con `dropstart` + `z-index` alto y contenedor sin clipping para evitar que el menú de tres puntos quede oculto.
- **Impacto UX:** Clientes vuelve a verse armónico y el menú de acciones en Presupuestos se despliega de forma más confiable.

## Flujo de trabajo aplicado (modo bugs)
1. **Product Manager:** confirmó que la tarea pertenece al modo diagnóstico continuo (sin releases).
2. **Analyst:** clasificó el incidente como bug funcional de integración UI/API por contrato de paginación.
3. **UX:** validó criterio de robustez: los combos deben cargar sin exponer errores técnicos al usuario.
4. **Architect:** definió corrección mínima y de bajo riesgo (alinear `pageSize` al límite del backend, sin romper contratos).
5. **Developer:** implementó constante explícita y reemplazo de query hardcodeada.
6. **QA:** validó build/tests y chequeo de regresión focal en pantalla de Depósitos.

## Flujo del equipo (ejecutado)
1. **Release Manager:** confirmó modo vigente (diagnóstico continuo) y validó pertenencia de la tarea.
2. **Análisis funcional:** relevamiento de síntoma visual reportado (`0.ToString("C")` visible en UI).
3. **UX/UI:** definición de criterio de legibilidad consistente para montos con fallback seguro.
4. **Diseño técnico:** encapsular toda la expresión monetaria en `@(...)` para evitar render literal parcial en Razor.
5. **Implementación:** ajuste puntual en `Cash.razor` sobre el chip de saldo.
6. **Validación QA:** build + tests para asegurar que el hotfix no introduzca regresiones.

## Entregables generados
- `GestAI.Web/MainLayout.razor`
  - prioriza el nombre/apellido del usuario autenticado (claims JWT) en el área de sesión activa.
- `GestAI.Web/Pages/Commerce/*.razor`
  - normaliza separadores visuales para evitar caracteres ambiguos en distintos entornos de render.
- `GestAI.Web/wwwroot/app-overrides.css`
  - mejora truncado de nombre de usuario y ajustes responsive para ancho notebook (<=1366px).
- `GestAI.Web/Pages/Commerce/Cash.razor`
  - corrige render de saldo en hero con formato monetario seguro sobre fallback.

## Validación y QA
- Se ejecutan build/test local para validación completa antes de cierre.
- Si la pipeline CI detecta regresión, se debe corregir y re-ejecutar hasta verde.

## Próximo paso recomendado
- Ejecutar validación visual completa cross-device (desktop/notebook/mobile) sobre los módulos comerciales más usados.
- Mantener checklist visual en QA para prevenir regresiones de legibilidad y layout.
