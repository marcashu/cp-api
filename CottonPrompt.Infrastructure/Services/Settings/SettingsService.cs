using CottonPrompt.Infrastructure.Entities;
using CottonPrompt.Infrastructure.Extensions;
using CottonPrompt.Infrastructure.Models.Rates;
using Microsoft.EntityFrameworkCore;
using System;
namespace CottonPrompt.Infrastructure.Services.Settings
{
    public class SettingsService(CottonPromptContext dbContext) : ISettingsService
    {
        public async Task<RatesModel> GetRatesAsync()
        {
            try
            {
                var rates = await dbContext.Settings.FirstAsync();
                var result = rates.AsRatesModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task UpdateAsync(decimal qualityControlRate, decimal changeRequestRate, decimal conceptAuthorRate, Guid updatedBy)
        {
            try
            {
                await dbContext.Settings
                    .Where(r => true)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(r => r.QualityControlRate, qualityControlRate)
                        .SetProperty(r => r.ChangeRequestRate, changeRequestRate)
                        .SetProperty(r => r.ConceptAuthorRate, conceptAuthorRate)
                        .SetProperty(r => r.UpdatedBy, updatedBy)
                        .SetProperty(r => r.UpdatedOn, DateTime.UtcNow));

                // Keep "Authors" design bracket in sync with ConceptAuthorRate
                await dbContext.OrderDesignBrackets
                    .Where(db => db.Name == "Authors")
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(db => db.Value, conceptAuthorRate)
                        .SetProperty(db => db.UpdatedBy, updatedBy)
                        .SetProperty(db => db.UpdatedOn, DateTime.UtcNow));
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
