﻿using Serilog;
using Serilog.Events;
using System.Diagnostics.CodeAnalysis;
using TinyFp.Extensions;
using TinyFpTest.Configuration;
using TinyFpTest.Services;
using TinyFpTest.Services.Api;
using TinyFpTest.Services.Details;
using TinyFpTest.Services.DetailsDrivenPorts;
using static TinyFp.Extensions.Functional;

namespace TinyFpTest.Complex
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        const string Microsoft = "Microsoft";
        const string System = "System";

        protected IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public virtual void ConfigureServices(IServiceCollection services)
            => services
                .Tee(InitializeControllers)
                .Tee(InitializeConfigurations)
                .Tee(InitializeCache)
                .Tee(InitializeApiClient)
                .Tee(InitializeSerachService)
                .Tee(InitializeDetailsDrivenPort)
                .Tee(InitializeSerilog);

        private void InitializeControllers(IServiceCollection services)
            => services
                .Tee(_ => _.AddControllers());

        private void InitializeCache(IServiceCollection services)
            => services
                .Tee(_ => _.AddSingleton<ICache, Cache>());

        private IServiceCollection InitializeApiClient(IServiceCollection services)
            => services
                .Tee(_ => _.AddHttpClient())
                .Tee(_ => _.AddSingleton<IApiClient>(_ =>
                            new ApiClient(() => _.GetRequiredService<IHttpClientFactory>().CreateClient())));

        private static void InitializeSerachService(IServiceCollection services)
            => services
                .Tee(_ => _.AddSingleton<SearchService>())
                .Tee(_ => _.AddSingleton(_ =>
                                new CachedSearchService(_.GetRequiredService<ICache>(),
                                                        _.GetRequiredService<SearchService>())))
                .Tee(_ => _.AddSingleton(_ =>
                                new ValidationSearchService(_.GetRequiredService<CachedSearchService>())))
                .Tee(_ => _.AddSingleton<ISearchService>(_ =>
                                new LoggedSearchService(_.GetRequiredService<ValidationSearchService>(),
                                                        _.GetRequiredService<Serilog.ILogger>())));

        private IServiceCollection InitializeConfigurations(IServiceCollection services)
            => Configuration
                .GetSection(typeof(ProductsApiConfiguration).Name)
                .Get<ProductsApiConfiguration>()
                .Map(services.AddSingleton)
                .Tee(_ => Configuration
                            .GetSection(typeof(ProductDetailsApiConfiguration).Name)
                            .Get<ProductDetailsApiConfiguration>()
                            .Map(services.AddSingleton));


        private void InitializeSerilog(IServiceCollection services)
            => Configuration
                .GetSection(typeof(SerilogConfiguration).Name).Get<SerilogConfiguration>()
                .Map(_ => (serilogConfig: _, loggerConfig: new LoggerConfiguration()))
                .Tee(_ => InitializeConfiguration(_.loggerConfig, _.serilogConfig))
                .Map(_ => _.loggerConfig.CreateLogger())
                .Tee(_ => services.AddSingleton<Serilog.ILogger>(_));

        private void InitializeDetailsDrivenPort(IServiceCollection services)
            => Configuration
                .GetSection(typeof(DetailsDrivenPortConfiguration).Name).Get<DetailsDrivenPortConfiguration>()
                .Map(_ => _.Adapter switch
                            {
                                DetailsDrivenPorts.DetailsDrivenPortApi => RegisterDetailsDrivenPortsAdapterApi(services),
                                DetailsDrivenPorts.DetailDrivenPortDb => RegisterDetailsDrivenPortsAdapterDb(services),
                                _ => throw new DetailsDrivenPortNotImplementedException(_)
                            })
                .Tee(_ => _.AddSingleton<IDetailsDrivenPort>(_ =>
                                new LoggedDetailsDrivenPort(_.GetRequiredService<ValidationDetailsDrivenPort>(),
                                                            _.GetRequiredService<Serilog.ILogger>())));

        private IServiceCollection RegisterDetailsDrivenPortsAdapterApi(IServiceCollection services)
            => services
                .Tee(_ => _.AddSingleton<DetailsDrivenPortAdapterApi>())
                .Tee(_ => _.AddSingleton(_ =>
                            new ValidationDetailsDrivenPort(_.GetRequiredService<DetailsDrivenPortAdapterApi>())));

        private IServiceCollection RegisterDetailsDrivenPortsAdapterDb(IServiceCollection services)
            => services
                .Tee(_ => _.AddSingleton<IDetailsRepository, DetailsRepository>())
                .Tee(_ => _.AddSingleton<DetailsDrivenPortAdapterDb>())
                .Tee(_ => _.AddSingleton(_ =>
                            new ValidationDetailsDrivenPort(_.GetRequiredService<DetailsDrivenPortAdapterDb>())));

        private static (LoggerConfiguration, SerilogConfiguration) InitializeConfiguration(LoggerConfiguration loggerConfig, 
                                                                                           SerilogConfiguration serilogConfig)
            => loggerConfig
                .Tee(_ => _.Enrich.WithProperty(nameof(serilogConfig.Environment), serilogConfig.Environment))
                .Tee(_ => _.Enrich.WithProperty(nameof(serilogConfig.System), serilogConfig.System))
                .Tee(_ => _.Enrich.WithProperty(nameof(serilogConfig.Customer), serilogConfig.Customer))
                .Tee(_ => _.Enrich.FromLogContext())
                .Tee(_ => 
                {
                    var parseSucceeded = Enum.TryParse(serilogConfig.LogEventLevel, true, out LogEventLevel logEventLevel);
                    _.MinimumLevel.Is(parseSucceeded ? logEventLevel : LogEventLevel.Debug);
                })
                .Tee(_ => _.MinimumLevel.Override(Microsoft, serilogConfig.MicrosoftLogEventLevel))
                .Tee(_ => _.MinimumLevel.Override(System, serilogConfig.SystemLogEventLevel))
                .Map(_ => (loggerConfig, serilogConfig));

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            => app
                .UseRouting()
                .UseEndpoints(_ => _.MapControllers());
    }
}
