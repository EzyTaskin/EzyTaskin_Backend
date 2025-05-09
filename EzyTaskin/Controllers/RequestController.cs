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
    private readonly PaymentService _paymentService;
    private readonly ProfileService _profileService;
    private readonly CategoryService _categoryService;

    public RequestController(
        RequestService requestService,
        PaymentService paymentService,
        ProfileService profileService,
        CategoryService categoryService
    )
    {
        _requestService = requestService;
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
                request.Categories = await _requestService
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
        [FromQuery] Guid? requestId
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

                return Ok(request);
            }
            else
            {
                var accountId = this.TryGetAccountId();
                if (accountId == Guid.Empty)
                {
                    return BadRequest(ErrorStrings.SessionExpired);
                }
                var consumer = await _profileService.GetConsumer(accountId);
                if (consumer is null)
                {
                    return Unauthorized(ErrorStrings.NotAConsumer);
                }
                return Ok(_requestService.GetRequests(consumer.Id));
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
        return Ok(_requestService.FindRequests(keywords, categoryId, location, isCompleted: false));
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

            var completedRequest = await _requestService.CompleteRequest(requestId);
            if (completedRequest is null)
            {
                return BadRequest(error: ErrorStrings.RequestAlreadyComplete);
            }

            var price = request.Selected.Price ?? request.Budget;
            price = Math.Min(price, request.Budget);

            await _paymentService.Transfer(
                fromId: consumerPaymentMethod.Id,
                toId: providerPaymentMethod.Id,
                amount: price
            );

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

            if (price.HasValue)
            {
                // TODO: Check if premium state is updated?
                if (!provider.IsPremium)
                {
                    return Unauthorized(ErrorStrings.PremiumRequired);
                }

                var request = await _requestService.GetRequest(requestId);
                if (request is null)
                {
                    return BadRequest(error: ErrorStrings.InvalidRequest);
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

            var offer = await _requestService.GetOffer(offerId);
            if (offer is null || offer.Request != requestId)
            {
                return BadRequest(error: ErrorStrings.InvalidOffer);
            }

            await _requestService.SelectOffer(offerId);

            return Ok(await _requestService.GetRequest(request.Id));
        }
        catch
        {
            return BadRequest(error: ErrorStrings.ErrorTryAgain);
        }
    }
}
