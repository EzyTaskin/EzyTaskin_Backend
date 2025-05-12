using EzyTaskin.Data.Model;
using EzyTaskin.Services;
using EzyTaskin.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzyTaskin.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProfileController : ControllerBase
{
    private readonly ProfileService _profileService;
    private readonly PaymentService _paymentService;
    private readonly NotificationService _notificationService;
    private readonly RequestService _requestService;
    private readonly CategoryService _categoryService;

    public ProfileController(
        ProfileService profileService,
        PaymentService paymentService,
        NotificationService notificationService,
        RequestService requestService,
        CategoryService categoryService
    )
    {
        _profileService = profileService;
        _paymentService = paymentService;
        _notificationService = notificationService;
        _requestService = requestService;
        _categoryService = categoryService;
    }

    [HttpPost(nameof(Provider))]
    [Authorize]
    public async Task<ActionResult<Provider>> CreateProvider()
    {
        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return BadRequest(error: ErrorStrings.SessionExpired);
        }

        try
        {
            return Ok(await _profileService.CreateProvider(accountId));
        }
        catch
        {
            return BadRequest(error: ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpGet(nameof(Provider))]
    [Authorize]
    public async Task<ActionResult<Provider>> GetProvider(
        [FromQuery] Guid? accountId
    )
    {
        accountId ??= this.TryGetAccountId();
        if (accountId.Value == Guid.Empty)
        {
            return BadRequest(error: ErrorStrings.SessionExpired);
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
                await _categoryService.GetProviderCategories(provider.Id).ToArrayAsync();

            // Populate completed requests.
            provider.CompletedRequests =
                await _requestService.GetCompletedRequests(provider.Id).ToArrayAsync();

            return Ok(provider);
        }
        catch
        {
            return BadRequest(error: ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpPatch(nameof(Provider))]
    [Authorize]
    public async Task<ActionResult> UpdateProvider(
        [FromForm] string? description,
        [FromForm] ICollection<string>? category
    )
    {
        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return BadRequest(error: ErrorStrings.SessionExpired);
        }

        try
        {
            var provider = await _profileService.GetProvider(accountId);
            if (provider is null)
            {
                return Unauthorized(ErrorStrings.NotAProvider);
            }

            List<Category>? newCategories = null;

            if (category is not null)
            {

            }

            provider = await _profileService.UpdateProvider(provider.Id, async (provider) =>
            {
                provider.Description = description;
                if (category is not null)
                {
                    try
                    {
                        var categoryIds = await _categoryService.GetCategoriesFor(category)
                            .Select(c => c.Id)
                            .ToListAsync();
                        newCategories = await _categoryService
                            .SetProviderCategories(provider.Id, categoryIds)
                            .ToListAsync();
                    }
                    catch
                    {
                        return null;
                    }
                }
                return provider;
            });

            if (provider is null)
            {
                return BadRequest(error: ErrorStrings.ErrorTryAgain);
            }

            provider.Categories ??= newCategories;

            return Ok(provider);
        }
        catch
        {
            return BadRequest(error: ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpPost($"{nameof(Provider)}/{nameof(ActivatePremium)}")]
    [Authorize]
    public async Task<ActionResult> ActivatePremium(
        [FromForm] Guid? paymentMethod
    )
    {
        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return BadRequest(ErrorStrings.SessionExpired);
        }

        try
        {
            var provider = await _profileService.GetProvider(accountId);
            if (provider is null)
            {
                return Unauthorized(ErrorStrings.NotAProvider);
            }
            if (provider.IsSubscriptionActive)
            {
                return BadRequest(ErrorStrings.PremiumAlreadyActive);
            }

            var validPaymentMethods =
                await _paymentService.GetPaymentMethods(accountId)
                    .Select(p => p.Id)
                    .ToHashSetAsync();

            if (paymentMethod.HasValue)
            {
                if (!validPaymentMethods.Contains(paymentMethod.Value))
                {
                    return BadRequest(error: ErrorStrings.InvalidPaymentMethod);
                }
            }
            else
            {
                if (validPaymentMethods.Count != 0)
                {
                    return BadRequest(error: ErrorStrings.NoPaymentMethod);
                }
                paymentMethod = validPaymentMethods.First();
            }

            provider = await _profileService.UpdateProvider(provider.Id, async (provider) =>
            {
                provider.IsPremium = true;

                if (provider.IsSubscriptionActive)
                {
                    return provider;
                }
                provider.IsSubscriptionActive = true;

                if (provider.SubscriptionDate.HasValue)
                {
                    return provider;
                }

                try
                {
                    await _paymentService.Debit(paymentMethod.Value, 8.99m);
                }
                catch
                {
                    return null;
                }

                provider.SubscriptionDate = DateTime.UtcNow;
                return provider;
            });

            if (provider is null)
            {
                return BadRequest(error: ErrorStrings.ErrorTryAgain);
            }

            await _notificationService.SendNotification(new()
            {
                Timestamp = DateTime.UtcNow,
                Account = provider.Account,
                Title = "Subscription",
                Content = "Your premium subscription has been activated."
            });

            return Ok(provider);
        }
        catch
        {
            return BadRequest(error: ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpPost($"{nameof(Provider)}/{nameof(DeactivatePremium)}")]
    [Authorize]
    public async Task<ActionResult> DeactivatePremium()
    {
        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return BadRequest(ErrorStrings.SessionExpired);
        }

        try
        {
            var provider = await _profileService.GetProvider(accountId);
            if (provider is null)
            {
                return Unauthorized(ErrorStrings.NotAProvider);
            }

            if (!provider.IsSubscriptionActive)
            {
                return BadRequest(ErrorStrings.PremiumNotActive);
            }

            provider = await _profileService.UpdateProvider(provider.Id, (provider) =>
            {
                provider.IsSubscriptionActive = false;
                return Task.FromResult<Provider?>(provider);
            });

            if (provider is null)
            {
                return BadRequest(error: ErrorStrings.ErrorTryAgain);
            }

            await _notificationService.SendNotification(new()
            {
                Timestamp = DateTime.UtcNow,
                Account = provider.Account,
                Title = "Subscription",
                Content = "Your premium subscription has been deactivated. " +
                    "Your benefits will remain until the next billing period."
            });

            return Ok(provider);
        }
        catch
        {
            return BadRequest(ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpPost(nameof(Consumer))]
    [Authorize]
    public async Task<ActionResult> CreateConsumer()
    {
        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return BadRequest(error: ErrorStrings.SessionExpired);
        }

        try
        {
            var consumer = await _profileService.CreateConsumer(accountId);
            return Ok(consumer);
        }
        catch
        {
            return BadRequest(error: ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpGet(nameof(Consumer))]
    [Authorize]
    public async Task<ActionResult<Consumer>> GetConsumer(
        [FromQuery] Guid? accountId
    )
    {
        accountId ??= this.TryGetAccountId();
        if (accountId.Value == Guid.Empty)
        {
            return BadRequest(error: ErrorStrings.SessionExpired);
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
            return BadRequest(error: ErrorStrings.ErrorTryAgain);
        }
    }
}
