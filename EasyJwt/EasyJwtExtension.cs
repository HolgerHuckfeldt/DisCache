using EasyJwt.Infrastructure;
using EasyJwt.Models;
using EasyJwt.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyJwt
{
    public static class EasyJwtExtension
    {

        public static IServiceCollection AddEasyJwt(this IServiceCollection services, IConfiguration config)
        {
            //if (useInternalUserManager)
            //    services.AddSingleton<IUserService, UserService>();
            var jwtTokenConfig = config.GetSection("jwtTokenConfig").Get<JwtTokenConfig>();


            if (jwtTokenConfig.IntegratedUserService)
            {
                var userList = new List<JwtUser>();
                var userSection = config.GetSection("jwtTokenConfig:User");

                foreach (var u in userSection.GetChildren())
                {
                    var key = u.Key;
                    var v = u.Value;
                    userList.Add(new JwtUser()
                    {
                        Name = u.Key, Password = u.Value
                    });
                }

                services.AddSingleton<IUserService>(
                    x=> new UserService(
                        x.GetRequiredService<ILogger<UserService>>(), userList));
            }

            services.AddSingleton<IJwtAuthManager, JwtAuthManager>();
            services.AddHostedService<JwtRefreshTokenCache>();

            services.AddSingleton(jwtTokenConfig);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = true;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtTokenConfig.Issuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtTokenConfig.Secret)),
                    ValidAudience = jwtTokenConfig.Audience,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });
            return services;
        }
    }
}
