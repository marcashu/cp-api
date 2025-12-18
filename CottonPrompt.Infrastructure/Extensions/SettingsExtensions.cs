using CottonPrompt.Infrastructure.Entities;
using CottonPrompt.Infrastructure.Models.Rates;

namespace CottonPrompt.Infrastructure.Extensions
{
    public static class SettingsExtensions
    {
        public static RatesModel AsRatesModel(this Setting entity)
        {
            var result = new RatesModel(entity.QualityControlRate, entity.ChangeRequestRate, entity.ConceptAuthorRate);
            return result;
        }
    }
}
