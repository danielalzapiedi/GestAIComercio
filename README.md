# GestAI SaaS Starter Template

Base SaaS reutilizable construida con:

- .NET 9
- ASP.NET Core Web API
- Blazor WebAssembly
- Clean Architecture
- CQRS con MediatR
- EF Core + SQL Server
- Multi-tenant por Account

## Estructura

- `GestAI.Domain`: entidades core SaaS (Account, AccountUser, planes, auditoría, usuarios).
- `GestAI.Application`: casos de uso y CQRS (acceso, cuenta, usuarios, auditoría).
- `GestAI.Infrastructure`: servicios de seguridad/identity y servicios transversales.
- `GestAI.Infrastructure.Persistence`: DbContext, configuraciones EF Core y migraciones.
- `GestAI.Api`: endpoints REST para auth + administración SaaS.
- `GestAI.Web`: UI Blazor WASM con dashboard y administración base.

## Módulos incluidos

- Autenticación (login/logout)
- Dashboard SaaS
- Account Settings
- Users (listar/crear/editar/activar/desactivar)
- Plans (visión de límites/features del plan activo)
- Audit Log

## Cómo ejecutar

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project GestAI.Api
```

Luego ejecutar el frontend:

```bash
dotnet run --project GestAI.Web
```

## Cómo extender con un nuevo dominio

1. Crear entidades del dominio en `GestAI.Domain/Entities`.
2. Agregar comandos/queries en `GestAI.Application/<Modulo>` con MediatR.
3. Registrar persistencia/configuración EF en `GestAI.Infrastructure.Persistence`.
4. Exponer endpoints en `GestAI.Api/Controllers`.
5. Crear páginas Blazor en `GestAI.Web/Pages` y proteger navegación según permisos.
6. Mantener toda operación bajo contexto de cuenta (`Account`) y usuario actual.
