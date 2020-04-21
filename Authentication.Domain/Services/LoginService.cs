using Authentication.Domain.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Authentication.Domain.Services
{
    public class LoginService : ILoginService<PlatformUser>
    {
        private UserManager<PlatformUser> _userManager;
        private SignInManager<PlatformUser> _signInManager;

        public LoginService(UserManager<PlatformUser> userManager, SignInManager<PlatformUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<PlatformUser> FindByUsername(string username)
        {
            var user = new PlatformUser();

            if(username != null)
            {
                try
                {
                    user = await _userManager.FindByEmailAsync(username);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            return user;
        }

        public async Task<bool> ValidateCredentials(PlatformUser user, string password)
        {
            return await _userManager.CheckPasswordAsync(user, password);
        }

        public Task SignIn(PlatformUser user)
        {
            return _signInManager.SignInAsync(user, true);
        }

        public async Task SignInAsync(PlatformUser user, AuthenticationProperties properties, string authenticationMethod = null)
        {
            try
            {
                await _signInManager.SignInAsync(user, properties, authenticationMethod);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<SignInResult> PasswordSignInAsync(PlatformUser user, string password, bool lockoutUser)
        {
            return await _signInManager.PasswordSignInAsync(user, password, false, lockoutUser);
        }

        public async Task<PlatformUser> GetTwoFactorAuthenticationUserAsync()
        {
            return await _signInManager.GetTwoFactorAuthenticationUserAsync();
        }

        public async Task<SignInResult> TwoFactorAuthenticatorSignInAsync(string authenticatorCode)
        {
            return await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, false, false);
        }

        public async Task<SignInResult> TwoFactorRecoveryCodeSignInAsync(string recoveryCode)
        {
            return await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);
        }
    }
}
