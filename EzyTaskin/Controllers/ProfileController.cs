using EzyTaskin.Data.Model;
using EzyTaskin.Services;
using EzyTaskin.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzyTaskin.Controllers;

[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly ProfileService _profileService;
    private readonly PaymentService _paymentService;

    public ProfileController(
        ProfileService profileService,
        PaymentService paymentService
    )
    {
        _profileService = profileService;
        _paymentService = paymentService;
    }

    [HttpPost(nameof(Provider))]
    [Authorize]
    public async Task<ActionResult> CreateProvider(
        [FromQuery] string? returnUrl
    )
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return this.RedirectWithError(error: ErrorStrings.SessionExpired);
        }

        try
        {
            await _profileService.CreateProvider(accountId);
            return this.RedirectToReferrer(returnUrl ?? "/");
        }
        catch
        {
            return this.RedirectWithError(error: ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpGet(nameof(Provider))]
    [Authorize]
    public async Task<ActionResult<Provider>> GetProvider(
        [FromQuery] Guid? accountId
    )
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        accountId ??= this.TryGetAccountId();
        if (accountId.Value == Guid.Empty)
        {
            return BadRequest();
        }

        try
        {
            var provider = await _profileService.GetProvider(accountId.Value);
            if (provider is null)
            {
                return NotFound();
            }

            if (provider.ReviewCount != 0)
            {
                // To the client, hide precise total rating in favor of an "average" rating.
                provider.AverageRating =
                    (decimal)provider.TotalRating.GetValueOrDefault() / provider.ReviewCount;
            }
            provider.TotalRating = null;

            // Populate categories.
            provider.Categories =
                await _profileService.GetProviderCategories(provider.Id).ToArrayAsync();

            return Ok(provider);
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpPatch(nameof(Provider))]
    [Authorize]
    public async Task<ActionResult> UpdateProvider(
        [FromForm] string? description,
        [FromForm] ICollection<Guid>? categoryId,
        [FromQuery] string? returnUrl
    )
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return this.RedirectWithError(ErrorStrings.SessionExpired);
        }

        try
        {
            var provider = await _profileService.GetProvider(accountId);
            if (provider is null)
            {
                return this.RedirectWithError(ErrorStrings.NotAProvider);
            }

            if (categoryId is not null)
            {
                if (categoryId.Count == 0
                    || categoryId.Count == 1 && categoryId.Single() == Guid.Empty)
                {
                    await _profileService.SetProviderCategories(provider.Id, [])
                        .ToListAsync();
                }
                else
                {
                    var succeed =
                        await _profileService.SetProviderCategories(provider.Id, categoryId)
                            .AnyAsync();

                    if (!succeed)
                    {
                        return this.RedirectWithError(ErrorStrings.InvalidCategory);
                    }
                }
            }

            provider.Description = description;
            await _profileService.UpdateProvider(provider);

            return this.RedirectToReferrer(returnUrl ?? "/");
        }
        catch
        {
            return this.RedirectWithError(ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpPost($"{nameof(Provider)}/{nameof(ActivatePremium)}")]
    [Authorize]
    public async Task<ActionResult> ActivatePremium(
        [FromQuery] string? returnUrl,
        [FromForm] Guid? paymentMethod
    )
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return this.RedirectWithError(ErrorStrings.SessionExpired);
        }

        try
        {
            var provider = await _profileService.GetProvider(accountId);
            if (provider is null)
            {
                return this.RedirectWithError(ErrorStrings.NotAProvider);
            }

            if (provider.IsSubscriptionActive)
            {
                return this.RedirectWithError(ErrorStrings.PremiumAlreadyActive);
            }

            // TODO: Inspect whether there may be a race condition?
            provider.IsPremium = true;
            provider.IsSubscriptionActive = true;
            provider.SubscriptionDate = DateTime.Now;

            var validPaymentMethods =
                await _paymentService.GetPaymentMethods(accountId)
                    .Select(p => p.Id)
                    .ToHashSetAsync();

            if (paymentMethod.HasValue)
            {
                if (!validPaymentMethods.Contains(paymentMethod.Value))
                {
                    return this.RedirectWithError(ErrorStrings.InvalidPaymentMethod);
                }
            }
            else
            {
                if (!validPaymentMethods.Any())
                {
                    var referrer = Request.GetTypedHeaders().Referer?.ToString() ?? returnUrl;
                    // TODO: Ask the frontend team where this endpoint is.
                    return this.RedirectToReferrerWithQuery("/Payment",
                        new Dictionary<string, object?>()
                        {
                            { "returnUrl", referrer }
                        }
                    );
                }
                paymentMethod = validPaymentMethods.First();
            }

            // TODO: Debit amount?
            await _paymentService.Debit(paymentMethod.Value, 0.0m);

            await _profileService.UpdateProvider(provider);

            return this.RedirectToReferrer(returnUrl ?? "/");
        }
        catch
        {
            return this.RedirectWithError(ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpPost($"{nameof(Provider)}/{nameof(DeactivatePremium)}")]
    [Authorize]
    public async Task<ActionResult> DeactivatePremium(
        [FromQuery] string? returnUrl
    )
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return this.RedirectWithError(ErrorStrings.SessionExpired);
        }

        try
        {
            var provider = await _profileService.GetProvider(accountId);
            if (provider is null)
            {
                return this.RedirectWithError(ErrorStrings.NotAProvider);
            }

            if (!provider.IsSubscriptionActive)
            {
                return this.RedirectWithError(ErrorStrings.PremiumNotActive);
            }

            provider.IsSubscriptionActive = false;
            await _profileService.UpdateProvider(provider);

            return this.RedirectToReferrer(returnUrl ?? "/");
        }
        catch
        {
            return this.RedirectWithError(ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpPost(nameof(Consumer))]
    [Authorize]
    public async Task<ActionResult> CreateConsumer(
        [FromQuery] string? returnUrl
    )
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return this.RedirectWithError(error: ErrorStrings.SessionExpired);
        }

        try
        {
            await _profileService.CreateConsumer(accountId);
            return this.RedirectToReferrer(returnUrl ?? "/");
        }
        catch
        {
            return this.RedirectWithError(error: ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpGet(nameof(Consumer))]
    [Authorize]
    public async Task<ActionResult<Consumer>> GetConsumer(
        [FromQuery] Guid? accountId
    )
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        accountId ??= this.TryGetAccountId();
        if (accountId.Value == Guid.Empty)
        {
            return BadRequest();
        }

        try
        {
            var consumer = await _profileService.GetConsumer(accountId.Value);
            if (consumer is null)
            {
                return NotFound();
            }
            return Ok(consumer);
        }
        catch
        {
            return BadRequest();
        }
    }
}
