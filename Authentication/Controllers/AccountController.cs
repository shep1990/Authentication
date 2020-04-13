﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Authentication.Domain.Model;
using Authentication.Domain.Services;
using Authentication.Models;
using Authentication.Resources;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Authentication.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILoginService<PlatformUser> _loginService;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly UserManager<PlatformUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly UrlEncoder _urlEncoder;

        public AccountController(
            ILoginService<PlatformUser> loginService,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            UserManager<PlatformUser> userManager,
            IConfiguration configuration,
            UrlEncoder urlEncoder
        )
        {
            _loginService = loginService;
            _interaction = interaction;
            _clientStore = clientStore;
            _userManager = userManager;
            _configuration = configuration;
            _urlEncoder = urlEncoder;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            if (context?.IdP != null)
            {
                throw new NotImplementedException("External login is not implemented!");
            }

            var vm = await BuildLoginViewModelAsync(returnUrl, context);

            return View(vm);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _loginService.FindByUsername(model.Email);
                var login = new Microsoft.AspNetCore.Identity.SignInResult();

                if (user != null)
                {
                    login = await _loginService.PasswordSignInAsync(user, model.Password, Convert.ToBoolean(_configuration.GetSection("LockOutIfCredentialsAreIncorrect").Value));
                }
                if (login.Succeeded)
                {
                    var tokenLifetime = _configuration.GetValue("TokenLifetimeMinutes", 120);

                    var props = new AuthenticationProperties
                    {
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(tokenLifetime),
                        AllowRefresh = true,
                        RedirectUri = model.ReturnUrl
                    };

                    await _loginService.SignInAsync(user, props);

                    if (_interaction.IsValidReturnUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                }

                if (login.IsLockedOut)
                {
                    ModelState.AddModelError("Password", String.Format(Strings.NumberOfLoginAttemptsExceeded, _configuration.GetSection("DefaultLockoutTimeSpanValue").Value));
                }
                else
                {
                    ModelState.AddModelError("Password", Strings.EmailAndPasswordIncorrect);
                }
            }

            var vm = await BuildLoginViewModelAsync(model);

            return View(vm);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl)
        {
            var registerViewModel = new RegisterViewModel
            {
                ReturnUrl = returnUrl
            };
            return View(registerViewModel);
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var existingUser = await _loginService.FindByUsername(model.Email);

            if (ModelState.IsValid)
            {
                var user = new PlatformUser
                {
                    UserName = model.Email,
                    Email = model.Email
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (result.Errors.Count() > 0)
                {
                    AddErrors(result);

                    return View(model);
                }
            }
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", Strings.DuplicateEmail);
                return View();
            }

            if (model.ReturnUrl != null)
            {
                if (HttpContext.User.Identity.IsAuthenticated)
                {
                    return Redirect(model.ReturnUrl);
                }
                else if (ModelState.IsValid)
                {
                    return RedirectToAction("login", "account", new { model.ReturnUrl });
                }
                return View(model);
            }

            return RedirectToAction("Login", "Account");
        }


        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl, AuthorizationRequest context)
        {
            if (context?.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.ClientId);
                if (client != null)
                {
                    var allowLocal = client.EnableLocalLogin;
                }
            }

            return new LoginViewModel
            {
                ReturnUrl = returnUrl
            };
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginViewModel model)
        {
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl, context);
            vm.Email = model.Email;
            return vm;
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}