using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Authetication.WebApiClient
{
    public static class AuthWebApiClientFactory
    {
        public static IAuthWebApiClient Create(string url)
        {
            return new AuthWebApiClient(new RestClient(url));
        }
    }
}
