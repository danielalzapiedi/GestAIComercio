namespace GestAI.Web.Service;

public sealed class CommerceFormValidator
{
    public List<string> ValidateCategory(string? name, int? parentCategoryId, int currentCategoryId)
    {
        var issues = new List<string>();
        if (string.IsNullOrWhiteSpace(name))
            issues.Add("Ingresá el nombre de la categoría.");
        if (parentCategoryId.HasValue && parentCategoryId.Value == currentCategoryId)
            issues.Add("La categoría no puede tenerse como padre.");
        return issues;
    }

    public List<string> ValidateProduct(string? name, int categoryId, decimal salePrice, decimal cost, decimal minimumStock)
    {
        var issues = new List<string>();
        if (string.IsNullOrWhiteSpace(name)) issues.Add("Ingresá el nombre del producto.");
        if (categoryId <= 0) issues.Add("Seleccioná una categoría.");
        if (salePrice < 0) issues.Add("El precio de venta no puede ser negativo.");
        if (cost < 0) issues.Add("El costo no puede ser negativo.");
        if (minimumStock < 0) issues.Add("El stock mínimo no puede ser negativo.");
        return issues;
    }

    public List<string> ValidateCommercialLines(int customerId, int itemsCount, bool hasInvalidQuantity, bool hasNegativePrice, bool hasEmptyDescription)
    {
        var issues = new List<string>();
        if (customerId <= 0) issues.Add("Seleccioná un cliente.");
        if (itemsCount == 0) issues.Add("Agregá al menos un ítem.");
        if (hasInvalidQuantity) issues.Add("Las cantidades deben ser mayores a cero.");
        if (hasNegativePrice) issues.Add("El precio no puede ser negativo.");
        if (hasEmptyDescription) issues.Add("Cada ítem debe tener descripción.");
        return issues;
    }

    public List<string> ValidatePurchaseLines(int supplierId, int itemsCount, bool hasInvalidQuantity, bool hasNegativeCost, bool hasInvalidSku)
    {
        var issues = new List<string>();
        if (supplierId <= 0) issues.Add("Seleccioná un proveedor.");
        if (itemsCount == 0) issues.Add("Agregá al menos un ítem.");
        if (hasInvalidQuantity) issues.Add("Las cantidades deben ser mayores a cero.");
        if (hasNegativeCost) issues.Add("El costo unitario no puede ser negativo.");
        if (hasInvalidSku) issues.Add("Seleccioná un SKU válido en todas las líneas.");
        return issues;
    }
}
