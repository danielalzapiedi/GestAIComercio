using System.Text.Json;

namespace GestAI.Web.Pages.Commerce;

public partial class Sales
{
    private List<string> ValidateEditor()
        => FormValidator.ValidateCommercialLines(
            _form.CustomerId,
            _form.Items.Count,
            _form.Items.Any(x => x.Quantity <= 0),
            _form.Items.Any(x => x.UnitPrice < 0),
            _form.Items.Any(x => string.IsNullOrWhiteSpace(x.Description)));

    private string BuildSnapshot()
        => JsonSerializer.Serialize(new
        {
            _form.CustomerId,
            _form.Status,
            _issuedAtDate,
            _form.Observations,
            Items = _form.Items.Select(x => new { x.ProductId, x.ProductVariantId, x.Description, x.InternalCode, x.Quantity, x.UnitPrice }).ToList()
        });

    private bool IsDirty => _showForm && _formSnapshot != BuildSnapshot();

    private void MarkClean() => _formSnapshot = BuildSnapshot();

    private async Task<bool> ConfirmDiscardIfDirtyAsync()
        => await Guard.ConfirmDiscardAsync(JS, IsDirty, _saving);
}
