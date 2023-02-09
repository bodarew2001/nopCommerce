using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Nop.Plugin.Seed.ProductSync.Models.ApiModels;
using RestSharp;
using RestSharp.Authenticators;

namespace Nop.Plugin.Seed.ProductSync.Clients;

public class InfigoClient:IInfigoClient
{
    
    private readonly ProductSyncSettings _settings;
    private readonly RestClient _client;

    public InfigoClient(ProductSyncSettings settings)
    {
        _settings = settings;
        var options = new RestClientOptions($"{_settings.InfigoUrl}")
        {
            Authenticator = new HttpBasicAuthenticator(_settings.ApiToken,"")
        };
        _client = new RestClient(options);
    }

    public async Task<List<ApiDataModel>> GetListAsync()
    {
        if (InfigoClient.IsConfigured(_settings))
        {
            var ids = await GetIdsAsync();
            var dataModels = new List<ApiDataModel>();
            foreach (var id in ids)
            {
                var model = await GetByIdAsync(id);
                dataModels.Add(model);
            }

            return dataModels;
        }

        return null;
    }

    public async Task<List<int>> GetIdsAsync()
    {
        if (InfigoClient.IsConfigured(_settings))
        {
            var request = new RestRequest($"Services/api/Catalog/ProductList");
            var response = await _client.ExecuteAsync(request);

            var model = JsonSerializer.Deserialize<List<int>>(response.Content);
            return model;
        }
        return null;
    }

    public async Task<ApiDataModel> GetByIdAsync(int id)
    {
        if (InfigoClient.IsConfigured(_settings))
        {
            var request = new RestRequest($"Services/api/Catalog/ProductDetails/{id}");
            var response = await _client.ExecuteAsync(request);

            var model = JsonSerializer.Deserialize<ApiDataModel>(response.Content);
            return model;
        }
        return null;
    }
    
    public static bool IsConfigured(ProductSyncSettings settings)
    {
        return !string.IsNullOrEmpty(settings?.InfigoUrl) && !string.IsNullOrEmpty(settings?.ApiToken);
    }
}