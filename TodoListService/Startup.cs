using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TodoListService
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
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; /// here you defined your
            })                                                                        /// authorization setup to use
            //add the handler of the jwt bearer token which validates bearer token.
            //it validates the token using same client Id, and secret key
            //which are the same used by protected API's consumer via whether
            //user grant flow (Cookie) or confidential client flow (Secrets)
            .AddAzureAdBearer(options => Configuration.Bind("AzureAd", options));     /// bearer token for access
                                                                                      /// instead of Cookie..

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseApiVersioning();

            app.UseAuthentication();
            app.UseMvc();
        }
    }

    static class ConfigureServicesExtensions
    {
        public static IServiceCollection AddAPIVersioning(this IServiceCollection services)
        {
            
            services.AddApiVersioning(a =>
            {
                a.AssumeDefaultVersionWhenUnspecified = true;
                a.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
                a.Conventions = new Microsoft.AspNetCore.Mvc.Versioning.Conventions.ApiVersionConventionBuilder();
            });

            return services;
        }
    }
}
