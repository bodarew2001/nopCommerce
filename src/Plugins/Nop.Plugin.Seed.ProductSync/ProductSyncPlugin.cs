using System.Collections.Generic;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Svg.FilterEffects;
using Task = System.Threading.Tasks.Task;

namespace Nop.Plugin.Seed.ProductSync;

public class ProductSyncPlugin:BasePlugin
{
    private readonly IWebHelper _webHelper;
    private readonly ILocalizationService _localizationService;
    private readonly ISettingService _settingService;

    public ProductSyncPlugin(IWebHelper webHelper, ILocalizationService localizationService, ISettingService settingService)
    {
        _webHelper = webHelper;
        _localizationService = localizationService;
        _settingService = settingService;
    }

    public override async Task InstallAsync()
    {
        var productSyncService = new ProductSyncSettings()
        {
            DeleteProduct = true,
            Enabled = true,
            SyncImages = true
        };
        await _settingService.SaveSettingAsync(productSyncService);
        await base.InstallAsync();
    }
    
    public override async Task UninstallAsync()
    {
        await _settingService.DeleteSettingAsync<ProductSyncSettings>();
        await base.InstallAsync();
    }

    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/SeedProductSync/Configure";
    }
}