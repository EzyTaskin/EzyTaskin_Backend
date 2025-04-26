using System.ComponentModel.DataAnnotations;
using EzyTaskin.Data.Model;
using EzyTaskin.Services;
using EzyTaskin.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzyTaskin.Controllers;

[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly MessageService _messageService;

    public MessageController(MessageService messageService)
    {
        _messageService = messageService;
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
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return BadRequest();
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
            return message;
        }
        catch
        {
            return BadRequest();
        }
    }
}
