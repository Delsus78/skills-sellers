using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace skills_sellers.Helpers;

public class AppExceptionFiltersAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context) 
    {
        if (context.Exception is AppException exception)
        {
            context.Result = new JsonResult(exception.Message)
            {
                StatusCode = exception.ErrorCode
            };
        }
    }
}