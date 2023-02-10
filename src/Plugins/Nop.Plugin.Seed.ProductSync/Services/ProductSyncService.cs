using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Data;
using Nop.Plugin.Seed.ProductSync.Clients;
using Nop.Plugin.Seed.ProductSync.Factories;
using Nop.Plugin.Seed.ProductSync.Models.ApiModels;
using Nop.Plugin.Seed.ProductSync.Services.Helpers;
using Nop.Services.Catalog;
using Nop.Services.Media;
using Nop.Services.Security;

namespace Nop.Plugin.Seed.ProductSync.Services;

public class ProductSyncService : IProductSyncService
{
    #region fields

    private readonly ProductSyncSettings _settings;
    private readonly IProductTagService _productTagService;
    private readonly IProductService _productService;
    private readonly IProductAttributeService _productAttributeService;
    private readonly IInfigoClient _infigoClient;
    private readonly IPictureService _pictureService;
    private readonly IRepository<ProductProductTagMapping> _productProductTagMapping;
    private readonly IProductSyncFactory _productSyncFactory;

    #endregion

    #region ctor

    public ProductSyncService(
        ProductSyncSettings settings,
        IPermissionService permissionService,
        IProductTagService productTagService,
        IProductService productService,
        IInfigoClient infigoClient, IProductAttributeService productAttributeService, IPictureService pictureService,
        IRepository<ProductProductTagMapping> productProductTagMapping, IProductSyncFactory productSyncFactory)
    {
        _settings = settings;
        _productTagService = productTagService;
        _productService = productService;
        _infigoClient = infigoClient;
        _productAttributeService = productAttributeService;
        _pictureService = pictureService;
        _productProductTagMapping = productProductTagMapping;
        _productSyncFactory = productSyncFactory;
    }

    #endregion

    #region Methods

    public async Task Merge()
    {
        var apiModels = await _infigoClient.GetList();
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
        }

        if (_settings.DeleteProduct)
        {
            await DeleteUnmatched();
        }
    }

    public async Task Create(ApiDataModel model)
    {
        //Insert product
        var entity = MapToProductEntity(model);
        await _productService.InsertProductAsync(entity);
        model.Tags.Add($"infigo_product_{model.Id}");
        var tags = model.Tags.ToArray();
        await _productTagService.UpdateProductTagsAsync(entity, tags);

        //upload images
        foreach (var thumbnail in model.ThumbnailUrls)
        {
            await InsertProductPicture(thumbnail, entity.Id);
        }

        //Insert attributes
        var attributes = await GetByIdProductAttributes(model.Id);
        await UpdateOrCreateProductAttributes(attributes, entity.Id);
    }

    public async Task Update(ApiDataModel model)
    {
        var product = await GetProductByInfigoId(model.Id);
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
                    await InsertProductPicture(thumbnail, product.Id);
                }
            }

            var attributes = await GetByIdProductAttributes(model.Id);
            if (!attributes.IsNullOrEmpty())
            {
                await UpdateOrCreateProductAttributes(attributes, updatedProduct.Id);
            }
        }
    }

    public async Task DeleteUnmatched()
    {
        var infigoProductsTags = await _productTagService.GetAllProductTagsAsync($"infigo_product");
        var infigoDbProductsIds = await infigoProductsTags.Select(x => Int32.Parse(Regex.Match(x.Name, @"\d+").Value))
            .ToListAsync();
        var infigoApiProductsIds = await _infigoClient.GetIds();

        var idsToDelete = infigoDbProductsIds.Except(infigoApiProductsIds);

        foreach (var id in idsToDelete)
        {
            var product = await GetProductByInfigoId(id);
            await _productService.DeleteProductAsync(product);
        }
    }

    #endregion

    #region Utilities

    #region Mappers

    private ProductAttribute MapAttribute(ApiProductAttributeModel apiModel)
    {
        return new ProductAttribute() { Description = apiModel.Description, Name = apiModel.Name };
    }

    private Product MapToProductEntity(ApiDataModel model)
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
            Published = true
        };
        return entity;
    }

    #endregion

    #region Others

    public async Task InsertProductAttributeValues(int productId, int productAttributeId,
        ApiProductAttributeModel attribute)
    {
        var productAttributeMapping =
            await _productSyncFactory.PrepareProductAttributeMapping(productId, productAttributeId, attribute);
        await _productAttributeService.InsertProductAttributeMappingAsync(productAttributeMapping);
        foreach (var value in attribute.ProductAttributeValues)
        {
            var productAttributeValue =
                await _productSyncFactory.PrepareProductAttributeValue(productAttributeMapping.Id, value);
            await _productAttributeService.InsertProductAttributeValueAsync(productAttributeValue);
        }
    }

    public async Task UpdateOrCreateProductAttributes(List<ApiProductAttributeModel> attributes, int productId = 0)
    {
        foreach (var attribute in attributes)
        {
            var productAttribute = await _productAttributeService.GetProductAttributeByNameAsync(attribute.Name);
            if (productAttribute is null)
            {
                productAttribute = MapAttribute(attribute);
                await _productAttributeService.InsertProductAttributeAsync(productAttribute);
            }
            else
            {
                await _productAttributeService.UpdateProductAttributeAsync(productAttribute);
            }
            var productAttributeMapping =
                await _productAttributeService.GetProductAttributeMappingAsync(productId, productAttribute.Id);
            if (productAttributeMapping is not null)
            {
                await _productAttributeService.DeleteProductAttributeMappingAsync(productAttributeMapping);
            }
            if (!attribute.ProductAttributeValues.IsNullOrEmpty())
            {
                await InsertProductAttributeValues(productId, productAttribute.Id, attribute);
            }
        }
    }

    public async Task InsertProductPicture(string thumbnail, int productId)
    {
        var fileExtention = Path.GetExtension(Path.GetFileName(thumbnail))?.Remove(0, 1);
        var mimeType = $"image/{fileExtention}";
        var byteData = await PluginHelpers.GetByteDataFromUrl(thumbnail);
        var filename = Path.GetFileNameWithoutExtension(thumbnail);
        var picture = await _pictureService.InsertPictureAsync(byteData, mimeType, filename);
        await _productService.InsertProductPictureAsync(new ProductPicture()
        {
            PictureId = picture.Id, ProductId = productId
        });
    }

    public async Task<Product> GetProductByInfigoId(int id)
    {
        var tagName = $"infigo_product_{id}";
        return await _productService.GetByTag(tagName);
    }

    public async Task<List<ApiProductAttributeModel>> GetByIdProductAttributes(int apiDataModelId)
    {
        var apiDataModel = await _infigoClient.GetById(apiDataModelId);
        return apiDataModel.ProductAttributes;
    }
    
    public async Task<Product> GetByIdProductEntity(int apiDataModelId)
    {
        var apiDataModel = await _infigoClient.GetById(apiDataModelId);
        var entity = MapToProductEntity(apiDataModel);
        return entity;
    }

    #endregion

    #endregion
}