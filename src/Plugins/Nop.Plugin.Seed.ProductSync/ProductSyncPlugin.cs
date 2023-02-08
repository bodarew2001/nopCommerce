using Nop.Core;
using Nop.Services.Plugins;
using Task = System.Threading.Tasks.Task;

namespace Nop.Plugin.Seed.ProductSync;

public class ProductSyncPlugin:BasePlugin
{
    private readonly IWebHelper _webHelper;

    public ProductSyncPlugin(IWebHelper webHelper)
    {
        _webHelper = webHelper;
    }

    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/SeedProductSync/Configure";
    }
}