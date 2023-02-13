using System.Threading.Tasks;
using Nop.Plugin.Seed.ProductSync.Services;
using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Seed.ProductSync.Tasks;

public class MergeTask:IScheduleTask
{
    private readonly IProductSyncService _productSyncService;

    public MergeTask(IProductSyncService productSyncService)
    {
        _productSyncService = productSyncService;
    }

    public async Task ExecuteAsync()
    {
        await _productSyncService.Merge();
    }
}