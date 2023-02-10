using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Seed.ProductSync.Models.ApiModels;

namespace Nop.Plugin.Seed.ProductSync.Factories;

public interface IProductSyncFactory
{
    Task<ProductAttributeValue> PrepareProductAttributeValue(int productAttributeMappingId,
        ApiProductAttributeValueModel value, ProductAttributeValue valueEntity = null);

    Task<ProductAttributeMapping> PrepareProductAttributeMapping(int productId, int attributeId,
        ApiProductAttributeModel apiModel, ProductAttributeMapping mappingEntity = null);
}