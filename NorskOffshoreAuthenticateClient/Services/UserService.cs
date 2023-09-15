using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graph.Models;
using NorskOffshoreAuthenticateClient.Models;
using NorskOffshoreAuthenticateClient.Utils;

namespace NorskOffshoreAuthenticateClient.Services
{
    public static class UsersServiceExtensions
    {
        public static void AddUsersService(this IServiceCollection services)
        {
            // https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            services.AddHttpClient<IUserService, UserService>();
        }
    }
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly string _NorskOffshoreAuthenticateServiceScope = string.Empty;
        private readonly string _UsersBaseAddress = string.Empty;
        private readonly string _RedirectUri = string.Empty;
        private readonly string _ApiRedirectUri = string.Empty;
        private readonly ITokenAcquisition _tokenAcquisition;

        public UserService(ITokenAcquisition tokenAcquisition, HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _tokenAcquisition = tokenAcquisition;
            _NorskOffshoreAuthenticateServiceScope = configuration["users:NorskOffshoreAuthenticateServiceScope"];
            _UsersBaseAddress = configuration["users:UsersBaseAddress"];
            _RedirectUri = configuration["RedirectUri"];
            _ApiRedirectUri = configuration["users:AdminConsentRedirectApi"];

            if (!string.IsNullOrEmpty(_UsersBaseAddress))
            {
                if (!_UsersBaseAddress.EndsWith("/"))
                {
                    _UsersBaseAddress = _UsersBaseAddress+"/";
                }
            }
        }

        public async Task<UserItem> GetLoggedInUser()
        {
            await PrepareAuthenticatedClient();
            var response = await _httpClient.GetAsync($"{_UsersBaseAddress}api/users/getloggedingraphuser");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<User>(content);

                return new UserItem()
                {
                    Id = user.UserPrincipalName,
                    Username = user.UserPrincipalName,
                    DisplayName = user.DisplayName,
                    GivenName = user.GivenName,
                    Enabled = user.AccountEnabled,
                    Email = user.Mail,
                    MobilePhone = user.MobilePhone,
                    Country = user.Country,
                    StreetAddress = user.StreetAddress,
                    Photo = user.Photo
                };
            }
            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }

        public async Task<IEnumerable<string>> GetAllGraphUsersAsync()
        {
            await PrepareAuthenticatedClient();
            var response = await _httpClient.GetAsync($"{ _UsersBaseAddress}api/users/getallgraphusers");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                IEnumerable<string> users = JsonConvert.DeserializeObject<IEnumerable<string>>(content);
                return users;
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                 HandleChallengeFromWebApi(response);
            }

            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }

        public async Task<UserItem> GetAsync(int id)
        {
            await PrepareAuthenticatedClient();
            var response = await _httpClient.GetAsync($"{ _UsersBaseAddress}api/users/{id}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                UserItem todo = JsonConvert.DeserializeObject<UserItem>(content);

                return todo;
            }

            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }

        private async Task PrepareAuthenticatedClient()
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { _NorskOffshoreAuthenticateServiceScope });
            Debug.WriteLine($"access token-{accessToken}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// If signed-in user does not have consent for a permission on the Web API, for instance "user.read.all" in this sample, 
        /// then Web API will throw MsalUiRequiredException. The response contains the details about consent Uri and proposed action. 
        /// </summary>
        /// <param name="response"></param>
        /// <exception cref="WebApiMsalUiRequiredException"></exception>
        private void HandleChallengeFromWebApi(HttpResponseMessage response)
        {
            //proposedAction="consent"
            List<string> result = new List<string>();
            AuthenticationHeaderValue bearer = response.Headers.WwwAuthenticate.First(v => v.Scheme == "Bearer");
            IEnumerable<string> parameters = bearer.Parameter.Split(',').Select(v => v.Trim()).ToList();
            string proposedAction = GetParameter(parameters, "proposedAction");

            if (proposedAction == "consent")
            {
                string consentUri = GetParameter(parameters, "consentUri");

                var uri = new Uri(consentUri);

                //Set values of query string parameters
                var queryString = System.Web.HttpUtility.ParseQueryString(uri.Query);
                queryString.Set("redirect_uri", _ApiRedirectUri);
                queryString.Add("prompt", "consent");
                queryString.Add("state", _RedirectUri);
                //Update values in consent Uri
                var uriBuilder = new UriBuilder(uri);
                uriBuilder.Query = queryString.ToString();
                var updateConsentUri = uriBuilder.Uri.ToString();
                result.Add("consentUri");
                result.Add(updateConsentUri);

                //throw custom exception
                throw new WebApiMsalUiRequiredException(updateConsentUri);
            }
        }

        private static string GetParameter(IEnumerable<string> parameters, string parameterName)
        {
            int offset = parameterName.Length + 1;
            return parameters.FirstOrDefault(p => p.StartsWith($"{parameterName}="))?.Substring(offset)?.Trim('"');
        }
    }
}