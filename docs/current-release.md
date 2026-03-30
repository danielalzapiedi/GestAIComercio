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

## Tarea aplicada (actualización 2026-03-30 - ajuste final botones filtros)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Corrección de overflow de texto en botones Buscar/Limpiar.
- **Detalle técnico:** se redistribuyeron columnas de filtros en Presupuestos para dar mayor ancho a acciones y se creó `ui-filter-btn` con padding/tamaño de fuente más compacto; en Clientes se removieron íconos de acción para preservar legibilidad dentro del botón.
- **Impacto UX:** botones en una sola línea y contenido contenido dentro del ancho visual del control, sin desborde.

## Tarea aplicada (actualización 2026-03-30 - ajuste fino visual grilla clientes/filtros)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Reducir peso visual de botones de filtros y corregir apariencia de filas en grilla de Clientes.
- **Detalle técnico:** se compactó `ui-filter-btn` (menor altura/padding/tamaño de fuente) para liberar espacio a filtros y en Clientes se removió `table-striped` para evitar cortes visuales del fondo gris por fila.
- **Impacto UX:** filtros con mayor aire útil y tabla de Clientes con lectura más limpia/sólida.

## Tarea aplicada (actualización 2026-03-30 - ajuste padding botones filtro)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Reducción adicional de tamaño visual en botones Buscar/Limpiar.
- **Detalle técnico:** en `.ui-filter-btn` se eliminó padding interno (`padding: 0`) y se redujo altura mínima para compactar controles.
- **Impacto UX:** acciones menos pesadas visualmente y mayor protagonismo para campos de filtro.

## Tarea aplicada (actualización 2026-03-30 - corrección layout filtros presupuestos)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Ajuste final de distribución horizontal de filtros/acciones en Presupuestos.
- **Detalle técnico:** se revirtió el tweak de `font-size/padding` en botones y se normalizó la grilla a seis columnas `col-xl-2` para evitar compresión desigual (el problema original estaba en `col-xl-1`).
- **Impacto UX:** botones conservan estilo estándar y layout más equilibrado en desktop.

## Tarea aplicada (actualización 2026-03-30 - unificación estilo moderno de grillas)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Homogeneización visual de grillas principales en Clientes y Presupuestos.
- **Detalle técnico:** se creó el estilo compartido `ui-modern-grid` (cabecera moderna, zebra suave, hover consistente) y se aplicó en ambas tablas principales para eliminar diferencias de apariencia.
- **Impacto UX:** percepción uniforme, moderna y consistente entre pantallas maestras comerciales.

## Tarea aplicada (actualización 2026-03-30 - alineación filas grilla clientes)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Corrección de desalineación visual entre columnas por fila en Clientes.
- **Detalle técnico:** se estandarizó el contenido de celdas con `ui-table-stack` en todas las columnas de datos y se fijó altura mínima homogénea por celda en `ui-modern-grid`.
- **Impacto UX:** filas alineadas de forma consistente, con altura y ritmo visual equivalentes a Presupuestos.

## Tarea aplicada (actualización 2026-03-30 - simplificación KPIs en Presupuestos)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Remoción de card no relevante “Carga rápida” en cabecera de Presupuestos.
- **Detalle técnico:** se eliminó la métrica de conteo de SKUs (`Productos + Variantes`) por no aportar valor operativo directo en la vista principal; la grilla superior pasó de 4 a 3 cards con ancho equilibrado (`col-xl-4`).
- **Impacto UX:** cabecera más clara, menos ruido y foco en indicadores útiles para decisión comercial.

## Tarea aplicada (actualización 2026-03-30 - fix selección menú importación/productos)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Evitar doble marcado de navegación al ingresar a Importación.
- **Detalle técnico:** en `MainLayout.razor` se configuró `Match=\"NavLinkMatch.All\"` para el link `/products`, evitando que quede activo por prefijo cuando la ruta actual es `/products/import`.
- **Impacto UX:** al abrir Importación queda activo solo ese menú, sin resaltar también Productos.

## Tarea aplicada (actualización 2026-03-30 - unificación de grillas comerciales)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Estandarización de grillas principales mediante componente compartido.
- **Detalle técnico:**
  1. se creó `UnifiedGrid` como contenedor reutilizable para tabla responsive + estilo `ui-modern-grid`,
  2. se migraron grillas maestras de Clientes, Presupuestos, Sucursales, Depósitos, Proveedores, Categorías, Ventas, Compras, Facturas, Productos y movimientos de Caja,
  3. se mantuvo soporte específico para dropdown de acciones (`ui-table-responsive-actions`) en Presupuestos.
- **Impacto UX:** todas las grillas principales comparten el mismo patrón visual moderno (referencia Clientes/Presupuestos), reduciendo inconsistencias entre módulos.

## Tarea aplicada (actualización 2026-03-30 - unificación de cabecera y filtros en listados)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Estandarizar estructura de cabecera y bloque de filtros en pantallas de listados comerciales.
- **Detalle técnico:**
  1. se creó `ListPageHeader` para encapsular `page-hero` con variantes reutilizables (`Actions` y `MetaContent`),
  2. se creó `ListPageFilters` para encapsular el wrapper `ui-filter-card`,
  3. se migraron listados de Clientes, Presupuestos, Proveedores, Sucursales, Depósitos, Categorías, Compras, Ventas, Facturas y Productos al patrón común.
- **Impacto UX:** las pantallas de listado ahora comparten jerarquía visual y estructura de filtros consistente, simplificando mantenimiento y reduciendo divergencias de UI entre módulos.

## Tarea aplicada (actualización 2026-03-30 - unificación de paginación en listados)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Consolidar barra de paginación de listados en un único componente reutilizable.
- **Detalle técnico:**
  1. se creó `ListPaginationBar` para estandarizar “Página X de Y” + acciones Anterior/Siguiente,
  2. se migraron los listados paginados de Clientes, Presupuestos, Proveedores, Sucursales, Depósitos, Categorías, Compras, Ventas y Productos.
- **Impacto UX:** comportamiento y estilo de paginación homogéneo en todas las opciones de menú con listado paginado.

## Oportunidades detectadas para próxima unificación visual
1. **`ui-card-header` reusable:** encapsular eyebrow/título/subtítulo para cards de datos, formularios y paneles de ayuda.
2. **`ui-summary-grid` reusable:** normalizar cards KPI (label/value/foot) en una pieza compartida.
3. **acciones por fila:** converger todos los listados a menú contextual de tres puntos con reglas de overflow/z-index comunes.
4. **toolbar de filtros avanzados:** incorporar una variante compacta para módulos con más de 4 filtros manteniendo misma grilla responsive.
5. **estados operativos comunes:** unificar loading/empty/error con wrappers reutilizables por tipo de pantalla (listado, detalle, editor).

## Tarea aplicada (actualización 2026-03-30 - auditoría visual integral)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Revisión transversal de pantallas para detectar mejoras visuales y oportunidades de unificación futura.
- **Entregable:** `docs/visual-audit-2026-03-30.md` con:
  1. cobertura completa de rutas/pantallas revisadas,
  2. hallazgos de consistencia visual por bloques,
  3. backlog priorizado por fases (quick wins, transversal y estados),
  4. criterio de impacto UX y riesgo de mantenimiento.
- **Impacto de producto:** roadmap visual accionable para seguir consolidando un estilo único en todas las opciones del menú.

## Tarea aplicada (actualización 2026-03-30 - continuidad SaaS en administración de comercios)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** mantener capacidades SaaS (cuenta, usuarios y planes) sin depender del naming “Hospedaje”.
- **Detalle técnico:**
  1. se reubicaron las pantallas de cuenta/usuarios a `GestAI.Web/Pages/Saas/Account.razor` y `GestAI.Web/Pages/Saas/Users.razor`,
  2. se restauraron accesos `/account` y `/users` en el menú de usuario de `MainLayout.razor`,
  3. se ajustó la auditoría visual para incluir explícitamente la capa de gestión SaaS junto al core comercial.
- **Impacto UX:** el producto conserva su operación SaaS multi-plan para administrar comercios y mantiene navegación coherente.

## Tarea aplicada (actualización 2026-03-30 - segunda pasada de armonía visual del menú)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** reevaluar consistencia visual en todos los ítems del menú principal.
- **Entregable:** ampliación de `docs/visual-audit-2026-03-30.md` con:
  1. matriz por ruta de menú (`Alta/Media` armonía),
  2. conclusión de brechas remanentes en pantallas satélite,
  3. batch recomendado para alcanzar 100% de consistencia visual.
- **Impacto UX:** diagnóstico actualizado y accionable para cerrar diferencias de estilo entre módulos core y secundarios.

## Tarea aplicada (actualización 2026-03-30 - cierre de brechas visuales en pantallas satélite)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** implementar el primer batch de unificación pendiente detectado en la auditoría.
- **Detalle técnico:**
  1. `DeliveryNotes` migrada a `ListPageHeader` + `UnifiedGrid`,
  2. `DocumentHistory` migrada a `ListPageHeader` + `ListPageFilters` + `UnifiedGrid`,
  3. `Reports` migrada a `ListPageHeader` + `ListPageFilters` + `UnifiedGrid` en tablas operativas,
  4. `PriceLists` migrada a `ListPageHeader` + `UnifiedGrid` en sus grillas principales.
- **Impacto UX:** se reduce la brecha de consistencia entre módulos core y satélite, reforzando una experiencia más armónica en todos los ítems clave del menú.

## Tarea aplicada (actualización 2026-03-30 - cierre extendido de consistencia en satélites)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** aplicar un segundo batch para converger cuentas corrientes, inventario y fiscal.
- **Detalle técnico:**
  1. `Inventory` migrada a `ListPageHeader` + `ListPageFilters` + `UnifiedGrid` en stock y movimientos,
  2. `CustomerCurrentAccounts` y `SupplierCurrentAccounts` migradas a `ListPageHeader` + `ListPageFilters` + `UnifiedGrid` en listados y trazabilidad principal,
  3. `FiscalConfiguration` alineada en cabecera con `ListPageHeader`.
- **Impacto UX:** el menú queda prácticamente armonizado en su totalidad, con brechas menores concentradas en Caja/Importación.

## Tarea aplicada (actualización 2026-03-30 - convergencia final de caja/importación)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** completar la armonización visual pendiente en Caja e Importación.
- **Detalle técnico:**
  1. `Cash` migrada en cabecera a `ListPageHeader`,
  2. `ProductImport` migrada en cabecera a `ListPageHeader`,
  3. tabla de preview de `ProductImport` migrada a `UnifiedGrid`.
- **Impacto UX:** se completa la convergencia visual en módulos operativos clave; la brecha remanente queda acotada a pantallas administrativas puntuales.

## Tarea aplicada (actualización 2026-03-30 - convergencia de auditoría)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** alinear `AuditLog` al patrón compartido de listados.
- **Detalle técnico:**
  1. cabecera migrada a `ListPageHeader`,
  2. filtros migrados a `ListPageFilters`,
  3. tabla principal migrada a `UnifiedGrid`.
- **Impacto UX:** la pantalla de auditoría queda visualmente consistente con historial y el resto de listados del menú.

## Tarea aplicada (actualización 2026-03-30 - convergencia visual de tenants)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** alinear `Tenants` al patrón compartido de listados manteniendo su split de gestión con editor lateral.
- **Detalle técnico:**
  1. cabecera migrada a `ListPageHeader`,
  2. filtros migrados a `ListPageFilters`,
  3. tabla principal migrada a `UnifiedGrid`,
  4. barra de paginación migrada a `ListPaginationBar`.
- **Impacto UX:** `Tenants` deja de estar en armonía media y queda consistente con el resto de listados administrativos/comerciales.

## Tarea aplicada (actualización 2026-03-30 - convergencia visual de dashboard)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** alinear cabecera de `Dashboard` al patrón compartido manteniendo su naturaleza de tablero.
- **Detalle técnico:**
  1. estado sin módulos operativos migrado a `ListPageHeader`,
  2. estado principal migrado a `ListPageHeader` con `MetaContent` para chips KPI/contexto.
- **Impacto UX:** `Dashboard` converge en jerarquía visual de cabecera con el resto del menú sin perder su layout funcional de tablero.

## Tarea aplicada (actualización 2026-03-30 - convergencia de acciones por fila en grillas)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** aplicar la premisa transversal de acciones por fila: cuando existen múltiples acciones, usar menú de tres puntos.
- **Detalle técnico:**
  1. migración de filas con acciones múltiples a dropdown contextual (`ui-kebab-trigger` + `ui-actions-menu`) en `Branches`, `Suppliers`, `Warehouses`, `Categories`, `Products` (incluye variantes), `Sales`, `Purchases`, `Tenants` y `Saas/Users`,
  2. activación de `ActionsDropdown=\"true\"` en `UnifiedGrid` donde correspondía para evitar clipping del menú.
- **Impacto UX:** criterio de interacción uniforme en todas las grillas con acciones múltiples, menor ruido visual y mejor consistencia operativa.

## Tarea aplicada (actualización 2026-03-30 - estandarización visual de controles en filtros)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** volver `ListPageFilters` más genérico para que inputs/selects/botones se vean consistentes en todas las pantallas de listado.
- **Detalle técnico:**
  1. `ListPageFilters` ahora expone una clase contenedora común (`ui-list-page-filters`) para normalizar estilo descendente,
  2. se definieron reglas visuales compartidas para labels, `form-control`, `form-select` y botones dentro del contenedor (altura, radios, foco, tipografía, sombra).
- **Impacto UX:** los filtros conservan campos propios por pantalla, pero con un lenguaje visual consistente entre módulos.

## Tarea aplicada (actualización 2026-03-30 - grilla fija de filtros `col-lg-2`)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** aplicar la premisa de layout uniforme en `ListPageFilters`: todas las columnas internas pasan a `col-lg-2`.
- **Detalle técnico:**
  1. se normalizaron los bloques de filtros en listados comerciales/administrativos (`Invoices`, `Products`, `Branches`, `Inventory`, `Customers`, `Purchases`, `Quotes`, `Suppliers`, `Categories`, `Sales`, `Warehouses`, `Tenants`),
  2. cada columna del row de filtros quedó con `col-lg-2` (manteniendo clases responsive para breakpoints menores cuando aplica).
- **Impacto UX:** ancho consistente por columna de filtros entre pantallas y lectura visual homogénea del bloque de búsqueda.

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
