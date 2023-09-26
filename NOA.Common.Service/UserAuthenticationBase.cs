using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NOA.Common.Service.Utils;

namespace NOA.Common.Service
{
    public class UserAuthenticationBase
    {
        protected readonly ITokenAcquisition _tokenAcquisition;
        protected readonly string _NorskOffshoreAuthenticateServiceScope = string.Empty;
        protected readonly string _RedirectUri = string.Empty;
        protected readonly string _ApiRedirectUri = string.Empty;
        protected readonly HttpClient _httpClient;

        public UserAuthenticationBase(
            HttpClient httpClient,
            ITokenAcquisition tokenAcquisition,
            IConfiguration configuration
            ) {
            _tokenAcquisition = tokenAcquisition;
            _NorskOffshoreAuthenticateServiceScope = configuration["users:NorskOffshoreAuthenticateServiceScope"];
            _RedirectUri = configuration["RedirectUri"];
            _ApiRedirectUri = configuration["users:AdminConsentRedirectApi"];
            _httpClient = httpClient;
        }

        protected async Task PrepareAuthenticatedClient()
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { _NorskOffshoreAuthenticateServiceScope });
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        protected void HandleChallengeFromWebApi(HttpResponseMessage response)
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

        protected static string GetParameter(IEnumerable<string> parameters, string parameterName)
        {
            int offset = parameterName.Length + 1;
            return parameters.FirstOrDefault(p => p.StartsWith($"{parameterName}="))?.Substring(offset)?.Trim('"');
        }
    }
}
