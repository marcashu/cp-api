﻿using CottonPrompt.Infrastructure.Entities;
using CottonPrompt.Infrastructure.Extensions;
using CottonPrompt.Infrastructure.Models.DesignBrackets;
using Microsoft.EntityFrameworkCore;

namespace CottonPrompt.Infrastructure.Services.DesignBrackets
{
    public class DesignBracketService(CottonPromptContext dbContext) : IDesignBracketService
    {
        public async Task<IEnumerable<DesignBracket>> GetAsync()
        {
            try
            {
                var designBrackets = await dbContext.OrderDesignBrackets.OrderBy(db => db.SortOrder).ToListAsync();
                var result = designBrackets.AsModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task SwapAsync(int id1, int id2)
        {
            try
            {
                var designBracket1 = await dbContext.OrderDesignBrackets.FindAsync(id1);
                var designBracket2 = await dbContext.OrderDesignBrackets.FindAsync(id2);

                if (designBracket1 is null || designBracket2 is null) return;

                (designBracket2.SortOrder, designBracket1.SortOrder) = (designBracket1.SortOrder, designBracket2.SortOrder);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task UpdateAsync(int id, string value)
        {
            try
            {
                await dbContext.OrderDesignBrackets
                    .Where(db => db.Id == id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(db => db.Value, value));
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
