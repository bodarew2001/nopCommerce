using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Seed.ProductSync.Models;
using Nop.Plugin.Seed.ProductSync.Services;
using Nop.Services.Configuration;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Seed.ProductSync.Controllers;

[AutoValidateAntiforgeryToken]
[AuthorizeAdmin]
[Area(AreaNames.Admin)]
public class SeedProductSyncController : BasePluginController
{
    private readonly IPermissionService _permissionService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;
    private readonly ProductSyncService _productSyncService;

    public SeedProductSyncController(IPermissionService permissionService, ISettingService settingService, ProductSyncService productSyncService)
    {
        _permissionService = permissionService;
        _settingService = settingService;
        _productSyncService = productSyncService;
    }

    public async Task<IActionResult> Configure()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
            return AccessDeniedView();

        var productSyncSettings = await _settingService.LoadSettingAsync<ProductSyncSettings>();
        var testResult = await _productSyncService.GetProductByIdAsync(1178);
        var testResultIds = await _productSyncService.GetProductIdsAsync();
        var testResultProducts = await _productSyncService.GetAllProductsDetails();
        var model = new ConfigurationModel()
        {
            Enabled = productSyncSettings.Enabled,
            SyncImages = productSyncSettings.SyncImages,
            DeleteProduct = productSyncSettings.DeleteProduct,
            ApiToken = productSyncSettings.ApiToken,
            InfigoUrl = productSyncSettings.InfigoUrl
        };
        
        return View("~/Plugins/Seed.ProductSync/Views/Configure.cshtml",model);
    }
    
    [HttpPost]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
            return AccessDeniedView();

        if (!ModelState.IsValid)
        {
            return View("~/Plugins/Seed.ProductSync/Views/Configure.cshtml",model);
        }

        var productSyncSettings = await _settingService.LoadSettingAsync<ProductSyncSettings>();
        productSyncSettings.DeleteProduct = model.DeleteProduct;
        productSyncSettings.SyncImages = model.SyncImages;
        productSyncSettings.Enabled = model.Enabled;
        productSyncSettings.InfigoUrl = model.InfigoUrl;
        productSyncSettings.ApiToken = model.ApiToken;

        await _settingService.SaveSettingOverridablePerStoreAsync(productSyncSettings,
            productSyncSettings => productSyncSettings.InfigoUrl,true,0, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(productSyncSettings,
            productSyncSettings => productSyncSettings.ApiToken,true,0, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(productSyncSettings,
            productSyncSettings => productSyncSettings.Enabled,true,0, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(productSyncSettings,
            productSyncSettings => productSyncSettings.DeleteProduct,true,0, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(productSyncSettings,
            productSyncSettings => productSyncSettings.SyncImages,true,0, false);

        await _settingService.ClearCacheAsync();

        return View("~/Plugins/Seed.ProductSync/Views/Configure.cshtml");
    }
}