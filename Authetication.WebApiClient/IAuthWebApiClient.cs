using Authentication.Domain.Dto;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Authetication.WebApiClient
{
    public interface IAuthWebApiClient
    {
        Task<RegisterDto> CreateProfile(RegisterDto registerViewModel);
    }
}
