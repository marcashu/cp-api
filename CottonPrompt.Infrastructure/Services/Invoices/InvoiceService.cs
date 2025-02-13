using CottonPrompt.Infrastructure.Entities;
using CottonPrompt.Infrastructure.Extensions;
using CottonPrompt.Infrastructure.Models.Invoices;
using Microsoft.EntityFrameworkCore;

namespace CottonPrompt.Infrastructure.Services.Invoices
{
    public class InvoiceService(CottonPromptContext dbContext) : IInvoiceService
    {
        public async Task<IEnumerable<GetInvoicesModel>> GetAsync(Guid? userId)
        {
            try
            {
                var invoices = await dbContext.Invoices
                    .Include(i => i.User)
                    .Where(i => userId == null || i.UserId == userId)
                    .OrderByDescending(i => i.StartDate).ThenBy(i => i.User.Name)
                    .ToListAsync();

      
                var users = await dbContext.Users.ToListAsync();

                foreach (var invoice in invoices)
                {
                    var user = users.Single(u => u.Id == invoice.UserId);
                    invoice.User.PaymentLink = user.PaymentLink;
                }

                var result = invoices.AsGetInvoicesModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<GetInvoiceModel> GetByIdAsync(Guid id)
        {
            try
            {
                var invoice = await dbContext.Invoices
                    .Include(i => i.InvoiceSections.OrderBy(s => s.Name))
                    .ThenInclude(s => s.InvoiceSectionOrders.OrderBy(o => o.OrderNumber))
                    .SingleAsync(i => i.Id == id);
                var result = invoice.AsGetInvoiceModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
