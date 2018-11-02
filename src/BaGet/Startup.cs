using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BaGet.Configurations;
using BaGet.Core.Configuration;
using BaGet.Core.Entities;
using BaGet.Extensions;
using BaGet.Web.Extensions;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BaGet
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureBaGet(Configuration, httpServices: true);

            services.AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
                .AddBasic(options =>
                {
                    options.AllowInsecureProtocol = true;
                    options.Realm = "baget";
                    options.Events = new BasicAuthenticationEvents
                    {
                        OnValidateCredentials = context =>
                        {
                            var bagetOptions = context.HttpContext.RequestServices.GetService<IOptions<BaGetOptions>>().Value;

                            if (context.Username == bagetOptions.Username && context.Password == bagetOptions.Password)
                            {
                                var claims = new[]
                                {
                                    new Claim(
                                        ClaimTypes.NameIdentifier, 
                                        context.Username, 
                                        ClaimValueTypes.String, 
                                        context.Options.ClaimsIssuer),
                                    new Claim(
                                        ClaimTypes.Name, 
                                        context.Username, 
                                        ClaimValueTypes.String, 
                                        context.Options.ClaimsIssuer)
                                };

                                context.Principal = new ClaimsPrincipal(
                                    new ClaimsIdentity(claims, context.Scheme.Name));
                                context.Success();
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseStatusCodePages();
            }

            // Run migrations automatically
            var scopeFactory = app.ApplicationServices
                .GetRequiredService<IServiceScopeFactory>();

            using (var scope = scopeFactory.CreateScope())
            {
                scope.ServiceProvider
                    .GetRequiredService<IContext>()
                    .Database
                    .Migrate();
            }

            app.UseAuthentication();

            app.UseForwardedHeaders();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseCors(ConfigureCorsOptions.CorsPolicy);

            app.UseMvc(routes =>
            {
                routes
                    .MapServiceIndexRoutes()
                    .MapPackagePublishRoutes()
                    .MapSearchRoutes()
                    .MapRegistrationRoutes()
                    .MapPackageContentRoutes();
            });
        }
    }
}
