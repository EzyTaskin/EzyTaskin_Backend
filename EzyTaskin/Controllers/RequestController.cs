using System.ComponentModel.DataAnnotations;
using EzyTaskin.Data.Model;
using EzyTaskin.Services;
using EzyTaskin.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzyTaskin.Controllers;

[Route("api/[controller]")]
public class RequestController : ControllerBase
{
    private readonly RequestService _requestService;
    private readonly PaymentService _paymentService;
    private readonly ProfileService _profileService;

    public RequestController(
        RequestService requestService,
        PaymentService paymentService,
        ProfileService profileService
    )
    {
        _requestService = requestService;
        _paymentService = paymentService;
        _profileService = profileService;
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
        [FromForm] ICollection<Guid>? categoryId,
        [FromQuery] string? returnUrl
    )
    {
        if (!ModelState.IsValid)
        {
            return this.RedirectWithError();
        }

        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return this.RedirectWithError(error: ErrorStrings.SessionExpired);
        }

        try
        {
            var consumer = await _profileService.GetConsumer(accountId);
            if (consumer is null)
            {
                return this.RedirectWithError(error: ErrorStrings.NotAConsumer);
            }

            var request = await _requestService.CreateRequest(new()
            {
                Consumer = consumer.Id,
                Title = title,
                Description = description,
                Location = location,
                Budget = budget,
                DueDate = dueDate,
                RemoteEligible = remoteEligible,
            });

            if (categoryId is not null)
            {
                await _requestService.SetRequestCategories(request.Id, categoryId)
                    .ToListAsync();
            }

            return this.RedirectToReferrer(returnUrl ?? "/");
        }
        catch
        {
            return this.RedirectWithError(error: ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> GetRequest(
        [FromQuery] Guid? requestId
    )
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

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
                    return BadRequest();
                }
                var consumer = await _profileService.GetConsumer(accountId);
                if (consumer is null)
                {
                    return BadRequest();
                }
                return Ok(_requestService.GetRequests(consumer.Id));
            }
        }
        catch
        {
            return BadRequest();
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
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        return Ok(_requestService.FindRequests(keywords, categoryId, location));
    }

    [HttpPost(nameof(CompleteRequest))]
    [Authorize]
    public async Task<ActionResult> CompleteRequest(
        [FromBody, Required] Guid requestId,
        [FromQuery] string? returnUrl
    )
    {
        if (!ModelState.IsValid)
        {
            return this.RedirectWithError();
        }

        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return this.RedirectWithError(error: ErrorStrings.SessionExpired);
        }

        try
        {
            var provider = await _profileService.GetProvider(accountId);
            if (provider is null)
            {
                return this.RedirectWithError(error: ErrorStrings.NotAProvider);
            }

            var request = await _requestService.GetRequest(requestId);
            if (request is null
                || request.Selected?.Provider != provider.Id)
            {
                return this.RedirectWithError(error: ErrorStrings.InvalidRequest);
            }

            if (request.CompletedDate != null)
            {
                return this.RedirectWithError(error: ErrorStrings.RequestAlreadyComplete);
            }

            var providerPaymentMethod = await _paymentService.GetPaymentMethods(accountId)
                .FirstOrDefaultAsync();

            var consumer = (await _profileService.GetConsumer(request.Consumer))!;
            var consumerPaymentMethod = await _paymentService.GetPaymentMethods(consumer.Account)
                .FirstOrDefaultAsync();

            if (providerPaymentMethod is null || consumerPaymentMethod is null)
            {
                return this.RedirectWithError(error: ErrorStrings.NoPaymentMethod);
            }

            var completedRequest = await _requestService.CompleteRequest(requestId);
            if (completedRequest is null)
            {
                return this.RedirectWithError(error: ErrorStrings.RequestAlreadyComplete);
            }

            await _paymentService.Transfer(
                fromId: consumerPaymentMethod.Id,
                toId: providerPaymentMethod.Id,
                amount: request.Budget
            );

            return this.RedirectToReferrer(returnUrl ?? "/");
        }
        catch
        {
            return this.RedirectWithError(error: ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpPost(nameof(Offer))]
    [Authorize]
    public async Task<ActionResult> CreateOffer(
        [FromBody, Required] Guid requestId,
        [FromQuery] string? returnUrl
    )
    {
        if (!ModelState.IsValid)
        {
            return this.RedirectWithError();
        }

        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return this.RedirectWithError(error: ErrorStrings.SessionExpired);
        }

        try
        {
            var provider = await _profileService.GetProvider(accountId);
            if (provider is null)
            {
                return this.RedirectWithError(error: ErrorStrings.NotAProvider);
            }

            await _requestService.CreateOffer(new()
            {
                Provider = provider.Id,
                Request = requestId
            });

            return this.RedirectToReferrer(returnUrl ?? "/");
        }
        catch
        {
            return this.RedirectWithError(error: ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpPost($"{nameof(Offer)}/{nameof(SelectOffer)}")]
    public async Task<ActionResult> SelectOffer(
        [FromBody, Required] Guid requestId,
        [FromBody, Required] Guid offerId,
        [FromQuery] string? returnUrl
    )
    {
        if (!ModelState.IsValid)
        {
            return this.RedirectWithError();
        }

        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return this.RedirectWithError(error: ErrorStrings.SessionExpired);
        }

        try
        {
            var consumer = await _profileService.GetConsumer(accountId);
            if (consumer is null)
            {
                return this.RedirectWithError(error: ErrorStrings.NotAConsumer);
            }

            var request = await _requestService.GetRequest(requestId);
            if (request is null || request.Consumer != consumer.Id)
            {
                return this.RedirectWithError(error: ErrorStrings.InvalidRequest);
            }

            var offer = await _requestService.GetOffer(offerId);
            if (offer is null || offer.Request != requestId)
            {
                return this.RedirectWithError(error: ErrorStrings.InvalidOffer);
            }

            await _requestService.SelectOffer(offerId);
            return this.RedirectToReferrer(returnUrl ?? "/");
        }
        catch
        {
            return this.RedirectWithError(error: ErrorStrings.ErrorTryAgain);
        }
    }
}
