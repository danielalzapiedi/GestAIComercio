using Microsoft.JSInterop;

namespace GestAI.Web.Service;

public sealed class UnsavedChangesGuardService
{
    private const string DefaultPrompt = "Tenés cambios sin guardar. ¿Querés descartarlos?";

    public async Task<bool> ConfirmDiscardAsync(IJSRuntime js, bool isDirty, bool isSaving, string? prompt = null)
    {
        if (!isDirty || isSaving)
            return true;

        return await js.InvokeAsync<bool>("confirm", string.IsNullOrWhiteSpace(prompt) ? DefaultPrompt : prompt);
    }
}
