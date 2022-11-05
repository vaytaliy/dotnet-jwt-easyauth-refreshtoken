using EasyAuth;
using EasyAuth.Utils.PasswordUtil;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Recipes.Data;
using Recipes.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Recipes
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            PasswordHasher.SetSalt(configuration["Salt"]);
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllersWithViews();
            services.AddControllers();

            //specify your auth options, add jwt bearer parameters that you'll be using
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJWTEasyAuth(o => {
                o.Audience = Configuration["AuthSettings:Audience"];
                o.Authority = Configuration["AuthSettings:Audience"];
                o.AuthKey = Configuration["SecretAuthKey"];
            });

            services.AddSingleton<RecipesContext>();
            services.AddTransient<UserRepository>();

            //provide your data provider for simple auth, it can be any database and logic or involved dependencies
            //must be set up manually
            services.AddTransient<ISimpleAuthDataAccess, MyJWTDataAccess>();
            services.AddSimpleAuthTokenProvider(o =>
            {
                o.Authority = Configuration["AuthSettings:Authority"];
                o.Audience = Configuration["AuthSettings:Audience"];
                o.Salt = Configuration["Salt"];
                o.AuthKey = Configuration["SecretAuthKey"];
                o.EmailVerificationKey = Configuration["EmailSecretAuthKey"];
                o.AuthTokenExpirationMinutes = int.Parse(Configuration["AuthSettings:AuthTokenExpirationMinutes"]);
                o.EmailTokenExpirationMinutes = int.Parse(Configuration["AuthSettings:EmailTokenExpirationMinutes"]);
                o.SendCookies = true; //not applicable to external apis
                o.SendTokensInResponseHeader = true;
            });
            services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    Description = "Authorization using the Bearer scheme."
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                          new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "bearer"
                                }
                            },
                            Array.Empty<string>()
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Recipes API V1");
                    c.RoutePrefix = "api_doc";
                });

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

            app.UseRouting();

            
            app.UseAuthentication();
            app.UseTokenRefreshing();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapControllerRoute(
                //    name: "default",
                //    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllers();
            });
        }
    }
}
