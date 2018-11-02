using System;
using BaGet.Configuration;
using BaGet.Core;
using BaGet.Core.Server.Extensions;
using BaGet.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            // In production, the UI files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "BaGet.UI/build";
            });

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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var options = Configuration.Get<BaGetOptions>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseStatusCodePages();
            }

            app.UseSpaStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseForwardedHeaders();
            app.UsePathBase(options.PathBase);
            app.UseCors(ConfigureCorsOptions.CorsPolicy);
            app.UseOperationCancelledMiddleware();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapServiceIndexRoutes();
                endpoints.MapPackagePublishRoutes();
                endpoints.MapSymbolRoutes();
                endpoints.MapSearchRoutes();
                endpoints.MapPackageMetadataRoutes();
                endpoints.MapPackageContentRoutes();
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "../BaGet.UI";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
    }
}
