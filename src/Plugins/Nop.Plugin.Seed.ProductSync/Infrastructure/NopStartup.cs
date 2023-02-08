using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Seed.ProductSync.Services;

namespace Nop.Plugin.Seed.ProductSync.Infrastructure;

public class NopStartup:INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ProductSyncService>();
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    public int Order { get; }
}