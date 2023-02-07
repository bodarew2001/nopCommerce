using FluentMigrator;
using FluentMigrator.Builders.Update;
using Nop.Core.Domain.Configuration;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Data.Migrations;
using Nop.Services.Configuration;

namespace Nop.Web.Framework.Migrations.CustomMigrations;

[NopMigration("2022-02-07 00:00:00", "4.70.0", UpdateMigrationType.Settings, MigrationProcessType.Update)]
public class UpdateSettingsMigration:MigrationBase
{
    public override void Up()
    {
        var settingService = EngineContext.Current.Resolve<ISettingService>();

        var gridPageSizesSetting = settingService.GetSetting("adminareasettings.gridpagesizes");
        gridPageSizesSetting.Value = "10, 20, 50, 100";
        
        settingService.UpdateSettingAsync(gridPageSizesSetting);
    }

    public override void Down()
    {
        
    }
}