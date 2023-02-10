using System;
using System.Collections.Generic;
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
        if(IsConfigured(_settings))
        {
            
            var options = new RestClientOptions($"{_settings.InfigoUrl}")
            {
                Authenticator = new HttpBasicAuthenticator(_settings.ApiToken,"")
            };
            _client = new RestClient(options);
        }
    }

    public async Task<List<ApiDataModel>> GetList()
    {
        if (IsConfigured(_settings))
        {
            var ids = await GetIds();
            var dataModels = new List<ApiDataModel>();
            foreach (var id in ids)
            {
                try
                {
                    var model = await GetById(id);
                    dataModels.Add(model);
                }
                catch (Exception ex)
                {
                    throw new Exception($"GetList of ApiDataModel failed on model with id {id}!", ex);
                }
            }

            return dataModels;
        }

        throw new Exception("Product sync plugin isn't configured!");
    }

    public async Task<List<int>> GetIds()
    {
        if (IsConfigured(_settings))
        {
            var request = new RestRequest($"Services/api/Catalog/ProductList");
            var response = await _client.ExecuteAsync(request);

            var model = JsonSerializer.Deserialize<List<int>>(response.Content);
            return model;
        }

        throw new Exception("Product sync plugin isn't configured!");
    }

    public async Task<ApiDataModel> GetById(int id)
    {
        if (IsConfigured(_settings))
        {
            var request = new RestRequest($"Services/api/Catalog/ProductDetails/{id}");
            var response = await _client.ExecuteAsync(request);

            var model = JsonSerializer.Deserialize<ApiDataModel>(response.Content);
            return model;
        }
        throw new Exception("Product sync plugin isn't configured!");
    }
    
    public bool IsConfigured(ProductSyncSettings settings)
    {
        return !string.IsNullOrEmpty(settings?.InfigoUrl) && !string.IsNullOrEmpty(settings?.ApiToken);
    }
}