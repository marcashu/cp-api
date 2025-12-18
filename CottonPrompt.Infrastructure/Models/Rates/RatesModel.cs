namespace CottonPrompt.Infrastructure.Models.Rates
{
    public record RatesModel(
        decimal QualityControlRate,
        decimal ChangeRequestRate,
        decimal ConceptAuthorRate
    );
}
