using System.IO;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Seed.ProductSync.Models;
using Nop.Plugin.Seed.ProductSync.Models.ApiModels;
using Nop.Plugin.Seed.ProductSync.Services.Helpers;

namespace Nop.Plugin.Seed.ProductSync.Factories;

public class ProductSyncFactory:IProductSyncFactory
{
    public async Task<ProductAttributeMapping> PrepareProductAttributeMapping(int productId, int attributeId,
        ApiProductAttributeModel apiModel, ProductAttributeMapping mappingEntity = null)
    {
        mappingEntity ??= new ProductAttributeMapping();
        mappingEntity.AttributeControlTypeId = apiModel.AttributeControlType;
        mappingEntity.IsRequired = apiModel.IsRequired;
        mappingEntity.ProductId = productId;
        mappingEntity.ProductAttributeId = attributeId;

        return mappingEntity;
    }

    public async Task<ProductAttributeValue> PrepareProductAttributeValue(int productAttributeMappingId,
        ApiProductAttributeValueModel value, ProductAttributeValue valueEntity = null)
    {
        valueEntity ??= new ProductAttributeValue();
        valueEntity.ProductAttributeMappingId = productAttributeMappingId;
        valueEntity.Name = value.Name;
        valueEntity.PriceAdjustment = value.PriceAdjustment;
        valueEntity.IsPreSelected = value.IsPreSelected;
        valueEntity.DisplayOrder = value.DisplayOrder;
        valueEntity.AttributeValueType = AttributeValueType.Simple;
        valueEntity.WeightAdjustment = value.WeightAdjustment;
        
        return valueEntity;
    }

    public async Task<PictureModel> PreparePictureModel(string thumbnail)
    {
        var fileExtention = Path.GetExtension(Path.GetFileName(thumbnail))?.Remove(0, 1);
        var mimeType = $"image/{fileExtention}";
        var byteData = await PluginHelpers.GetByteDataFromUrl(thumbnail);
        var filename = Path.GetFileNameWithoutExtension(thumbnail);

        return new PictureModel()
        {
            FileExtention = fileExtention, MimeType = mimeType, ByteData = byteData, FileName = filename
        };
    }
}