namespace GestAI.Web.Service;

public static class PricingPolicy
{
    public enum PricingMode
    {
        CatalogOnly = 1,
        CatalogWithOverride = 2
    }

    public const string QuickSalePolicyLabel = "Precio catálogo";
    public const string QuickSalePolicyDescription = "La venta rápida usa precio de lista vigente para evitar inconsistencias.";
    public const string StandardSalePolicyDescription = "En venta/presupuesto estándar el precio es editable con validación comercial.";

    public static PricingMode QuickSaleMode => PricingMode.CatalogOnly;
    public static PricingMode StandardSaleMode => PricingMode.CatalogWithOverride;

    public static decimal ResolvePrice(PricingMode mode, decimal catalogPrice, decimal? requestedPrice = null)
        => mode == PricingMode.CatalogOnly
            ? catalogPrice
            : requestedPrice ?? catalogPrice;
}
