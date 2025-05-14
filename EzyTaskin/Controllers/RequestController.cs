using System.ComponentModel.DataAnnotations;
using EzyTaskin.Data.Model;
using EzyTaskin.Services;
using EzyTaskin.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzyTaskin.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RequestController : ControllerBase
{
    private readonly RequestService _requestService;
    private readonly NotificationService _notificationService;
    private readonly PaymentService _paymentService;
    private readonly ProfileService _profileService;
    private readonly CategoryService _categoryService;

    public RequestController(
        RequestService requestService,
        NotificationService notificationService,
        PaymentService paymentService,
        ProfileService profileService,
        CategoryService categoryService
    )
    {
        _requestService = requestService;
        _notificationService = notificationService;
        _paymentService = paymentService;
        _profileService = profileService;
        _categoryService = categoryService;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult> CreateRequest(
        [FromForm, Required] string title,
        [FromForm, Required] string description,
        [FromForm, Required] string location,
        [FromForm, Required] decimal budget,
        [FromForm] DateTime? dueDate,
        [FromForm, Required] bool remoteEligible,
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
            var consumer = await _profileService.GetConsumer(accountId);
            if (consumer is null)
            {
                return Unauthorized(ErrorStrings.NotAConsumer);
            }

            var request = await _requestService.CreateRequest(new()
            {
                Consumer = new() { Id = consumer.Id },
                Title = title,
                Description = description,
                Location = location,
                Budget = budget,
                DueDate = dueDate,
                RemoteEligible = remoteEligible,
            });

            if (category is not null)
            {
                var categoryIds = await _categoryService.GetCategoriesFor(category)
                    .Select(c => c.Id)
                    .ToListAsync();
                request.Categories = await _categoryService
                    .SetRequestCategories(request.Id, categoryIds)
                    .ToListAsync();
            }

            return Ok(request);
        }
        catch
        {
            return BadRequest(error: ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> GetRequest(
        [FromQuery] Guid? requestId,
        [FromQuery, RegularExpression($"^(?i:{nameof(Consumer)}|{nameof(Provider)})$")]
        string? type
    )
    {
        try
        {
            if (requestId.HasValue)
            {
                var request = await _requestService.GetRequest(requestId.Value);
                if (request is null)
                {
                    return NotFound();
                }

                // Populate offers for targeted query
                request.Offers = await _requestService.GetOffers(requestId.Value).ToListAsync();
                request.Categories = await _categoryService.GetRequestCategories(request.Id)
                    .ToListAsync();

                return Ok(request);
            }
            else
            {
                var accountId = this.TryGetAccountId();
                if (accountId == Guid.Empty)
                {
                    return BadRequest(ErrorStrings.SessionExpired);
                }

                Guid? consumerId = null;
                Guid? providerId = null;

                switch (type)
                {
                    case null:
                    case nameof(Consumer):
                    {
                        var consumer = await _profileService.GetConsumer(accountId);
                        if (consumer is null)
                        {
                            return Unauthorized(ErrorStrings.NotAConsumer);
                        }
                        consumerId = consumer.Id;
                    }
                    break;
                    case nameof(Provider):
                    {
                        var provider = await _profileService.GetConsumer(accountId);
                        if (provider is null)
                        {
                            return Unauthorized(ErrorStrings.NotAConsumer);
                        }
                        providerId = provider.Id;
                    }
                    break;
                    default:
                    {
                        return BadRequest(ErrorStrings.InvalidProfileType);
                    }
                }

                return Ok(_requestService.GetRequests(
                    consumerId: consumerId,
                    providerId: providerId
                ));
            }
        }
        catch
        {
            return BadRequest(ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpGet(nameof(FindRequests))]
    [Authorize]
    public ActionResult<IAsyncEnumerable<Request>> FindRequests(
        [FromQuery] string? keywords,
        [FromQuery] ICollection<Guid>? categoryId,
        [FromQuery] string? location
    )
    {
        return Ok(
            _requestService.FindRequests(keywords, categoryId, location, isCompleted: false)
                .Select(async (r, _, _) =>
                {
                    r.Categories = await _categoryService.GetRequestCategories(r.Id)
                        .ToListAsync();
                    return r;
                })
        );
    }

    [HttpPost(nameof(CompleteRequest))]
    [Authorize]
    public async Task<ActionResult> CompleteRequest(
        [FromForm, Required] Guid requestId
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

            var request = await _requestService.GetRequest(requestId);
            if (request is null
                || request.Selected is null
                || request.Selected?.Provider.Id != provider.Id)
            {
                return BadRequest(error: ErrorStrings.InvalidRequest);
            }

            if (request.CompletedDate != null)
            {
                return BadRequest(error: ErrorStrings.RequestAlreadyComplete);
            }

            var providerPaymentMethod = await _paymentService.GetPaymentMethods(accountId)
                .FirstOrDefaultAsync();

            var consumerPaymentMethod =
                await _paymentService.GetPaymentMethods(request.Consumer.Account)
                    .FirstOrDefaultAsync();

            if (providerPaymentMethod is null || consumerPaymentMethod is null)
            {
                return BadRequest(error: ErrorStrings.NoPaymentMethod);
            }

            var completedRequest = await _requestService.CompleteRequest(requestId, async (_) =>
            {
                var price = request.Selected.Price ?? request.Budget;
                price = Math.Min(price, request.Budget);

                return await _paymentService.Transfer(
                    new TransferPaymentCommand(
                        consumerPaymentMethod,
                        providerPaymentMethod,
                        price
                    )
                );
            });

            if (completedRequest is null)
            {
                return BadRequest(error: ErrorStrings.ErrorTryAgain);
            }

            await _notificationService.SendNotification(new()
            {
                Timestamp = DateTime.UtcNow,
                Account = request.Consumer.Account,
                Title = "Tasks",
                Content = $"\"{request.Title}\" has been completed."
            });

            await _notificationService.SendNotification(new()
            {
                Timestamp = DateTime.UtcNow,
                Account = provider.Account,
                Title = "Tasks",
                Content = $"\"{request.Title}\" has been completed."
            });

            return Ok(await _requestService.GetRequest(requestId));
        }
        catch
        {
            return BadRequest(error: ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpPost(nameof(Offer))]
    [Authorize]
    public async Task<ActionResult> CreateOffer(
        [FromForm, Required] Guid requestId,
        [FromForm] decimal? price
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

            var request = await _requestService.GetRequest(requestId);
            if (request is null)
            {
                return BadRequest(error: ErrorStrings.InvalidRequest);
            }

            if (price.HasValue)
            {
                if (!provider.IsPremium)
                {
                    return Unauthorized(ErrorStrings.PremiumRequired);
                }

                if (price.Value > request.Budget)
                {
                    return BadRequest(error: ErrorStrings.OfferPriceExceedsBudget);
                }
            }

            var offer = await _requestService.CreateOffer(new()
            {
                Provider = provider,
                Request = requestId,
                Price = price
            });

            if (offer is null)
            {
                return BadRequest(error: ErrorStrings.ErrorTryAgain);
            }

            await _notificationService.SendNotification(new()
            {
                Timestamp = DateTime.UtcNow,
                Account = request.Consumer.Account,
                Title = "Tasks",
                Content =
                    $"A provider is offering to do \"{request.Title}\"" +
                        (offer.Price.HasValue ? $" for {offer.Price}." : ".")
            });

            return Ok(offer);
        }
        catch
        {
            return BadRequest(error: ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpPost($"{nameof(Offer)}/{nameof(SelectOffer)}")]
    [Authorize]
    public async Task<ActionResult> SelectOffer(
        [FromForm, Required] Guid requestId,
        [FromForm, Required] Guid offerId
    )
    {
        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return BadRequest(error: ErrorStrings.SessionExpired);
        }

        try
        {
            var consumer = await _profileService.GetConsumer(accountId);
            if (consumer is null)
            {
                return Unauthorized(ErrorStrings.NotAConsumer);
            }

            var request = await _requestService.GetRequest(requestId);
            if (request is null || request.Consumer.Id != consumer.Id)
            {
                return BadRequest(error: ErrorStrings.InvalidRequest);
            }

            if (request.CompletedDate != null)
            {
                return BadRequest(error: ErrorStrings.RequestAlreadyComplete);
            }

            var oldOffer = request.Selected;

            var offer = await _requestService.GetOffer(offerId);
            if (offer is null || offer.Request != requestId)
            {
                return BadRequest(error: ErrorStrings.InvalidOffer);
            }

            offer = await _requestService.SelectOffer(offerId);
            if (offer is null)
            {
                return BadRequest(error: ErrorStrings.ErrorTryAgain);
            }

            await _notificationService.SendNotification(new()
            {
                Timestamp = DateTime.UtcNow,
                Account = offer.Provider.Account,
                Title = "Tasks",
                Content = $"Your offer for \"{request.Title}\" has been selected."
            });

            if (oldOffer is not null)
            {
                await _notificationService.SendNotification(new()
                {
                    Timestamp = DateTime.UtcNow,
                    Account = oldOffer.Provider.Account,
                    Title = "Tasks",
                    Content =
                        $"Unfortunately, your offer for \"{request.Title}\" has been unselected."
                });
            }

            return Ok(await _requestService.GetRequest(request.Id));
        }
        catch
        {
            return BadRequest(error: ErrorStrings.ErrorTryAgain);
        }
    }
}
