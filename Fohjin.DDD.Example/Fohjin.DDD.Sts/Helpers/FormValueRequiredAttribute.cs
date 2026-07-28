using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Fohjin.DDD.Sts.Helpers;

// Adapted from the OpenIddict team's own Velusia sample
// (github.com/openiddict/openiddict-samples, samples/Velusia/Velusia.Server/Helpers).
public sealed class FormValueRequiredAttribute(string name) : ActionMethodSelectorAttribute
{
    private readonly string _name = name;

    public override bool IsValidForRequest(RouteContext context, ActionDescriptor action)
    {
        if (string.Equals(context.HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(context.HttpContext.Request.Method, "HEAD", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(context.HttpContext.Request.Method, "DELETE", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(context.HttpContext.Request.Method, "TRACE", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrEmpty(context.HttpContext.Request.ContentType))
        {
            return false;
        }

        if (!context.HttpContext.Request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !string.IsNullOrEmpty(context.HttpContext.Request.Form[_name]);
    }
}
