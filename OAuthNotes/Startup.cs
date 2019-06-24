using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace OAuthNotes
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                // If an authentication cookie is present, use it to get authentication information
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                // If authentication is required, and no cookie is present, use Okta (configured below) to sign in
                options.DefaultChallengeScheme = "Okta";
            })
            .AddCookie() // cookie authentication middleware first
            .AddOAuth("Okta", options =>
            {
                // Oauth authentication middleware is second

                var oktaDomain = Configuration.GetValue<string>("Okta:OktaDomain");

                // When a user needs to sign in, the will be redirected to the authorize endpoint
                options.AuthorizationEndpoint = $"{oktaDomain}/oauth2/default/v1/authorize";

                // Okta's OAuth server is OpenID compliant, so request the standard openid
                // scopes when redirecting to the authorization endpoint
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");

                // Afterthe user signs in, an authorization code will be sent to a callback
                // in this app. The OAuth middleware will intercept it
                options.CallbackPath = new PathString("/authorization-code/callback");

                // The OAuth middleware will send the ClientId, ClientSecret, and the
                // authorization code to the token endpoint, and get an access token in return
                options.ClientId = Configuration.GetValue<string>("Okta:ClientId");
                options.ClientSecret = Configuration.GetValue<string>("Okta:ClientSecret");
                options.TokenEndpoint = $"{oktaDomain}/oauth2/default/v1/token";

                // Below we call the userinfo endpoint to get information about the user
                options.UserInformationEndpoint = $"{oktaDomain}/oauth2/default/v1/userinfo";
                
                // Describe how to map the user info we receive to user claims
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
                options.ClaimActions.MapJsonKey(ClaimTypes.Name, "given_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = async context =>
                    {
                        // Get user info from the userinfo endpoint and use it to populate user claims
                        var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                        var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                        response.EnsureSuccessStatusCode();

                        var user = JObject.Parse(await response.Content.ReadAsStringAsync());

                        context.RunClaimActions(user);
                    }
                };
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
