# QA Regression Checklist (Commerce)

## Objetivo
Checklist rápido para validar regresión funcional en flujos críticos de Commerce antes de liberar cambios.

## 1) Acceso y contexto
- [ ] Login válido de usuario de tenant.
- [ ] Navegación a módulos Commerce sin errores de permisos inesperados.
- [ ] Cambio de pantalla sin loops de redirección.

## 2) Maestros
### Categorías
- [ ] Crear categoría.
- [ ] Editar categoría.
- [ ] Activar/desactivar categoría.

### Productos y variantes
- [ ] Crear producto.
- [ ] Editar producto.
- [ ] Crear variante.
- [ ] Editar variante.
- [ ] Activar/desactivar producto y variante.

## 3) Documentos comerciales
### Presupuestos
- [ ] Crear presupuesto con al menos 1 ítem.
- [ ] Editar presupuesto.
- [ ] Convertir presupuesto a venta.

### Ventas
- [ ] Crear venta manual.
- [ ] Editar venta.
- [ ] Crear venta rápida.

### Compras
- [ ] Crear compra.
- [ ] Editar compra editable.
- [ ] Crear recepción sobre compra.

## 4) Pricing
### Listas de precios
- [ ] Crear lista.
- [ ] Editar lista.
- [ ] Guardar precio manual por SKU.
- [ ] Ejecutar actualización masiva.

## 5) Fiscal
- [ ] Guardar configuración fiscal.
- [ ] Subir certificado.
- [ ] Subir clave privada.

## 6) UX / errores
- [ ] Botones de guardar deshabilitados durante operación en formularios críticos.
- [ ] Errores de API visibles en pantalla.
- [ ] Mensajes de éxito consistentes tras operación exitosa.

## 7) No regresión técnica
- [ ] `dotnet build GestAI.sln`
- [ ] `dotnet test GestAI.sln`
- [ ] Verificación de endpoints críticos (sales/quotes/purchases) sin rutas duplicadas.
