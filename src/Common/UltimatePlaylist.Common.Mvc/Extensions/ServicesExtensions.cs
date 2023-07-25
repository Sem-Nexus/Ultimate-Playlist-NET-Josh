#region Usings

using System;
using System.Collections.Generic;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Common.Mvc.Attributes;
using UltimatePlaylist.Common.Mvc.Enums;
using UltimatePlaylist.Common.Mvc.Helpers;
using UltimatePlaylist.Common.Mvc.Swagger;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity;
using UltimatePlaylist.Database.Infrastructure.Stores;
using static UltimatePlaylist.Common.Mvc.Consts.Consts;

#endregion

namespace UltimatePlaylist.Common.Mvc.Extensions
{
    public static class ServicesExtensions
    {
        #region Swagger

        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services, ApiType apiType)
        {
            services.AddSwaggerGen(options =>
            {
                var projectName = $"{typeof(ServicesExtensions).Namespace.Split('.')[0]} {apiType}";

                foreach (var group in GetApiGroups(apiType))
                {
                    options.SwaggerDoc(group.Value, new OpenApiInfo() { Title = $"{projectName} {group.Key}", Version = group.Value });
                }

                options.OperationFilter<PagedAttributeCheckFilter>();
                options.DocumentFilter<DateTimeDocumentFilter>();
                options.DocumentFilter<LowercaseDocumentFilter>();
                options.DescribeAllParametersInCamelCase();

                options.MapType<FileContentResult>(() => new OpenApiSchema
                {
                    Type = "file",
                });

                options.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Description = "Please enter into field the word 'Bearer' following by space and JWT",
                        Name = "Authorization",
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "bearer",
                    });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                  {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = "Bearer",
                                Type = ReferenceType.SecurityScheme,
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    },
                  });
            });

            services.AddSwaggerGenNewtonsoftSupport();

            return services;
        }

        #endregion

        #region Hangfire
        public static IServiceCollection AddHangfire(this IServiceCollection services, string connectionString)
        {
            services.AddHangfire(x => x.UseSqlServerStorage(connectionString,
                new SqlServerStorageOptions
                {                 
                    //CommandTimeout = TimeSpan.FromMinutes(30),

                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UsePageLocksOnDequeue = false,
                    DisableGlobalLocks = false
                }));
            services.AddHangfireServer();
            return services;
        }

        #endregion

        #region Auth
        public static IServiceCollection AddAuthentication(this IServiceCollection services, AuthConfig jwtOptions)
        {
            services.AddIdentity<User, Role>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 4;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequiredUniqueChars = 0;
            })
            .AddRoleStore<PlatformRoleStore>()
            .AddUserStore<PlatformUserStore>()
            .AddDefaultTokenProviders();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = TokenValidation.GetTokenValidation(jwtOptions);
            });

            return services;
        }

        #endregion

        #region Database

        public static IServiceCollection UseDatabase<T>(this IServiceCollection services, string connectionString, string migrationAssemblyName)
            where T : DbContext
        {
            services.AddDbContext<T>(opt =>
            {
                opt.UseSqlServer(connectionString, builder => builder.MigrationsAssembly(migrationAssemblyName));
            });

            services.AddScoped<EFTransactionAttribute>();
            return services;
        }

        #endregion

        #region Private

        private static IDictionary<string, string> GetApiGroups(ApiType apiType) => apiType switch
        {
            ApiType.Mobile => MobileApiGroups.GetNameValueDictionary(),
            ApiType.AdminPanel => AdminApiGroups.GetNameValueDictionary(),
            _ => throw new Exception("Incorrect API type!")
        };
        #endregion
    }
}
