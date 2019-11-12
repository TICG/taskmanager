using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Portal.Helpers;

namespace Portal.Pages
{

    // NOTE - no auth on in portal.azure.com or locally (other than anon)

    [Authorize]
    public class GregTestModel : PageModel
    {
        public string Username { get; set; }

        public void OnGet()
        {
            var graphUser = this.User.ToGraphUserAccount();

            var authenticationProvider = CreateAuthorizationProvider();
            var graphClient = new GraphServiceClient(authenticationProvider);

            var graphResult = graphClient.Users[graphUser.ObjectId].Request().GetAsync().Result;

            Username = graphResult.DisplayName;
        }

        private static IAuthenticationProvider CreateAuthorizationProvider()
        {
            var clientId = "ID HERE"; // applicationId
            var clientSecret = "SECRET HERE"; // applicationSecret from AAD app registrations secrets

            // Needs to be conditional whether localhost or in Azure (both URLs need to be in the app registration for reply URL)
            // e.g. for when running local
            var redirectUri = "https://localhost:44308/signin-oidc";

            var authority = $"https://login.microsoftonline.com/9134ca48-663d-4a05-968a-31a42f0aed3e/v2.0";

            List<string> scopes = new List<string>();
            scopes.Add("https://graph.microsoft.com/.default");

            var cca = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithAuthority(authority)
                .WithRedirectUri(redirectUri)
                .WithClientSecret(clientSecret)
                .Build();

            return new MsalAuthenticationProvider(cca, scopes.ToArray());
        }
    }
}