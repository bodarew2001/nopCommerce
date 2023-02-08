using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Nop.Plugin.Seed.ProductSync.Models;
using RestSharp;
using RestSharp.Authenticators;

namespace Nop.Plugin.Seed.ProductSync.Services;

public class ProductSyncService
{
    private readonly ProductSyncSettings _settings;

    public ProductSyncService(ProductSyncSettings settings)
    {
        _settings = settings;
    }

    public async Task<List<ProductModel>> GetAllProductsDetails()
    {
        if (ProductSyncService.IsConfigured(_settings))
        {
            var ids = await GetProductIdsAsync();
            var products = await ids.Select(x => GetProductByIdAsync(x).Result).ToListAsync();
            return products;
        }

        return null;
    }

    public async Task<List<int>> GetProductIdsAsync()
    {
        if (ProductSyncService.IsConfigured(_settings))
        {
            var options = new RestClientOptions($"{_settings.InfigoUrl}")
            {
                Authenticator = new HttpBasicAuthenticator(_settings.ApiToken,"")
            };
            var client = new RestClient(options);
            var request = new RestRequest($"Services/api/Catalog/ProductList");
            var response = await client.ExecuteAsync(request);

            var model = JsonSerializer.Deserialize<List<int>>(response.Content);
            return model;
        }

        return null;
    }

    public async Task<ProductModel> GetProductByIdAsync(int id)
    {
        if (ProductSyncService.IsConfigured(_settings))
        {
            var options = new RestClientOptions($"{_settings.InfigoUrl}")
            {
                Authenticator = new HttpBasicAuthenticator(_settings.ApiToken,"")
            };
            var client = new RestClient(options);
            var request = new RestRequest($"Services/api/Catalog/ProductDetails/{id}");
            var response = await client.ExecuteAsync(request);

            var model = JsonSerializer.Deserialize<ProductModel>(response.Content);
            return model;
        }

        return null;
    }
    
    public static bool IsConfigured(ProductSyncSettings settings)
    {
        //Client ID and API Key are required to request services
        return !string.IsNullOrEmpty(settings?.InfigoUrl) && !string.IsNullOrEmpty(settings?.ApiToken);
    }
}