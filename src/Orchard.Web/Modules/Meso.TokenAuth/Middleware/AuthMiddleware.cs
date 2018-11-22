using Meso.TokenAuth.Providers;
using Microsoft.Owin;
using Microsoft.Owin.Security.OAuth;
using Orchard.Owin;
using Orchard.Security;
using Orchard;
using Owin;
using System;
using System.Collections.Generic;

namespace Meso.TokenAuth.Middleware {
    public class AuthMiddleware : IOwinMiddlewareProvider {
        private readonly IWorkContextAccessor _workContextAccessor;

        public AuthMiddleware(
            IWorkContextAccessor workContextAccessor) {

            _workContextAccessor = workContextAccessor;
        }

        public IEnumerable<OwinMiddlewareRegistration> GetOwinMiddlewares() {
            return new[] {
                new OwinMiddlewareRegistration {
                    Priority = "1",
                    Configure = app => {
                        var oAuthOptions = new OAuthAuthorizationServerOptions
                        {
                            TokenEndpointPath = new PathString("/api/auth/token"),
                            Provider = new AuthProvider(_workContextAccessor),
                            AccessTokenExpireTimeSpan = TimeSpan.FromDays(30),
                            AllowInsecureHttp = true
                        };

                        // Enable the application to use bearer tokens to authenticate users
                        app.UseOAuthAuthorizationServer(oAuthOptions);
                        app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());
                    }
                },
                new OwinMiddlewareRegistration {
                    Priority = "2",
                    Configure = app => {
                        app.Use(async (context, next) => {
                            var workContext = _workContextAccessor.GetContext();
                            var authenticationService = workContext.Resolve<IAuthenticationService>();
                            var membershipService = workContext.Resolve<IMembershipService>();
                            
                            var orchardUser = membershipService.GetUser(context.Request.User.Identity.Name);
                            authenticationService.SetAuthenticatedUserForRequest(orchardUser);

                            await next();
                        });
                    }
                }
            };
        }
    }
}