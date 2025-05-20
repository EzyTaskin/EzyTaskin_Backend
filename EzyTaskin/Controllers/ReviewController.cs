using System.ComponentModel.DataAnnotations;
using EzyTaskin.Services;
using EzyTaskin.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzyTaskin.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReviewController : ControllerBase
{
    private readonly ReviewService _reviewService;
    private readonly NotificationService _notificationService;
    private readonly ProfileService _profileService;
    private readonly RequestService _requestService;

    public ReviewController(
        ReviewService reviewService,
        NotificationService notificationService,
        ProfileService profileService,
        RequestService requestService
    )
    {
        _reviewService = reviewService;
        _notificationService = notificationService;
        _profileService = profileService;
        _requestService = requestService;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult> AddReview(
        [FromForm, Required] Guid requestId,
        [FromForm, Required, Range(1, 5, ErrorMessage = "Invalid rating.")] int rating,
        [FromForm] string? description
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
            if (request is null
                || request.Selected is null
                || request.Consumer.Id != consumer.Id
                || request.CompletedDate == null)
            {
                return BadRequest(error: ErrorStrings.InvalidRequest);
            }

            var review = await _reviewService.AddReview(new()
            {
                Request = requestId,
                Rating = rating,
                Description = description
            });

            if (review is null)
            {
                return BadRequest(error: ErrorStrings.ErrorTryAgain);
            }

            await _notificationService.SendNotification(new()
            {
                Timestamp = DateTime.UtcNow,
                Account = request.Selected.Provider.Account,
                Title = "Reviews",
                Content =
                    $"A review of your performance in \"{request.Title}\"" +
                        " has been added."
            });

            return Ok(review);
        }
        catch
        {
            return BadRequest(error: ErrorStrings.ErrorTryAgain);
        }
    }

    [HttpGet]
    public async Task<ActionResult> GetReview(
        [FromQuery] Guid? requestId,
        [FromQuery] Guid? providerId
    )
    {
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
