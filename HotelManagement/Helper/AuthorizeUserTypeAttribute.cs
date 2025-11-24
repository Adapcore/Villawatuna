using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Security;
using Serilog;
using System.Data;
using System;
using Umbraco.Cms.Core.Services;

namespace HotelManagement.Helper
{
    public class AuthorizeUserTypeAttribute : ActionFilterAttribute
    {
        private readonly string _userType;

        public AuthorizeUserTypeAttribute(string userType)
        {
            _userType = userType;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var signInManager = context.HttpContext.RequestServices.GetService<IMemberSignInManager>();
            var memberManager = context.HttpContext.RequestServices.GetService<IMemberManager>();
            var memberService = context.HttpContext.RequestServices.GetService<IMemberService>();

            var user = context.HttpContext.User.Identity?.Name;

            if (string.IsNullOrEmpty(user))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var memberIdentity = memberManager?.FindByNameAsync(user).Result;
            if (memberIdentity == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Get the underlying IMember
            var member = memberService.GetByKey(memberIdentity.Key);
            if (member == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Read custom property
            var rawType = member.GetValue<string>("userType") ?? "";
            var userType = rawType.Replace("[", "").Replace("]", "").Replace("\"", "").Trim();

            if (!string.Equals(userType, _userType, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}