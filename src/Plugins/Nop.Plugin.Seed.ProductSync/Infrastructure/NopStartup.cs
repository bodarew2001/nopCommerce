using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Seed.ProductSync.Clients;
using Nop.Plugin.Seed.ProductSync.Factories;
using Nop.Plugin.Seed.ProductSync.Services;
using Nop.Services.Catalog;

namespace Nop.Plugin.Seed.ProductSync.Infrastructure;

public class NopStartup:INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IProductSyncService, ProductSyncService>();
        services.AddScoped<IInfigoClient,InfigoClient>();
        services.AddScoped<IProductSyncFactory, ProductSyncFactory>();
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    public int Order => 1;
}