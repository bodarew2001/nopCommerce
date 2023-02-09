using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Seed.ProductSync.Models.ApiModels;

namespace Nop.Plugin.Seed.ProductSync.Clients;

public interface IInfigoClient
{
    Task<List<ApiDataModel>> GetListAsync();
    Task<List<int>> GetIdsAsync();
    Task<ApiDataModel> GetByIdAsync(int id);
}