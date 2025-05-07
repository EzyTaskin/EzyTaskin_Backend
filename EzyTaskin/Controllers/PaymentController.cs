using System.ComponentModel.DataAnnotations;
using EzyTaskin.Data.Model;
using EzyTaskin.Services;
using EzyTaskin.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzyTaskin.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _paymentService;

    public PaymentController(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    [Authorize]
    public ActionResult<IAsyncEnumerable<PaymentMethod>> GetPaymentMethods()
    {
        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return BadRequest(error: ErrorStrings.SessionExpired);
        }

        return Ok(_paymentService.GetPaymentMethods(accountId));
    }

    [HttpPost("Card")]
    [Authorize]
    public async Task<ActionResult<CardPaymentMethod>> AddCard(
        [FromForm, Required, CreditCard] string number,
        [FromForm, Required]
        [RegularExpression(@"(?:0\d|1(?:0|1|2))/\d\d", ErrorMessage = "Invalid expiry.")]
        string expiry,
        [FromForm, Required, RegularExpression(@"\d\d\d", ErrorMessage = "Invalid CVV.")]
        string cvv,
        [FromForm, Required] string name
    )
    {
        var accountId = this.TryGetAccountId();
        if (accountId == Guid.Empty)
        {
            return BadRequest(error: ErrorStrings.SessionExpired);
        }

        try
        {
            return Ok(await _paymentService.AddCard(accountId, number, expiry, cvv, name));
        }
        catch
        {
            return BadRequest(error: ErrorStrings.ErrorTryAgain);
        }
    }
}
