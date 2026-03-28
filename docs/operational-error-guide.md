# Guía de mensajes operativos y códigos de error de negocio

Fecha: 2026-03-28

## Objetivo
Definir una referencia única para códigos de error de negocio y mensajes base orientados a operación.

## Códigos base
- `forbidden`: el usuario no tiene permisos para ejecutar la acción.
- `unauthorized`: sesión inválida o vencida.
- `not_found`: recurso inexistente.
- `duplicate`: registro duplicado.
- `duplicate_code`: código de negocio duplicado.
- `validation_error`: datos inválidos en la solicitud.

## Reglas de uso
1. Mantener `errorCode` estable en backend para facilitar observabilidad y soporte.
2. Priorizar mensajes entendibles por usuario final en español.
3. Incluir `correlationId` en respuestas de error para trazabilidad con logs.
4. Evitar mensajes técnicos internos en UI final.

## Contrato esperado en API
- Respuesta de error en `ProblemDetails` con:
  - `status`
  - `detail`
  - `extensions.errorCode`
  - `extensions.correlationId`

## Fuente técnica
- `GestAI.Application/Common/BusinessErrorCatalog.cs`
- `GestAI.Api/Middleware/AppResultHttpMappingFilter.cs`
