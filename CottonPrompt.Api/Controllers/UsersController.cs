using CottonPrompt.Api.Messages.Users;
using CottonPrompt.Infrastructure.Models;
using CottonPrompt.Infrastructure.Models.Users;
using CottonPrompt.Infrastructure.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using System.Net;

namespace CottonPrompt.Api.Controllers
{
    [Authorize]
    [RequiredScope("access_as_user")]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController(IUserService userService) : ControllerBase
    {
        [HttpGet("login")]
        [ProducesResponseType<GetUsersModel>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> LoginAsync()
        {
            var user = User;

            var idClaim = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");
            var nameClaim = user.FindFirst("name");
            var emailClaim = user.FindFirst("preferred_username");

            if (idClaim is null || nameClaim is null || emailClaim is null)
            {
                return BadRequest("Missing required user claims");
            }

            var id = Guid.Parse(idClaim.Value);
            var name = nameClaim.Value;
            var email = emailClaim.Value;
            var result = await userService.LoginAsync(id, name, email);
            return Ok(result);
        }

        [HttpGet("unregistered")]
        [ProducesResponseType<IEnumerable<GetUsersModel>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetUnregisteredAsync()
        {
            var result = await userService.GetUnregisteredAsync();
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("registered")]
        [ProducesResponseType<IEnumerable<GetUsersModel>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRegisteredAsync()
        {
            var result = await userService.GetRegisteredAsync();
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{id}/can-update-role")]
        [ProducesResponseType<CanDoModel>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CanUpdateRoleAsync([FromRoute] Guid id, [FromQuery] string? roles)
        {
            var roleList = roles?.Split(',') ?? Enumerable.Empty<string>();
            var result = await userService.CanUpdateRoleAsync(id, roleList);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPut("{id}/role")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> UpdateRoleAsync([FromRoute] Guid id, [FromBody] UpdateUserRoleRequest request)
        {
            await userService.UpdateRoleAsync(id, request.UpdatedBy, request.Roles);
            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> AddAsync([FromBody] AddUserRequest request)
        {
            await userService.AddAsync(request.Id, request.Name, request.Email, request.Roles, request.CreatedBy);
            return NoContent();
        }

        [AllowAnonymous]
        [HttpGet("checker/{id}/has-waiting-for-customer")]
        [ProducesResponseType<IEnumerable<CanDoModel>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CheckerHasWaitingForCustomerAsync([FromRoute] Guid id)
        {
            var result = await userService.CheckerHasWaitingForCustomerAsync(id);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("not-member-of-group/{userGroupId}")]
        [ProducesResponseType<IEnumerable<GetUsersModel>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetNotMemberOfGroupAsync([FromRoute] int userGroupId)
        {
            var result = await userService.GetNotMemberOfGroupAsync(userGroupId);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("paymentLink")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]

        public async Task<IActionResult> AddPaymentLinkAsync([FromBody] PaymentLinkRequest request)
        {
            await userService.AddPaymentLinkAsync(
                request.UserId,
                request.PaymentLink);

            return NoContent();
        }
    }
}
    