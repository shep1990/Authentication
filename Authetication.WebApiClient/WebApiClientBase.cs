using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Authetication.WebApiClient
{
    public abstract class WebApiClientBase
    {
        protected readonly IRestClient _client;

        public WebApiClientBase(IRestClient client)
        {
            _client = client;
        }

        public async Task<T> PostAsync<T>(string resource, string accessToken = null)
        {
            var request = new RestRequest(resource);

            if (accessToken != null)
            {
                _client.AddDefaultHeader("Authorization", string.Format("Bearer {0}", accessToken));
            }

            IRestResponse<T> response = await _client.ExecutePostTaskAsync<T>(request);

            return HandleResponse(response);
        }

        public async Task<T> PostAsync<T>(object body, string resource, string accessToken = null)
        {
            var request = new RestRequest(resource);
            request.AddJsonBody(body);

            if (accessToken != null)
            {
                _client.AddDefaultHeader("Authorization", string.Format("Bearer {0}", accessToken));
            }

            IRestResponse<T> response = await _client.ExecutePostTaskAsync<T>(request);

            return HandleResponse(response);
        }

        public async Task<T> GetAsync<T>(string resource, Dictionary<string, object> parameters = null, string accessToken = null)
        {
            var request = new RestRequest(resource);

            if (accessToken != null)
            {
                _client.AddDefaultHeader("Authorization", string.Format("Bearer {0}", accessToken));
            }

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    request.AddParameter(parameter.Key, parameter.Value, ParameterType.UrlSegment);
                }
            }

            IRestResponse<T> response = await _client.ExecuteGetTaskAsync<T>(request);
            return HandleResponse(response);
        }

        public async Task<T> DeleteAsync<T>(object body, string resouce, string accessToken = null)
        {
            var request = new RestRequest(resouce, Method.DELETE);
            request.AddJsonBody(body);

            if (accessToken != null)
            {
                _client.AddDefaultHeader("Authorization", string.Format("Bearer {0}", accessToken));
            }

            IRestResponse<T> response = await _client.ExecuteTaskAsync<T>(request);
            return HandleResponse(response);
        }

        public async Task<T> PutAsync<T>(object body, string resource, string accessToken = null)
        {
            var request = new RestRequest(resource, Method.PUT);
            request.AddJsonBody(body);

            if (accessToken != null)
            {
                _client.AddDefaultHeader("Authorization", string.Format("Bearer {0}", accessToken));
            }

            IRestResponse<T> response = await _client.ExecuteTaskAsync<T>(request);
            return HandleResponse(response);
        }

        private T HandleResponse<T>(IRestResponse<T> response, string accessToken = null)
        {
            if (response.IsSuccessful)
            {
                return response.Data;
            }
            else
            {
                throw new Exception(response.ErrorMessage, response.ErrorException);
            }
        }
    }
}
