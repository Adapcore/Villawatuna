﻿using Microsoft.AspNetCore.Mvc;
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
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace HotelManagement.Controllers
{
    public class AccountController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IMemberSignInManager _signInManager;
        private readonly IMemberService _memberService;


        public AccountController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IMemberManager memberManager,
            IMemberSignInManager signInManager,
            IMemberService memberService)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager;
            _signInManager = signInManager;
            _memberService = memberService;
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
            {
                //return Redirect(model.ReturnUrl ?? "/Internal/Invoices");


                if (result.Succeeded)
                {
                    var member1 = Services.MemberService.GetByUsername(model.Username);

                    // Get the User Type from custom dropdown property
                    var userTypeRaw = member1.GetValue<string>("userType") ?? string.Empty;
                    var roles = JsonSerializer.Deserialize<List<string>>(userTypeRaw);
                    string userType = roles?.FirstOrDefault() ?? string.Empty;


                    //// Get the Umbraco member user
                    //var user = await _memberManager.FindByNameAsync(model.Username);
                    //var userPrincipal = await _signInManager.CreateUserPrincipalAsync(user);

                    //// Add role claim from userType property
                    //var identity = userPrincipal.Identity as System.Security.Claims.ClaimsIdentity;
                    //if (!string.IsNullOrWhiteSpace(userType) && identity != null)
                    //{
                    //    identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, userType));
                    //}

                    //// Sign in with the enriched identity
                    //await _signInManager.SignInAsync(user, model.RememberMe);


                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, model.Username),
                        new Claim(ClaimTypes.Role, userType) // <-- make role claim
                    };

                    var identity = new ClaimsIdentity(claims, "UmbracoMember");
                    await HttpContext.SignInAsync(new ClaimsPrincipal(identity));


                    // Redirect based on role
                    if (string.Equals(userType, "Admin", StringComparison.OrdinalIgnoreCase))
                        return Redirect("/Internal/Invoices");
                    else if (string.Equals(userType, "User", StringComparison.OrdinalIgnoreCase))
                        return Redirect("/Internal/Invoices");
                    else
                        return Redirect("/");
                }


                //// Retrieve user type from member properties
                //var memberEntity = _memberService.GetById(int.Parse(member.Id));
                //var userType = memberEntity.GetValue<string>("userType") ?? "User"; // default fallback

                //// Redirect based on user type
                //if (userType.Contains("Admin"))
                //    return Redirect(model.ReturnUrl ?? "/Internal/Invoices");

                //return Redirect(model.ReturnUrl ?? "/Home");
            }
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
