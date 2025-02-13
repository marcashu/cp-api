namespace CottonPrompt.Infrastructure.Models.Invoices
{
    public record GetInvoicesModel(
        Guid Id,
        DateTime StartDate,
        DateTime EndDate,
        string User,
        decimal Amount,
        string Status
    );
}
