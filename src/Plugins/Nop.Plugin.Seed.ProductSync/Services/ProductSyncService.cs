using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Tax;
using Nop.Data;
using Nop.Plugin.Seed.ProductSync.Clients;
using Nop.Plugin.Seed.ProductSync.Models;
using Nop.Plugin.Seed.ProductSync.Models.ApiModels;
using Nop.Services.Catalog;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Catalog;
using RestSharp;
using RestSharp.Authenticators;

namespace Nop.Plugin.Seed.ProductSync.Services;

public class ProductSyncService : IProductSyncService
{
    private readonly ProductSyncSettings _settings;
    private readonly IProductTagService _productTagService;
    private readonly IProductService _productService;
    private readonly IProductAttributeService _productAttributeService;
    private readonly IInfigoClient _infigoClient;
    private readonly IPictureService _pictureService;
    private readonly IRepository<ProductProductTagMapping> _productProductTagMapping;

    public ProductSyncService(
        ProductSyncSettings settings, 
        IPermissionService permissionService,
        IProductTagService productTagService, 
        IProductService productService,
        IInfigoClient infigoClient, IProductAttributeService productAttributeService, IPictureService pictureService, IRepository<ProductProductTagMapping> productProductTagMapping)
    {
        _settings = settings;
        _productTagService = productTagService;
        _productService = productService;
        _infigoClient = infigoClient;
        _productAttributeService = productAttributeService;
        _pictureService = pictureService;
        _productProductTagMapping = productProductTagMapping;
    }


    public async Task<List<Product>> GetAllProducts()
    {
        var models = await _infigoClient.GetListAsync();
        var entities = await models.Select(x => MapToProductEntity(x)).ToListAsync();

        return entities;
    }

    public Product MapToProductEntity(ApiDataModel model)
    {
        var entity = new Product()
        {
            ProductTypeId = model.Type,
            StockQuantity = model.StockValue,
            Price = model.Price,
            Name = model.Name,
            ShortDescription = model.ShortDescription,
            FullDescription = model.LongDescription,
            Sku = model.Sku,
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow,
            Published = true
        };
        return entity;
    }
    
    public async Task<Product> GetByIdProductEntity(int apiDataModelId)
    {
        var apiDataModel = await _infigoClient.GetByIdAsync(apiDataModelId);
        var entity = new Product()
        {
            ProductTypeId = apiDataModel.Type,
            StockQuantity = apiDataModel.StockValue,
            Price = apiDataModel.Price,
            Name = apiDataModel.Name,
            ShortDescription = apiDataModel.ShortDescription,
            FullDescription = apiDataModel.LongDescription,
            Sku = apiDataModel.Sku,
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow,
            Published = true
        };
        return entity;
    }

    public async Task<List<ProductAttribute>> GetByIdProductAttributes(int apiDataModelId)
    {
        var apiDataModel = await _infigoClient.GetByIdAsync(apiDataModelId);
        var productAttributes = new List<ProductAttribute>();
        foreach (var attribute in apiDataModel.ProductAttributes)
        {
            productAttributes.Add(new ProductAttribute()
            {
                Name = attribute.Name, Description = attribute.Description
            });
        }

        return productAttributes;
    }

    public async Task Merge()
    {
        var apiModels = await _infigoClient.GetListAsync();
        foreach (var model in apiModels)
        {
            var dbProduct = await GetProductByInfigoId(model.Id);
            if (dbProduct is null)
            {
                await Create(model);
            }
            else
            {
                await Update(model);
            }

            if (_settings.DeleteProduct)
            {
                await Delete(model);
            }
        }
    }

    public async Task Create(ApiDataModel model)
    {
        //Insert product with images
        var entity = MapToProductEntity(model);
        await _productService.InsertProductAsync(entity);
        model.Tags.Add($"infigo_product_{model.Id}");
        var tags = model.Tags.ToArray();
        await _productTagService.UpdateProductTagsAsync(entity, tags);
        
        //upload images
        foreach (var thumbnail in model.ThumbnailUrls)
        {
            var productPicture = await GetProductPicture(thumbnail, entity.Id);
            await _productService.InsertProductPictureAsync(productPicture);
        }
        //Insert attributes
        var attributes = await GetByIdProductAttributes(model.Id);
        await UpdateOrCreateProductAttributes(attributes);
    }

    public async Task Update(ApiDataModel model)
    {
        var tagName = $"infigo_product_{model.Id}";
        var product = await GetProductByTagName(tagName);
        if (product is null)
        {
            await Create(model);
        }
        else
        {
            var updatedProduct = await GetByIdProductEntity(model.Id);
            updatedProduct.Id = product.Id;
            await _productService.UpdateProductAsync(updatedProduct);
            if (_settings.SyncImages)
            {
                foreach (var thumbnail in model.ThumbnailUrls)
                {
                    var productPicture = await GetProductPicture(thumbnail, product.Id);
                    await _productService.UpdateProductPictureAsync(productPicture);
                }
            }
            var attributes = await GetByIdProductAttributes(model.Id);
            await UpdateOrCreateProductAttributes(attributes);
        }
    }

    public async Task Delete(ApiDataModel model)
    {
        var infigoProductsTags = await _productTagService.GetAllProductTagsAsync($"infigo_product");
        var infigoDbProductsIds = await infigoProductsTags.Select(x => Int32.Parse(Regex.Match(x.Name, @"\d+").Value)).ToListAsync();
        var infigoApiProductsIds = await _infigoClient.GetIdsAsync();

        var idsToDelete = infigoDbProductsIds.Except(infigoApiProductsIds);

        foreach (var id in idsToDelete)
        {
            var product = await GetProductByInfigoId(model.Id);
            await _productService.DeleteProductAsync(product);
        }
    }

    private async Task<byte[]> GetByteDataFromUrl(string url)
    {
        var client = new HttpClient();
        var result = await client.GetAsync(url);

        var content = result.Content;

        var memoryStream= new MemoryStream();

        await content.CopyToAsync(memoryStream);

        return memoryStream.ToArray();
    }

    public async Task<Product> GetProductByTagName(string tagName)
    {
        var tags = await _productTagService.GetAllProductTagsAsync(tagName);
        if (!tags.IsNullOrEmpty())
        {
            var first = tags?.First();
            var productProductTagMappings = await _productProductTagMapping.Table.FirstOrDefaultAsync(x=>x.ProductTagId==first.Id);
            return await _productService.GetProductByIdAsync(productProductTagMappings.ProductId);
        }

        return null;
    }

    public async Task<ProductPicture> GetProductPicture(string thumbnail, int productId)
    {
        var fileExtention = Path.GetExtension(Path.GetFileName(thumbnail))?.Remove(0,1);
        var mimeType = $"image/{fileExtention}";
        var byteData = await GetByteDataFromUrl(thumbnail);
        var filename = Path.GetFileNameWithoutExtension(thumbnail);
        var picture = await _pictureService.InsertPictureAsync(byteData, mimeType, filename);
        return new ProductPicture() { PictureId = picture.Id, ProductId = productId };
    }

    public async Task UpdateOrCreateProductAttributes(List<ProductAttribute> attributes)
    {
        foreach (var attribute in attributes)
        {
            var productAttribute = await _productAttributeService.GetProductAttributeByNameAsync(attribute.Name);
            if (productAttribute is null)
            {
                await _productAttributeService.InsertProductAttributeAsync(attribute);
            }
            else
            {
                await _productAttributeService.UpdateProductAttributeAsync(attribute);
            }
        }
    }

    public async Task<Product> GetProductByInfigoId(int id)
    {
        var tagName = $"infigo_product_{id}";
        var product = await GetProductByTagName(tagName);

        return product;
    }
}