namespace GestAI.Application.Common;

public static class BusinessErrorCatalog
{
    public const string Forbidden = "forbidden";
    public const string Unauthorized = "unauthorized";
    public const string NotFound = "not_found";
    public const string Duplicate = "duplicate";
    public const string DuplicateCode = "duplicate_code";
    public const string ValidationError = "validation_error";

    private static readonly Dictionary<string, string> DefaultMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        [Forbidden] = "No tenés permisos para ejecutar esta acción.",
        [Unauthorized] = "Tu sesión no es válida o expiró.",
        [NotFound] = "El recurso solicitado no existe.",
        [Duplicate] = "Ya existe un registro con los mismos datos.",
        [DuplicateCode] = "Ya existe un registro con el código indicado.",
        [ValidationError] = "Hay datos inválidos en la solicitud."
    };

    public static string ResolveMessage(string? errorCode, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(fallback))
            return fallback;

        if (string.IsNullOrWhiteSpace(errorCode))
            return "No se pudo completar la operación.";

        return DefaultMessages.TryGetValue(errorCode.Trim(), out var message)
            ? message
            : "No se pudo completar la operación.";
    }
}
