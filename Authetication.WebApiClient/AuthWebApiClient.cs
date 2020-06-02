using Authentication.Domain.Dto;
using RestSharp;
using System.Threading.Tasks;

namespace Authetication.WebApiClient
{
    public class AuthWebApiClient : WebApiClientBase, IAuthWebApiClient
    {
        public AuthWebApiClient(IRestClient client) : base(client)
        {
        }

        public async Task<RegisterDto> CreateProfile(RegisterDto registerViewModel)
        {
            return await PostAsync<RegisterDto>(registerViewModel, $"/api/SignUp/SignUpConfirmation");
        }
    }
}
