using CottonPrompt.Infrastructure.Entities;
using CottonPrompt.Infrastructure.Extensions;
using CottonPrompt.Infrastructure.Models.DesignBrackets;
using Microsoft.EntityFrameworkCore;

namespace CottonPrompt.Infrastructure.Services.DesignBrackets
{
    public class DesignBracketService(CottonPromptContext dbContext) : IDesignBracketService
    {
        public async Task CreateAsync(string name, decimal value, Guid userId)
        {
            try
            {
                var sortOrder = await dbContext.OrderDesignBrackets
                    .OrderByDescending(db => db.SortOrder)
                    .Select(db => db.SortOrder + 1)
                    .FirstOrDefaultAsync();

                var designBracket = new OrderDesignBracket
                {
                    Name = name,
                    Value = value,
                    CreatedBy = userId,
                    SortOrder = sortOrder,
                    Active = true,
                };

                await dbContext.OrderDesignBrackets.AddAsync(designBracket);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                await dbContext.OrderDesignBrackets.Where(db => db.Id == id).ExecuteDeleteAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DisableAsync(int id, Guid userId)
        {
            try
            {
                await dbContext.OrderDesignBrackets
                    .Where(db => db.Id == id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(db => db.Active, false)
                        .SetProperty(db => db.UpdatedBy, userId)
                        .SetProperty(db => db.UpdatedOn, DateTime.UtcNow));
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task EnableAsync(int id, Guid userId)
        {
            try
            {
                await dbContext.OrderDesignBrackets
                    .Where(db => db.Id == id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(db => db.Active, true)
                        .SetProperty(db => db.UpdatedBy, userId)
                        .SetProperty(db => db.UpdatedOn, DateTime.UtcNow));
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<DesignBracket>> GetAsync(bool hasActiveFilter, bool active)
        {
            try
            {
                var designBrackets = await dbContext.OrderDesignBrackets
                    .Where(db => !hasActiveFilter || (hasActiveFilter && db.Active == active))
                    .OrderBy(db => db.SortOrder)
                    .ToListAsync();
                var result = designBrackets.AsModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<GetDesignBracketOrdersCountModel> GetOrdersCountAsync(int id)
        {
            try
            {
                var result = new GetDesignBracketOrdersCountModel
                {
                    Count = await dbContext.Orders.CountAsync(o => o.DesignBracketId == id)
                };
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task SwapAsync(int id1, int id2, Guid userId)
        {
            try
            {
                var designBracket1 = await dbContext.OrderDesignBrackets.FindAsync(id1);
                var designBracket2 = await dbContext.OrderDesignBrackets.FindAsync(id2);

                if (designBracket1 is null || designBracket2 is null) return;

                (designBracket2.SortOrder, designBracket1.SortOrder) = (designBracket1.SortOrder, designBracket2.SortOrder);
                designBracket1.UpdatedBy = userId;
                designBracket1.UpdatedOn = DateTime.UtcNow;
                designBracket2.UpdatedBy = userId;
                designBracket2.UpdatedOn = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task UpdateAsync(int id, string name, decimal value, Guid userId)
        {
            try
            {
                // Get the current bracket to check if it's the "Authors" bracket
                var currentBracket = await dbContext.OrderDesignBrackets.FindAsync(id);
                var isAuthorsBracket = currentBracket?.Name == "Authors" || name == "Authors";

                await dbContext.OrderDesignBrackets
                    .Where(db => db.Id == id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(db => db.Name, name)
                        .SetProperty(db => db.Value, value)
                        .SetProperty(db => db.UpdatedBy, userId)
                        .SetProperty(db => db.UpdatedOn, DateTime.UtcNow));

                // Keep ConceptAuthorRate in sync with "Authors" design bracket
                if (isAuthorsBracket)
                {
                    await dbContext.Settings
                        .Where(s => true)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(s => s.ConceptAuthorRate, value)
                            .SetProperty(s => s.UpdatedBy, userId)
                            .SetProperty(s => s.UpdatedOn, DateTime.UtcNow));
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
