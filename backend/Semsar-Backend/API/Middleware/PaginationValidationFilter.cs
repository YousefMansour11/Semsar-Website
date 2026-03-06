using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace API.Middleware;

public class PaginationValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var kvp in context.ActionArguments)
        {
            if ((kvp.Key == "page" || kvp.Key == "PageNumber") && kvp.Value is int page && page < 1)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    success = false,
                    message = "Page number must be greater than 0",
                    errorCode = "invalid_pagination"
                });
                return;
            }

            if ((kvp.Key == "pageSize" || kvp.Key == "PageSize") && kvp.Value is int pageSize)
            {
                if (pageSize < 1 || pageSize > 100)
                {
                    context.Result = new BadRequestObjectResult(new
                    {
                        success = false,
                        message = "Page size must be between 1 and 100",
                        errorCode = "invalid_pagination"
                    });
                    return;
                }
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
