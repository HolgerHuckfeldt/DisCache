using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace DisCache.Attributes
{
    public class CustomAuthorizeAttribute : TypeFilterAttribute
    {
        public CustomAuthorizeAttribute()
            : base(typeof(CustomAuthorizeAsyncActionFilterAttribute))
        {
            //Arguments = new object[] { policyName };
        }
    }

    public class CustomAuthorizeAsyncActionFilterAttribute : Attribute, IAsyncActionFilter
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly AuthSettings _authSettings;
        private readonly string _policyName;

        public CustomAuthorizeAsyncActionFilterAttribute(
            IAuthorizationService authorizationService,
            IOptions<AuthSettings> authSettings,
            string policyName)
        {
            _authorizationService = authorizationService;
            _authSettings = authSettings.Value;
            _policyName = policyName;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (_authSettings.AuthEnabled)
            {
                var authorizationResult = await _authorizationService.AuthorizeAsync(context.HttpContext.User, context.ActionArguments, _policyName);
                if (authorizationResult.Succeeded)
                {
                    await next.Invoke();
                }
                else
                {
                    context.Result = new ForbidResult();
                }
            }
            else
            {
                await next.Invoke();
            }
        }
    }
}
