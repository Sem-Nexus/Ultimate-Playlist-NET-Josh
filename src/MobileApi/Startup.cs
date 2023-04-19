#region Usings

using System.Net.Http.Headers;
using System.Reflection;
using AspNetCoreRateLimit;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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
using UltimatePlaylist.Database.Infrastructure.Seeding;
using UltimatePlaylist.Games.Interfaces;
using UltimatePlaylist.Games.Services;
using UltimatePlaylist.Services.All;
using UltimatePlaylist.Services.Common.Interfaces.UserSong;
using UltimatePlaylist.Services.Games;
using UltimatePlaylist.Services.Notification;
using UltimatePlaylist.Services.UserSong.Repositories;

#endregion

namespace UltimatePlaylist.MobileApi
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
                .AddNewtonsoftJson(options => options.SerializerSettings.SetupJsonSettings())
                .ConfigureApiBehaviorOptions(opt =>
                {
                    opt.SuppressModelStateInvalidFilter = true;
                });

            var connectionString = Configuration.GetConnectionString(Config.ConnectionString);
            services.UseDatabase<EFContext>(connectionString, MigrationsAssemblyName);

            //services.AddDbContext<EFContext>(options => options.UseSqlServer(connectionString));

            services.AddDbContext<EFContext>(options => options.UseSqlServer(connectionString, builder =>
            {
                builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
            }));
            var jwtOptions = services
                .BindConfigurationWithValidation<AuthConfig>(Configuration, "Auth");

            services.AddAuthentication(jwtOptions);
            services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.ClientId = Configuration.GetValue<string>("Google:ClientId");
                options.ClientSecret = Configuration.GetValue<string>("Google:ClientSecret");
            })
            .AddApple(options =>
            {
                options.ClientId = Configuration.GetValue<string>("Apple:ClientId");
                options.TeamId = Configuration.GetValue<string>("Apple:TeamId");
                options.KeyId = Configuration.GetValue<string>("Apple:KeyId");
                //options.P8KeyFilePath = Configuration["Apple:P8KeyFilePath"];
            });

            services.AddHangfire(connectionString);
            services.AddSwaggerConfiguration(ApiType.Mobile);
            services.AddFluentValidationRulesToSwagger();
            services.AddHealthChecks();
            services.AddApplicationInsightsTelemetry();
            services.AddHttpClient();

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

            // Games lib
            services.AddScoped<ILotteryService, LotteryService>();
            services.AddScoped<IRaffleService, RaffleService>();

            // Database seed
            services.AddScoped<DatabaseInitializer>();

            // Repositories
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(IReadOnlyRepository<>), typeof(ReadOnlyRepository<>));
            services.AddScoped<IUserSongRepository, UserSongReposiotry>();
            services.AddScoped<IPlaylistSQLRepository, PlaylistSQLRepository>(x=> new PlaylistSQLRepository(connectionString));

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
            services
                 .AddOptions<AzureStorageConfig>()
                 .Bind(Configuration.GetSection("AzureStorage"))
                 .ValidateDataAnnotations();

            services.AddOptions<EmailConfig>()
                .Bind(Configuration.GetSection("Email"))
                .ValidateDataAnnotations();

            services.AddOptions<DatabaseSeedConfig>()
               .Bind(Configuration.GetSection("DatabaseSeed"))
               .ValidateDataAnnotations();

            services.AddOptions<DSPConfig>()
               .Bind(Configuration.GetSection("DSP"))
               .ValidateDataAnnotations();

            services.AddOptions<SpotifyConfig>()
               .Bind(Configuration.GetSection("Spotify"))
               .ValidateDataAnnotations();

            services.AddOptions<TicketConfig>()
               .Bind(Configuration.GetSection("Ticket"))
               .ValidateDataAnnotations();

            services.AddOptions<PlaylistConfig>()
               .Bind(Configuration.GetSection("Playlist"))
               .ValidateDataAnnotations();

            services.AddOptions<FilesConfig>()
                .Bind(Configuration.GetSection("Files"))
                .ValidateDataAnnotations();

            services.AddOptions<GamesConfig>()
                .Bind(Configuration.GetSection("Games"))
                .ValidateDataAnnotations();

            services.AddOptions<FirebaseConfig>()
                .Bind(Configuration.GetSection("Firebase"))
                .ValidateDataAnnotations();

            services.AddOptions<NotificationConfig>()
                .Bind(Configuration.GetSection("Notification"))
                .ValidateDataAnnotations();

            var redisConnectionString = Configuration.GetValue<string>("Redis:ConnectionString");
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);

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
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IRecurringJobManager recurringJobManager,
            IOptions<GamesConfig> gamesConfig,
            IOptions<NotificationConfig> notificationConfig,
            IOptions<PlaylistConfig> playlistConfig)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseIpRateLimiting();

            app.UseExceptionMiddleware();
            app.UseCors("AllowSelected");            
            app.UseSerilogRequestLogging();
            app.SetupSwaggerAndHealth(ApiType.Mobile, Configuration.GetValue<bool>("EnableSwagger"));
            app.SetupHangfire();
            app.SetupApi();
            app.UseAuthentication();
            app.UseAuthorization();
            /*
            DailyCashDrawingScheduler.RemoveDaliCashDrawingJobs(recurringJobManager);
            UltimatePayoutGameScheduler.RemoveUltimatePayoutJob(recurringJobManager);
            UltimatePayoutGameRewardScheduler.RemoveUltimatePayoutGameRewardJob(recurringJobManager);
            */
            if (gamesConfig.Value.RunGames)
            {
                UltimatePayoutGameRewardScheduler.ScheduleUltimatePayoutGameRewardJob(recurringJobManager, playlistConfig.Value.TimeZone);
                DailyCashDrawingScheduler.SchedulealiCashDrawingJob(recurringJobManager, gamesConfig.Value, playlistConfig.Value);
                UltimatePayoutGameScheduler.ScheduleUltimatePayoutJob(recurringJobManager, gamesConfig.Value, playlistConfig.Value);
            }
        }
    }
}
