using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace EzyTaskin.Utils;

public static class ControllerExtensions
{
    public static RedirectResult RedirectWithQuery(this ControllerBase controller,
        string returnUrl, IEnumerable<KeyValuePair<string, object?>> args)
    {
        return controller.RedirectImpl(returnUrl, null, args);
    }

    public static RedirectResult RedirectToReferrer(this ControllerBase controller,
        string returnUrl)
    {
        return controller.RedirectImpl(
            returnUrl,
            controller.Request.GetTypedHeaders().Referer,
            null
        );
    }

    public static RedirectResult RedirectToReferrerWithQuery(this ControllerBase controller,
        string returnUrl, IEnumerable<KeyValuePair<string, object?>> args)
    {
        return controller.RedirectImpl(
            returnUrl,
            controller.Request.GetTypedHeaders().Referer,
            args
        );
    }

    public static RedirectResult RedirectWithError(
        this ControllerBase controller,
        string? error = null,
        string? returnUrl = null,
        bool includeForm = true
    )
    {
        returnUrl ??= controller.Request.GetTypedHeaders().Referer?.ToString();
        returnUrl ??= "/";

        if (!controller.ModelState.IsValid)
        {
            error ??= controller.ModelState.Values
                .SelectMany(v => v.Errors)
                .FirstOrDefault()
                ?.ErrorMessage;
        }

        var args = controller.Request.Query
            .SelectMany(q => q.Value.Select(v => new KeyValuePair<string, object?>(q.Key, v)));

        if (includeForm && controller.Request.HasFormContentType)
        {
            var form = controller.Request.Form
                .SelectMany(f => f.Value.Select(v => new KeyValuePair<string, object?>(f.Key, v)));

            args = args.Concat(form);
        }

        if (error is not null)
        {
            args = args.Concat([ new(nameof(error), error) ]);
        }

        return controller.RedirectImpl(returnUrl, null, args);
    }

    private static RedirectResult RedirectImpl(this ControllerBase controller,
        string returnUrl, Uri? referrer, IEnumerable<KeyValuePair<string, object?>>? args)
    {
        if (referrer is not null)
        {
            // Requested return is already absolute.
            returnUrl = new Uri(referrer, returnUrl).ToString();
        }

        if (args is not null)
        {
            returnUrl = QueryHelpers.AddQueryString(returnUrl, args.Select((kvp) =>
            {
                return new KeyValuePair<string, string?>(kvp.Key, kvp.Value switch
                {
                    bool b => b ? "true" : "false",
                    _ => kvp.Value?.ToString()
                });
            }));
        }

        return controller.Redirect(returnUrl);
    }

    public static Guid TryGetAccountId(this ControllerBase controller)
    {
        var value = controller.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(value))
        {
            return Guid.Empty;
        }

        if (!Guid.TryParse(value, out Guid userId))
        {
            return Guid.Empty;
        }

        return userId;
    }
}
