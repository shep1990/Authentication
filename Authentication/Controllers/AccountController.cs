using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Authentication.Domain.Dto;
using Authentication.Domain.Model;
using Authentication.Domain.Services;
using Authentication.Email;
using Authentication.Models;
using Authentication.Resources;
using Authetication.WebApiClient;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using log4net;
using MailKit.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;

namespace Authentication.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILoginService<PlatformUser> _loginService;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly UserManager<PlatformUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IAuthWebApiClient _authWebApiClient;
        private readonly IEmailService _emailService;
        private readonly IEmailConfiguration _emailConfiguration;
        private readonly ILog _logger = LogManager.GetLogger(typeof(AccountController));

        public AccountController(
            ILoginService<PlatformUser> loginService,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            UserManager<PlatformUser> userManager,
            IConfiguration configuration,
            IAuthWebApiClient authWebApiClient,
            IEmailService emailService,
            IEmailConfiguration emailConfiguration
        )
        {
            _loginService = loginService;
            _interaction = interaction;
            _clientStore = clientStore;
            _userManager = userManager;
            _configuration = configuration;
            _authWebApiClient = authWebApiClient;
            _emailService = emailService;
            _emailConfiguration = emailConfiguration;
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
            var result = new IdentityResult();

            if (ModelState.IsValid)
            {
                var userObj = new PlatformUser
                {
                    UserName = model.Email,
                    Email = model.Email
                };

                result = await _userManager.CreateAsync(userObj, model.Password);

                if (result.Succeeded)
                {
                    try
                    {
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(userObj);
                        var confirmationLink = Url.Action(nameof(ConfirmEmail), "Account", new { token, email = model.Email }, Request.Scheme);
                        var message = new EmailMessage()
                        {
                            ToAddresses = new EmailAddress
                            {
                                Name = model.Name,
                                Address = model.Email
                            },
                            FromAddresses = new EmailAddress
                            {
                                Name = "Social Network",
                                Address = _emailConfiguration.Username
                            },
                            Content = confirmationLink,
                            Subject = "Confirm Email"
                        };

                        _emailService.Send(message);

                        var today = DateTime.Today;
                        var age = today.Year - model.DateOfBirth.Value.Year;
                        if (model.DateOfBirth.Value.Date > today.AddYears(-age)) age--;

                        var user = await _loginService.FindByUsername(model.Email);

                        await _authWebApiClient.CreateProfile(new RegisterDto
                        {
                            Id = user.Id,
                            Name = model.Name,
                            DateOfBirth = model.DateOfBirth.Value,
                            Age = age,
                            Email = model.Email
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(string.Format("There was an error while creating the user profile {0}", ex.Message));
                        throw ex;
                    }

                    return RedirectToAction("Login", "Account");
                }

                if (result.Errors.Count() > 0)
                {
                    AddErrors(result);
                }

            }
            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return View("Error");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            return View(result.Succeeded ? nameof(ConfirmEmail) : "Error");
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
                if(error.Code == "DuplicateUserName")
                {
                    ModelState.AddModelError("Email", error.Description);
                }
            }
        }
    }
}