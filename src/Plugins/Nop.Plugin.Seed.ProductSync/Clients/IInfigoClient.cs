using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Seed.ProductSync.Models.ApiModels;

namespace Nop.Plugin.Seed.ProductSync.Clients;

public interface IInfigoClient
{
    Task<List<ApiDataModel>> GetList();
    Task<List<int>> GetIds();
    Task<ApiDataModel> GetById(int id);
}