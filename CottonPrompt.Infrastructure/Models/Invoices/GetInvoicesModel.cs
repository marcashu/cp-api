namespace CottonPrompt.Infrastructure.Models.Invoices
{
    public record GetInvoicesModel(
        Guid Id,
        DateTime StartDate,
        DateTime EndDate,
        string User,
        string PaymentLink,
        decimal Amount,
        string Status
    );
}
