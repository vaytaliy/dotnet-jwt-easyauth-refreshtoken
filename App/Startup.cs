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
using App.Data;
using App.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyAuth.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;

namespace App
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton<DbConnectionContext>();
            services.AddTransient<IAuthUserRepository, UserRepository>();

            PasswordHasher.SetSalt(Configuration["Salt"]);
            //[A] It's up to user how secrets are brought in

            //Make sure the following keys are set in your secrets store or environment:

            //SecretAuthKey (make sure string is long enough,256b minimum for hashing algorithm)
            //Salt (make sure string is long enough, 256b minimum for hashing algorithm)
            //EmailSecretAuthKey (make sure string is long enough, 256b minimum for hashing algorithm)
            //EmailUser (Email account username used for sending emails to users)
            //EmailPassword (Email account password used for sending emails to users)
            //EmailSecretRecoveryKey (make sure string is long enough, 256b minimum for hashing algorithm)

            services.AddAuthOptions(o =>
            {
                o.SecretAuthKey = Configuration["SecretAuthKey"];
                o.EmailSecretAuthKey = Configuration["EmailSecretAuthKey"];
                o.EmailSecretRecoveryKey = Configuration["EmailSecretRecoveryKey"];
                o.EmailPassword = Configuration["EmailPassword"];
            });
            //specify your auth options, add jwt bearer parameters that you'll be using
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJWTEasyAuth(Configuration, o =>
                {
                    o.SecretAuthKey = Configuration["SecretAuthKey"];
                }
                );

            //provide your data provider for simple auth, it can be any database and logic or involved dependencies
            //must be set up manually
            services.AddSimpleAuthTokenProvider();

            //Enable default auth controller
            services.AddAuthenticationController();


            services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "JWT",
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
                                    Id = "Bearer"
                                }
                            },
                            new string[]{}
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                IdentityModelEventSource.ShowPII = true;
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Authentication API v1");
                    c.RoutePrefix = "api";
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
            
            app.UseAuthorization();
            


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //Enable authentication controller
            app.UseAuthenticationController(Configuration["AuthSettings:AuthURL"]);
        }
    }
}
