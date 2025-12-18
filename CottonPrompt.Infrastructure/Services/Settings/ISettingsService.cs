using CottonPrompt.Infrastructure.Models.Rates;

namespace CottonPrompt.Infrastructure.Services.Settings
{
    public interface ISettingsService
    {
        Task<RatesModel> GetRatesAsync();

        Task UpdateAsync(decimal qualityControlRate, decimal changeRequestRate, decimal conceptAuthorRate, Guid updatedBy);
    }
}
