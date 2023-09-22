using Microsoft.Graph.Models;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;

namespace NOA.Common.Service
{
    public class GraphServiceProxy : IGraphServiceProxy
    {

        private readonly string[] _graphScopes;
        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ITokenAcquisition _tokenAcquisition;

        public GraphServiceProxy()
        {
            _graphScopes = null;
            _consentHandler = null;
            _graphServiceClient = null;
            _tokenAcquisition = null;
        }

        public GraphServiceProxy(
            ITokenAcquisition tokenAcquisition,
            IConfiguration configuration,
            GraphServiceClient graphServiceClient,
            MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler)
        {
            _tokenAcquisition = tokenAcquisition;

            _graphScopes = configuration
                .GetValue<string>("DownstreamApi:Scopes")?
                .Split(' ');

            this._graphServiceClient = graphServiceClient;
            if (this._graphServiceClient == null)
                throw new NullReferenceException("The GraphServiceClient has not been added to the services collection during the ConfigureServices()");

            this._consentHandler = consentHandler;
            if (this._consentHandler == null)
                throw new NullReferenceException("The MicrosoftIdentityConsentAndConditionalAccessHandler has not been added to the services collection during the ConfigureServices()");

        }

        public async Task<User> GetGraphApiUser(string filter)
        {
            // we use MSAL.NET to get a token to call the API On Behalf Of the current user
            try
            {
                // Call the Graph API and retrieve the user's profile.
                var users =
                    await CallGraphWithCAEFallback(
                        async () =>
                        {
                            try
                            {
                                var result = await _graphServiceClient.Users.GetAsync(r =>
                                {
                                    r.QueryParameters.Filter = filter;
                                    r.QueryParameters.Select = new string[]
                                    {
                                            "id", "userPrincipalName", "displayName",
                                            "givenName", "accountEnabled", "mail",
                                            "mobilePhone", "country", "streetAddress",
                                            "photo"
                                    };

                                }
                                );

                                return result;
                            }
                            catch (Exception ex)
                            {
                                throw;
                            }
                        }
                    );

                if (users?.Value != null)
                {
                    return users.Value?.FirstOrDefault();
                }

                throw new Exception();
            }
            catch (MsalUiRequiredException ex)
            {
                _tokenAcquisition.ReplyForbiddenWithWwwAuthenticateHeader(_graphScopes, ex);
                throw;
            }
            catch (MicrosoftIdentityWebChallengeUserException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<string>> GetAllGraphApiUsers()
        {
            // We use MSAL.NET to get a token to call the API On Behalf Of the current user
            try
            {
                // Call the Graph API and retrieve the user's profile.
                var users =
                await CallGraphWithCAEFallback(
                async () =>
                    {
                        return await _graphServiceClient.Users.GetAsync(r =>
                        {
                            r.QueryParameters.Filter = "accountEnabled eq true";
                            r.QueryParameters.Select = new string[] { "id", "userPrincipalName" };
                        }
                                                              );

                    }
                );

                if (users != null)
                {
                    return users.Value.Select(x => x.UserPrincipalName).ToList();
                }
                throw new Exception("Got null from Microsoft Graph Api");
            }
            catch (MsalUiRequiredException ex)
            {
                _tokenAcquisition.ReplyForbiddenWithWwwAuthenticateHeader(_graphScopes, ex);
                throw ex;
            }
        }

        /// <summary>
        /// Calls a Microsoft Graph API, but wraps and handle a CAE exception, if thrown
        /// </summary>
        /// <typeparam name="T">The type of the object to return from MS Graph call</typeparam>
        /// <param name="graphAPIMethod">The graph API method to call.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Unknown error just occurred. Message: {ex.Message}</exception>
        /// <autogeneratedoc />
        private async Task<T> CallGraphWithCAEFallback<T>(Func<Task<T>> graphAPIMethod)
        {
            try
            {
                return await graphAPIMethod();
            }
            catch (ServiceException ex) when (ex.Message.Contains("Continuous access evaluation resulted in claims challenge"))
            {
                try
                {
                    // Get challenge from response of Graph API
                    var claimChallenge = WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(ex.ResponseHeaders);

                    _consentHandler.ChallengeUser(_graphScopes, claimChallenge);
                }
                catch (Exception ex2)
                {
                    _consentHandler.HandleException(ex2);
                }

                return default;
            }
        }
    }
}
