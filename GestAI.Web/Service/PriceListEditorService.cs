using GestAI.Web.Dtos;

namespace GestAI.Web.Service;

public sealed class PriceListEditorService(ApiClient api)
{
    public Task<AppResult<int>?> CreateListAsync(PriceListUpsertCommand command, CancellationToken ct = default)
        => api.PostAsync<PriceListUpsertCommand, AppResult<int>>("api/commerce/price-lists", command, ct);

    public Task<AppResult?> UpdateListAsync(int id, PriceListUpsertCommand command, CancellationToken ct = default)
        => api.PutAsync<PriceListUpsertCommand, AppResult>($"api/commerce/price-lists/{id}", command, ct);

    public Task<AppResult<int>?> SaveItemAsync(int listId, PriceListItemUpsertCommand command, CancellationToken ct = default)
        => api.PostAsync<PriceListItemUpsertCommand, AppResult<int>>($"api/commerce/price-lists/{listId}/items", command, ct);

    public Task<AppResult<BulkPriceUpdateResultDto>?> ApplyBulkAsync(int listId, BulkPriceUpdateCommand command, CancellationToken ct = default)
        => api.PostAsync<BulkPriceUpdateCommand, AppResult<BulkPriceUpdateResultDto>>($"api/commerce/price-lists/{listId}/bulk-update", command, ct);
}
