using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Seed.ProductSync.Models;
using Nop.Plugin.Seed.ProductSync.Models.ApiModels;
using Nop.Services.Catalog;

namespace Nop.Plugin.Seed.ProductSync.Services;

public interface IProductSyncService
{
    Task Merge();
    Task Create(ApiDataModel model);
    Task Update(ApiDataModel model);
    Task DeleteUnmatched();
}