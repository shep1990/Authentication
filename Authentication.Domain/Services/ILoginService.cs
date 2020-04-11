using Authentication.Domain.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Authentication.Domain.Services
{
    public interface ILoginService<T>
    {
        Task<bool> ValidateCredentials(T user, string password);

        Task<T> FindByUsername(string user);

        Task SignIn(T user);

        Task SignInAsync(T user, AuthenticationProperties properties, string authenticationMethod = null);

        Task<SignInResult> PasswordSignInAsync(T user, string password, bool lockOutUser);

        Task<PlatformUser> GetTwoFactorAuthenticationUserAsync();

        Task<SignInResult> TwoFactorAuthenticatorSignInAsync(string authenticatorCode);

        Task<SignInResult> TwoFactorRecoveryCodeSignInAsync(string recoveryCode);
    }
}
