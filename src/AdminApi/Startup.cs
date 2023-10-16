#region Usings

using System.Net.Http.Headers;
using System.Reflection;
using AspNetCoreRateLimit;
using FluentValidation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Scrutor;
using Serilog;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Common.Const;
using UltimatePlaylist.Common.Mvc.Enums;
using UltimatePlaylist.Common.Mvc.Extensions;
using UltimatePlaylist.Common.Mvc.Filters;
using UltimatePlaylist.Common.Mvc.Utils;
using UltimatePlaylist.Database.Infrastructure;
using UltimatePlaylist.Database.Infrastructure.Context;
using UltimatePlaylist.Database.Infrastructure.Repositories;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Services.All;
using UltimatePlaylist.Services.Common.Interfaces.Song;
using UltimatePlaylist.Services.Common.Interfaces.User;
using UltimatePlaylist.Services.Common.Interfaces.UserSong;
using UltimatePlaylist.Services.Song;
using UltimatePlaylist.Services.UserManagement;
using UltimatePlaylist.Services.UserSong.Repositories;
using Microsoft.Extensions.Configuration;
using UltimatePlaylist.Services.Common.Interfaces.Ticket;
using Newtonsoft.Json;

#endregion

namespace UltimatePlaylist.AdminApi
{
    public class Startup
    {
        private const string MigrationsAssemblySuffixName = "Database.Migrations";
        private const string ServicesAssemblySuffixName = "Services";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private string MigrationsAssemblyName => $"{typeof(Startup).Namespace.Split('.')[0]}.{MigrationsAssemblySuffixName}";

        private string ServicesAssemblyName => $"{typeof(Startup).Namespace.Split('.')[0]}.{ServicesAssemblySuffixName}";

        private Assembly[] ServiceAssemblies => typeof(ServicesAssembly)
            .Assembly
            .GetReferencedAssemblies()
            .Where(an => an.FullName.StartsWith(ServicesAssemblyName))
            .Select(a => Assembly.Load(a))
            .ToArray();

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();

            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

            services
                .AddControllers(opt =>
                {
                    opt.Filters.Add(typeof(ValidationFilter));
                })
                .AddFluentValidation(opt =>
                {
                    opt.RegisterValidatorsFromAssemblies(ServiceAssemblies.Append(typeof(Startup).Assembly));
                    opt.LocalizationEnabled = false;
                })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.SetupJsonSettings();
                })
                .ConfigureApiBehaviorOptions(opt =>
                {
                    opt.SuppressModelStateInvalidFilter = true;
                });

            var connectionString = Configuration.GetConnectionString(Config.ConnectionString);
            services.UseDatabase<EFContext>(connectionString, MigrationsAssemblyName);

            var jwtOptions = services
                .BindConfigurationWithValidation<AuthConfig>(Configuration, "Auth");

            services.AddAuthentication(jwtOptions);
            //services.AddHangfire(connectionString);
            services.AddSwaggerConfiguration(ApiType.AdminPanel);
            services.AddFluentValidationRulesToSwagger();
            services.AddHealthChecks();
            services.AddApplicationInsightsTelemetry();

            var allowedOrigins = new List<string>();
            Configuration.GetSection("AllowedOrigins").Bind(allowedOrigins);

            services.AddCors(opt => opt.AddPolicy(
                "AllowSelected",
                builder => builder
                .WithOrigins(allowedOrigins.ToArray())
                .AllowAnyMethod()
                .AllowAnyHeader()));

            // Services
            services.AddTransient(typeof(Lazy<>), typeof(LazyServiceProvider<>));

            services.Scan(scan =>
                scan.FromAssemblies(ServiceAssemblies)
                .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service")))
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Handler")))
                .UsingRegistrationStrategy(RegistrationStrategy.Append)
                .AsImplementedInterfaces()
                .AddClasses(classess => classess.Where(type => type.Name.EndsWith("Job")))
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsSelf()
                .WithScopedLifetime()
                .AddClasses(classes => classes.Where(type => type.Name.StartsWith("InMemory") && type.Name.EndsWith("Store")))
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsImplementedInterfaces()
                .WithSingletonLifetime());

            // Repositories
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(IReadOnlyRepository<>), typeof(ReadOnlyRepository<>));
            services.AddScoped<IUserSongRepository, UserSongReposiotry>();
            services.AddScoped<IPlaylistSQLRepository, PlaylistSQLRepository>(x => new PlaylistSQLRepository(connectionString));
            services.AddScoped<ISongStatisticsProcedureRepository, SongStatisticsProcedureRepository>();
            services.AddScoped<IUserManagementProcedureRepository, UserManagementProcedureRepository>();
            services.AddScoped<ITicketProcedureRepository, TicketProcedureRepository>();
            services.AddScoped<ISongStatisticsProcedureRepository, SongStatisticsProcedureRepository>();

            // Rate Limit
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            // Distributed cache
            services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();

            // Automapper
            var automapperAssemblies = ServiceAssemblies.ToList();
            automapperAssemblies.Add(typeof(Startup).Assembly);
            automapperAssemblies.Add(typeof(InfrastructureAssembly).Assembly);

            services.AddAutoMapper(automapperAssemblies);

            // Config
            services.AddOptions<EmailConfig>()
                .Bind(Configuration.GetSection("Email"))
                .ValidateDataAnnotations();

            services.AddOptions<FilesConfig>()
                .Bind(Configuration.GetSection("Files"))
                .ValidateDataAnnotations();

            services
                 .AddOptions<AzureStorageConfig>()
                 .Bind(Configuration.GetSection("AzureStorage"))
                 .ValidateDataAnnotations();
            services
                .AddOptions<AzureMediaServicesConfig>()
                .Bind(Configuration.GetSection("AzureMediaServices"))
                .ValidateDataAnnotations();

            services
                .AddOptions<AzureEventGridConfig>()
                .Bind(Configuration.GetSection("AzureEventGrid"))
                .ValidateDataAnnotations();

            services.AddOptions<PlaylistConfig>()
               .Bind(Configuration.GetSection("Playlist"))
               .ValidateDataAnnotations();

            services.AddOptions<FirebaseConfig>()
               .Bind(Configuration.GetSection("Firebase"))
               .ValidateDataAnnotations();

            services.AddOptions<NotificationConfig>()
                .Bind(Configuration.GetSection("Notification"))
                .ValidateDataAnnotations();

            var appleMusic = services.BindConfigurationWithValidation<AppleMusicProviderConfig>(Configuration, "AppleApi");
            services.AddHttpClient(
              Config.AppleHttpClient,
              client =>
              {
                  client.BaseAddress = new Uri(appleMusic.Url);
                  client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
              });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseIpRateLimiting();

            app.UseExceptionMiddleware();
            app.UseCors("AllowSelected");
            app.UseSerilogRequestLogging();
            app.SetupSwaggerAndHealth(ApiType.AdminPanel, Configuration.GetValue<bool>("EnableSwagger"));
            //app.SetupHangfire();
            app.SetupApi();
        }
    }
}
