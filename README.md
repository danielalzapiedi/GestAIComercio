# GestAI Booking MVP3 · Refinamiento final

## Requisitos
- .NET 9 SDK
- SQL Server
- Herramienta `dotnet-ef` instalada si vas a correr migraciones

## Ejecución local
1. Restaurar paquetes:
   `dotnet restore GestAI.sln`
2. Compilar la solución:
   `dotnet build GestAI.sln`
3. Ejecutar tests:
   `dotnet test GestAI.sln`
4. Actualizar la base de datos si corresponde:
   `dotnet ef database update --project GestAI.Infrastructure.Persistence --startup-project GestAI.Api`
5. Levantar API:
   `dotnet run --project GestAI.Api`
6. Levantar Blazor WebAssembly:
   `dotnet run --project GestAI.Web`

## Qué incluye este refinamiento
- Reorganización del menú lateral en secciones operativas, comerciales, de gestión y cuenta.
- Limpieza de branding residual del template y unificación del lenguaje a español.
- Mejora visual del dashboard, agenda, detalle de reserva, listados y reportes.
- Auditoría visible con filtros por entidad, usuario y fechas.
- Reportes ampliados con KPIs, ocupación por unidad, reservas por estado y métricas de señas.
- Estados vacíos más claros y consistentes.
- Tests base para tarifas, promociones, solapamientos y cambio de estado de reserva.

## Notas
- La arquitectura original se mantuvo.
- No se integraron IA, bots, WhatsApp ni servicios externos nuevos.
- Antes de empaquetar para entrega conviene ejecutar los comandos de build/test en una máquina con .NET 9 instalado.
