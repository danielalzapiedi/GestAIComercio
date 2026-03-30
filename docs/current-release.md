# Current Delivery Status

## Modo actual
- **Modo:** Diagnóstico de producto continuo (sin releases activas)
- **Estado:** En progreso
- **Fecha de actualización:** 2026-03-30

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

## Tarea aplicada (actualización 2026-03-30 - cobertura extendida de filtros `col-lg-2`)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** completar pantallas remanentes para que la premisa `col-lg-2` se cumpla en todo `ListPageFilters`.
- **Detalle técnico:**
  1. se extendió la normalización a `AuditLog`, `DocumentHistory`, `Reports`, `CustomerCurrentAccounts`, `SupplierCurrentAccounts` y `SupplierAccounts`,
  2. `SupplierAccounts` se migró de wrapper manual `ui-filter-card` a `ListPageFilters` para entrar en el mismo contrato visual.
- **Impacto UX:** no quedan pantallas con `ListPageFilters` fuera de la grilla uniforme; todos los filtros comparten misma proporción en desktop.

## Tarea aplicada (actualización 2026-03-30 - ajuste de tamaño visual en botones de filtros)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** reducir peso visual de botones dentro de `ListPageFilters` para que queden armónicos con inputs/selects.
- **Detalle técnico:** en `.ui-list-page-filters .btn` se fijó altura de 38px alineada a controles de entrada, se ajustó tipografía/peso (`.9rem`, `600`) y se compactó padding/line-height.
- **Impacto UX:** botones de acción con presencia visual equilibrada respecto de los campos de filtro, evitando sensación de desproporción.

## Tarea aplicada (actualización 2026-03-30 - unificación estructural/escala de `ListPageHeader`)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** reforzar criterio común de `ListPageHeader` para que se perciba igual en todas las pantallas que lo usan, respetando particularidades de cada módulo.
- **Detalle técnico:**
  1. `ListPageHeader` ahora expone clases estructurales comunes (`ui-list-page-header`, `ui-list-page-header-main`, `ui-list-page-header-meta`),
  2. se normalizó escala visual de botones/chips dentro del header (`38px`, tipografía `0.9rem`, peso `600`) para alinearlos con los controles de filtros.
- **Impacto UX:** cabeceras más homogéneas entre pantallas, con acciones visualmente armónicas (sin botones sobredimensionados).

## Tarea aplicada (actualización 2026-03-30 - alineación izquierda uniforme en `ListPageHeader`)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** evitar variaciones derecha/izquierda en chips y acciones del header unificado.
- **Detalle técnico:** en `.ui-list-page-header` se forzó layout con `justify-content/align-items` a inicio y en `.ui-list-page-header-meta` se fijó `width:100%` + `justify-content:flex-start` para que botones/chips queden siempre a la izquierda.
- **Impacto UX:** criterio visual único en todas las pantallas con `ListPageHeader`, sin desplazamientos de acciones hacia la derecha según largo de texto.

## Tarea aplicada (actualización 2026-03-30 - centrado de texto en `btn-outline-secondary`)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** corregir descentrado visual de texto detectado en botones `btn-outline-secondary`.
- **Detalle técnico:** se definió `display:inline-flex` con `align-items:center` y `justify-content:center` para `btn-outline-secondary`, asegurando centrado consistente de label en alto/ancho.
- **Impacto UX:** botones secundarios con lectura centrada y apariencia estable en headers, filtros y acciones de navegación.

## Tarea aplicada (actualización 2026-03-30 - iconización de acciones unitarias en filas)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** cuando una fila tiene una sola acción, usar icono representativo (sin texto) como regla transversal.
- **Detalle técnico:**
  1. se migraron acciones unitarias de filas (`Abrir`, `Detalle`, `Ver detalle`, `Ver movimientos`, `Quitar`) a botones/iconos con `title` + `aria-label`,
  2. se aplicó en grillas de `Dashboard`, `DeliveryNotes`, `Invoices`, `PriceLists`, `CustomerCurrentAccounts`, `SupplierCurrentAccounts`, `SupplierAccounts` y tablas de líneas en ventas/presupuestos/quick-sale.
- **Impacto UX:** lectura visual más limpia en grillas con acción única y patrón consistente con el criterio de acciones contextuales.

## Tarea aplicada (actualización 2026-03-30 - stack vertical en cuentas corrientes)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** evitar compresión en notebook de las dos cards principales de Ctas. Clientes y Ctas. Proveedores.
- **Detalle técnico:** en `CustomerCurrentAccounts` y `SupplierCurrentAccounts` se cambió el primer bloque de dos columnas (`col-xl-6` + `col-xl-6`) a stack vertical (`col-12` + `col-12`) para render full width en todas las resoluciones.
- **Impacto UX:** mejor legibilidad y menor sensación de pantalla “amontonada”; ambas cards se leen en bloque continuo.

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

## Tarea aplicada (actualización 2026-03-30 - hardening null-safe en listados paginados)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** Corrección preventiva de nulidad en respuestas paginadas de API para evitar crash de UI cuando `Data` llega nulo sin excepción HTTP.
- **Detalle técnico:**
  1. se agregó fallback explícito `_result ??= new PagedResult<...>(...)` en cargas de listados de `Clientes`, `Presupuestos`, `Productos`, `Sucursales`, `Proveedores`, `Ventas`, `Compras`, `Remitos`, `Facturas`, `Listas de precios` y `Tenants`,
  2. en `DeliveryNotes` se expandió `Load()` para aplicar el mismo contrato null-safe,
  3. se mantiene comportamiento funcional actual (pantalla vacía controlada) en lugar de riesgo de `NullReferenceException` en render.
- **Impacto funcional:** mayor resiliencia ante respuestas inconsistentes del backend o envelopes con `Success=false` y `Data=null`, reduciendo riesgo de regresión visible en navegación/listados.

## Flujo de trabajo aplicado (modo resolver bugs - equipo)
1. **Product Manager:** validó que el trabajo entra en modo diagnóstico continuo y priorizó robustez transversal.
2. **Analyst:** detectó riesgo funcional repetido: cargas paginadas sin fallback nulo en múltiples pantallas.
3. **UX:** definió criterio de experiencia: ante datos nulos, mostrar estado vacío/estable y nunca error técnico de render.
4. **Architect:** eligió cambio de bajo riesgo y alto alcance: fallback local por pantalla sin romper contratos API.
5. **Developer:** implementó hardening null-safe en todos los listados críticos identificados.
6. **QA:** ejecutó chequeos de consistencia de cambios y validación estática; dejó advertido límite de entorno para build/test (`dotnet` no disponible).

## Tarea aplicada (actualización 2026-03-30 - hardening centralizado de envelopes AppResult)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** corregir inconsistencia de manejo de errores cuando la API responde HTTP 200 pero `AppResult.Success=false`.
- **Detalle técnico:**
  1. en `GestAI.Web/ApiClient.cs` se agregó validación centralizada post-deserialización para `AppResult` y `AppResult<T>`,
  2. si el envelope llega en estado no exitoso, ahora se lanza `ApiClientException` con `Message/ErrorCode` del envelope,
  3. la validación se aplica en `GetAsync`, `PostAsync<TRequest,TResponse>`, `PutAsync<TRequest,TResponse>` y `DeleteAsync<TResponse>`.
- **Impacto funcional:** la UI deja de interpretar envelopes fallidos como respuestas válidas y pasa a activar el flujo de error controlado (mensajes + fallback), reduciendo inconsistencias y riesgos de estado nulo silencioso.

## Flujo de trabajo aplicado (modo resolver bugs - equipo, iteración 2)
1. **Product Manager:** priorizó resolver la causa sistémica por encima de fixes puntuales por pantalla.
2. **Analyst:** identificó que el bug raíz era la aceptación de `Success=false` como “éxito técnico”.
3. **UX:** validó que el usuario debe ver feedback de error consistente y no una UI en estado ambiguo.
4. **Architect:** definió endurecimiento en capa `ApiClient` para normalizar comportamiento transversal.
5. **Developer:** implementó validación central de envelopes y reutilizó `ApiClientException` existente.
6. **QA:** ejecutó validación estática y checklist de riesgo de regresión; build/tests pendientes por limitación de entorno (`dotnet` no disponible).

## Tarea aplicada (actualización 2026-03-30 - cobertura AppResult en comandos sin payload tipado)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** extender hardening de `ApiClient` a comandos que no esperan DTO de respuesta (`PostAsync<TRequest>`, `PutAsync<TRequest>`, `DeleteAsync`).
- **Síntoma detectado:** cuando esos comandos devolvían HTTP 200 con body `AppResult { Success = false }`, la UI lo trataba como éxito porque no deserializaba ni validaba el envelope.
- **Detalle técnico:**
  1. se incorporó `EnsureAppResultEnvelopeSuccessOrThrowIfPresentAsync` para leer/validar envelopes JSON en respuestas con contenido,
  2. se invocó esa validación luego de `EnsureSuccessOrThrowAsync` en `PostAsync<TRequest>`, `PutAsync<TRequest>` y `DeleteAsync`,
  3. para payloads no-JSON o no-`AppResult`, el método no interviene (fallback seguro sin romper compatibilidad).
- **Impacto funcional:** errores lógicos del backend en comandos ya no pasan silenciosamente; se activa manejo de error consistente en la UI.

## Flujo de trabajo aplicado (modo resolver bugs - equipo, iteración 3)
1. **Product Manager:** priorizó cerrar la brecha remanente de comandos sin respuesta tipada.
2. **Analyst:** verificó que el bug persistía en operaciones de escritura pese al hardening previo.
3. **UX:** confirmó que los comandos deben mostrar feedback de error cuando el backend rechaza la operación.
4. **Architect:** definió extensión de la validación en capa de cliente HTTP para mantener coherencia transversal.
5. **Developer:** implementó validación opcional de envelope en métodos `void` del cliente.
6. **QA:** validó consistencia de contrato y riesgo de regresión bajo en rutas no-JSON.

## Tarea aplicada (actualización 2026-03-30 - refactor tipado de envelopes AppResult)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** eliminar fragilidad por reflexión en validación de envelopes `AppResult` del cliente web.
- **Detalle técnico:**
  1. se introdujo `IAppResultEnvelope` en `CommonDtos` y `AppResult`/`AppResult<T>` ahora implementan ese contrato,
  2. `ApiClient` dejó de inspeccionar `AppResult<T>` vía reflection y pasó a validación tipada (`payload is IAppResultEnvelope envelope`),
  3. se mantiene el mismo comportamiento funcional de error (`ApiClientException`) pero con menor deuda técnica y mejor mantenibilidad.
- **Impacto técnico:** menor complejidad, menos puntos frágiles por nombres de propiedades en runtime, y mejor consistencia arquitectónica en la capa de transporte.

## Flujo de trabajo aplicado (modo resolver bugs - equipo, iteración 4)
1. **Product Manager:** priorizó deuda técnica crítica en manejo transversal de errores.
2. **Analyst:** identificó riesgo de regresión por uso de reflection en paths calientes de cliente.
3. **UX:** validó que el cambio no altera feedback al usuario (solo robustez interna).
4. **Architect:** definió contrato explícito común para envelopes de resultado.
5. **Developer:** implementó interfaz compartida y simplificó validación en `ApiClient`.
6. **QA:** validó coherencia de tipos y ausencia de cambios en contratos públicos de API.

## Tarea aplicada (actualización 2026-03-30 - hotfix compilación ApiClient)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** corrección de error de compilación posterior al refactor de envelopes tipados.
- **Síntoma:** `CS0246` en `ApiClient.cs` por no resolver `IAppResultEnvelope` y `AppResult`.
- **Detalle técnico:** se agregó `using GestAI.Web.Dtos;` en `GestAI.Web/ApiClient.cs` para vincular explícitamente los tipos de envelope.
- **Impacto:** restaura compilación del proyecto web en entornos locales/CI que no tengan global usings equivalentes.

## Tarea aplicada (actualización 2026-03-30 - fix icono en select desplegables)
- **Modo:** Resolver bugs (diagnóstico continuo, sin releases activas).
- **Tarea:** recuperar la señal visual de desplegable en controles `select`.
- **Síntoma reportado:** los `select` se percibían como inputs comunes porque no mostraban el icono de flecha.
- **Causa raíz:** en `app-overrides.css`, la regla compartida de `.form-control, .form-select` usaba `background` shorthand, lo que sobreescribía `background-image` de Bootstrap para `form-select`.
- **Detalle técnico:**
  1. se reemplazó `background` por `background-color` en la regla compartida,
  2. se agregó regla explícita de `.form-select` para respetar `--bs-form-select-bg-img` y asegurar posición/tamaño/padding del ícono.
- **Impacto UX:** los selects vuelven a ser identificables visualmente como desplegables, mejorando escaneabilidad y usabilidad de formularios/filtros.
