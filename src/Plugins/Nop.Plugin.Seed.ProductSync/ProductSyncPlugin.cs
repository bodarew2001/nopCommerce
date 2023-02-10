using System;
using System.Collections.Generic;
using Nop.Core;
using Nop.Core.Domain.ScheduleTasks;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;
using Svg.FilterEffects;
using Task = System.Threading.Tasks.Task;

namespace Nop.Plugin.Seed.ProductSync;

public class ProductSyncPlugin:BasePlugin
{
    private readonly IWebHelper _webHelper;
    private readonly ILocalizationService _localizationService;
    private readonly ISettingService _settingService;
    private readonly IScheduleTaskService _scheduleTaskService;

    public ProductSyncPlugin(IWebHelper webHelper, ILocalizationService localizationService, ISettingService settingService, IScheduleTaskService scheduleTaskService)
    {
        _webHelper = webHelper;
        _localizationService = localizationService;
        _settingService = settingService;
        _scheduleTaskService = scheduleTaskService;
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
        await _scheduleTaskService.InsertTaskAsync(new ScheduleTask()
        {
            Name = "Merge with InfigoAPI",
            Seconds = 300,
            Type = "Nop.Plugin.Seed.ProductSync.Tasks.MergeTask",
            Enabled = true,
            StopOnError = false,
            LastEnabledUtc = DateTime.UtcNow
        });
        await base.InstallAsync();
    }
    
    public override async Task UninstallAsync()
    {
        await _settingService.DeleteSettingAsync<ProductSyncSettings>();
        var task = await _scheduleTaskService.GetTaskByTypeAsync("Nop.Plugin.Seed.ProductSync.Tasks.MergeTask");
        await _scheduleTaskService.DeleteTaskAsync(task);
        await base.InstallAsync();
    }

    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/SeedProductSync/Configure";
    }
}