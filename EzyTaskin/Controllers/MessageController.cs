using System.ComponentModel.DataAnnotations;
using EzyTaskin.Data.Model;
using EzyTaskin.Services;
using EzyTaskin.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzyTaskin.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MessageController : ControllerBase
{
    private readonly MessageService _messageService;
    private readonly NotificationService _notificationService;

    public MessageController(
        MessageService messageService,
        NotificationService notificationService
    )
    {
        _messageService = messageService;
        _notificationService = notificationService;
    }

    [HttpGet]
    [Authorize]
    public ActionResult<IAsyncEnumerable<Message>> GetMessages(
        [FromQuery][Required] Guid peerId,
        [FromQuery] DateTime? after
    )
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty || peerId == Guid.Empty)
        {
            return BadRequest();
        }

        return Ok(_messageService.GetMessages(accountId, peerId, after));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Message>> SendMessage(
        [FromQuery][Required] Guid peerId,
        [FromBody][Required] string content
    )
    {
        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return BadRequest(error: ErrorStrings.SessionExpired);
        }

        try
        {
            var message = await _messageService.SendMessage(new()
            {
                Timestamp = DateTime.UtcNow,
                Sender = accountId,
                Receiver = peerId,
                Content = content
            });

            if (message is null)
            {
                return BadRequest(error: ErrorStrings.ErrorTryAgain);
            }

            await _notificationService.SendNotification(new()
            {
                Timestamp = DateTime.UtcNow,
                Account = peerId,
                Title = "Messages",
                Content = "You have an incoming message."
            });

            return Ok(message);
        }
        catch
        {
            return BadRequest(error: ErrorStrings.ErrorTryAgain);
        }
    }
}
