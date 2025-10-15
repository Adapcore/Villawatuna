using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Core.Cache;
using Microsoft.Extensions.Logging;
using HotelManagement.Models.ViewModels;
using System.Threading.Tasks;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Common;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;

namespace HotelManagement.Controllers
{
    public class AccountController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IMemberSignInManager _signInManager;

        public AccountController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IMemberManager memberManager,
            IMemberSignInManager signInManager)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            // Check if already logged in
            var currentMember = await _memberManager.GetCurrentMemberAsync();

            if (currentMember != null)
            {
                // Already logged in, redirect to home or dashboard
                return Redirect(returnUrl ?? "/Internal/Invoices");
            }

            // Otherwise show login page
            return View("~/Views/Account/Login.cshtml", new LoginViewModel { ReturnUrl = returnUrl });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Account/Login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Account/Login.cshtml", model);

            var member = await _memberManager.FindByNameAsync(model.Username);
            if (member == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View("~/Views/Account/Login.cshtml", model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, false);

            if (result.Succeeded)
                return Redirect(model.ReturnUrl ?? "/Internal/Invoices");

            ModelState.AddModelError("", "Invalid username or password.");
            return View("~/Views/Account/Login.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Account/Logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/Account/Login");
        }
    }
}
