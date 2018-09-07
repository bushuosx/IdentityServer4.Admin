using IdentityServer4;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skoruba.IdentityServer4.Admin.Configuration
{
    public class MyClients
    {
        public static List<Client> GetMyClients()
        {
            return new List<Client>
            {
                ///////////////////////////////////////////
                // MVC Hybrid Flow Samples
                //////////////////////////////////////////
                new Client
                                {
                                        ClientId = "mvc",
                                        ClientName = "MVC Hybrid",
                                        ClientUri = "http://identityserver.io",

                                        ClientSecrets =
                                        {
                                                new Secret("secret".Sha256())
                                        },

                                        AllowedGrantTypes = GrantTypes.Hybrid,
                                        AllowAccessTokensViaBrowser = false,

                                        RedirectUris = { "http://localhost:5002/signin-oidc" },
                                        FrontChannelLogoutUri = "http://localhost:5002/signout-oidc",
                                        PostLogoutRedirectUris = { "http://localhost:5002/signout-callback-oidc" },

                                        AllowOfflineAccess = true,

                                        AllowedScopes =
                                        {
                                                IdentityServerConstants.StandardScopes.OpenId,
                                                IdentityServerConstants.StandardScopes.Profile,
                                                IdentityServerConstants.StandardScopes.Email,
                                                "api1", "api2.read_only",
                                        },
                                }
            };
        }
    }
}
