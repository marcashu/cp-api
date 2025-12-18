using CottonPrompt.Api.Messages.Rates;
using CottonPrompt.Infrastructure.Models.Rates;
using CottonPrompt.Infrastructure.Services.Settings;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CottonPrompt.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController(ISettingsService settingsService) : ControllerBase
    {
        [HttpGet("rates")]
        [ProducesResponseType<RatesModel>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRatesAsync()
        {
            var result = await settingsService.GetRatesAsync();
            return Ok(result);
        }

        [HttpPut("rates")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> UpdateAsync([FromBody] UpdateRatesRequest request)
        {
            await settingsService.UpdateAsync(request.QualityControlRate, request.ChangeRequestRate, request.ConceptAuthorRate, request.UpdatedBy);
            return NoContent();
        }
    }
}
