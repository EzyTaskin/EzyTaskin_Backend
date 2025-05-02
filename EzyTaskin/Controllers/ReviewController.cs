using System.ComponentModel.DataAnnotations;
using EzyTaskin.Services;
using EzyTaskin.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzyTaskin.Controllers;

[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly ReviewService _reviewService;
    private readonly ProfileService _profileService;
    private readonly RequestService _requestService;

    public ReviewController(
        ReviewService reviewService,
        ProfileService profileService,
        RequestService requestService
    )
    {
        _reviewService = reviewService;
        _profileService = profileService;
        _requestService = requestService;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult> AddReview(
        [FromForm, Required] Guid requestId,
        [FromForm, Required, Range(1, 5, ErrorMessage = "Invalid rating.")] int rating,
        [FromForm] string? description,
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
            if (request is null
                || request.Consumer.Id != consumer.Id
                || request.CompletedDate == null)
            {
                return this.RedirectWithError(error: ErrorStrings.InvalidRequest);
            }

            await _reviewService.AddReview(new()
            {
                Request = requestId,
                Rating = rating,
                Description = description
            });

            return this.RedirectToReferrer(returnUrl ?? "/");
        }
        catch
        {
            return this.RedirectWithError(error: ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpGet]
    public async Task<ActionResult> GetReview(
        [FromQuery] Guid? requestId,
        [FromQuery] Guid? providerId
    )
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        if (requestId.HasValue && providerId.HasValue)
        {
            return BadRequest();
        }
        else if (requestId.HasValue)
        {
            var review =
                await _reviewService.GetReviews(requestId: requestId).SingleOrDefaultAsync();
            if (review is null)
            {
                return NotFound();
            }
            return Ok(review);
        }
        else if (providerId.HasValue)
        {
            return Ok(_reviewService.GetReviews(providerId: providerId));
        }
        else
        {
            return BadRequest();
        }
    }
}
