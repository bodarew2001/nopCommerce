using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Plugin.Seed.ProductSync.Clients;
using Nop.Plugin.Seed.ProductSync.Models.ApiModels;
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

    #endregion

    #region ctor

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

    #endregion

    #region Methods

    public async Task<List<Product>> GetAllProducts()
    {
        var models = await _infigoClient.GetListAsync();
        var entities = await models.Select(x => MapToProductEntity(x)).ToListAsync();

        return entities;
    }

    public async Task<Product> GetByIdProductEntity(int apiDataModelId)
    {
        var apiDataModel = await _infigoClient.GetByIdAsync(apiDataModelId);
        var entity = MapToProductEntity(apiDataModel);
        return entity;
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
        }
        if (_settings.DeleteProduct)
        {
            await Delete();
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
            var productPicture = await GetProductPicture(thumbnail, entity.Id);
            await _productService.InsertProductPictureAsync(productPicture);
        }
        //Insert attributes
        var attributes = await GetByIdProductAttributes(model.Id);
        await UpdateOrCreateProductAttributes(attributes,entity.Id);
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
                    var productPicture = await GetProductPicture(thumbnail, product.Id);
                    await _productService.UpdateProductPictureAsync(productPicture);
                }
            }
            var attributes = await GetByIdProductAttributes(model.Id);
            if (!attributes.IsNullOrEmpty())
            {
                await UpdateOrCreateProductAttributes(attributes,updatedProduct.Id);
            }
        }
    }

    public async Task Delete()
    {
        var infigoProductsTags = await _productTagService.GetAllProductTagsAsync($"infigo_product");
        var infigoDbProductsIds = await infigoProductsTags.Select(x => Int32.Parse(Regex.Match(x.Name, @"\d+").Value)).ToListAsync();
        var infigoApiProductsIds = await _infigoClient.GetIdsAsync();

        var idsToDelete = infigoDbProductsIds.Except(infigoApiProductsIds);

        foreach (var id in idsToDelete)
        {
            var product = await GetProductByInfigoId(id);
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

    #endregion

    #region Utilities

    #region Mappers

    private ProductAttribute MapAttribute(ApiProductAttributeModel apiModel)
    {
        return new ProductAttribute()
        {
            Description = apiModel.Description,
            Name = apiModel.Name
        };
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
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow,
            Published = true
        };
        return entity;
    }

    #endregion

    #region Others

    public async Task UpdateOrCreateProductAttributes(List<ApiProductAttributeModel> attributes,int productId=0)
    {
        foreach (var attribute in attributes)
        {
            var productAttribute = await _productAttributeService.GetProductAttributeByNameAsync(attribute.Name);
            if (productAttribute is null)
            {
                var entity = MapAttribute(attribute);
                await _productAttributeService.InsertProductAttributeAsync(entity);
                if (!attribute.ProductAttributeValues.IsNullOrEmpty())
                {
                    var productAttributeMapping = new ProductAttributeMapping()
                    {
                        AttributeControlTypeId = attribute.AttributeControlType,
                        IsRequired = attribute.IsRequired,
                        ProductId = productId,
                        ProductAttributeId = entity.Id
                    };
                    await _productAttributeService.InsertProductAttributeMappingAsync(productAttributeMapping);
                    foreach (var value in attribute.ProductAttributeValues)
                    {
                        var productAttributeValue = new ProductAttributeValue()
                        {
                            ProductAttributeMappingId = productAttributeMapping.Id,
                            Name = value.Name,
                            PriceAdjustment = value.PriceAdjustment,
                            IsPreSelected = value.IsPreSelected,
                            DisplayOrder = value.DisplayOrder,
                            AttributeValueType = AttributeValueType.Simple,
                            WeightAdjustment = value.WeightAdjustment,
                        };
                        await _productAttributeService.InsertProductAttributeValueAsync(productAttributeValue);
                    }
                }
            }
            else
            {
                await _productAttributeService.UpdateProductAttributeAsync(productAttribute);
                if (!attribute.ProductAttributeValues.IsNullOrEmpty())
                {
                    var productAttributeMappings = await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(productId);

                    if (!productAttributeMappings.IsNullOrEmpty())
                    {
                        var productAttributeMapping =
                            productAttributeMappings.FirstOrDefault(x => x.ProductAttributeId == productAttribute.Id);
                        if (productAttributeMapping != null)
                        {
                            productAttributeMapping.AttributeControlTypeId = attribute.AttributeControlType;
                            productAttributeMapping.IsRequired = attribute.IsRequired;
                            productAttributeMapping.ProductId = productId;
                            productAttributeMapping.ProductAttributeId = productAttribute.Id;

                            await _productAttributeService.UpdateProductAttributeMappingAsync(productAttributeMapping);
                            var attributeValues =
                                await _productAttributeService.GetProductAttributeValuesAsync(productId);
                            foreach (var value in attribute.ProductAttributeValues)
                            {
                                var valueEntity = attributeValues.FirstOrDefault(x => x.Name == value.Name);
                                if (valueEntity != null)
                                {
                                    valueEntity.ProductAttributeMappingId = productAttributeMapping.Id;
                                    valueEntity.Name = value.Name;
                                    valueEntity.PriceAdjustment = value.PriceAdjustment;
                                    valueEntity.IsPreSelected = value.IsPreSelected;
                                    valueEntity.DisplayOrder = 1;
                                    valueEntity.WeightAdjustment = value.WeightAdjustment;
                                    valueEntity.AttributeValueType = AttributeValueType.Simple;

                                    await _productAttributeService.UpdateProductAttributeValueAsync(valueEntity);
                                }
                            }
                        }
                    }
                    else
                    {
                        var productAttributeMapping = new ProductAttributeMapping()
                        {
                            AttributeControlTypeId = attribute.AttributeControlType,
                            IsRequired = attribute.IsRequired,
                            ProductId = productId,
                            ProductAttributeId = productAttribute.Id
                        };
                        await _productAttributeService.InsertProductAttributeMappingAsync(productAttributeMapping);
                        foreach (var value in attribute.ProductAttributeValues)
                        {
                            var productAttributeValue = new ProductAttributeValue()
                            {
                                ProductAttributeMappingId = productAttributeMapping.Id,
                                Name = value.Name,
                                PriceAdjustment = value.PriceAdjustment,
                                IsPreSelected = value.IsPreSelected,
                                DisplayOrder = 1,
                                AttributeValueType = AttributeValueType.Simple,
                                WeightAdjustment = value.WeightAdjustment,
                            };
                            await _productAttributeService.InsertProductAttributeValueAsync(productAttributeValue);
                        }
                    }
                }
            }
        }
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

    public async Task<Product> GetProductByInfigoId(int id)
    {
        var tagName = $"infigo_product_{id}";
        var tags = await _productTagService.GetAllProductTagsAsync(tagName);
        if (!tags.IsNullOrEmpty())
        {
            var first = tags?.First();
            var productProductTagMappings = await _productProductTagMapping.Table.FirstOrDefaultAsync(x=>x.ProductTagId==first.Id);
            var product = await _productService.GetProductByIdAsync(productProductTagMappings.ProductId);
            return product;
        }

        return null;
    }
    
    public async Task<List<ApiProductAttributeModel>> GetByIdProductAttributes(int apiDataModelId)
    {
        var apiDataModel = await _infigoClient.GetByIdAsync(apiDataModelId);
        return apiDataModel.ProductAttributes;
    }

    #endregion

    #endregion
}